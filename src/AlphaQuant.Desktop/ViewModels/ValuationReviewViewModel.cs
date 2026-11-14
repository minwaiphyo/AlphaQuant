using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AlphaQuant.Core;
using AlphaQuant.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaQuant.Desktop.ViewModels;

public sealed record ReviewSourceInputs(IReadOnlyList<RawFact> Facts, DateTimeOffset? RetrievedAt, bool Retained);

/// <summary>Explicit user assertions, held in memory. No extraction certifies issuer or capital scope.</summary>
public partial class ValuationReviewViewModel : ObservableObject
{
    private readonly ReviewedEvidenceStore _store;
    private readonly Func<IReadOnlyList<CompanyRef>> _companies;
    private readonly Func<long, ReviewSourceInputs> _sources;
    private readonly ILinkOpener _links;
    private readonly Action _invalidate;
    private readonly Func<DateOnly> _today;
    public ValuationReviewViewModel(ReviewedEvidenceStore store, Func<IReadOnlyList<CompanyRef>> companies,
        Func<long, ReviewSourceInputs> sources, ILinkOpener links, Action invalidate, Func<DateOnly>? today = null)
    { _store = store; _companies = companies; _sources = sources; _links = links; _invalidate = invalidate; _today = today ?? (() => DateOnly.FromDateTime(DateTime.UtcNow)); }
    public ObservableCollection<CompanyRef> Companies { get; } = [];
    public ObservableCollection<EvidenceFact> SourceFacts { get; } = [];
    [ObservableProperty][NotifyPropertyChangedFor(nameof(CompanyIdentity))][NotifyPropertyChangedFor(nameof(HasCompany))] private CompanyRef? _selectedCompany;
    [ObservableProperty] private bool _scopeConfirmed;
    [ObservableProperty] private bool _classConfirmed;
    [ObservableProperty] private string _splitThrough = string.Empty;
    [ObservableProperty] private string _filingAccession = string.Empty;
    [ObservableProperty] private string _reviewReference = string.Empty;
    [ObservableProperty] private bool _includeCapital;
    [ObservableProperty] private bool _capitalClaimsConfirmed;
    [ObservableProperty] private string _sharesDate = string.Empty;
    [ObservableProperty] private string _balanceSheetDate = string.Empty;
    [ObservableProperty] private string _outstandingShares = string.Empty;
    [ObservableProperty] private string _debt = string.Empty;
    [ObservableProperty] private string _cash = string.Empty;
    [ObservableProperty] private string _preferredEquity = string.Empty;
    [ObservableProperty] private string _nonControllingInterests = string.Empty;
    [ObservableProperty] private string _status = "Choose a company. Edits are drafts until Apply; previous applied reviews remain unchanged on failure.";
    [ObservableProperty] private string _sourceStatus = "Open a comparison for source inputs, or review the SEC filing independently. No network request is started here.";
    public string TodayLabel => $"UTC evaluation date: {_today():yyyy-MM-dd}. Split review must cover this date; share and balance-sheet dates must be within 135 days.";
    public bool HasCompany => SelectedCompany is not null;
    public string CompanyIdentity => SelectedCompany is { } c ? $"{c.Ticker} · CIK {c.Cik:D10} · {c.Name}" : "No company selected";
    public string AppliedSummary
    {
        get
        {
            if (SelectedCompany is not { } c || !_store.Equity.TryGetValue(c.Cik, out var equity)) return "No reviewed equity basis applied for this company. Valuation remains guarded.";
            var text = $"Session equity review: {equity.Symbol} · split basis through {equity.SplitAdjustedThrough:yyyy-MM-dd}. {equity.ReviewReference}";
            if (_store.Capital.TryGetValue(c.Cik, out var capital))
                text += $"\nActual outstanding common shares: {capital.OutstandingShares.ToString("G", CultureInfo.InvariantCulture)} at {capital.SharesDate:yyyy-MM-dd}; balance sheet {capital.BalanceSheetDate:yyyy-MM-dd}."
                    + $"\nUSD debt {capital.Debt:G}; cash {capital.Cash:G}; preferred {capital.PreferredEquity:G}; NCI {capital.NonControllingInterests:G}.";
            else text += "\nNo capital review: EV/sales remains unavailable.";
            return text + "\nApplying a review does not certify accuracy or supply an eligible price/TTM period.";
        }
    }
    public void RefreshCompanies()
    {
        var previous = SelectedCompany;
        var choices = _companies().DistinctBy(c => c.Cik).Take(5).ToArray();
        Companies.Clear(); foreach (var company in choices) Companies.Add(company);
        SelectedCompany = Companies.FirstOrDefault(c => previous is not null && c.Cik == previous.Cik && c.Ticker == previous.Ticker) ?? Companies.FirstOrDefault();
        RefreshSources(); RefreshAppliedStatus();
    }
    partial void OnSelectedCompanyChanged(CompanyRef? value)
    {
        ResetFields(); RefreshSources(); RefreshAppliedStatus();
        Status = value is null ? "Choose companies before reviewing valuation inputs."
            : "Draft is empty. Existing applied reviews were retained; verify this company's source and each claim before applying.";
    }
    public void RefreshAppliedStatus() => OnPropertyChanged(nameof(AppliedSummary));
    private void RefreshSources()
    {
        SourceFacts.Clear();
        if (SelectedCompany is not { } company) { SourceStatus = "Choose companies first. No source request is made by this form."; return; }
        var source = _sources(company.Cik);
        foreach (var fact in source.Facts.Take(100))
            SourceFacts.Add(new EvidenceFact(new AnnualFact(fact.Concept, fact.Concept, fact.Value, fact.Unit, fact.Start, fact.End,
                fact.Form, fact.Filed, fact.Accession), company.Cik, _links));
        var retrieval = source.RetrievedAt is { } time ? $"retrieved {time:yyyy-MM-dd HH:mm} UTC" : "retrieval date unavailable";
        SourceStatus = $"{SourceFacts.Count} selected calculation inputs · {retrieval} · {(source.Retained ? "saved/stale evidence" : "original financial snapshot")}. "
            + "These are not a complete capital disclosure. Inspect filings: diluted EPS is not actual outstanding shares; cash/debt candidates do not confirm complete claims.";
    }
    private void ResetFields()
    {
        ScopeConfirmed = false; ClassConfirmed = false; IncludeCapital = false; CapitalClaimsConfirmed = false;
        SplitThrough = FilingAccession = ReviewReference = SharesDate = BalanceSheetDate = OutstandingShares = Debt = Cash = PreferredEquity = NonControllingInterests = string.Empty;
    }
    [RelayCommand] private void ClearDraft() { ResetFields(); Status = "Draft cleared. Applied reviews remain unchanged."; }
    [RelayCommand]
    private void OpenFiling()
    {
        if (SelectedCompany is { } c && Regex.IsMatch(FilingAccession ?? string.Empty, "^[0-9]{10}-[0-9]{2}-[0-9]{6}$", RegexOptions.CultureInvariant)
            && FilingLinks.TryBuild(c.Cik, FilingAccession ?? string.Empty) is { } uri) _links.Open(uri);
        else Status = "Enter a valid SEC filing accession for the selected company first.";
    }
    [RelayCommand]
    private void Apply()
    {
        OnPropertyChanged(nameof(TodayLabel));
        try
        {
            var company = SelectedCompany;
            if (company is null || !_companies().Any(c => c.Cik == company.Cik && c.Ticker == company.Ticker)) throw new ArgumentException("Choose a company that is still in the peer selection.");
            if (!ScopeConfirmed || !ClassConfirmed) throw new ArgumentException("Explicitly review US domestic non-financial US-GAAP/USD scope, simple common class and compatible diluted-EPS/split basis. A ticker cannot establish these claims.");
            var today = _today();
            var split = ParseDate(SplitThrough, "Split review date", today);
            if (split != today) throw new ArgumentException("Split review must cover today's UTC evaluation date; stale evidence cannot enable current valuation.");
            if (!Regex.IsMatch(FilingAccession ?? string.Empty, "^[0-9]{10}-[0-9]{2}-[0-9]{6}$", RegexOptions.CultureInvariant)) throw new ArgumentException("Enter the reviewed SEC filing accession in 0000000000-00-000000 form.");
            var filing = FilingLinks.TryBuild(company.Cik, FilingAccession ?? string.Empty) ?? throw new ArgumentException("The filing identity is invalid.");
            var reference = RequiredReference(ReviewReference);
            var equity = new EquityBasisEvidence(company.Cik, company.Ticker, split, filing.AbsoluteUri + " · " + reference, true);
            CapitalStructureEvidence? capital = null;
            if (IncludeCapital)
            {
                if (!CapitalClaimsConfirmed) throw new ArgumentException("Confirm complete reviewed capital claims, including the source of explicit zero claims.");
                var sharesDate = ParseDate(SharesDate, "Actual-share date", today);
                var balanceDate = ParseDate(BalanceSheetDate, "Balance-sheet date", today);
                if (today.DayNumber - sharesDate.DayNumber > 135 || today.DayNumber - balanceDate.DayNumber > 135)
                    throw new ArgumentException("Share and balance-sheet evidence older than 135 days cannot enable current EV/sales.");
                capital = new(company.Cik, company.Ticker, sharesDate, balanceDate,
                    ParseAmount(OutstandingShares, "Actual outstanding common shares", true), ParseAmount(Debt, "Debt"), ParseAmount(Cash, "Cash"),
                    ParseAmount(PreferredEquity, "Preferred equity"), ParseAmount(NonControllingInterests, "Non-controlling interests"),
                    filing.AbsoluteUri, reference, split, true, true);
            }
            _store.ReplaceCompany(equity, capital);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        { Status = "Review not applied: " + ex.Message + " Previous session inputs retained."; return; }
        _invalidate(); RefreshAppliedStatus();
        Status = IncludeCapital ? "Equity and capital reviews applied atomically for this session. Return to comparison and explicitly refresh; source, TTM and fresh-price eligibility still apply. No certification or persistence."
            : "Equity review applied for this session. This company's prior capital review was cleared; EV/sales stays unavailable. Return to comparison to refresh. No certification or persistence.";
    }
    [RelayCommand]
    private void ClearApplied()
    {
        if (SelectedCompany is not { } company) return;
        _store.RemoveCompany(company.Cik); _invalidate(); RefreshAppliedStatus();
        Status = "Applied reviews cleared for this company; other company reviews retained. Valuation remains unavailable until valid evidence and price are supplied.";
    }
    private static DateOnly ParseDate(string? text, string name, DateOnly today)
    {
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date > today)
            throw new ArgumentException(name + " must be explicit yyyy-MM-dd, not a future date.");
        return date;
    }
    private static decimal ParseAmount(string? text, string name, bool positive = false)
    {
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            || (positive ? value <= 0 : value < 0)) throw new ArgumentException(name + (positive ? " must be an explicit positive number." : " must be an explicit non-negative number; enter 0 only after reviewing the zero claim.") + " Use a decimal point and no grouping separators or unit suffix.");
        return value;
    }
    private static string RequiredReference(string? text)
    {
        var reference = text?.Trim();
        if (string.IsNullOrEmpty(reference) || reference.Length > 500 || reference.Any(char.IsControl)) throw new ArgumentException("Add a 1–500 character review/source note with page/class/composition details.");
        return reference;
    }
}
