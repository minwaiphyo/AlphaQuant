using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlphaQuant.Core;
using Microsoft.Data.Sqlite;

namespace AlphaQuant.Infrastructure;

/// <summary>Local-only, short-lived SQLite connections. Never resets an unreadable file.</summary>
public sealed partial class SqliteResearchStore : IResearchStore
{
    internal const int ApplicationId = 0x41515253;
    internal const int CurrentVersion = 4;
    private const string Schema4Changes = "CREATE TABLE research_documents(id TEXT NOT NULL, revision INTEGER NOT NULL CHECK(revision>=1), payload TEXT NOT NULL, PRIMARY KEY(id,revision));";
    private const string Schema2Changes = "ALTER TABLE research_sets ADD COLUMN revision INTEGER NOT NULL DEFAULT 1 CHECK(revision>=1); ALTER TABLE research_sets ADD COLUMN deleted_utc TEXT;";
    private const string Schema3Changes = "ALTER TABLE research_sets ADD COLUMN preset_json TEXT; CREATE TABLE financial_snapshots(id TEXT PRIMARY KEY, set_id TEXT NOT NULL REFERENCES research_sets(id), captured_utc TEXT NOT NULL, payload_schema INTEGER NOT NULL, payload TEXT NOT NULL); CREATE INDEX snapshots_by_set ON financial_snapshots(set_id,captured_utc);";
    private static readonly JsonSerializerOptions JsonOptions = new()
    { MaxDepth = 32, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true };
    private const int MaxSnapshotBytes = 2 * 1024 * 1024;
    internal const string Schema1 = """
        CREATE TABLE companies(cik INTEGER PRIMARY KEY CHECK(cik>0 AND cik<=9999999999), ticker TEXT NOT NULL, name TEXT NOT NULL);
        CREATE TABLE research_sets(id TEXT PRIMARY KEY, name TEXT NOT NULL, kind INTEGER NOT NULL CHECK(kind IN(0,1)), created_utc TEXT NOT NULL, updated_utc TEXT NOT NULL);
        CREATE TABLE set_members(set_id TEXT NOT NULL REFERENCES research_sets(id) ON DELETE CASCADE, cik INTEGER NOT NULL REFERENCES companies(cik), position INTEGER NOT NULL CHECK(position>=0), PRIMARY KEY(set_id,cik), UNIQUE(set_id,position));
        """;
    private const long MaxDatabaseBytes = 50L * 1024 * 1024;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates =
        new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly string _path;
    private readonly bool _readOnly;
    public SqliteResearchStore(string path, bool readOnly = false)
    {
        _path = Path.GetFullPath(path);
        _readOnly = readOnly;
    }
    public static string DefaultPath => Path.Combine(LocalDataPaths.ApplicationRoot, "research", "research.db");
    public string FilePath => _path;

    public Task InitializeAsync(CancellationToken token = default) => AccessAsync<object?>(() =>
    {
        var exists = File.Exists(_path);
        if (_readOnly && !exists) throw Failure(ResearchStorageFailure.ReadOnly, "No research database exists at this read-only location.");
        if (exists && new FileInfo(_path).Length == 0) throw Failure(ResearchStorageFailure.Corrupt, "The research file is empty; it was preserved. Restore a backup into a new location.");
        using var db = Open(_path, exists ? (_readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite) : SqliteOpenMode.ReadWriteCreate);
        if (!exists)
        {
            using var tx = db.BeginTransaction();
            Execute(db, $"PRAGMA application_id={ApplicationId};" + Schema1 + Schema2Changes + Schema3Changes + Schema4Changes + $"PRAGMA user_version={CurrentVersion};", tx);
            token.ThrowIfCancellationRequested();
            tx.Commit();
        }
        var version = Validate(db);
        if (version < CurrentVersion)
        {
            EnsureWritable();
            MakeBackup(db, _path);
            Upgrade(db, token);
        }
        return null;
    }, token);

    public Task<IReadOnlyList<SavedResearchSet>> ListSetsAsync(bool includeDeleted = false, CancellationToken token = default) => AccessAsync<IReadOnlyList<SavedResearchSet>>(() =>
    {
        using var db = OpenCurrent();
        using var tx = db.BeginTransaction(deferred: true);
        var result = ReadSets(db, includeDeleted, token, tx);
        tx.Commit();
        return result;
    }, token);

    public Task<SavedResearchSet> SaveSetAsync(ResearchSetDraft draft, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        // Capture caller-owned collections before awaiting a gate.
        var companies = draft.Companies?.ToArray() ?? throw Failure(ResearchStorageFailure.Validation, "Choose companies first.");
        var name = ValidName(draft.Name);
        if (!Enum.IsDefined(draft.Kind) || companies.Length < (draft.Kind == ResearchSetKind.PeerSet ? 2 : 1)
            || companies.Length > (draft.Kind == ResearchSetKind.PeerSet ? 5 : 100)
            || companies.Select(c => c?.Cik).Distinct().Count() != companies.Length)
            throw Failure(ResearchStorageFailure.Validation, "Peer sets need 2–5 distinct companies; watchlists need 1–100.");
        companies = companies.Select(ValidCompany).ToArray();
        ValidatePreset(draft.Preset);
        object presetJson = draft.Preset is null ? DBNull.Value : JsonSerializer.Serialize(draft.Preset, JsonOptions);
        if (draft.Id == Guid.Empty || (draft.Id.HasValue != draft.ExpectedRevision.HasValue) || draft.ExpectedRevision is <= 0)
            throw Failure(ResearchStorageFailure.Validation, "Updates require a stable ID and positive expected revision.");
        return AccessAsync(() =>
        {
            EnsureWritable();
            using var db = OpenCurrent();
            using var tx = db.BeginTransaction();
            var id = draft.Id ?? Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            if (draft.Id.HasValue)
            {
                var changed = Execute(db, "UPDATE research_sets SET name=$name, preset_json=$preset, updated_utc=$now, revision=revision+1 WHERE id=$id AND revision=$revision AND deleted_utc IS NULL AND kind=$kind;", tx,
                    ("$name", name), ("$preset", presetJson), ("$now", Time(now)), ("$id", id.ToString()), ("$revision", draft.ExpectedRevision!.Value), ("$kind", (int)draft.Kind));
                if (changed != 1) throw Failure(ResearchStorageFailure.Conflict, "Research changed, was deleted, or changed kind. Reload before saving.");
                Execute(db, "DELETE FROM set_members WHERE set_id=$id;", tx, ("$id", id.ToString()));
            }
            else Execute(db, "INSERT INTO research_sets(id,name,kind,created_utc,updated_utc,revision,preset_json) VALUES($id,$name,$kind,$now,$now,1,$preset);", tx,
                ("$id", id.ToString()), ("$name", name), ("$kind", (int)draft.Kind), ("$now", Time(now)), ("$preset", presetJson));
            for (var i = 0; i < companies.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                var company = companies[i];
                Execute(db, "INSERT INTO companies(cik,ticker,name) VALUES($cik,$ticker,$name) ON CONFLICT(cik) DO UPDATE SET ticker=excluded.ticker,name=excluded.name;", tx,
                    ("$cik", company.Cik), ("$ticker", company.Ticker), ("$name", company.Name));
                Execute(db, "INSERT INTO set_members(set_id,cik,position) VALUES($id,$cik,$position);", tx,
                    ("$id", id.ToString()), ("$cik", company.Cik), ("$position", i));
            }
            var result = ReadSets(db, true, token, tx).Single(s => s.Id == id);
            token.ThrowIfCancellationRequested();
            tx.Commit();
            return result;
        }, token);
    }

    public Task<SavedResearchSet> SetDeletedAsync(Guid id, long expectedRevision, bool deleted, CancellationToken token = default) => AccessAsync(() =>
    {
        EnsureWritable();
        if (id == Guid.Empty || expectedRevision < 1) throw Failure(ResearchStorageFailure.Validation, "A stable ID and revision are required.");
        using var db = OpenCurrent();
        using var tx = db.BeginTransaction();
        var now = Time(DateTimeOffset.UtcNow);
        var changed = Execute(db, "UPDATE research_sets SET deleted_utc=$deleted, updated_utc=$now, revision=revision+1 WHERE id=$id AND revision=$revision;", tx,
            ("$deleted", deleted ? now : DBNull.Value), ("$now", now), ("$id", id.ToString()), ("$revision", expectedRevision));
        if (changed != 1) throw Failure(ResearchStorageFailure.Conflict, "Research changed or no longer exists. Reload before changing it.");
        var result = ReadSets(db, true, token, tx).Single(s => s.Id == id);
        token.ThrowIfCancellationRequested();
        tx.Commit();
        return result;
    }, token);

    public Task<ResearchBackup> BackupAsync(CancellationToken token = default) => AccessAsync(() =>
    {
        EnsureWritable();
        using var db = OpenCurrent();
        token.ThrowIfCancellationRequested();
        return MakeBackup(db, _path);
    }, token);

    public Task<SavedFinancialSnapshot> SaveSnapshotAsync(FinancialSnapshotDraft draft, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var snapshot = new SavedFinancialSnapshot(Guid.NewGuid(), draft.PeerSetId, draft.PeerSetRevision,
            DateTimeOffset.UtcNow, draft.DefinitionVersion, draft.Companies?.ToArray() ?? []);
        ValidateSnapshot(snapshot);
        // Debt in comparison-1.1 is an analyst capital-review input, not an
        // independently selected public filing fact. Keep its values/references
        // session-only even though it is displayed among financial fundamentals.
        snapshot = snapshot with { Companies = snapshot.Companies.Select(company => company with
        {
            DefinitionVersion = company.DefinitionVersion ?? snapshot.DefinitionVersion,
            Fundamentals = company.Fundamentals is { } financials ? financials with
            {
                DebtBalance = ComparisonAnalyzer.Missing("Reviewed interest-bearing debt", "USD",
                    "Reviewed capital inputs are session-only and were not retained in this snapshot.")
            } : null
        }).ToArray() };
        // Deep-copy only the explicitly typed financial DTO before awaiting I/O.
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaxSnapshotBytes) throw Failure(ResearchStorageFailure.Validation, "Financial snapshot exceeds the 2 MiB limit; nothing saved.");
        var captured = JsonSerializer.Deserialize<SavedFinancialSnapshot>(json, JsonOptions)!;
        return AccessAsync(() =>
        {
            EnsureWritable();
            using var db = OpenCurrent();
            using var tx = db.BeginTransaction();
            var set = ReadSets(db, false, token, tx).SingleOrDefault(s => s.Id == draft.PeerSetId);
            if (set is null || set.Revision != draft.PeerSetRevision) throw Failure(ResearchStorageFailure.Conflict, "Saved research changed. Reload before capturing a snapshot.");
            if (!set.Companies.Select(c => c.Cik).Order().SequenceEqual(captured.Companies.Select(c => c.Company.Cik).Order()))
                throw Failure(ResearchStorageFailure.Validation, "Snapshot companies must match the entire saved peer set. Choose a matching set first.");
            if (new FileInfo(_path).Length + Encoding.UTF8.GetByteCount(json) + 16384 > MaxDatabaseBytes)
                throw Failure(ResearchStorageFailure.Validation, "Research storage is full; preserve a backup before choosing a new research file.");
            Execute(db, "INSERT INTO financial_snapshots(id,set_id,captured_utc,payload_schema,payload) VALUES($id,$set,$time,2,$payload);", tx,
                ("$id", captured.Id.ToString()), ("$set", captured.PeerSetId.ToString()), ("$time", Time(captured.CapturedAt)), ("$payload", json));
            token.ThrowIfCancellationRequested();
            tx.Commit();
            return captured;
        }, token);
    }

    public Task<IReadOnlyList<SavedFinancialSnapshot>> ListSnapshotsAsync(Guid setId, CancellationToken token = default) => AccessAsync<IReadOnlyList<SavedFinancialSnapshot>>(() =>
    {
        using var db = OpenCurrent();
        return ReadSnapshots(db, setId, token);
    }, token);

    private static IReadOnlyList<SavedFinancialSnapshot> ReadSnapshots(SqliteConnection db, Guid setId, CancellationToken token, SqliteTransaction? transaction = null)
    {
        using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id,payload_schema,payload FROM financial_snapshots WHERE set_id=$id ORDER BY captured_utc DESC,id;";
        command.Parameters.AddWithValue("$id", setId.ToString());
        using var reader = command.ExecuteReader();
        var result = new List<SavedFinancialSnapshot>();
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            if (Enumerable.Range(0, 3).Any(reader.IsDBNull)) throw Failure(ResearchStorageFailure.Corrupt, "Required snapshot metadata is missing; file preserved.");
            var payloadVersion = reader.GetInt32(1);
            if (payloadVersion is not (1 or 2)) throw Failure(ResearchStorageFailure.UnsupportedVersion, "Snapshot payload version is unsupported; preserve the database and use a compatible app.");
            var text = reader.GetString(2);
            if (Encoding.UTF8.GetByteCount(text) > MaxSnapshotBytes) throw Failure(ResearchStorageFailure.Corrupt, "Stored snapshot exceeds the supported size.");
            var item = JsonSerializer.Deserialize<SavedFinancialSnapshot>(text, JsonOptions) ?? throw Failure(ResearchStorageFailure.Corrupt, "Stored snapshot is empty.");
            if (payloadVersion == 2 && item.Companies is not null && item.Companies.Any(company => company is null || string.IsNullOrWhiteSpace(company.DefinitionVersion)))
                throw Failure(ResearchStorageFailure.Corrupt, "Snapshot per-company definition provenance is missing; file preserved.");
            if (payloadVersion == 1 && item.Companies is not null && item.Companies.All(company => company is not null))
                item = item with { Companies = item.Companies.Select(company => company with { DefinitionVersion = item.DefinitionVersion }).ToArray() };
            try { ValidateSnapshot(item); }
            catch (ResearchStorageException ex) when (ex.Kind == ResearchStorageFailure.Validation)
            { throw new ResearchStorageException(ResearchStorageFailure.Corrupt, "Stored financial snapshot is malformed; it was preserved.", ex); }
            if (item.Id.ToString() != reader.GetString(0) || item.PeerSetId != setId) throw Failure(ResearchStorageFailure.Corrupt, "Snapshot identity does not match its stored record.");
            result.Add(item);
        }
        return result;
    }

    private static void ValidateContent(SqliteConnection db, CancellationToken token, SqliteTransaction? transaction = null)
    {
        foreach (var set in ReadSets(db, true, token, transaction)) _ = ReadSnapshots(db, set.Id, token, transaction);
        _ = ReadDocuments(db, token, transaction);
    }

    public Task RestoreAsync(string backupPath, CancellationToken token = default) => AccessAsync<object?>(() =>
    {
        EnsureWritable();
        var sourcePath = Path.GetFullPath(backupPath);
        if (string.Equals(sourcePath, _path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw Failure(ResearchStorageFailure.Validation, "Choose a separate verified backup.");
        var stagePath = _path + ".restore-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var source = Open(sourcePath, SqliteOpenMode.ReadOnly))
            {
                Validate(source);
                using var stage = Open(stagePath, SqliteOpenMode.ReadWriteCreate);
                source.BackupDatabase(stage);
                Upgrade(stage, token);
                Validate(stage);
                ValidateContent(stage, token);
            }
            var exists = File.Exists(_path);
            // Never create a DB over a corrupt/foreign existing file. A new path is the recovery route.
            using var target = Open(_path, exists ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadWriteCreate);
            if (exists)
            {
                Validate(target);
                MakeBackup(target, _path);
            }
            token.ThrowIfCancellationRequested();
            using var prepared = Open(stagePath, SqliteOpenMode.ReadOnly);
            prepared.BackupDatabase(target); // SQLite transaction, not file replacement over an open DB.
            return null;
        }
        finally { TryDelete(stagePath); }
    }, token);

    private async Task<T> AccessAsync<T>(Func<T> operation, CancellationToken token)
    {
        var gate = Gates.GetOrAdd(_path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            token.ThrowIfCancellationRequested();
            if (!_readOnly) Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            FileStream access;
            try
            {
                access = new FileStream(_path + ".access.lock", _readOnly ? FileMode.Open : FileMode.OpenOrCreate,
                    _readOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex) { throw new ResearchStorageException(ResearchStorageFailure.Busy, "Research is in use or its access lock is unavailable. Close other instances and retry.", ex); }
            using (access) return await Task.Run(operation, token).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            var kind = ex.SqliteErrorCode switch { 5 or 6 => ResearchStorageFailure.Busy, 8 => ResearchStorageFailure.ReadOnly,
                11 or 26 => ResearchStorageFailure.Corrupt, _ => ResearchStorageFailure.Io };
            throw new ResearchStorageException(kind, kind == ResearchStorageFailure.Corrupt
                ? "Research is unreadable and was preserved. Restore a verified backup into a new location."
                : "Research could not be accessed. Retry after checking permissions and other open instances; do not delete the database.", ex);
        }
        catch (UnauthorizedAccessException ex) { throw new ResearchStorageException(ResearchStorageFailure.ReadOnly, "Research location does not permit this operation.", ex); }
        catch (IOException ex) { throw new ResearchStorageException(ResearchStorageFailure.Io, "Research file access failed; existing data was preserved.", ex); }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        { throw new ResearchStorageException(ResearchStorageFailure.Corrupt, "Research contains malformed values; file preserved. Restore a verified backup into a new location.", ex); }
        catch (JsonException ex) { throw new ResearchStorageException(ResearchStorageFailure.Corrupt, "Research payload is malformed; it was preserved. Restore a verified backup into a new location.", ex); }
        catch (InvalidCastException ex) { throw new ResearchStorageException(ResearchStorageFailure.Corrupt, "Research column types are invalid; file preserved.", ex); }
        finally { gate.Release(); }
    }

    private SqliteConnection OpenCurrent()
    {
        var db = Open(_path, _readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite);
        try
        {
            if (Validate(db) != CurrentVersion) throw Failure(ResearchStorageFailure.UnsupportedVersion, "Initialize and migrate the research store before use.");
            return db;
        }
        catch { db.Dispose(); throw; }
    }
    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        if (File.Exists(path) && new FileInfo(path).Length > MaxDatabaseBytes)
            throw Failure(ResearchStorageFailure.Validation, "Research file exceeds the supported 50 MiB limit. Preserve it and choose a smaller backup.");
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false, DefaultTimeout = 1, ForeignKeys = true }.ToString());
        try { db.Open(); if (mode != SqliteOpenMode.ReadOnly) Execute(db, "PRAGMA synchronous=FULL;"); return db; }
        catch { db.Dispose(); throw; }
    }
    private static int Validate(SqliteConnection db)
    {
        if ((long)Scalar(db, "PRAGMA application_id;") != ApplicationId)
            throw Failure(ResearchStorageFailure.Corrupt, "This is not an AlphaQuant research database. It was not changed.");
        var version = checked((int)(long)Scalar(db, "PRAGMA user_version;"));
        if (version < 1 || version > CurrentVersion)
            throw Failure(ResearchStorageFailure.UnsupportedVersion, "Unsupported research schema. Keep this file and use a compatible application version.");
        if (!Equals(Scalar(db, "PRAGMA quick_check;"), "ok")) throw Failure(ResearchStorageFailure.Corrupt, "Research integrity check failed; file preserved.");
        using var check = db.CreateCommand(); check.CommandText = "PRAGMA foreign_key_check;";
        using var reader = check.ExecuteReader();
        if (reader.Read()) throw Failure(ResearchStorageFailure.Corrupt, "Research relationships are invalid; file preserved.");
        return version;
    }
    private static void Upgrade(SqliteConnection db, CancellationToken token)
    {
        var version = Validate(db);
        if (version == CurrentVersion) return;
        using var tx = db.BeginTransaction();
        if (version < 2) Execute(db, Schema2Changes, tx);
        if (version < 3) Execute(db, Schema3Changes, tx);
        if (version < 4) Execute(db, Schema4Changes, tx);
        Execute(db, $"PRAGMA user_version={CurrentVersion};", tx);
        ValidateContent(db, token, tx);
        token.ThrowIfCancellationRequested();
        tx.Commit();
    }
    private static ResearchBackup MakeBackup(SqliteConnection source, string path)
    {
        var version = Validate(source);
        var created = DateTimeOffset.UtcNow;
        var final = path + ".backup-" + Guid.NewGuid().ToString("N") + ".db";
        var temp = final + ".tmp";
        try
        {
            using (var destination = Open(temp, SqliteOpenMode.ReadWriteCreate))
            {
                source.BackupDatabase(destination);
                if (Validate(destination) != version) throw Failure(ResearchStorageFailure.Corrupt, "Backup version verification failed.");
                if (version == CurrentVersion) ValidateContent(destination, CancellationToken.None);
            }
            File.Move(temp, final, false);
            return new ResearchBackup(final, version, created);
        }
        finally { TryDelete(temp); }
    }
    private static IReadOnlyList<SavedResearchSet> ReadSets(SqliteConnection db, bool includeDeleted, CancellationToken token, SqliteTransaction? transaction = null)
    {
        var rows = new List<SavedResearchSet>();
        using (var command = db.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT id,name,kind,revision,created_utc,updated_utc,deleted_utc,preset_json FROM research_sets " + (includeDeleted ? "" : "WHERE deleted_utc IS NULL ") + "ORDER BY name COLLATE NOCASE,id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                if (Enumerable.Range(0, 6).Any(reader.IsDBNull)) throw Failure(ResearchStorageFailure.Corrupt, "Required research metadata is missing; file preserved.");
                rows.Add(new SavedResearchSet(Guid.Parse(reader.GetString(0)), reader.GetString(1), (ResearchSetKind)reader.GetInt32(2), [], reader.GetInt64(3),
                    ParseTime(reader.GetString(4)), ParseTime(reader.GetString(5)), reader.IsDBNull(6) ? null : ParseTime(reader.GetString(6)))
                { Preset = reader.IsDBNull(7) ? null : JsonSerializer.Deserialize<ResearchScreenPreset>(reader.GetString(7), JsonOptions) });
            }
        }
        for (var i = 0; i < rows.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            using var command = db.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT c.cik,c.ticker,c.name FROM set_members m JOIN companies c ON c.cik=m.cik WHERE m.set_id=$id ORDER BY m.position;";
            command.Parameters.AddWithValue("$id", rows[i].Id.ToString());
            using var reader = command.ExecuteReader();
            var members = new List<CompanyRef>();
            while (reader.Read())
            {
                if (Enumerable.Range(0, 3).Any(reader.IsDBNull)) throw Failure(ResearchStorageFailure.Corrupt, "Stored company metadata is missing; file preserved.");
                members.Add(new CompanyRef(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
            }
            rows[i] = rows[i] with { Companies = members.ToArray() };
            try
            {
                ValidName(rows[i].Name); ValidatePreset(rows[i].Preset);
                if (!Enum.IsDefined(rows[i].Kind) || rows[i].Revision < 1 || members.Count < (rows[i].Kind == ResearchSetKind.PeerSet ? 2 : 1)
                    || members.Count > (rows[i].Kind == ResearchSetKind.PeerSet ? 5 : 100))
                    throw Failure(ResearchStorageFailure.Validation, "Invalid saved-set shape.");
                foreach (var company in members) ValidCompany(company);
            }
            catch (ResearchStorageException ex) when (ex.Kind == ResearchStorageFailure.Validation)
            { throw new ResearchStorageException(ResearchStorageFailure.Corrupt, "Stored research identity, preset or membership is malformed; file preserved.", ex); }
        }
        return rows;
    }
    private static object Scalar(SqliteConnection db, string sql) { using var c = db.CreateCommand(); c.CommandText = sql; return c.ExecuteScalar()!; }
    private static int Execute(SqliteConnection db, string sql, SqliteTransaction? transaction = null, params (string Name, object Value)[] parameters)
    {
        var changes = 0;
        // Statements are application-owned SQL; user strings are parameters.
        // Execute each migration step explicitly so later preparation failures
        // cannot be mistaken for successful completion of an earlier step.
        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var command = db.CreateCommand(); command.CommandText = statement; command.Transaction = transaction;
            foreach (var p in parameters) command.Parameters.AddWithValue(p.Name, p.Value);
            changes += command.ExecuteNonQuery();
        }
        return changes;
    }
    private void EnsureWritable() { if (_readOnly) throw Failure(ResearchStorageFailure.ReadOnly, "Research store is read-only; no changes were made."); }
    private static string ValidName(string? name)
    {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 80 || value.Any(char.IsControl)) throw Failure(ResearchStorageFailure.Validation, "Use a research name of 1–80 printable characters.");
        return value;
    }
    private static void ValidatePreset(ResearchScreenPreset? preset)
    {
        if (preset is null) return;
        ValidName(preset.DefinitionVersion); ValidName(preset.SortMetric);
        if (preset.FilterMetric is null && preset.Operator is null && preset.Threshold is null) return;
        if (preset.FilterMetric is null || preset.Operator is not (">" or ">=" or "<" or "<=") || preset.Threshold is null)
            throw Failure(ResearchStorageFailure.Validation, "A filter needs a named metric, operator and explicit threshold.");
        ValidName(preset.FilterMetric);
    }
    private static void ValidateSnapshot(SavedFinancialSnapshot snapshot)
    {
        if (snapshot.Id == Guid.Empty || snapshot.PeerSetId == Guid.Empty || snapshot.PeerSetRevision < 1
            || snapshot.Companies is null || snapshot.Companies.Count is < 2 or > 5
            || snapshot.Companies.Any(c => c is null || c.Company is null) || snapshot.Companies.Select(c => c.Company.Cik).Distinct().Count() != snapshot.Companies.Count
            || snapshot.Companies.All(c => c.Fundamentals is null))
            throw Failure(ResearchStorageFailure.Validation, "Capture 2–5 distinct saved peers with at least one financial result.");
        ValidName(snapshot.DefinitionVersion);
        foreach (var company in snapshot.Companies)
        {
            ValidCompany(company.Company);
            ValidName(company.DefinitionVersion ?? snapshot.DefinitionVersion);
            if (company.Fundamentals is not { } f) continue;
            if (f.Expanded is null || f.Expanded.Count > 130 || f.Expanded.Any(item => item is null || string.IsNullOrWhiteSpace(item.DefinitionVersion) || item.Result is null) || f.History is null || f.History.Count > 5 || f.History.Any(h => h is null) || f.Warnings is null)
                throw Failure(ResearchStorageFailure.Validation, "A financial snapshot needs valid history/warnings and supports up to five annual periods.");
            var values = f.History.SelectMany(h => new[] { h.Revenue, h.RevenueGrowth, h.OperatingMargin, h.OperatingCashFlow })
                .Concat(f.Expanded.Select(item => item.Result))
                .Concat(new[] { f.TtmRevenue, f.TtmOperatingIncome, f.TtmOperatingMargin, f.TtmOperatingCashFlow, f.TtmDilutedEps, f.CashBalance, f.DebtBalance });
            foreach (var value in values)
                if (value is null || string.IsNullOrWhiteSpace(value.Name) || string.IsNullOrWhiteSpace(value.Unit) || value.Inputs is null || value.EvidenceReferences is null
                    || value.Inputs.Any(input => input?.Fact is null || string.IsNullOrWhiteSpace(input.Fact.Concept) || string.IsNullOrWhiteSpace(input.Fact.Unit)
                        || string.IsNullOrWhiteSpace(input.Fact.Form) || string.IsNullOrWhiteSpace(input.Fact.Accession)))
                    throw Failure(ResearchStorageFailure.Validation, "A financial snapshot contains malformed values or source evidence.");
        }
    }
    private static CompanyRef ValidCompany(CompanyRef? company)
    {
        if (company is null || company.Cik is <= 0 or > 9999999999 || string.IsNullOrWhiteSpace(company.Ticker) || company.Ticker.Length > 32
            || !Regex.IsMatch(company.Ticker, "^[A-Za-z0-9.-]+$", RegexOptions.CultureInvariant) || string.IsNullOrWhiteSpace(company.Name)
            || company.Name.Length > 200 || company.Name.Any(char.IsControl)) throw Failure(ResearchStorageFailure.Validation, "A company identity is invalid.");
        return company with { Ticker = company.Ticker.ToUpperInvariant(), Name = company.Name.Trim() };
    }
    private static string Time(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset ParseTime(string value) => DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture);
    private static ResearchStorageException Failure(ResearchStorageFailure kind, string text) => new(kind, text);
    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } }
}
