namespace AlphaQuant.Core;

public enum MarketConnectionState { Disconnected, Connecting, Authenticating, Connected, Reconnecting, Failed }
public enum MarketFailure { Authentication, Entitlement, ConnectionInUse, RateLimited, Network, InvalidData, Configuration }

/// <summary>Safe for display. Provider response bodies and credentials must never be used as messages.</summary>
public sealed class MarketDataException(MarketFailure failure, string message) : Exception(message)
{
    public MarketFailure Failure { get; } = failure;
}

public sealed record MarketCapabilities(string ProviderId, string Feed, string Coverage, bool RealTime, bool Streaming);
public sealed record MarketAccess(MarketCapabilities Capabilities, IReadOnlyList<string> Symbols);

public sealed record MarketPrice(string Symbol, decimal Price, string Currency, string ProviderId, string Feed,
    DateTimeOffset EventAt, DateTimeOffset ReceivedAt, string TradeId);

/// <summary>Null Price with a symbol invalidates that symbol, e.g. after a trade correction.</summary>
public sealed record MarketEvent(MarketConnectionState State, string? Symbol = null, MarketPrice? Price = null,
    string? Message = null);

public interface IMarketDataProvider
{
    MarketCapabilities Capabilities { get; }
    Task<MarketAccess> TestAccessAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken);
    IAsyncEnumerable<MarketEvent> StreamAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken);
    void Disconnect();
}

public interface IBrowserLauncher
{
    void Open(Uri authorizationUri);
}
