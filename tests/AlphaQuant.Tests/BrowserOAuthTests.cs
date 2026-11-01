using System.Net;
using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public sealed class BrowserOAuthTests
{
    private sealed class Browser : IBrowserLauncher
    {
        public Uri? Opened;
        public void Open(Uri uri) => Opened = uri;
    }

    private sealed class Handler(string? maliciousUrl = null) : HttpMessageHandler
    {
        public int Cancelled;
        public string? Challenge;
        public string? Verifier;
        private readonly string _id = new('a', 43);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            if (request.RequestUri!.AbsolutePath == "/sessions")
            {
                Challenge = body.RootElement.GetProperty("challenge").GetString();
                return Json(new { sessionId = _id, authorizationUrl = maliciousUrl ??
                    $"https://app.alpaca.markets/oauth/authorize?scope=data&env=paper&response_type=code&state={_id}" });
            }
            Verifier = body.RootElement.GetProperty("verifier").GetString();
            if (request.RequestUri.AbsolutePath.EndsWith("cancel")) { Cancelled++; return new(HttpStatusCode.NoContent); }
            return Json(new { accessToken = "fixture-token" });
        }
        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
            { Content = new StringContent(JsonSerializer.Serialize(value)) };
    }

    [Fact]
    public async Task BrowserConsentUsesVerifierBoundRetrievalAndMemoryOnlySession()
    {
        var handler = new Handler(); var browser = new Browser(); var session = new OAuthSession();
        await new AlpacaBrowserOAuth(new HttpClient(handler), new("https://broker.example/"), browser, session).ConnectAsync(default);
        Assert.True(session.Connected);
        Assert.NotNull(browser.Opened);
        Assert.DoesNotContain("fixture-token", browser.Opened.AbsoluteUri);
        Assert.NotEqual(handler.Challenge, handler.Verifier);
        Assert.Equal(43, handler.Verifier!.Length);
        Assert.Equal(1, handler.Cancelled);
        session.Clear(); Assert.False(session.Connected);
    }

    [Theory]
    [InlineData("https://evil.example/oauth/authorize")]
    [InlineData("https://app.alpaca.markets/oauth/authorize?scope=trading")]
    public async Task UntrustedOrOverPrivilegedAuthorizationIsNeverOpened(string uri)
    {
        var browser = new Browser(); var handler = new Handler(uri);
        await Assert.ThrowsAsync<MarketDataException>(() => new AlpacaBrowserOAuth(new HttpClient(handler),
            new("https://broker.example/"), browser, new()).ConnectAsync(default));
        Assert.Null(browser.Opened);
    }

    [Fact]
    public async Task NonLoopbackPlaintextBrokerIsRejectedBeforeSendingCredentials()
    {
        var handler = new Handler();
        await Assert.ThrowsAsync<MarketDataException>(() => new AlpacaBrowserOAuth(new HttpClient(handler),
            new("http://broker.example/"), new Browser(), new()).ConnectAsync(default));
        Assert.Null(handler.Challenge);
    }
}
