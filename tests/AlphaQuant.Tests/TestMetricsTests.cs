using AlphaQuant.Core;

namespace AlphaQuant.Tests;

public class TestMetricsTests
{
    private static AnnualFact Fact(string label, decimal value, string unit = "USD") =>
        new(label, "Concept", value, unit, new DateOnly(2024, 10, 1), new DateOnly(2025, 9, 30), "10-K", new DateOnly(2025, 11, 1), "0000000001-25-000001");

    [Fact]
    public void RevenueGrowthIsPercentChange()
    {
        // 110 / 80 - 1 = 0.375, so 37.5%.
        var result = TestMetrics.RevenueGrowth(Fact("Revenue", 110m), Fact("Revenue (prior year)", 80m));

        Assert.Equal(MetricStatus.Available, result.Status);
        Assert.Equal(37.5m, result.Value);
        Assert.Equal("%", result.DisplayUnit);
        Assert.Equal(AnnualAnalyzer.DefinitionVersion, result.DefinitionVersion);
        Assert.Equal(2, result.Inputs.Count);
    }

    [Fact]
    public void RevenueGrowthCanBeNegative()
    {
        var result = TestMetrics.RevenueGrowth(Fact("Revenue", 75m), Fact("Revenue (prior year)", 100m));

        Assert.Equal(-25m, result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void RevenueGrowthIsUnavailableWhenPriorRevenueIsNotPositive(int prior)
    {
        var result = TestMetrics.RevenueGrowth(Fact("Revenue", 100m), Fact("Revenue (prior year)", prior));

        Assert.Equal(MetricStatus.Unavailable, result.Status);
        Assert.Null(result.Value);
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public void RevenueGrowthIsUnavailableWithoutAPriorPeriod()
    {
        var result = TestMetrics.RevenueGrowth(Fact("Revenue", 100m), null);

        Assert.Equal(MetricStatus.Unavailable, result.Status);
        Assert.Contains("prior-year", result.Reason);
    }

    [Fact]
    public void NetMarginIsNetIncomeOverRevenue()
    {
        var result = TestMetrics.NetMargin(Fact("Revenue", 400m), Fact("Net income", 50m));

        Assert.Equal(12.5m, result.Value);
    }

    [Fact]
    public void NetMarginKeepsALoss()
    {
        var result = TestMetrics.NetMargin(Fact("Revenue", 200m), Fact("Net income", -50m));

        Assert.Equal(-25m, result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NetMarginIsUnavailableWhenRevenueIsNotPositive(int revenue)
    {
        var result = TestMetrics.NetMargin(Fact("Revenue", revenue), Fact("Net income", 5m));

        Assert.Equal(MetricStatus.Unavailable, result.Status);
    }

    [Fact]
    public void NetMarginIsUnavailableWithoutNetIncome()
    {
        var result = TestMetrics.NetMargin(Fact("Revenue", 100m), null);

        Assert.Equal(MetricStatus.Unavailable, result.Status);
        Assert.Contains("Net income", result.Reason);
    }

    [Fact]
    public void CurrentRatioIsAssetsOverLiabilities()
    {
        var result = TestMetrics.CurrentRatio(Fact("Current assets", 150m), Fact("Current liabilities", 100m));

        Assert.Equal(1.5m, result.Value);
        Assert.Equal("x", result.DisplayUnit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CurrentRatioIsUnavailableWhenLiabilitiesAreNotPositive(int liabilities)
    {
        var result = TestMetrics.CurrentRatio(Fact("Current assets", 150m), Fact("Current liabilities", liabilities));

        Assert.Equal(MetricStatus.Unavailable, result.Status);
    }

    [Fact]
    public void CurrentRatioNamesWhichInputIsMissing()
    {
        var noAssets = TestMetrics.CurrentRatio(null, Fact("Current liabilities", 100m));
        var noLiabilities = TestMetrics.CurrentRatio(Fact("Current assets", 100m), null);

        Assert.Contains("Current assets", noAssets.Reason);
        Assert.Contains("Current liabilities", noLiabilities.Reason);
        Assert.All([noAssets, noLiabilities], r => Assert.Null(r.Value));
    }

    [Fact]
    public void LargeValuesKeepFullDecimalPrecision()
    {
        // 416,161,000,000 / 391,035,000,000 is not exactly representable as a double-rounded percent.
        var result = TestMetrics.RevenueGrowth(
            Fact("Revenue", 416_161_000_000m),
            Fact("Revenue (prior year)", 391_035_000_000m));

        Assert.Equal(6.4, Math.Round((double)result.Value!.Value, 1));
        Assert.True(result.Value!.Value > 6.425m && result.Value!.Value < 6.426m);
    }

    [Fact]
    public void OverflowBecomesUnavailableInsteadOfThrowing()
    {
        var result = TestMetrics.RevenueGrowth(Fact("Revenue", decimal.MaxValue), Fact("Revenue (prior year)", 1m));

        Assert.Equal(MetricStatus.Unavailable, result.Status);
        Assert.Contains("range", result.Reason);
    }

    [Fact]
    public void UnsupportedReturnsAllThreeMetricsWithTheReason()
    {
        var results = TestMetrics.Unsupported("reason");

        Assert.Equal(3, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal(MetricStatus.Unsupported, r.Status);
            Assert.Equal("reason", r.Reason);
            Assert.Null(r.Value);
        });
    }
}
