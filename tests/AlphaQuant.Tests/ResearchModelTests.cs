using AlphaQuant.Core;
namespace AlphaQuant.Tests;
public sealed class ResearchModelTests
{
    public static ForecastInput Input()=>new(ForecastEngine.Version,1000,100,[new(.1m,.2m,.25m,.02m,.03m,.1m),new(.1m,.2m,.25m,.02m,.03m,.1m)],"Synthetic historical basis; explicit assumptions");
    public static DcfAssumptions Dcf()=>new(DcfEngine.Version,.1m,.03m,124.63m,"Explicit sustainable year N+1 FCFF; fixture policy",new(20,0,100,10,5,100,"Explicit synthetic actual shares and claims",true,new(2026,3,1)));
    [Fact]public void Independent_forecast_bridge_and_loss_tax_policy()
    {
        var years=ForecastEngine.Calculate(Input());Assert.Equal(1100,years[0].Revenue);Assert.Equal(220,years[0].Ebit);Assert.Equal(55,years[0].Tax);Assert.Equal(144,years[0].Fcff);Assert.Equal(158.4m,years[1].Fcff);
        var loss=ForecastEngine.Calculate(Input() with{Years=[new(0,-.1m,.25m,0,0,0)]});Assert.Equal(0,loss[0].Tax);Assert.Equal(0,loss[0].Revenue-1000);Assert.Equal(-100,loss[0].Nopat);
    }
    [Fact]public void Independent_rational_dcf_bridge_and_scaling()
    {
        var flows=new[]{new ForecastYear(1,0,0,0,0,0,0,0,0,110),new ForecastYear(2,0,0,0,0,0,0,0,0,121)};
        var result=DcfEngine.Discount(flows,Dcf());Assert.InRange(decimal.Abs(result.EnterpriseValue-11700m/7),0,.00000000000000000001m);Assert.InRange(decimal.Abs(result.PerShare-2207m/140),0,.00000000000000000001m);
        var split=DcfEngine.Discount(flows,Dcf() with{Capital=Dcf().Capital with{ActualShares=200}});Assert.Equal(result.PerShare/2,split.PerShare);
        var negative=DcfEngine.Discount(flows,Dcf() with{Capital=Dcf().Capital with{Debt=10000}});Assert.True(negative.EquityValue<0);Assert.True(negative.PerShare<0);
    }
    [Fact]public void Invalid_horizons_terminal_gap_and_overflow_are_not_results()
    {
        Assert.Throws<ArgumentException>(()=>ForecastEngine.Calculate(Input() with{Years=[]}));
        Assert.Throws<ArgumentException>(()=>DcfEngine.Calculate(Input(),Dcf() with{TerminalGrowth=.099m}));
        Assert.Throws<OverflowException>(()=>ForecastEngine.Calculate(Input() with{BaseRevenue=decimal.MaxValue}));
        using var c=new CancellationTokenSource();c.Cancel();Assert.ThrowsAny<OperationCanceledException>(()=>ForecastEngine.Calculate(Input(),c.Token));
    }
    [Fact]public void Sensitivity_is_bounded_and_invalid_cells_remain_invalid()
    {
        var model=new ResearchModel("Base",Input(),Dcf());var cells=DcfEngine.Sensitivity(model,[.1m,.02m],[.03m,.01m]);Assert.Equal(4,cells.Count);Assert.Contains(cells,c=>c.PerShare is null&&c.Reason is not null);
        Assert.Throws<ArgumentException>(()=>DcfEngine.Sensitivity(model,Enumerable.Repeat(.1m,11).ToArray(),Enumerable.Repeat(.03m,10).ToArray()));
    }
    [Fact]public void Reverse_growth_reconciles_target_and_rejects_flat_or_unbounded_claims()
    {
        var model=new ResearchModel("Base",Input(),Dcf());var target=DcfEngine.Calculate(Input(),Dcf()).PerShare;
        var solved=DcfEngine.ReverseGrowth(model,target,0,.2m,.0000001m);Assert.Equal("candidate",solved.State);Assert.InRange(Math.Abs(solved.Residual!.Value),0,.0000001m);Assert.InRange(solved.Evaluations,65,267);
        var flat=model with{Forecast=Input() with{BaseRevenue=0,BaseWorkingCapital=0}};
        Assert.Equal("uncertain",DcfEngine.ReverseGrowth(flat,target,0,.2m,.01m).State);
        Assert.Equal("no-solution",DcfEngine.ReverseGrowth(model,100000,0,.2m,.01m).State);
    }
    [Fact]public void Nonmonotonic_reverse_paths_and_cancelled_sensitivity_do_not_claim_solution()
    {
        var mixed=Input() with{Years=[new(0,-.2m,0,0,0,0),new(0,.2m,0,0,0,0)]};
        var model=new ResearchModel("Base",mixed,Dcf() with{NextFcff=0});
        Assert.Equal("uncertain",DcfEngine.ReverseGrowth(model,1,-.9m,1,.001m).State);
        using var c=new CancellationTokenSource();c.Cancel();Assert.ThrowsAny<OperationCanceledException>(()=>DcfEngine.RevenueSensitivity(model,[0,.1m],c.Token));
        var driver=DcfEngine.RevenueSensitivity(new("Base",Input(),Dcf()),[.1m,.2m]);Assert.Equal(DcfEngine.Calculate(Input(),Dcf()).PerShare,driver[0].PerShare);Assert.True(driver[1].PerShare>driver[0].PerShare);
    }
    [Fact]public void New_dcf_requires_supported_scope_and_dated_claims_while_legacy_replay_is_labelled()
    {
        var unreviewed=Dcf() with{Capital=Dcf().Capital with{SupportedSimpleDomesticNonFinancialUsd=false}};
        Assert.Throws<ArgumentException>(()=>DcfEngine.Calculate(Input(),unreviewed));
        Assert.Throws<ArgumentException>(()=>DcfEngine.Calculate(Input(),Dcf() with{Capital=Dcf().Capital with{ReviewedAsOf=DateOnly.MaxValue}}));
        var old=unreviewed with{Version=DcfEngine.LegacyVersion};var replay=DcfEngine.Calculate(Input(),old);Assert.Equal(DcfEngine.LegacyVersion,replay.Version);
    }
    [Fact]public void Missing_rule_inputs_never_improve_rank_and_weights_are_visible()
    {
        var rule=new ResearchRule(Guid.NewGuid(),"Net margin",">",5,2);var rules=new ResearchRuleSet(ResearchScreening.Version,[rule]);
        var f=ComparisonAnalyzer.Analyze([new("Revenues","USD",100,new(2025,1,1),new(2025,12,31),"10-K",new(2026,2,1),"0000000001-26-000001"),new("NetIncomeLoss","USD",10,new(2025,1,1),new(2025,12,31),"10-K",new(2026,2,1),"0000000001-26-000001")],new(2026,3,1));
        var row=new ComparisonRow(new(1,"AAA","Synthetic"),f,null,MarketConnectionState.Disconnected,null,DateTimeOffset.UtcNow,false);
        Assert.Equal(100,ResearchScreening.Evaluate(row,rules).Score);
        Assert.Null(ResearchScreening.Evaluate(row with{FactsStale=true},rules).Score);
        var missing=rules with{Rules=[rule with{Metric="Interest coverage"}]};var result=ResearchScreening.Evaluate(row,missing);Assert.Null(result.Score);Assert.Null(result.Observations[0].Triggered);
    }
}
