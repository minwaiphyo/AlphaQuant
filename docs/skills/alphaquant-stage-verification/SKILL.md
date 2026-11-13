---
name: alphaquant-stage-verification
description: Execute or review an AlphaQuant roadmap stage with package-level evidence, domain-specific checks, safe resumption and honest release gates. Use for stage delivery or pre-merge verification, not to turn unrelated edits into a full audit.
---

# Stage quality and reproducible delivery

Read `docs/roadmap/Stage_Runbook.md`, the selected contract and `Stage_Ledger.json`. Read `docs/skills/Skill_Routing.md` and load only skills needed by the package's actual changes. This skill does not activate a draft stage or expand its scope/authority.

Define the acceptance evidence before writing code. Use [the evidence contract](references/evidence-contract.md) to distinguish source reconciliation, mathematical correctness, UI behavior, lifecycle safety and live release readiness. A high test count is not a substitute for those properties.

Inspect actual changes, run proportional checks and self-review the diff. Preserve unrelated work. For formulas require independent expectations, for UI actual relevant state/layout evidence, for persistence migration/recovery, and for connections negative/lifecycle cases. Do not demand every domain skill or every suite for a wording-only edit.

Record actual commands/results, source/model/fixture versions, what was not verified and exact next action. Required checks must pass on the final PR head before normal merge under standing stage authorization. Self-review is not a separate reviewer and no subagent is implied.

If blocked externally, retain partial progress and use the runbook's independent fallback. Do not substitute fake evidence or waive a release gate. Update the ledger/handoff so another agent resumes from the current package instead of repeating finished work.
