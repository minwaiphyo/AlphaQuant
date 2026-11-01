using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AlphaQuant.Core;

namespace AlphaQuant.Desktop.Services;

/// <summary>Explicit analyst-supplied evidence, held only in memory. Parsing does not certify its accuracy.</summary>
public sealed class ReviewedEvidenceStore
{
    public IReadOnlyDictionary<long, CapitalStructureEvidence> Capital { get; private set; } = new Dictionary<long, CapitalStructureEvidence>();
    public IReadOnlyDictionary<long, EquityBasisEvidence> Equity { get; private set; } = new Dictionary<long, EquityBasisEvidence>();
    public void Clear() { Capital = new Dictionary<long, CapitalStructureEvidence>(); Equity = new Dictionary<long, EquityBasisEvidence>(); }
    public void Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 65536) throw new ArgumentException("Provide an evidence document under 64 KiB.");
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 12 });
        var root = doc.RootElement;
        if (root.GetProperty("Version").GetInt32() != 1) throw new ArgumentException("Unsupported evidence version.");
        var capital = Parse<CapitalStructureEvidence>("Capital", ["Cik", "Symbol", "SharesDate", "BalanceSheetDate", "OutstandingShares", "Debt", "Cash", "PreferredEquity", "NonControllingInterests", "FilingReference", "ReviewReference", "SplitAdjustedThrough", "SimpleSingleClass", "SupportedDomesticNonFinancialUsGaapUsd"]);
        var equity = Parse<EquityBasisEvidence>("Equity", ["Cik", "Symbol", "SplitAdjustedThrough", "ReviewReference", "SupportedSimpleDomesticNonFinancialUsGaapUsd"]);
        // Build both before replacing either: invalid input must preserve prior reviewed inputs.
        var newCapital = capital.ToDictionary(x => x.Cik);
        var newEquity = equity.ToDictionary(x => x.Cik);
        if (capital.Any(x => x.Cik <= 0 || !ValidSymbol(x.Symbol) || string.IsNullOrWhiteSpace(x.FilingReference) || string.IsNullOrWhiteSpace(x.ReviewReference))
            || equity.Any(x => x.Cik <= 0 || !ValidSymbol(x.Symbol) || string.IsNullOrWhiteSpace(x.ReviewReference)))
            throw new ArgumentException("Identity and review references are required.");
        Capital = newCapital; Equity = newEquity;

        T[] Parse<T>(string name, string[] fields)
        {
            var list = root.GetProperty(name);
            if (list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > 5) throw new ArgumentException("Each evidence list supports at most five companies.");
            return list.EnumerateArray().Select(item =>
            {
                foreach (var field in fields)
                    if (!item.TryGetProperty(field, out var property) || property.ValueKind == JsonValueKind.Null)
                        throw new ArgumentException("Every review field must be explicit, including zero-valued claims.");
                if (item.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() != 1)) throw new ArgumentException("Duplicate evidence fields are invalid.");
                return item.Deserialize<T>() ?? throw new ArgumentException("Invalid evidence.");
            }).ToArray();
        }
    }
    private static bool ValidSymbol(string? symbol) => symbol is { Length: > 0 and <= 15 }
        && symbol.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) || c is '.' or '-');
}
