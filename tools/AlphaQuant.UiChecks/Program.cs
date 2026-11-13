using AlphaQuant.Core;
using AlphaQuant.Desktop;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Desktop.Views;
using AlphaQuant.Infrastructure;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Automation;

// Fixture-only rendering. No production composition, settings, credentials or network.
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--output") throw new ArgumentException("Use --output <artifact-directory>");
        var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        AppBuilder.Configure<CheckApplication>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var backend = new FixtureBackend();
        var vm = new FinancialAnalysisViewModel(backend, new NoLinks(), () => {}, () => {});
        var peers = Enumerable.Range(1, 5).Select(i => new CompanyRef(i, $"DEMO{i}", $"Synthetic research company {i} with a long name")).ToArray();
        vm.LoadAsync(peers).GetAwaiter().GetResult();
        var view = new FinancialAnalysisView { DataContext = vm };
        var window = new Window { Content = view, Width = 1200, Height = 760, Padding = new Thickness(24) };
        window.Show();
        void Capture(string name, double width, double height, ThemeVariant theme)
        {
            window.Width = width; window.Height = height; window.RequestedThemeVariant = theme;
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame");
            frame.Save(Path.Combine(output, name + ".png"), new PngBitmapEncoderOptions());
            if (window.Content is not Control content || content.Bounds.Width <= 0 || content.Bounds.Height <= 0) throw new InvalidOperationException("Empty layout");
        }
        Capture("comparison-light", 1200, 760, ThemeVariant.Light);
        Capture("comparison-minimum-dark", 648, 560, ThemeVariant.Dark);
        var filters = view.GetVisualDescendants().OfType<Expander>().First();
        filters.IsExpanded = true;
        Capture("filters-minimum-dark", 648, 560, ThemeVariant.Dark);
        filters.IsExpanded = false;
        var metric = view.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("metric"));
        metric.Focus(); metric.Command!.Execute(null);
        metric.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        if (!view.FindControl<Button>("CloseEvidenceButton")!.IsFocused) throw new InvalidOperationException("Evidence did not receive keyboard focus");
        Capture("evidence-minimum-dark", 648, 560, ThemeVariant.Dark);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Dispatcher.UIThread.RunJobs();
        if (vm.HasEvidence || !metric.IsFocused) throw new InvalidOperationException("Escape did not return to origin");
        vm.SelectedMetric = "TTM diluted EPS"; vm.Threshold = "99"; vm.ApplyFilterCommand.Execute(null);
        Capture("empty-filter-light", 648, 560, ThemeVariant.Light);
        vm.ClearFilterCommand.Execute(null);
        backend.Fail = true; vm.LoadAsync(peers).GetAwaiter().GetResult();
        Capture("retained-setup-light", 1200, 760, ThemeVariant.Light);
        var researchRoot = Path.Combine(Path.GetTempPath(), "aq-ui-research-" + Guid.NewGuid().ToString("N"));
        var store = new SqliteResearchStore(Path.Combine(researchRoot, "research.db"));
        var savedVm = new SavedResearchViewModel(store, () => peers, vm,
            _ => throw new InvalidOperationException("No source refresh from the saved fixture"), vm.ShowSavedSnapshotAsync);
        void Complete(Task task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            { Dispatcher.UIThread.RunJobs(); Thread.Sleep(1); }
            if (!task.IsCompleted) throw new TimeoutException("Fixture research operation timed out");
            task.GetAwaiter().GetResult(); Dispatcher.UIThread.RunJobs();
        }
        try
        {
            Complete(savedVm.ReloadCommand.ExecuteAsync(null));
            savedVm.InputName = "Synthetic long-name peers and independent evidence";
            Complete(savedVm.SaveNewCommand.ExecuteAsync(null));
            Complete(savedVm.CaptureCommand.ExecuteAsync(null));
            if (savedVm.Snapshots.Count != 1) throw new InvalidOperationException("Saved snapshot was not captured");
            var savedView = new SavedResearchView { DataContext = savedVm };
            window.Content = savedView;
            Capture("saved-research-light", 1200, 760, ThemeVariant.Light);
            Capture("saved-research-minimum-dark", 648, 560, ThemeVariant.Dark);
            var savedScroll = savedView.GetVisualDescendants().OfType<ScrollViewer>().First();
            savedScroll.Offset = new Vector(0, 540);
            Capture("saved-members-minimum-dark", 648, 560, ThemeVariant.Dark);
            savedView.GetVisualDescendants().OfType<Expander>().First().IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            savedScroll.Offset = new Vector(0, Math.Max(0, savedScroll.Extent.Height - savedScroll.Viewport.Height));
            Capture("saved-recovery-minimum-dark", 648, 560, ThemeVariant.Dark);
            var name = savedView.GetVisualDescendants().OfType<TextBox>().First();
            name.Focus(); Dispatcher.UIThread.RunJobs();
            if (!name.IsFocused) throw new InvalidOperationException("Saved research name field did not receive focus");
            Complete(savedVm.OpenOfflineCommand.ExecuteAsync(null));
            if (!vm.IsOffline || vm.Companies.Count != peers.Length) throw new InvalidOperationException("Offline comparison did not retain selected financials");
            window.Content = view;
            Capture("saved-offline-dark", 648, 560, ThemeVariant.Dark);
            vm.Companies[0].Metrics[0].InspectCommand.Execute(null);
            Capture("saved-offline-evidence-dark", 648, 560, ThemeVariant.Dark);
        }
        finally { savedVm.CancelOperations(); if (Directory.Exists(researchRoot)) Directory.Delete(researchRoot, true); }
        var reviewStore = new ReviewedEvidenceStore();
        var reviewVm = new ValuationReviewViewModel(reviewStore, () => peers, vm.ReviewInputs, new NoLinks(), vm.StopWatching);
        reviewVm.RefreshCompanies();
        var reviewView = new ValuationReviewView { DataContext = reviewVm };
        var reviewScroll = new ScrollViewer { Content = reviewView, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        window.Content = reviewScroll;
        Capture("review-empty-light", 1200, 760, ThemeVariant.Light);
        Capture("review-minimum-dark", 648, 560, ThemeVariant.Dark);
        var filingInput = reviewView.GetVisualDescendants().OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Review filing accession");
        filingInput.Focus(); Dispatcher.UIThread.RunJobs();
        if (!filingInput.IsFocused) throw new InvalidOperationException("Review filing field did not receive focus");
        reviewVm.FilingAccession = "0000000001-26-000001";
        reviewVm.ReviewReference = "Synthetic source review only; no issuer eligibility or real source acceptance claimed";
        reviewVm.ScopeConfirmed = reviewVm.ClassConfirmed = true; reviewVm.IncludeCapital = true;
        reviewVm.SplitThrough = DateTime.UtcNow.ToString("yyyy-MM-dd");
        reviewVm.SharesDate = reviewVm.BalanceSheetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        reviewVm.OutstandingShares = "100"; reviewVm.Debt = "400"; reviewVm.CapitalClaimsConfirmed = true;
        reviewVm.ApplyCommand.Execute(null);
        if (reviewStore.Capital.Count != 0) throw new InvalidOperationException("Incomplete review was accepted");
        reviewScroll.Offset = default;
        Capture("review-missing-capital-dark", 648, 560, ThemeVariant.Dark);
        reviewVm.Cash = "100"; reviewVm.PreferredEquity = reviewVm.NonControllingInterests = "0";
        reviewVm.ApplyCommand.Execute(null);
        if (reviewStore.Capital.Count != 1) throw new InvalidOperationException("Explicit synthetic review was not applied");
        Dispatcher.UIThread.RunJobs();
        var sharesInput = reviewView.GetVisualDescendants().OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Actual outstanding common shares");
        var sharesPoint = sharesInput.TranslatePoint(default, reviewView) ?? throw new InvalidOperationException("Share input layout missing");
        reviewScroll.Offset = new Vector(0, Math.Max(0, sharesPoint.Y - 80));
        Capture("review-capital-fields-dark", 648, 560, ThemeVariant.Dark);
        var preferredInput = reviewView.GetVisualDescendants().OfType<TextBox>().Single(t => AutomationProperties.GetName(t) == "Reviewed preferred equity USD");
        var preferredPoint = preferredInput.TranslatePoint(default, reviewView) ?? throw new InvalidOperationException("Preferred input layout missing");
        reviewScroll.Offset = new Vector(0, Math.Max(0, preferredPoint.Y - 80));
        Capture("review-explicit-zero-claims-dark", 648, 560, ThemeVariant.Dark);
        var sourceExpander = reviewView.GetVisualDescendants().OfType<Expander>().First(); sourceExpander.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        var sourcePoint = sourceExpander.TranslatePoint(default, reviewView) ?? throw new InvalidOperationException("Source layout missing");
        reviewScroll.Offset = new Vector(0, Math.Max(0, sourcePoint.Y - 200));
        Capture("review-applied-source-dark", 648, 560, ThemeVariant.Dark);
        var mixedDraft = vm.CaptureFinancials(Guid.NewGuid(), 1);
        var mixedCompanies = mixedDraft.Companies.Select((company, index) => index == 1
            ? company with { DefinitionVersion = "comparison-legacy-fixture" } : company).ToArray();
        Complete(vm.ShowSavedSnapshotAsync(new SavedFinancialSnapshot(Guid.NewGuid(), mixedDraft.PeerSetId, 1,
            DateTimeOffset.UtcNow, "mixed definitions", mixedCompanies)));
        if (!vm.Title.Contains("mixed definitions") || !vm.FilterDescription.Contains("disabled")) throw new InvalidOperationException("Mixed-definition guards were not visible");
        window.Content = view;
        Capture("mixed-definition-offline-dark", 648, 560, ThemeVariant.Dark);
        window.Close();
        File.WriteAllText(Path.Combine(output, "scope.txt"), "Synthetic services only. Real Avalonia layout/rendering and evidence Escape/focus checked. Viewport 648x560 approximates the research area at the 900x720 shell minimum. Not a native accessibility or live-provider certification.\n");
        Console.WriteLine("UI fixture rendering and evidence keyboard checks passed.");
    }
}
internal sealed class CheckApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://AlphaQuant.Desktop/")) { Source = new Uri("avares://AlphaQuant.Desktop/Styles/ResearchTheme.axaml") });
    }
}
internal sealed class NoLinks : ILinkOpener { public void Open(Uri uri) => throw new InvalidOperationException("Links disabled in fixture harness"); }
internal sealed class FixtureBackend : IResearchBackend
{
    public bool Fail;
    public bool Configured => false;
    public bool Authorized => false;
    public Task ConnectAsync(CancellationToken token) => throw new InvalidOperationException("No live connection");
    public Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token) => throw new InvalidOperationException("No live access");
    public void Disconnect() { }
    public async IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        await Task.CompletedTask;
        var facts = new List<RawFact>();
        foreach (var year in Enumerable.Range(2021, 5))
        {
            void Add(string concept, string unit, decimal value) => facts.Add(new(concept, unit, value, new(year, 1, 1), new(year, 12, 31), "10-K", new(year + 1, 2, 1), "0000000001-26-000001"));
            Add("Revenues", "USD", (year - 2020) * 1000000m); Add("OperatingIncomeLoss", "USD", 250000m);
            Add("NetCashProvidedByUsedInOperatingActivities", "USD", 300000m); Add("EarningsPerShareDiluted", "USD/shares", 0.0046m);
        }
        var fundamentals = ComparisonAnalyzer.Analyze(facts, new(2026, 3, 1));
        yield return new(companies.Select(c => new AlphaQuant.Core.ComparisonRow(c, Fail ? null : fundamentals, null, MarketConnectionState.Disconnected,
            Fail ? "Configure the SEC contact in Data connections, then refresh." : null, DateTimeOffset.UtcNow, false)
            { FinancialErrorKind = Fail ? DataSourceErrorKind.ContactMissing : null }).ToArray());
    }
}
