using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public sealed class ComparisonUiTests
{
    private static readonly CompanyRef Alder = new(1, "ALDR", "Alder Industries");
    private static readonly CompanyRef Birch = new(2, "BRCH", "Birch Systems");
    private static readonly CompanyRef Fern = new(3, "FERN", "Fern Health");

    private sealed class FactsSource : ICompanyFactsSource
    {
        public Func<long, CancellationToken, Task<CompanyFacts>> Get { get; set; } = (cik, _) => Task.FromResult(Facts(cik == 1 ? 120m : 140m));
        public List<long> Calls { get; } = [];
        public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken token) { Calls.Add(cik); return Get(cik, token); }
    }
    private sealed class DirectorySource : ICompanyDirectory
    {
        public Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken token) => Task.FromResult(new CompanyDirectoryResult([Alder, Birch, Fern], DateTimeOffset.UtcNow, false));
    }
    private sealed class Links : ILinkOpener
    {
        public Uri? Opened { get; private set; }
        public void Open(Uri uri) => Opened = uri;
    }
    private static CompanyFacts Facts(decimal revenue = 120m, bool missingPrior = false, bool stale = false)
    {
        var start = new DateOnly(2025, 1, 1); var end = new DateOnly(2025, 12, 31);
        var filed = new DateOnly(2026, 2, 20);
        RawFact Fact(string concept, decimal value, DateOnly? from, DateOnly to) => new(concept, "USD", value, from, to, "10-K", filed, "0000000001-26-000001");
        var facts = new List<RawFact> { Fact("Revenues", revenue, start, end), Fact("ProfitLoss", 24m, start, end), Fact("AssetsCurrent", 200m, null, end), Fact("LiabilitiesCurrent", 100m, null, end) };
        if (!missingPrior) facts.Add(Fact("Revenues", 100m, new(2024, 1, 1), new(2024, 12, 31)));
        return new CompanyFacts("Synthetic", facts, 0, new DateTimeOffset(2026, 2, 21, 12, 0, 0, TimeSpan.Zero), stale);
    }
    private static ComparisonViewModel Workspace(FactsSource? source = null, Links? links = null) => new(source ?? new FactsSource(), links ?? new Links(), () => { }, () => { }, (_, _) => { });

    [Fact]
    public void SelectionIsDeduplicatedByCikAndLimitedToFive()
    {
        var vm = Workspace();
        vm.Toggle(Alder); Assert.False(vm.CanCompare);
        vm.Toggle(Alder with { Ticker = "OTHER" }); Assert.Empty(vm.Peers);
        for (var i = 1; i <= 6; i++) vm.Toggle(new(i, $"C{i}", $"Company {i}"));
        Assert.Equal(5, vm.Peers.Count); Assert.True(vm.CanCompare);
        vm.Peers[0].RemoveCommand.Execute(null); Assert.Equal(4, vm.Peers.Count);
    }

    [Theory]
    [InlineData(">", "30", "BRCH")]
    [InlineData("≥", "40", "BRCH")]
    [InlineData("<", "30", "ALDR")]
    [InlineData("≤", "20", "ALDR")]
    [InlineData("=", "20", "ALDR")]
    public async Task FiltersUseRawMetricValues(string op, string threshold, string ticker)
    {
        var vm = Workspace(); vm.Toggle(Alder); vm.Toggle(Birch); await vm.LoadAsync(false);
        vm.SelectedOperator = vm.Operators.Single(o => o.Symbol == op); vm.Threshold = threshold; vm.AddFilterCommand.Execute(null);
        Assert.Equal(ticker, Assert.Single(vm.Rows).Peer.Ticker);
        vm.Filters[0].RemoveCommand.Execute(null); Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public async Task FiltersCombineAndExcludeUnavailableButKeepStaleNumericValues()
    {
        var source = new FactsSource { Get = (cik, _) => Task.FromResult(Facts(120m, missingPrior: cik == 3, stale: cik == 1)) };
        var vm = Workspace(source); vm.Toggle(Alder); vm.Toggle(Fern); await vm.LoadAsync(false);
        vm.Threshold = "10"; vm.AddFilterCommand.Execute(null);
        Assert.Equal("ALDR", Assert.Single(vm.Rows).Peer.Ticker);
        Assert.Contains("Stale", vm.Rows[0].Status);
        vm.SelectedMetric = vm.Metrics[2]; vm.Threshold = "3"; vm.AddFilterCommand.Execute(null); Assert.Empty(vm.Rows);
        vm.ClearFiltersCommand.Execute(null); Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public async Task FilteringDoesNotUseRoundedDisplayOrAcceptInvalidThreshold()
    {
        var source = new FactsSource { Get = (_, _) => Task.FromResult(Facts(120.004m)) };
        var vm = Workspace(source); vm.Toggle(Alder); await vm.LoadAsync(false);
        vm.SelectedOperator = vm.Operators.Single(o => o.Symbol == "="); vm.Threshold = "20"; vm.AddFilterCommand.Execute(null);
        Assert.Empty(vm.Rows);
        vm.Threshold = "not a number"; vm.AddFilterCommand.Execute(null); Assert.Single(vm.Filters); Assert.NotNull(vm.FilterError);
    }

    [Fact]
    public async Task NumericSortingKeepsUnavailableLastInBothDirections()
    {
        var source = new FactsSource { Get = (cik, _) => Task.FromResult(Facts(cik == 1 ? 120m : 140m, cik == 3)) };
        var vm = Workspace(source); vm.Toggle(Alder); vm.Toggle(Birch); vm.Toggle(Fern); await vm.LoadAsync(false);
        vm.SelectedSort = vm.SortChoices[1]; Assert.Equal(new[] { "ALDR", "BRCH", "FERN" }, vm.Rows.Select(r => r.Peer.Ticker));
        vm.Descending = true; Assert.Equal(new[] { "BRCH", "ALDR", "FERN" }, vm.Rows.Select(r => r.Peer.Ticker));
    }

    [Fact]
    public async Task HeaderCommandsToggleSortAndUnsupportedRowsNeverPassNumericFilters()
    {
        var source = new FactsSource { Get = (cik, _) => Task.FromResult(cik == 3
            ? new CompanyFacts("Unsupported", [], 0, DateTimeOffset.UtcNow, false) : Facts(cik == 1 ? 120m : 140m)) };
        var vm = Workspace(source); vm.Toggle(Alder); vm.Toggle(Birch); vm.Toggle(Fern); await vm.LoadAsync(false);
        Assert.Equal("Unsupported", vm.Rows.Single(r => r.Peer.Ticker == "FERN").Cells[0].DisplayValue);
        vm.Headers[0].SortCommand.Execute(null); Assert.False(vm.Descending);
        vm.Headers[0].SortCommand.Execute(null); Assert.True(vm.Descending);
        Assert.Equal("BRCH", vm.Rows[0].Peer.Ticker); Assert.Equal("FERN", vm.Rows[^1].Peer.Ticker);
        vm.Threshold = "0"; vm.AddFilterCommand.Execute(null); Assert.Equal(2, vm.Rows.Count);
        vm.ShowFacts = true; Assert.Equal(4, vm.Rows[0].Cells.Count); Assert.Single(vm.Filters);
    }

    [Fact]
    public async Task EvidenceContainsActualInputsAndSafeSourceLinks()
    {
        var links = new Links(); var vm = Workspace(links: links); vm.Toggle(Alder); await vm.LoadAsync(false);
        vm.Rows[0].Cells[0].InspectCommand.Execute(null);
        Assert.NotNull(vm.Evidence); Assert.Contains("test-0", vm.Evidence.Detail); Assert.Equal(2, vm.Evidence.Facts.Count);
        Assert.Contains("2024-01-01", vm.Evidence.Facts[1].Period);
        vm.Evidence.Facts[0].OpenFilingCommand.Execute(null); Assert.True(FilingLinks.IsSafeToOpen(links.Opened!));
        vm.CloseEvidenceCommand.Execute(null); Assert.False(vm.HasEvidence);
        vm.ShowFacts = true; vm.Rows[0].Cells[2].InspectCommand.Execute(null); Assert.Equal("Current assets", vm.Evidence!.Title);
        Assert.Single(vm.Evidence.Facts); Assert.Contains("AssetsCurrent", vm.Evidence.Facts[0].Source);
    }

    [Fact]
    public async Task PartialFailureKeepsOtherCompaniesAndFailedReloadRetainsSnapshot()
    {
        var source = new FactsSource(); var vm = Workspace(source); vm.Toggle(Alder); vm.Toggle(Birch); await vm.LoadAsync(false);
        var previous = vm.Peers[0].Report;
        source.Get = (cik, _) => cik == 1 ? throw new DataSourceException(DataSourceErrorKind.Network, "Offline") : Task.FromResult(Facts(150m));
        await vm.LoadAsync(true);
        Assert.Same(previous, vm.Peers[0].Report); Assert.Contains("Previous snapshot retained", vm.Peers[0].LoadStatus);
        Assert.Equal(150m, vm.Peers[1].Report!.Facts.Single(f => f.Label == "Revenue").Value);
    }

    [Fact]
    public async Task CancellationIgnoresLateResponsesAndAllowsNewLoad()
    {
        var pending = new TaskCompletionSource<CompanyFacts>(); var source = new FactsSource { Get = (_, _) => pending.Task };
        var vm = Workspace(source); vm.Toggle(Alder); var oldLoad = vm.LoadAsync(false); vm.CancelLoading();
        Assert.False(vm.IsLoading);
        source.Get = (_, _) => Task.FromResult(Facts(150m)); await vm.LoadAsync(false);
        pending.SetResult(Facts(999m)); await oldLoad;
        Assert.Equal(150m, vm.Peers[0].Report!.Facts.Single(f => f.Label == "Revenue").Value);
    }

    [Fact]
    public async Task RemovingACompanyPreventsInFlightResultsFromReappearing()
    {
        var pending = new TaskCompletionSource<CompanyFacts>(); var source = new FactsSource { Get = (_, _) => pending.Task };
        var vm = Workspace(source); vm.Toggle(Alder); var load = vm.LoadAsync(false); vm.Toggle(Alder);
        pending.SetResult(Facts()); await load; Assert.Empty(vm.Peers); Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task SettingsRetryTargetsOnlyMissingContactFailures()
    {
        var source = new FactsSource { Get = (cik, _) => throw new DataSourceException(cik == 1 ? DataSourceErrorKind.ContactMissing : DataSourceErrorKind.Network, "Setup/error") };
        var vm = Workspace(source); vm.Toggle(Alder); vm.Toggle(Birch); await vm.LoadAsync(false);
        Assert.True(vm.HasSetupIssue);
        source.Calls.Clear(); source.Get = (_, _) => Task.FromResult(Facts());
        await vm.LoadAsync(false, contactOnly: true); Assert.Equal(new long[] { 1 }, source.Calls); Assert.False(vm.HasSetupIssue);
    }

    [Fact]
    public async Task NavigationPreservesSearchSelectionAndFiltersAndReusesDetailReport()
    {
        var source = new FactsSource(); var settings = new ContactSettings(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "settings.json"), () => null);
        var main = new MainWindowViewModel(new DirectorySource(), source, settings, new Links());
        main.ShowSearchCommand.Execute(null); var search = Assert.IsType<SearchViewModel>(main.CurrentPage);
        await search.EnsureLoadedAsync(); search.Query = "aldr"; search.Results[0].ToggleCommand.Execute(null);
        search.Query = "brch"; search.Results[0].ToggleCommand.Execute(null); search.CompareCommand.Execute(null);
        await main.Comparison.EnsureLoadedAsync(); Assert.Equal(2, source.Calls.Count);
        main.Comparison.Threshold = "10"; main.Comparison.AddFilterCommand.Execute(null);
        main.NavigateSettingsCommand.Execute(null); var page = Assert.IsType<SettingsViewModel>(main.CurrentPage); page.BackCommand.Execute(null);
        Assert.Same(main.Comparison, main.CurrentPage); Assert.Single(main.Comparison.Filters);
        main.Comparison.Rows[0].OpenCompanyCommand.Execute(null); Assert.IsType<CompanyDetailViewModel>(main.CurrentPage); Assert.Equal(2, source.Calls.Count);
        main.ShowSearchCommand.Execute(null); Assert.Same(search, main.CurrentPage); Assert.Equal("brch", search.Query); Assert.Equal(2, main.Comparison.Peers.Count);
    }
}
