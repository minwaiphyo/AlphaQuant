using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public partial class SavedMemberChoice : ObservableObject
{
    private readonly Action _changed;
    public SavedMemberChoice(CompanyRef company, bool selected, Action changed)
    { Company = company; _changed = changed; _isSelected = selected; }
    public CompanyRef Company { get; }
    public string Label => $"{Company.Ticker} · {Company.Name} · CIK {Company.Cik:D10}";
    [ObservableProperty] private bool _isSelected;
    partial void OnIsSelectedChanged(bool value) => _changed();
}

/// <summary>Saved research stays usable without source credentials or network calls.</summary>
public partial class SavedResearchViewModel : ObservableObject
{
    private readonly IResearchStore _store;
    private readonly Func<IReadOnlyList<CompanyRef>> _selection;
    private readonly FinancialAnalysisViewModel _analysis;
    private readonly Action<IReadOnlyList<CompanyRef>> _compare;
    private readonly Func<SavedFinancialSnapshot, ResearchScreenPreset?, Task<bool>> _openOffline;
    private readonly CancellationTokenSource _lifetime = new();
    private int _selectionGeneration;
    public SavedResearchViewModel(IResearchStore store, Func<IReadOnlyList<CompanyRef>> selection,
        FinancialAnalysisViewModel analysis, Action<IReadOnlyList<CompanyRef>> compare,
        Func<SavedFinancialSnapshot, ResearchScreenPreset?, Task<bool>> openOffline)
    { _store = store; _selection = selection; _analysis = analysis; _compare = compare; _openOffline = openOffline; }
    public ObservableCollection<SavedResearchSet> Sets { get; } = [];
    public ObservableCollection<SavedMemberChoice> Members { get; } = [];
    public ObservableCollection<SavedFinancialSnapshot> Snapshots { get; } = [];
    private Task<bool> _selectionLoad = Task.FromResult(true);
    public Task SelectionLoad => _selectionLoad;
    [ObservableProperty] private SavedResearchSet? _selectedSet;
    [ObservableProperty] private SavedFinancialSnapshot? _selectedSnapshot;
    [ObservableProperty] private string _inputName = string.Empty;
    [ObservableProperty] private bool _saveAsWatchlist;
    [ObservableProperty] private bool _includeDeleted;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Load local research to begin. No network connection is needed.";
    [ObservableProperty] private string _backupPath = string.Empty;
    [ObservableProperty] private bool _confirmRestore;
    public string SelectedDetails => SelectedSet is not { } set ? "Choose a saved peer set or watchlist."
        : $"{(set.Kind == ResearchSetKind.PeerSet ? "Peer set" : "Watchlist")} · {set.Companies.Count} companies · revision {set.Revision} · "
            + (set.DeletedAt is { } deleted ? $"Deleted {deleted:yyyy-MM-dd HH:mm} UTC; recover before editing." : $"Updated {set.UpdatedAt:yyyy-MM-dd HH:mm} UTC.");
    public string SelectionDetails => $"{Members.Count(m => m.IsSelected)} selected for comparison (choose 2–5). No companies are silently truncated.";
    public string UpdateMembersLabel => SelectedSet?.Kind == ResearchSetKind.Watchlist ? "Add current companies to watchlist" : "Replace saved peers with current companies";
    public void CancelOperations() { _lifetime.Cancel(); ++_selectionGeneration; }
    partial void OnIncludeDeletedChanged(bool value) { if (!IsBusy) _ = ReloadAsync(); }
    partial void OnSelectedSetChanged(SavedResearchSet? value)
    {
        var generation = ++_selectionGeneration;
        Members.Clear(); Snapshots.Clear(); SelectedSnapshot = null;
        if (value is not null)
        {
            InputName = value.Name;
            foreach (var company in value.Companies)
                Members.Add(new(company, value.Kind == ResearchSetKind.PeerSet, () => OnPropertyChanged(nameof(SelectionDetails))));
            _selectionLoad = ReadSnapshotsAsync(value.Id, generation);
        }
        else _selectionLoad = Task.FromResult(true);
        OnPropertyChanged(nameof(SelectedDetails)); OnPropertyChanged(nameof(SelectionDetails)); OnPropertyChanged(nameof(UpdateMembersLabel));
    }
    private async Task<bool> ReadSnapshotsAsync(Guid id, int generation)
    {
        try
        {
            var snapshots = await _store.ListSnapshotsAsync(id, _lifetime.Token);
            if (generation != _selectionGeneration || SelectedSet?.Id != id || _lifetime.IsCancellationRequested) return false;
            Snapshots.Clear(); foreach (var item in snapshots) Snapshots.Add(item);
            SelectedSnapshot = Snapshots.FirstOrDefault();
            return true;
        }
        catch (ResearchStorageException ex) { if (generation == _selectionGeneration) Status = ex.Message; return false; }
        catch (OperationCanceledException) { return false; }
    }
    private async Task<bool> RefreshSetsAsync(Guid? selected = null)
    {
        var sets = await _store.ListSetsAsync(IncludeDeleted, _lifetime.Token);
        var id = selected ?? SelectedSet?.Id;
        SelectedSet = null;
        Sets.Clear(); foreach (var set in sets) Sets.Add(set);
        SelectedSet = Sets.FirstOrDefault(s => s.Id == id);
        return await _selectionLoad;
    }
    private async Task RunAsync(Func<Task> operation)
    {
        if (IsBusy || _lifetime.IsCancellationRequested) return;
        IsBusy = true;
        try { await operation(); }
        catch (ResearchStorageException ex) { Status = ex.Message; }
        catch (OperationCanceledException) { Status = "Research operation cancelled. Previously committed research remains available."; }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private Task ReloadAsync() => RunAsync(async () =>
    {
        await _store.InitializeAsync(_lifetime.Token);
        if (!await RefreshSetsAsync()) return;
        Status = "Local research loaded. Offline snapshots retain original dates; quotes and credentials are never saved here.";
    });
    [RelayCommand]
    private Task SaveNewAsync() => RunAsync(async () =>
    {
        await _store.InitializeAsync(_lifetime.Token);
        var saved = await _store.SaveSetAsync(new(InputName, SaveAsWatchlist ? ResearchSetKind.Watchlist : ResearchSetKind.PeerSet, _selection())
            { Preset = _analysis.CapturePreset() }, _lifetime.Token);
        if (!await RefreshSetsAsync(saved.Id)) return;
        Status = "Saved a new named set and its applied filter/sort preset. Financial snapshots are captured separately.";
    });
    [RelayCommand]
    private Task RenameAsync() => RunAsync(async () =>
    {
        if (SelectedSet is not { } set) { Status = "Choose a saved set to rename."; return; }
        var saved = await _store.SaveSetAsync(new(InputName, set.Kind, set.Companies, set.Id, set.Revision) { Preset = set.Preset }, _lifetime.Token);
        if (await RefreshSetsAsync(saved.Id)) Status = "Renamed. Members and historical snapshots were retained.";
    });
    [RelayCommand]
    private Task UpdateMembersAsync() => RunAsync(async () =>
    {
        if (SelectedSet is not { } set) { Status = "Choose a saved set first."; return; }
        var companies = set.Kind == ResearchSetKind.Watchlist ? set.Companies.Concat(_selection()).DistinctBy(c => c.Cik).ToArray() : _selection();
        var saved = await _store.SaveSetAsync(new(set.Name, set.Kind, companies, set.Id, set.Revision)
            { Preset = _analysis.CapturePreset() }, _lifetime.Token);
        if (await RefreshSetsAsync(saved.Id)) Status = "Saved members and the applied filter preset. Existing financial snapshots were not rewritten.";
    });
    [RelayCommand] private Task DeleteAsync() => ChangeDeletedAsync(true);
    [RelayCommand]
    private Task RemoveMembersAsync() => RunAsync(async () =>
    {
        if (SelectedSet is not { Kind: ResearchSetKind.Watchlist } set) { Status = "Choose a watchlist to remove checked members."; return; }
        var remove = Members.Where(m => m.IsSelected).Select(m => m.Company.Cik).ToHashSet();
        var remaining = set.Companies.Where(c => !remove.Contains(c.Cik)).ToArray();
        if (remove.Count == 0 || remaining.Length == 0) { Status = "Check members to remove, keeping at least one. Move the whole set to deleted if it is no longer needed."; return; }
        var saved = await _store.SaveSetAsync(new(set.Name, set.Kind, remaining, set.Id, set.Revision) { Preset = set.Preset }, _lifetime.Token);
        if (await RefreshSetsAsync(saved.Id)) Status = "Removed the checked watchlist members. Historical financial snapshots were retained.";
    });
    [RelayCommand] private Task RecoverAsync() => ChangeDeletedAsync(false);
    private Task ChangeDeletedAsync(bool deleted) => RunAsync(async () =>
    {
        if (SelectedSet is not { } set) { Status = "Choose a saved set first."; return; }
        var saved = await _store.SetDeletedAsync(set.Id, set.Revision, deleted, _lifetime.Token);
        if (!await RefreshSetsAsync(saved.Id)) return;
        Status = deleted ? "Moved to deleted. Enable Include deleted to recover it; historical snapshots remain stored." : "Recovered the saved set and its snapshots.";
    });
    [RelayCommand]
    private Task CaptureAsync() => RunAsync(async () =>
    {
        if (SelectedSet is not { } set || set.DeletedAt is not null) { Status = "Choose an active saved set matching the financial comparison first."; return; }
        var saved = await _store.SaveSnapshotAsync(_analysis.CaptureFinancials(set.Id, set.Revision), _lifetime.Token);
        if (SelectedSet?.Id == saved.PeerSetId && !await ReadSnapshotsAsync(saved.PeerSetId, _selectionGeneration)) return;
        Status = "Captured immutable financial results and source evidence. No market quotes, valuation reviews, contact or tokens were stored.";
    });
    [RelayCommand]
    private Task OpenOfflineAsync() => RunAsync(async () =>
    {
        if (SelectedSnapshot is not { } snapshot) { Status = "Choose a financial snapshot. Opening an empty set does not fetch data."; return; }
        if (await _openOffline(snapshot, SelectedSet?.Preset)) Status = "Opened saved financial evidence without a network request.";
        else Status = "Previous update is still closing. Retry shortly; no network request was started.";
    });
    [RelayCommand]
    private void CompareMembers()
    {
        if (IsBusy) return;
        var companies = Members.Where(m => m.IsSelected).Select(m => m.Company).ToArray();
        if (companies.Length is < 2 or > 5) { Status = "Select two to five members to compare. A refresh makes an explicit source request."; return; }
        _compare(companies);
        _analysis.RestorePreset(SelectedSet?.Preset);
    }
    [RelayCommand]
    private Task BackupAsync() => RunAsync(async () =>
    {
        var backup = await _store.BackupAsync(_lifetime.Token);
        BackupPath = backup.Path; Status = "Verified local research backup created. Settings and credentials are stored separately and are not included.";
    });
    [RelayCommand]
    private Task RestoreAsync() => RunAsync(async () =>
    {
        if (!ConfirmRestore) { Status = "Confirm research replacement before restoring. A verified copy of the current valid database will be preserved."; return; }
        if (string.IsNullOrWhiteSpace(BackupPath)) { Status = "Choose the local backup file path first."; return; }
        ConfirmRestore = false;
        await _store.RestoreAsync(BackupPath, _lifetime.Token);
        if (await RefreshSetsAsync()) Status = "Research restored from the verified backup; the previous valid database was backed up separately.";
    });
}
