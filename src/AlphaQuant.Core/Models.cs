namespace AlphaQuant.Core;

/// <summary>A company as listed by the SEC. The CIK, not the ticker, identifies the entity.</summary>
public sealed record CompanyRef(long Cik, string Ticker, string Name);

/// <summary>One reported XBRL fact for a selected concept, exactly as filed. <c>Start</c> is null for instant facts.</summary>
public sealed record RawFact(
    string Concept,
    string Unit,
    decimal Value,
    DateOnly? Start,
    DateOnly End,
    string Form,
    DateOnly Filed,
    string Accession);

/// <summary>Facts for the concepts AlphaQuant uses, plus how fresh they are.</summary>
public sealed record CompanyFacts(
    string? EntityName,
    IReadOnlyList<RawFact> Facts,
    int SkippedFacts,
    DateTimeOffset RetrievedAt,
    bool IsStale);

public sealed record CompanyDirectoryResult(
    IReadOnlyList<CompanyRef> Companies,
    DateTimeOffset RetrievedAt,
    bool IsStale);

/// <summary>A fact selected for display, with the label and period it represents.</summary>
public sealed record AnnualFact(
    string Label,
    string Concept,
    decimal Value,
    string Unit,
    DateOnly? Start,
    DateOnly End,
    string Form,
    DateOnly Filed,
    string Accession);

public enum MetricStatus
{
    Available,
    Unavailable,
    Unsupported,
}

public sealed record MetricResult(
    string Name,
    string DefinitionVersion,
    MetricStatus Status,
    decimal? Value,
    string? DisplayUnit,
    string? Reason,
    IReadOnlyList<string> Inputs);

public sealed record CompanyReport(
    CompanyRef Company,
    IReadOnlyList<AnnualFact> Facts,
    IReadOnlyList<MetricResult> Metrics,
    IReadOnlyList<string> Warnings,
    string? UnsupportedReason,
    DateTimeOffset RetrievedAt,
    bool IsStale);
