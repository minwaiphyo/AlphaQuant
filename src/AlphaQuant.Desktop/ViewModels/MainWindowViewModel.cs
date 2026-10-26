using CommunityToolkit.Mvvm.ComponentModel;

namespace AlphaQuant.Desktop.ViewModels;

/// <summary>Owns navigation: exactly one page is shown at a time.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly HomeViewModel _home;
    private readonly ReleaseNotesViewModel _releaseNotes;

    [ObservableProperty]
    private ObservableObject _currentPage;

    public MainWindowViewModel()
    {
        _home = new HomeViewModel(ShowReleaseNotes);
        _releaseNotes = new ReleaseNotesViewModel(ShowHome);
        _currentPage = _home;
    }

    private void ShowHome() => CurrentPage = _home;

    private void ShowReleaseNotes() => CurrentPage = _releaseNotes;
}
