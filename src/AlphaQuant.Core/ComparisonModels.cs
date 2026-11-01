namespace AlphaQuant.Core;

public sealed record FactContribution(RawFact Fact, decimal Multiplier = 1m);

public sealed record FinancialValue(string Name, decimal? Value, string Unit, DateOnly? Start, DateOnly? End,
    IReadOnlyList<FactContribution> Inputs, string? Reason = null)
{
    public bool Available => Value is not null && Reason is null;
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];
}

public sealed record AnnualFundamentals(DateOnly Start, DateOnly End, FinancialValue Revenue,
    FinancialValue RevenueGrowth, FinancialValue OperatingMargin, FinancialValue OperatingCashFlow);

public sealed record ComparisonFundamentals(IReadOnlyList<AnnualFundamentals> History,
    FinancialValue TtmRevenue, FinancialValue TtmOperatingIncome, FinancialValue TtmOperatingMargin,
    FinancialValue TtmOperatingCashFlow, FinancialValue TtmDilutedEps, IReadOnlyList<string> Warnings)
{
    public FinancialValue CashBalance { get; init; } = ComparisonAnalyzer.Missing("Cash and cash equivalents", "USD", "A compatible balance-sheet fact is missing.");
    public FinancialValue DebtBalance { get; init; } = ComparisonAnalyzer.Missing("Reviewed interest-bearing debt", "USD", "Total debt composition requires explicit reviewed evidence; components are not silently summed.");
}

/// <summary>
/// Explicitly reviewed evidence, not inferred from an absent tag. No reviewed capital structure => no EV/sales.
/// Supporting evidence must establish a simple single-class issuer, USD values, scope, zero claims and split basis.
/// </summary>
public sealed record CapitalStructureEvidence(long Cik, string Symbol, DateOnly SharesDate, DateOnly BalanceSheetDate,
    decimal OutstandingShares, decimal Debt, decimal Cash, decimal PreferredEquity, decimal NonControllingInterests,
    string FilingReference, string ReviewReference, DateOnly SplitAdjustedThrough, bool SimpleSingleClass,
    bool SupportedDomesticNonFinancialUsGaapUsd = false);

public sealed record EquityBasisEvidence(long Cik, string Symbol, DateOnly SplitAdjustedThrough,
    string ReviewReference, bool SupportedSimpleDomesticNonFinancialUsGaapUsd = false);

public sealed record ValuationResult(string DefinitionVersion, FinancialValue PriceEarnings,
    FinancialValue EnterpriseValueSales, decimal? EnterpriseValue, MarketPrice? Price,
    CapitalStructureEvidence? CapitalStructure, EquityBasisEvidence? EquityBasis);

public sealed record ComparisonRow(CompanyRef Company, ComparisonFundamentals? Fundamentals,
    ValuationResult? Valuation, MarketConnectionState Connection, string? Error,
    DateTimeOffset? FactsRetrievedAt, bool FactsStale);

public sealed record ComparisonSnapshot(IReadOnlyList<ComparisonRow> Rows);
