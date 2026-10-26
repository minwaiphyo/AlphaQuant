namespace AlphaQuant.Core;

/// <summary>Case-insensitive ticker and name search. Ranks exact ticker, ticker prefix, name prefix, then name contains.</summary>
public static class CompanySearch
{
    public const int DefaultMaxResults = 20;

    public static IReadOnlyList<CompanyRef> Search(
        IEnumerable<CompanyRef> companies,
        string? query,
        int maxResults = DefaultMaxResults)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text) || maxResults <= 0)
        {
            return [];
        }

        var matches = new List<(int Rank, int Order, CompanyRef Company)>();
        var order = 0;
        foreach (var company in companies)
        {
            if (Rank(company, text) is { } rank)
            {
                matches.Add((rank, order, company));
            }

            order++;
        }

        return matches
            .OrderBy(m => m.Rank)
            .ThenBy(m => m.Order)
            .Take(maxResults)
            .Select(m => m.Company)
            .ToList();
    }

    private static int? Rank(CompanyRef company, string text)
    {
        if (company.Ticker.Equals(text, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (company.Ticker.StartsWith(text, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (company.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (company.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return null;
    }
}
