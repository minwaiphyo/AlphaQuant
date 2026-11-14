# Saved research and valuation review delivery

Date: 2026-11-14. Stages S04–S06, roadmap 1.0.

The cumulative branch delivers SQLite research storage/recovery, named peer sets
and watchlists, immutable offline financial snapshots and session-only structured
valuation reviews. Final provenance and logical-backup safeguards span the three
stages, so they are reviewed and delivered together. PR24 is already merged.

## Acceptance and review

- Storage: isolated tests cover reopen, schema upgrades and rollback, verified
  pre-migration/pre-restore backups, corrupt/foreign/future files, revision
  conflicts, cancellation, read-only locations and cooperative/external locks.
- Offline research: tests preserve exact decimals, dates, weighted facts and
  per-company definition versions through restart and partial refresh. Saved
  snapshots exclude quote/session metadata and reviewed debt inputs. Old payloads
  retain their original definitions. Malformed payloads and logically invalid
  backups fail explicitly without replacing research.
- Reviews: tests cover independently expected P/E and EV/sales, explicit zeros,
  negative EV, stale/future dates, 135/136-day boundaries, unsupported scope/class,
  atomic replacement, per-company clearing, JSON interchange and shutdown clearing.
  Entry never certifies source facts or bypasses fresh-price requirements.
- UI: synthetic saved/recovery/review/offline states exercise real Avalonia/Skia
  rendering with focus checks. Inspected dark minimum-size saved/recovery, capital
  fields and offline source evidence. No production settings or providers used.

The source review covered parameterized transactions, staging before restore,
backup preservation, no credential/quote serialization, asynchronous selection
races and offline Settings return. No blocking defect found. This is self-review,
not independent finance certification. See Research_Storage.md and
Valuation_Review.md for contracts and supported limits.

## Current validation

After merging current main, Linux Release: 274 .NET tests passed, zero failed or
skipped; solution build zero warnings/errors. Skill validation passed for six
skills, 20 routes and eight resources. All 19 Python tests and isolated PowerShell
installer checks passed. UI harness generated 19 PNG states and passed keyboard/
focus checks. SourceCapture preflight reported missing SEC contact and sent no
requests. Required exact-head Windows CI/packaging remains a pre-merge gate;
the stage ledger records the current PR and run.

## Remaining gates

No live SEC/Alpaca reconciliation, OAuth registration/deployment, provider rights,
Windows signing, native Windows accessibility/scaling or owner-PC acceptance is
claimed. These remain separate gates. Linux and CI checks cannot close them.
No release or environment publication is part of this delivery.
