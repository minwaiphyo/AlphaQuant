using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public sealed record ResearchMetric(string Name, string Value, string Period, string Reason, IRelayCommand InspectCommand);
public sealed record ResearchCompany(string Name, string Status, string Price, string Warnings, IReadOnlyList<ResearchMetric> Metrics)
{
    public IReadOnlyList<ResearchMetric> Summary => Metrics.Take(4).Concat(Metrics.Where(m => m.Period.StartsWith("TTM", StringComparison.Ordinal)
        || m.Period == "Current valuation" || m.Period.StartsWith("Balance sheet", StringComparison.Ordinal))).ToArray();
}

public partial class FinancialAnalysisViewModel : ObservableObject
{
    private readonly IResearchBackend _backend;
    private readonly ILinkOpener _links;
    private CancellationTokenSource? _watch;
    private int _generation;
    private ComparisonSnapshot? _snapshot;
    public FinancialAnalysisViewModel(IResearchBackend backend, ILinkOpener links, Action back, Action settings)
    {
        _backend = backend; _links = links;
        BackCommand = new RelayCommand(back); SettingsCommand = new RelayCommand(settings);
    }
    public IRelayCommand BackCommand { get; }
    public IRelayCommand SettingsCommand { get; }
    public ObservableCollection<ResearchCompany> Companies { get; } = [];
    [ObservableProperty] private bool _running;
    [ObservableProperty] private string _status = "Choose two to five companies in the peer workspace.";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasEvidence))] private CellEvidence? _evidence;
    public bool HasEvidence => Evidence is not null;
    [RelayCommand] private void CloseEvidence() => Evidence = null;
    [RelayCommand] private void Stop() => StopWatching();

    public void StopWatching()
    {
        ++_generation; _watch?.Cancel(); _watch = null; Running = false;
        // Leaving the screen invalidates prices immediately; financial snapshots remain inspectable.
        if (_snapshot is not null)
        {
            _snapshot = new(_snapshot.Rows.Select(r => r with { Valuation = null, Connection = MarketConnectionState.Disconnected }).ToArray());
            Render(_snapshot);
        }
        Evidence = null;
        Status = "Updates stopped. Financial snapshots retained; reopen analysis to refresh and subscribe.";
    }

    public async Task LoadAsync(IReadOnlyList<CompanyRef> selected)
    {
        StopWatching();
        if (selected.Count is < 2 or > 5) { Companies.Clear(); Status = "Select two to five distinct companies first."; return; }
        var retained = _snapshot?.Rows.Where(r => selected.Any(c => c == r.Company)).ToArray();
        if (retained is { Length: > 0 }) Render(new(retained)); else Companies.Clear();
        _snapshot = retained is { Length: > 0 } ? new(retained) : null;
        var generation = ++_generation;
        using var cts = new CancellationTokenSource(); _watch = cts; Running = true;
        Status = "Loading filing evidence. Previous snapshots remain visible until new results arrive.";
        try
        {
            await foreach (var snapshot in _backend.WatchAsync(selected, cts.Token))
            {
                if (generation != _generation || cts.IsCancellationRequested) return;
                // Preserve usable financials if an individual refresh fails, but never retain its prices.
                var merged = snapshot.Rows.Select(row => row.Fundamentals is null
                    && _snapshot?.Rows.FirstOrDefault(old => old.Company == row.Company)?.Fundamentals is { } previous
                    ? row with { Fundamentals = previous, FactsRetrievedAt = _snapshot.Rows.First(old => old.Company == row.Company).FactsRetrievedAt,
                        FactsStale = true, Error = "Refresh failed. Previous financial snapshot retained. " + row.Error }
                    : row).ToArray();
                _snapshot = new(merged); Render(_snapshot);
                Status = _backend.Authorized ? "IEX limited-venue feed · test access for the selected symbols in Data connections before streaming. Inspect connection, timestamps and age below; market session unknown."
                    : "Fundamentals loaded. Connect Alpaca in Data connections for prices; reviewed equity/capital evidence is required for valuation.";
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (Exception) { if (generation == _generation) Status = "Could not refresh analysis. Check SEC contact in Data connections and retry. Existing snapshots retained."; }
        finally { if (generation == _generation) { Running = false; _watch = null; } }
    }

    private void Render(ComparisonSnapshot snapshot)
    {
        Companies.Clear();
        foreach (var row in snapshot.Rows)
        {
            var metrics = new List<ResearchMetric>();
            void Add(FinancialValue value, string basis)
            {
                var period = value.End is null ? basis : value.Start is null ? $"{basis} · at {value.End:yyyy-MM-dd}"
                    : $"{basis} · {value.Start:yyyy-MM-dd} to {value.End:yyyy-MM-dd}";
                metrics.Add(new(value.Name, Format(value), period, value.Reason ?? string.Empty,
                    new RelayCommand(() => Inspect(row, value, period))));
            }
            var f = row.Fundamentals;
            if (f is not null)
            {
                foreach (var year in f.History.OrderByDescending(y => y.End))
                {
                    Add(year.Revenue, "Annual"); Add(year.RevenueGrowth, "Annual");
                    Add(year.OperatingMargin, "Annual"); Add(year.OperatingCashFlow, "Annual");
                }
                Add(f.TtmRevenue, "TTM"); Add(f.TtmOperatingMargin, "TTM"); Add(f.TtmOperatingCashFlow, "TTM");
                Add(f.TtmDilutedEps, "TTM"); Add(f.CashBalance, "Balance sheet"); Add(f.DebtBalance, "Reviewed balance sheet");
            }
            Add(row.Valuation?.PriceEarnings ?? ComparisonAnalyzer.Missing("Trailing P/E", "x", "A fresh price and reviewed compatible equity/split basis are required."), "Current valuation");
            Add(row.Valuation?.EnterpriseValueSales ?? ComparisonAnalyzer.Missing("EV / TTM sales", "x", "A fresh price and reviewed simple capital structure are required."), "Current valuation");
            var quote = row.Valuation?.Price;
            var age = quote is null ? 0 : Math.Max(0, (DateTimeOffset.UtcNow - quote.EventAt).TotalSeconds);
            var price = quote is null ? "Price unavailable · connect Alpaca and await an eligible IEX trade."
                : $"{quote.Symbol} · {quote.Price.ToString("N2", CultureInfo.InvariantCulture)} {quote.Currency} · {quote.ProviderId}/{quote.Feed}\nEvent {quote.EventAt:yyyy-MM-dd HH:mm:ss} UTC · received {quote.ReceivedAt:HH:mm:ss} UTC · age {age:F0}s · {(age > 60 ? "Stale" : "Check connection and valuation validity")} · session unknown";
            Companies.Add(new($"{row.Company.Name} · {row.Company.Ticker}",
                $"{row.Connection} · Retrieved {row.FactsRetrievedAt:yyyy-MM-dd HH:mm} UTC{(row.FactsStale ? " · stale/retained financial snapshot" : "")}\n{row.Error}", price,
                string.Join("\n", f?.Warnings ?? []), metrics));
        }
    }
    private static string Format(FinancialValue value) => value.Available
        ? $"{value.Value!.Value.ToString("N2", CultureInfo.InvariantCulture)} {value.Unit}" : "Unavailable";
    private void Inspect(AlphaQuant.Core.ComparisonRow row, FinancialValue value, string period)
    {
        Evidence = new()
        {
            Company = row.Company.Name, Title = value.Name, Value = Format(value),
            Formula = "comparison-1 · " + (value.Name == "Trailing P/E" ? "Current eligible price / positive compatible TTM diluted EPS."
                : value.Name == "EV / TTM sales" ? "(Price × actual outstanding shares + debt + preferred equity + non-controlling interests − cash) / TTM revenue."
                : value.Name.Contains("margin", StringComparison.OrdinalIgnoreCase)
                ? "Operating income / matching-period revenue × 100."
                : value.Name.Contains("growth", StringComparison.OrdinalIgnoreCase)
                ? "(Current annual revenue / comparable prior annual revenue − 1) × 100."
                : "Reported value or reviewed sum of the weighted source contributions shown below. TTM uses a reported FY, four contiguous quarters, or FY + current YTD − prior YTD; EPS excludes YTD subtraction."),
            Detail = $"{period}\n{value.Reason}\n" + string.Join("\n", value.Inputs.Select(i => $"{i.Multiplier:+0.####;-0.####;0} × {i.Fact.Concept}: {i.Fact.Value} {i.Fact.Unit}"))
                + "\n" + string.Join("\n", value.EvidenceReferences)
                + (value.Name is "Trailing P/E" or "EV / TTM sales" ? ValuationEvidence(row.Valuation) : string.Empty),
            Retrieved = $"Financial snapshot {row.FactsRetrievedAt:yyyy-MM-dd HH:mm} UTC",
            Warnings = string.Join("\n", row.Fundamentals?.Warnings ?? []),
            Facts = value.Inputs.Select(i => new EvidenceFact(new AnnualFact(i.Fact.Concept, i.Fact.Concept, i.Fact.Value,
                i.Fact.Unit, i.Fact.Start, i.Fact.End, i.Fact.Form, i.Fact.Filed, i.Fact.Accession), row.Company.Cik, _links)).ToArray()
        };
    }
    private static string ValuationEvidence(ValuationResult? v) => v is null ? "\nNo valuation snapshot."
        : $"\nPrice: {v.Price?.Price} {v.Price?.Currency}; {v.Price?.ProviderId}/{v.Price?.Feed}; event {v.Price?.EventAt:O}; received {v.Price?.ReceivedAt:O}."
            + $"\nEquity review: {v.EquityBasis?.ReviewReference}; split basis through {v.EquityBasis?.SplitAdjustedThrough:yyyy-MM-dd}."
            + (v.CapitalStructure is not { } c ? "\nCapital review unavailable."
                : $"\nCapital filing: {c.FilingReference}; review: {c.ReviewReference}.\nOutstanding shares {c.OutstandingShares} at {c.SharesDate:yyyy-MM-dd}; balance sheet {c.BalanceSheetDate:yyyy-MM-dd}.\nDebt {c.Debt}; cash {c.Cash}; preferred {c.PreferredEquity}; NCI {c.NonControllingInterests} USD.\nEnterprise value {v.EnterpriseValue}; split basis through {c.SplitAdjustedThrough:yyyy-MM-dd}.");
}
