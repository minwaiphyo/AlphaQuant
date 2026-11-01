using AlphaQuant.Core;

namespace AlphaQuant.Tests;

public sealed class ComparisonBackendTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static RawFact Fact(string tag, decimal value, string start, string end, string form = "10-Q", string unit = "USD",
        string filed = "2026-10-20", string accession = "fixture") =>
        new(tag, unit, value, DateOnly.Parse(start), DateOnly.Parse(end), form, DateOnly.Parse(filed), accession);

    internal static List<RawFact> ReviewedSample() =>
    [
        Fact("Revenues", 1200m, "2025-01-01", "2025-12-31", "10-K", filed: "2026-02-01"),
        Fact("Revenues", 1000m, "2026-01-01", "2026-09-30"),
        Fact("Revenues", 850m, "2025-01-01", "2025-09-30", filed: "2025-10-20"),
        Fact(ComparisonConcepts.OperatingIncome, 240m, "2025-01-01", "2025-12-31", "10-K"),
        Fact(ComparisonConcepts.OperatingIncome, 200m, "2026-01-01", "2026-09-30"),
        Fact(ComparisonConcepts.OperatingIncome, 170m, "2025-01-01", "2025-09-30"),
        Fact(ComparisonConcepts.OperatingCashFlow, 300m, "2025-01-01", "2025-12-31", "10-K"),
        Fact(ComparisonConcepts.OperatingCashFlow, 280m, "2026-01-01", "2026-09-30"),
        Fact(ComparisonConcepts.OperatingCashFlow, 220m, "2025-01-01", "2025-09-30"),
        Fact(ComparisonConcepts.DilutedEps, .75m, "2025-10-01", "2025-12-31", "10-K", "USD/shares"),
        Fact(ComparisonConcepts.DilutedEps, .75m, "2026-01-01", "2026-03-31", unit: "USD/shares"),
        Fact(ComparisonConcepts.DilutedEps, .75m, "2026-04-01", "2026-06-30", unit: "USD/shares"),
        Fact(ComparisonConcepts.DilutedEps, .75m, "2026-07-01", "2026-09-30", unit: "USD/shares")
    ];

    [Fact]
    public void ReviewedYtdBridgeMatchesDiscreteQuarterCrossCheckAndPreservesNegativeContribution()
    {
        var result = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        Assert.Equal(1350m, result.TtmRevenue.Value);
        Assert.Equal(350m + 300m + 350m + 350m, result.TtmRevenue.Value);
        Assert.Equal(270m, result.TtmOperatingIncome.Value);
        Assert.Equal(20m, result.TtmOperatingMargin.Value);
        Assert.Equal(360m, result.TtmOperatingCashFlow.Value);
        Assert.Equal(3m, result.TtmDilutedEps.Value);
        Assert.Equal(new DateOnly(2025, 10, 1), result.TtmRevenue.Start);
        Assert.Single(result.TtmRevenue.Inputs, input => input.Multiplier == -1m);
    }

    [Fact]
    public void MissingPriorYtdDoesNotUseOlderAnnualValueAsCurrentTtm()
    {
        var facts = ReviewedSample();
        facts.RemoveAll(f => f.Concept == "Revenues" && f.End == new DateOnly(2025, 9, 30));
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Null(result.TtmRevenue.Value);
        Assert.Single(result.History);
        Assert.Null(result.TtmOperatingMargin.Value);
    }

    [Fact]
    public void EpsNeverUsesAnnualPlusYtdSubtraction()
    {
        var facts = ReviewedSample().Where(f => f.Concept != ComparisonConcepts.DilutedEps).ToList();
        facts.Add(Fact(ComparisonConcepts.DilutedEps, 3m, "2025-01-01", "2025-12-31", "10-K", "USD/shares"));
        facts.Add(Fact(ComparisonConcepts.DilutedEps, 2m, "2026-01-01", "2026-09-30", unit: "USD/shares"));
        facts.Add(Fact(ComparisonConcepts.DilutedEps, 1m, "2025-01-01", "2025-09-30", unit: "USD/shares"));
        Assert.Null(ComparisonAnalyzer.Analyze(facts, Today).TtmDilutedEps.Value);
    }

    [Fact]
    public void SameDateConflictingRestatementIsNotArbitrarilySelected()
    {
        var facts = ReviewedSample();
        facts.Add(facts[1] with { Value = 9999m, Accession = "other" });
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Null(result.TtmRevenue.Value);
        Assert.Contains(result.Warnings, w => w.Contains("Conflicting"));
    }

    [Fact]
    public void LaterFiledAmendmentWinsAndIsDisclosed()
    {
        var facts = ReviewedSample();
        facts.Add(facts[1] with { Value = 1010m, Filed = Today, Accession = "amended", Form = "10-Q/A" });
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Equal(1360m, result.TtmRevenue.Value);
        Assert.Contains(result.Warnings, w => w.Contains("amended"));
    }

    [Fact]
    public void FutureFilingsCannotChangeTodaysResearch()
    {
        var facts = ReviewedSample();
        facts.Add(facts[1] with { Value = 9999m, Filed = Today.AddDays(1) });
        Assert.Equal(1350m, ComparisonAnalyzer.Analyze(facts, Today).TtmRevenue.Value);
    }

    [Fact]
    public void CurrencyAndPeriodMismatchDoNotBecomeZeroOrMargin()
    {
        var facts = ReviewedSample();
        facts = facts.Select(f => f.Concept == ComparisonConcepts.OperatingIncome ? f with { Unit = "EUR" } : f).ToList();
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Null(result.TtmOperatingIncome.Value);
        Assert.Null(result.TtmOperatingMargin.Value);
    }

    [Fact]
    public void MissingCapitalAndSplitReviewLeavesValuationUnavailableButFundamentalsUsable()
    {
        var financials = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        var now = new DateTimeOffset(2026, 10, 28, 14, 0, 0, TimeSpan.Zero);
        var result = ComparisonValuation.Calculate(new(1, "TEST", "Test"), financials,
            new("TEST", 60, "USD", "alpaca", "iex", now, now, "1"), now);
        Assert.Null(result.PriceEarnings.Value);
        Assert.Null(result.EnterpriseValueSales.Value);
        Assert.Equal(1350m, financials.TtmRevenue.Value);
    }

    [Fact]
    public void WorkedValuationAndSplitInvariantReconcile()
    {
        var company = new CompanyRef(1, "TEST", "Test");
        var financials = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        var now = new DateTimeOffset(2026, 10, 28, 14, 0, 0, TimeSpan.Zero);
        var price = new MarketPrice("TEST", 60m, "USD", "alpaca", "iex", now, now, "1");
        var capital = new CapitalStructureEvidence(1, "TEST", Today, Today, 100m, 400m, 100m, 0m, 0m,
            "synthetic-filing", "reviewed-fixture", Today, true, true);
        var basis = new EquityBasisEvidence(1, "TEST", Today, "reviewed-fixture", true);
        var result = ComparisonValuation.Calculate(company, financials, price, now, capital, basis);
        Assert.Equal(20m, result.PriceEarnings.Value);
        Assert.Equal(6300m, result.EnterpriseValue);
        Assert.Equal(6300m / 1350m, result.EnterpriseValueSales.Value);
        var split = ComparisonValuation.Calculate(company, financials with
            { TtmDilutedEps = financials.TtmDilutedEps with { Value = 1.5m } }, price with { Price = 30m }, now,
            capital with { OutstandingShares = 200m }, basis);
        Assert.Equal(result.EnterpriseValue, split.EnterpriseValue);
        Assert.Equal(result.PriceEarnings.Value, split.PriceEarnings.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void NonPositiveEarningsAreNotCheapRankings(int eps)
    {
        var f = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        f = f with { TtmDilutedEps = f.TtmDilutedEps with { Value = eps } };
        var now = DateTimeOffset.Parse("2026-10-28T14:00:00Z");
        var r = ComparisonValuation.Calculate(new(1, "TEST", "Test"), f,
            new("TEST", 60, "USD", "alpaca", "iex", now, now, "1"), now,
            basis: new(1, "TEST", Today, "review", true));
        Assert.Null(r.PriceEarnings.Value);
    }

    [Fact]
    public void OldPriceAndMismatchedIdentityCannotDriveValuation()
    {
        var f = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        var now = DateTimeOffset.Parse("2026-10-28T14:00:00Z");
        foreach (var price in new[]
        {
            new MarketPrice("TEST", 60, "USD", "alpaca", "iex", now.AddMinutes(-2), now, "1"),
            new MarketPrice("OTHER", 60, "USD", "alpaca", "iex", now, now, "1")
        }) Assert.Null(ComparisonValuation.Calculate(new(1, "TEST", "Test"), f, price, now,
            basis: new(1, "TEST", Today, "review", true)).PriceEarnings.Value);
    }

    [Fact]
    public void FiveDisplayedAnnualGrowthValuesUseSixDistinctYears()
    {
        var facts = new List<RawFact>();
        for (var year = 2020; year <= 2025; year++)
            facts.Add(Fact("Revenues", 100m * (year - 2019), $"{year}-01-01", $"{year}-12-31", "10-K"));
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Equal(5, result.History.Count);
        Assert.Equal(20m, result.History[0].RevenueGrowth.Value);
        Assert.Equal(100m, result.History[^1].RevenueGrowth.Value);
        Assert.All(result.History, year => Assert.NotNull(year.RevenueGrowth.Value));
    }

    [Fact]
    public void UnsupportedIssuerScopeCannotBeApprovedByHavingNumbersAlone()
    {
        var f = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        var now = DateTimeOffset.Parse("2026-10-28T14:00:00Z");
        var r = ComparisonValuation.Calculate(new(1, "TEST", "Test"), f,
            new("TEST", 60, "USD", "alpaca", "iex", now, now, "1"), now,
            basis: new(1, "TEST", Today, "review"));
        Assert.Null(r.PriceEarnings.Value);
    }

    [Fact]
    public void OldCapitalStructureIsUnavailableEvenWhenPriceIsFresh()
    {
        var f = ComparisonAnalyzer.Analyze(ReviewedSample(), Today);
        var now = DateTimeOffset.Parse("2026-10-28T14:00:00Z");
        var old = Today.AddDays(-136);
        var capital = new CapitalStructureEvidence(1, "TEST", old, old, 100, 400, 100, 0, 0,
            "fixture-filing", "review", Today, true, true);
        Assert.Null(ComparisonValuation.Calculate(new(1, "TEST", "Test"), f,
            new("TEST", 60, "USD", "alpaca", "iex", now, now, "1"), now, capital).EnterpriseValueSales.Value);
    }

    [Fact]
    public void OverflowIsReportedPerMetricWithoutThrowingOrRounding()
    {
        var facts = ReviewedSample();
        facts[0] = facts[0] with { Value = decimal.MaxValue };
        Assert.Null(ComparisonAnalyzer.Analyze(facts, Today).TtmRevenue.Value);
    }

    [Fact]
    public void AmbiguousAnnualStartDatesAreNotSelectedByApiOrder()
    {
        var facts = new[]
        {
            Fact("Revenues", 1200m, "2025-01-01", "2025-12-31", "10-K"),
            Fact("Revenues", 1300m, "2024-12-25", "2025-12-31", "10-K")
        };
        var result = ComparisonAnalyzer.Analyze(facts, Today);
        Assert.Empty(result.History);
        Assert.Null(result.TtmRevenue.Value);
        Assert.Contains(result.Warnings, w => w.Contains("Ambiguous"));
    }
}
