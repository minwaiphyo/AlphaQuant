using System.Net;
using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

/// <summary>Complete browser/broker/bearer/IEX/comparison path, exclusively through in-process fakes.</summary>
public sealed class ComparisonBackendEndToEndTests
{
    private sealed class Clock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-28T14:00:00Z"); }
    private sealed class Facts : ICompanyFactsSource
    {
        public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken token) => Task.FromResult(
            new CompanyFacts("Synthetic", ComparisonBackendTests.ReviewedSample(), 0, new Clock().GetUtcNow(), false));
    }
    private sealed class ConsentBrowser(OAuthBrokerService broker) : IBrowserLauncher
    {
        public void Open(Uri uri)
        {
            var state = uri.Query.TrimStart('?').Split('&').Single(p => p.StartsWith("state=", StringComparison.Ordinal))[6..];
            broker.CompleteAsync(state, "synthetic-consent-code", false, default).GetAwaiter().GetResult();
        }
    }
    private sealed class TokenEndpoint : HttpMessageHandler
    {
        public int Exchanges;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("https://api.alpaca.markets/oauth/token", request.RequestUri!.AbsoluteUri);
            var form = await request.Content!.ReadAsStringAsync(token);
            Assert.Contains("client_secret=server-fixture-secret", form);
            Exchanges++;
            return Json(new { token_type = "bearer", scope = "data", access_token = "fixture-bearer" });
        }
    }
    private sealed class BrokerTransport(OAuthBrokerService broker) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.DoesNotContain("client_secret", body.RootElement.ToString());
            var path = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length == 1) return Json(broker.Start(body.RootElement.GetProperty("challenge").GetString()!));
            var verifier = body.RootElement.GetProperty("verifier").GetString()!;
            if (path[^1] == "cancel") { broker.Cancel(path[1], verifier); return new(HttpStatusCode.NoContent); }
            var result = broker.Poll(path[1], verifier);
            return result.State == OAuthPollState.Complete ? Json(new { accessToken = result.AccessToken })
                : new(HttpStatusCode.Accepted);
        }
    }
    private sealed class MarketTransport : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("Bearer fixture-bearer", request.Headers.Authorization!.ToString());
            Assert.Contains("feed=iex", request.RequestUri!.Query);
            return Task.FromResult(Json(new { trades = new Dictionary<string, object>
                { ["ONE"] = new { p = 60 }, ["TWO"] = new { p = 60 } } }));
        }
    }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
        { Content = new StringContent(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))) };

    [Fact]
    public async Task BrowserOAuthThroughMarketAdapterProducesTwoReviewedValuationsWithoutTouchingDesktop()
    {
        var clock = new Clock(); var endpoint = new TokenEndpoint();
        var broker = new OAuthBrokerService(new HttpClient(endpoint), "fixture-client", "server-fixture-secret",
            new("https://broker.example/oauth/callback"), clock);
        var socket = new MarketAdapterTests.Socket(
            """[{"T":"success","msg":"authenticated"}]""",
            """[{"T":"subscription","trades":["ONE","TWO"]}]""",
            """[{"T":"t","S":"ONE","p":60,"i":1,"c":["@"],"t":"2026-10-28T14:00:00Z"},{"T":"t","S":"TWO","p":60,"i":2,"c":["@"],"t":"2026-10-28T14:00:00Z"}]""");
        var backend = new ComparisonBackend(new Facts(), new HttpClient(new BrokerTransport(broker)),
            new("https://broker.example/"), new HttpClient(new MarketTransport()), new ConsentBrowser(broker), clock, () => socket);
        await backend.ConnectWithBrowserAsync(default);
        Assert.True(backend.Authorized);
        Assert.Equal(2, (await backend.TestMarketAccessAsync(["ONE", "TWO"], default)).Symbols.Count);
        var companies = new[] { new CompanyRef(1, "ONE", "One"), new CompanyRef(2, "TWO", "Two") };
        var today = new DateOnly(2026, 10, 28);
        var basis = companies.ToDictionary(c => c.Cik, c => new EquityBasisEvidence(c.Cik, c.Ticker, today, "synthetic review", true));
        var capital = companies.ToDictionary(c => c.Cik, c => new CapitalStructureEvidence(c.Cik, c.Ticker,
            today, today, 100, 400, 100, 0, 0, "synthetic filing", "synthetic review", today, true, true));
        await foreach (var result in backend.WatchComparisonAsync(companies, capital, basis))
        {
            if (!result.Rows.All(r => r.Valuation?.Price is not null)) continue;
            Assert.All(result.Rows, r =>
            {
                Assert.Equal(20m, r.Valuation!.PriceEarnings.Value);
                Assert.Equal(6300m / 1350m, r.Valuation.EnterpriseValueSales.Value);
                Assert.NotEmpty(r.Fundamentals!.TtmRevenue.Inputs);
            });
            break;
        }
        Assert.True(socket.Disposed);
        Assert.Single(socket.Sent, s => s.Contains("\"action\":\"auth\""));
        Assert.Equal(1, endpoint.Exchanges);
        backend.Disconnect(); Assert.False(backend.Authorized);
    }
}
