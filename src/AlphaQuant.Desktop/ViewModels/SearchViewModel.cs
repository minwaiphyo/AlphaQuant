using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public sealed class SearchResultItem : ObservableObject
{
    public SearchResultItem(CompanyRef company, Action<CompanyRef> open, ComparisonViewModel? workspace = null)
    {
        Ticker = company.Ticker;
        Name = company.Name;
        CikText = $"CIK {company.Cik}";
        OpenCommand = new RelayCommand(() => open(company));
        Company = company;
        _workspace = workspace;
        ToggleCommand = new RelayCommand(() => workspace?.Toggle(company), () => workspace is not null && (workspace.Contains(company.Cik) || workspace.Peers.Count < 5));
    }

    private readonly ComparisonViewModel? _workspace;
    public CompanyRef Company { get; }
    public string SelectionLabel => _workspace?.Contains(Company.Cik) == true ? "Remove" : "Add +";
    public IRelayCommand ToggleCommand { get; }
    public void UpdateSelection() { OnPropertyChanged(nameof(SelectionLabel)); ToggleCommand.NotifyCanExecuteChanged(); }

    public string Ticker { get; }

    public string Name { get; }

    public string CikText { get; }

    public IRelayCommand OpenCommand { get; }
}

public partial class SearchViewModel : ObservableObject
{
    private readonly ICompanyDirectory _directory;
    private readonly Action<CompanyRef> _openCompany;
    private readonly Action _back;
    private readonly Action _openSettings;
    private IReadOnlyList<CompanyRef> _companies = [];
    private CancellationTokenSource? _cts;

    public SearchViewModel(ICompanyDirectory directory, Action<CompanyRef> openCompany, Action back, Action openSettings)
    {
        _directory = directory;
        _openCompany = openCompany;
        _back = back;
        _openSettings = openSettings;
    }

    public ComparisonViewModel? Workspace { get; set; }
    public Action? CompareAction { get; set; }
    public bool CanCompare => Workspace?.CanCompare == true;
    public void UpdateSelection() { foreach (var result in Results) result.UpdateSelection(); OnPropertyChanged(nameof(CanCompare)); CompareCommand.NotifyCanExecuteChanged(); }
    [RelayCommand(CanExecute = nameof(CanCompare))] private void Compare() => CompareAction?.Invoke();

    public ObservableCollection<SearchResultItem> Results { get; } = [];

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _setupMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _directoryNote;

    [ObservableProperty]
    private string _statusText = string.Empty;

    partial void OnQueryChanged(string value) => Refresh();

    /// <summary>Loads the SEC company list once; later visits keep the query and results.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_companies.Count > 0 || IsLoading)
        {
            return;
        }

        await LoadAsync();
    }

    public void CancelLoading() => _cts?.Cancel();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        IsLoading = true;
        ErrorMessage = null;
        SetupMessage = null;
        try
        {
            var result = await _directory.GetCompaniesAsync(cts.Token);
            _companies = result.Companies;
            DirectoryNote = result.IsStale
                ? $"The SEC could not be reached; using the cached company list from {result.RetrievedAt.ToLocalTime():yyyy-MM-dd HH:mm}."
                : null;
            Refresh();
        }
        catch (OperationCanceledException)
        {
            // Navigated away or superseded by a newer load.
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

    [RelayCommand]
    private void OpenFirst()
    {
        if (Results.Count > 0)
        {
            Results[0].OpenCommand.Execute(null);
        }
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

    private void Refresh()
    {
        Results.Clear();
        if (_companies.Count == 0)
        {
            StatusText = string.Empty;
            return;
        }

        var query = Query.Trim();
        if (query.Length == 0)
        {
            StatusText = "Type a ticker or company name, for example AAPL or apple.";
            return;
        }

        foreach (var company in CompanySearch.Search(_companies, query))
        {
            Results.Add(new SearchResultItem(company, _openCompany, Workspace));
        }

        StatusText = Results.Count == 0
            ? $"No companies match \"{query}\"."
            : $"{Results.Count} result(s). Select a company to see its latest annual facts.";
    }
}
