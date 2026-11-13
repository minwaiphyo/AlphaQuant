# Durable research contract

No storage implementation is assumed merely because S04 is planned. Inspect current code and select a compatible maintained provider/license within the authorized stage. Prefer the smallest dependency that supports required behavior.

## Distinct records

- Issuer/security identity uses stable keys and explicit symbol/class context, not a mutable ticker as every relation's identity.
- A captured financial result retains source facts, reporting/retrieval/as-of dates, definition version and missing/unsupported reasons.
- A scenario/forecast retains immutable assumption revisions and model versions. A thesis points to the intended revision/snapshot, not whatever is current at export time.
- Watchlists/peer sets/preferences are editable state with explicit duplicate/delete semantics.
- Secrets/session objects are never serialized into these records. Contact settings remain separate.

## Migration and recovery checks

Specify source/target schema versions, invariant preservation, transaction boundary, backup identity, restore validation and failure behavior. A rollback plan that only works before any data is written is insufficient for an interrupted migration.

Check fresh schema, supported old versions, interrupted transaction/migration, read-only disk, locks, malformed/unsupported versions and restore into an isolated copy. Verify foreign-key/revision relationships and saved-definition compatibility. Do not overwrite the only valid backup or initialize an empty DB over an unreadable user file.

Use explicit error/recovery states instead of pretending failed writes succeeded. Parameterize user text and ensure concurrent writes/cancellation do not leave partial research. Keep import/export atomic and validate size/type/version before replacement.

Offline research keeps original dates and stored limitations. Refresh failure must not erase usable data. Persisting a price series needs verified rights and point-in-time semantics, not just available disk space.

Reference: [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) for the proposed provider family. Recheck exact compatibility before adding a dependency.
