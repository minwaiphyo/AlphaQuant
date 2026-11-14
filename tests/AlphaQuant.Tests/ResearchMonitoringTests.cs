using AlphaQuant.Core;
namespace AlphaQuant.Tests;
public sealed class ResearchMonitoringTests
{
    private static SavedFinancialSnapshot Snapshot(decimal revenue,string accession,string version="comparison-1.1")
    {
        var f=ComparisonAnalyzer.Analyze([new("Revenues","USD",revenue,new(2025,1,1),new(2025,12,31),"10-K",new(2026,2,1),accession)],new(2026,3,1));
        return new(Guid.NewGuid(),Guid.NewGuid(),1,new(2026,3,1,0,0,0,TimeSpan.Zero),version,[new(new(1,"AAA","Synthetic"),f,null,false){DefinitionVersion=version}]);
    }
    [Fact]public void Duplicate_definition_and_source_changes_remain_distinct()
    {
        var first=Snapshot(100,"0000000001-26-000001");Assert.Equal("duplicate",Assert.Single(ResearchMonitoring.Compare(first,first)).Kind);
        var restated=Snapshot(120,"0000000001-26-000002") with{CapturedAt=first.CapturedAt.AddDays(1)};Assert.Contains(ResearchMonitoring.Compare(first,restated),c=>c.Kind=="restatement-or-source-change");
        var changed=restated with{Companies=restated.Companies.Select(c=>c with{DefinitionVersion="different"}).ToArray()};Assert.Equal("definition-change",Assert.Single(ResearchMonitoring.Compare(first,changed)).Kind);
        var refiling=Snapshot(100,"0000000001-26-000002") with{CapturedAt=first.CapturedAt.AddDays(1)};Assert.Contains(ResearchMonitoring.Compare(first,refiling),c=>c.Kind=="new-filing");
        Assert.Throws<ArgumentException>(()=>ResearchMonitoring.Compare(restated,first));
    }
    [Fact]public void Sourced_history_has_original_dates_values_and_missing_reasons()
    {var snapshot=Snapshot(123.456m,"0000000001-26-000001");var history=ResearchMonitoring.History(snapshot.Companies[0],"Annual revenue");Assert.Equal(123.456m,Assert.Single(history).Value);Assert.Contains("sec.gov",history[0].Source);Assert.Null(Assert.Single(ResearchMonitoring.History(snapshot.Companies[0],"Annual operating cash flow")).Value);}
}
