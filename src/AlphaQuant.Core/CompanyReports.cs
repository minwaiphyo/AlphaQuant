namespace AlphaQuant.Core;

public static class CompanyReports
{
    public static CompanyReport Build(CompanyRef company, CompanyFacts facts)
    {
        var analysis = AnnualAnalyzer.Analyze(facts.Facts);
        var warnings = analysis.Warnings.ToList();

        if (facts.SkippedFacts > 0)
        {
            warnings.Add($"{facts.SkippedFacts} malformed fact entries in the SEC data were skipped.");
        }

        if (facts.IsStale)
        {
            warnings.Add($"The SEC could not be reached; showing cached data retrieved {facts.RetrievedAt:yyyy-MM-dd HH:mm} UTC.");
        }

        return new CompanyReport(
            company,
            analysis.Facts,
            analysis.Metrics,
            warnings,
            analysis.UnsupportedReason,
            facts.RetrievedAt,
            facts.IsStale);
    }
}
