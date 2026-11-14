using System.Globalization;
namespace AlphaQuant.Core;
public sealed record ReportLine(string Section,string Label,string Value,string Unit,string Basis,string Source);
public sealed record FrozenResearchReport(string Version,string Title,DateTimeOffset CapturedAt,IReadOnlyList<ReportLine> Lines);
public static class ResearchReport
{
    public static FrozenResearchReport Capture(ResearchDocumentRevision document,IReadOnlyList<SavedFinancialSnapshot> snapshots)
    {
        var previous=CultureInfo.CurrentCulture;
        try{CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;return CaptureInvariant(document,snapshots);}
        finally{CultureInfo.CurrentCulture=previous;}
    }
    private static FrozenResearchReport CaptureInvariant(ResearchDocumentRevision document,IReadOnlyList<SavedFinancialSnapshot> snapshots)
    {
        if(document.SnapshotIds.Any(id=>snapshots.All(s=>s.Id!=id)))throw new ArgumentException("All linked snapshots must be available for a consistent frozen report.");
        var lines=new List<ReportLine>();
        void Add(string section,string label,string value,string unit="",string basis="",string source="")=>lines.Add(new(section,label,value,unit,basis,source));
        Add("Scope","Limitations","User research and assumptions, not investment advice. No current prices, credentials or session reviews. Latest-filed history is not point-in-time valuation.");
        Add("Revision","Identity",$"{document.Id} / revision {document.Revision}",basis:document.SavedAt.ToString("O"));
        var t=document.Thesis;
        foreach(var pair in new[]{("Thesis",t.Thesis),("Variant view",t.VariantView),("Catalysts",t.Catalysts),("Risks",t.Risks),("Invalidation",t.InvalidationConditions),("Horizon",t.Horizon),("Peer comparability",t.PeerComparability),("User conclusion",t.Conclusion)})Add("User-authored thesis",pair.Item1,pair.Item2);
        foreach(var snapshot in snapshots.Where(s=>document.SnapshotIds.Contains(s.Id)))foreach(var company in snapshot.Companies)
        {
            Add("Snapshot",company.Company.Name,$"CIK {company.Company.Cik:D10} / {company.Company.Ticker}",basis:$"Captured {snapshot.CapturedAt:O}; retrieved {company.FactsRetrievedAt:O}; definition {company.DefinitionVersion??snapshot.DefinitionVersion}");
            if(company.Fundamentals is not{} f){Add("Financials","Unavailable","No financial results saved.");continue;}
            var metrics=f.History.SelectMany(y=>new[]{y.Revenue,y.RevenueGrowth,y.OperatingMargin,y.OperatingCashFlow}.Select(v=>v with{Start=y.Start,End=y.End})).Concat(new[]{f.TtmRevenue,f.TtmOperatingMargin,f.TtmOperatingCashFlow,f.TtmDilutedEps,f.CashBalance}).Select(v=>(Value:v,Definition:company.DefinitionVersion??snapshot.DefinitionVersion)).Concat(f.Expanded.Select(m=>(Value:m.Result,Definition:m.DefinitionVersion)));
            foreach(var entry in metrics)
            {
                var metric=entry.Value;
                Add(company.Company.Ticker,metric.Name,metric.Available?metric.Value!.Value.ToString("G29",CultureInfo.InvariantCulture):"Unavailable: "+metric.Reason,metric.Unit,$"{metric.Start:yyyy-MM-dd} / {metric.End:yyyy-MM-dd}; {entry.Definition}",string.Join("; ",metric.Inputs.Select(i=>$"{i.Fact.Concept}={i.Fact.Value.ToString("G29",CultureInfo.InvariantCulture)} {i.Fact.Unit}; {i.Fact.Start:yyyy-MM-dd}/{i.Fact.End:yyyy-MM-dd}; filed {i.Fact.Filed:yyyy-MM-dd}; {FilingLinks.TryBuild(company.Company.Cik,i.Fact.Accession)}")));
            }
        }
        if(document.Rules is{} rules)foreach(var r in rules.Rules)Add("User rules",r.Metric,$"{r.Operator} {r.Threshold.ToString(CultureInfo.InvariantCulture)}; weight {r.Weight.ToString(CultureInfo.InvariantCulture)}",basis:rules.Version);
        foreach(var review in document.ConditionReviews)foreach(var result in review.Results)Add("Manual condition review",$"Snapshot {review.SnapshotId} / CIK {result.Cik}",$"Coverage {result.Available}/{result.Total}; score {result.Score}; "+string.Join("; ",result.Observations.Select(o=>$"{o.Rule.Metric} {o.Rule.Operator} {o.Rule.Threshold}: input={o.Input}, trigger={o.Triggered}, period={o.PeriodEnd:yyyy-MM-dd}, reason={o.Reason}")),basis:review.EvaluatedAt.ToString("O"));
        if(document.Model is{} model)
        {
            Add("Assumptions","Scenario",model.Scenario+" (user label, not probability)");Add("Assumptions","Basis",model.Forecast.BasisNote,basis:model.Forecast.Version);Add("Assumptions","Base revenue",model.Forecast.BaseRevenue.ToString(CultureInfo.InvariantCulture),"USD");Add("Assumptions","Base OWC",model.Forecast.BaseWorkingCapital.ToString(CultureInfo.InvariantCulture),"USD");
            for(var i=0;i<model.Forecast.Years.Count;i++){var y=model.Forecast.Years[i];Add("Assumptions",$"Year {i+1}",$"growth={y.RevenueGrowth}; margin={y.OperatingMargin}; tax={y.TaxRate}; D&A={y.DepreciationRate}; capex={y.CapexRate}; OWC={y.WorkingCapitalRate}","fractions");}
            var d=model.Dcf;Add("Assumptions","Discount / terminal",$"WACC={d.Wacc}; growth={d.TerminalGrowth}; next FCFF={d.NextFcff}",basis:d.Version);Add("Assumptions","Terminal policy",d.TerminalPolicy);Add("Assumptions","Capital review",d.Capital.ReviewNote);Add("Assumptions","Capital bridge",$"cash={d.Capital.ExcessCash}; other={d.Capital.OtherAssets}; debt={d.Capital.Debt}; preferred={d.Capital.Preferred}; NCI={d.Capital.Nci}; actual shares={d.Capital.ActualShares}");
            var exploration=ResearchExploration.Replay(model);
            foreach(var cell in exploration.DiscountGrid)Add("Sensitivity",$"WACC {cell.Wacc}; g {cell.Growth}",cell.PerShare?.ToString("G29",CultureInfo.InvariantCulture)??("Unavailable: "+cell.Reason),"USD/share","Other assumptions and terminal next-FCFF fixed");
            foreach(var cell in exploration.RevenueDriver)Add("Driver sensitivity",$"Constant explicit-year growth {cell.Growth}",cell.PerShare?.ToString("G29",CultureInfo.InvariantCulture)??("Unavailable: "+cell.Reason),"USD/share","Other assumptions and terminal next-FCFF fixed");
            if(model.Exploration?.Reverse is{} target&&exploration.Reverse is{} solved)Add("Reverse DCF","User target and bounds",$"Target {target.Target}; bounds {target.Lower}..{target.Upper}; tolerance {target.Tolerance}; limit {target.Iterations}; state {solved.State}; growth {solved.Growth}; forward {solved.ForwardPrice}; residual {solved.Residual}; evaluations {solved.Evaluations}. {solved.Explanation}","USD/share except growth",DcfEngine.Version);
            var value=DcfEngine.Calculate(model.Forecast,d);
            foreach(var y in value.Years)Add("Forecast",$"Year {y.Forecast.Year}",$"revenue={y.Forecast.Revenue}; EBIT={y.Forecast.Ebit}; tax={y.Forecast.Tax}; NOPAT={y.Forecast.Nopat}; D&A={y.Forecast.Depreciation}; capex={y.Forecast.Capex}; OWC={y.Forecast.WorkingCapital}; delta OWC={y.Forecast.ChangeWorkingCapital}; FCFF={y.Forecast.Fcff}; factor={y.DiscountFactor}; PV={y.PresentValue}","USD except factor",model.Forecast.Version);
            foreach(var pair in new[]{("Terminal PV",value.TerminalPresentValue),("Enterprise value",value.EnterpriseValue),("Common equity",value.EquityValue),("Per actual share",value.PerShare)})Add("DCF",pair.Item1,pair.Item2.ToString("G29",CultureInfo.InvariantCulture),pair.Item1=="Per actual share"?"USD/share":"USD",value.Version);
        }
        // Freeze culture-sensitive interpolated assumptions under invariant culture at caller boundary.
        if(lines.Count>5000||lines.Any(l=>new[]{l.Section,l.Label,l.Value,l.Unit,l.Basis,l.Source}.Any(v=>v.Length>32000)))throw new ArgumentException("Report exceeds supported line/text limits.");
        return new("report-1",document.Title,document.SavedAt,lines.ToArray());
    }
}
