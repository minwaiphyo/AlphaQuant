using System.Text;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public class SecParserTests
{
    [Fact]
    public void ParsesDirectoryEntriesAndSkipsInvalidOnes()
    {
        var companies = SecParser.ParseDirectory(Fixtures.Read("company_tickers.sample.json"));

        Assert.Equal(["AAPL", "MSFT", "GOOGL"], companies.Select(c => c.Ticker));
        Assert.Equal(320193, companies[0].Cik);
        Assert.Equal("Apple Inc.", companies[0].Name);
        Assert.Equal(1652044, companies[2].Cik); // cik_str given as a string
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"0\":{\"ticker\":\"X\"}}")]
    public void RejectsADirectoryWithoutValidCompanies(string json)
    {
        var ex = Assert.Throws<DataSourceException>(() => SecParser.ParseDirectory(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(DataSourceErrorKind.InvalidData, ex.Kind);
    }

    [Fact]
    public void ParsesOnlyTheConceptsAlphaQuantUses()
    {
        var parsed = SecParser.ParseFacts(Fixtures.Read("companyfacts.sample.json"));

        Assert.Equal("EXAMPLE CORP", parsed.EntityName);
        Assert.Equal(7, parsed.Facts.Count);
        Assert.DoesNotContain(parsed.Facts, f => f.Concept is "Goodwill" or "EntityCommonStockSharesOutstanding");
        Assert.Equal(1, parsed.SkippedFacts);
    }

    [Fact]
    public void ReadsValuesAsExactDecimalsWithDatesFormAndAccession()
    {
        var parsed = SecParser.ParseFacts(Fixtures.Read("companyfacts.sample.json"));

        var revenue = parsed.Facts.Single(f =>
            f.Concept == "RevenueFromContractWithCustomerExcludingAssessedTax"
            && f.End == new DateOnly(2025, 9, 30));
        Assert.Equal(500_000_000_000m, revenue.Value);
        Assert.Equal(new DateOnly(2024, 10, 1), revenue.Start);
        Assert.Equal("10-K", revenue.Form);
        Assert.Equal(new DateOnly(2025, 11, 1), revenue.Filed);
        Assert.Equal("0000000001-25-000001", revenue.Accession);
        Assert.Equal("USD", revenue.Unit);

        var assets = parsed.Facts.Single(f => f.Concept == "AssetsCurrent");
        Assert.Null(assets.Start);
    }

    [Fact]
    public void KeepsLargeDecimalsWithoutFloatingPointRounding()
    {
        const string json = """
            {"facts":{"us-gaap":{"AssetsCurrent":{"units":{"USD":[
              {"end":"2025-09-30","val":123456789012345678,"accn":"0000000001-25-000001","form":"10-K","filed":"2025-11-01"}
            ]}}}}}
            """;

        var parsed = SecParser.ParseFacts(Encoding.UTF8.GetBytes(json));

        Assert.Equal(123_456_789_012_345_678m, Assert.Single(parsed.Facts).Value);
    }

    [Fact]
    public void ACompanyWithoutUsGaapFactsParsesToNoFacts()
    {
        const string json = """{"cik":1,"entityName":"IFRS CO","facts":{"ifrs-full":{}}}""";

        var parsed = SecParser.ParseFacts(Encoding.UTF8.GetBytes(json));

        Assert.Empty(parsed.Facts);
        Assert.Equal(0, parsed.SkippedFacts);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    public void RejectsMalformedFactsDocuments(string json)
    {
        var ex = Assert.Throws<DataSourceException>(() => SecParser.ParseFacts(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(DataSourceErrorKind.InvalidData, ex.Kind);
    }

    [Fact]
    public void ParsedFixtureProducesTheExpectedMetricsEndToEnd()
    {
        var parsed = SecParser.ParseFacts(Fixtures.Read("companyfacts.sample.json"));

        var analysis = AnnualAnalyzer.Analyze(parsed.Facts.ToList());

        Assert.Equal(25m, analysis.Metrics.Single(m => m.Name == TestMetrics.RevenueGrowthName).Value);
        Assert.Equal(10m, analysis.Metrics.Single(m => m.Name == TestMetrics.NetMarginName).Value);
        Assert.Equal(1.5m, analysis.Metrics.Single(m => m.Name == TestMetrics.CurrentRatioName).Value);
    }
}
