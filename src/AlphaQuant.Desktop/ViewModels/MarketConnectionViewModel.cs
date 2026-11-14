using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public partial class MarketConnectionViewModel(IResearchBackend backend, Func<IReadOnlyList<string>> symbols, ReviewedEvidenceStore? evidence = null) : ObservableObject
{
    private CancellationTokenSource? _operation;
    public bool Configured => backend.Configured;
    public bool Authorized => backend.Authorized;
    public event Action? Disconnected;
    public event Action? ReviewedInputsChanged;
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _evidenceJson = string.Empty;
    [ObservableProperty] private string _evidenceStatus = "No reviewed valuation inputs loaded. Missing inputs remain unavailable.";
    [RelayCommand]
    private void LoadEvidence()
    {
        if (evidence is null) return;
        var changed = false;
        try { evidence.Load(EvidenceJson); EvidenceJson = string.Empty; EvidenceStatus = $"Loaded {evidence.Capital.Count} capital and {evidence.Equity.Count} equity reviews for this session. Accuracy is not certified by parsing. Return to analysis to apply."; changed = true; }
        catch (Exception) { EvidenceStatus = "Invalid evidence document. All fields, explicit zeros, dates, unique identities and review references are required. Previous reviews retained. See the preview guide."; }
        if (changed) ReviewedInputsChanged?.Invoke();
    }
    [RelayCommand] private void ClearEvidence() { evidence?.Clear(); EvidenceJson = string.Empty; EvidenceStatus = "Reviewed inputs cleared. Return to analysis to refresh."; ReviewedInputsChanged?.Invoke(); }
    [ObservableProperty] private string _status = backend.Configured
        ? "Disconnected. Connect with your eligible Alpaca account in the browser."
        : "Configuration required: the application operator must configure the HTTPS OAuth broker. Fundamentals work without it.";

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (Busy || !Configured) return;
        using var cts = new CancellationTokenSource();
        _operation = cts; Busy = true;
        try
        {
            Status = "Complete data-only consent in your browser. You can cancel here.";
            await backend.ConnectAsync(cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            Status = "Authorized. Test access for your selected companies before opening financial analysis.";
        }
        catch (OperationCanceledException) { backend.Disconnect(); Status = "Authorization cancelled or timed out. Retry when ready."; }
        catch (Exception) { backend.Disconnect(); Status = "Authorization failed. Check the broker configuration and retry; no credentials were saved."; }
        finally { _operation = null; Busy = false; OnPropertyChanged(nameof(Authorized)); }
    }

    [RelayCommand]
    private async Task TestAsync()
    {
        if (Busy) return;
        var selected = symbols().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (!Authorized) { Status = "Connect with Alpaca first."; return; }
        if (selected.Length == 0) { Status = "Choose companies before testing symbol access."; return; }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        _operation = cts; Busy = true;
        try
        {
            Status = "Testing IEX access for the selected symbols…";
            var access = await backend.TestAsync(selected, cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            Status = $"Access verified: {access.Capabilities.Feed} · {access.Capabilities.Coverage}. A sample trade is not a fresh streaming price. Open financial analysis to subscribe.";
        }
        catch (OperationCanceledException) { Status = "Access test cancelled or timed out."; }
        catch (Exception) { Status = "Access could not be verified. Check account eligibility, symbol access and connection; no paid fallback is used."; }
        finally { _operation = null; Busy = false; OnPropertyChanged(nameof(Authorized)); }
    }

    [RelayCommand] private void Cancel() => _operation?.Cancel();
    [RelayCommand]
    private void Disconnect()
    {
        _operation?.Cancel(); backend.Disconnect(); Disconnected?.Invoke();
        Status = "Disconnected. Session token cleared; provider authorization is not revoked.";
        OnPropertyChanged(nameof(Authorized));
    }
}
