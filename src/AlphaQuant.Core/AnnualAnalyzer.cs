namespace AlphaQuant.Core;

public sealed record AnnualAnalysis(
    IReadOnlyList<AnnualFact> Facts,
    IReadOnlyList<MetricResult> Metrics,
    IReadOnlyList<string> Warnings,
    string? UnsupportedReason);

/// <summary>
/// Selects annual facts and computes the test metrics (definition version test-0, not approved v1 definitions).
/// Periods are chosen from reported start and end dates only; the fy and fp fields describe the filing, not the period.
/// </summary>
public static class AnnualAnalyzer
{
    public const string DefinitionVersion = "test-0";

    private const int MinAnnualDays = 350;
    private const int MaxAnnualDays = 380;
    private const int TypicalYearDays = 365;
    private const string Usd = "USD";

    private sealed record Resolved(RawFact Fact, bool DiffersAcrossFilings);

    public static AnnualAnalysis Analyze(IReadOnlyList<RawFact> facts)
    {
        var eligible = facts.Where(f => IsAnnualForm(f.Form)).ToList();

        var revenueAll = eligible.Where(f => ConceptMap.Revenue.Contains(f.Concept)).ToList();
        if (revenueAll.Count == 0)
        {
            return Unsupported("No annual (10-K) revenue facts were found in the SEC data.");
        }

        var revenueUsd = revenueAll.Where(f => f.Unit == Usd).ToList();
        if (revenueUsd.Count == 0)
        {
            var units = string.Join(", ", revenueAll.Select(f => f.Unit).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            return Unsupported($"Revenue is not reported in USD (reported units: {units}).");
        }

        var annualRevenue = revenueUsd.Where(IsAnnualDuration).ToList();
        if (annualRevenue.Count == 0)
        {
            return Unsupported($"No annual-duration ({MinAnnualDays} to {MaxAnnualDays} day) revenue facts were found.");
        }

        var periods = annualRevenue
            .GroupBy(f => (Start: f.Start!.Value, f.End))
            .Select(g => Resolve(g, ConceptMap.Revenue)!)
            .ToList();

        var latest = periods
            .OrderByDescending(p => p.Fact.End)
            .ThenBy(p => Math.Abs(DurationDays(p.Fact) - TypicalYearDays))
            .First();

        var prior = periods
            .Where(p => GapDays(latest.Fact, p.Fact) is >= MinAnnualDays and <= MaxAnnualDays)
            .OrderBy(p => Math.Abs(GapDays(latest.Fact, p.Fact) - TypicalYearDays))
            .FirstOrDefault();

        var netIncome = Resolve(
            eligible.Where(f => f.Unit == Usd
                && f.Start == latest.Fact.Start
                && f.End == latest.Fact.End
                && ConceptMap.NetIncome.Contains(f.Concept)),
            ConceptMap.NetIncome);

        var assets = Instant(eligible, ConceptMap.AssetsCurrent, latest.Fact.End);
        var liabilities = Instant(eligible, ConceptMap.LiabilitiesCurrent, latest.Fact.End);

        var warnings = new List<string>();
        var revenueFact = ToFact("Revenue", latest, warnings);
        var priorFact = prior is null ? null : ToFact("Revenue (prior year)", prior, warnings);
        var netIncomeFact = netIncome is null ? null : ToFact(NetIncomeLabel(netIncome.Fact), netIncome, warnings);
        var assetsFact = assets is null ? null : ToFact("Current assets", assets, warnings);
        var liabilitiesFact = liabilities is null ? null : ToFact("Current liabilities", liabilities, warnings);

        if (netIncome is { Fact.Concept: not "ProfitLoss" })
        {
            warnings.Add("Net income is the amount attributable to the parent; the consolidated figure (ProfitLoss) was not reported for this period.");
        }

        if (priorFact is not null && priorFact.Concept != revenueFact.Concept)
        {
            warnings.Add($"Revenue uses different tags in the two periods ({priorFact.Concept} and {revenueFact.Concept}); comparability is not verified.");
        }

        var shown = new[] { revenueFact, priorFact, netIncomeFact, assetsFact, liabilitiesFact }
            .Where(f => f is not null)
            .Select(f => f!)
            .ToList();

        var metrics = new[]
        {
            TestMetrics.RevenueGrowth(revenueFact, priorFact),
            TestMetrics.NetMargin(revenueFact, netIncomeFact),
            TestMetrics.CurrentRatio(assetsFact, liabilitiesFact),
        };

        return new AnnualAnalysis(shown, metrics, warnings, null);
    }

    private static AnnualAnalysis Unsupported(string reason) =>
        new([], TestMetrics.Unsupported(reason), [], reason);

    private static bool IsAnnualForm(string form) =>
        form is "10-K" or "10-K/A";

    private static bool IsAnnualDuration(RawFact fact) =>
        fact.Start is not null && DurationDays(fact) is >= MinAnnualDays and <= MaxAnnualDays;

    private static int DurationDays(RawFact fact) =>
        fact.End.DayNumber - fact.Start!.Value.DayNumber;

    private static int GapDays(RawFact later, RawFact earlier) =>
        later.End.DayNumber - earlier.End.DayNumber;

    private static Resolved? Instant(IEnumerable<RawFact> eligible, string concept, DateOnly end) =>
        Resolve(
            eligible.Where(f => f.Unit == Usd && f.Start is null && f.End == end && f.Concept == concept),
            [concept]);

    /// <summary>
    /// For one period, uses the first tag in order that has a value, then the most recently filed fact for it.
    /// Flags when other filings report a different value for the same period.
    /// </summary>
    private static Resolved? Resolve(IEnumerable<RawFact> facts, IReadOnlyList<string> tagOrder)
    {
        var list = facts.ToList();
        foreach (var tag in tagOrder)
        {
            var candidates = list.Where(f => f.Concept == tag).ToList();
            if (candidates.Count == 0)
            {
                continue;
            }

            var pick = candidates
                .OrderByDescending(f => f.Filed)
                .ThenByDescending(f => f.Accession, StringComparer.Ordinal)
                .First();
            return new Resolved(pick, candidates.Any(c => c.Value != pick.Value));
        }

        return null;
    }

    private static AnnualFact ToFact(string label, Resolved resolved, List<string> warnings)
    {
        var f = resolved.Fact;
        var fact = new AnnualFact(label, f.Concept, f.Value, f.Unit, f.Start, f.End, f.Form, f.Filed, f.Accession);
        if (resolved.DiffersAcrossFilings)
        {
            warnings.Add(
                $"{label} ({TestMetrics.PeriodText(fact)}) differs across filings (restated or amended); "
                + $"using the most recently filed value (accession {f.Accession}, filed {f.Filed:yyyy-MM-dd}).");
        }

        return fact;
    }

    private static string NetIncomeLabel(RawFact fact) =>
        fact.Concept == "ProfitLoss" ? "Net income (consolidated)" : "Net income (attributable to parent)";
}
