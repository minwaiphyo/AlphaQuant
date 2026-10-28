using System.Net.WebSockets;
using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

public interface IMarketSocket : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken);
    Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken);
}

public sealed class AlpacaMarketSocket : IMarketSocket
{
    private readonly ClientWebSocket _socket = new();
    public AlpacaMarketSocket()
    {
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        _socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(15);
    }
    public Task ConnectAsync(CancellationToken cancellationToken) =>
        _socket.ConnectAsync(new Uri("wss://stream.data.alpaca.markets/v2/iex"), cancellationToken);
    public async Task SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken) =>
        await _socket.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
    public async Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var data = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await _socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
            if (read.MessageType == WebSocketMessageType.Close) return null;
            if (read.MessageType != WebSocketMessageType.Text || data.Length + read.Count > 1_048_576)
                throw new MarketDataException(MarketFailure.InvalidData, "Market stream returned an invalid frame.");
            data.Write(buffer, 0, read.Count);
            if (read.EndOfMessage) return data.ToArray();
        }
    }
    public ValueTask DisposeAsync() { _socket.Abort(); _socket.Dispose(); return ValueTask.CompletedTask; }
}
