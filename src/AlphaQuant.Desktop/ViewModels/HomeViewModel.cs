using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly Action _showSearch;
    private readonly Action _showSettings;
    private readonly Action _showReleaseNotes;

    public HomeViewModel(Action showSearch, Action showSettings, Action showReleaseNotes)
    {
        _showSearch = showSearch;
        _showSettings = showSettings;
        _showReleaseNotes = showReleaseNotes;
    }

    public string Message => "Hello from AlphaQuant";

    [RelayCommand]
    private void SearchCompanies() => _showSearch();

    [RelayCommand]
    private void OpenSettings() => _showSettings();

    [RelayCommand]
    private void ViewReleaseNotes() => _showReleaseNotes();
}
