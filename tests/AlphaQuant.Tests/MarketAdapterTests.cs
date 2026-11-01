using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public sealed class MarketAdapterTests
{
    internal sealed class Socket(params string[] frames) : IMarketSocket
    {
        private readonly Queue<string> _frames = new(frames);
        public List<string> Sent { get; } = [];
        public bool Disposed { get; private set; }
        public Task ConnectAsync(CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.CompletedTask; }
        public Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken token)
        { Sent.Add(Encoding.UTF8.GetString(message.Span)); return Task.CompletedTask; }
        public async Task<byte[]?> ReceiveAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_frames.Count > 0) return Encoding.UTF8.GetBytes(_frames.Dequeue());
            await Task.Delay(Timeout.Infinite, token);
            return null;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    internal static OAuthSession Session()
    { var session = new OAuthSession(); session.SetToken("fixture-oauth-token"); return session; }

    private static readonly string Auth = """[{"T":"success","msg":"authenticated"}]""";
    private static readonly string Subscription = """[{"T":"subscription","trades":["TEST"]}]""";
    private static string Trade(decimal price, string timestamp, string condition = "@") =>
        JsonSerializer.Serialize(new[] { new Dictionary<string, object>
            { ["T"] = "t", ["S"] = "TEST", ["p"] = price, ["t"] = timestamp, ["i"] = 1, ["c"] = new[] { condition } } });

    [Fact]
    public async Task HandshakeRequestsOnlyTradesAndFiltersLateOddLotAndFutureEvents()
    {
        var clock = new FakeTime();
        var now = clock.GetUtcNow();
        var socket = new Socket(Auth, Subscription, Trade(60m, now.ToString("O")),
            Trade(1m, now.AddMinutes(-1).ToString("O")), Trade(2m, now.AddSeconds(1).ToString("O"), "I"),
            Trade(3m, now.AddMinutes(5).ToString("O")), Trade(61m, now.AddSeconds(1).ToString("O")));
        var provider = new AlpacaMarketDataProvider(new HttpClient(), Session(), () => socket, clock);
        var prices = new List<decimal>();
        await foreach (var update in provider.StreamAsync(["TEST"], default))
        {
            if (update.Price is not null) prices.Add(update.Price.Price);
            if (prices.Count == 2) break;
        }
        Assert.Equal(new[] { 60m, 61m }, prices);
        Assert.True(socket.Disposed);
        Assert.Equal(2, socket.Sent.Count);
        Assert.Contains("\"key\":\"oauth\"", socket.Sent[0]);
        Assert.Contains("subscribe", socket.Sent[1]);
        Assert.DoesNotContain("orders", string.Join(" ", socket.Sent));
    }

    [Fact]
    public async Task CorrectionInvalidatesPriceInsteadOfSilentlyKeepingItValid()
    {
        var socket = new Socket(Auth, Subscription, """[{"T":"c","S":"TEST","oi":1,"cp":5}]""");
        var provider = new AlpacaMarketDataProvider(new HttpClient(), Session(), () => socket);
        MarketEvent? invalidated = null;
        await foreach (var update in provider.StreamAsync(["TEST"], default))
        { if (update.Symbol is not null) { invalidated = update; break; } }
        Assert.NotNull(invalidated);
        Assert.Null(invalidated.Price);
        Assert.Contains("correction", invalidated.Message);
    }

    [Theory]
    [InlineData(402, MarketFailure.Authentication)]
    [InlineData(409, MarketFailure.Entitlement)]
    [InlineData(406, MarketFailure.ConnectionInUse)]
    [InlineData(405, MarketFailure.RateLimited)]
    public async Task ErrorsAreTypedAndDoNotEchoSecretProviderMessages(int code, MarketFailure expected)
    {
        var socket = new Socket($$"""[{"T":"error","code":{{code}},"msg":"fixture-oauth-token private"}]""");
        var calls = 0;
        var provider = new AlpacaMarketDataProvider(new HttpClient(), Session(), () => { calls++; return socket; });
        var error = await Assert.ThrowsAsync<MarketDataException>(async () =>
        { await foreach (var _ in provider.StreamAsync(["TEST"], default)) { } });
        Assert.Equal(expected, error.Failure);
        Assert.DoesNotContain("fixture-oauth-token", error.ToString());
        Assert.Equal(1, calls);
        Assert.True(socket.Disposed);
    }

    [Fact]
    public async Task SubscriptionMismatchIsNotAUsableConnection()
    {
        var socket = new Socket(Auth, """[{"T":"subscription","trades":[]}]""");
        var provider = new AlpacaMarketDataProvider(new HttpClient(), Session(), () => socket);
        var error = await Assert.ThrowsAsync<MarketDataException>(async () =>
        { await foreach (var _ in provider.StreamAsync(["TEST"], default)) { } });
        Assert.Equal(MarketFailure.Entitlement, error.Failure);
    }

    [Fact]
    public async Task LeavingScreenCancelsReadDisposesSocketAndAllowsAnotherEnumeration()
    {
        var sockets = new List<Socket>();
        var provider = new AlpacaMarketDataProvider(new HttpClient(), Session(), () =>
        { var socket = new Socket(Auth, Subscription); sockets.Add(socket); return socket; });
        for (var i = 0; i < 2; i++)
        {
            using var cts = new CancellationTokenSource();
            await using var enumerator = provider.StreamAsync(["TEST"], cts.Token).GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
                if (enumerator.Current.State == MarketConnectionState.Connected) break;
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
        }
        Assert.Equal(2, sockets.Count);
        Assert.All(sockets, socket => Assert.True(socket.Disposed));
    }

    [Fact]
    public async Task AccessTestUsesBearerAndIexOnlyWithoutTradingEndpoints()
    {
        var handler = new RecordingHandler();
        var provider = new AlpacaMarketDataProvider(new HttpClient(handler), Session());
        var access = await provider.TestAccessAsync(["TEST"], default);
        Assert.Equal(new[] { "TEST" }, access.Symbols);
        Assert.Contains("data.alpaca.markets/v2/stocks/trades/latest?feed=iex", handler.Uri);
        Assert.Equal("Bearer fixture-oauth-token", handler.Authorization);
        Assert.DoesNotContain("fixture-oauth-token", handler.Uri);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Uri;
        public string? Authorization;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uri = request.RequestUri!.AbsoluteUri; Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("""{"trades":{"TEST":{"p":60}}}""") });
        }
    }

    [Fact]
    public void DisconnectDropsSessionAndSessionToStringNeverContainsToken()
    {
        var session = Session();
        Assert.DoesNotContain("fixture-oauth-token", session.ToString());
        new AlpacaMarketDataProvider(new HttpClient(), session).Disconnect();
        Assert.False(session.Connected);
    }
}
