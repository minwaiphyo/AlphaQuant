using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using AlphaQuant.Desktop.ViewModels;
using AlphaQuant.Infrastructure;
using Microsoft.Data.Sqlite;

namespace AlphaQuant.Tests;

public sealed class SavedResearchTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aq-saved-tests-" + Guid.NewGuid().ToString("N"));
    private static readonly CompanyRef A = new(1, "AAA", "Synthetic Alpha");
    private static readonly CompanyRef B = new(2, "BBB", "Synthetic Beta");
    private static readonly CompanyRef C = new(3, "CCC", "Synthetic Gamma");
    private string DbPath => Path.Combine(_root, "research.db");
    public SavedResearchTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private SqliteResearchStore Store() => new(DbPath);
    private static ComparisonFundamentals Fundamentals(decimal revenue = 100m) => ComparisonAnalyzer.Analyze(
        [new RawFact("Revenues", "USD", revenue, new(2025, 1, 1), new(2025, 12, 31), "10-K", new(2026, 2, 1), "0000000001-26-000001")], new(2026, 3, 1));
    private static FinancialSnapshotDraft Snapshot(SavedResearchSet set, decimal revenue = 100m, string? version = null) => new(set.Id, set.Revision,
        version ?? ComparisonAnalyzer.DefinitionVersion, set.Companies.Select(c => new SavedFinancialCompany(c, Fundamentals(revenue),
            new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero), false)).ToArray());
    private async Task<SavedResearchSet> PeerSet()
    { var store = Store(); await store.InitializeAsync(); return await store.SaveSetAsync(new("Peers", ResearchSetKind.PeerSet, [A, B])); }

    [Fact]
    public async Task Immutable_snapshots_reopen_exact_values_dates_and_weighted_sources()
    {
        var set = await PeerSet(); var store = Store();
        var first = await store.SaveSnapshotAsync(Snapshot(set, 100.123456m));
        await store.SaveSnapshotAsync(Snapshot(set, 900m));
        var reopened = await Store().ListSnapshotsAsync(set.Id);
        Assert.Equal(2, reopened.Count);
        var original = reopened.Single(s => s.Id == first.Id);
        Assert.Equal(first.CapturedAt, original.CapturedAt);
        Assert.Equal(100.123456m, original.Companies[0].Fundamentals!.History[0].Revenue.Value);
        Assert.Equal(new DateOnly(2025, 12, 31), original.Companies[0].Fundamentals!.History[0].Revenue.Inputs[0].Fact.End);
        Assert.Equal(1m, original.Companies[0].Fundamentals!.History[0].Revenue.Inputs[0].Multiplier);
        Assert.Equal(Snapshot(set).Companies[0].FactsRetrievedAt, original.Companies[0].FactsRetrievedAt);
    }

    [Fact]
    public async Task Membership_change_and_soft_delete_preserve_old_snapshot_identity()
    {
        var set = await PeerSet(); var store = Store();
        var captured = await store.SaveSnapshotAsync(Snapshot(set));
        var updated = await store.SaveSetAsync(new("Changed", set.Kind, [B, C], set.Id, set.Revision));
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSnapshotAsync(Snapshot(set)));
        await store.SetDeletedAsync(updated.Id, updated.Revision, true);
        Assert.Empty(await store.ListSetsAsync());
        var old = Assert.Single(await store.ListSnapshotsAsync(set.Id));
        Assert.Equal(captured.Id, old.Id); Assert.Equal(new long[] { 1, 2 }, old.Companies.Select(c => c.Company.Cik));
    }

    [Fact]
    public async Task Unknown_definitions_are_stored_and_displayed_without_recalculation_or_network()
    {
        var set = await PeerSet(); var store = Store();
        var old = await store.SaveSnapshotAsync(Snapshot(set, 123.456m, "comparison-old-unknown"));
        var backend = new OfflineBackend();
        var vm = new FinancialAnalysisViewModel(backend, new NoLinks(), () => { }, () => { });
        Assert.True(await vm.ShowSavedSnapshotAsync(old, new("comparison-old-unknown", "Annual revenue", false, "Annual revenue", ">", 999m)));
        Assert.Equal(0, backend.Requests); Assert.True(vm.IsOffline); Assert.Equal(2, vm.Companies.Count);
        Assert.Contains("comparison-old-unknown", vm.Title); Assert.Contains("No current prices", vm.Status);
        vm.Companies[0].Metrics.First(m => m.Name == "Annual revenue").InspectCommand.Execute(null);
        Assert.Contains("without re-evaluating", vm.Evidence!.Formula);
        Assert.Contains("123.456", vm.Evidence.Detail);
    }

    [Fact]
    public async Task Presets_roundtrip_and_missing_or_oversized_snapshot_never_replace_prior_data()
    {
        var store = Store(); await store.InitializeAsync();
        var preset = new ResearchScreenPreset(ComparisonAnalyzer.DefinitionVersion, "Annual revenue", true, "TTM diluted EPS", ">=", 0.0046m);
        var set = await store.SaveSetAsync(new("Saved rules", ResearchSetKind.PeerSet, [A, B]) { Preset = preset });
        Assert.Equal(preset, Assert.Single(await store.ListSetsAsync()).Preset);
        var saved = await store.SaveSnapshotAsync(Snapshot(set));
        var huge = Snapshot(set) with { Companies = [new(A, Fundamentals() with { Warnings = [new string('x', 2 * 1024 * 1024)] }, null, false), new(B, Fundamentals(), null, false)] };
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSnapshotAsync(huge));
        var unmatched = Snapshot(set) with { Companies = [new(A, Fundamentals(), null, false), new(C, Fundamentals(), null, false)] };
        await Assert.ThrowsAsync<ResearchStorageException>(() => store.SaveSnapshotAsync(unmatched));
        Assert.Equal(saved.Id, Assert.Single(await store.ListSnapshotsAsync(set.Id)).Id);
    }

    [Fact]
    public async Task Payload_version_or_required_field_corruption_is_explicit_not_an_empty_success()
    {
        var set = await PeerSet(); var store = Store(); await store.SaveSnapshotAsync(Snapshot(set));
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "UPDATE financial_snapshots SET payload_schema=999;"; command.ExecuteNonQuery();
        }
        var unsupported = await Assert.ThrowsAsync<ResearchStorageException>(() => store.ListSnapshotsAsync(set.Id));
        Assert.Equal(ResearchStorageFailure.UnsupportedVersion, unsupported.Kind);
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "UPDATE financial_snapshots SET payload_schema=1,payload='{}';"; command.ExecuteNonQuery();
        }
        var malformed = await Assert.ThrowsAsync<ResearchStorageException>(() => store.ListSnapshotsAsync(set.Id));
        Assert.Equal(ResearchStorageFailure.Corrupt, malformed.Kind);
        Assert.Single(await store.ListSetsAsync());
        var analysis = new FinancialAnalysisViewModel(new OfflineBackend(), new NoLinks(), () => { }, () => { });
        var page = new SavedResearchViewModel(store, () => [A, B], analysis, _ => { }, (_, _) => Task.FromResult(true));
        page.SelectedSet = set; await page.SelectionLoad;
        await page.ReloadCommand.ExecuteAsync(null);
        Assert.Contains("malformed", page.Status); Assert.DoesNotContain("Local research loaded", page.Status);
        page.CancelOperations();
    }

    [Fact]
    public async Task Full_viewmodel_capture_excludes_market_metadata_and_session_error_text()
    {
        var set = await PeerSet(); var store = Store();
        var backend = new OfflineBackend { AllowSynthetic = true };
        var vm = new FinancialAnalysisViewModel(backend, new NoLinks(), () => { }, () => { });
        await vm.LoadAsync([A, B]);
        Assert.Contains("SECRET_SESSION_DETAIL", vm.Companies[0].Status);
        var saved = await store.SaveSnapshotAsync(vm.CaptureFinancials(set.Id, set.Revision));
        var json = JsonSerializer.Serialize(saved);
        Assert.DoesNotContain("SECRET_SESSION_DETAIL", json);
        Assert.DoesNotContain("DO_NOT_STORE_MARKET", json);
        Assert.DoesNotContain("DO_NOT_STORE_REVIEW_NOTE", json);
        Assert.False(saved.Companies[0].Fundamentals!.DebtBalance.Available);
        Assert.Contains("session-only", saved.Companies[0].Fundamentals!.DebtBalance.Reason);
        Assert.DoesNotContain("Valuation", json); Assert.DoesNotContain("Connection", json); Assert.DoesNotContain("accessToken", json);
        Assert.Equal(1, backend.Requests);
    }

    [Fact]
    public async Task Restart_saved_navigation_and_settings_return_make_no_network_requests()
    {
        var set = await PeerSet(); var store = Store(); var snapshot = await store.SaveSnapshotAsync(Snapshot(set));
        var backend = new OfflineBackend();
        var main = new MainWindowViewModel(new NoDirectory(), new NoFacts(), new ContactSettings(Path.Combine(_root, "contact.json"), () => null),
            new NoLinks(), backend, researchStore: store);
        var saved = main.SavedResearch!;
        await saved.ReloadCommand.ExecuteAsync(null);
        saved.SelectedSet = Assert.Single(saved.Sets); saved.SelectedSnapshot = snapshot;
        await saved.OpenOfflineCommand.ExecuteAsync(null);
        Assert.Same(main.Analysis, main.CurrentPage); Assert.True(main.Analysis!.IsOffline);
        main.NavigateSettingsCommand.Execute(null);
        var settings = Assert.IsType<SettingsViewModel>(main.CurrentPage);
        settings.BackCommand.Execute(null);
        Assert.Same(main.Analysis, main.CurrentPage); Assert.True(main.Analysis.IsOffline);
        Assert.Equal(0, backend.Requests);
        main.Shutdown();
    }

    [Fact]
    public async Task Watchlist_deduplicates_appends_and_requires_explicit_two_to_five_selection()
    {
        var store = Store(); await store.InitializeAsync();
        var original = await store.SaveSetAsync(new("Watch", ResearchSetKind.Watchlist, [A, B]));
        IReadOnlyList<CompanyRef> selected = [B, C];
        var analysis = new FinancialAnalysisViewModel(new OfflineBackend(), new NoLinks(), () => { }, () => { });
        var compared = 0;
        var vm = new SavedResearchViewModel(store, () => selected, analysis, c => compared = c.Count, (_, _) => Task.FromResult(true));
        await vm.ReloadCommand.ExecuteAsync(null); vm.SelectedSet = original;
        await vm.UpdateMembersCommand.ExecuteAsync(null);
        Assert.Equal(new long[] { 1, 2, 3 }, Assert.Single(await store.ListSetsAsync()).Companies.Select(c => c.Cik));
        vm.CompareMembersCommand.Execute(null); Assert.Equal(0, compared);
        vm.Members[0].IsSelected = true; vm.Members[1].IsSelected = true;
        vm.CompareMembersCommand.Execute(null); Assert.Equal(2, compared);
    }

    [Fact]
    public async Task Schema_two_upgrade_backs_up_and_keeps_existing_set()
    {
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = $"PRAGMA application_id={SqliteResearchStore.ApplicationId};" + SqliteResearchStore.Schema1;
            command.ExecuteNonQuery();
            command.CommandText = "ALTER TABLE research_sets ADD COLUMN revision INTEGER NOT NULL DEFAULT 1;"; command.ExecuteNonQuery();
            command.CommandText = "ALTER TABLE research_sets ADD COLUMN deleted_utc TEXT;"; command.ExecuteNonQuery();
            command.CommandText = "PRAGMA user_version=2;"; command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO companies VALUES(1,'AAA','Legacy Alpha');"; command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO research_sets VALUES('11111111-1111-1111-1111-111111111111','Legacy watch',1,'2026-01-01T00:00:00.0000000+00:00','2026-01-01T00:00:00.0000000+00:00',4,NULL);"; command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO set_members VALUES('11111111-1111-1111-1111-111111111111',1,0);"; command.ExecuteNonQuery();
        }
        var store = Store(); await store.InitializeAsync();
        var backup = Assert.Single(Directory.GetFiles(_root, "*.backup-*.db"));
        using var original = new SqliteConnection($"Data Source={backup};Pooling=False"); original.Open();
        using var check = original.CreateCommand(); check.CommandText = "PRAGMA user_version;";
        Assert.Equal(2L, check.ExecuteScalar());
        var preserved = Assert.Single(await store.ListSetsAsync());
        Assert.Equal("Legacy watch", preserved.Name); Assert.Equal(4, preserved.Revision); Assert.Null(preserved.Preset);
    }

    [Fact]
    public async Task Late_snapshot_response_cannot_replace_another_selected_sets_evidence()
    {
        var first = await PeerSet(); var store = Store();
        var firstSnapshot = await store.SaveSnapshotAsync(Snapshot(first));
        var second = await store.SaveSetAsync(new("Other peers", ResearchSetKind.PeerSet, [B, C]));
        var secondSnapshot = await store.SaveSnapshotAsync(Snapshot(second, 900m));
        var delayed = new DelayedStore(store, first.Id);
        var analysis = new FinancialAnalysisViewModel(new OfflineBackend(), new NoLinks(), () => { }, () => { });
        var vm = new SavedResearchViewModel(delayed, () => [A, B], analysis, _ => { }, (_, _) => Task.FromResult(true));
        vm.SelectedSet = first; var pending = vm.SelectionLoad;
        vm.SelectedSet = second; await vm.SelectionLoad;
        delayed.Delayed.SetResult([firstSnapshot]); await pending;
        Assert.Equal(second.Id, vm.SelectedSet!.Id);
        Assert.Equal(secondSnapshot.Id, Assert.Single(vm.Snapshots).Id);
        vm.CancelOperations();
    }

    [Fact]
    public async Task Explicit_null_source_evidence_is_rejected_as_corrupt_without_changing_payload()
    {
        var set = await PeerSet(); var store = Store(); await store.SaveSnapshotAsync(Snapshot(set));
        string corrupted;
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand(); command.CommandText = "SELECT payload FROM financial_snapshots;";
            var node = System.Text.Json.Nodes.JsonNode.Parse((string)command.ExecuteScalar()!)!;
            node["Companies"]![0]!["Fundamentals"]!["TtmRevenue"]!["Inputs"] = null;
            corrupted = node.ToJsonString();
            command.CommandText = "UPDATE financial_snapshots SET payload=$payload;"; command.Parameters.AddWithValue("$payload", corrupted); command.ExecuteNonQuery();
        }
        var error = await Assert.ThrowsAsync<ResearchStorageException>(() => store.ListSnapshotsAsync(set.Id));
        Assert.Equal(ResearchStorageFailure.Corrupt, error.Kind);
        using var inspect = new SqliteConnection($"Data Source={DbPath};Pooling=False"); inspect.Open();
        using var read = inspect.CreateCommand(); read.CommandText = "SELECT payload FROM financial_snapshots;";
        Assert.Equal(corrupted, read.ExecuteScalar());
    }

    [Fact]
    public async Task Malformed_stored_filter_is_not_silently_applied_or_treated_as_empty_research()
    {
        await PeerSet();
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "UPDATE research_sets SET preset_json=$preset;";
            command.Parameters.AddWithValue("$preset", "{\"DefinitionVersion\":\"comparison-1.1\",\"SortMetric\":\"Annual revenue\",\"Descending\":false,\"FilterMetric\":\"Annual revenue\",\"Operator\":null,\"Threshold\":1}");
            command.ExecuteNonQuery();
        }
        var error = await Assert.ThrowsAsync<ResearchStorageException>(() => Store().ListSetsAsync());
        Assert.Equal(ResearchStorageFailure.Corrupt, error.Kind);
    }

    [Fact]
    public async Task Physically_valid_backup_with_bad_payload_cannot_replace_current_research()
    {
        var set = await PeerSet(); var store = Store(); var snapshot = await store.SaveSnapshotAsync(Snapshot(set));
        var backup = await store.BackupAsync();
        await store.SaveSetAsync(new("Current research must survive", ResearchSetKind.Watchlist, [C]));
        using (var db = new SqliteConnection($"Data Source={backup.Path};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "UPDATE financial_snapshots SET payload='{}';"; command.ExecuteNonQuery();
            command.CommandText = "PRAGMA quick_check;"; Assert.Equal("ok", command.ExecuteScalar());
        }
        var error = await Assert.ThrowsAsync<ResearchStorageException>(() => store.RestoreAsync(backup.Path));
        Assert.Equal(ResearchStorageFailure.Corrupt, error.Kind);
        Assert.Equal(2, (await store.ListSetsAsync()).Count);
        Assert.Equal(snapshot.Id, Assert.Single(await store.ListSnapshotsAsync(set.Id)).Id);
        Assert.Single(Directory.GetFiles(_root, "*.backup-*.db"));
    }

    [Fact]
    public async Task Partial_refresh_preserves_each_retained_definition_in_UI_and_new_snapshot()
    {
        var set = await PeerSet(); var store = Store();
        var old = await store.SaveSnapshotAsync(Snapshot(set, 123.456m, "comparison-legacy"));
        var backend = new OfflineBackend { AllowSynthetic = true, NextRows =
            [new(A, Fundamentals(200m), null, MarketConnectionState.Disconnected, null, DateTimeOffset.UtcNow, false),
             new(B, null, null, MarketConnectionState.Disconnected, "Synthetic refresh failure", null, false)] };
        var vm = new FinancialAnalysisViewModel(backend, new NoLinks(), () => { }, () => { });
        Assert.True(await vm.ShowSavedSnapshotAsync(old));
        await vm.LoadAsync([A, B]);
        Assert.Contains("mixed definitions", vm.Title);
        var retained = vm.Companies.Single(c => c.Name.Contains(B.Name));
        Assert.Contains("comparison-legacy", retained.Status);
        retained.Metrics.First(m => m.Name == "Annual revenue").InspectCommand.Execute(null);
        Assert.Contains("comparison-legacy", vm.Evidence!.Formula); Assert.Contains("123.456", vm.Evidence.Detail);
        vm.Threshold = "0"; vm.ApplyFilterCommand.Execute(null);
        Assert.Contains("requires one supported definition", vm.FilterError); Assert.Equal(2, vm.Companies.Count);
        var captured = await store.SaveSnapshotAsync(vm.CaptureFinancials(set.Id, set.Revision));
        Assert.Equal(ComparisonAnalyzer.DefinitionVersion, captured.Companies.Single(c => c.Company.Cik == 1).DefinitionVersion);
        Assert.Equal("comparison-legacy", captured.Companies.Single(c => c.Company.Cik == 2).DefinitionVersion);
        var reopened = (await store.ListSnapshotsAsync(set.Id)).Single(s => s.Id == captured.Id);
        var viewer = new FinancialAnalysisViewModel(backend, new NoLinks(), () => { }, () => { });
        Assert.True(await viewer.ShowSavedSnapshotAsync(reopened)); Assert.Contains("mixed definitions", viewer.Title);
        Assert.Equal(1, backend.Requests);
    }

    [Fact]
    public async Task Legacy_snapshot_payload_uses_its_original_root_definition_for_each_company()
    {
        var set = await PeerSet(); var store = Store(); await store.SaveSnapshotAsync(Snapshot(set, 100m, "comparison-legacy"));
        using (var db = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            db.Open(); using var command = db.CreateCommand(); command.CommandText = "SELECT payload FROM financial_snapshots;";
            var node = System.Text.Json.Nodes.JsonNode.Parse((string)command.ExecuteScalar()!)!;
            foreach (var company in node["Companies"]!.AsArray()) company!.AsObject().Remove("DefinitionVersion");
            command.CommandText = "UPDATE financial_snapshots SET payload_schema=1,payload=$payload;";
            command.Parameters.AddWithValue("$payload", node.ToJsonString()); command.ExecuteNonQuery();
        }
        var restored = Assert.Single(await store.ListSnapshotsAsync(set.Id));
        Assert.All(restored.Companies, c => Assert.Equal("comparison-legacy", c.DefinitionVersion));
    }

    private sealed class NoLinks : ILinkOpener { public void Open(Uri uri) => throw new InvalidOperationException("No links in synthetic tests"); }
    private sealed class NoDirectory : ICompanyDirectory
    { public Task<CompanyDirectoryResult> GetCompaniesAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected network directory request"); }
    private sealed class NoFacts : ICompanyFactsSource
    { public Task<CompanyFacts> GetFactsAsync(long cik, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected network facts request"); }
    private sealed class OfflineBackend : IResearchBackend
    {
        public int Requests; public bool AllowSynthetic;
        public IReadOnlyList<AlphaQuant.Core.ComparisonRow>? NextRows;
        public bool Configured => false; public bool Authorized => false;
        public Task ConnectAsync(CancellationToken token) => throw new InvalidOperationException("No live connections");
        public Task<MarketAccess> TestAsync(IReadOnlyList<string> symbols, CancellationToken token) => throw new InvalidOperationException("No live requests");
        public void Disconnect() { }
        public async IAsyncEnumerable<ComparisonSnapshot> WatchAsync(IReadOnlyList<CompanyRef> companies,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            Requests++; if (!AllowSynthetic) throw new InvalidOperationException("Unexpected backend request");
            await Task.CompletedTask;
            if (NextRows is not null) { yield return new(NextRows); yield break; }
            yield return new(companies.Select(c => new AlphaQuant.Core.ComparisonRow(c, Fundamentals() with
                { DebtBalance = new FinancialValue("Reviewed interest-bearing debt", 987654m, "USD", null, new(2025, 12, 31), [])
                    { EvidenceReferences = ["DO_NOT_STORE_REVIEW_NOTE"] } },
                new ValuationResult(ComparisonAnalyzer.DefinitionVersion, ComparisonAnalyzer.Missing("Trailing P/E", "x", "Synthetic missing review"),
                    ComparisonAnalyzer.Missing("EV / TTM sales", "x", "Synthetic missing review"), null,
                    new MarketPrice(c.Ticker, 60m, "USD", "DO_NOT_STORE_MARKET", "IEX", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "synthetic-trade"), null, null),
                MarketConnectionState.Disconnected, "SECRET_SESSION_DETAIL", DateTimeOffset.UtcNow, false)).ToArray());
        }
    }
    private sealed class DelayedStore(IResearchStore inner, Guid delayedId) : IResearchStore
    {
        public TaskCompletionSource<IReadOnlyList<SavedFinancialSnapshot>> Delayed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<SavedFinancialSnapshot>> ListSnapshotsAsync(Guid setId, CancellationToken token = default)
            => setId == delayedId ? Delayed.Task : inner.ListSnapshotsAsync(setId, token);
        public Task InitializeAsync(CancellationToken token = default) => inner.InitializeAsync(token);
        public Task<IReadOnlyList<SavedResearchSet>> ListSetsAsync(bool includeDeleted = false, CancellationToken token = default) => inner.ListSetsAsync(includeDeleted, token);
        public Task<SavedResearchSet> SaveSetAsync(ResearchSetDraft draft, CancellationToken token = default) => inner.SaveSetAsync(draft, token);
        public Task<SavedResearchSet> SetDeletedAsync(Guid id, long expectedRevision, bool deleted, CancellationToken token = default) => inner.SetDeletedAsync(id, expectedRevision, deleted, token);
        public Task<ResearchBackup> BackupAsync(CancellationToken token = default) => inner.BackupAsync(token);
        public Task RestoreAsync(string path, CancellationToken token = default) => inner.RestoreAsync(path, token);
        public Task<SavedFinancialSnapshot> SaveSnapshotAsync(FinancialSnapshotDraft draft, CancellationToken token = default) => inner.SaveSnapshotAsync(draft, token);
    }
}
