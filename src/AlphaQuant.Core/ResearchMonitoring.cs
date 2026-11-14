namespace AlphaQuant.Core;
public sealed record SnapshotChange(long Cik,string Metric,string Kind,string Explanation);
public sealed record HistoricalObservation(DateOnly End,decimal? Value,string Unit,string Source);
public static class ResearchMonitoring
{
    public static IReadOnlyList<SnapshotChange> Compare(SavedFinancialSnapshot older,SavedFinancialSnapshot newer)
    {
        if(older.Id==newer.Id)return [new(0,"Snapshot","duplicate","Same immutable snapshot; no new refresh or observation.")];
        if(older.CapturedAt>newer.CapturedAt)throw new ArgumentException("Choose snapshots in capture-time order.");
        var result=new List<SnapshotChange>();
        foreach(var cik in older.Companies.Select(c=>c.Company.Cik).Union(newer.Companies.Select(c=>c.Company.Cik)).Order())
        {
            var a=older.Companies.SingleOrDefault(c=>c.Company.Cik==cik);var b=newer.Companies.SingleOrDefault(c=>c.Company.Cik==cik);
            if(a is null||b is null){result.Add(new(cik,"Membership","coverage",a is null?"Company newly covered; no zero baseline.":"Company no longer covered; no zero replacement."));continue;}
            if((a.DefinitionVersion??older.DefinitionVersion)!=(b.DefinitionVersion??newer.DefinitionVersion)){result.Add(new(cik,"Definition","definition-change","Definition changed; raw differences are not comparable business changes."));continue;}
            if(!string.Join("|",a.Fundamentals?.Expanded.Select(m=>m.DefinitionVersion).Distinct().Order().ToArray()??[]).Equals(string.Join("|",b.Fundamentals?.Expanded.Select(m=>m.DefinitionVersion).Distinct().Order().ToArray()??[]),StringComparison.Ordinal))result.Add(new(cik,"Expanded definitions","definition-change","Expanded metric definitions differ; inspect versions before comparing."));
            var first=Metrics(a.Fundamentals).ToDictionary(v=>(v.Name,v.Start,v.End));var second=Metrics(b.Fundamentals).ToDictionary(v=>(v.Name,v.Start,v.End));
            foreach(var key in first.Keys.Union(second.Keys).OrderBy(k=>k.Name).ThenBy(k=>k.End))
            {
                first.TryGetValue(key,out var x);second.TryGetValue(key,out var y);
                if(x is null||y is null){result.Add(new(cik,key.Name,"period-coverage",$"Period {key.Start:yyyy-MM-dd}/{key.End:yyyy-MM-dd} added/removed; not a same-period change."));continue;}
                if(!x.Available||!y.Available){if(x.Reason!=y.Reason||x.Available!=y.Available)result.Add(new(cik,key.Name,"availability","Availability changed; missing inputs do not become zero."));continue;}
                var sourceChanged=!x.Inputs.Select(i=>(i.Fact.Accession,i.Fact.Filed,i.Fact.Value)).SequenceEqual(y.Inputs.Select(i=>(i.Fact.Accession,i.Fact.Filed,i.Fact.Value)));
                if(sourceChanged&&x.Inputs.Count>0&&y.Inputs.Count>0&&y.Inputs.Max(i=>i.Fact.Filed)<x.Inputs.Max(i=>i.Fact.Filed)){result.Add(new(cik,key.Name,"older-source","Evidence filing date moved backwards; inspect source ordering before interpreting values."));continue;}
                if(x.Value!=y.Value)result.Add(new(cik,key.Name,sourceChanged?"restatement-or-source-change":"value-change",$"Same period {key.Start:yyyy-MM-dd}/{key.End:yyyy-MM-dd}: {x.Value} → {y.Value} {y.Unit}; inspect exact filings/inputs. No causal business attribution is inferred."));
                else if(sourceChanged)result.Add(new(cik,key.Name,"new-filing","Same value, different filed evidence; inspect original source dates."));
            }
        }
        return result.Count==0?[new(0,"Snapshot","unchanged","Comparable observed financial values and sources unchanged.")]:result;
    }
    private static IEnumerable<FinancialValue> Metrics(ComparisonFundamentals? f)=>f is null?[]:f.History.SelectMany(y=>new[]{y.Revenue,y.RevenueGrowth,y.OperatingMargin,y.OperatingCashFlow}.Select(v=>v with{Start=y.Start,End=y.End})).Concat(f.Expanded.Select(m=>m.Result with{Name=m.DefinitionVersion+" / "+m.Result.Name}));
    public static IReadOnlyList<HistoricalObservation> History(SavedFinancialCompany company,string metric)
    {
        if(company.Fundamentals is not{} f)return [];
        return f.History.OrderBy(y=>y.End).Select(y=>(metric switch{"Annual revenue"=>y.Revenue,"Annual operating margin"=>y.OperatingMargin,"Annual operating cash flow"=>y.OperatingCashFlow,_=>throw new ArgumentException("Choose supported historical fundamental.")}) with{Start=y.Start,End=y.End}).Select(v=>new HistoricalObservation(v.End!.Value,v.Available?v.Value:null,v.Unit,string.Join("; ",v.Inputs.Select(i=>$"{i.Fact.Concept}, filed {i.Fact.Filed:yyyy-MM-dd}, {FilingLinks.TryBuild(company.Company.Cik,i.Fact.Accession)}"))+(v.Reason is null?"":" Unavailable: "+v.Reason))).ToArray();
    }
}
