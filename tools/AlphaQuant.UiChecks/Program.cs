using AlphaQuant.Core;
using AlphaQuant.Desktop;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Desktop.Views;
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
            if (view.Bounds.Width <= 0 || view.Bounds.Height <= 0) throw new InvalidOperationException("Empty layout");
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
