using System;
using AlphaQuant.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ContactSettings _settings;
    private readonly Action _back;

    public SettingsViewModel(ContactSettings settings, Action back)
    {
        _settings = settings;
        _back = back;
        _contact = settings.SavedContact ?? string.Empty;
    }

    public string SettingsFile => _settings.FilePath;

    /// <summary>Says which contact is in use, so an environment variable is never a hidden surprise.</summary>
    public string SourceText => _settings.Source switch
    {
        ContactSource.Saved => "The saved contact is used for SEC requests.",
        ContactSource.Environment => $"No contact is saved. The {SecContact.EnvironmentVariable} environment variable is used instead.",
        _ => "No contact is set. SEC requests are disabled until you save one.",
    };

    [ObservableProperty]
    private string _contact;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _statusIsError;

    /// <summary>Called when the page is shown, so it reflects the stored value rather than a stale edit.</summary>
    public void Reset()
    {
        Contact = _settings.SavedContact ?? string.Empty;
        StatusMessage = null;
        StatusIsError = false;
        OnPropertyChanged(nameof(SourceText));
    }

    [RelayCommand]
    private void Save()
    {
        Report(_settings.Save(Contact));
        if (StatusIsError is false)
        {
            Contact = _settings.SavedContact ?? string.Empty;
        }
    }

    [RelayCommand]
    private void ClearSaved()
    {
        Contact = string.Empty;
        Report(_settings.Save(null));
    }

    [RelayCommand]
    private void Back() => _back();

    private void Report(SettingsSaveResult result)
    {
        StatusIsError = !result.Success;
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(SourceText));
    }
}
