namespace AlphaQuant.Core;
public sealed record ForecastYearAssumption(decimal RevenueGrowth,decimal OperatingMargin,decimal TaxRate,decimal DepreciationRate,decimal CapexRate,decimal WorkingCapitalRate);
public sealed record ForecastInput(string Version,decimal BaseRevenue,decimal BaseWorkingCapital,IReadOnlyList<ForecastYearAssumption> Years,string BasisNote);
public sealed record ForecastYear(int Year,decimal Revenue,decimal Ebit,decimal Tax,decimal Nopat,decimal Depreciation,decimal Capex,decimal WorkingCapital,decimal ChangeWorkingCapital,decimal Fcff);
public sealed record CapitalBridge(decimal ExcessCash,decimal OtherAssets,decimal Debt,decimal Preferred,decimal Nci,decimal ActualShares,string ReviewNote);
public sealed record DcfAssumptions(string Version,decimal Wacc,decimal TerminalGrowth,decimal NextFcff,string TerminalPolicy,CapitalBridge Capital);
public sealed record DiscountedYear(ForecastYear Forecast,decimal DiscountFactor,decimal PresentValue);
public sealed record DcfResult(string Version,IReadOnlyList<DiscountedYear> Years,decimal TerminalValue,decimal TerminalPresentValue,decimal EnterpriseValue,decimal EquityValue,decimal PerShare,decimal? TerminalContribution);
public sealed record ReverseAssumption(decimal Target,decimal Lower,decimal Upper,decimal Tolerance,int Iterations=100);
public sealed record ModelExploration(IReadOnlyList<decimal> WaccValues,IReadOnlyList<decimal> TerminalGrowthValues,IReadOnlyList<decimal> RevenueGrowthValues,ReverseAssumption? Reverse);
public sealed record ResearchModel(string Scenario,ForecastInput Forecast,DcfAssumptions Dcf)
{ public ModelExploration? Exploration { get; init; } }
public sealed record ExplorationResult(IReadOnlyList<SensitivityCell> DiscountGrid,IReadOnlyList<RevenueSensitivityCell> RevenueDriver,ReverseDcfResult? Reverse);
public static class ResearchExploration
{
    public static ExplorationResult Replay(ResearchModel model,CancellationToken token=default)
    {
        if(model.Exploration is not{} e)return new([],[],null);
        if(e.WaccValues is null||e.TerminalGrowthValues is null||e.RevenueGrowthValues is null||e.WaccValues.Count>100||e.TerminalGrowthValues.Count>100||e.RevenueGrowthValues.Count>100||((e.WaccValues.Count==0)!=(e.TerminalGrowthValues.Count==0)))throw new ArgumentException("Exploration lists must be bounded and discount grids need both axes.");
        var grid=e.WaccValues.Count==0?[]:DcfEngine.Sensitivity(model,e.WaccValues,e.TerminalGrowthValues,token);
        var driver=e.RevenueGrowthValues.Count==0?[]:DcfEngine.RevenueSensitivity(model,e.RevenueGrowthValues,token);
        var reverse=e.Reverse is{} r?DcfEngine.ReverseGrowth(model,r.Target,r.Lower,r.Upper,r.Tolerance,r.Iterations,token):null;
        return new(grid,driver,reverse);
    }
}
public sealed record SensitivityCell(decimal Wacc,decimal Growth,decimal? PerShare,string? Reason);
public sealed record RevenueSensitivityCell(decimal Growth,decimal? PerShare,string? Reason);
public sealed record ReverseDcfResult(string State,decimal? Growth,decimal? ForwardPrice,decimal? Residual,int Evaluations,string Explanation);
public static class ForecastEngine
{
    public const string Version="forecast-1";
    public static IReadOnlyList<ForecastYear> Calculate(ForecastInput input,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(input.Version!=Version||input.BaseRevenue<0||string.IsNullOrWhiteSpace(input.BasisNote)||input.BasisNote.Length>2000||input.Years is null||input.Years.Count is <1 or >10)throw new ArgumentException("Use forecast-1, nonnegative base revenue, 1–10 explicit years and a basis/source note.");
        var revenue=input.BaseRevenue;var working=input.BaseWorkingCapital;var result=new List<ForecastYear>();
        foreach(var year in input.Years)
        {
            token.ThrowIfCancellationRequested();
            if(year is null||year.RevenueGrowth is <-1 or >5||year.OperatingMargin is <-1 or >1||year.TaxRate is <0 or >1||year.DepreciationRate is <0 or >1||year.CapexRate is <0 or >1||year.WorkingCapitalRate is <-1 or >1)throw new ArgumentException("Forecast assumptions are outside forecast-1 bounds; percentages use decimal fractions.");
            revenue*=1+year.RevenueGrowth;var ebit=revenue*year.OperatingMargin;var tax=Math.Max(ebit,0)*year.TaxRate;var depreciation=revenue*year.DepreciationRate;var capex=revenue*year.CapexRate;var currentWorking=revenue*year.WorkingCapitalRate;var change=currentWorking-working;
            result.Add(new(result.Count+1,revenue,ebit,tax,ebit-tax,depreciation,capex,currentWorking,change,ebit-tax+depreciation-capex-change));working=currentWorking;
        }
        return result;
    }
}
public static class DcfEngine
{
    public const string Version="dcf-1";
    public static DcfResult Calculate(ForecastInput forecast,DcfAssumptions input,CancellationToken token=default)=>Discount(ForecastEngine.Calculate(forecast,token),input,token);
    public static DcfResult Discount(IReadOnlyList<ForecastYear> forecast,DcfAssumptions input,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(forecast);
        var c=input.Capital;
        if(input.Version!=Version||input.Wacc is <=0 or >1||input.TerminalGrowth<=-1||input.Wacc-input.TerminalGrowth<.005m||string.IsNullOrWhiteSpace(input.TerminalPolicy)||input.TerminalPolicy.Length>2000||c is null||new[]{c.ExcessCash,c.OtherAssets,c.Debt,c.Preferred,c.Nci}.Any(v=>v<0)||c.ActualShares<=0||string.IsNullOrWhiteSpace(c.ReviewNote)||c.ReviewNote.Length>2000||forecast.Count is <1 or >10)throw new ArgumentException("Explicit dcf-1 assumptions, sustainable terminal policy, valid discount/growth gap and reviewed nonnegative capital/positive actual shares are required.");
        var years=new List<DiscountedYear>();var factor=1m;
        for(var i=0;i<forecast.Count;i++)
        {
            token.ThrowIfCancellationRequested();if(forecast[i].Year!=i+1)throw new ArgumentException("Use sequential end-year cash flows.");factor/=1+input.Wacc;years.Add(new(forecast[i],factor,forecast[i].Fcff*factor));
        }
        var terminal=input.NextFcff/(input.Wacc-input.TerminalGrowth);var pv=terminal*factor;var ev=years.Sum(y=>y.PresentValue)+pv;var equity=ev+c.ExcessCash+c.OtherAssets-c.Debt-c.Preferred-c.Nci;
        return new(Version,years,terminal,pv,ev,equity,equity/c.ActualShares,ev==0?null:pv/ev*100);
    }
    public static IReadOnlyList<SensitivityCell> Sensitivity(ResearchModel model,IReadOnlyList<decimal> wacc,IReadOnlyList<decimal> growth,CancellationToken token=default)
    {
        if(wacc.Count==0||growth.Count==0||(long)wacc.Count*growth.Count>100)throw new ArgumentException("Use 1–100 total sensitivity cells.");
        var result=new List<SensitivityCell>();
        foreach(var w in wacc)foreach(var g in growth)
        {
            token.ThrowIfCancellationRequested();
            try{result.Add(new(w,g,Calculate(model.Forecast,model.Dcf with{Wacc=w,TerminalGrowth=g},token).PerShare,null));}
            catch(Exception ex)when(ex is ArgumentException or OverflowException){result.Add(new(w,g,null,"Invalid or overflowing dcf-1 assumptions."));}
        }
        return result;
    }
    public static IReadOnlyList<RevenueSensitivityCell> RevenueSensitivity(ResearchModel model,IReadOnlyList<decimal> growth,CancellationToken token=default)
    {
        if(growth.Count is <1 or >100)throw new ArgumentException("Use 1–100 growth-driver evaluations.");
        var result=new List<RevenueSensitivityCell>();
        foreach(var value in growth)
        {
            token.ThrowIfCancellationRequested();
            try{result.Add(new(value,Calculate(model.Forecast with{Years=model.Forecast.Years.Select(y=>y with{RevenueGrowth=value}).ToArray()},model.Dcf,token).PerShare,null));}
            catch(Exception ex)when(ex is ArgumentException or OverflowException){result.Add(new(value,null,"Invalid/overflowing constant-growth assumptions."));}
        }
        return result;
    }
    public static ReverseDcfResult ReverseGrowth(ResearchModel model,decimal target,decimal lower,decimal upper,decimal tolerance,int iterations=100,CancellationToken token=default)
    {
        if(target<=0||lower<-1||upper>5||lower>=upper||tolerance<=0||iterations is <1 or >200)throw new ArgumentException("Use positive target/tolerance, finite ordered growth bounds and 1–200 iterations.");
        var evaluations=0;
        decimal Price(decimal g){token.ThrowIfCancellationRequested();evaluations++;return Calculate(model.Forecast with{Years=model.Forecast.Years.Select(y=>y with{RevenueGrowth=g}).ToArray()},model.Dcf,token).PerShare;}
        try
        {
            var samples=Enumerable.Range(0,65).Select(i=>Price(lower+(upper-lower)*i/64)).ToArray();
            var signs=samples.Zip(samples.Skip(1),(a,b)=>Math.Sign(b-a)).Distinct().ToArray();
            if(signs.Length!=1||signs[0]==0)return new("uncertain",null,null,null,evaluations,"Sampled path is flat or non-monotonic; no unique solution claim.");
            // Prove strict monotonicity conservatively with interval bounds on
            // the exact polynomial derivative in x=1+growth. Sampling alone is
            // insufficient for uniqueness between grid points.
            var coefficients=new decimal[model.Forecast.Years.Count+1]; var discount=1m;
            for(var i=0;i<model.Forecast.Years.Count;i++)
            {
                var y=model.Forecast.Years[i];discount/=1+model.Dcf.Wacc;
                coefficients[i+1]+=model.Forecast.BaseRevenue*(y.OperatingMargin-Math.Max(y.OperatingMargin,0)*y.TaxRate+y.DepreciationRate-y.CapexRate-y.WorkingCapitalRate)*discount;
                if(i>0)coefficients[i]+=model.Forecast.BaseRevenue*model.Forecast.Years[i-1].WorkingCapitalRate*discount;
            }
            decimal Power(decimal v,int n){var result=1m;for(var i=0;i<n;i++)result*=v;return result;}
            var minDerivative=0m;var maxDerivative=0m;
            for(var i=1;i<coefficients.Length;i++)
            {
                var a=i*coefficients[i]*Power(1+lower,i-1);var b=i*coefficients[i]*Power(1+upper,i-1);
                minDerivative+=Math.Min(a,b);maxDerivative+=Math.Max(a,b);
            }
            if(minDerivative<=0&&maxDerivative>=0)return new("uncertain",null,null,null,evaluations,"Derivative interval cannot prove strict monotonicity throughout bounds; no unique solution claim.");
            var increasing=minDerivative>0;
            if(target<samples.Min()||target>samples.Max())return new("no-solution",null,null,null,evaluations,"Target outside model values within supplied bounds.");
            var lo=lower;var hi=upper;
            for(var i=0;i<iterations;i++)
            {
                var mid=lo+(hi-lo)/2;var value=Price(mid);var residual=value-target;
                if(Math.Abs(residual)<=tolerance){var check=Price(mid);return new("candidate",mid,check,check-target,evaluations,"Strict monotonicity proved by derivative interval on supplied bounds and checked at 65 points; unique candidate forward-reconciled within tolerance. Other assumptions, including terminal next-FCFF, fixed.");}
                if((value<target)==increasing)lo=mid;else hi=mid;
                if(lo==hi)break;
            }
            return new("not-converged",null,null,null,evaluations,"Tolerance not reached within evaluation limit.");
        }
        catch(Exception ex)when(ex is ArgumentException or OverflowException){return new("invalid",null,null,null,evaluations,"Invalid/overflowing model in requested bounds; no solution supplied.");}
    }
}
