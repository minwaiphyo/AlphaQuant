using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

/// <summary>Data endpoints only. No orders, brokerage-account reads, API-key login or paid-feed fallback.</summary>
public sealed class AlpacaMarketDataProvider(HttpClient http, OAuthSession session,
    Func<IMarketSocket>? socketFactory = null, TimeProvider? time = null,
    Func<TimeSpan, CancellationToken, Task>? delay = null) : IMarketDataProvider
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private CancellationTokenSource? _active;
    private int _streaming;
    public MarketCapabilities Capabilities { get; } = new("alpaca", "iex", "IEX only; not consolidated US market", true, true);

    public static HttpClient CreateHttpClient() => new(new SocketsHttpHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(30) };

    public void Disconnect()
    {
        session.Clear();
        lock (this) { _active?.Cancel(); }
    }

    public async Task<MarketAccess> TestAccessAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        ValidateSymbols(symbols);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://data.alpaca.markets/v2/stocks/trades/latest?feed=iex&symbols="
                + Uri.EscapeDataString(string.Join(',', symbols)));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.RequireToken());
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized) session.Clear();
        if (!response.IsSuccessStatusCode)
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => Error(MarketFailure.Authentication),
                HttpStatusCode.Forbidden => Error(MarketFailure.Entitlement),
                HttpStatusCode.TooManyRequests => Error(MarketFailure.RateLimited),
                _ => Error(MarketFailure.Network)
            };
        try
        {
            using var json = await AlpacaBrowserOAuth.ReadBoundedAsync(response, cancellationToken);
            if (!json.RootElement.TryGetProperty("trades", out var trades) || trades.ValueKind != JsonValueKind.Object)
                throw Error(MarketFailure.InvalidData);
            // Authenticate and check this sample; success does not imply every symbol or another feed is entitled.
            return new(Capabilities, symbols.Where(s => trades.TryGetProperty(s, out var trade)
                && trade.ValueKind == JsonValueKind.Object).ToArray());
        }
        catch (JsonException) { throw Error(MarketFailure.InvalidData); }
    }

    public async IAsyncEnumerable<MarketEvent> StreamAsync(IReadOnlyList<string> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ValidateSymbols(symbols);
        if (Interlocked.CompareExchange(ref _streaming, 1, 0) != 0) throw Error(MarketFailure.ConnectionInUse);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (this) { _active = lifetime; }
        var selected = symbols.ToHashSet(StringComparer.Ordinal);
        var latest = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        try
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                lifetime.Token.ThrowIfCancellationRequested();
                yield return new(attempt == 0 ? MarketConnectionState.Connecting : MarketConnectionState.Reconnecting);
                await using var socket = (socketFactory ?? (() => new AlpacaMarketSocket()))();
                var connected = await TryConnectAsync(socket, lifetime.Token);
                if (!connected)
                {
                    if (attempt == 3) throw Error(MarketFailure.Network);
                    await Wait(attempt, lifetime.Token); continue;
                }
                yield return new(MarketConnectionState.Authenticating);
                using var handshake = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                handshake.CancelAfter(TimeSpan.FromSeconds(15));
                await SendCheckedAsync(socket, JsonSerializer.SerializeToUtf8Bytes(new { action = "auth", key = "oauth", secret = session.RequireToken() }), handshake.Token);
                bool authenticated = false, subscribed = false;
                while (true)
                {
                    // No new trades does not imply connection failure. WebSocket ping/pong handles liveness.
                    var bytes = await TryReceiveAsync(socket, authenticated && subscribed ? lifetime.Token : handshake.Token, lifetime.Token);
                    if (bytes is null) break;
                    using var json = Parse(bytes);
                    if (json.RootElement.ValueKind != JsonValueKind.Array) throw Error(MarketFailure.InvalidData);
                    foreach (var item in json.RootElement.EnumerateArray())
                    {
                        var kind = String(item, "T");
                        if (kind == "error")
                        {
                            var code = item.TryGetProperty("code", out var number) && number.ValueKind == JsonValueKind.Number
                                && number.TryGetInt32(out var n) ? n : 0;
                            if (code is 401 or 402) session.Clear();
                            throw Error(code switch { 401 or 402 => MarketFailure.Authentication, 403 or 409 => MarketFailure.Entitlement,
                                406 => MarketFailure.ConnectionInUse, 405 or 407 => MarketFailure.RateLimited, _ => MarketFailure.InvalidData });
                        }
                        if (kind == "success" && String(item, "msg") == "authenticated" && !authenticated)
                        {
                            authenticated = true;
                            await SendCheckedAsync(socket, JsonSerializer.SerializeToUtf8Bytes(new { action = "subscribe", trades = symbols }), handshake.Token);
                        }
                        if (kind == "subscription" && authenticated)
                        {
                            if (!item.TryGetProperty("trades", out var trades) || trades.ValueKind != JsonValueKind.Array
                                || !selected.SetEquals(trades.EnumerateArray().Select(t => t.ValueKind == JsonValueKind.String ? t.GetString() ?? "" : "")))
                                throw Error(MarketFailure.Entitlement);
                            subscribed = true;
                            yield return new(MarketConnectionState.Connected);
                        }
                        var symbol = String(item, "S");
                        if (!subscribed || symbol is null || !selected.Contains(symbol)) continue;
                        if (kind is "c" or "x")
                        {
                            // Conservative invalidation: a corrected/cancelled trade is not a usable valuation price.
                            yield return new(MarketConnectionState.Connected, symbol, Message: "Trade correction/cancellation; awaiting a new eligible price.");
                            continue;
                        }
                        if (kind != "t" || !EligibleTrade(item)) continue;
                        if (!item.TryGetProperty("p", out var p) || p.ValueKind != JsonValueKind.Number || !p.TryGetDecimal(out var value) || value <= 0
                            || !item.TryGetProperty("t", out var timestamp) || timestamp.ValueKind != JsonValueKind.String
                            || !timestamp.TryGetDateTimeOffset(out var at)) continue;
                        var now = _time.GetUtcNow();
                        if (at > now.AddSeconds(5) || latest.TryGetValue(symbol, out var last) && at <= last) continue;
                        latest[symbol] = at;
                        var id = item.TryGetProperty("i", out var tradeId) ? tradeId.ToString() : "";
                        yield return new(MarketConnectionState.Connected, symbol,
                            new(symbol, value, "USD", "alpaca", "iex", at, now, id));
                    }
                }
                yield return new(MarketConnectionState.Reconnecting, Message: "Price stream interrupted; prior prices are not live.");
                if (attempt == 3) throw Error(MarketFailure.Network);
                await Wait(attempt, lifetime.Token);
            }
        }
        finally
        {
            lock (this) { _active = null; }
            Interlocked.Exchange(ref _streaming, 0);
        }
    }

    private Task Wait(int attempt, CancellationToken token) =>
        (delay ?? Task.Delay)(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
    private static async Task SendCheckedAsync(IMarketSocket socket, byte[] data, CancellationToken token)
    {
        try { await socket.SendAsync(data, token); }
        catch (WebSocketException) { throw Error(MarketFailure.Network); }
    }
    private static async Task<bool> TryConnectAsync(IMarketSocket socket, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try { await socket.ConnectAsync(timeout.Token); return true; }
        catch (WebSocketException) { return false; }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return false; }
    }
    private static async Task<byte[]?> TryReceiveAsync(IMarketSocket socket, CancellationToken token, CancellationToken lifetime)
    {
        try { return await socket.ReceiveAsync(token); }
        catch (WebSocketException) { return null; }
        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested) { return null; }
    }
    private static JsonDocument Parse(byte[] data)
    {
        try { return JsonDocument.Parse(data); }
        catch (JsonException) { throw Error(MarketFailure.InvalidData); }
    }
    private static string? String(JsonElement item, string key) => item.ValueKind == JsonValueKind.Object
        && item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool EligibleTrade(JsonElement item) => item.TryGetProperty("c", out var conditions)
        && conditions.ValueKind == JsonValueKind.Array && conditions.EnumerateArray().All(c => c.ValueKind == JsonValueKind.String && c.GetString() == "@");
    private static void ValidateSymbols(IReadOnlyList<string> symbols)
    {
        if (symbols.Count is < 1 or > 5 || symbols.Distinct(StringComparer.Ordinal).Count() != symbols.Count
            || symbols.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 16 || !s.All(c => char.IsAsciiLetterUpper(c) || char.IsDigit(c) || c is '.' or '-')))
            throw new ArgumentException("Use one to five distinct supported uppercase symbols.", nameof(symbols));
    }
    private static MarketDataException Error(MarketFailure failure) => new(failure, failure switch
    {
        MarketFailure.Authentication => "Alpaca authorization is missing, expired or rejected. Connect again.",
        MarketFailure.Entitlement => "The account does not provide the required IEX symbols/access.",
        MarketFailure.ConnectionInUse => "The Alpaca stream connection is already in use. Close the other connection and retry.",
        MarketFailure.RateLimited => "Alpaca request or subscription limit reached. Retry later.",
        MarketFailure.InvalidData => "Alpaca returned unsupported market data.",
        _ => "The Alpaca data connection failed. Retry the connection."
    });
}
