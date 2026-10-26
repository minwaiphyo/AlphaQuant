using AlphaQuant.Core;

namespace AlphaQuant.Tests;

public class AnnualAnalyzerTests
{
    private const string Rev = "RevenueFromContractWithCustomerExcludingAssessedTax";

    private static RawFact Dur(
        string concept,
        decimal value,
        string start,
        string end,
        string form = "10-K",
        string filed = "2025-11-01",
        string accession = "0000000001-25-000001",
        string unit = "USD") =>
        new(concept, unit, value, DateOnly.Parse(start), DateOnly.Parse(end), form, DateOnly.Parse(filed), accession);

    private static RawFact Inst(
        string concept,
        decimal value,
        string end,
        string form = "10-K",
        string filed = "2025-11-01",
        string accession = "0000000001-25-000001",
        string unit = "USD") =>
        new(concept, unit, value, null, DateOnly.Parse(end), form, DateOnly.Parse(filed), accession);

    // Current period 2024-10-01 to 2025-09-30 (364 days); prior 2023-10-01 to 2024-09-30 (365 days).
    private static List<RawFact> Healthy() =>
    [
        Dur(Rev, 500m, "2024-10-01", "2025-09-30"),
        Dur(Rev, 400m, "2023-10-01", "2024-09-30", filed: "2024-11-01", accession: "0000000001-24-000001"),
        Dur("ProfitLoss", 50m, "2024-10-01", "2025-09-30"),
        Inst("AssetsCurrent", 300m, "2025-09-30"),
        Inst("LiabilitiesCurrent", 200m, "2025-09-30"),
    ];

    private static MetricResult Metric(AnnualAnalysis analysis, string name) =>
        Assert.Single(analysis.Metrics, m => m.Name == name);

    [Fact]
    public void ComputesAllThreeMetricsFromAnnualFacts()
    {
        var analysis = AnnualAnalyzer.Analyze(Healthy());

        Assert.Null(analysis.UnsupportedReason);
        Assert.Equal(5, analysis.Facts.Count);
        Assert.Equal(25m, Metric(analysis, TestMetrics.RevenueGrowthName).Value);
        Assert.Equal(10m, Metric(analysis, TestMetrics.NetMarginName).Value);
        Assert.Equal(1.5m, Metric(analysis, TestMetrics.CurrentRatioName).Value);
        Assert.Empty(analysis.Warnings);
    }

    [Fact]
    public void FactsCarryProvenance()
    {
        var revenue = AnnualAnalyzer.Analyze(Healthy()).Facts.First(f => f.Label == "Revenue");

        Assert.Equal(Rev, revenue.Concept);
        Assert.Equal("10-K", revenue.Form);
        Assert.Equal(new DateOnly(2025, 11, 1), revenue.Filed);
        Assert.Equal("0000000001-25-000001", revenue.Accession);
        Assert.Equal(new DateOnly(2024, 10, 1), revenue.Start);
        Assert.Equal(new DateOnly(2025, 9, 30), revenue.End);
    }

    [Fact]
    public void AcceptsA371DayFiftyThreeWeekYear()
    {
        var facts = new List<RawFact>
        {
            Dur(Rev, 229m, "2016-09-25", "2017-09-30"),
            Dur(Rev, 200m, "2015-09-27", "2016-09-24", filed: "2016-10-26", accession: "0000000001-16-000001"),
        };

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(229m / 200m * 100m - 100m, Metric(analysis, TestMetrics.RevenueGrowthName).Value);
    }

    [Fact]
    public void IgnoresQuarterlyAndYearToDateFactsAndNonAnnualForms()
    {
        var facts = Healthy();
        facts.Add(Dur(Rev, 130m, "2025-10-01", "2025-12-31", form: "10-Q", filed: "2026-02-01"));
        facts.Add(Dur(Rev, 70m, "2025-10-01", "2025-12-31", form: "10-K"));
        facts.Add(Dur(Rev, 260m, "2024-10-01", "2025-03-31", form: "10-K"));
        facts.Add(Inst("AssetsCurrent", 999m, "2025-12-31", form: "10-Q", filed: "2026-02-01"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(new DateOnly(2025, 9, 30), analysis.Facts.First(f => f.Label == "Revenue").End);
        Assert.Equal(25m, Metric(analysis, TestMetrics.RevenueGrowthName).Value);
        Assert.Equal(300m, analysis.Facts.First(f => f.Label == "Current assets").Value);
    }

    [Fact]
    public void DeduplicatesComparativesRepeatedInLaterFilingsWithoutWarning()
    {
        var facts = Healthy();
        facts.Add(Dur(Rev, 400m, "2023-10-01", "2024-09-30", filed: "2025-11-01", accession: "0000000001-25-000001"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        var prior = analysis.Facts.First(f => f.Label == "Revenue (prior year)");
        Assert.Equal("0000000001-25-000001", prior.Accession);
        Assert.Empty(analysis.Warnings);
    }

    [Fact]
    public void LaterAmendmentWithADifferentValueWinsAndWarns()
    {
        var facts = Healthy();
        facts.Add(Dur(Rev, 480m, "2024-10-01", "2025-09-30", form: "10-K/A", filed: "2025-12-15", accession: "0000000001-25-000999"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        var revenue = analysis.Facts.First(f => f.Label == "Revenue");
        Assert.Equal(480m, revenue.Value);
        Assert.Equal("10-K/A", revenue.Form);
        Assert.Contains(analysis.Warnings, w => w.Contains("differs across filings") && w.Contains("0000000001-25-000999"));
        Assert.Equal(20m, Metric(analysis, TestMetrics.RevenueGrowthName).Value);
    }

    [Fact]
    public void BalanceSheetFactsMustMatchTheRevenuePeriodEnd()
    {
        var facts = Healthy();
        facts.RemoveAll(f => f.Concept == "LiabilitiesCurrent");
        facts.Add(Inst("LiabilitiesCurrent", 150m, "2024-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        var ratio = Metric(analysis, TestMetrics.CurrentRatioName);
        Assert.Equal(MetricStatus.Unavailable, ratio.Status);
        Assert.Contains("Current liabilities", ratio.Reason);
        Assert.DoesNotContain(analysis.Facts, f => f.Label == "Current liabilities");
    }

    [Fact]
    public void NetIncomeMustCoverTheSameStartAndEndAsRevenue()
    {
        var facts = Healthy();
        facts.RemoveAll(f => f.Concept == "ProfitLoss");
        facts.Add(Dur("ProfitLoss", 40m, "2023-10-01", "2024-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        var margin = Metric(analysis, TestMetrics.NetMarginName);
        Assert.Equal(MetricStatus.Unavailable, margin.Status);
        Assert.Null(margin.Value);
    }

    [Fact]
    public void PrefersConsolidatedProfitOverProfitAttributableToParent()
    {
        var facts = Healthy();
        facts.Add(Dur("NetIncomeLoss", 45m, "2024-10-01", "2025-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(10m, Metric(analysis, TestMetrics.NetMarginName).Value);
        Assert.Contains(analysis.Facts, f => f.Label == "Net income (consolidated)");
        Assert.Empty(analysis.Warnings);
    }

    [Fact]
    public void FallsBackToParentProfitWithADisclosure()
    {
        var facts = Healthy();
        facts.RemoveAll(f => f.Concept == "ProfitLoss");
        facts.Add(Dur("NetIncomeLoss", 45m, "2024-10-01", "2025-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(9m, Metric(analysis, TestMetrics.NetMarginName).Value);
        Assert.Contains(analysis.Facts, f => f.Label == "Net income (attributable to parent)");
        Assert.Contains(analysis.Warnings, w => w.Contains("attributable to the parent"));
    }

    [Fact]
    public void UsesTheFirstListedRevenueTagPerPeriodAndNeverSums()
    {
        var facts = Healthy();
        facts.Add(Dur("Revenues", 9_999m, "2024-10-01", "2025-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(500m, analysis.Facts.First(f => f.Label == "Revenue").Value);
    }

    [Fact]
    public void WarnsWhenRevenueTagsDifferBetweenPeriods()
    {
        var facts = Healthy();
        facts.RemoveAll(f => f.Concept == Rev && f.End == new DateOnly(2024, 9, 30));
        facts.Add(Dur("SalesRevenueNet", 400m, "2023-10-01", "2024-09-30"));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(25m, Metric(analysis, TestMetrics.RevenueGrowthName).Value);
        Assert.Contains(analysis.Warnings, w => w.Contains("different tags"));
    }

    [Fact]
    public void MissingPriorYearLeavesGrowthUnavailableButOtherMetricsWork()
    {
        var facts = Healthy();
        facts.RemoveAll(f => f.End == new DateOnly(2024, 9, 30));

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Equal(MetricStatus.Unavailable, Metric(analysis, TestMetrics.RevenueGrowthName).Status);
        Assert.Equal(10m, Metric(analysis, TestMetrics.NetMarginName).Value);
        Assert.Equal(1.5m, Metric(analysis, TestMetrics.CurrentRatioName).Value);
    }

    [Fact]
    public void NonUsdRevenueIsUnsupportedWithTheUnitsInTheReason()
    {
        var facts = new List<RawFact>
        {
            Dur(Rev, 500m, "2024-10-01", "2025-09-30", unit: "EUR"),
        };

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Contains("EUR", analysis.UnsupportedReason);
        Assert.Empty(analysis.Facts);
        Assert.All(analysis.Metrics, m =>
        {
            Assert.Equal(MetricStatus.Unsupported, m.Status);
            Assert.Null(m.Value);
        });
    }

    [Fact]
    public void NoAnnualRevenueFactsIsUnsupported()
    {
        var onlyQuarterly = new List<RawFact>
        {
            Dur(Rev, 130m, "2025-10-01", "2025-12-31", form: "10-Q"),
            Inst("AssetsCurrent", 1m, "2025-09-30"),
        };

        Assert.NotNull(AnnualAnalyzer.Analyze(onlyQuarterly).UnsupportedReason);
        Assert.NotNull(AnnualAnalyzer.Analyze([]).UnsupportedReason);
    }

    [Fact]
    public void OnlyShortDurationRevenueInAnnualFormsIsUnsupported()
    {
        var facts = new List<RawFact> { Dur(Rev, 70m, "2025-10-01", "2025-12-31", form: "10-K") };

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Contains("annual-duration", analysis.UnsupportedReason);
    }

    [Fact]
    public void MissingFactsNeverBecomeZero()
    {
        var facts = new List<RawFact> { Dur(Rev, 500m, "2024-10-01", "2025-09-30") };

        var analysis = AnnualAnalyzer.Analyze(facts);

        Assert.Single(analysis.Facts);
        Assert.All(analysis.Metrics, m =>
        {
            Assert.Equal(MetricStatus.Unavailable, m.Status);
            Assert.Null(m.Value);
        });
    }
}
