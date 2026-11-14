# Research storage contract (S04)

Local research uses Microsoft.Data.Sqlite 10.0.15 (stable NuGet release in the
.NET 10 provider line), parameterized SQL, foreign keys, full synchronous writes,
short-lived nonpooled connections and serialized access. Review its MIT license
and SQLitePCLRaw/native notices after restore. This is a zero-cost local dependency,
not a remote service. Settings/contact, OAuth tokens and market quotes remain
outside the research store.

Restored NuGet metadata confirms MIT for Microsoft.Data.Sqlite/Core 10.0.15 and
Apache-2.0 for the SQLitePCLRaw 2.1.13 bundle/core/provider/native packages.
These notices belong in subsequent distribution audits; dependency selection
does not close the separate Windows signing gate.

## Schema and records

Schema 1 holds CIK-keyed companies, named sets and ordered members. Schema 2 adds
optimistic revisions and soft deletion. Peer sets contain 2–5 distinct CIKs;
watchlists contain 1–100. Names are trimmed, bounded and parameterized. Tickers
are normalized metadata, never primary identity. IDs are GUIDs; timestamps are UTC.
Updates/deletes/restores require the revision read by the caller. Historical
snapshots and later research revisions use separate immutable records, not mutable
session-object serialization.

New files receive an application ID and explicit schema version. An existing
zero-length, foreign, corrupt, newer-version or malformed database is never
silently initialized/reset. The default location is the local application-data
AlphaQuant research directory, separate from settings.json and rebuildable cache.

## Transactions, migration and recovery

Operations cooperate through a per-path process semaphore and an exclusive access
file while holding short-lived SQLite connections. External/noncooperating SQLite
locks are handled by SQLite with a bounded busy timeout; failures are explicit.
Cancellation before a transaction commits rolls back; the synchronous atomic
SQLite backup commit cannot be interrupted once started.

Before migrating an existing supported schema, make a unique online SQLite backup,
validate its integrity/application/schema version, then migrate transactionally.
On failure keep the original schema and the verified backup; never overwrite the
only backup. No destructive migration or downgrade is permitted.

Backups use SQLite's online backup API, not copying an active database file.
Restore first validates and upgrades an isolated staging copy, then preserves a
verified backup of the current valid database and atomically restores through
SQLite's backup API. A corrupt current file is left untouched: restore into a new
chosen store path instead. Existing unrelated files and unsupported future schemas
are not overwritten. Only owned temporary staging files may be removed.

## Acceptance

Use isolated directories for fresh/reopen, schema-1 upgrade, failed upgrade,
backup/tamper/corrupt input, restore, cancellation, read-only, busy/lock and
concurrent-instance tests. Verify revisions, ordering, foreign keys, SQL-like user
names, unchanged input after failed operations, and no settings/token serialization.
Store failures must remain actionable errors; no empty-store success fallback.
Desktop saved/offline integration belongs to S05. No live SEC/Alpaca requests or
market-data retention is authorized by storage work.

## S04 verification checkpoint

On 2026-11-13, the cloud Release solution build passed with zero warnings/errors;
224 synthetic .NET tests passed, including 14 isolated research-storage tests.
These exercise failed second-step migration rollback, verified pre-migration and
pre-restore backups, corrupt/foreign/future files, restore into a fresh path,
parameterized SQL-like names, optimistic revisions, soft-delete recovery,
read-only access, pre-cancellation, mid-write abort rollback and both cooperative
and external SQLite locks. Skill-resource validation also passed.

Remote exact-head CI/PR delivery and actual Windows execution remain unverified.
The old S02 live-data and Windows signing gates remain open; no source data,
prices, contacts or market sessions were persisted by these tests.

## S05 saved/offline research

Schema 3 adds an optional typed filter/sort preset on each named set and immutable
fundamentals-only snapshot records. The schema-2 upgrade is backed up and
transactional. Current snapshot payload schema 2 preserves original decimals, reporting
periods, weighted source facts, retrieval dates and definition version; it does
not recalculate old results or persist quotes, valuations, session-error text,
contact settings or credential objects. Payloads are bounded to 2 MiB, with a
50 MiB supported database limit. Unknown payload schemas fail explicitly.

Payload schema 2 records each issuer's calculation-definition version. Legacy
payload schema 1 retains its original outer version for every company. A partial
refresh never relabels retained old financials with the current definition; mixed
versions remain visible per issuer, with numeric filtering/sorting disabled until
one supported version is available. Old snapshots remain readable and are never
silently rewritten.

New captures also exclude reviewed debt values/references: comparison-1.1 debt is
a session capital-review input even though it appears among fundamentals.
Snapshots mark that field unavailable with an explicit retention reason. Public
filing cash/history and their source inputs remain unchanged. Existing immutable
snapshot records are never silently rewritten.

Current-schema backups and restore staging validate logical set/preset/snapshot
payloads as well as SQLite integrity and foreign keys. A physically valid database
with malformed research JSON is rejected before replacing the current store.
Supported migrations validate the resulting records inside the schema transaction;
bad logical data rolls back and leaves the verified old backup available.

The Saved research page supports new named sets, rename, peer replacement,
duplicate-free watchlist append, explicit member removal, recoverable deletion,
backup and confirmed restore. Select 2–5 watchlist members explicitly; selection
is never silently truncated. Capturing a financial snapshot requires exact saved
membership and the expected set revision. Later membership changes do not rewrite
old snapshots. Applied filter/sort state is stored with its definition version;
older unsupported presets are not silently reapplied.

Opening saved evidence starts no source request and clears current prices. It
waits for the preceding stream to close, preserves source/retrieval dates and
labels the view offline. Returning from Settings remains offline; Refresh is an
explicit new request. Stale or late selection responses cannot replace a newer
set's evidence. Missing/corrupt data produces an error instead of a successful
empty research list. SEC contact and reviewed capital inputs remain separate.

Recovery uses IResearchStore.RestoreAsync at a new chosen database path if the
original file is corrupt; the corrupt file is never overwritten. The normal UI
restores verified backups over an existing valid store only after confirmation
and preserves a verified pre-restore backup. Selecting a different recovery path
in the UI is not yet implemented; preserve the original and use the documented
API/diagnostic recovery path rather than deleting it.

S05 validation on 2026-11-13: 241 .NET tests passed, including snapshot replay,
old definitions, required-field/schema corruption, membership revision conflicts,
real market-metadata exclusion, no-network restart/Settings recovery, watchlist
editing and delayed-selection isolation. The Avalonia headless fixture produced
12 PNGs under artifacts/ui-stage05-final. Light/dark and a 648x560 research
viewport, scrolled members, backup controls and offline evidence were inspected;
keyboard focus/Escape checks passed. These are synthetic Linux rendering checks,
not native Windows accessibility/scaling certification. Remote CI/delivery remains
pending GitHub API access; PR24 itself remains unmerged.

## Schema 4 research revisions

The research_documents table stores append-only thesis/rule/model revisions with
optimistic concurrency and soft archive/recovery. Models and source snapshot IDs
are typed and validated before commit; saved scenario assumptions replay under
forecast-1/dcf-1. Manual condition reviews retain their snapshot/time/rule evidence.
Current backups validate revision continuity, payload shape, models and links.
Supported schema 1–3 migrations create verified backups and run transactionally.
See Research_Workbench.md and the versioned research-model contract.

Default settings/cache/research paths require an absolute OS local-data root, even
when it does not yet exist. They never silently fall back to the current checkout.
If a previous Linux run created a relative AlphaQuant directory, preserve it and
inspect/backup it before any explicit migration; do not delete or auto-merge files.
