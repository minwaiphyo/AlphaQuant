# AlphaQuant developer and agent guidance

## Context and document ownership

This tracked file is the entry point for developers and agents across chats and laptops. Read it before working; do not assume previous chat history is available. Keep shared workflow instructions, continuation context and workflow decisions here. Keep this file concise and update its current handoff after substantive work.

`AlphaQuant_Proposal.md` owns software requirements, proposed ideas, product and technical decisions with rationale, architecture, implementation status, known issues, verification and releases. `README.md` gives the user-facing overview and, when implemented, setup and usage. Read proposal sections 17 through 19 for the latest software state, then the relevant detailed sections. Do not duplicate changing software specifications here.

Distinguish proposed, approved, deferred, rejected, superseded, implemented and verified states. Preserve reasons and replacement references. Record durable facts rather than transcripts. Public files must exclude secrets, personal information and private motivations. Private files and earlier chats are not prerequisites for shared project work.

### Start of every task

1. Read `AlphaQuant_Proposal.md` and `README.md`, then inspect relevant source, tests and recent commits. Start with sections 17 through 19 for the current state; consult earlier sections for definitions and constraints.
2. Inspect Git status, branch and remotes. Fetch the configured remote and fast-forward the appropriate branch only when safe. Preserve local changes and reconcile divergence deliberately; never reset or rewrite history as a routine refresh.
3. Compare documentation with actual files and verification evidence. Record discrepancies and resolve them before relying on stale claims. Missing context must remain an explicit open question rather than an invented requirement.
4. Select one small milestone with observable acceptance criteria. Use a descriptive feature branch. Resolve routine engineering choices autonomously; seek clarification for material product or financial decisions, missing credentials or actions beyond existing authority.

### During development and discussion

Update `AlphaQuant_Proposal.md` whenever a substantive requirement, idea, decision, implementation result or blocker changes, including planning-only sessions. Record workflow-only changes here instead of in the software proposal. Save the context when the change is established and before handing off or ending a session; do not wait for a release. Keep the applicable detailed sections and the current-state summary aligned with the dated logs.

For each material idea, record its purpose, status, rationale, affected scope and next validation step. Record rejected or deferred alternatives when their rationale will prevent repeated discussion. For implementation, record changed behavior, relevant files, the commit or branch when available, checks actually run and their results, limitations and unfinished work. Never reuse stale test results as current evidence.

Keep UI, calculations, ingestion and persistence responsibilities clear. Implement only approved financial scope. Use reviewed fixtures, explicit periods and units, missing-data handling and source lineage as financial behavior is introduced. Run proportionate checks for each increment and preserve the architecture, data-flow and release diagrams as the design changes.

### Completion and synchronization

1. Update sections 17 through 19 with completed work, actual verification, remaining issues and one exact next action. Add dated decision records for changed direction; update detailed requirements and README when affected.
2. Keep the local PDF edition synchronized with `AlphaQuant_Proposal.md` after substantive changes. Preserve any private-only prefix in its editable source, refresh the shared proposal content, regenerate the PDF and visually inspect every page. The local PDF and its source remain untracked; Git does not transfer them between laptops. A fresh checkout must still have complete shared project context in the tracked proposal and this guidance. Report unavailable local files or failed regeneration explicitly.
3. Inspect status, filenames and diffs before staging. Check ignore rules and scan staged content for secrets and private information. Never force-add private artifacts. Commit coherent changes and push the agreed feature branch when authorized; preserve peer review and required checks before merging or releasing.
4. Report saved, verified, committed and pushed states separately. If a file is locked, a check fails or synchronization is blocked, preserve the work and record what remains pending. Never claim a local save is remotely synchronized.
5. On another laptop, fetch the latest branch and repeat the startup checks. Reconcile concurrent proposal edits deliberately and coordinate local-edition updates. Do not depend on ignored files or an earlier chat for shared requirements and decisions.

## Collaboration and environment

Use subagents only when explicitly authorized by the user or applicable instructions; discussion of multi-agent workflows alone is not authorization. Give delegated work clear ownership and acceptance criteria. Coordinate shared files and leave integration and verification with one owner. Do not let concurrent writers overwrite documentation or binary documents.

Before scaffolding, inspect installed .NET tooling and verify a supported stable Avalonia/runtime combination. There are no validated application build or test commands yet. Add exact commands after implementation and verification; do not invent passed checks.

If an authorized private master context is available, follow its synchronization instructions and serialize edits. A missing private master must not block public project work. Never publish private context as a workaround. Keep ignored handovers consistent with this tracked guidance where available.

## Workflow decisions

- AQ-D005, 2026-10-25, superseded in document placement: shared context and continuation instructions were initially placed in proposal revision 0.4, with the local PDF synchronized.
- AQ-W001, 2026-10-25, approved, replaces AQ-D005 placement: agent/developer instructions, workflow decisions and continuation handoffs belong in tracked `AGENTS.md`. The proposal describes the software and retains product/engineering decisions and progress. Preserve private/public boundaries and synchronize the untracked local PDF with software documentation changes.

## Current handoff

- Documentation separation completed on `codex/separate-agent-guidance`, based on `codex/proposal-continuation-workflow` at `d9d0f7a`. The earlier proposal update was pushed on that branch and has not been merged into main. A fresh checkout of main therefore does not yet contain these changes; fetch and select the development branch until review and merge.
- Proposal revision 0.5 retains software scope, diagrams, candidate definitions and implementation status. No application code, tests, CI, installer or release exists.
- Exact next action: review proposal sections 4, 5 and 15 to select the first workflow, metric and supported universe; then choose a bounded implementation milestone. A compatibility/UI foundation can be scoped separately from financial behavior.
- Check Git status and remote state for current save/commit/push status rather than relying on embedded hashes. Report pending checks or synchronization honestly.
