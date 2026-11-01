using AlphaQuant.Core;

namespace AlphaQuant.Infrastructure;

/// <summary>
/// Composition boundary for the UI owner. Uses injected clients/source; owns no UI, trading methods or disk secrets.
/// Cancel/dispose a WatchComparisonAsync enumeration when the screen closes; Disconnect also clears the session.
/// </summary>
public sealed class ComparisonBackend
{
    private readonly OAuthSession _session = new();
    private readonly AlpacaBrowserOAuth _oauth;
    private readonly AlpacaMarketDataProvider _market;
    private readonly ComparisonService _comparison;

    public ComparisonBackend(ICompanyFactsSource facts, HttpClient brokerHttp, Uri brokerOrigin,
        HttpClient marketHttp, IBrowserLauncher browser, TimeProvider? time = null, Func<IMarketSocket>? socketFactory = null)
    {
        _oauth = new(brokerHttp, brokerOrigin, browser, _session);
        _market = new(marketHttp, _session, socketFactory, time);
        _comparison = new(facts, _market, time);
    }

    public bool Authorized => _session.Connected;
    public MarketCapabilities Capabilities => _market.Capabilities;
    public Task ConnectWithBrowserAsync(CancellationToken cancellationToken) => _oauth.ConnectAsync(cancellationToken);
    public Task<MarketAccess> TestMarketAccessAsync(IReadOnlyList<string> symbols, CancellationToken cancellationToken) =>
        _market.TestAccessAsync(symbols, cancellationToken);
    public IAsyncEnumerable<ComparisonSnapshot> WatchComparisonAsync(IReadOnlyList<CompanyRef> companies,
        IReadOnlyDictionary<long, CapitalStructureEvidence>? capital = null,
        IReadOnlyDictionary<long, EquityBasisEvidence>? basis = null, CancellationToken cancellationToken = default) =>
        _comparison.WatchAsync(companies, capital, basis, cancellationToken);
    public void Disconnect() => _market.Disconnect();
}
