using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public sealed record ReleaseNoteEntry(string Version, string Date, IReadOnlyList<string> Changes);

public partial class ReleaseNotesViewModel : ObservableObject
{
    private readonly Action _showHome;

    public ReleaseNotesViewModel(Action showHome)
    {
        _showHome = showHome;
    }

    public string Title => "Release Notes";

    // Static placeholder content until releases exist.
    public IReadOnlyList<ReleaseNoteEntry> Entries { get; } =
    [
        new ReleaseNoteEntry(
            "Unreleased",
            "2026-10-26",
            [
                "Placeholder desktop window.",
                "Navigation between the Home and Release Notes pages.",
                "No financial functionality yet.",
            ]),
    ];

    [RelayCommand]
    private void Back() => _showHome();
}
