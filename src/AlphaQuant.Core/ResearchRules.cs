namespace AlphaQuant.Core;

public sealed record ResearchRule(Guid Id,string Metric,string Operator,decimal Threshold,decimal Weight);
public sealed record ResearchRuleSet(string Version,IReadOnlyList<ResearchRule> Rules);
public sealed record RuleObservation(ResearchRule Rule,decimal? Input,bool? Triggered,DateOnly? PeriodEnd,string? Reason);
public sealed record ScreeningResult(long Cik,string Version,IReadOnlyList<RuleObservation> Observations,decimal? Score,int Available,int Total);
public static class ResearchScreening
{
    public const string Version="screening-1";
    public static ResearchRuleSet Defaults()=>new(Version,new[]{("Cash conversion","<",1m),("Receivables / revenue",">",.25m),("Inventory / revenue",">",.25m),("Gross margin","<",20m),("Net debt",">",0m),("Interest coverage","<",2m),("Dilution (weighted shares)",">",5m)}.Select((r,i)=>new ResearchRule(new Guid(i+1,0,0,new byte[8]),r.Item1,r.Item2,r.Item3,1)).ToArray());
    public static void Validate(ResearchRuleSet set)
    {
        if(set.Version!=Version||set.Rules is null||set.Rules.Count is <1 or >20||set.Rules.Any(r=>r is null||r.Id==Guid.Empty||!ExpandedFundamentals.Names.Contains(r.Metric)||r.Operator is not(">" or ">=" or "<" or "<=")||r.Weight is <=0 or >100)||set.Rules.Select(r=>r.Id).Distinct().Count()!=set.Rules.Count)throw new ArgumentException("Use 1–20 unique screening-1 rules, known metrics, explicit comparisons and weights >0–100.");
    }
    public static ScreeningResult Evaluate(ComparisonRow row,ResearchRuleSet set)
    {
        Validate(set);
        var observations=set.Rules.Select(rule=>
        {
            var metric=row.Fundamentals?.Expanded.Where(m=>m.Result.Name==rule.Metric).OrderByDescending(m=>m.Result.End).FirstOrDefault();
            if(row.FactsStale||metric?.DefinitionVersion!=ExpandedFundamentals.Version||metric.Result.Available!=true)return new RuleObservation(rule,null,null,metric?.Result.End,row.FactsStale?"Retained/stale financial evidence; review before evaluating.":metric?.Result.Reason??"Supported metric input unavailable.");
            var input=metric.Result.Value!.Value;
            var trigger=rule.Operator switch{">"=>input>rule.Threshold,">="=>input>=rule.Threshold,"<"=>input<rule.Threshold,_=>input<=rule.Threshold};
            return new RuleObservation(rule,input,trigger,metric.Result.End,null);
        }).ToArray();
        decimal? score=observations.All(o=>o.Triggered.HasValue)?observations.Where(o=>o.Triggered==true).Sum(o=>o.Rule.Weight)/observations.Sum(o=>o.Rule.Weight)*100:null;
        return new(row.Company.Cik,Version,observations,score,observations.Count(o=>o.Triggered.HasValue),observations.Length);
    }
}
