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

public partial class ResearchMetric : ObservableObject
{
    public ResearchMetric(string name, string value, string period, string reason, IRelayCommand inspectCommand, decimal? numericValue = null)
    { Name = name; Value = value; Period = period; Reason = reason; InspectCommand = inspectCommand; NumericValue = numericValue; }
    public string Name { get; }
    public string Period { get; }
    public decimal? NumericValue { get; private set; }
    [ObservableProperty] private string _value;
    [ObservableProperty] private string _reason;
    [ObservableProperty] private IRelayCommand _inspectCommand;
    public void Update(ResearchMetric next)
    { Value = next.Value; Reason = next.Reason; InspectCommand = next.InspectCommand; NumericValue = next.NumericValue; }
}
public partial class ResearchCompany : ObservableObject
{
    public ResearchCompany(string name, string status, string price, string warnings, IReadOnlyList<ResearchMetric> metrics)
    { Name = name; Status = status; Price = price; Warnings = warnings; foreach (var metric in metrics) Metrics.Add(metric); }
    public string Name { get; }
    [ObservableProperty] private string _status;
    [ObservableProperty] private string _price;
    [ObservableProperty] private string _warnings;
    public ObservableCollection<ResearchMetric> Metrics { get; } = [];
    public IReadOnlyList<ResearchMetric> Summary => Metrics.Where(m => m.Period.StartsWith("Annual", StringComparison.Ordinal)).Take(4)
        .Concat(Metrics.Where(m => m.Period.StartsWith("TTM", StringComparison.Ordinal)
        || m.Period == "Current valuation" || m.Period.StartsWith("Balance sheet", StringComparison.Ordinal)
        || m.Period.StartsWith("Reviewed balance sheet", StringComparison.Ordinal))).ToArray();
    public void Update(ResearchCompany next)
    {
        Status = next.Status; Price = next.Price; Warnings = next.Warnings;
        var desired = next.Metrics.Select(m =>
        {
            var old = Metrics.FirstOrDefault(x => x.Name == m.Name && x.Period == m.Period);
            if (old is null) return m;
            old.Update(m); return old;
        }).ToArray();
        var summaryChanged = !Metrics.SequenceEqual(desired);
        FinancialAnalysisViewModel.Synchronize(Metrics, desired);
        if (summaryChanged) OnPropertyChanged(nameof(Summary));
    }
}

public partial class FinancialAnalysisViewModel : ObservableObject
{
    private readonly IResearchBackend _backend;
    private readonly ILinkOpener _links;
    private CancellationTokenSource? _watch;
    private int _generation;
    private ComparisonSnapshot? _snapshot;
    private Task _activeLoad = Task.CompletedTask;
    private readonly TimeSpan _closeTimeout;
    private readonly Dictionary<long, ResearchCompany> _companies = [];
    private IReadOnlyList<CompanyRef> _selected = [];
    private IReadOnlyList<ResearchCompany> _rendered = [];
    public IReadOnlyList<string> MetricChoices { get; } = ["Annual revenue", "Annual revenue growth", "Annual operating margin", "Annual operating cash flow", "TTM revenue", "TTM operating margin", "TTM operating cash flow", "TTM diluted EPS", "Cash and cash equivalents", "Reviewed interest-bearing debt", "Trailing P/E", "EV / TTM sales"];
    public IReadOnlyList<string> Operators { get; } = [">=", "<=", ">", "<"];
    [ObservableProperty] private string _selectedMetric = "Annual revenue";
    [ObservableProperty] private string _selectedOperator = ">=";
    [ObservableProperty] private string _threshold = string.Empty;
    [ObservableProperty] private string _filterError = string.Empty;
    [ObservableProperty] private string _filterDescription = "No numeric filter. Values use the units displayed in each cell.";
    [ObservableProperty] private bool _descending;
    private (string Metric, string Operator, decimal Threshold)? _filter;
    public string ResultsText => $"{Companies.Count} of {_rendered.Count} companies · annual metrics use the latest available annual period; TTM is separate.";
    public bool NoResults => Companies.Count == 0;
    partial void OnSelectedMetricChanged(string value) => ApplyView();
    partial void OnDescendingChanged(bool value) => ApplyView();
    [RelayCommand] private void ApplyFilter()
    {
        if (DefinitionVersion != ComparisonAnalyzer.DefinitionVersion)
        { FilterError = "Numeric filtering requires one supported definition. Inspect each saved company's version or refresh all companies."; return; }
        if (!decimal.TryParse(Threshold, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var threshold))
        { FilterError = "Enter a number using a decimal point, without separators or a unit."; return; }
        if (!MetricChoices.Contains(SelectedMetric) || !Operators.Contains(SelectedOperator)) return;
        _filter = (SelectedMetric, SelectedOperator, threshold);
        FilterDescription = $"{SelectedMetric} {SelectedOperator} {threshold.ToString(CultureInfo.InvariantCulture)} in the cell's displayed unit. Unavailable values are excluded.";
        FilterError = string.Empty; ApplyView();
    }
    [RelayCommand] private void ClearFilter()
    { _filter = null; FilterError = string.Empty; FilterDescription = DefinitionVersion == ComparisonAnalyzer.DefinitionVersion
        ? "No numeric filter. Values use the units displayed in each cell."
        : "Older or mixed definitions: numeric filtering/sorting disabled; original per-company versions remain visible."; ApplyView(); }
    private void ApplyView()
    {
        decimal? Value(ResearchCompany company, string metric) => DefinitionVersion == ComparisonAnalyzer.DefinitionVersion
            ? company.Metrics.FirstOrDefault(m => m.Name == metric)?.NumericValue : null;
        var rows = _rendered.Where(c => _filter is not { } f || Value(c, f.Metric) is { } n && (f.Operator switch
        { ">=" => n >= f.Threshold, "<=" => n <= f.Threshold, ">" => n > f.Threshold, "<" => n < f.Threshold, _ => false }));
        // Missing values always sort last. Comparisons use raw decimals, never the rounded display text.
        var sorted = rows.OrderBy(c => Value(c, SelectedMetric) is null);
        var ordered = Descending ? sorted.ThenByDescending(c => Value(c, SelectedMetric)) : sorted.ThenBy(c => Value(c, SelectedMetric));
        Synchronize(Companies, ordered.ThenBy(c => c.Name, StringComparer.Ordinal).ToArray());
        OnPropertyChanged(nameof(ResultsText)); OnPropertyChanged(nameof(NoResults));
    }
    public FinancialAnalysisViewModel(IResearchBackend backend, ILinkOpener links, Action back, Action settings, TimeSpan? closeTimeout = null)
    {
        _backend = backend; _links = links;
        _closeTimeout = closeTimeout ?? TimeSpan.FromSeconds(3);
        BackCommand = new RelayCommand(back); SettingsCommand = new RelayCommand(settings);
    }
    public IRelayCommand BackCommand { get; }
    public IRelayCommand SettingsCommand { get; }
    public ObservableCollection<ResearchCompany> Companies { get; } = [];
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Title))] private string _definitionVersion = ComparisonAnalyzer.DefinitionVersion;
    [ObservableProperty] private bool _isOffline;
    private SavedFinancialSnapshot? _offlineSnapshot;
    public string Title => "Company comparison · " + DefinitionVersion;
    public ResearchScreenPreset CapturePreset() => new(DefinitionVersion, SelectedMetric, Descending,
        _filter?.Metric, _filter?.Operator, _filter?.Threshold);
    public string RestorePreset(ResearchScreenPreset? preset)
    {
        ClearFilter();
        if (preset is null) return string.Empty;
        if (preset.DefinitionVersion != DefinitionVersion || preset.DefinitionVersion != ComparisonAnalyzer.DefinitionVersion
            || !MetricChoices.Contains(preset.SortMetric) || (preset.FilterMetric is not null && (!MetricChoices.Contains(preset.FilterMetric) || !Operators.Contains(preset.Operator ?? string.Empty))))
            return "Saved filter definition is not supported here; stored values remain unchanged and no filter was applied.";
        if (preset.FilterMetric is not null && preset.Threshold is { } threshold)
        {
            SelectedMetric = preset.FilterMetric; SelectedOperator = preset.Operator!;
            Threshold = threshold.ToString(CultureInfo.InvariantCulture); ApplyFilter();
        }
        SelectedMetric = preset.SortMetric; Descending = preset.Descending;
        return string.Empty;
    }
    public FinancialSnapshotDraft CaptureFinancials(Guid setId, long revision)
    {
        if (_snapshot is null) throw new ResearchStorageException(ResearchStorageFailure.Validation, "Open a financial comparison before capturing it.");
        return new(setId, revision, DefinitionVersion, _snapshot.Rows.Select(r =>
            new SavedFinancialCompany(r.Company, r.Fundamentals, r.FactsRetrievedAt, r.FactsStale)
                { DefinitionVersion = r.FinancialDefinitionVersion }).ToArray());
    }
    public ReviewSourceInputs ReviewInputs(long cik)
    {
        var row = _snapshot?.Rows.FirstOrDefault(r => r.Company.Cik == cik);
        if (row?.Fundamentals is not { } f) return new([], row?.FactsRetrievedAt, true);
        var values = f.History.SelectMany(h => new[] { h.Revenue, h.RevenueGrowth, h.OperatingMargin, h.OperatingCashFlow })
            .Concat(new[] { f.TtmRevenue, f.TtmOperatingIncome, f.TtmOperatingMargin, f.TtmOperatingCashFlow, f.TtmDilutedEps, f.CashBalance, f.DebtBalance });
        return new(values.SelectMany(v => v.Inputs).Select(i => i.Fact).Distinct().OrderByDescending(fact => fact.Filed).Take(100).ToArray(), row.FactsRetrievedAt, IsOffline || row.FactsStale);
    }
    public async Task<bool> ShowSavedSnapshotAsync(SavedFinancialSnapshot saved, ResearchScreenPreset? preset = null)
    {
        StopWatching();
        var generation = ++_generation;
        try { await _activeLoad.WaitAsync(_closeTimeout); }
        catch (TimeoutException) { Status = "Previous update is still closing. Retry opening the saved snapshot shortly."; return false; }
        if (generation != _generation) return false;
        DefinitionVersion = saved.DefinitionVersion; IsOffline = true; _offlineSnapshot = saved;
        _selected = saved.Companies.Select(c => c.Company).ToArray();
        _snapshot = new(saved.Companies.Select(c => new AlphaQuant.Core.ComparisonRow(c.Company, c.Fundamentals, null,
            MarketConnectionState.Disconnected, c.Fundamentals is null ? "No financial results saved for this company." : null, c.FactsRetrievedAt, true)
                { FinancialDefinitionVersion = c.DefinitionVersion ?? saved.DefinitionVersion }).ToArray());
        Render(_snapshot); HasSetupIssue = false;
        var warning = RestorePreset(preset);
        Status = OfflineStatus(saved) + (warning.Length == 0 ? string.Empty : " " + warning);
        return true;
    }
    private static string OfflineStatus(SavedFinancialSnapshot saved) => $"Saved offline snapshot · captured {saved.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC · {saved.DefinitionVersion}. Original filing/retrieval dates retained. No current prices or valuations. Refresh is an explicit new request.";
    [ObservableProperty] private bool _hasSetupIssue;
    [RelayCommand] private void Refresh() => _ = LoadAsync(_selected);
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
        Status = IsOffline && _offlineSnapshot is not null ? OfflineStatus(_offlineSnapshot)
            : "Updates stopped. Financial snapshots retained; reopen analysis to refresh and subscribe.";
    }

    public Task LoadAsync(IReadOnlyList<CompanyRef> selected)
    {
        StopWatching();
        IsOffline = false; _offlineSnapshot = null;
        _selected = selected.ToArray();
        if (selected.Count is < 2 or > 5 || selected.Select(c => c.Cik).Distinct().Count() != selected.Count
            || selected.Select(c => c.Ticker).Distinct(StringComparer.OrdinalIgnoreCase).Count() != selected.Count)
        { DefinitionVersion = ComparisonAnalyzer.DefinitionVersion; _rendered = []; ApplyView(); _snapshot = null; Status = "Select two to five distinct companies first."; return Task.CompletedTask; }
        var retained = _snapshot?.Rows.Where(r => selected.Any(c => c == r.Company)).ToArray();
        if (retained is { Length: > 0 }) Render(new(retained)); else { DefinitionVersion = ComparisonAnalyzer.DefinitionVersion; _rendered = []; ApplyView(); }
        _snapshot = retained is { Length: > 0 } ? new(retained) : null;
        var generation = ++_generation;
        var previous = _activeLoad;
        var operation = LoadCoreAsync(previous, selected.ToArray(), generation);
        // Keep the original close pending even if this attempt times out. A retry must
        // not treat a timed-out wait as proof that the old enumerator was disposed.
        _activeLoad = previous.IsCompletedSuccessfully ? operation : Task.WhenAll(previous, operation);
        return operation;
    }
    private async Task LoadCoreAsync(Task previous, IReadOnlyList<CompanyRef> selected, int generation)
    {
        try { await previous.WaitAsync(_closeTimeout); }
        catch (TimeoutException)
        {
            if (generation == _generation) Status = "Previous update is still closing. Retry refresh shortly; no second subscription was started.";
            return;
        }
        if (generation != _generation) return;
        using var cts = new CancellationTokenSource(); _watch = cts; Running = true;
        HasSetupIssue = false;
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
                        FinancialDefinitionVersion = _snapshot.Rows.First(old => old.Company == row.Company).FinancialDefinitionVersion,
                        FactsStale = true, Error = "Refresh failed. Previous financial snapshot retained. " + row.Error }
                    : row).ToArray();
                _snapshot = new(merged); Render(_snapshot);
                HasSetupIssue = merged.Any(r => r.FinancialErrorKind == DataSourceErrorKind.ContactMissing);
                Status = HasSetupIssue ? "SEC contact required. Configure it in Data connections, then refresh. Previous snapshots may be retained."
                    : merged.Length == 0 || merged.All(r => r.Error is not null) ? "Financial refresh failed. Inspect each company for its reason; any retained snapshot is marked stale."
                    : merged.Any(r => r.Error is not null) ? "Some financial results failed. Inspect each company; retained snapshots are marked stale."
                    : _backend.Authorized ? "IEX limited-venue feed · test access for the selected symbols in Data connections before streaming. Inspect connection, timestamps and age below; market session unknown."
                    : "Fundamentals loaded. Connect Alpaca in Data connections for prices; reviewed equity/capital evidence is required for valuation.";
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (Exception) { if (generation == _generation) Status = "Could not refresh analysis. Check SEC contact in Data connections and retry. Existing snapshots retained."; }
        finally { if (generation == _generation) { Running = false; _watch = null; } }
    }

    private void Render(ComparisonSnapshot snapshot)
    {
        var definitions = snapshot.Rows.Where(r => r.Fundamentals is not null).Select(r => r.FinancialDefinitionVersion).Distinct().ToArray();
        DefinitionVersion = definitions.Length == 0 ? ComparisonAnalyzer.DefinitionVersion : definitions.Length == 1 ? definitions[0] : "mixed definitions (inspect each company)";
        if (DefinitionVersion != ComparisonAnalyzer.DefinitionVersion)
        {
            _filter = null;
            FilterDescription = "Older or mixed definitions: numeric filtering/sorting disabled; original per-company versions remain visible.";
        }
        else if (_filter is null) FilterDescription = "No numeric filter. Values use the units displayed in each cell.";
        var desired = new List<ResearchCompany>();
        foreach (var row in snapshot.Rows)
        {
            var metrics = new List<ResearchMetric>();
            void Add(FinancialValue value, string basis)
            {
                var definition = row.FinancialDefinitionVersion;
                var period = value.End is null ? basis : value.Start is null ? $"{basis} · at {value.End:yyyy-MM-dd}"
                    : $"{basis} · {value.Start:yyyy-MM-dd} to {value.End:yyyy-MM-dd}";
                metrics.Add(new(value.Name, Format(value), period, value.Reason ?? string.Empty,
                    new RelayCommand(() => Inspect(row, value, period, definition)), value.Available ? value.Value : null));
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
            var next = new ResearchCompany($"{row.Company.Name} · {row.Company.Ticker}",
                $"{row.Connection} · {row.FinancialDefinitionVersion} · Retrieved {row.FactsRetrievedAt:yyyy-MM-dd HH:mm} UTC{(row.FactsStale ? " · stale/retained financial snapshot" : "")}\n{row.Error}", price,
                string.Join("\n", f?.Warnings ?? []), metrics);
            if (_companies.TryGetValue(row.Company.Cik, out var existing)) existing.Update(next);
            else _companies[row.Company.Cik] = existing = next;
            desired.Add(existing);
        }
        foreach (var key in _companies.Keys.Where(k => !snapshot.Rows.Any(r => r.Company.Cik == k)).ToArray()) _companies.Remove(key);
        _rendered = desired; ApplyView();
    }
    internal static void Synchronize<T>(ObservableCollection<T> target, IReadOnlyList<T> desired) where T : class
    {
        for (var i = 0; i < desired.Count; i++)
        {
            if (i < target.Count && ReferenceEquals(target[i], desired[i])) continue;
            var old = target.IndexOf(desired[i]);
            if (old >= 0) target.Move(old, i); else target.Insert(i, desired[i]);
        }
        while (target.Count > desired.Count) target.RemoveAt(target.Count - 1);
    }
    private static string Format(FinancialValue value) => value.Available
        ? $"{value.Value!.Value.ToString(value.Unit == "USD/shares" ? "0.########" : "N2", CultureInfo.InvariantCulture)} {value.Unit}" : "Unavailable";
    private void Inspect(AlphaQuant.Core.ComparisonRow row, FinancialValue value, string period, string definition)
    {
        Evidence = new()
        {
            Company = row.Company.Name, Title = value.Name, Value = value.Available ? Display.FactValue(value.Value!.Value, value.Unit) : "Unavailable",
            Formula = definition != ComparisonAnalyzer.DefinitionVersion ? definition + " · Saved result; stored inputs shown without re-evaluating this older or unknown definition."
                : definition + " · " + (value.Name == "Trailing P/E" ? "Current eligible price / positive compatible TTM diluted EPS."
                : value.Name == "EV / TTM sales" ? "(Price × actual outstanding shares + debt + preferred equity + non-controlling interests − cash) / TTM revenue."
                : value.Name.Contains("margin", StringComparison.OrdinalIgnoreCase)
                ? "Operating income / matching-period revenue × 100."
                : value.Name.Contains("growth", StringComparison.OrdinalIgnoreCase)
                ? "(Current annual revenue / comparable prior annual revenue − 1) × 100."
                : "Reported value or reviewed sum of the weighted source contributions shown below. TTM uses a reported FY, four contiguous quarters, or FY + current YTD − prior YTD; EPS excludes YTD subtraction."),
            Detail = $"{period}\n{value.Reason}\n" + string.Join("\n", value.Inputs.Select(i => $"{i.Multiplier:+0.####;-0.####;0} × {i.Fact.Concept}: {i.Fact.Value} {i.Fact.Unit}"))
                + "\n" + string.Join("\n", value.EvidenceReferences)
                + (value.Name is "Trailing P/E" or "EV / TTM sales" ? ValuationEvidence(row.Valuation) : string.Empty),
            Retrieved = $"Financial snapshot {row.FactsRetrievedAt:yyyy-MM-dd HH:mm} UTC. Evidence captured {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC; this panel is a static snapshot and may be superseded by updates.",
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
