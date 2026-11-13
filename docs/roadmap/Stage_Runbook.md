# Autonomous stage execution runbook

Updated: 2026-11-13. Companion: [plan](Development_Plan.md), [ledger](Stage_Ledger.json).

## Commands the owner can use

| Instruction | Meaning |
|---|---|
| Proceed with the next stage | Execute the lowest-numbered unfinished stage whose start prerequisites are satisfied, including checks and Git delivery |
| Proceed with Stage S06 | Execute that stage, completing necessary unfinished prerequisites first when possible. Announce the selected prerequisite |
| Resume Stage S06 | Recover its saved work and continue the next incomplete package, without rebuilding finished work |
| Continue autonomously through ready stages | Continue across stages until no independent approved work remains, a platform/session limit intervenes or the owner stops work |
| Give me a status update | Report actual ledger/Git/CI status. It does not cancel active work |
| Pause after the current package | Finish a safe checkpoint, save/push permitted work and stop |

The first proceed/acceptance instruction after reviewing version 1.0 records plan approval and the invocation in the ledger. Planning answers authorize the listed policy choices but do not themselves start application implementation.

One-stage commands normally stop after that stage is done. If it becomes externally blocked, the owner has authorized independent-stage fallback: record the blocker, announce the next ready stage and complete one independent fallback stage, then report both outcomes. Multiple-stage continuation requires an instruction such as "continue autonomously through ready stages". Do not ask again about Git delivery, delegated definitions, SQLite or authorized validation. Do not interpret a stage invocation as permission for excluded product features.

An invocation starts work in the current active agent/session. Documents do not create a background daemon, start a new chat automatically, schedule work or give an agent capabilities it lacks. A future agent resumes when invoked and given this repository. Checkpoints make that restart inexpensive.

## Startup, every time

1. Read README, this runbook, the plan, the ledger, developer guidance and proposal current-state/decision sections. Read the selected stage's detailed definition and relevant finance/UI specs.
2. Inspect local changes, branch, remote, current PRs and available tools. Fetch safely. Preserve other people's work. Never reset, clean or force-push to obtain an apparently clean baseline.
   Check that CI runners, artifact retention and any chosen execution/deployment service meet the zero-required-cost constraint before enabling usage. Never assume a repository/service still has the same billing eligibility as an earlier run.
3. Reconcile ledger against source, Git and CI. The prior 203-test result is historical evidence. Reuse unchanged verified work but do not invent a new run or assume an artifact exists on this laptop.
4. If plan approval is still draft and the current instruction accepts/invokes it, record the user's instruction/date as approval. Update the plan approval header and current proposal/handoff references in the same change, so the next agent does not reopen approval. Otherwise remain in planning. Product decisions must match the accepted plan version.
5. Select stage/package by prerequisites and actual progress. Resume an existing compatible branch/PR where possible. Create a descriptive branch for new work, following the active tool/platform branch convention.
6. Claim ownership in the ledger: stage, run identifier, branch, baseline SHA, selected package and declared file areas. Check for another active owner. Do not assume ownership expired merely because time passed.
7. Give a concise progress update with outcome, scope, any known external gates and the chosen package. Begin authorized work rather than asking to reapprove the stage.

## Work and checkpoint protocol

For each package:

1. Verify prerequisites and write a brief actionable approach against its acceptance criteria. Read relevant code/tests, not every file in the repository.
2. Implement a small coherent increment. Preserve teammate changes and layer boundaries. Choose routine engineering details within standing authority.
3. For financial changes, write/update a versioned definition before behavior changes. Record formula, units, period/ownership/scope, source mappings, exclusions, invalid/missing treatment and an independently recomputed example. Test the definition, not a copy of production arithmetic. Do not fabricate independent certification.
4. For persistence, migrations, auth or provider changes, exercise meaningful failure/recovery cases with isolated fixtures. No production data reset or weakened security.
5. Validate the changed behavior. Run appropriate tests and the full required solution/CI checks before merge. Use relevant document/spreadsheet/PDF/computer-use skills when their artifacts require it. Read their instructions and comply with platform approvals.
6. Self-review the diff for correctness, scope, financial meaning, cancellation/lifetime behavior, secret leakage, persistence safety and user-facing clarity. Fix material findings before delivery.
7. Save a checkpoint in the ledger after substantive progress and before tool/session/context limits. Use actual evidence and one exact next action. A local save is not a pushed checkpoint.

Each checkpoint includes:

```text
Stage/package and plan version
Base commit, branch, current commit or uncommitted files
Completed work packages and remaining acceptance criteria
What changed and why
Validation commands/results and relevant fixture/source references
Known failures or uncertainty
Non-secret external blocker IDs and unblock action
Next exact command/file/package to work on
Owner/run identity and current state
PR/CI/artifact references, including merge state
```

Tracked records must be sufficient on another laptop. Ignored files may hold local artifacts but cannot be the only copy of a plan, expected calculation, blocker or next step. Never put tokens, personal contacts, secret values or sensitive diagnostics in these records.

Blocker entries use stable IDs with affected stage/package IDs, category, first/last check date, concrete evidence, attempted safe alternatives, exact unblock action, owner/access dependency and independent work still available. Checkpoints reference blocker IDs rather than copying inconsistent status into multiple documents.

## Delivery and merge

The accepted stage authorization covers commit, push, PR and normal merge. Complete authorized preparation without repeatedly asking for permission.

1. Inspect filenames/diffs and secret/ignore boundaries. Do not stage unrelated changes or force-add private artifacts.
2. Commit coherent work and update the stage PR description around the final problem, behavior, checks and limitations. Use a body file/structured argument for multiline text.
3. Fetch current main and reconcile concurrent work without discarding it. Validate affected behavior and await all required checks for the exact final PR head. Do not bypass branch protection or claim self-review was a separate reviewer.
4. Merge normally when required checks pass and no unresolved critical/high-impact defect remains. Update local main only when safe.
5. Record merge SHA, CI and artifact references in the ledger. Update proposal, current handoff and affected user docs. Where the merge SHA was unknown pre-merge, a small delivery-record commit/PR may finalize it.
6. Mark done only after this record is delivered. A passed build with an unmerged required PR is verified work, not done delivery. A draft release is not a deployed product.

A release stage may publish the accurately scoped supported preview/pilot only after its own data, rights, environment and distribution matrix passes. Otherwise prepare a draft, keep the unmet release task open and continue independent work. Do not upgrade accounts, spend money, send messages or bypass authentication/security to close a gate.

## Scheduler and blocker policy

The ledger is the execution state source. The plan supplies acceptance contracts. If they disagree, reconcile explicitly from owner decisions and source evidence rather than silently choosing the easier completion claim.

- Normal selection: unfinished stage with satisfied start dependencies, in ascending ID order. Within a stage, use the next unfinished package whose prerequisites are satisfied.
- Named-stage selection: respect the requested target. Complete its necessary ready prerequisite first. If its only missing prerequisites are external, record them and use independent fallback.
- Blocked-stage selection: finish independent packages in it, then scan other stages. A release-only completion dependency need not prevent clearly specified independent engineering work, but can never be ignored to mark completion.
- Failed tests or defects are engineering work, not an external blocker. Investigate and fix them. After repeated identical tool/environment failures, preserve evidence and try a safe alternative. Do not loop indefinitely or mark a difficult task complete.
- True external blocker: missing account access, secret placement, required consent, host capability/cost eligibility, provider permission, trusted distribution or unavoidable platform limit. Record exactly what was checked, the evidence, owner action and what remains possible.
- Recheck a blocker only when its prerequisites/configuration change or a new invocation warrants a bounded readiness check. Do not repeatedly ask the same unanswered question in one run.
- Never substitute delayed prices, invent zero claims, assume data rights, fabricate model results or make a purchase to obtain a green status.

When nothing independent remains, save/push the checkpoint and report the exact remaining gates. Do not claim work will continue unattended after the session ends. No reminder/automation or new agent is created unless separately authorized.

## Ledger state meanings

| State | Meaning |
|---|---|
| planned | Defined, not started. Readiness is derived from approval/dependencies |
| in_progress | An owner has claimed actual implementation work |
| blocked | Remaining packages need an identified external dependency, with partial work/evidence preserved |
| verifying | Implementation is being checked, reviewed or awaiting required CI |
| delivering | Verified work is being committed/pushed/merged and its delivery recorded |
| done | All packages, acceptance and required delivery evidence are complete |

Plan approval is a separate field. A planned stage with met dependencies is not runnable until approval/invocation applies. Current gate status is non-secret evidence, not permission to take excluded actions. Split or revise a stage only with a recorded rationale and preserved IDs/traceability, never to hide blocked deliverables.

## Ownership and interruptions

Use one active writer per stage/shared file area. This plan allows different agents to resume work, not uncontrolled concurrent editing or subagent spawning. An explicit resume may transfer inactive ownership after checking available branch/PR/session state. If another owner appears active and cannot be coordinated through authorized means, select non-overlapping ready work or report the conflict rather than overwrite it.

At interruption/context rollover, checkpoint immediately. On resume, inspect actual source/diff/tests first. If changes are uncommitted, preserve and continue them. If main has the package already, verify lineage and mark it complete with evidence rather than duplicate it. If a PR is pending, resume that PR. Never treat a fresh chat as a new project or erase a prior partial result.

## Stopping and reporting

For a one-stage invocation, finish when that stage is delivered, subject to the authorized blocker fallback. For an all-ready-stages invocation, continue until no independent approved work remains or a real execution limit/user stop intervenes. Save/push a safe handoff before returning.

Report outcome first, actual checks, committed/pushed/merged states, artifact/deployment status, unresolved gates and the next runnable stage. Never present a guessed percentage or elapsed time as completion evidence. Keep user questions to missing consequential authority or access that is genuinely required, while continuing unaffected work.
