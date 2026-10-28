using System.Runtime.CompilerServices;
using AlphaQuant.Core;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public sealed class ComparisonIntegrationTests
{
    private sealed class Facts : ICompanyFactsSource
    {
        public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken token) => Task.FromResult(
            new CompanyFacts("Synthetic", ComparisonBackendTests.ReviewedSample(), 0,
                DateTimeOffset.Parse("2026-10-28T12:00:00Z"), false));
    }
    private sealed class Feed(TimeProvider clock) : IMarketDataProvider
    {
        public MarketCapabilities Capabilities => new("fake", "fixture", "Synthetic only", true, true);
        public bool Stopped;
        public void Disconnect() { }
        public Task<MarketAccess> TestAccessAsync(IReadOnlyList<string> symbols, CancellationToken token) =>
            Task.FromResult(new MarketAccess(Capabilities, symbols));
        public async IAsyncEnumerable<MarketEvent> StreamAsync(IReadOnlyList<string> symbols,
            [EnumeratorCancellation] CancellationToken token)
        {
            try
            {
                yield return new(MarketConnectionState.Connected);
                foreach (var symbol in symbols)
                    yield return new(MarketConnectionState.Connected, symbol,
                        new(symbol, 60m, "USD", "fake", "fixture", clock.GetUtcNow(), clock.GetUtcNow(), "1"));
                await Task.Delay(Timeout.Infinite, token);
            }
            finally { Stopped = true; }
        }
    }

    [Fact]
    public async Task TwoCompanyComparisonCombinesFixturesAndPricesAndStopsWhenScreenCloses()
    {
        var clock = new FixedClock(); var feed = new Feed(clock);
        var companies = new[] { new CompanyRef(1, "ONE", "One"), new CompanyRef(2, "TWO", "Two") };
        var basis = companies.ToDictionary(c => c.Cik, c => new EquityBasisEvidence(c.Cik, c.Ticker,
            new(2026, 10, 28), "synthetic-basis", true));
        var service = new ComparisonService(new Facts(), feed, clock);
        ComparisonSnapshot? final = null;
        await foreach (var snapshot in service.WatchAsync(companies, equityBasis: basis))
        {
            final = snapshot;
            if (snapshot.Rows.All(r => r.Valuation?.Price is not null)) break;
        }
        Assert.True(feed.Stopped);
        Assert.All(final!.Rows, row =>
        {
            Assert.Equal(1350m, row.Fundamentals!.TtmRevenue.Value);
            Assert.Equal(20m, row.Valuation!.PriceEarnings.Value);
            Assert.Null(row.Valuation.EnterpriseValueSales.Value); // No invented capital claims.
        });
    }

    [Fact]
    public async Task DuplicateCompaniesAreRejectedBeforeAnyStream()
    {
        var clock = new FixedClock();
        var service = new ComparisonService(new Facts(), new Feed(clock), clock);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        { await foreach (var _ in service.WatchAsync([new(1, "ONE", "One"), new(1, "ONE", "One")])) { } });
    }

    private sealed class FixedClock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-28T12:00:00Z"); }
}
