using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Desktop.Services;

public interface IResearchBackend
{
    bool Configured { get; }
    bool Authorized { get; }
    Task ConnectAsync(CancellationToken token);
    Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token);
    IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies, CancellationToken token);
    void Disconnect();
}

public sealed class ResearchBackend(ComparisonBackend backend, bool configured, ReviewedEvidenceStore? evidence = null) : IResearchBackend
{
    private string[] _verified = [];
    public bool Configured => configured;
    public bool Authorized => backend.Authorized;
    public async Task ConnectAsync(CancellationToken token)
    {
        _verified = [];
        if (!Configured) throw new MarketDataException(MarketFailure.Configuration, "Configure the OAuth broker first.");
        await backend.ConnectWithBrowserAsync(token);
    }
    public async Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token)
    {
        _verified = [];
        var access = await backend.TestMarketAccessAsync(symbols, token);
        token.ThrowIfCancellationRequested();
        if (!access.Capabilities.RealTime || !access.Capabilities.Streaming)
            throw new MarketDataException(MarketFailure.Entitlement, "Real-time streaming access is required.");
        if (symbols.Any(s => !access.Symbols.Contains(s, System.StringComparer.OrdinalIgnoreCase)))
            throw new MarketDataException(MarketFailure.Entitlement, "Access is not verified for every selected symbol.");
        _verified = access.Symbols.ToArray();
        return access;
    }
    public async IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies,
        [EnumeratorCancellation] CancellationToken token)
    {
        var canStream = Authorized && companies.All(c => _verified.Contains(c.Ticker, System.StringComparer.OrdinalIgnoreCase));
        await foreach (var snapshot in backend.WatchComparisonAsync(companies, evidence?.Capital, evidence?.Equity, cancellationToken: token))
        {
            yield return snapshot;
            // Disposing after the first yield never starts the underlying market producer.
            if (!canStream) yield break;
        }
    }
    public void Disconnect() { _verified = []; backend.Disconnect(); }
}
