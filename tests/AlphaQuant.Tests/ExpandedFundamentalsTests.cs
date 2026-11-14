using AlphaQuant.Core;
using System.Text.Json;
namespace AlphaQuant.Tests;

public sealed class ExpandedFundamentalsTests
{
    private static readonly DateOnly End = new(2025,12,31);
    private static List<RawFact> Facts()
    {
        var facts=new List<RawFact>();
        void Flow(string tag,decimal value,string unit="USD")=>facts.Add(new(tag,unit,value,new(2025,1,1),End,"10-K",new(2026,2,1),"0000000001-26-000001"));
        void Balance(string tag,decimal first,decimal last){facts.Add(new(tag,"USD",first,null,new(2024,12,31),"10-K",new(2026,2,1),"0000000001-26-000001"));facts.Add(new(tag,"USD",last,null,End,"10-K",new(2026,2,1),"0000000001-26-000001"));}
        Flow("Revenues",200);Flow("GrossProfit",80);Flow("NetIncomeLoss",20);Flow("OperatingIncomeLoss",40);Flow("NetCashProvidedByUsedInOperatingActivities",30);Flow("PaymentsToAcquirePropertyPlantAndEquipment",12);Flow("IncomeTaxExpenseBenefit",5);Flow("IncomeLossFromContinuingOperationsBeforeIncomeTaxesExtraordinaryItemsNoncontrollingInterest",25);Flow("InterestAndDebtExpense",8);
        Balance("AssetsCurrent",90,90);Balance("LiabilitiesCurrent",30,30);Balance("Assets",180,220);Balance("StockholdersEquity",80,120);Balance("StockholdersEquityIncludingPortionAttributableToNoncontrollingInterest",90,130);Balance("LongTermDebtAndFinanceLeaseObligationsIncludingCurrentMaturities",50,60);Balance("ShortTermBorrowings",10,10);Balance("CashAndCashEquivalentsAtCarryingValue",20,20);
        return facts;
    }
    private static FinancialValue Value(IEnumerable<RawFact> facts,string name)=>ComparisonAnalyzer.Analyze(facts.ToArray(),new(2026,3,1)).Expanded.Single(m=>m.Result.Name==name).Result;
    [Theory]
    [InlineData("Gross margin",40)] [InlineData("Net margin",10)] [InlineData("Current ratio",3)]
    [InlineData("Free cash flow (OCF less capex)",18)] [InlineData("Cash conversion",1.5)]
    [InlineData("Return on equity",20)] [InlineData("Asset turnover",1)] [InlineData("Net debt",50)] [InlineData("Interest coverage",5)]
    public void Independent_worked_metrics_preserve_source_dates_and_definition(string name,decimal expected)
    { var v=Value(Facts(),name);Assert.Equal(expected,v.Value);Assert.Equal(End,v.End);Assert.NotEmpty(v.Inputs);Assert.Contains("expanded-1",Assert.Single(v.EvidenceReferences)); }
    [Fact] public void Roic_uses_positive_average_capital_and_no_tax_loss_benefit()
    {
        var facts=Facts();Assert.InRange(decimal.Abs(640m/31m-Value(facts,"Return on invested capital").Value!.Value),0m,0.000000000000000000000001m);
        facts=facts.Select(f=>f.Concept=="IncomeTaxExpenseBenefit"?f with{Value=-5}:f).ToList();
        Assert.False(Value(facts,"Return on invested capital").Available);
    }
    [Fact] public void Missing_explicit_short_debt_does_not_become_zero_and_conflicts_are_not_resolved_by_order()
    {
        var facts=Facts();facts.RemoveAll(f=>f.Concept=="ShortTermBorrowings");Assert.False(Value(facts,"Net debt").Available);
        facts=Facts();facts.Add(facts.First(f=>f.Concept=="GrossProfit") with{Value=99});Assert.False(Value(facts,"Gross margin").Available);
    }
    [Fact] public void Positive_base_growth_and_contiguous_cagr_reject_negative_or_missing_years()
    {
        var facts=Facts();
        foreach(var year in new[]{2022,2023,2024})facts.Add(new("Revenues","USD",year==2022?100:150,new(year,1,1),new(year,12,31),"10-K",new(2026,2,1),"0000000001-26-000001"));
        var latest=ComparisonAnalyzer.Analyze(facts,new(2026,3,1)).Expanded.First(m=>m.Result.Name=="Revenue CAGR (3 years)").Result;
        Assert.InRange(latest.Value!.Value,25.9921049894m,25.9921049896m);
        facts.RemoveAll(f=>f.Concept=="Revenues"&&f.End.Year==2023);
        Assert.False(ComparisonAnalyzer.Analyze(facts,new(2026,3,1)).Expanded.First(m=>m.Result.Name=="Revenue CAGR (3 years)").Result.Available);
    }
    [Fact] public void Overflow_and_future_facts_are_unavailable_without_rewriting_comparison_baseline()
    {
        var facts=Facts().Select(f=>f.Concept=="Assets"?f with{Value=decimal.MaxValue}:f).ToList();
        Assert.False(Value(facts,"Asset turnover").Available);
        facts=Facts().Select(f=>f.Concept=="GrossProfit"?f with{Filed=new(2027,1,1)}:f).ToList();Assert.False(Value(facts,"Gross margin").Available);
        Assert.Equal("comparison-1.1",ComparisonAnalyzer.DefinitionVersion);
    }
    [Fact] public void Old_snapshots_have_no_expanded_results_and_full_results_roundtrip()
    {
        var source=ComparisonAnalyzer.Analyze(Facts(),new(2026,3,1));
        var json=JsonSerializer.Serialize(source);Assert.Equal(source.Expanded.Count,JsonSerializer.Deserialize<ComparisonFundamentals>(json)!.Expanded.Count);
        var node=System.Text.Json.Nodes.JsonNode.Parse(json)!;node.AsObject().Remove("Expanded");Assert.Empty(JsonSerializer.Deserialize<ComparisonFundamentals>(node.ToJsonString())!.Expanded);
        Assert.Equal(ExpandedFundamentals.Names.Order(),source.Expanded.Select(m=>m.Result.Name).Order());
    }
}
