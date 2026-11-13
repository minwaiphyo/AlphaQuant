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
