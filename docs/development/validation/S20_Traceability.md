# S20 hardening and requirement traceability

2026-11-14. Engineering verification is separate from live/provider/trusted Windows
release acceptance. S20 cannot be marked complete while S11 or historical S19 gates
remain open. No user stories or catalogue requirements were rewritten to imply
completion. Scope remains the approved roadmap.

| Requirement | Implemented evidence | Remaining acceptance |
|---|---|---|
| US-01/02 company selection/retrieval | CompanySearchTests, SecClientTests, ComparisonIntegrationTests; search and comparison UI harness | S02 real SEC reconciliation; S08 provider path |
| US-03/04 growth/source inspection | SourceReconciliationTests, ExpandedFundamentalsTests, evidence rendering/focus | Real filing coverage/reconciliation |
| US-05/06 comparison/filter/sort | ResearchUiIntegrationTests, SavedResearchTests; raw decimals, missing-last, selection/stream lifecycle | Native Windows scaling/accessibility |
| US-07/08 net margin/current ratio | expanded-1 source/version policy and independent worked tests | Per-issuer real source reconciliation, supported-scope review |
| US-09/10 watchlists/offline | ResearchStorageTests, SavedResearchTests; immutable snapshot restart, recovery and definitions | Owner-PC upgrade/restore acceptance |
| US-11/12 accessibility/install/upgrade | Real Avalonia focus/minimum light/dark renders; distribution guards, dependency manifest/hash, backup/migration tests | Clean supported Windows install/upgrade/failure/rollback, signing/trust and native accessibility |
| US-13 feedback | SafeDiagnosticsTests; preview/export/help and user-submitted draft | Actual external usability participants not fabricated |
| FA-01 comparable company analysis | comparison-1.1 + expanded-1, scoped reviews and source inputs | Live finance/issuer verification |
| FA-02 quality/investigation | screening-1, editable thresholds/weights/coverage, ResearchModelTests | Interpretation remains user judgment, not professional certification |
| FA-03 current/historical price valuation | Existing guarded P/E/EV and reviewed-source tests | Actual eligible dated IEX quote; historical prices/shares/actions/then-available filings and rights |
| FA-04 forecasts/scenarios/reverse | forecast-1/dcf-1, saved Bear/Base/Bull revisions, bounded grids/drivers, derivative proof and forward residual tests | User assumptions require review; no market upside/recommendation claim |
| FA-05 thesis | Schema-4 immutable revisions, conflict/archive/recovery and snapshot links; ResearchDocumentTests | No portfolio suitability/tax/position-sizing scope |
| FA-06 screening/shortlisting | Saved sets/presets, explicit rules, complete-coverage scores and ties | Missing data never improves rank; no overall buy/sell rating |
| FA-07 monitoring/reporting | Manual condition history, source-aware changes and fundamental plots; frozen CSV/XLSX/PDF replay and independent readers | Historical valuation rights/data; no background notifications/sharing |

## Verification and supported bounds

Release suite: 325 tests, zero failed/skipped. Full Release build zero warnings/errors.
NuGet audit of all transitive dependencies reports none vulnerable from current
configured sources; signatures/TLS were not bypassed. PDFsharp exact package
6.2.4 is MIT; DejaVu license retained, patched Pkcs 10.0.15 selected. Actual published
package graph supplies notices and file hashes, not a stale manual dependency list.

Storage tests cover supported schemas 1–4, rollback, verified backups/restores,
corruption, read-only/locks, concurrent revisions, offline/saved research, model
replay and 100-document-revision reopen/recovery. The full test suite's recorded
runner duration was 3 seconds on this Linux instance; this is not an end-user
performance guarantee or a Windows benchmark. Database cap50MiB, snapshot/document
payload2MiB, forecast10years, screening20rules, sensitivity100cells, reverse bounded
65sample checks + at most200bisections/one forward reconciliation, reports5000lines,
PDF100pages and export32MiB are explicit bounds; failures preserve prior files.

Real Avalonia/Skia rendering and focus checks cover comparison, offline/review,
diagnostics and workbench states. Representative minimum dark thesis/rules/model
bridge/source chart and PDF pages inspected. Reports were reopened using independent
openpyxl and Poppler tools: six XLSX columns, no formulas; readable wrapped PDF.
These do not establish native Windows screen-reader/scaling or clean-PC acceptance.

Safe diagnostics use an allowlist; no telemetry, contacts, headers, home paths,
credentials or research are collected. Exports preview a frozen saved revision,
neutralize spreadsheet text and atomically create new files. Feedback/reminders
remain user-initiated, with no external messages or schedules created.

## Distribution and rollback

Unsigned development ZIPs include build/dirty-state, SDK, dependency/license and
per-file hash evidence. Do not call them trusted installers or recruiter releases.
Use versioned application directories and separate user data. Before a schema
upgrade preserve a verified research backup. Never downgrade/overwrite the newer
DB for rollback: restore a verified compatible backup to a new path and retain both
originals. Actual Windows install/upgrade/rollback/uninstall-with-data-retention and
trusted provider/signing acceptance remain open. See Windows_Distribution.md.

The consolidated gate/action report is Roadmap_Blockers.md. No environment or full
product release publication is part of these checks.

Container build contexts now exclude artifacts/diagnostics, local credentials,
research databases, caches and logs, in addition to existing secret/key patterns.
No private local artifact should be uploaded merely to build the broker.

## Native Linux path regression fixed

Final isolated native startup found that an absent XDG data directory caused the
OS folder lookup to return empty, making settings/cache/research paths relative to
the checkout. LocalDataPaths now requests the absolute OS path without requiring
preexistence and fails closed if no absolute root exists. Settings, cache and
research share that root. Four regression cases cover missing/relative roots and
a new absolute directory. The suite now passes 323 tests with zero build warnings.

Repeated native startup used deliberately nonexistent isolated XDG directories,
opened Research and models, loaded schema-4 history and visibly rendered the page.
The research DB appeared under the configured absolute data directory and no
checkout-relative folder was created. No source/provider request was made. The
empty owned smoke reproduction was preserved outside the checkout; user data was
not reset or automatically migrated. Native Windows acceptance is still separate.

Final model review added dcf-1.1 scope/date guards with explicit legacy dcf-1
replay/label preservation. New unsupported or future-reviewed models are rejected;
no silent evidence upgrade. 324 tests and the updated model-editor harness pass.

Final independent hardening merged through PR31 (`6f8571b`) after exact-head
Windows/security CI 43552911371 at `a0d15a5`. The clean local ZIP for that exact
head was verified: all 301 file hashes match, 31 dependencies have copied license
evidence, x64 SQLite is present, and no settings/database/key files were bundled.
Original repository text/binary attributes are retained alongside the exact-byte
notice exception. Full product acceptance remains externally gated.

The PDF-only script boundary is now enforced before output; unsupported Myanmar/
CJK text leaves no partial PDF while CSV/XLSX retain its Unicode. Final suite325.
