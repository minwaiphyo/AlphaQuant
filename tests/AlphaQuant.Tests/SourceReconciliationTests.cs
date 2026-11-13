using System.Globalization;
using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using AlphaQuant.Tests.Support;

namespace AlphaQuant.Tests;

public class SourceReconciliationTests
{
    [Fact]
    public void ParsedSyntheticSourcesReconcileToIndependentWrittenExpectations()
    {
        var bytes = Fixtures.Read("s02-independent-worked.sample.json");
        using var doc = JsonDocument.Parse(bytes);
        var expected = doc.RootElement.GetProperty("_s02").GetProperty("expected");
        var parsed = SecParser.ParseFacts(bytes);
        var today = new DateOnly(2026, 10, 28);
        var f = ComparisonAnalyzer.Analyze(parsed.Facts, today);
        decimal Expect(string name) => decimal.Parse(expected.GetProperty(name).GetString()!, CultureInfo.InvariantCulture);
        Assert.Equal(Expect("annualRevenueGrowth"), f.History[0].RevenueGrowth.Value);
        Assert.Equal(Expect("ttmRevenue"), f.TtmRevenue.Value);
        Assert.Equal(Expect("ttmOperatingIncome"), f.TtmOperatingIncome.Value);
        Assert.Equal(Expect("ttmOperatingMargin"), f.TtmOperatingMargin.Value);
        Assert.Equal(Expect("ttmOperatingCashFlow"), f.TtmOperatingCashFlow.Value);
        Assert.Equal(Expect("ttmEps"), f.TtmDilutedEps.Value);
        Assert.Equal(Expect("cash"), f.CashBalance.Value);
        Assert.Equal(new DateOnly(2025, 10, 1), f.TtmRevenue.Start);
        Assert.Equal(new DateOnly(2026, 9, 30), f.TtmRevenue.End);
        var now = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        var v = ComparisonValuation.Calculate(new(1, "FIX", "Synthetic"), f,
            new("FIX", 60m, "USD", "fixture", "synthetic", now, now, "1"), now,
            new(1, "FIX", today, today, 100000000m, 400000000m, 100000000m, 0m, 0m, "synthetic-filing", "synthetic-review", today, true, true),
            new(1, "FIX", today, "synthetic-review", true));
        Assert.Equal(Expect("pe"), v.PriceEarnings.Value);
        Assert.Equal(Expect("enterpriseValue"), v.EnterpriseValue);
        Assert.InRange(decimal.Abs(v.EnterpriseValueSales.Value!.Value - Expect("evSales")), 0m, 0.000000000000000000000001m);
        Assert.Equal(ComparisonAnalyzer.DefinitionVersion, v.DefinitionVersion);
    }

    [Fact]
    public void SignedBridgeSourceReferencesRemainInspectable()
    {
        var facts = SecParser.ParseFacts(Fixtures.Read("s02-independent-worked.sample.json"));
        var f = ComparisonAnalyzer.Analyze(facts.Facts, new(2026, 10, 28));
        Assert.Equal(3, f.TtmRevenue.Inputs.Count);
        var deduction = Assert.Single(f.TtmRevenue.Inputs, i => i.Multiplier == -1m);
        Assert.Equal(850000000m, deduction.Fact.Value);
        Assert.Equal(new DateOnly(2025, 9, 30), deduction.Fact.End);
        Assert.Equal("0000000001-26-000001", deduction.Fact.Accession);
        Assert.Equal(4, f.TtmDilutedEps.Inputs.Count);
        Assert.All(f.TtmDilutedEps.Inputs, i => Assert.Equal(1m, i.Multiplier));
    }
}
