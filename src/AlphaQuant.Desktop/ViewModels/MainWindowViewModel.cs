using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

/// <summary>Owns navigation: exactly one page is shown at a time.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICompanyFactsSource _facts;
    private readonly ILinkOpener _linkOpener;
    private readonly HomeViewModel _home;
    private readonly ReleaseNotesViewModel _releaseNotes;
    private readonly SearchViewModel _search;
    private readonly SettingsViewModel _settings;
    public ComparisonViewModel Comparison { get; }
    private CompanyDetailViewModel? _detail;
    private ObservableObject? _settingsReturnPage;

    [ObservableProperty]
    private ObservableObject _currentPage;

    public MainWindowViewModel(
        ICompanyDirectory directory,
        ICompanyFactsSource facts,
        ContactSettings contactSettings,
        ILinkOpener linkOpener)
    {
        _facts = facts;
        _linkOpener = linkOpener;
        _home = new HomeViewModel(ShowSearch, OpenSettingsFromHome, ShowReleaseNotes);
        _releaseNotes = new ReleaseNotesViewModel(ShowHome);
        _search = new SearchViewModel(directory, ShowCompany, ShowHome, OpenSettingsFromSearch);
        _settings = new SettingsViewModel(contactSettings, CloseSettings);
        Comparison = new ComparisonViewModel(facts, linkOpener, ShowSearch, OpenSettingsFromComparison, ShowComparisonCompany);
        _search.Workspace = Comparison;
        _search.CompareAction = ShowComparison;
        Comparison.SelectionChangedEvent += _search.UpdateSelection;
        _currentPage = _home;
    }

    public bool IsHome => CurrentPage == _home;
    public bool IsSearch => CurrentPage == _search || CurrentPage == _detail;
    public bool IsComparison => CurrentPage == Comparison;
    public bool IsSettings => CurrentPage == _settings;
    public bool IsReleaseNotes => CurrentPage == _releaseNotes;
    public string PageTitle => IsHome ? "Overview" : IsSearch ? "Company research" : IsComparison ? "Comparison" : IsSettings ? "Data connections" : "Release notes";

    partial void OnCurrentPageChanging(ObservableObject value)
    {
        if (CurrentPage == Comparison && value != Comparison) Comparison.CancelLoading();
        if (CurrentPage == _search && value != _search) _search.CancelLoading();
        if (CurrentPage == _detail && value != _detail) _detail?.CancelLoading();
    }
    partial void OnCurrentPageChanged(ObservableObject value)
    {
        foreach (var name in new[] { nameof(IsHome), nameof(IsSearch), nameof(IsComparison), nameof(IsSettings), nameof(IsReleaseNotes), nameof(PageTitle) }) OnPropertyChanged(name);
    }
    [RelayCommand] private void ShowHome() => CurrentPage = _home;
    [RelayCommand]
    private void ShowComparison()
    {
        if (!Comparison.CanCompare) { CurrentPage = Comparison; return; }
        CurrentPage = Comparison;
        _ = Comparison.EnsureLoadedAsync();
    }
    [RelayCommand] private void NavigateSettings() { if (!IsSettings) OpenSettings(CurrentPage); }
    private void OpenSettingsFromComparison() => OpenSettings(Comparison);
    private void ShowComparisonCompany(CompanyRef company, CompanyReport? report)
    {
        _detail = new CompanyDetailViewModel(company, _facts, _linkOpener, ShowComparison, OpenSettingsFromDetail);
        CurrentPage = _detail;
        if (report is not null) _detail.ShowReport(report);
        else _ = _detail.LoadAsync();
    }

    [RelayCommand]
    private void ShowReleaseNotes() => CurrentPage = _releaseNotes;

    [RelayCommand]
    private void ShowSearch()
    {
        _detail?.CancelLoading();
        CurrentPage = _search;
        _ = _search.EnsureLoadedAsync();
    }

    private void ShowCompany(CompanyRef company)
    {
        _detail?.CancelLoading();
        _detail = new CompanyDetailViewModel(company, _facts, _linkOpener, ShowSearch, OpenSettingsFromDetail);
        CurrentPage = _detail;
        _ = _detail.LoadAsync();
    }

    private void OpenSettingsFromHome() => OpenSettings(_home);

    private void OpenSettingsFromSearch() => OpenSettings(_search);

    private void OpenSettingsFromDetail()
    {
        if (_detail is not null)
        {
            OpenSettings(_detail);
        }
    }

    /// <summary>Opens Settings and remembers which page to return to.</summary>
    private void OpenSettings(ObservableObject returnTo)
    {
        _settingsReturnPage = returnTo;
        _settings.Reset();
        CurrentPage = _settings;
    }

    private void CloseSettings()
    {
        var target = _settingsReturnPage ?? _home;
        _settingsReturnPage = null;
        CurrentPage = target;

        // Retry whatever setup was blocking, now that the contact may have changed.
        if (ReferenceEquals(target, Comparison))
        {
            _ = Comparison.LoadAsync(false, contactOnly: true);
        }
        else if (ReferenceEquals(target, _search))
        {
            _ = _search.EnsureLoadedAsync();
        }
        else if (ReferenceEquals(target, _detail))
        {
            _ = _detail!.ReloadIfSetupWasNeededAsync();
        }
    }
}
