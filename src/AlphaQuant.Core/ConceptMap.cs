namespace AlphaQuant.Core;

/// <summary>
/// US-GAAP tags accepted for the test metrics (definition version test-0). Tags are tried in order, the first
/// present wins, and alternatives are never summed.
/// </summary>
public static class ConceptMap
{
    public static IReadOnlyList<string> Revenue { get; } =
    [
        "RevenueFromContractWithCustomerExcludingAssessedTax",
        "Revenues",
        "SalesRevenueNet",
    ];

    /// <summary>Consolidated profit first; profit attributable to the parent is only a disclosed fallback.</summary>
    public static IReadOnlyList<string> NetIncome { get; } =
    [
        "ProfitLoss",
        "NetIncomeLoss",
    ];

    public const string AssetsCurrent = "AssetsCurrent";

    public const string LiabilitiesCurrent = "LiabilitiesCurrent";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        [.. Revenue, .. NetIncome, AssetsCurrent, LiabilitiesCurrent],
        StringComparer.Ordinal);
}
