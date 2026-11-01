using System.Text.Json;
using System.Text.Json.Nodes;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
namespace AlphaQuant.Tests;
public class ReviewedEvidenceTests
{
    private static string Valid() => JsonSerializer.Serialize(new {
        Version = 1,
        Capital = new[] { new CapitalStructureEvidence(1,"ALDR",new(2026,11,1),new(2026,9,30),100m,20m,5m,0m,0m,"SEC filing reference","Reviewer record",new(2026,11,1),true,true) },
        Equity = new[] { new EquityBasisEvidence(1,"ALDR",new(2026,11,1),"Reviewed EPS and splits",true) }
    });
    [Fact] public void ExplicitReviewedZeroClaimsAreAcceptedAndSessionCanBeCleared()
    {
        var store=new ReviewedEvidenceStore(); store.Load(Valid());
        Assert.Equal(0m,store.Capital[1].PreferredEquity); Assert.Single(store.Equity);
        store.Clear(); Assert.Empty(store.Capital); Assert.Empty(store.Equity);
    }
    [Theory][InlineData("Debt")][InlineData("Cash")][InlineData("PreferredEquity")][InlineData("NonControllingInterests")][InlineData("SupportedDomesticNonFinancialUsGaapUsd")]
    public void MissingReviewFieldsNeverDefaultToZeroOrApproval(string field)
    {
        var json=JsonNode.Parse(Valid())!; json["Capital"]![0]!.AsObject().Remove(field);
        Assert.Throws<ArgumentException>(()=>new ReviewedEvidenceStore().Load(json.ToJsonString()));
    }
    [Fact] public void InvalidReplacementPreservesBothPreviousReviewSets()
    {
        var store=new ReviewedEvidenceStore(); store.Load(Valid());
        var original=store.Capital; var json=JsonNode.Parse(Valid())!;
        json["Equity"]![0]!["ReviewReference"]="";
        Assert.Throws<ArgumentException>(()=>store.Load(json.ToJsonString())); Assert.Same(original,store.Capital); Assert.Single(store.Equity);
    }
    [Fact] public void DuplicateCompanyAndOversizedInputAreRejected()
    {
        var json=JsonNode.Parse(Valid())!; json["Capital"]!.AsArray().Add(json["Capital"]![0]!.DeepClone());
        Assert.Throws<ArgumentException>(()=>new ReviewedEvidenceStore().Load(json.ToJsonString()));
        Assert.Throws<ArgumentException>(()=>new ReviewedEvidenceStore().Load(new string(' ',65537)));
    }
}
