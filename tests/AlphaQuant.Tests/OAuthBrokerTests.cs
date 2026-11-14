using System.Net;
using System.Security.Cryptography;
using System.Text;
using AlphaQuant.Infrastructure;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public sealed class OAuthBrokerTests
{
    private static string Verifier(char c) => new(c, 43);
    private static string Challenge(string verifier) => AlpacaBrowserOAuth.Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    private static OAuthBrokerService Broker(HttpMessageHandler handler, TimeProvider? time = null) =>
        new(new HttpClient(handler), "public-client", "server-only-fixture-secret", new("https://broker.example/oauth/callback"), time);

    [Fact]
    public async Task TokenCanOnlyBeCollectedByOwningVerifierAndOnlyOnce()
    {
        var handler = new FakeHandler().EnqueueOk(Encoding.UTF8.GetBytes("""{"token_type":"bearer","scope":"data","access_token":"fixture-token"}"""));
        var broker = Broker(handler);
        var start = broker.Start(Challenge(Verifier('a')));
        Assert.Contains("scope=data&env=paper", start.AuthorizationUrl);
        Assert.DoesNotContain("trading", start.AuthorizationUrl);
        Assert.DoesNotContain("server-only", start.AuthorizationUrl);
        Assert.Equal(OAuthPollState.Missing, broker.Poll(start.SessionId, Verifier('b')).State);
        Assert.Equal(OAuthPollState.Pending, broker.Poll(start.SessionId, Verifier('a')).State);
        await broker.CompleteAsync(start.SessionId, "fixture-code", false, default);
        await broker.CompleteAsync(start.SessionId, "replay", false, default);
        Assert.Single(handler.Requests);
        var result = broker.Poll(start.SessionId, Verifier('a'));
        Assert.Equal("fixture-token", result.AccessToken);
        Assert.DoesNotContain("fixture-token", result.ToString());
        Assert.Equal(OAuthPollState.Missing, broker.Poll(start.SessionId, Verifier('a')).State);
    }

    [Fact]
    public async Task UnknownExpiredAndDeniedCallbacksNeverExchangeCodes()
    {
        var clock = new FakeTime();
        var handler = new FakeHandler();
        var broker = Broker(handler, clock);
        var start = broker.Start(Challenge(Verifier('a')));
        await broker.CompleteAsync("unknown", "code", false, default);
        clock.Advance(TimeSpan.FromMinutes(6));
        await broker.CompleteAsync(start.SessionId, "code", false, default);
        Assert.Equal(OAuthPollState.Missing, broker.Poll(start.SessionId, Verifier('a')).State);
        var denied = broker.Start(Challenge(Verifier('a')));
        await broker.CompleteAsync(denied.SessionId, null, true, default);
        Assert.Equal(OAuthPollState.Failed, broker.Poll(denied.SessionId, Verifier('a')).State);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task TradingScopeIsRejectedEvenWithASuccessfulTokenResponse()
    {
        var broker = Broker(new FakeHandler().EnqueueOk(Encoding.UTF8.GetBytes(
            """{"token_type":"bearer","scope":"data trading","access_token":"fixture-token"}""")));
        var start = broker.Start(Challenge(Verifier('a')));
        await broker.CompleteAsync(start.SessionId, "code", false, default);
        Assert.Equal(OAuthPollState.Failed, broker.Poll(start.SessionId, Verifier('a')).State);
    }

    [Fact]
    public async Task CancelledSessionCannotBeRetrievedAndDoesNotLeakProviderError()
    {
        var handler = new FakeHandler().EnqueueStatus(HttpStatusCode.Unauthorized);
        var broker = Broker(handler);
        var start = broker.Start(Challenge(Verifier('a')));
        broker.Cancel(start.SessionId, Verifier('b'));
        Assert.Equal(OAuthPollState.Pending, broker.Poll(start.SessionId, Verifier('a')).State);
        broker.Cancel(start.SessionId, Verifier('a'));
        await broker.CompleteAsync(start.SessionId, "code", false, default);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void CapacityIsBoundedAndExpiredSessionsReleaseSlots()
    {
        var clock = new FakeTime(); var broker = Broker(new FakeHandler(), clock);
        for (var i = 0; i < 256; i++) broker.Start(Challenge(Verifier('a')));
        Assert.Throws<InvalidOperationException>(() => broker.Start(Challenge(Verifier('a'))));
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.NotNull(broker.Start(Challenge(Verifier('a'))));
    }

    [Fact]
    public async Task CancellationDuringExchangeCannotResurrectSession()
    {
        var handler = new DeferredExchange(); var broker = Broker(handler);
        var start = broker.Start(Challenge(Verifier('a')));
        var exchange = broker.CompleteAsync(start.SessionId, "synthetic-code", false, default);
        await handler.Started.Task;
        broker.Cancel(start.SessionId, Verifier('a'));
        handler.Response.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("""{"token_type":"bearer","scope":"data","access_token":"late-synthetic-token"}""") });
        await exchange;
        Assert.Equal(OAuthPollState.Missing, broker.Poll(start.SessionId, Verifier('a')).State);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"token_type\":123}")]
    [InlineData("not json")]
    public async Task MalformedExchangeNeverDeliversTokenOrProviderDetails(string json)
    {
        var broker = Broker(new FakeHandler().EnqueueOk(Encoding.UTF8.GetBytes(json)));
        var start = broker.Start(Challenge(Verifier('a')));
        await broker.CompleteAsync(start.SessionId, "code", false, default);
        Assert.Equal(OAuthPollState.Failed, broker.Poll(start.SessionId, Verifier('a')).State);
        Assert.Equal(OAuthPollState.Missing, broker.Poll(start.SessionId, Verifier('a')).State);
    }

    private sealed class DeferredExchange : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<HttpResponseMessage> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Started.SetResult(); return Response.Task; }
    }

    [Theory]
    [InlineData("http://example.com/oauth/callback")]
    [InlineData("https://user:secret@example.com/oauth/callback")]
    [InlineData("https://example.com/other")]
    public void RejectsInsecureOrWrongRedirect(string uri) => Assert.ThrowsAny<Exception>(() =>
        new OAuthBrokerService(new HttpClient(), "client", "secret", new(uri)));
}
