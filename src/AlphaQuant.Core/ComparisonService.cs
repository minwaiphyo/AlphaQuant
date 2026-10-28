using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AlphaQuant.Core;

/// <summary>UI-independent two-to-five-company coordinator. Dispose enumeration when leaving comparison.</summary>
public sealed class ComparisonService(ICompanyFactsSource factsSource, IMarketDataProvider market, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies,
        IReadOnlyDictionary<long, CapitalStructureEvidence>? capital = null,
        IReadOnlyDictionary<long, EquityBasisEvidence>? equityBasis = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (companies.Count is < 2 or > 5 || companies.Select(c => c.Cik).Distinct().Count() != companies.Count
            || companies.Select(c => c.Ticker).Distinct(StringComparer.OrdinalIgnoreCase).Count() != companies.Count)
            throw new ArgumentException("Select two to five distinct companies and symbols.", nameof(companies));
        var rows = await Task.WhenAll(companies.Select(async company =>
        {
            try
            {
                var facts = await factsSource.GetFactsAsync(company.Cik, cancellationToken);
                var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
                var financials = ComparisonAnalyzer.Analyze(facts.Facts, today);
                var structure = capital?.GetValueOrDefault(company.Cik);
                if (structure is not null && structure.Cik == company.Cik && structure.Symbol == company.Ticker
                    && structure.SimpleSingleClass && structure.SupportedDomesticNonFinancialUsGaapUsd
                    && structure.BalanceSheetDate <= today && today.DayNumber - structure.BalanceSheetDate.DayNumber <= 135
                    && structure.Debt >= 0 && structure.Cash >= 0 && !string.IsNullOrWhiteSpace(structure.FilingReference)
                    && !string.IsNullOrWhiteSpace(structure.ReviewReference))
                {
                    financials = financials with
                    {
                        DebtBalance = new("Reviewed interest-bearing debt", structure.Debt, "USD", null, structure.BalanceSheetDate, [])
                            { EvidenceReferences = [structure.FilingReference, structure.ReviewReference] }
                    };
                }
                return new ComparisonRow(company, financials,
                    null, MarketConnectionState.Disconnected, null, facts.RetrievedAt, facts.IsStale);
            }
            catch (DataSourceException) { return new ComparisonRow(company, null, null, MarketConnectionState.Disconnected,
                "Financial data could not be loaded. Retry the financial refresh.", null, false); }
            catch (HttpRequestException) { return new ComparisonRow(company, null, null, MarketConnectionState.Disconnected,
                "Financial connection failed. Retry the financial refresh.", null, false); }
        }));
        yield return new(rows.ToArray());
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateBounded<MarketEvent?>(new BoundedChannelOptions(256)
            { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        var producer = ProduceAsync();
        var timer = TickAsync();
        var prices = new Dictionary<string, MarketPrice>(StringComparer.OrdinalIgnoreCase);
        var state = MarketConnectionState.Connecting;
        string? error = null;
        try
        {
            await foreach (var change in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (change is not null)
                {
                    state = change.State; error = change.Message;
                    if (change.Symbol is not null)
                    {
                        if (change.Price is null) prices.Remove(change.Symbol);
                        else prices[change.Symbol] = change.Price;
                    }
                }
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    var quote = prices.GetValueOrDefault(row.Company.Ticker);
                    // Old prices remain inspectable, but disconnected quotes do not drive valid valuations.
                    var valuation = row.Fundamentals is null ? null : ComparisonValuation.Calculate(row.Company,
                        row.Fundamentals, state == MarketConnectionState.Connected ? quote : null, _clock.GetUtcNow(),
                        capital?.GetValueOrDefault(row.Company.Cik), equityBasis?.GetValueOrDefault(row.Company.Cik));
                    if (valuation is not null && quote is not null && state != MarketConnectionState.Connected)
                        valuation = valuation with { Price = quote };
                    rows[i] = row with { Valuation = valuation, Connection = state,
                        Error = row.Fundamentals is null ? row.Error : error };
                }
                yield return new(rows.ToArray());
            }
        }
        finally
        {
            lifetime.Cancel();
            await Task.WhenAll(producer, timer);
        }

        async Task ProduceAsync()
        {
            try
            {
                await foreach (var item in market.StreamAsync(companies.Select(c => c.Ticker).ToArray(), lifetime.Token))
                    await channel.Writer.WriteAsync(item, lifetime.Token);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (OperationCanceledException)
            {
                await channel.Writer.WriteAsync(new(MarketConnectionState.Disconnected,
                    Message: "Market data disconnected. Financial results remain available."), lifetime.Token);
            }
            catch (HttpRequestException)
            {
                await channel.Writer.WriteAsync(new(MarketConnectionState.Failed,
                    Message: "The market connection failed. Retry the connection."), lifetime.Token);
            }
            catch (MarketDataException ex)
            {
                await channel.Writer.WriteAsync(new(MarketConnectionState.Failed, Message: ex.Message), lifetime.Token);
            }
            finally { channel.Writer.TryComplete(); }
        }
        async Task TickAsync()
        {
            try
            {
                while (!lifetime.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _clock, lifetime.Token);
                    await channel.Writer.WriteAsync(null, lifetime.Token);
                }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (ChannelClosedException) { }
        }
    }
}
