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

public sealed record MetricChoice(string Name)
{
    public string Label => Name + (Name == TestMetrics.CurrentRatioName ? " (×)" : " (%)");
}
public sealed record FilterOperator(string Label, string Symbol);
public sealed record SortChoice(string Label, int CellIndex);
public sealed record ComparisonHeader(string Label, IRelayCommand SortCommand);

public sealed class ScreenFilter
{
    public ScreenFilter(string metric, string op, decimal threshold, Action<ScreenFilter> remove)
    {
        Metric = metric; Operator = op; Threshold = threshold;
        RemoveCommand = new RelayCommand(() => remove(this));
    }
    public string Metric { get; }
    public string Operator { get; }
    public decimal Threshold { get; }
    public string Label => $"{Metric} {Operator} {Threshold.ToString("G", CultureInfo.GetCultureInfo("en-US"))}  ×";
    public IRelayCommand RemoveCommand { get; }
}

public sealed class EvidenceFact
{
    public EvidenceFact(AnnualFact fact, long cik, ILinkOpener opener)
    {
        Label = fact.Label;
        Value = Display.Money(fact.Value, fact.Unit);
        Period = TestMetrics.PeriodText(fact);
        Source = $"{fact.Form} · Filed {fact.Filed:yyyy-MM-dd}\n{fact.Accession}\n{fact.Concept} · {fact.Unit}";
        var url = FilingLinks.TryBuild(cik, fact.Accession);
        OpenFilingCommand = new RelayCommand(() => { if (url is not null) opener.Open(url); }, () => url is not null);
    }
    public string Label { get; }
    public string Value { get; }
    public string Period { get; }
    public string Source { get; }
    public IRelayCommand OpenFilingCommand { get; }
}

public sealed class CellEvidence
{
    public required string Company { get; init; }
    public required string Title { get; init; }
    public required string Value { get; init; }
    public required string Formula { get; init; }
    public required string Detail { get; init; }
    public required string Retrieved { get; init; }
    public required string Warnings { get; init; }
    public IReadOnlyList<EvidenceFact> Facts { get; init; } = [];
}

public sealed class ComparisonCell
{
    public required string Label { get; init; }
    public required string DisplayValue { get; init; }
    public decimal? NumericValue { get; init; }
    public required string Status { get; init; }
    public required IRelayCommand InspectCommand { get; init; }
}

public partial class PeerViewModel : ObservableObject
{
    public PeerViewModel(CompanyRef company, Action<PeerViewModel> remove)
    {
        Company = company;
        RemoveCommand = new RelayCommand(() => remove(this));
    }
    public CompanyRef Company { get; }
    public string Name => Company.Name;
    public string Ticker => Company.Ticker;
    public string Identity => $"CIK {Company.Cik:D10}";
    public IRelayCommand RemoveCommand { get; }
    public CompanyReport? Report { get; set; }
    [ObservableProperty] private string _loadStatus = "Not loaded";
    public bool NeedsContact { get; set; }
    public bool NeedsReload { get; set; } = true;
}

public sealed class ComparisonRow
{
    public required PeerViewModel Peer { get; init; }
    public required IReadOnlyList<ComparisonCell> Cells { get; init; }
    public required string Period { get; init; }
    public required string Status { get; init; }
    public required IRelayCommand OpenCompanyCommand { get; init; }
}

/// <summary>Session-only peer workspace over existing annual reports. No new financial definitions.</summary>
public partial class ComparisonViewModel : ObservableObject
{
    private readonly ICompanyFactsSource _source;
    private readonly ILinkOpener _opener;
    private readonly Action _chooseCompanies;
    private readonly Action _settings;
    private readonly Action<CompanyRef, CompanyReport?> _openCompany;
    private CancellationTokenSource? _loading;
    private int _generation;

    public ComparisonViewModel(ICompanyFactsSource source, ILinkOpener opener, Action chooseCompanies,
        Action settings, Action<CompanyRef, CompanyReport?> openCompany)
    {
        _source = source; _opener = opener; _chooseCompanies = chooseCompanies; _settings = settings; _openCompany = openCompany;
        _selectedMetric = Metrics[0]; _selectedOperator = Operators[0];
        SetSortChoices();
    }

    public ObservableCollection<PeerViewModel> Peers { get; } = [];
    public ObservableCollection<ComparisonRow> Rows { get; } = [];
    public ObservableCollection<ScreenFilter> Filters { get; } = [];
    public ObservableCollection<ComparisonHeader> Headers { get; } = [];
    public ObservableCollection<SortChoice> SortChoices { get; } = [];
    public IReadOnlyList<MetricChoice> Metrics { get; } = [new(TestMetrics.RevenueGrowthName), new(TestMetrics.NetMarginName), new(TestMetrics.CurrentRatioName)];
    public IReadOnlyList<FilterOperator> Operators { get; } = [new("Greater than", ">"), new("At least", "≥"), new("Less than", "<"), new("At most", "≤"), new("Equal to", "=")];
    public bool CanCompare => Peers.Count >= 2;
    public bool HasPeers => Peers.Count > 0;
    public bool IsEmpty => !CanCompare || Rows.Count == 0;
    public string SelectionText => $"{Peers.Count} / 5 companies selected";
    public string ResultsText => $"{Rows.Count} of {Peers.Count} selected companies";
    public bool HasEvidence => Evidence is not null;
    public bool HasSetupIssue => Peers.Any(p => p.NeedsContact);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isLoading;
    [ObservableProperty] private string _progress = "Choose companies to begin.";
    [ObservableProperty] private bool _showFacts;
    [ObservableProperty] private bool _isFilterEditorOpen;
    [ObservableProperty] private bool _descending;
    [ObservableProperty] private MetricChoice _selectedMetric;
    [ObservableProperty] private FilterOperator _selectedOperator;
    [ObservableProperty] private string _threshold = string.Empty;
    [ObservableProperty] private string? _filterError;
    [ObservableProperty] private SortChoice? _selectedSort;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasEvidence))] private CellEvidence? _evidence;

    partial void OnShowFactsChanged(bool value) { Evidence = null; SetSortChoices(); Rebuild(); }
    partial void OnDescendingChanged(bool value) => Rebuild();
    partial void OnSelectedSortChanged(SortChoice? value) => Rebuild();

    public bool Contains(long cik) => Peers.Any(p => p.Company.Cik == cik);
    public void Toggle(CompanyRef company)
    {
        var existing = Peers.FirstOrDefault(p => p.Company.Cik == company.Cik);
        if (existing is not null) Remove(existing);
        else if (Peers.Count < 5) { Peers.Add(new PeerViewModel(company, Remove)); SelectionChanged(); }
    }

    private void Remove(PeerViewModel peer)
    {
        // Removed peers cannot be updated by an in-flight response.
        Peers.Remove(peer); Evidence = null; SelectionChanged();
    }

    private void SelectionChanged()
    {
        OnPropertyChanged(nameof(CanCompare)); OnPropertyChanged(nameof(HasPeers)); OnPropertyChanged(nameof(SelectionText));
        ReloadCommand.NotifyCanExecuteChanged(); Rebuild();
        SelectionChangedEvent?.Invoke();
    }
    public event Action? SelectionChangedEvent;

    private bool CanReload() => CanCompare && !IsLoading;
    [RelayCommand(CanExecute = nameof(CanReload))]
    private Task ReloadAsync() => LoadAsync(true);
    public Task EnsureLoadedAsync() => LoadAsync(false);

    public async Task LoadAsync(bool reload, bool contactOnly = false)
    {
        if (IsLoading) return;
        var targets = Peers.Where(p => contactOnly ? p.NeedsContact : reload || p.NeedsReload).ToArray();
        if (targets.Length == 0) return;
        var generation = ++_generation;
        using var cts = new CancellationTokenSource();
        _loading = cts; IsLoading = true;
        try
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var peer = targets[i];
                if (!Peers.Contains(peer)) continue;
                Progress = $"Loading {peer.Ticker} · {i + 1} of {targets.Length}… Previous snapshots remain visible.";
                peer.LoadStatus = peer.Report is null ? "Loading…" : "Reloading · Previous snapshot"; Rebuild();
                try
                {
                    var facts = await _source.GetFactsAsync(peer.Company.Cik, cts.Token);
                    if (generation != _generation || cts.IsCancellationRequested) return;
                    if (!Peers.Contains(peer)) continue;
                    Evidence = null;
                    peer.Report = CompanyReports.Build(peer.Company, facts);
                    peer.NeedsContact = false; peer.NeedsReload = false;
                    peer.LoadStatus = facts.IsStale ? "Stale cached data" : "Loaded";
                }
                catch (OperationCanceledException) { if (generation == _generation && Peers.Contains(peer)) peer.LoadStatus = peer.Report is null ? "Cancelled · Retry available" : "Cancelled · Previous snapshot retained"; break; }
                catch (Exception ex)
                {
                    if (generation != _generation || cts.IsCancellationRequested) return;
                    if (!Peers.Contains(peer)) continue;
                    peer.NeedsContact = ex is DataSourceException { Kind: DataSourceErrorKind.ContactMissing };
                    peer.NeedsReload = true;
                    var message = ex is DataSourceException ? ex.Message : "Could not load company data. Retry is available.";
                    peer.LoadStatus = (peer.Report is null ? "" : "Previous snapshot retained · ") + message;
                }
                Rebuild();
            }
        }
        finally
        {
            if (generation == _generation)
            {
                _loading = null; IsLoading = false;
                Progress = cts.IsCancellationRequested ? "Loading cancelled. Existing snapshots retained." : "Annual SEC data · Existing cache policy applies. Reload to retry failed companies.";
                Rebuild();
            }
        }
    }

    public void CancelLoading()
    {
        if (_loading is null && !IsLoading) return;
        _loading?.Cancel();
        ++_generation;
        _loading = null; IsLoading = false;
        foreach (var peer in Peers.Where(p => p.LoadStatus.StartsWith("Loading", StringComparison.Ordinal) || p.LoadStatus.StartsWith("Reloading", StringComparison.Ordinal)))
            peer.LoadStatus = peer.Report is null ? "Cancelled · Retry available" : "Cancelled · Previous snapshot retained";
        Progress = "Loading cancelled. Existing snapshots retained."; Rebuild();
    }
    [RelayCommand(CanExecute = nameof(IsLoading))] private void Cancel() => CancelLoading();
    [RelayCommand] private void ShowMetrics() => ShowFacts = false;
    [RelayCommand] private void ShowReportedFacts() => ShowFacts = true;
    [RelayCommand] private void ChooseCompanies() => _chooseCompanies();
    [RelayCommand] private void OpenSettings() => _settings();
    [RelayCommand] private void CloseEvidence() => Evidence = null;
    [RelayCommand] private void ClearFilters() { Filters.Clear(); FilterError = null; Rebuild(); }
    [RelayCommand] private void OpenFilterEditor() => IsFilterEditorOpen = true;
    [RelayCommand] private void CancelFilter() { IsFilterEditorOpen = false; FilterError = null; }
    [RelayCommand]
    private void AddFilter()
    {
        if (!decimal.TryParse(Threshold, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out var value))
        { FilterError = "Enter a valid number, for example 10 or -2.5."; return; }
        FilterError = null;
        Filters.Add(new ScreenFilter(SelectedMetric.Name, SelectedOperator.Symbol, value, f => { Filters.Remove(f); Rebuild(); }));
        Threshold = string.Empty; IsFilterEditorOpen = false; Rebuild();
    }

    private void SetSortChoices()
    {
        Headers.Clear(); SortChoices.Clear();
        SortChoices.Add(new("Company", -1));
        var names = ShowFacts ? new[] { "Revenue · USD", "Net income · USD", "Current assets · USD", "Current liabilities · USD" }
            : new[] { "Revenue growth · %", "Net profit margin · %", "Current ratio · ×" };
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            Headers.Add(new ComparisonHeader(names[i] + " ↕", new RelayCommand(() => ToggleSort(index))));
            SortChoices.Add(new(names[i], i));
        }
        SelectedSort = SortChoices[0];
    }

    [RelayCommand] private void SortCompany() => ToggleSort(-1);
    private void ToggleSort(int index)
    {
        if (SelectedSort?.CellIndex == index) Descending = !Descending;
        else { Descending = false; SelectedSort = SortChoices.First(s => s.CellIndex == index); }
    }

    private static MetricResult? Metric(CompanyReport? report, string name) => report?.Metrics.FirstOrDefault(m => m.Name == name);
    private static bool Matches(PeerViewModel peer, ScreenFilter filter)
    {
        var metric = Metric(peer.Report, filter.Metric);
        if (metric is not { Status: MetricStatus.Available, Value: { } value }) return false;
        return filter.Operator switch { ">" => value > filter.Threshold, "≥" => value >= filter.Threshold, "<" => value < filter.Threshold, "≤" => value <= filter.Threshold, "=" => value == filter.Threshold, _ => false };
    }
    private void Rebuild()
    {
        var rows = Peers.Where(p => Filters.All(f => Matches(p, f))).Select(BuildRow).ToList();
        var index = SelectedSort?.CellIndex ?? -1;
        if (index >= 0 && rows.All(r => index < r.Cells.Count))
        {
            var comparer = Descending ? Comparer<decimal?>.Create((a, b) => Nullable.Compare(b, a)) : Comparer<decimal?>.Default;
            rows = rows.OrderBy(r => r.Cells[index].NumericValue is null)
                .ThenBy(r => r.Cells[index].NumericValue, comparer)
                .ThenBy(r => r.Peer.Name, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Peer.Company.Cik).ToList();
        }
        else rows = (Descending ? rows.OrderByDescending(r => r.Peer.Name, StringComparer.OrdinalIgnoreCase) : rows.OrderBy(r => r.Peer.Name, StringComparer.OrdinalIgnoreCase)).ThenBy(r => r.Peer.Company.Cik).ToList();
        Rows.Clear(); foreach (var row in rows) Rows.Add(row);
        OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(ResultsText)); OnPropertyChanged(nameof(HasSetupIssue));
    }
    private ComparisonRow BuildRow(PeerViewModel peer)
    {
        var report = peer.Report;
        var facts = report?.Facts ?? [];
        var cells = ShowFacts
            ? new[] { facts.FirstOrDefault(f => f.Label == "Revenue"), facts.FirstOrDefault(f => f.Label.StartsWith("Net income", StringComparison.Ordinal)), facts.FirstOrDefault(f => f.Label == "Current assets"), facts.FirstOrDefault(f => f.Label == "Current liabilities") }
                .Select((f, i) => FactCell(peer, f, new[] { "Revenue · USD", "Net income · USD", "Current assets · USD", "Current liabilities · USD" }[i])).ToArray()
            : Metrics.Select(m => MetricCell(peer, Metric(report, m.Name), m.Name)).ToArray();
        var revenue = facts.FirstOrDefault(f => f.Label == "Revenue");
        var status = report is null ? peer.LoadStatus : $"{peer.LoadStatus}\n{(report.UnsupportedReason is not null ? "Unsupported" : report.Warnings.Count > 0 ? "Review warnings" : "Available")}\nRetrieved {report.RetrievedAt:yyyy-MM-dd HH:mm} UTC";
        return new ComparisonRow
        {
            Peer = peer,
            Cells = cells,
            Period = revenue is null ? "No annual period available" : TestMetrics.PeriodText(revenue),
            Status = status,
            OpenCompanyCommand = new RelayCommand(() => _openCompany(peer.Company, peer.Report))
        };
    }
    private ComparisonCell MetricCell(PeerViewModel peer, MetricResult? metric, string name)
    {
        var usable = metric is { Status: MetricStatus.Available, Value: not null };
        return new ComparisonCell
        {
            Label = name,
            DisplayValue = usable ? Display.Metric(metric!) : metric?.Status == MetricStatus.Unsupported ? "Unsupported" : "Unavailable",
            NumericValue = usable ? metric!.Value : null,
            Status = metric?.Reason ?? (peer.Report is null ? "No report loaded" : "Annual · test-0"),
            InspectCommand = new RelayCommand(() => Inspect(peer, metric, null, name))
        };
    }
    private ComparisonCell FactCell(PeerViewModel peer, AnnualFact? fact, string label) => new()
    {
        Label = label,
        DisplayValue = fact is null ? "Unavailable" : Display.Money(fact.Value, fact.Unit),
        NumericValue = fact?.Value,
        Status = fact is null ? "No supported annual fact" : fact.Label + "\n" + TestMetrics.PeriodText(fact),
        InspectCommand = new RelayCommand(() => Inspect(peer, null, fact, fact?.Label ?? label))
    };
    private void Inspect(PeerViewModel peer, MetricResult? metric, AnnualFact? fact, string name)
    {
        var report = peer.Report;
        var inputs = report?.Facts.Where(f => name switch
        {
            TestMetrics.RevenueGrowthName => f.Label is "Revenue" or "Revenue (prior year)",
            TestMetrics.NetMarginName => f.Label == "Revenue" || f.Label.StartsWith("Net income", StringComparison.Ordinal),
            TestMetrics.CurrentRatioName => f.Label is "Current assets" or "Current liabilities",
            _ => f == fact
        }).ToArray() ?? [];
        Evidence = new CellEvidence
        {
            Company = $"{peer.Name} · {peer.Ticker}",
            Title = name,
            Value = metric is not null ? Display.Metric(metric) : fact is not null ? Display.Money(fact.Value, fact.Unit) : "Unavailable",
            Formula = name switch { TestMetrics.RevenueGrowthName => "(Current revenue / prior revenue − 1) × 100", TestMetrics.NetMarginName => "Net income / revenue × 100", TestMetrics.CurrentRatioName => "Current assets / current liabilities", _ => "Reported annual filing fact" },
            Detail = metric is null ? fact is null ? report?.UnsupportedReason ?? peer.LoadStatus : TestMetrics.PeriodText(fact) : $"{metric.Status} · Definition {metric.DefinitionVersion}\n{metric.Reason}\n{string.Join("\n", metric.Inputs)}",
            Retrieved = report is null ? "No snapshot available" : $"Retrieved {report.RetrievedAt:yyyy-MM-dd HH:mm} UTC · {(report.IsStale ? "Stale cached snapshot" : "SEC snapshot")}",
            Warnings = string.Join("\n", report?.Warnings ?? []),
            Facts = inputs.Select(f => new EvidenceFact(f, peer.Company.Cik, _opener)).ToArray()
        };
    }
}
