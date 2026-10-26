using AlphaQuant.Core;

namespace AlphaQuant.Tests;

public class CompanySearchTests
{
    private static readonly CompanyRef[] Companies =
    [
        new(1, "APLE", "Apple Hospitality REIT, Inc."),
        new(2, "AAPL", "Apple Inc."),
        new(3, "PNPL", "Pineapple Express Inc"),
        new(4, "AP", "Ampco-Pittsburgh Corp"),
        new(5, "MSFT", "MICROSOFT CORP"),
    ];

    [Fact]
    public void ExactTickerRanksBeforeTickerPrefixAndNameMatches()
    {
        var results = CompanySearch.Search(Companies, "ap");

        // AP: exact ticker; APLE: ticker prefix; AAPL: name starts with; PNPL: name contains.
        Assert.Equal(["AP", "APLE", "AAPL", "PNPL"], results.Select(c => c.Ticker));
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        Assert.Equal("MSFT", Assert.Single(CompanySearch.Search(Companies, "msft")).Ticker);
        Assert.Equal("MSFT", Assert.Single(CompanySearch.Search(Companies, "microsoft")).Ticker);
    }

    [Fact]
    public void NameStartsWithRanksBeforeNameContains()
    {
        var results = CompanySearch.Search(Companies, "apple");

        Assert.Equal(["APLE", "AAPL", "PNPL"], results.Select(c => c.Ticker));
    }

    [Fact]
    public void BlankQueryAndNoMatchReturnNothing()
    {
        Assert.Empty(CompanySearch.Search(Companies, ""));
        Assert.Empty(CompanySearch.Search(Companies, "   "));
        Assert.Empty(CompanySearch.Search(Companies, null));
        Assert.Empty(CompanySearch.Search(Companies, "zzzz"));
    }

    [Fact]
    public void ResultsAreCappedAndKeepSourceOrderWithinARank()
    {
        var many = Enumerable.Range(0, 50).Select(i => new CompanyRef(i, $"AB{i:00}", $"Company {i}")).ToList();

        var results = CompanySearch.Search(many, "AB", maxResults: 20);

        Assert.Equal(20, results.Count);
        Assert.Equal("AB00", results[0].Ticker);
        Assert.Equal("AB19", results[19].Ticker);
    }
}
