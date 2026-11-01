namespace AlphaQuant.Core;

public static class ComparisonConcepts
{
    public const string OperatingIncome = "OperatingIncomeLoss";
    public const string OperatingCashFlow = "NetCashProvidedByUsedInOperatingActivities";
    public const string DilutedEps = "EarningsPerShareDiluted";
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        [.. ConceptMap.All, OperatingIncome, OperatingCashFlow, DilutedEps,
            "CashAndCashEquivalentsAtCarryingValue", "LongTermDebtCurrent", "LongTermDebtNoncurrent",
            "ShortTermBorrowings", "CommonStockSharesOutstanding", "StockholdersEquity",
            "MinorityInterest", "PreferredStockValue"], StringComparer.Ordinal);
}

/// <summary>comparison-1: no annual-minus-YTD EPS, calendar fy/fp heuristics, or inferred zero capital claims.</summary>
public static class ComparisonAnalyzer
{
    public const string DefinitionVersion = "comparison-1";

    public static ComparisonFundamentals Analyze(IReadOnlyList<RawFact> facts, DateOnly asOf)
    {
        var warnings = new List<string>();
        var eligible = facts.Where(f => f.Filed <= asOf && f.End <= asOf
            && f.Form is "10-K" or "10-K/A" or "10-Q" or "10-Q/A")
            .Where(f => f.Start is null || f.Start <= f.End).ToList();
        var annualCandidates = eligible.Where(f => ConceptMap.Revenue.Contains(f.Concept) && f.Unit == "USD"
                && Annual(f) && f.Form is "10-K" or "10-K/A")
            .GroupBy(f => (f.Start, f.End)).Select(g => Pick(g, ConceptMap.Revenue, warnings))
            .Where(f => f is not null).Select(f => f!).ToList();
        var annual = new List<RawFact>();
        foreach (var period in annualCandidates.GroupBy(f => f.End).OrderByDescending(g => g.Key))
        {
            if (period.Count() != 1)
            { warnings.Add($"Ambiguous annual start dates ending {period.Key:yyyy-MM-dd}; period unavailable."); continue; }
            annual.Add(period.Single());
            if (annual.Count == 6) break;
        }
        var history = new List<AnnualFundamentals>();
        foreach (var revenue in annual.Take(5))
        {
            var prior = annual.Where(f => f.End < revenue.End && f.End.AddDays(1) == revenue.Start)
                .OrderByDescending(f => f.End).FirstOrDefault();
            var rev = Value("Annual revenue", [new(revenue)], "USD");
            var growth = Missing("Annual revenue growth", "%", "No comparable positive prior annual revenue.");
            if (prior is not null && prior.Value > 0 && prior.Concept == revenue.Concept)
            {
                growth = Calculate("Annual revenue growth", "%", revenue.Start, revenue.End,
                    [new(revenue), new(prior)], () => (revenue.Value / prior.Value - 1m) * 100m);
            }
            var op = Exact(eligible, ComparisonConcepts.OperatingIncome, "USD", revenue.Start, revenue.End, warnings);
            var cf = Exact(eligible, ComparisonConcepts.OperatingCashFlow, "USD", revenue.Start, revenue.End, warnings);
            var opValue = op is null ? Missing("Operating income", "USD", "Compatible operating income is missing.")
                : Value("Operating income", [new(op)], "USD");
            history.Add(new(revenue.Start!.Value, revenue.End, rev, growth,
                Ratio("Annual operating margin", opValue, rev, "%", 100m),
                cf is null ? Missing("Annual operating cash flow", "USD", "Compatible operating cash flow is missing.")
                    : Value("Annual operating cash flow", [new(cf)], "USD")));
        }
        var latestEnd = eligible.Where(f => ConceptMap.Revenue.Contains(f.Concept) && f.Unit == "USD"
                && f.Start is not null && Days(f) >= 60).Select(f => (DateOnly?)f.End).Max();
        var ttmRev = Ttm(eligible, ConceptMap.Revenue, "USD", "TTM revenue", latestEnd, warnings);
        var ttmOp = Ttm(eligible, [ComparisonConcepts.OperatingIncome], "USD", "TTM operating income", latestEnd, warnings);
        var ttmCf = Ttm(eligible, [ComparisonConcepts.OperatingCashFlow], "USD", "TTM operating cash flow", latestEnd, warnings);
        var eps = Ttm(eligible, [ComparisonConcepts.DilutedEps], "USD/shares", "TTM diluted EPS", latestEnd, warnings, additiveYtd: false);
        if (history.Count < 5) warnings.Add("Fewer than five comparable annual periods are available.");
        warnings.Add("Issuer coverage and class-level capital/split eligibility require explicit review; fact availability does not establish supported scope.");
        warnings.Add("Current research uses latest filed facts, not a historical point-in-time reconstruction.");
        var cash = latestEnd is { } balanceDate ? Exact(eligible, "CashAndCashEquivalentsAtCarryingValue", "USD", null, balanceDate, warnings) : null;
        return new(history, ttmRev, ttmOp, Ratio("TTM operating margin", ttmOp, ttmRev, "%", 100m),
            ttmCf, eps, warnings.Distinct().ToArray())
        {
            CashBalance = cash is null || cash.Value < 0 ? Missing("Cash and cash equivalents", "USD", "A valid cash fact at the latest financial period end is missing.")
                : Value("Cash and cash equivalents", [new(cash)], "USD")
        };
    }

    private static FinancialValue Ttm(List<RawFact> facts, IReadOnlyList<string> tags, string unit, string name,
        DateOnly? latest, List<string> warnings, bool additiveYtd = true)
    {
        if (latest is null) return Missing(name, unit, "No latest financial period is available.");
        foreach (var tag in tags)
        {
            var set = facts.Where(f => f.Concept == tag && f.Unit == unit && f.Start is not null)
                .GroupBy(f => (f.Start, f.End)).Select(g => Pick(g, [tag], warnings))
                .Where(f => f is not null).Select(f => f!).ToList();
            var fullPeriods = set.Where(f => f.End == latest && Annual(f) && f.Form is "10-K" or "10-K/A").ToArray();
            if (fullPeriods.Length > 1) return Missing(name, unit, "Conflicting annual start dates require review.");
            var full = fullPeriods.SingleOrDefault();
            if (full is not null) return Value(name, [new(full)], unit);
            // Exactly four directly reported contiguous quarters. EPS never uses an additive YTD bridge.
            var parts = new List<RawFact>();
            var end = latest.Value;
            for (var i = 0; i < 4; i++)
            {
                var quarter = set.Where(f => f.End == end && Days(f) is >= 70 and <= 110).ToArray();
                if (quarter.Length != 1) break;
                parts.Add(quarter[0]); end = quarter[0].Start!.Value.AddDays(-1);
            }
            if (parts.Count == 4 && latest.Value.DayNumber - parts[^1].Start!.Value.DayNumber is >= 349 and <= 379)
                return Value(name, parts.Select(f => new FactContribution(f)).ToArray(), unit);
            if (!additiveYtd) continue;
            var fy = set.Where(f => f.End < latest && Annual(f) && f.Form is "10-K" or "10-K/A")
                .OrderByDescending(f => f.End).FirstOrDefault();
            if (fy is null) continue;
            var current = set.SingleOrDefault(f => f.End == latest && f.Start == fy.End.AddDays(1)
                && Days(f) is >= 60 and <= 300);
            if (current is null) continue;
            var previous = set.Where(f => f.Start == fy.Start && f.End < fy.End
                && Math.Abs(Days(f) - Days(current)) <= 7
                && latest.Value.DayNumber - f.End.DayNumber is >= 349 and <= 379).ToArray();
            if (previous.Length != 1) continue;
            return Calculate(name, unit, previous[0].End.AddDays(1), latest,
                [new(fy), new(current), new(previous[0], -1m)], () => fy.Value + current.Value - previous[0].Value);
        }
        return Missing(name, unit, additiveYtd ? "Compatible full year/YTD bridge or four discrete quarters unavailable."
            : "Four compatible directly reported quarters (or a full year ending at the latest period) are required; EPS is not reconstructed from YTD subtraction.");
    }

    private static RawFact? Exact(List<RawFact> facts, string tag, string unit, DateOnly? start, DateOnly end,
        List<string> warnings) => Pick(facts.Where(f => f.Concept == tag && f.Unit == unit
            && f.Start == start && f.End == end), [tag], warnings);

    private static RawFact? Pick(IEnumerable<RawFact> facts, IReadOnlyList<string> tags, List<string> warnings)
    {
        foreach (var tag in tags)
        {
            var set = facts.Where(f => f.Concept == tag).OrderByDescending(f => f.Filed).ToArray();
            if (set.Length == 0) continue;
            var latest = set.Where(f => f.Filed == set[0].Filed).ToArray();
            if (latest.Select(f => f.Value).Distinct().Count() != 1)
            {
                warnings.Add($"Conflicting same-date facts for {tag}, {set[0].End:yyyy-MM-dd}; unavailable.");
                return null;
            }
            var chosen = latest.OrderByDescending(f => f.Accession, StringComparer.Ordinal).First();
            if (set.Any(f => f.Value != chosen.Value)) warnings.Add($"Restatement for {tag}; using {chosen.Accession}, filed {chosen.Filed:yyyy-MM-dd}.");
            return chosen;
        }
        return null;
    }

    private static bool Annual(RawFact f) => f.Start is not null && Days(f) is >= 349 and <= 379;
    private static int Days(RawFact f) => f.End.DayNumber - f.Start!.Value.DayNumber;
    private static FinancialValue Value(string name, IReadOnlyList<FactContribution> inputs, string unit) =>
        Calculate(name, unit, inputs.Min(i => i.Fact.Start), inputs.Max(i => i.Fact.End), inputs,
            () => inputs.Sum(i => i.Fact.Value * i.Multiplier));

    public static FinancialValue Missing(string name, string unit, string reason) => new(name, null, unit, null, null, [], reason);

    public static FinancialValue Ratio(string name, FinancialValue numerator, FinancialValue denominator, string unit, decimal scale = 1m)
    {
        if (!numerator.Available || !denominator.Available || denominator.Value <= 0
            || numerator.Start != denominator.Start || numerator.End != denominator.End)
            return Missing(name, unit, "Compatible inputs and a positive denominator are required.");
        return Calculate(name, unit, numerator.Start, numerator.End, [.. numerator.Inputs, .. denominator.Inputs],
            () => numerator.Value!.Value / denominator.Value!.Value * scale);
    }

    internal static FinancialValue Calculate(string name, string unit, DateOnly? start, DateOnly? end,
        IReadOnlyList<FactContribution> inputs, Func<decimal> compute)
    {
        try { return new(name, compute(), unit, start, end, inputs); }
        catch (OverflowException) { return Missing(name, unit, "Calculation exceeds the supported decimal range."); }
    }
}
