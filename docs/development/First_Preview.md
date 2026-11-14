# Current engineering preview update (2026-11-14)

The current build adds expanded source-traceable metrics and **Research and models**:
immutable thesis/scenario revisions, explicit forecasts/DCF, bounded sensitivity/
reverse models, frozen CSV/XLSX/PDF exports and manual snapshot/condition reviews.
Read Research_Workbench.md and the included model/metric definitions. Save before
closing; no autosave, background monitoring, automatic sharing or recommendations.

This is an unsigned development build. Real SEC reconciliation, eligible IEX/OAuth
acceptance, zero-cost host permission, clean Windows signing/trust/install/upgrade
and historical valuation rights remain open. Keep Smart App Control and Defender
enabled. No general supported/full-product release is claimed. Existing user data
stays separate from binaries; preserve verified backups before schema upgrades.

The earlier preview notes below are historical background; the stage ledger and
current workbench/distribution guides govern the new engineering capabilities.

# AlphaQuant first preview

This Windows x64 preview supports company selection, SEC annual comparisons, and comparison-1 financial research. It is a research tool; it does not trade or produce buy/sell recommendations.

## Run

1. Extract the entire ZIP into a new folder; retain every DLL and native library.
2. Run AlphaQuant.Desktop.exe. The self-contained build includes its .NET runtime; no SDK or PATH change is required.
3. Open Data connections, enter a contact email you monitor and Save. SEC requests identify themselves using this address. It is stored on this computer; no contact is bundled in the application.
4. Choose companies and add two to five peers. Comparison opens the existing annual test-0 workspace with filters and evidence. Financial analysis opens the comparison-1 history/TTM screen; click a metric to inspect inputs and filing links.
5. Data connections returns to your analysis. Stop updates or leave Financial analysis to stop streaming. Closing the app or Disconnect clears market tokens from memory.

## Optional Alpaca connection

Fundamentals work without Alpaca. The operator must first register the OAuth application and deploy a permitted HTTPS token broker using [Alpaca_OAuth_Setup.md](https://github.com/minwaiphyo/AlphaQuant/blob/main/docs/development/Alpaca_OAuth_Setup.md). No account, endpoint or secret is bundled.

Set AQ_OAUTH_BROKER_ORIGIN to the broker's public HTTPS origin before starting the app (for example https://your-broker.example). This is public configuration, not an Alpaca secret. The desktop never needs the application's client secret. HTTP loopback is development-only.

Connect with Alpaca opens your browser for data-only consent. Select your peers, Test selected symbols, then return to Financial analysis. Only verified symbols may start the IEX stream. Authorization, sample access and a fresh eligible streaming trade are distinct states. IEX covers a limited venue, not the consolidated US market; the app shows event/receive times, age and connection status. Market session is unknown. No subscription purchase or paid fallback occurs.

## Known limitations and release gates

- This is an unsigned development preview. Windows Smart App Control or organizational policy may block it. Do not disable Windows security to run it. A trusted signing/distribution route or an approved development environment is still required on enforcing devices. A ZIP, checksum or self-contained runtime does not establish publisher trust.
- P/E and EV/sales require reviewed issuer/class/split/capital evidence. Data connections offers a structured review form and advanced explicit JSON import for this session. Neither entry nor parsing certifies a review. Missing scope, claims, dates or eligible prices leave valuation unavailable. See [structured review](Valuation_Review.md).
- Debt requires explicit reviewed composition and is unavailable without it. Missing facts never become zero. Supported scope is not automatically certified from fact availability.
- Five years appear where compatible filings exist; annual and TTM periods differ. Historical fundamentals use latest-filed facts, not historical point-in-time valuation.
- No live OAuth/provider or independent finance certification is implied by synthetic tests. Registration, permitted pilot use and zero-cost host availability must be established before a connected pilot.
- Production Compare uses comparison-1.1; legacy annual test-0 references remain separately labelled. Saved research now includes named peers/watchlists, presets and immutable financial snapshots with original dates and offline evidence. Quotes and reviewed valuation objects remain session-only. Thesis notes and exports are not implemented.

## Build and deployment

From the repository: dotnet test AlphaQuant.slnx, then powershell -File tools/Publish-Preview.ps1. The script produces a self-contained win-x64 ZIP and SHA256 file under ignored artifacts. It refuses to overwrite an existing version. Publish the OAuth broker separately; never put its environment secrets into the desktop ZIP. The GitHub workflow builds and uploads short-retention preview artifacts; it does not deploy a host, register OAuth or publish a production release.

First-version acceptance: clean build/tests; inspected UI with fake sources; install/run smoke on a supported Windows machine; trusted distribution decision; real SEC reconciliation; registered/deployed OAuth and entitlement verification for a connected preview. Pending external gates must stay explicit.

## Reviewed evidence schema

The structured form avoids editing this JSON. Its per-company Apply is atomic;
equity-only Apply explicitly removes that company's prior capital review while
retaining other issuers. Clear draft and Clear applied are separate. The following
advanced import retains its original whole-document replacement behavior.

Data connections → Reviewed valuation inputs accepts this versioned JSON structure. Arrays may be empty or contain up to five unique CIKs each. Property names are case-sensitive. Every property is mandatory for each entry, including explicit zeros; the app never infers that an omitted claim equals zero. Loading a valid document replaces both review lists atomically; invalid input preserves the prior lists. Clear removes both lists. Nothing is written to disk. Return to financial analysis after loading/clearing to apply it.

The following is a **fictional format example**, not evidence for a real company. Do not paste it to populate a real issuer. Replace every value/reference with reviewed filing evidence, actual ticker/CIK, dates, scope and a documented review. EPS/split basis must be reviewed through the current UTC date; stale/unsupported inputs still yield unavailable valuation. Use false when eligibility is unverified. Review references should identify the calculation record rather than contain personal contact details.

```json
{
  "Version": 1,
  "Equity": [{
    "Cik": 1,
    "Symbol": "ALDR",
    "SplitAdjustedThrough": "2026-11-01",
    "ReviewReference": "Fictional EPS, class and split review record",
    "SupportedSimpleDomesticNonFinancialUsGaapUsd": false
  }],
  "Capital": [{
    "Cik": 1,
    "Symbol": "ALDR",
    "SharesDate": "2026-09-30",
    "BalanceSheetDate": "2026-09-30",
    "OutstandingShares": 100000000,
    "Debt": 400000000,
    "Cash": 100000000,
    "PreferredEquity": 0,
    "NonControllingInterests": 0,
    "FilingReference": "Fictional accession and relevant share/capital notes",
    "ReviewReference": "Fictional scope, debt composition and zero-claims review record",
    "SplitAdjustedThrough": "2026-11-01",
    "SimpleSingleClass": true,
    "SupportedDomesticNonFinancialUsGaapUsd": false
  }]
}
```

The app does not verify that pasted review assertions are true. This advanced input is for documented analyst reviews, not a shortcut around missing data. Incompatible dates, scope, nonpositive earnings and stale prices retain the backend's unavailable states.
