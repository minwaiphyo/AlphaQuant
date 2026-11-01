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
            "2026-11-01",
            [
                "Financial analysis now consumes comparison-1 annual/TTM results with source evidence and peer summaries.",
                "Browser OAuth, symbol access testing, session-only reviewed valuation inputs and optional IEX streaming are integrated; registration and live verification remain pending.",
                "Self-contained Windows preview packaging and CI are available; binaries remain unsigned.",
                "Native research workspace with system/light/dark appearance and persistent navigation.",
                "Select two to five peers, compare annual results, add numeric filters, sort, and inspect source evidence.",
                "Search SEC-listed companies by ticker or name and view the latest annual revenue, net income and current assets and liabilities.",
                "Settings page to enter the SEC contact email the SEC requires; it is stored locally and applies without a restart.",
                "Revenue growth, net profit margin and current ratio use test definitions (version test-0). They are not approved metric definitions.",
            ]),
    ];

    [RelayCommand]
    private void Back() => _showHome();
}
