---
name: alphaquant-research-storage
description: Design or verify AlphaQuant SQLite research persistence, versioned snapshots/theses/scenarios, migration, backup, restore and offline behavior. Use for durable research changes, not session-only OAuth storage or routine database queries elsewhere.
---

# Research persistence and recovery

The roadmap selects local SQLite. Verify the stage is authorized and inspect actual repository/storage interfaces before adding schemas. This skill does not create a database merely by being loaded. Read [the persistence contract](references/persistence-contract.md).

Keep facts, calculation/assumption versions, research revisions and current UI selections distinct. Preserve reproducible snapshots instead of silently recalculating/replacing saved historical research. Price retention requires actual source rights. A cached quote is not valid current valuation.

Use parameterized access, atomic transactions and explicit schema versions. Design verified pre-migration backup and recovery before implementing a destructive migration. Never delete/reset a user database as routine error handling.

Provider tokens/application secrets do not belong in research storage, backups, exports or diagnostics. Existing SEC contact settings remain separate. Use isolated directories for all tests.

Deliver schema/migration rationale, atomicity/replay behavior, interrupted-write and restore evidence, and the exact limits of supported upgrades/offline use. A successful fresh install does not prove safe migration or restore.
