# Research and models workbench

S13–S19 independent engineering, 2026-11-14. Open **Research and models**.
No source/provider request is initiated from this page.

- Thesis: enter user-authored thesis, variant view, catalysts, risks, invalidation,
  horizon, peer notes and conclusion. Explicit saves create immutable revisions.
  Loading replaces the editor only on command. Reload preserves unsaved fields;
  stale writes fail without discarding the draft. Archive/recover append revisions.
- Link existing frozen financial snapshots. Clear links also clears pending condition
  reviews. Missing links prevent a falsely complete saved report.
- Rules: inspect/edit defaults, thresholds and weights; evaluate the selected dated
  snapshot or in-memory comparison. Missing/stale inputs cannot improve a score.
  Record a snapshot condition review in the next explicit revision; no external
  reminders/background polling. Rule weights are not probabilities or ratings.
- Models: fill every annual assumption (1–10 years) and base historical/source
  note. Fields begin blank; enter zero explicitly. Forecast cash-flow bridges and
  DCF terminal/capital assumptions are inspectable. Model results are a calculated
  snapshot; edits require recalculation. No current-price upside is claimed.
- Save independent Bear/Base/Bull copies, or a new revision. Labels are user names,
  not probabilities. Versioned assumptions replay after restart.
- Sensitivity is bounded to 100 cells/evaluations. Reverse solves only constant
  explicit-year revenue growth with other assumptions/terminal next-FCFF fixed;
  derivative bounds must prove uniqueness on the specified interval and the
  candidate must forward-reconcile. Invalid/uncertain/no-solution stays explicit.
- Reports freeze a selected SAVED revision; unsaved changes never enter export.
  Preview before CSV/XLSX/PDF. New files only, atomically committed; existing files
  and source research survive failure/cancellation. No upload/sharing is automatic.
- Changes compares saved snapshots on demand, separating definition, period/source
  and value changes. Sourced fundamental charts retain original periods/values and
  gaps. Historical valuation remains blocked on point-in-time data and rights.

Definitions and supported limits are in ../requirements/Research_Models.md. Schema
4 adds research_documents; supported schema 1–3 upgrades preserve verified backups
and rollback if later validation fails. Backups/restores validate document models,
revision identity/history and snapshot links. A corrupt current file is preserved;
restore into a new chosen path using the existing recovery procedure.

Validation: 319 synthetic Release tests, zero-warning/error build; 100-revision
reopen/backup-corruption test; model/scenario replay; independent rational/forecast
examples; missing/partial/invalid/cancelled inputs; culture/injection/file failures.
The real Avalonia harness generates thesis/rules/model/report/history states with
keyboard checks. Minimum dark thesis, rule, model bridge and chart renders inspected.
XLSX independently reopened with openpyxl (six columns, no formulas); PDF reopened,
rendered with pdftoppm and inspected. Unicode alpha and source links retained.
Current transitive NuGet audit reports no vulnerabilities after pinning patched
Pkcs 10.0.15. Native Windows, provider/live finance and historical-rights gates
remain external; no release, deployment or independent professional review claimed.

New per-share models use dcf-1.1: explicitly confirm supported simple domestic
non-financial USD scope and complete claims, and enter the reviewed actual-share/
capital date. Legacy dcf-1 revisions remain readable/replayable with a scope warning;
loading them does not silently confirm new evidence. The final suite has 324 tests.

PDF export rejects scripts outside the bundled basic Latin/Greek/Cyrillic and
supported punctuation/math ranges before writing. Use CSV/XLSX for full Unicode
(including Myanmar/CJK); the app reports this limitation without replacing output.
The final suite has 325 tests including this no-partial-file case.
