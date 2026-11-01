using System.Text;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;

namespace AlphaQuant.Tests;

public sealed class ComparisonParserTests
{
    [Fact]
    public void ParsesQuarterlyCashFlowAndPerShareUnitsWithoutLosingSourceLineage()
    {
        var json = """
        {"entityName":"Synthetic","facts":{"us-gaap":{
          "OperatingIncomeLoss":{"units":{"USD":[{"val":20,"start":"2026-01-01","end":"2026-03-31","filed":"2026-05-01","form":"10-Q","accn":"fixture-op"}]}},
          "EarningsPerShareDiluted":{"units":{"USD/shares":[{"val":0.75,"start":"2026-01-01","end":"2026-03-31","filed":"2026-05-01","form":"10-Q","accn":"fixture-eps"}]}},
          "NetCashProvidedByUsedInOperatingActivities":{"units":{"USD":[{"val":30,"start":"2026-01-01","end":"2026-03-31","filed":"2026-05-01","form":"10-Q","accn":"fixture-cf"}]}},
          "UnreviewedCustomMetric":{"units":{"USD":[{"val":999,"end":"2026-03-31","filed":"2026-05-01","form":"10-Q","accn":"not-allowed"}]}}
        }}}
        """;
        var parsed = SecParser.ParseFacts(Encoding.UTF8.GetBytes(json));
        Assert.Equal(3, parsed.Facts.Count);
        var eps = Assert.Single(parsed.Facts, f => f.Concept == ComparisonConcepts.DilutedEps);
        Assert.Equal(.75m, eps.Value);
        Assert.Equal("USD/shares", eps.Unit);
        Assert.Equal("fixture-eps", eps.Accession);
        Assert.DoesNotContain(parsed.Facts, f => f.Concept == "UnreviewedCustomMetric");
    }
}
