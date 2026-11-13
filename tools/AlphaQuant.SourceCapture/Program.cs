using System.Text.Json;
using AlphaQuant.Core;
using AlphaQuant.Infrastructure;

if (args.Length == 1 && args[0] == "--help")
{
    Console.WriteLine("SourceCapture --live-sec [--output <new-directory>] | --preflight");
    Console.WriteLine("Uses configured local SEC contact, never accepts/prints it as a CLI argument. Captures are pending independent source review, not certification.");
    return 0;
}
var preflight = args.Length == 1 && args[0] == "--preflight";
var live = args.Length is 1 or 3 && args[0] == "--live-sec" && (args.Length == 1 || args[1] == "--output");
if (!preflight && !live) { Console.Error.WriteLine("Use --help for the explicit capture command."); return 2; }

var settings = new ContactSettings(ContactSettings.DefaultPath);
if (preflight)
{
    Console.WriteLine(settings.EffectiveContact is null ? "SEC configuration required. No requests sent." : "SEC contact configuration available. No requests sent.");
    return 0;
}
if (settings.EffectiveContact is null)
{
    Console.Error.WriteLine("SEC configuration required in Settings or AQ_SEC_CONTACT. No requests sent. Do not put contact details in command arguments or chat.");
    return 2;
}
var output = Path.GetFullPath(args.Length == 3 ? args[2] : Path.Combine(".local", "source-validation", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")));
if (Directory.Exists(output) || File.Exists(output)) { Console.Error.WriteLine("Output must be a new directory. Nothing overwritten."); return 2; }

using var http = SecClient.CreateHttpClient();
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
// Isolated in-memory cache, no modification of the user's financial cache/settings.
var client = new SecClient(http, () => settings.EffectiveContact, new CaptureCache(), new SecClientOptions
    { MinRequestInterval = TimeSpan.FromMilliseconds(500), MaxRetries = 1, MaxResponseBytes = 50L * 1024 * 1024 });
var symbols = new[] { "MSFT", "AAPL", "COST", "KO", "AMZN" };
var results = new List<CaptureResult>();
try
{
    var directory = await client.GetCompaniesAsync(timeout.Token);
    foreach (var symbol in symbols)
    {
        var identities = directory.Companies.Where(c => c.Ticker.Equals(symbol, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (identities.Length != 1) { results.Add(new(symbol, null, null, null, "unresolved_identity", null)); continue; }
        var company = identities[0];
        try
        {
            var facts = await client.GetFactsAsync(company.Cik, timeout.Token);
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var financials = ComparisonAnalyzer.Analyze(facts.Facts, today);
            var values = financials.History.SelectMany(y => new[] { y.Revenue, y.RevenueGrowth, y.OperatingMargin, y.OperatingCashFlow })
                .Concat([financials.TtmRevenue, financials.TtmOperatingIncome, financials.TtmOperatingMargin,
                    financials.TtmOperatingCashFlow, financials.TtmDilutedEps, financials.CashBalance]).ToArray();
            // Preserve candidate alternatives for selected periods, not just the winning facts.
            var earliest = financials.History.LastOrDefault()?.Start.AddYears(-1) ?? today.AddYears(-6);
            var candidates = facts.Facts.Where(f => f.End >= earliest && f.End <= today && f.Filed <= today).ToArray();
            results.Add(new(symbol, company, facts.RetrievedAt,
                $"https://data.sec.gov/api/xbrl/companyfacts/CIK{company.Cik:D10}.json", "pending_independent_source_review",
                new CaptureEvidence(today, ComparisonAnalyzer.DefinitionVersion, facts.IsStale, values, candidates, financials.Warnings)));
        }
        catch (DataSourceException ex) { results.Add(new(symbol, company, null, null, "source_error_" + ex.Kind, null)); }
    }
    Directory.CreateDirectory(output);
    var report = new { SchemaVersion = 1, CapturedAt = DateTimeOffset.UtcNow, Status = "capture_only_not_reconciled",
        RequestBudget = "One directory plus at most five issuer requests, at most one retry each, three-minute total timeout.",
        ScopeReview = "Issuer/scope/class eligibility is not certified by ticker or fact availability.", Results = results };
    await File.WriteAllTextAsync(Path.Combine(output, "capture.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Captured {results.Count} issuer outcomes. Review capture.json locally before committing minimal public-fact fixtures. No contact/header/token included.");
    return results.All(r => r.Status == "pending_independent_source_review") ? 0 : 1;
}
catch (OperationCanceledException) { Console.Error.WriteLine("Capture timed out or was cancelled. No completed reconciliation claimed."); return 2; }
catch (Exception) { Console.Error.WriteLine("Capture could not complete. Check source/configuration/output access without sharing private values."); return 2; }

sealed record CaptureResult(string Symbol, CompanyRef? Company, DateTimeOffset? RetrievedAt, string? SourceUrl, string Status, CaptureEvidence? Evidence);
sealed record CaptureEvidence(DateOnly AsOf, string DefinitionVersion, bool Stale, IReadOnlyList<FinancialValue> SelectedValues,
    IReadOnlyList<RawFact> CandidateFacts, IReadOnlyList<string> Warnings);
sealed class CaptureCache : ISecCache
{
    private readonly Dictionary<string, CacheEntry> _entries = [];
    public CacheEntry? TryRead(string key) => _entries.GetValueOrDefault(key);
    public void Write(string key, byte[] bytes) => _entries[key] = new(bytes, DateTimeOffset.UtcNow);
    public void Delete(string key) => _entries.Remove(key);
}
