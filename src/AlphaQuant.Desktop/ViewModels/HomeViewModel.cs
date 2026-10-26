using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly Action _showReleaseNotes;

    public HomeViewModel(Action showReleaseNotes)
    {
        _showReleaseNotes = showReleaseNotes;
    }

    public string Message => "Hello from AlphaQuant";

    [RelayCommand]
    private void ViewReleaseNotes() => _showReleaseNotes();
}
