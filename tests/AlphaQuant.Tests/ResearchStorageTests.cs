using AlphaQuant.Core;
using AlphaQuant.Infrastructure;
using Microsoft.Data.Sqlite;

namespace AlphaQuant.Tests;

public sealed class ResearchStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aq-research-tests-" + Guid.NewGuid().ToString("N"));
    private string DbPath => Path.Combine(_root, "research.db");
    private static readonly CompanyRef A = new(1, "AAA", "Synthetic Alpha");
    private static readonly CompanyRef B = new(2, "BBB", "Synthetic Beta");
    public ResearchStorageTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private SqliteResearchStore Store(bool readOnly = false) => new(DbPath, readOnly);
    private static ResearchSetDraft Draft(string name = "Peers") => new(name, ResearchSetKind.PeerSet, [A, B]);
    private static SqliteConnection Open(string path)
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false, DefaultTimeout = 1 }.ToString());
        db.Open(); return db;
    }
    private static object? Sql(SqliteConnection db, string text)
    { using var c = db.CreateCommand(); c.CommandText = text; return c.ExecuteScalar(); }
    private void Legacy(bool broken = false)
    {
        using var db = Open(DbPath);
        Sql(db, $"PRAGMA application_id={SqliteResearchStore.ApplicationId};" + SqliteResearchStore.Schema1 + "PRAGMA user_version=1;");
        Sql(db, "INSERT INTO companies VALUES(1,'AAA','Original'); INSERT INTO research_sets VALUES('11111111-1111-1111-1111-111111111111','Legacy',1,'2026-01-01T00:00:00.0000000+00:00','2026-01-01T00:00:00.0000000+00:00'); INSERT INTO set_members VALUES('11111111-1111-1111-1111-111111111111',1,0);");
        if (broken) Sql(db, "ALTER TABLE research_sets ADD COLUMN deleted_utc TEXT;");
    }

    [Fact]
    public async Task Fresh_reopen_preserves_identity_order_and_parameterized_name()
    {
        var store = Store(); await store.InitializeAsync();
        var saved = await store.SaveSetAsync(Draft("Research'; DROP TABLE companies;--"));
        var reopened = Store(); await reopened.InitializeAsync();
        var actual = Assert.Single(await reopened.ListSetsAsync());
        Assert.Equal(saved.Id, actual.Id); Assert.Equal(saved.Name, actual.Name);
        Assert.Equal(new long[] { 1, 2 }, actual.Companies.Select(c => c.Cik));
        Assert.Equal(1, actual.Revision); Assert.Equal(TimeSpan.Zero, actual.CreatedAt.Offset);
        Assert.DoesNotContain("settings", string.Join('\n', Directory.GetFiles(_root)));
    }

    [Fact]
    public async Task Duplicate_company_or_invalid_identity_leaves_prior_set_intact()
    {
        var store = Store(); await store.InitializeAsync();
        var saved = await store.SaveSetAsync(Draft());
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(new("Bad", ResearchSetKind.PeerSet, [A, A with { Ticker = "OTHER" }])));
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(new("Bad", ResearchSetKind.Watchlist, [A with { Cik = 0 }])));
        Assert.Equal(saved.Id, Assert.Single(await store.ListSetsAsync()).Id);
    }

    [Fact]
    public async Task Concurrent_instances_require_the_observed_revision()
    {
        var store = Store(); await store.InitializeAsync();
        var saved = await store.SaveSetAsync(Draft());
        var other = Store();
        var update = new ResearchSetDraft("Renamed", saved.Kind, [B, A], saved.Id, saved.Revision);
        var result = await store.SaveSetAsync(update);
        var ex = await Assert.ThrowsAsync<ResearchStorageException>(() => other.SaveSetAsync(update));
        Assert.Equal(ResearchStorageFailure.Conflict, ex.Kind);
        Assert.Equal(2, result.Revision); Assert.Equal(saved.CreatedAt, result.CreatedAt);
        Assert.Equal(new long[] { 2, 1 }, Assert.Single(await other.ListSetsAsync()).Companies.Select(c => c.Cik));
    }

    [Fact]
    public async Task Delete_is_recoverable_and_does_not_allow_stale_overwrite()
    {
        var store = Store(); await store.InitializeAsync();
        var saved = await store.SaveSetAsync(Draft());
        var deleted = await store.SetDeletedAsync(saved.Id, saved.Revision, true);
        Assert.Empty(await store.ListSetsAsync()); Assert.NotNull(deleted.DeletedAt);
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(Draft() with { Id = saved.Id, ExpectedRevision = saved.Revision }));
        var restored = await store.SetDeletedAsync(deleted.Id, deleted.Revision, false);
        Assert.Null(restored.DeletedAt); Assert.Equal(3, restored.Revision);
        Assert.Equal(2, restored.Companies.Count);
    }

    [Fact]
    public async Task Supported_upgrade_preserves_data_and_verified_old_backup()
    {
        Legacy(); var store = Store(); await store.InitializeAsync();
        var saved = Assert.Single(await store.ListSetsAsync());
        Assert.Equal("Legacy", saved.Name); Assert.Equal(1, saved.Revision);
        var backup = Assert.Single(Directory.GetFiles(_root, "*.backup-*.db"));
        using var db = Open(backup);
        Assert.Equal(1L, Sql(db, "PRAGMA user_version;"));
        Assert.Equal("ok", Sql(db, "PRAGMA quick_check;"));
        Assert.Equal("Original", Sql(db, "SELECT name FROM companies;"));
    }

    [Fact]
    public async Task Failed_upgrade_rolls_back_first_alter_and_retains_verified_backup()
    {
        Legacy(broken: true);
        using (var before = Open(DbPath)) Assert.Equal(1L, Sql(before, "SELECT count(*) FROM pragma_table_info('research_sets') WHERE name='deleted_utc';"));
        await Assert.ThrowsAsync<ResearchStorageException>(() => Store().InitializeAsync());
        using var db = Open(DbPath);
        Assert.Equal(1L, Sql(db, "PRAGMA user_version;"));
        Assert.Equal(0L, Sql(db, "SELECT count(*) FROM pragma_table_info('research_sets') WHERE name='revision';"));
        Assert.Equal("Original", Sql(db, "SELECT name FROM companies;"));
        Assert.Single(Directory.GetFiles(_root, "*.backup-*.db"));
    }

    [Fact]
    public async Task Restore_is_atomic_and_preserves_pre_restore_research()
    {
        var store = Store(); await store.InitializeAsync();
        var first = await store.SaveSetAsync(Draft("First"));
        var backup = await store.BackupAsync();
        await store.SaveSetAsync(Draft("Second"));
        await store.RestoreAsync(backup.Path);
        Assert.Equal(first.Id, Assert.Single(await store.ListSetsAsync()).Id);
        var retainedFiles = Directory.GetFiles(_root, "*.backup-*.db").Where(p => p != backup.Path).ToArray();
        Assert.True(retainedFiles.Length == 1, string.Join('\n', retainedFiles));
        var retained = retainedFiles.Single();
        using var db = Open(retained);
        Assert.Equal(2L, Sql(db, "SELECT count(*) FROM research_sets;"));
        Assert.Equal(1L, Sql(db, "SELECT count(*) FROM research_sets WHERE name='Second';"));
    }

    [Fact]
    public async Task Corrupt_backup_and_future_database_are_never_used_or_reset()
    {
        var store = Store(); await store.InitializeAsync(); await store.SaveSetAsync(Draft());
        var bad = Path.Combine(_root, "bad.db"); await File.WriteAllTextAsync(bad, "not SQLite");
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.RestoreAsync(bad));
        Assert.Single(await store.ListSetsAsync());
        var before = File.ReadAllBytes(DbPath);
        using (var db = Open(DbPath)) Sql(db, "PRAGMA user_version=999;");
        var future = File.ReadAllBytes(DbPath);
        var ex = await Assert.ThrowsAsync<ResearchStorageException>(() => store.InitializeAsync());
        Assert.Equal(ResearchStorageFailure.UnsupportedVersion, ex.Kind);
        Assert.Equal(future, File.ReadAllBytes(DbPath)); Assert.NotEqual(before, future);
    }

    [Fact]
    public async Task Corrupt_current_file_is_preserved_and_backup_restores_to_new_path()
    {
        var store = Store(); await store.InitializeAsync(); await store.SaveSetAsync(Draft());
        var backup = await store.BackupAsync();
        var corrupt = new byte[] { 1, 2, 3, 4 }; await File.WriteAllBytesAsync(DbPath, corrupt);
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.RestoreAsync(backup.Path));
        Assert.Equal(corrupt, File.ReadAllBytes(DbPath));
        var recovered = new SqliteResearchStore(Path.Combine(_root, "recovered.db"));
        await recovered.RestoreAsync(backup.Path); await recovered.InitializeAsync();
        Assert.Single(await recovered.ListSetsAsync());
    }

    [Fact]
    public async Task Cancelled_write_and_readonly_instance_preserve_data()
    {
        var store = Store(); await store.InitializeAsync();
        var saved = await store.SaveSetAsync(Draft());
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveSetAsync(Draft("Cancelled"), cts.Token));
        var read = Store(readOnly: true); await read.InitializeAsync();
        Assert.Equal(saved.Id, Assert.Single(await read.ListSetsAsync()).Id);
        var ex = await Assert.ThrowsAsync<ResearchStorageException>(() => read.SaveSetAsync(Draft("No write")));
        Assert.Equal(ResearchStorageFailure.ReadOnly, ex.Kind);
        Assert.Single(await store.ListSetsAsync());
    }

    [Fact]
    public async Task External_access_lock_returns_busy_without_database_changes()
    {
        var store = Store(); await store.InitializeAsync(); await store.SaveSetAsync(Draft());
        using (var held = new FileStream(DbPath + ".access.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var ex = await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(Draft("Blocked")));
            Assert.Equal(ResearchStorageFailure.Busy, ex.Kind);
        }
        Assert.Single(await store.ListSetsAsync());
    }

    [Fact]
    public async Task Foreign_and_empty_files_are_not_initialized()
    {
        await File.WriteAllBytesAsync(DbPath, []);
        var empty = await Assert.ThrowsAsync<ResearchStorageException>(() => Store().InitializeAsync());
        Assert.Equal(ResearchStorageFailure.Corrupt, empty.Kind); Assert.Empty(File.ReadAllBytes(DbPath));
        using (var db = Open(DbPath)) Sql(db, "CREATE TABLE unrelated(value TEXT); INSERT INTO unrelated VALUES('preserve');");
        var before = File.ReadAllBytes(DbPath);
        await Assert.ThrowsAsync<ResearchStorageException>(() => Store().InitializeAsync());
        Assert.Equal(before, File.ReadAllBytes(DbPath));
    }

    [Fact]
    public async Task Mid_transaction_failure_rolls_back_name_members_and_company_upserts()
    {
        var store = Store(); await store.InitializeAsync();
        var original = await store.SaveSetAsync(Draft());
        using (var db = Open(DbPath)) Sql(db, "CREATE TRIGGER interrupt_members BEFORE INSERT ON set_members WHEN NEW.cik=2 BEGIN SELECT RAISE(ABORT,'injected failure'); END;");
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(
            new("Must roll back", original.Kind, [new CompanyRef(3, "CCC", "Synthetic Gamma"), B], original.Id, original.Revision)));
        var after = Assert.Single(await store.ListSetsAsync());
        Assert.Equal(original.Name, after.Name); Assert.Equal(original.Revision, after.Revision);
        Assert.Equal(new long[] { 1, 2 }, after.Companies.Select(c => c.Cik));
        using var inspect = Open(DbPath);
        Assert.Equal(0L, Sql(inspect, "SELECT count(*) FROM companies WHERE cik=3;"));
    }

    [Fact]
    public async Task Noncooperating_sqlite_writer_returns_busy_and_does_not_erase_data()
    {
        var store = Store(); await store.InitializeAsync(); await store.SaveSetAsync(Draft());
        using (var other = Open(DbPath))
        using (var held = other.BeginTransaction())
        {
            var ex = await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSetAsync(Draft("Blocked")));
            Assert.Equal(ResearchStorageFailure.Busy, ex.Kind);
            held.Rollback();
        }
        Assert.Single(await store.ListSetsAsync());
    }
}
