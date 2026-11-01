using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public sealed class FactRow
{
    public FactRow(AnnualFact fact, long cik, ILinkOpener opener)
    {
        Label = fact.Label;
        Value = Display.Money(fact.Value, fact.Unit);
        Period = TestMetrics.PeriodText(fact);
        Form = fact.Form;
        Filed = fact.Filed.ToString("yyyy-MM-dd");
        Accession = fact.Accession;
        Tag = fact.Concept;
        Link = FilingLinks.TryBuild(cik, fact.Accession);
        OpenFilingCommand = new RelayCommand(
            () =>
            {
                if (Link is not null)
                {
                    opener.Open(Link);
                }
            },
            () => Link is not null);
    }

    public string Label { get; }

    public string Value { get; }

    public string Period { get; }

    public string Form { get; }

    public string Filed { get; }

    public string Accession { get; }

    public string Tag { get; }

    public Uri? Link { get; }

    public IRelayCommand OpenFilingCommand { get; }
}

public sealed class MetricRow
{
    public MetricRow(MetricResult metric)
    {
        Name = metric.Name;
        Value = Display.Metric(metric);
        Status = metric.Status.ToString();
        Version = metric.DefinitionVersion;
        Detail = metric.Reason is { } reason
            ? reason
            : string.Join(Environment.NewLine, metric.Inputs);
    }

    public string Name { get; }

    public string Value { get; }

    /// <summary>Spelled out in words so quality is never conveyed by colour alone.</summary>
    public string Status { get; }

    public string Version { get; }

    public string Detail { get; }
}

public partial class CompanyDetailViewModel : ObservableObject
{
    private readonly CompanyRef _company;
    private readonly ICompanyFactsSource _source;
    private readonly ILinkOpener _opener;
    private readonly Action _back;
    private readonly Action _openSettings;
    private CancellationTokenSource? _cts;
    private bool _userCancelled;

    public CompanyDetailViewModel(
        CompanyRef company,
        ICompanyFactsSource source,
        ILinkOpener opener,
        Action back,
        Action openSettings)
    {
        _company = company;
        _source = source;
        _opener = opener;
        _back = back;
        _openSettings = openSettings;
    }

    public string Title => $"{_company.Name} ({_company.Ticker})";

    public string Subtitle => $"CIK {_company.Cik:D10}";

    public ObservableCollection<FactRow> Facts { get; } = [];

    public ObservableCollection<MetricRow> Metrics { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasReport;

    [ObservableProperty]
    private bool _hasFacts;

    [ObservableProperty]
    private bool _hasWarnings;

    [ObservableProperty]
    private string? _setupMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _unsupportedReason;

    [ObservableProperty]
    private string? _retrievedText;

    public void CancelLoading()
    {
        _userCancelled = false;
        _cts?.Cancel();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        _userCancelled = false;
        IsLoading = true;
        Clear();
        try
        {
            var facts = await _source.GetFactsAsync(_company.Cik, cts.Token);
            Show(CompanyReports.Build(_company, facts));
        }
        catch (OperationCanceledException)
        {
            if (_userCancelled)
            {
                ErrorMessage = "Loading was cancelled.";
            }
        }
        catch (DataSourceException ex) when (ex.Kind == DataSourceErrorKind.ContactMissing)
        {
            SetupMessage = ex.Message;
        }
        catch (DataSourceException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_cts, cts))
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsLoading))]
    private void Cancel()
    {
        _userCancelled = true;
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Back()
    {
        CancelLoading();
        _back();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        CancelLoading();
        _openSettings();
    }

    /// <summary>Reloads after the user returns from Settings, but only if setup was what blocked loading.</summary>
    public async Task ReloadIfSetupWasNeededAsync()
    {
        if (SetupMessage is not null)
        {
            await LoadAsync();
        }
    }

    private void Clear()
    {
        Facts.Clear();
        Metrics.Clear();
        Warnings.Clear();
        HasReport = false;
        HasFacts = false;
        HasWarnings = false;
        SetupMessage = null;
        ErrorMessage = null;
        UnsupportedReason = null;
        RetrievedText = null;
    }

    public void ShowReport(CompanyReport report)
    {
        Clear();
        Show(report);
    }

    private void Show(CompanyReport report)
    {
        foreach (var fact in report.Facts)
        {
            Facts.Add(new FactRow(fact, report.Company.Cik, _opener));
        }

        foreach (var metric in report.Metrics)
        {
            Metrics.Add(new MetricRow(metric));
        }

        foreach (var warning in report.Warnings)
        {
            Warnings.Add(warning);
        }

        UnsupportedReason = report.UnsupportedReason;
        HasFacts = Facts.Count > 0;
        HasWarnings = Warnings.Count > 0;
        RetrievedText = report.IsStale
            ? $"Cached copy retrieved {report.RetrievedAt.ToLocalTime():yyyy-MM-dd HH:mm} (SEC unreachable)"
            : $"Retrieved {report.RetrievedAt.ToLocalTime():yyyy-MM-dd HH:mm}";
        HasReport = true;
    }
}
