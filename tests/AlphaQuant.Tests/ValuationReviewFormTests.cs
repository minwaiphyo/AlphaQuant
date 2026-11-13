using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;

namespace AlphaQuant.Tests;

public sealed class ValuationReviewFormTests
{
    private static readonly CompanyRef A = new(1, "AAA", "Synthetic Alpha");
    private static readonly CompanyRef B = new(2, "BBB", "Synthetic Beta");
    private static readonly DateOnly Today = new(2026, 3, 1);
    private static ValuationReviewViewModel Form(ReviewedEvidenceStore store, Action? invalidate = null,
        Func<IReadOnlyList<CompanyRef>>? companies = null, ILinkOpener? links = null) => new(store, companies ?? (() => [A, B]),
            _ => new([], null, true), links ?? new Links(), invalidate ?? (() => { }), () => Today);
    private static void Fill(ValuationReviewViewModel vm, bool capital = true)
    {
        vm.RefreshCompanies(); vm.SelectedCompany = A;
        vm.ScopeConfirmed = vm.ClassConfirmed = true;
        vm.SplitThrough = "2026-03-01"; vm.FilingAccession = "0000000001-26-000001";
        vm.ReviewReference = "Synthetic source review, common class, cash/debt composition and explicit zero claims";
        vm.IncludeCapital = capital; vm.CapitalClaimsConfirmed = capital;
        vm.SharesDate = vm.BalanceSheetDate = "2026-02-01";
        vm.OutstandingShares = "100"; vm.Debt = "400"; vm.Cash = "100";
        vm.PreferredEquity = vm.NonControllingInterests = "0";
    }
    private static ComparisonFundamentals Financials() => ComparisonAnalyzer.Analyze(
        [new RawFact("Revenues", "USD", 1350m, new(2025, 1, 1), new(2025, 12, 31), "10-K", new(2026, 2, 1), "0000000001-26-000001"),
         new RawFact("EarningsPerShareDiluted", "USD/shares", 3m, new(2025, 1, 1), new(2025, 12, 31), "10-K", new(2026, 2, 1), "0000000001-26-000001")], Today);
    private static ValuationResult Calculate(ReviewedEvidenceStore store, DateTimeOffset? received = null)
    {
        var now = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var price = new MarketPrice("AAA", 60m, "USD", "Synthetic", "IEX", received ?? now, received ?? now, "fixture");
        return ComparisonValuation.Calculate(A, Financials(), price, now, store.Capital.GetValueOrDefault(1), store.Equity.GetValueOrDefault(1));
    }

    [Fact]
    public void Reviewed_explicit_inputs_reproduce_independent_baseline_without_JSON_editing()
    {
        var store = new ReviewedEvidenceStore(); var invalidations = 0;
        var vm = Form(store, () => invalidations++); Fill(vm); vm.ApplyCommand.Execute(null);
        Assert.Single(store.Capital); Assert.Single(store.Equity); Assert.Equal(1, invalidations);
        Assert.Equal("https://www.sec.gov/Archives/edgar/data/1/000000000126000001/", store.Capital[1].FilingReference);
        var result = Calculate(store);
        Assert.Equal(20m, result.PriceEarnings.Value); Assert.Equal(6300m, result.EnterpriseValue);
        Assert.Equal(4.6666666666666666666666666667m, result.EnterpriseValueSales.Value);
        Assert.Contains("Actual outstanding", vm.AppliedSummary); Assert.Contains("No certification", vm.Status);
    }

    [Theory]
    [InlineData("preferred")]
    [InlineData("nci")]
    [InlineData("debt")]
    [InlineData("cash")]
    [InlineData("shares")]
    [InlineData("claims")]
    public void Partial_or_unconfirmed_capital_is_atomic_and_never_becomes_zero(string missing)
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.ApplyCommand.Execute(null);
        var priorEquity = store.Equity[1]; var priorCapital = store.Capital[1];
        switch (missing)
        {
            case "preferred": vm.PreferredEquity = ""; break;
            case "nci": vm.NonControllingInterests = ""; break;
            case "debt": vm.Debt = ""; break;
            case "cash": vm.Cash = ""; break;
            case "shares": vm.OutstandingShares = ""; break;
            case "claims": vm.CapitalClaimsConfirmed = false; break;
        }
        vm.ReviewReference = "This partial replacement must not be applied"; vm.ApplyCommand.Execute(null);
        Assert.Equal(priorEquity, store.Equity[1]); Assert.Equal(priorCapital, store.Capital[1]);
        Assert.Contains("not applied", vm.Status);
    }

    [Theory]
    [InlineData("1,000")]
    [InlineData("100 USD")]
    [InlineData("NaN")]
    [InlineData("-1")]
    [InlineData("99999999999999999999999999999999999999")]
    public void Amounts_are_finite_nonnegative_and_invariant_without_unit_guessing(string text)
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.Cash = text;
        vm.ApplyCommand.Execute(null); Assert.Empty(store.Capital); Assert.Empty(store.Equity);
    }

    [Theory]
    [InlineData("2026-03-02", "2026-02-01", "2026-02-01")]
    [InlineData("2026-02-28", "2026-02-01", "2026-02-01")]
    [InlineData("2026-03-01", "2026-03-02", "2026-02-01")]
    [InlineData("2026-03-01", "2026-02-01", "2026-03-02")]
    [InlineData("2026-03-01", "2025-01-01", "2026-02-01")]
    public void Future_stale_and_split_dates_cannot_enable_valuation(string split, string shares, string balance)
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm);
        vm.SplitThrough = split; vm.SharesDate = shares; vm.BalanceSheetDate = balance;
        vm.ApplyCommand.Execute(null); Assert.Empty(store.Equity); Assert.Empty(store.Capital);
    }

    [Fact]
    public void Scope_and_class_checks_are_explicit_not_inferred_from_source_or_ticker()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm);
        vm.ScopeConfirmed = false; vm.ApplyCommand.Execute(null); Assert.Empty(store.Equity);
        vm.ScopeConfirmed = true; vm.ClassConfirmed = false; vm.ApplyCommand.Execute(null); Assert.Empty(store.Equity);
    }

    [Fact]
    public void Equity_only_replacement_clears_only_this_issuers_capital_review()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.ApplyCommand.Execute(null);
        var first = store.Capital[1]; var equity = store.Equity[1];
        store.ReplaceCompany(equity with { Cik = 2, Symbol = "BBB" }, first with { Cik = 2, Symbol = "BBB" });
        Fill(vm, capital: false); vm.ApplyCommand.Execute(null);
        Assert.True(store.Equity.ContainsKey(1)); Assert.False(store.Capital.ContainsKey(1)); Assert.True(store.Capital.ContainsKey(2));
        Assert.True(Calculate(store).PriceEarnings.Available); Assert.False(Calculate(store).EnterpriseValueSales.Available);
    }

    [Fact]
    public void Identity_conflict_or_removed_peer_preserves_all_previous_reviews()
    {
        var store = new ReviewedEvidenceStore(); IReadOnlyList<CompanyRef> choices = [A, B];
        var vm = Form(store, companies: () => choices); Fill(vm); vm.ApplyCommand.Execute(null);
        var original = store.Capital[1]; choices = [B]; vm.ApplyCommand.Execute(null);
        Assert.Equal(original, store.Capital[1]);
        Assert.Throws<ArgumentException>(() => store.ReplaceCompany(store.Equity[1], original with { Cik = 2 }));
        Assert.Equal(original, store.Capital[1]);
    }

    [Fact]
    public void Clear_draft_and_clear_applied_are_separate_and_other_issuers_survive()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.ApplyCommand.Execute(null);
        store.ReplaceCompany(store.Equity[1] with { Cik = 2, Symbol = "BBB" }, store.Capital[1] with { Cik = 2, Symbol = "BBB" });
        vm.ClearDraftCommand.Execute(null); Assert.True(store.Capital.ContainsKey(1)); Assert.False(vm.ScopeConfirmed); Assert.Equal("", vm.Cash);
        vm.ClearAppliedCommand.Execute(null); Assert.False(store.Capital.ContainsKey(1)); Assert.True(store.Capital.ContainsKey(2));
        Assert.False(Calculate(store).EnterpriseValueSales.Available);
    }

    [Fact]
    public void Fresh_review_does_not_bypass_price_age_or_clamp_negative_enterprise_value()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.Cash = "10000"; vm.ApplyCommand.Execute(null);
        Assert.Equal(-3600m, Calculate(store).EnterpriseValue); // 6000 common equity + 400 debt - 10000 cash.
        var stale = Calculate(store, new DateTimeOffset(2026, 3, 1, 11, 58, 0, TimeSpan.Zero));
        Assert.False(stale.PriceEarnings.Available); Assert.False(stale.EnterpriseValueSales.Available);
    }

    [Fact]
    public void New_session_has_no_reviews_and_filing_open_is_an_explicit_safe_action()
    {
        var store = new ReviewedEvidenceStore(); var links = new Links(); var vm = Form(store, links: links);
        Fill(vm); Assert.Empty(links.Opened); vm.OpenFilingCommand.Execute(null);
        Assert.Equal("www.sec.gov", Assert.Single(links.Opened).Host);
        vm.ApplyCommand.Execute(null);
        Assert.Empty(new ReviewedEvidenceStore().Capital);
        vm.FilingAccession = "https://evil.invalid/"; vm.OpenFilingCommand.Execute(null); Assert.Single(links.Opened);
    }

    [Theory]
    [InlineData(135, true)]
    [InlineData(136, false)]
    public void Capital_age_boundary_matches_existing_135_day_definition(int days, bool accepted)
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm);
        vm.SharesDate = vm.BalanceSheetDate = Today.AddDays(-days).ToString("yyyy-MM-dd");
        vm.ApplyCommand.Execute(null); Assert.Equal(accepted, store.Capital.ContainsKey(1));
    }

    [Fact]
    public void Structured_output_remains_compatible_with_strict_JSON_interchange()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.ApplyCommand.Execute(null);
        var json = System.Text.Json.JsonSerializer.Serialize(new { Version = 1, Equity = store.Equity.Values, Capital = store.Capital.Values });
        var imported = new ReviewedEvidenceStore(); imported.Load(json);
        Assert.Equal(store.Equity[1], imported.Equity[1]); Assert.Equal(store.Capital[1], imported.Capital[1]);
        Assert.Equal(20m, Calculate(imported).PriceEarnings.Value);
    }

    [Fact]
    public void Production_shutdown_clears_applied_reviews_and_unapplied_form_fields()
    {
        var store = new ReviewedEvidenceStore(); var vm = Form(store); Fill(vm); vm.ApplyCommand.Execute(null);
        var main = new MainWindowViewModel(new NoDirectory(), new NoFacts(),
            new AlphaQuant.Infrastructure.ContactSettings(Path.Combine(Path.GetTempPath(), "aq-unused-contact-" + Guid.NewGuid() + ".json"), () => null),
            new Links(), new NoBackend(), store);
        main.Comparison.Toggle(A); main.Comparison.Toggle(B); main.NavigateSettingsCommand.Execute(null);
        var review = Assert.IsType<SettingsViewModel>(main.CurrentPage).Review!;
        review.Cash = "123"; review.ScopeConfirmed = true;
        main.Shutdown();
        Assert.Empty(store.Capital); Assert.Empty(store.Equity); Assert.Equal("", review.Cash); Assert.False(review.ScopeConfirmed);
    }

    [Fact]
    public void Source_cards_keep_subcent_EPS_and_high_precision_cash_without_confirming_claims()
    {
        var fact = new RawFact("EarningsPerShareDiluted", "USD/shares", 0.0046m, new(2025, 1, 1), new(2025, 12, 31),
            "10-K", new(2026, 2, 1), "0000000001-26-000001");
        var store = new ReviewedEvidenceStore();
        var vm = new ValuationReviewViewModel(store, () => [A], _ => new([fact], null, true), new Links(), () => { }, () => Today);
        vm.RefreshCompanies();
        Assert.Equal("0.0046 USD/shares", Assert.Single(vm.SourceFacts).Value);
        Assert.Contains("retrieval date unavailable", vm.SourceStatus);
        Assert.False(vm.ScopeConfirmed); Assert.False(vm.ClassConfirmed); Assert.Equal("", vm.OutstandingShares);
        var cash = new EvidenceFact(new AnnualFact("Cash", "CashAndCashEquivalentsAtCarryingValue", 123.456789m,
            "USD", null, new(2025, 12, 31), "10-K", new(2026, 2, 1), "0000000001-26-000001"), 1, new Links());
        Assert.Equal("123.456789 USD", cash.Value); Assert.Empty(store.Equity);
    }

    private sealed class NoDirectory : ICompanyDirectory
    { public Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken token) => throw new InvalidOperationException("No network"); }
    private sealed class NoFacts : ICompanyFactsSource
    { public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken token) => throw new InvalidOperationException("No network"); }
    private sealed class NoBackend : IResearchBackend
    {
        public bool Configured => false; public bool Authorized => false;
        public void Disconnect() { }
        public Task ConnectAsync(CancellationToken token) => throw new InvalidOperationException("No network");
        public Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token) => throw new InvalidOperationException("No network");
        public IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies, CancellationToken token) => throw new InvalidOperationException("No network");
    }

    private sealed class Links : ILinkOpener
    { public List<Uri> Opened { get; } = []; public void Open(Uri uri) => Opened.Add(uri); }
}
