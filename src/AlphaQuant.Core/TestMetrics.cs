using System.Globalization;

namespace AlphaQuant.Core;

/// <summary>
/// Test definitions (version <see cref="AnnualAnalyzer.DefinitionVersion"/>) used to exercise the pipeline.
/// They are not approved v1 metric definitions. Values are decimals and are not rounded here.
/// </summary>
public static class TestMetrics
{
    public const string RevenueGrowthName = "Annual revenue growth";
    public const string NetMarginName = "Annual net profit margin";
    public const string CurrentRatioName = "Current ratio";

    private const string Percent = "%";
    private const string Times = "x";

    /// <summary>(revenue in year t / revenue in year t-1 - 1) x 100; the prior value must be positive.</summary>
    public static MetricResult RevenueGrowth(AnnualFact current, AnnualFact? prior)
    {
        if (prior is null)
        {
            return Unavailable(RevenueGrowthName, "No comparable prior-year revenue period was found.", [Describe(current)]);
        }

        var inputs = new[] { Describe(current), Describe(prior) };
        if (prior.Value <= 0)
        {
            return Unavailable(RevenueGrowthName, "Prior-year revenue is zero or negative.", inputs);
        }

        return Compute(RevenueGrowthName, Percent, inputs, () => (current.Value / prior.Value - 1m) * 100m);
    }

    /// <summary>Net income / revenue x 100; revenue must be positive.</summary>
    public static MetricResult NetMargin(AnnualFact revenue, AnnualFact? netIncome)
    {
        if (netIncome is null)
        {
            return Unavailable(NetMarginName, "Net income was not found for the revenue period.", [Describe(revenue)]);
        }

        var inputs = new[] { Describe(netIncome), Describe(revenue) };
        if (revenue.Value <= 0)
        {
            return Unavailable(NetMarginName, "Revenue is zero or negative.", inputs);
        }

        return Compute(NetMarginName, Percent, inputs, () => netIncome.Value / revenue.Value * 100m);
    }

    /// <summary>Current assets / current liabilities at the same balance-sheet date; liabilities must be positive.</summary>
    public static MetricResult CurrentRatio(AnnualFact? currentAssets, AnnualFact? currentLiabilities)
    {
        var inputs = new[] { currentAssets, currentLiabilities }
            .Where(f => f is not null)
            .Select(f => Describe(f!))
            .ToList();

        if (currentAssets is null || currentLiabilities is null)
        {
            var missing = currentAssets is null ? "Current assets" : "Current liabilities";
            return Unavailable(CurrentRatioName, $"{missing} not found at the balance-sheet date.", inputs);
        }

        if (currentLiabilities.Value <= 0)
        {
            return Unavailable(CurrentRatioName, "Current liabilities are zero or negative.", inputs);
        }

        return Compute(CurrentRatioName, Times, inputs, () => currentAssets.Value / currentLiabilities.Value);
    }

    public static IReadOnlyList<MetricResult> Unsupported(string reason) =>
    [
        UnsupportedMetric(RevenueGrowthName, reason),
        UnsupportedMetric(NetMarginName, reason),
        UnsupportedMetric(CurrentRatioName, reason),
    ];

    public static string Describe(AnnualFact fact) =>
        $"{fact.Label} ({PeriodText(fact)}): {fact.Value.ToString("0.##", CultureInfo.InvariantCulture)} {fact.Unit}";

    public static string PeriodText(AnnualFact fact) =>
        fact.Start is { } start
            ? $"{start:yyyy-MM-dd} to {fact.End:yyyy-MM-dd}"
            : $"at {fact.End:yyyy-MM-dd}";

    private static MetricResult Compute(string name, string unit, IReadOnlyList<string> inputs, Func<decimal> calculate)
    {
        try
        {
            return new MetricResult(
                name,
                AnnualAnalyzer.DefinitionVersion,
                MetricStatus.Available,
                calculate(),
                unit,
                null,
                inputs);
        }
        catch (OverflowException)
        {
            return Unavailable(name, "The calculation exceeded the supported numeric range.", inputs);
        }
    }

    private static MetricResult Unavailable(string name, string reason, IReadOnlyList<string> inputs) =>
        new(name, AnnualAnalyzer.DefinitionVersion, MetricStatus.Unavailable, null, null, reason, inputs);

    private static MetricResult UnsupportedMetric(string name, string reason) =>
        new(name, AnnualAnalyzer.DefinitionVersion, MetricStatus.Unsupported, null, null, reason, []);
}
