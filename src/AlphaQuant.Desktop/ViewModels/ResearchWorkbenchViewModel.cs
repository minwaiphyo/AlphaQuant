using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace AlphaQuant.Desktop.ViewModels;
public partial class ForecastYearEditor:ObservableObject
{
    public int Year{get;}
    public ForecastYearEditor(int year){Year=year;}
    [ObservableProperty]private string _growth="";
    [ObservableProperty]private string _margin="";
    [ObservableProperty]private string _tax="";
    [ObservableProperty]private string _depreciation="";
    [ObservableProperty]private string _capex="";
    [ObservableProperty]private string _workingCapital="";
}
public partial class RuleEditor:ObservableObject
{
    public Guid Id{get;}
    public RuleEditor(ResearchRule rule){Id=rule.Id;_metric=rule.Metric;_operator=rule.Operator;_threshold=rule.Threshold.ToString(CultureInfo.InvariantCulture);_weight=rule.Weight.ToString(CultureInfo.InvariantCulture);}
    public IReadOnlyList<string> Metrics=>ExpandedFundamentals.Names;
    public IReadOnlyList<string> Operators{get;}=[">",">=","<","<="];
    [ObservableProperty]private string _metric;
    [ObservableProperty]private string _operator;
    [ObservableProperty]private string _threshold;
    [ObservableProperty]private string _weight;
}
public sealed record RevisionChoice(ResearchDocumentRevision Revision){public string Label=>$"{Revision.Title} · r{Revision.Revision} · {Revision.Model?.Scenario??"Thesis"} · {(Revision.Deleted?"archived":"saved")} · {Revision.SavedAt:yyyy-MM-dd HH:mm} UTC";}
public sealed record SnapshotChoice(SavedFinancialSnapshot Snapshot){public string Label=>$"{Snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC · {Snapshot.Id} · {Snapshot.DefinitionVersion}";}
public sealed record ChartSegment(Points Points);
public partial class ResearchWorkbenchViewModel:ObservableObject
{
    private readonly IResearchStore _store;
    private readonly Func<IReadOnlyList<AlphaQuant.Core.ComparisonRow>> _rows;
    private ResearchDocumentRevision? _editing;
    private FrozenResearchReport? _report;
    private readonly List<ThesisConditionReview> _conditionReviews = [];
    private CancellationTokenSource? _operation;
    public ResearchWorkbenchViewModel(IResearchStore store,Func<IReadOnlyList<AlphaQuant.Core.ComparisonRow>> rows){_store=store;_rows=rows;Years.Add(new(1));SetRules(ResearchScreening.Defaults());}
    public ObservableCollection<RevisionChoice> Revisions{get;}=[];
    public ObservableCollection<SnapshotChoice> Snapshots{get;}=[];
    public ObservableCollection<Guid> LinkedSnapshots{get;}=[];
    public ObservableCollection<ForecastYearEditor> Years{get;}=[];
    public ObservableCollection<RuleEditor> Rules{get;}=[];
    public ObservableCollection<ChartSegment> ChartSegments{get;}=[];
    public IReadOnlyList<string> Scenarios{get;}=["Bear","Base","Bull"];
    public IReadOnlyList<string> Formats{get;}=["CSV","XLSX","PDF"];
    public IReadOnlyList<string> HistoricalMetrics{get;}=["Annual revenue","Annual operating margin","Annual operating cash flow"];
    [ObservableProperty]private RevisionChoice? _selectedRevision;
    [ObservableProperty]private SnapshotChoice? _selectedSnapshot;
    [ObservableProperty]private SnapshotChoice? _olderSnapshot;
    [ObservableProperty]private bool _isBusy;
    [ObservableProperty]private string _status="Explicit local saves only. Draft edits stay in this session until saved; no network request is started here.";
    [ObservableProperty]private string _documentTitle="";
    [ObservableProperty]private string _thesis="";
    [ObservableProperty]private string _variantView="";
    [ObservableProperty]private string _catalysts="";
    [ObservableProperty]private string _risks="";
    [ObservableProperty]private string _invalidation="";
    [ObservableProperty]private string _horizon="";
    [ObservableProperty]private string _peerNotes="";
    [ObservableProperty]private string _conclusion="";
    [ObservableProperty]private bool _includeModel;
    [ObservableProperty]private bool _includeRules;
    [ObservableProperty]private bool _includeExploration;
    [ObservableProperty]private string _scenario="Base";
    [ObservableProperty]private string _baseRevenue="";
    [ObservableProperty]private string _baseWorkingCapital="";
    [ObservableProperty]private string _basisNote="";
    [ObservableProperty]private string _wacc="";
    [ObservableProperty]private string _terminalGrowth="";
    [ObservableProperty]private string _nextFcff="";
    [ObservableProperty]private string _terminalPolicy="";
    [ObservableProperty]private string _cash="";
    [ObservableProperty]private string _otherAssets="";
    [ObservableProperty]private string _debt="";
    [ObservableProperty]private string _preferred="";
    [ObservableProperty]private string _nci="";
    [ObservableProperty]private string _actualShares="";
    [ObservableProperty]private bool _capitalScopeConfirmed;
    [ObservableProperty]private string _capitalAsOf="";
    [ObservableProperty]private string _capitalNote="";
    [ObservableProperty]private string _modelOutput="No calculated model. All amounts and percentages must be explicitly entered; no WACC or tax assumption is inferred.";
    [ObservableProperty]private string _sensitivityWacc="";
    [ObservableProperty]private string _sensitivityGrowth="";
    [ObservableProperty]private string _driverGrowths="";
    [ObservableProperty]private string _targetPrice="";
    [ObservableProperty]private string _lowerGrowth="";
    [ObservableProperty]private string _upperGrowth="";
    [ObservableProperty]private string _tolerance="";
    [ObservableProperty]private string _screeningOutput="No evaluation. Rules are user-defined investigation prompts, not recommendations. Missing inputs never improve rank.";
    [ObservableProperty]private string _reportPreview="";
    [ObservableProperty]private string _reportPath="";
    [ObservableProperty]private string _reportFormat="CSV";
    [ObservableProperty]private string _monitoringOutput="Choose older/newer saved snapshots; evaluation is manual, never continuous background monitoring.";
    [ObservableProperty]private string _chartMetric="Annual revenue";
    [ObservableProperty]private string _chartCik="";
    [ObservableProperty]private string _chartDomain="No plotted observations.";
    [ObservableProperty]private string _chartLabel="No sourced historical plot. Historical valuation is unavailable without point-in-time data and rights.";
    public string EditingIdentity=>_editing is null?"New document; no saved revision selected for editing.":$"Editing {_editing.Id} from revision {_editing.Revision}. Saving creates a new immutable revision; stale edits fail without losing this draft.";
    partial void OnSelectedRevisionChanged(RevisionChoice? value){_report=null;ReportPreview="";}
    private async Task Run(Func<CancellationToken,Task> work)
    {
        if(IsBusy)return;IsBusy=true;_operation=new();
        try{await work(_operation.Token);}
        catch(OperationCanceledException){Status="Cancelled. Previous saved revisions and output files were preserved.";}
        catch(Exception ex)when(ex is ResearchStorageException or ArgumentException or OverflowException or IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
        {Status=ex is ReportFontException?ex.Message:ex is ResearchStorageException r?r.Message:"Operation failed. Check explicit inputs, supported bounds and a new writable output path. Draft and saved revisions retained.";}
        finally{_operation.Dispose();_operation=null;IsBusy=false;}
    }
    [RelayCommand]private void Cancel()=>_operation?.Cancel();
    public void Shutdown()=>_operation?.Cancel();
    private async Task Reload(CancellationToken token)
    {
        await _store.InitializeAsync(token);var revisions=await _store.ListDocumentRevisionsAsync(token);var snapshots=new List<SavedFinancialSnapshot>();
        foreach(var set in await _store.ListSetsAsync(true,token))snapshots.AddRange(await _store.ListSnapshotsAsync(set.Id,token));
        Revisions.Clear();foreach(var revision in revisions.OrderByDescending(r=>r.SavedAt))Revisions.Add(new(revision));
        Snapshots.Clear();foreach(var snapshot in snapshots.OrderByDescending(s=>s.CapturedAt))Snapshots.Add(new(snapshot));
    }
    [RelayCommand]private Task ReloadAsync()=>Run(async token=>{await Reload(token);Status="Local revisions and snapshots loaded. Unsaved editor fields were not replaced.";});
    [RelayCommand]private void LinkSnapshot(){if(SelectedSnapshot is null){Status="Choose an existing immutable snapshot to link.";return;}if(!LinkedSnapshots.Contains(SelectedSnapshot.Snapshot.Id)&&LinkedSnapshots.Count<20)LinkedSnapshots.Add(SelectedSnapshot.Snapshot.Id);}
    [RelayCommand]private void ClearLinks(){LinkedSnapshots.Clear();_conditionReviews.Clear();Status="Draft links and pending condition reviews cleared; saved history unchanged.";}
    private void ResetModelFields()
    {
        CapitalScopeConfirmed=false;CapitalAsOf="";
        IncludeExploration=false;SensitivityWacc=SensitivityGrowth=DriverGrowths=TargetPrice=LowerGrowth=UpperGrowth=Tolerance="";
        BaseRevenue=BaseWorkingCapital=BasisNote=Wacc=TerminalGrowth=NextFcff=TerminalPolicy=Cash=OtherAssets=Debt=Preferred=Nci=ActualShares=CapitalNote="";
        Years.Clear();Years.Add(new(1));Scenario="Base";
    }
    [RelayCommand]private void NewDocument()
    {
        if(IsBusy)return;_editing=null;SelectedRevision=null;DocumentTitle=Thesis=VariantView=Catalysts=Risks=Invalidation=Horizon=PeerNotes=Conclusion="";
        IncludeModel=IncludeRules=IncludeExploration=false;ResetModelFields();ClearLinks();SetRules(ResearchScreening.Defaults());ModelOutput="No model calculated.";OnPropertyChanged(nameof(EditingIdentity));Status="Editor cleared for a new document. Saved revisions unchanged; unsaved editor values discarded by your action.";
    }
    [RelayCommand]private void AddYear(){if(Years.Count<10)Years.Add(new(Years.Count+1));}
    [RelayCommand]private void RemoveYear(){if(Years.Count>1)Years.RemoveAt(Years.Count-1);}
    [RelayCommand]private void AddRule(){if(Rules.Count<20)Rules.Add(new(new(Guid.NewGuid(),"Net margin",">",0,1)));}
    [RelayCommand]private void RemoveRule(){if(Rules.Count>1)Rules.RemoveAt(Rules.Count-1);}
    private void SetRules(ResearchRuleSet set){Rules.Clear();foreach(var r in set.Rules)Rules.Add(new(r));}
    private static decimal Number(string text,string name){if(!decimal.TryParse(text,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))throw new ArgumentException(name+" requires an explicit invariant number.");return value;}
    private static string NumberText(decimal value)=>value.ToString("G29",CultureInfo.InvariantCulture);
    private ResearchRuleSet BuildRules(){var set=new ResearchRuleSet(ResearchScreening.Version,Rules.Select(r=>new ResearchRule(r.Id,r.Metric,r.Operator,Number(r.Threshold,"Rule threshold"),Number(r.Weight,"Rule weight"))).ToArray());ResearchScreening.Validate(set);return set;}
    private ResearchModel BuildModel()
    {
        var forecast=new ForecastInput(ForecastEngine.Version,Number(BaseRevenue,"Base revenue"),Number(BaseWorkingCapital,"Base OWC"),Years.Select(y=>new ForecastYearAssumption(Number(y.Growth,"Growth %")/100,Number(y.Margin,"Margin %")/100,Number(y.Tax,"Tax %")/100,Number(y.Depreciation,"D&A %")/100,Number(y.Capex,"Capex %")/100,Number(y.WorkingCapital,"OWC %")/100)).ToArray(),BasisNote);
        var dcf=new DcfAssumptions(DcfEngine.Version,Number(Wacc,"WACC %")/100,Number(TerminalGrowth,"Terminal growth %")/100,Number(NextFcff,"Next FCFF"),TerminalPolicy,new(Number(Cash,"Excess cash"),Number(OtherAssets,"Other assets"),Number(Debt,"Debt"),Number(Preferred,"Preferred"),Number(Nci,"NCI"),Number(ActualShares,"Actual shares"),CapitalNote,CapitalScopeConfirmed,DateOnly.TryParseExact(CapitalAsOf,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var reviewed)?reviewed:null));
        ModelExploration? exploration=null;
        if(IncludeExploration)
        {
            decimal[] List(string text)=>text.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(v=>Number(v,"Exploration percentage")/100).ToArray();
            var reverse=new[]{TargetPrice,LowerGrowth,UpperGrowth,Tolerance}.Any(v=>!string.IsNullOrWhiteSpace(v))?new ReverseAssumption(Number(TargetPrice,"Target"),Number(LowerGrowth,"Lower growth")/100,Number(UpperGrowth,"Upper growth")/100,Number(Tolerance,"Tolerance")):null;
            exploration=new(List(SensitivityWacc),List(SensitivityGrowth),List(DriverGrowths),reverse);
        }
        return new(Scenario,forecast,dcf){Exploration=exploration};
    }
    private ResearchDocumentDraft BuildDraft(bool copy)=>new(DocumentTitle,new(Thesis,VariantView,Catalysts,Risks,Invalidation,Horizon,PeerNotes,Conclusion),LinkedSnapshots.ToArray(),IncludeModel?BuildModel():null,IncludeRules?BuildRules():null,copy?null:_editing?.Id,copy?null:_editing?.Revision) { ConditionReviews = _conditionReviews.ToArray() };
    private Task Save(bool copy)=>Run(async token=>{var saved=await _store.SaveDocumentAsync(BuildDraft(copy),token);_editing=saved;OnPropertyChanged(nameof(EditingIdentity));await Reload(token);SelectedRevision=Revisions.Single(r=>r.Revision.Id==saved.Id&&r.Revision.Revision==saved.Revision);Status="Saved a new immutable research revision. Other history and editor fields retained.";});
    [RelayCommand]private Task SaveRevisionAsync()=>Save(false);
    [RelayCommand]private Task SaveNewCopyAsync()=>Save(true);
    [RelayCommand]private void LoadSelected()
    {
        if(IsBusy||SelectedRevision is null)return;var r=SelectedRevision.Revision;_editing=r;_conditionReviews.Clear();_conditionReviews.AddRange(r.ConditionReviews);DocumentTitle=r.Title;var t=r.Thesis;Thesis=t.Thesis;VariantView=t.VariantView;Catalysts=t.Catalysts;Risks=t.Risks;Invalidation=t.InvalidationConditions;Horizon=t.Horizon;PeerNotes=t.PeerComparability;Conclusion=t.Conclusion;LinkedSnapshots.Clear();foreach(var id in r.SnapshotIds)LinkedSnapshots.Add(id);IncludeModel=r.Model is not null;IncludeRules=r.Rules is not null;if(r.Rules is{} rules)SetRules(rules);else SetRules(ResearchScreening.Defaults());
        ModelOutput="No calculation for this loaded revision. Recalculate explicitly.";
        if(r.Model is null)ResetModelFields();
        if(r.Model is{} m)
        {
            IncludeExploration=m.Exploration is not null;
            if(m.Exploration is{} e)
            {
                string List(IReadOnlyList<decimal> values)=>string.Join(",",values.Select(v=>NumberText(v*100)));
                SensitivityWacc=List(e.WaccValues);SensitivityGrowth=List(e.TerminalGrowthValues);DriverGrowths=List(e.RevenueGrowthValues);
                TargetPrice=e.Reverse is{} reverse?NumberText(reverse.Target):"";LowerGrowth=e.Reverse is{} lower?NumberText(lower.Lower*100):"";UpperGrowth=e.Reverse is{} upper?NumberText(upper.Upper*100):"";Tolerance=e.Reverse is{} tolerance?NumberText(tolerance.Tolerance):"";
            }
            else SensitivityWacc=SensitivityGrowth=DriverGrowths=TargetPrice=LowerGrowth=UpperGrowth=Tolerance="";
            Scenario=m.Scenario;BaseRevenue=NumberText(m.Forecast.BaseRevenue);BaseWorkingCapital=NumberText(m.Forecast.BaseWorkingCapital);BasisNote=m.Forecast.BasisNote;Years.Clear();
            foreach(var y in m.Forecast.Years)Years.Add(new(Years.Count+1){Growth=NumberText(y.RevenueGrowth*100),Margin=NumberText(y.OperatingMargin*100),Tax=NumberText(y.TaxRate*100),Depreciation=NumberText(y.DepreciationRate*100),Capex=NumberText(y.CapexRate*100),WorkingCapital=NumberText(y.WorkingCapitalRate*100)});
            var d=m.Dcf;Wacc=NumberText(d.Wacc*100);TerminalGrowth=NumberText(d.TerminalGrowth*100);NextFcff=NumberText(d.NextFcff);TerminalPolicy=d.TerminalPolicy;Cash=NumberText(d.Capital.ExcessCash);OtherAssets=NumberText(d.Capital.OtherAssets);Debt=NumberText(d.Capital.Debt);Preferred=NumberText(d.Capital.Preferred);Nci=NumberText(d.Capital.Nci);ActualShares=NumberText(d.Capital.ActualShares);CapitalNote=d.Capital.ReviewNote;CapitalScopeConfirmed=d.Capital.SupportedSimpleDomesticNonFinancialUsd;CapitalAsOf=d.Capital.ReviewedAsOf?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"";
        }
        OnPropertyChanged(nameof(EditingIdentity));Status="Loaded the selected saved revision into the editor. Save a new copy for an independent scenario, or a new revision against the observed revision. No autosave.";
    }
    [RelayCommand]private Task ArchiveAsync()=>SetDeleted(true);
    [RelayCommand]private Task RecoverAsync()=>SetDeleted(false);
    private Task SetDeleted(bool deleted)=>Run(async token=>{if(SelectedRevision is not{} choice)throw new ArgumentException("Choose a saved revision first.");var r=choice.Revision;var saved=await _store.SaveDocumentAsync(new(r.Title,r.Thesis,r.SnapshotIds,r.Model,r.Rules,r.Id,r.Revision,deleted) { ConditionReviews = r.ConditionReviews },token);await Reload(token);SelectedRevision=Revisions.Single(c=>c.Revision.Id==saved.Id&&c.Revision.Revision==saved.Revision);Status="Archive/recovery recorded as a new revision. Editor draft unchanged; load latest before editing.";});
    [RelayCommand]private Task CalculateAsync()=>Run(async token=>{var model=BuildModel();var result=await Task.Run(()=>DcfEngine.Calculate(model.Forecast,model.Dcf,token),token);var sb=new StringBuilder("Last calculated assumption snapshot; edits require recalculation. No current-price upside.\n");foreach(var y in result.Years)sb.AppendLine(FormattableString.Invariant($"Year {y.Forecast.Year}: revenue {y.Forecast.Revenue:G}; EBIT {y.Forecast.Ebit:G}; tax {y.Forecast.Tax:G}; NOPAT {y.Forecast.Nopat:G}; D&A {y.Forecast.Depreciation:G}; capex {y.Forecast.Capex:G}; ΔOWC {y.Forecast.ChangeWorkingCapital:G}; FCFF {y.Forecast.Fcff:G}; factor {y.DiscountFactor:G}; PV {y.PresentValue:G}"));sb.AppendLine(FormattableString.Invariant($"Terminal PV {result.TerminalPresentValue:G}; contribution {result.TerminalContribution:G}%\nEV {result.EnterpriseValue:G}; equity {result.EquityValue:G}; per actual share {result.PerShare:G} USD\n{model.Forecast.Version} / {model.Dcf.Version}; {model.Scenario}, user label only."));ModelOutput=sb.ToString();Status="Calculated explicit forecast/DCF. Save a research revision to retain assumptions and export frozen output.";});
    [RelayCommand]private Task SensitivityAsync()=>Run(async token=>{var model=BuildModel();decimal[] ParseList(string text)=>text.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(v=>Number(v,"Sensitivity %")/100).ToArray();var w=ParseList(SensitivityWacc);var g=ParseList(SensitivityGrowth);var cells=await Task.Run(()=>DcfEngine.Sensitivity(model,w,g,token),token);ModelOutput="WACC/growth grid; other assumptions and terminal next-FCFF fixed. No probabilities.\n"+string.Join("\n",cells.Select(c=>FormattableString.Invariant($"WACC {c.Wacc*100}% / g {c.Growth*100}%: {c.PerShare?.ToString("G29",CultureInfo.InvariantCulture)??c.Reason}")));Status="Bounded sensitivity evaluated. Invalid cells remain unavailable.";});
    [RelayCommand]private Task RevenueSensitivityAsync()=>Run(async token=>{var model=BuildModel();var growth=DriverGrowths.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Select(v=>Number(v,"Revenue growth %")/100).ToArray();var result=await Task.Run(()=>DcfEngine.RevenueSensitivity(model,growth,token),token);ModelOutput="Constant explicit-year revenue-growth sensitivity; all other assumptions, including terminal next-FCFF, fixed.\n"+string.Join("\n",result.Select(r=>FormattableString.Invariant($"Growth {r.Growth*100}%: {r.PerShare?.ToString("G29",CultureInfo.InvariantCulture)??r.Reason}")));Status="Bounded revenue-driver sensitivity complete.";});
    [RelayCommand]private Task ReverseAsync()=>Run(async token=>{var model=BuildModel();var target=Number(TargetPrice,"Target assumption");var lo=Number(LowerGrowth,"Lower growth %")/100;var hi=Number(UpperGrowth,"Upper growth %")/100;var tolerance=Number(Tolerance,"Absolute price tolerance");var result=await Task.Run(()=>DcfEngine.ReverseGrowth(model,target,lo,hi,tolerance,100,token),token);ModelOutput=FormattableString.Invariant($"User target assumption {target}; bounds {lo*100}%..{hi*100}%\nState {result.State}; growth {result.Growth*100}%\nForward price {result.ForwardPrice}; residual {result.Residual}; evaluations {result.Evaluations}\n{result.Explanation}\nFixed WACC {model.Dcf.Wacc}; terminal next-FCFF {model.Dcf.NextFcff}; actual shares {model.Dcf.Capital.ActualShares}; cash {model.Dcf.Capital.ExcessCash}; other assets {model.Dcf.Capital.OtherAssets}; debt {model.Dcf.Capital.Debt}; preferred {model.Dcf.Capital.Preferred}; NCI {model.Dcf.Capital.Nci}.");Status="Reverse-model evaluation complete; no fetched/current quote used.";});
    [RelayCommand]private void EvaluateRules()
    {
        try
        {
            var set=BuildRules();var snapshot=SelectedSnapshot?.Snapshot;var rows=snapshot is null?_rows():snapshot.Companies.Select(c=>new AlphaQuant.Core.ComparisonRow(c.Company,c.Fundamentals,null,MarketConnectionState.Disconnected,null,c.FactsRetrievedAt,c.FactsStale){FinancialDefinitionVersion=c.DefinitionVersion??snapshot.DefinitionVersion}).ToArray();
            var results=rows.Select(r=>ResearchScreening.Evaluate(r,set)).OrderByDescending(r=>r.Score.HasValue).ThenByDescending(r=>r.Score).ThenBy(r=>r.Cik).ToArray();
            ScreeningOutput=(snapshot is null?"Current in-memory financial snapshot":"Dated frozen snapshot; NOT a current market ranking")+"\nInvestigation prompts, not recommendations. Correlated conditions can overlap; missing data never improves score.\n"+string.Join("\n\n",results.Select(r=>$"CIK {r.Cik:D10}: coverage {r.Available}/{r.Total}; score {r.Score?.ToString("G29",CultureInfo.InvariantCulture)??"unavailable"}\n"+string.Join("\n",r.Observations.Select(o=>FormattableString.Invariant($"{o.Rule.Metric}: {o.Input} {o.Rule.Operator} {o.Rule.Threshold}; weight {o.Rule.Weight}; period {o.PeriodEnd:yyyy-MM-dd}; trigger {o.Triggered?.ToString()??"unknown"} {o.Reason}")))));
            Status="Rules evaluated only on request. Check source dates, coverage and every component before interpreting a score.";
        }
        catch(Exception ex)when(ex is ArgumentException or OverflowException){Status="Rule evaluation failed. Review explicit thresholds, metric names, operators and weights.";}
    }
    [RelayCommand]private void RecordConditions()
    {
        try
        {
            if(SelectedSnapshot?.Snapshot is not{} snapshot||_conditionReviews.Count>=20)throw new ArgumentException("Select a snapshot; at most 20 recorded reviews per document.");
            var rules=BuildRules();var results=snapshot.Companies.Select(c=>ResearchScreening.Evaluate(new AlphaQuant.Core.ComparisonRow(c.Company,c.Fundamentals,null,MarketConnectionState.Disconnected,null,c.FactsRetrievedAt,c.FactsStale),rules)).ToArray();
            if(!LinkedSnapshots.Contains(snapshot.Id))LinkedSnapshots.Add(snapshot.Id);
            _conditionReviews.Add(new(snapshot.Id,DateTimeOffset.UtcNow,results));
            Status="Recorded a dated snapshot condition review in this draft. Save a new revision to retain it. No background reminder or notification was scheduled.";
        }
        catch(ArgumentException){Status="Condition review not recorded. Choose a saved snapshot and valid bounded rules.";}
    }
    [RelayCommand]private void PreviewReport()
    {
        try{if(SelectedRevision is null)throw new ArgumentException("Choose a saved revision.");_report=ResearchReport.Capture(SelectedRevision.Revision,Snapshots.Select(s=>s.Snapshot).ToArray());ReportPreview=$"Frozen saved revision: {SelectedRevision.Label}\n"+string.Join("\n",_report.Lines.Select(l=>$"{l.Section} / {l.Label}: {l.Value} {l.Unit}\n{l.Basis}\n{l.Source}"));Status="Frozen report preview ready. Export uses this displayed saved revision, never unsaved editor changes.";}
        catch(ArgumentException){_report=null;ReportPreview="";Status="Report preview failed. Choose a saved revision with all linked snapshots loaded.";}
    }
    [RelayCommand]private Task ExportReportAsync()=>Run(async token=>{if(_report is null)throw new ArgumentException("Preview the saved report first.");var report=_report;var format=ReportFormat;var path=ReportPath;await Task.Run(()=>ResearchReportExporter.SaveNewAsync(path,report,format,token),token);Status="Frozen report saved locally. No source research modified; nothing uploaded or sent.";});
    [RelayCommand]private void CompareSnapshots()
    {try{if(OlderSnapshot is null||SelectedSnapshot is null)throw new ArgumentException("Select snapshots.");MonitoringOutput=string.Join("\n",ResearchMonitoring.Compare(OlderSnapshot.Snapshot,SelectedSnapshot.Snapshot).Select(c=>$"CIK {c.Cik:D10} / {c.Metric}: {c.Kind} — {c.Explanation}"));Status="Manual snapshot comparison only; no background monitor or source request.";}catch(ArgumentException){Status="Choose older/newer snapshots in capture-time order.";}}
    [RelayCommand]private void PlotHistory()
    {
        ChartSegments.Clear();ChartDomain="No plotted observations.";
        if(SelectedSnapshot?.Snapshot is not{} snapshot||!long.TryParse(ChartCik,out var cik)||snapshot.Companies.FirstOrDefault(c=>c.Company.Cik==cik) is not{} company){ChartLabel="Choose a saved snapshot and its issuer CIK for sourced history.";return;}
        try
        {
            var rows=ResearchMonitoring.History(company,ChartMetric);var available=rows.Where(r=>r.Value.HasValue).ToArray();if(available.Length==0){ChartLabel="No supported observations; missing history is not zero.";return;}
            var min=available.Min(r=>r.Value!.Value);var max=available.Max(r=>r.Value!.Value);var first=rows.Min(r=>r.End.DayNumber);var last=rows.Max(r=>r.End.DayNumber);var points=new Points();
            foreach(var r in rows){if(r.Value is null){if(points.Count>0)ChartSegments.Add(new(points));points=new();continue;}var x=20+(r.End.DayNumber-first)*500.0/Math.Max(1,last-first);var y=160-(double)((r.Value.Value-min)/(max==min?1:max-min))*140;points.Add(new(x,y));}if(points.Count>0)ChartSegments.Add(new(points));
            ChartDomain=FormattableString.Invariant($"Last plot: {company.Company.Ticker} / {ChartMetric}; Y {min:G}..{max:G} {available[0].Unit}; X {rows.First().End:yyyy-MM-dd}..{rows.Last().End:yyyy-MM-dd}. Missing observations form gaps.");
            ChartLabel=FormattableString.Invariant($"{company.Company.Ticker} / {ChartMetric}; {company.DefinitionVersion??snapshot.DefinitionVersion}\nY-axis {min:G}..{max:G} {available[0].Unit}; X-axis {rows.First().End:yyyy-MM-dd}..{rows.Last().End:yyyy-MM-dd}. Gaps are not connected.\n")+string.Join("\n",rows.Select(r=>$"{r.End:yyyy-MM-dd}: {r.Value?.ToString("G29",CultureInfo.InvariantCulture)??"unavailable"} {r.Unit}; {r.Source}"));Status="Plot contains saved sourced fundamentals only. Historical valuation remains blocked on point-in-time data and rights.";
        }
        catch(ArgumentException){ChartLabel="Choose a supported historical metric.";}
    }
}
