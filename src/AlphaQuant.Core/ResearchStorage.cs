namespace AlphaQuant.Core;

public enum ResearchSetKind { PeerSet, Watchlist }
public sealed record ResearchSetDraft(string Name, ResearchSetKind Kind, IReadOnlyList<CompanyRef> Companies,
    Guid? Id = null, long? ExpectedRevision = null);
public sealed record SavedResearchSet(Guid Id, string Name, ResearchSetKind Kind,
    IReadOnlyList<CompanyRef> Companies, long Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);
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
    Task InitializeAsync(CancellationToken token = default);
    Task<IReadOnlyList<SavedResearchSet>> ListSetsAsync(bool includeDeleted = false, CancellationToken token = default);
    Task<SavedResearchSet> SaveSetAsync(ResearchSetDraft draft, CancellationToken token = default);
    Task<SavedResearchSet> SetDeletedAsync(Guid id, long expectedRevision, bool deleted, CancellationToken token = default);
    Task<ResearchBackup> BackupAsync(CancellationToken token = default);
    Task RestoreAsync(string backupPath, CancellationToken token = default);
}
