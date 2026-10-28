using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

/// <summary>Session-only bearer token. Never serializable as an automatic record or printed by ToString.</summary>
public sealed class OAuthSession
{
    private string? _token;
    public bool Connected => Volatile.Read(ref _token) is not null;
    public void Clear() => Interlocked.Exchange(ref _token, null);
    internal string RequireToken() => Volatile.Read(ref _token) ?? throw new MarketDataException(
        MarketFailure.Authentication, "Connect with Alpaca before requesting market data.");
    internal void SetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 8192 || token.Any(char.IsControl))
            throw new MarketDataException(MarketFailure.InvalidData, "The authorization response was invalid.");
        Interlocked.Exchange(ref _token, token);
    }
    public override string ToString() => Connected ? "OAuth session (connected)" : "OAuth session (disconnected)";
}

/// <summary>
/// Browser consent through a configured HTTPS broker; client never receives the registered application's secret.
/// A random verifier binds token retrieval to the initiating desktop. Tokens never appear in browser redirects.
/// </summary>
public sealed class AlpacaBrowserOAuth(HttpClient http, Uri broker, IBrowserLauncher browser, OAuthSession session,
    Func<TimeSpan, CancellationToken, Task>? delay = null)
{
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        ValidateBroker(broker);
        if (broker.AbsolutePath != "/" || !string.IsNullOrEmpty(broker.Query) || !string.IsNullOrEmpty(broker.Fragment))
            throw new MarketDataException(MarketFailure.Configuration, "OAuth broker must be an origin URL.");
        var verifier = Base64(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var token = timeout.Token;
        string? id = null;
        try
        {
            using var response = await http.PostAsJsonAsync(new Uri(broker, "sessions"), new { challenge }, token);
            if (!response.IsSuccessStatusCode) throw Failure();
            using var json = await ReadBoundedAsync(response, token);
            id = json.RootElement.GetProperty("sessionId").GetString();
            var authorize = json.RootElement.GetProperty("authorizationUrl").GetString();
            if (id is null || id.Length != 43 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                || !Uri.TryCreate(authorize, UriKind.Absolute, out var uri) || uri.Scheme != "https"
                || uri.Host != "app.alpaca.markets" || uri.AbsolutePath != "/oauth/authorize" || uri.UserInfo.Length != 0)
                throw Failure();
            var parameters = uri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2))
                .Where(part => part.Length == 2).GroupBy(part => Uri.UnescapeDataString(part[0]))
                .ToDictionary(group => group.Key, group => group.Select(part => Uri.UnescapeDataString(part[1])).ToArray());
            if (!parameters.TryGetValue("scope", out var scopes) || scopes.Length != 1 || scopes[0] != "data"
                || !parameters.TryGetValue("env", out var environments) || environments.Length != 1 || environments[0] != "paper"
                || !parameters.TryGetValue("state", out var states) || states.Length != 1 || states[0] != id
                || !parameters.TryGetValue("response_type", out var responseTypes) || responseTypes.Length != 1 || responseTypes[0] != "code")
                throw Failure();
            browser.Open(uri);
            while (true)
            {
                using var poll = await http.PostAsJsonAsync(new Uri(broker, $"sessions/{id}/result"), new { verifier }, token);
                if (poll.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    await (delay ?? Task.Delay)(TimeSpan.FromSeconds(1), token);
                    continue;
                }
                if (!poll.IsSuccessStatusCode) throw Failure();
                using var result = await ReadBoundedAsync(poll, token);
                session.SetToken(result.RootElement.GetProperty("accessToken").GetString() ?? "");
                return;
            }
        }
        catch (JsonException) { throw Failure(); }
        catch (KeyNotFoundException) { throw Failure(); }
        catch (InvalidOperationException) { throw Failure(); }
        catch (HttpRequestException) { throw new MarketDataException(MarketFailure.Network, "The authorization service could not be reached."); }
        finally
        {
            if (id is not null)
            {
                try
                {
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var _ = await http.PostAsJsonAsync(new Uri(broker, $"sessions/{id}/cancel"), new { verifier }, cleanup.Token);
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException) { }
            }
        }
    }

    public static void ValidateBroker(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.UserInfo.Length != 0 || (uri.Scheme != "https"
            && !(uri.Scheme == "http" && uri.IsLoopback)))
            throw new MarketDataException(MarketFailure.Configuration, "Use an HTTPS OAuth broker (HTTP only for loopback development).");
    }

    internal static string Base64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static async Task<JsonDocument> ReadBoundedAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength > 1_048_576) throw Failure();
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (memory.Length + read > 1_048_576) throw Failure();
            memory.Write(buffer, 0, read);
        }
        return JsonDocument.Parse(memory.ToArray());
    }
    private static MarketDataException Failure() => new(MarketFailure.Authentication, "Authorization failed or expired. Connect again.");
}
