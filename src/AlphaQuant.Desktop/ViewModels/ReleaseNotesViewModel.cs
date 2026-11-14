using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using AlphaQuant.Infrastructure;
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

    public string Title => "Help, diagnostics and release notes";
    public string BuildIdentity => typeof(ReleaseNotesViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
    [ObservableProperty] private string _diagnosticPreview = string.Empty;
    [ObservableProperty] private string _feedbackPreview = string.Empty;
    [ObservableProperty] private string _exportPath = string.Empty;
    [ObservableProperty] private string _diagnosticStatus = "Preview safe metadata before exporting. No automatic telemetry or messages.";
    [RelayCommand] private void PreviewDiagnostics()
    {
        DiagnosticPreview = SafeDiagnostics.Capture(BuildIdentity);
        FeedbackPreview = SafeDiagnostics.FeedbackDraft(DiagnosticPreview);
        DiagnosticStatus = "Preview ready. Only the displayed safe metadata is exported; feedback remains a draft for you to submit.";
    }
    [RelayCommand] private async Task ExportDiagnosticsAsync()
    {
        if (string.IsNullOrWhiteSpace(DiagnosticPreview)) { DiagnosticStatus = "Preview diagnostics before exporting."; return; }
        try
        {
            await ResearchDocumentFile.SaveNewAsync(ExportPath, DiagnosticPreview);
            DiagnosticStatus = "Diagnostics saved locally. Nothing uploaded or sent.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { DiagnosticStatus = "Export failed. Choose a new file in a writable existing folder; previous files were preserved."; }
    }

    // Engineering capability notes; live/trusted release gates remain explicit.
    public IReadOnlyList<ReleaseNoteEntry> Entries { get; } =
    [
        new ReleaseNoteEntry("Engineering preview", "2026-11-14", ["Expanded versioned source metrics, immutable thesis/scenario research and safe local diagnostics.", "Explicit forecasts/DCF, bounded sensitivity/reverse models and frozen CSV/XLSX/PDF reports.", "Manual snapshot/condition review and sourced fundamentals history; no background monitoring.", "Live/provider, trusted Windows and historical valuation gates remain pending; unsigned development build only."]),
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
