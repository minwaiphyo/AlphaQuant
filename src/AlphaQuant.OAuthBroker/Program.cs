using System.Threading.RateLimiting;
using System.Text.Json;
using AlphaQuant.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// OAuth callback query strings and authorization headers must not enter default request/exception logs.
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16384);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 90, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value
    : throw new InvalidOperationException($"Missing required server configuration: {name}.");
var redirect = new Uri(Required("AQ_OAUTH_REDIRECT_URI"));
var broker = new OAuthBrokerService(AlpacaMarketDataProvider.CreateHttpClient(), Required("AQ_ALPACA_CLIENT_ID"),
    Required("AQ_ALPACA_CLIENT_SECRET"), redirect);
builder.Services.AddSingleton(broker);
builder.Services.AddHostedService<SessionExpiry>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    // Binding a loopback listener is the only supported unencrypted development mode.
    if (redirect.Scheme != "https" && !(context.Connection.RemoteIpAddress is { } ip && System.Net.IPAddress.IsLoopback(ip)))
    { context.Response.StatusCode = 403; return; }
    try { await next(); }
    catch (Exception ex) when (ex is JsonException or BadHttpRequestException or ArgumentException)
    { if (!context.Response.HasStarted) { context.Response.StatusCode = 400; await context.Response.WriteAsync("Invalid request."); } }
    catch (Exception)
    { if (!context.Response.HasStarted) { context.Response.StatusCode = 503; await context.Response.WriteAsync("Authorization service unavailable."); } }
});
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.MapPost("/sessions", (StartRequest request) =>
{
    try { return Results.Ok(broker.Start(request.Challenge)); }
    catch (InvalidOperationException) { return Results.StatusCode(429); }
});
app.MapGet("/oauth/callback", async (HttpContext context) =>
{
    var state = context.Request.Query["state"].ToString();
    if (state.Length != 43) return Results.BadRequest("Invalid authorization response.");
    await broker.CompleteAsync(state, context.Request.Query["code"].ToString(),
        context.Request.Query.ContainsKey("error"), context.RequestAborted);
    return Results.Text("Authorization response received. Return to AlphaQuant. You may close this browser tab.", "text/plain");
});
app.MapPost("/sessions/{id}/result", (string id, VerifyRequest request) =>
{
    var result = broker.Poll(id, request.Verifier);
    return result.State switch
    {
        OAuthPollState.Pending => Results.StatusCode(202),
        OAuthPollState.Complete => Results.Ok(new { accessToken = result.AccessToken }),
        OAuthPollState.Failed => Results.BadRequest(new { error = "Authorization failed. Connect again." }),
        _ => Results.NotFound()
    };
});
app.MapPost("/sessions/{id}/cancel", (string id, VerifyRequest request) =>
{ broker.Cancel(id, request.Verifier); return Results.NoContent(); });
app.Run();

public sealed record StartRequest(string Challenge);
public sealed record VerifyRequest(string Verifier);

internal sealed class SessionExpiry(OAuthBrokerService broker) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken)) broker.ExpireSessions();
    }
}
