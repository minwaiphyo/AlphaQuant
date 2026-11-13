using System.Runtime.CompilerServices;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public class ResearchUiIntegrationTests
{
    private static readonly CompanyRef[] Selection = [new(1, "ALDR", "Alder"), new(2, "BRCH", "Birch")];
    private sealed class Links : ILinkOpener { public Uri? Last; public void Open(Uri uri) => Last = uri; }
    private sealed class Backend : IResearchBackend
    {
        public bool Configured { get; set; } = true;
        public bool Authorized { get; set; }
        public int Connections;
        public Func<CancellationToken, Task>? Connect;
        public Func<CancellationToken, Task<MarketAccess>>? Test { get; set; }
        public Func<CancellationToken, IAsyncEnumerable<ComparisonSnapshot>>? Watch;
        public Task ConnectAsync(CancellationToken token) { Connections++; return Connect?.Invoke(token) ?? Task.CompletedTask; }
        public Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token) => Test?.Invoke(token)
            ?? Task.FromResult(new MarketAccess(new("alpaca", "iex", "IEX only", true, true), symbols));
        public IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies, CancellationToken token) => Watch?.Invoke(token) ?? Once(Snapshot());
        public void Disconnect() => Authorized = false;
    }
    private static ComparisonSnapshot Snapshot(bool failure = false, bool price = false)
    {
        RawFact Fact(string concept, decimal value) => new(concept, "USD", value, new(2025,1,1), new(2025,12,31), "10-K", new(2026,2,1), "0000000001-26-000001");
        var f = ComparisonAnalyzer.Analyze([Fact("Revenues", 120m), Fact("OperatingIncomeLoss", 24m), Fact("NetCashProvidedByUsedInOperatingActivities", 20m)], new(2026,3,1));
        return new(Selection.Select(c => new AlphaQuant.Core.ComparisonRow(c, failure ? null : f,
            price ? new("comparison-1", ComparisonAnalyzer.Missing("P/E", "x", "Review needed"), ComparisonAnalyzer.Missing("EV/sales", "x", "Review needed"), null,
                new(c.Ticker, 60m, "USD", "alpaca", "iex", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "1"), null, null) : null,
            price ? MarketConnectionState.Connected : MarketConnectionState.Disconnected, failure ? "Offline" : null, DateTimeOffset.UtcNow, false)).ToArray());
    }
    private static async IAsyncEnumerable<ComparisonSnapshot> Once(ComparisonSnapshot snapshot)
    { await Task.CompletedTask; yield return snapshot; }

    [Fact]
    public async Task FinancialScreenShowsApprovedHistoryAndSourceEvidenceWithoutMarketConnection()
    {
        var links = new Links(); var vm = new FinancialAnalysisViewModel(new Backend(), links, () => {}, () => {});
        await vm.LoadAsync(Selection);
        Assert.Equal(2, vm.Companies.Count);
        Assert.Contains(vm.Companies[0].Metrics, m => m.Name.Contains("operating margin") && m.Value.Contains("20.00"));
        var revenue = vm.Companies[0].Metrics.First(m => m.Name == "Annual revenue");
        revenue.InspectCommand.Execute(null);
        Assert.True(vm.HasEvidence); Assert.Contains("comparison-1", vm.Evidence!.Formula);
        Assert.Single(vm.Evidence.Facts); vm.Evidence.Facts[0].OpenFilingCommand.Execute(null);
        Assert.True(FilingLinks.IsSafeToOpen(links.Last!));
        Assert.Contains(vm.Companies[0].Metrics, m => m.Name == "Trailing P/E" && m.Value == "Unavailable");
    }

    [Fact]
    public async Task FirstLoadFailureHasOneUnavailableCellPerValuationInSummary()
    {
        var vm = new FinancialAnalysisViewModel(new Backend { Watch = _ => Once(Snapshot(failure: true)) }, new Links(), () => {}, () => {});
        await vm.LoadAsync(Selection);
        Assert.Equal(2, vm.Companies[0].Summary.Count);
        Assert.Equal(2, vm.Companies[0].Summary.Select(m => m.Name).Distinct().Count());
        Assert.Contains("refresh failed", vm.Status);
        Assert.DoesNotContain("Fundamentals loaded", vm.Status);
    }

    [Fact]
    public async Task FiltersUseRawValuesAndMissingValuesSortLastInBothDirections()
    {
        var original = Snapshot();
        var first = original.Rows[0];
        var financials = first.Fundamentals!;
        var second = original.Rows[1] with { Fundamentals = financials with { TtmDilutedEps = new("TTM diluted EPS", 0.0046m, "USD/shares", null, null, []) } };
        var vm = new FinancialAnalysisViewModel(new Backend { Watch = _ => Once(new([first, second])) }, new Links(), () => {}, () => {});
        await vm.LoadAsync(Selection);
        vm.SelectedMetric = "TTM diluted EPS";
        Assert.Contains("Birch", vm.Companies[0].Name);
        vm.Descending = true; Assert.Contains("Birch", vm.Companies[0].Name);
        vm.Threshold = "0.0045"; vm.ApplyFilterCommand.Execute(null);
        Assert.Single(vm.Companies); Assert.Contains("Birch", vm.Companies[0].Name);
        vm.Threshold = "0.0047"; vm.ApplyFilterCommand.Execute(null); Assert.Empty(vm.Companies);
        vm.ClearFilterCommand.Execute(null); Assert.Equal(2, vm.Companies.Count);
        vm.Threshold = "1,000"; vm.ApplyFilterCommand.Execute(null); Assert.NotEmpty(vm.FilterError); Assert.Equal(2, vm.Companies.Count);
    }

    [Fact]
    public async Task RepeatedSnapshotsRetainPresentationObjectsAndUpdateEvidence()
    {
        var advance = new TaskCompletionSource();
        async IAsyncEnumerable<ComparisonSnapshot> Updates()
        {
            yield return Snapshot(); await advance.Task; yield return Snapshot(price: true);
        }
        var vm = new FinancialAnalysisViewModel(new Backend { Watch = _ => Updates() }, new Links(), () => {}, () => {});
        var load = vm.LoadAsync(Selection);
        var company = vm.Companies[0]; var metric = company.Metrics[0]; var summary = company.Summary[0];
        var oldCommand = metric.InspectCommand;
        advance.SetResult(); await load;
        Assert.Same(company, vm.Companies[0]); Assert.Same(metric, company.Metrics[0]); Assert.Same(summary, company.Summary[0]);
        Assert.NotSame(oldCommand, metric.InspectCommand); Assert.Contains("60.00", company.Price);
    }

    [Fact]
    public async Task ContactFailureIsTypedAndSafeEvenWhenEarlierFinancialsAreRetained()
    {
        var backend = new Backend();
        var vm = new FinancialAnalysisViewModel(backend, new Links(), () => {}, () => {});
        await vm.LoadAsync(Selection);
        backend.Watch = _ => Once(new(Snapshot(true).Rows.Select(r => r with { FinancialErrorKind = DataSourceErrorKind.ContactMissing }).ToArray()));
        await vm.LoadAsync(Selection);
        Assert.True(vm.HasSetupIssue); Assert.Contains("SEC contact required", vm.Status);
        Assert.NotEmpty(vm.Companies[0].Metrics);
    }

    [Fact]
    public async Task TimedOutCloseCannotBeBypassedByAnotherRefresh()
    {
        var close = new TaskCompletionSource(); var calls = 0;
        async IAsyncEnumerable<ComparisonSnapshot> Blocked()
        { calls++; await close.Task; yield return Snapshot(); }
        var backend = new Backend { Watch = _ => Blocked() };
        var vm = new FinancialAnalysisViewModel(backend, new Links(), () => {}, () => {}, TimeSpan.FromMilliseconds(10));
        var old = vm.LoadAsync(Selection);
        await vm.LoadAsync(Selection); await vm.LoadAsync(Selection);
        Assert.Equal(1, calls); Assert.Contains("still closing", vm.Status);
        close.SetResult(); await old;
        await vm.LoadAsync(Selection); Assert.Equal(2, calls);
    }

    [Fact]
    public void EvidenceFactKeepsSubCentEpsPrecision()
    {
        var fact = new EvidenceFact(new AnnualFact("EPS", "EPS", 0.0046m, "USD/shares", new(2025,1,1), new(2025,12,31), "10-K", new(2026,2,1), "0000000001-26-000001"), 1, new Links());
        Assert.Equal("0.0046 USD/shares", fact.Value);
    }

    [Fact]
    public async Task FailedRefreshRetainsFinancialsAndNeverPreviousPrices()
    {
        var backend = new Backend { Watch = _ => Once(Snapshot(price: true)) };
        var vm = new FinancialAnalysisViewModel(backend, new Links(), () => {}, () => {});
        await vm.LoadAsync(Selection); Assert.Contains("60.00", vm.Companies[0].Price);
        backend.Watch = _ => Once(Snapshot(failure: true));
        await vm.LoadAsync(Selection);
        Assert.Contains("Previous financial snapshot retained", vm.Companies[0].Status);
        Assert.Contains("Price unavailable", vm.Companies[0].Price);
        Assert.Contains(vm.Companies[0].Metrics, m => m.Name == "Annual revenue" && m.Value.Contains("120.00"));
    }

    [Fact]
    public async Task CancellationRejectsLateSnapshotAndNewSelectionDoesNotShowRemovedCompany()
    {
        var pending = new TaskCompletionSource();
        async IAsyncEnumerable<ComparisonSnapshot> Late([EnumeratorCancellation] CancellationToken token)
        { await pending.Task; yield return Snapshot(price: true); }
        var backend = new Backend { Watch = Late };
        var vm = new FinancialAnalysisViewModel(backend, new Links(), () => {}, () => {});
        var old = vm.LoadAsync(Selection); vm.StopWatching();
        backend.Watch = _ => Once(new([])); await vm.LoadAsync([new(3,"FERN","Fern"), new(4,"PINE","Pine")]);
        pending.SetResult(); await old;
        Assert.Empty(vm.Companies); Assert.False(vm.Running);
    }

    [Fact]
    public async Task DisconnectInvalidatesDisplayedQuoteImmediately()
    {
        var backend = new Backend { Watch = _ => Once(Snapshot(price: true)), Authorized = true };
        var vm = new FinancialAnalysisViewModel(backend, new Links(), () => {}, () => {});
        var market = new MarketConnectionViewModel(backend, () => ["ALDR","BRCH"]);
        market.Disconnected += vm.StopWatching;
        await vm.LoadAsync(Selection); market.DisconnectCommand.Execute(null);
        Assert.False(backend.Authorized); Assert.Contains("Price unavailable", vm.Companies[0].Price);
        Assert.NotEmpty(vm.Companies[0].Metrics);
    }

    [Fact]
    public async Task UnconfiguredConnectionDoesNotCallBackend()
    {
        var backend = new Backend { Configured = false };
        var vm = new MarketConnectionViewModel(backend, () => []);
        await vm.ConnectCommand.ExecuteAsync(null);
        Assert.Equal(0, backend.Connections); Assert.Contains("Configuration required", vm.Status);
    }

    [Fact]
    public async Task CancellationClearsLateAuthorizationAndDoesNotExposeExceptionText()
    {
        var pending = new TaskCompletionSource(); var backend = new Backend();
        backend.Connect = async _ => { await pending.Task; backend.Authorized = true; };
        var vm = new MarketConnectionViewModel(backend, () => []);
        var operation = vm.ConnectCommand.ExecuteAsync(null); vm.CancelCommand.Execute(null); pending.SetResult(); await operation;
        Assert.False(backend.Authorized); Assert.Contains("cancelled", vm.Status);
        backend.Connect = _ => throw new Exception("secret-token-do-not-show");
        await vm.ConnectCommand.ExecuteAsync(null);
        Assert.DoesNotContain("secret-token", vm.Status); Assert.False(vm.Busy);
    }

    [Fact]
    public async Task AccessTestRequiresAuthorizationAndSelection()
    {
        var backend = new Backend(); var vm = new MarketConnectionViewModel(backend, () => []);
        await vm.TestCommand.ExecuteAsync(null); Assert.Contains("Connect", vm.Status);
        backend.Authorized = true; await vm.TestCommand.ExecuteAsync(null); Assert.Contains("Choose companies", vm.Status);
    }

    private sealed class Facts : ICompanyFactsSource
    {
        public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken token) => Task.FromResult(new CompanyFacts("Synthetic", [], 0, DateTimeOffset.UtcNow, false));
    }
    private sealed class DirectorySource : ICompanyDirectory
    {
        public Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken token) => Task.FromResult(new CompanyDirectoryResult(Selection, DateTimeOffset.UtcNow, false));
    }
    [Fact]
    public async Task ProductionCompareUsesOneAnalysisAndSettingsPreservesFilterAndSelection()
    {
        var settings = new ContactSettings(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json"), () => null);
        var main = new MainWindowViewModel(new DirectorySource(), new Facts(), settings, new Links(), new Backend());
        main.ShowSearchCommand.Execute(null);
        var search = Assert.IsType<SearchViewModel>(main.CurrentPage); await search.EnsureLoadedAsync();
        main.Comparison.Toggle(Selection[0]); main.Comparison.Toggle(Selection[1]);
        search.CompareCommand.Execute(null);
        var analysis = Assert.IsType<FinancialAnalysisViewModel>(main.CurrentPage);
        Assert.Same(main.Analysis, analysis); Assert.False(main.Comparison.IsLoading);
        analysis.SelectedMetric = "Annual operating margin"; analysis.Threshold = "10"; analysis.ApplyFilterCommand.Execute(null);
        analysis.SettingsCommand.Execute(null);
        Assert.True(main.IsSettings);
        Assert.IsType<SettingsViewModel>(main.CurrentPage).BackCommand.Execute(null);
        Assert.Same(analysis, main.CurrentPage); Assert.Equal("10", analysis.Threshold); Assert.Equal(2, main.Comparison.Peers.Count);
        analysis.BackCommand.Execute(null); Assert.IsType<SearchViewModel>(main.CurrentPage);
    }
    private sealed class Browser : IBrowserLauncher { public void Open(Uri uri) => throw new Exception("Must not open"); }
    private sealed class NoNetwork : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; throw new Exception("Must not request"); }
    }
    [Fact]
    public async Task RealFacadeWithoutVerifiedAccessLoadsFundamentalsWithoutNetworkOrBrowser()
    {
        var handler = new NoNetwork(); using var http = new HttpClient(handler);
        var facade = new ComparisonBackend(new Facts(), http, new("https://not-configured.invalid"), http, new Browser());
        var adapter = new ResearchBackend(facade, false); var count = 0;
        await foreach (var snapshot in adapter.WatchAsync(Selection, CancellationToken.None)) { count++; Assert.Equal(2, snapshot.Rows.Count); }
        Assert.Equal(1, count); Assert.Equal(0, handler.Calls);
    }
}
