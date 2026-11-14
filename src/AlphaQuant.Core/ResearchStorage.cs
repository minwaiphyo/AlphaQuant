namespace AlphaQuant.Core;

public enum ResearchSetKind { PeerSet, Watchlist }
public sealed record ResearchSetDraft(string Name, ResearchSetKind Kind, IReadOnlyList<CompanyRef> Companies,
    Guid? Id = null, long? ExpectedRevision = null)
{
    public ResearchScreenPreset? Preset { get; init; }
}
public sealed record SavedResearchSet(Guid Id, string Name, ResearchSetKind Kind,
    IReadOnlyList<CompanyRef> Companies, long Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt)
{
    public ResearchScreenPreset? Preset { get; init; }
}
public sealed record ResearchScreenPreset(string DefinitionVersion, string SortMetric, bool Descending,
    string? FilterMetric = null, string? Operator = null, decimal? Threshold = null);
/// <summary>Deliberately excludes quotes, valuations, session errors, review objects and credentials.</summary>
public sealed record SavedFinancialCompany(CompanyRef Company, ComparisonFundamentals? Fundamentals,
    DateTimeOffset? FactsRetrievedAt, bool FactsStale)
{
    public string? DefinitionVersion { get; init; }
}
public sealed record FinancialSnapshotDraft(Guid PeerSetId, long PeerSetRevision, string DefinitionVersion,
    IReadOnlyList<SavedFinancialCompany> Companies);
public sealed record SavedFinancialSnapshot(Guid Id, Guid PeerSetId, long PeerSetRevision,
    DateTimeOffset CapturedAt, string DefinitionVersion, IReadOnlyList<SavedFinancialCompany> Companies)
{
    public string Label => $"{CapturedAt:yyyy-MM-dd HH:mm:ss} UTC · {DefinitionVersion}";
}
public sealed record ResearchBackup(string Path, int SchemaVersion, DateTimeOffset CreatedAt);
public enum ResearchStorageFailure { Validation, Conflict, Busy, ReadOnly, Corrupt, UnsupportedVersion, Io }
public sealed class ResearchStorageException(ResearchStorageFailure kind, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public ResearchStorageFailure Kind { get; } = kind;
}

/// <summary>Typed research state only. No contact, market-session or credential objects.</summary>
public interface IResearchStore
{
    Task<ResearchDocumentRevision> SaveDocumentAsync(ResearchDocumentDraft draft, CancellationToken token = default) => throw new NotSupportedException("Research documents unsupported by this store.");
    Task<IReadOnlyList<ResearchDocumentRevision>> ListDocumentRevisionsAsync(CancellationToken token = default) => throw new NotSupportedException("Research documents unsupported by this store.");
    Task InitializeAsync(CancellationToken token = default);
    Task<IReadOnlyList<SavedResearchSet>> ListSetsAsync(bool includeDeleted = false, CancellationToken token = default);
    Task<SavedResearchSet> SaveSetAsync(ResearchSetDraft draft, CancellationToken token = default);
    Task<SavedResearchSet> SetDeletedAsync(Guid id, long expectedRevision, bool deleted, CancellationToken token = default);
    Task<ResearchBackup> BackupAsync(CancellationToken token = default);
    Task RestoreAsync(string backupPath, CancellationToken token = default);
    Task<SavedFinancialSnapshot> SaveSnapshotAsync(FinancialSnapshotDraft draft, CancellationToken token = default);
    Task<IReadOnlyList<SavedFinancialSnapshot>> ListSnapshotsAsync(Guid setId, CancellationToken token = default);
}
