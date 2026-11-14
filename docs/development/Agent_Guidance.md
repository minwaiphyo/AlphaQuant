# AlphaQuant developer and agent guidance

## Context and document ownership

This tracked file is the entry point for developers and agents across chats and laptops. Read it before working; do not assume previous chat history is available. Keep shared workflow instructions, continuation context and workflow decisions here. Keep this file concise and update its current handoff after substantive work.

`docs/product/AlphaQuant_Proposal.md` owns software requirements, proposed ideas, product and technical decisions with rationale, architecture, implementation status, known issues, verification and releases. `README.md` gives the user-facing overview and, when implemented, setup and usage. Read proposal sections 17 through 19 for the latest software state, then the relevant detailed sections. Do not duplicate changing software specifications here.

Distinguish proposed, approved, deferred, rejected, superseded, implemented and verified states. Preserve reasons and replacement references. Record durable facts rather than transcripts. Public files must exclude secrets, personal information and private motivations. Private files and earlier chats are not prerequisites for shared project work.

### Start of every task

For domain work, load the relevant project skills from [the routing guide](../skills/Skill_Routing.md). Canonical `docs/skills/alphaquant-*/SKILL.md` files are tracked and portable. `tools/Install-ProjectSkills.ps1` creates scoped ignored discovery links without overwriting existing skills. Missing native discovery is not a reason to skip the canonical instructions. Do not load unrelated skills, activate a draft stage or expand permissions merely because a skill exists.

For a roadmap instruction such as "proceed with the next stage", read [the execution runbook](../roadmap/Stage_Runbook.md), [stage plan](../roadmap/Development_Plan.md) and [ledger](../roadmap/Stage_Ledger.json) first. They contain the 2026-11-13 owner choices for delegated definitions, local SQLite, bounded configured live validation, autonomous Git delivery and blocked-stage fallback. A reviewed stage invocation accepts the draft operational plan and authorizes its bounded work. Do not ask to re-plan an unchanged accepted stage. Record real progress/checkpoints and preserve external gates. Reading a draft is not permission to start application changes.

1. Read `docs/product/AlphaQuant_Proposal.md` and `README.md`, then inspect relevant source, tests and recent commits. Start with sections 17 through 19 for the current state; consult earlier sections for definitions and constraints.
2. Inspect Git status, branch and remotes. Fetch the configured remote and fast-forward the appropriate branch only when safe. Preserve local changes and reconcile divergence deliberately; never reset or rewrite history as a routine refresh.
3. Compare documentation with actual files and verification evidence. Record discrepancies and resolve them before relying on stale claims. Missing context must remain an explicit open question rather than an invented requirement.
4. Select one small milestone with observable acceptance criteria. Use a descriptive feature branch. Resolve routine engineering choices autonomously; seek clarification for material product or financial decisions, missing credentials or actions beyond existing authority.

### Planning gate before implementation

Assess each requested feature or change after the initial read-only inspection. Prompt the user to switch to planning mode before implementation when any of these apply:

- The task is large or spans several interacting components with significant integration work.
- The goal, scope, user behavior or acceptance criteria are ambiguous or materially incomplete.
- Financial definitions, data-selection rules, architecture or other consequential design choices need refinement.
- The change introduces substantial persistence, migrations, external ingestion, installation/distribution or recovery behavior whose failure cases need agreement.

Explain briefly which uncertainty or complexity triggered the gate, and explicitly ask the user to switch to planning mode so the idea can be refined. Pause implementation until the user responds; do not scaffold, edit implementation files or install dependencies while waiting. Read-only investigation may continue to identify constraints and useful questions. Do not claim to have switched modes yourself; the active mode must be changed through the available user-facing controls.

In planning mode, inspect the existing project, clarify material decisions, and produce a bounded plan covering scope and exclusions, assumptions, affected components, implementation steps, acceptance criteria, tests and relevant failure cases. Let the user refine the plan. Begin implementation only after the user accepts the scope and the session returns to an implementation-capable mode. If planning mode is unavailable, explain that and ask whether to conduct a planning-only discussion instead; do not silently bypass the gate. An explicit user instruction to proceed without planning overrides this default; record material assumptions and decisions.

Skip this gate for small, well-defined work such as typos, focused documentation edits, simple UI adjustments and understood bug fixes. Do not repeat the prompt when the task is already in planning mode or is executing an accepted plan. If new material uncertainty or scope expansion arises during implementation, pause the affected work and revisit the gate. Store resulting software decisions in the proposal and workflow decisions here.

### During development and discussion

Update `docs/product/AlphaQuant_Proposal.md` whenever a substantive requirement, idea, decision, implementation result or blocker changes, including planning-only sessions. Record workflow-only changes here instead of in the software proposal. Save the context when the change is established and before handing off or ending a session; do not wait for a release. Keep the applicable detailed sections and the current-state summary aligned with the dated logs.

For each material idea, record its purpose, status, rationale, affected scope and next validation step. Record rejected or deferred alternatives when their rationale will prevent repeated discussion. For implementation, record changed behavior, relevant files, the commit or branch when available, checks actually run and their results, limitations and unfinished work. Never reuse stale test results as current evidence.

Keep UI, calculations, ingestion and persistence responsibilities clear. Implement only approved financial scope. Use reviewed fixtures, explicit periods and units, missing-data handling and source lineage as financial behavior is introduced. Run proportionate checks for each increment and preserve the architecture, data-flow and release diagrams as the design changes.

### Completion and synchronization

1. Update sections 17 through 19 with completed work, actual verification, remaining issues and one exact next action. Add dated decision records for changed direction; update detailed requirements and README when affected.
2. Inspect status, filenames and diffs before staging. Check ignore rules and scan staged content for secrets and private information. Never force-add private artifacts. Commit coherent changes and push the agreed feature branch when authorized; preserve peer review and required checks before merging or releasing.
3. Report saved, verified, committed and pushed states separately. If a file is locked, a check fails or synchronization is blocked, preserve the work and record what remains pending. Never claim a local save is remotely synchronized.
4. On another laptop, fetch the latest branch and repeat the startup checks. Reconcile concurrent proposal edits deliberately and coordinate local-edition updates. Do not depend on ignored files or an earlier chat for shared requirements and decisions.

## Collaboration and environment

Use subagents only when explicitly authorized by the user or applicable instructions; discussion of multi-agent workflows alone is not authorization. Give delegated work clear ownership and acceptance criteria. Coordinate shared files and leave integration and verification with one owner. Do not let concurrent writers overwrite documentation or binary documents.

Before changing the stack, inspect installed .NET tooling and verify a supported stable Avalonia/runtime combination. `global.json` pins SDK 10.0.403 (roll forward to the latest feature band). Verified on 2026-10-26 on Windows 11 x64: `dotnet build AlphaQuant.slnx` succeeds with 0 warnings and 0 errors, `dotnet test AlphaQuant.slnx` passes (128 tests, no live SEC access), and `dotnet run --project src/AlphaQuant.Desktop` opens the app. Tests use synthetic fixtures and a fake HTTP handler; never make live SEC access a required test. Do not invent passed checks.

The SEC contact is entered in Settings and stored in `%LOCALAPPDATA%\AlphaQuant\settings.json`, with `AQ_SEC_CONTACT` as fallback. Never commit it. Default UI/tests use isolated settings and synthetic sources and must not overwrite the user's existing configuration. On 2026-11-13 the owner authorized bounded live SEC validation with the configured contact sent in the request-identification header, and Alpaca market validation after explicit user connection. Record live checks separately, use the paced client and verify current source access requirements. This supersedes the former blanket no-live-request guidance for these authorized validation tasks. Never fabricate a contact, silently turn a synthetic check live or extract tokens.

If the SDK is installed per-user (`%LOCALAPPDATA%\Microsoft\dotnet`) rather than system-wide, set `DOTNET_ROOT` to that folder before launching the built `.exe` directly. Otherwise the app host cannot find the runtime and shows a Windows error dialog instead of the app. `dotnet run` does not need this.

If an authorized private master context is available, follow its synchronization instructions and serialize edits. A missing private master must not block public project work. Never publish private context as a workaround. Keep ignored handovers consistent with this tracked guidance where available.

## Git naming

Use standard purpose-based branch prefixes such as `feat/`, `fix/`, `docs/`, `perf/`, `refactor/`, `test/` and `chore/`, followed by a concise kebab-case description. Use product/engineering commit messages, optionally with conventional prefixes such as `feat:`, `fix:` or `docs:`. Do not include tool/model names or AI attribution in branch names, commit messages or trailers. Preserve existing commit history unless the user explicitly authorizes rewriting it.

## Workflow decisions

- AQ-W006, 2026-11-13: at the owner's request, encode reusable financial evidence, quant models, Avalonia design, connection lifecycle, storage/recovery and stage verification methods as six repository-owned skills. Use progressive references and meaningful deterministic helpers, with selective stage routing and actual validation evidence. This supersedes the earlier deferral of project-specific skills. No subagent spawning or application-stage activation is implied.

- AQ-W005, 2026-11-13: use docs/roadmap/Stage_Runbook.md and Stage_Ledger.json for accepted stage invocations. The owner's selected policy permits commit/push/PR/normal merge after checks/self-review, delegated in-scope definitions, local SQLite and bounded configured live validation. Existing accepted stages do not need another planning approval. Preserve checkpoints and continue independent work if external access blocks a package. The roadmap remains draft until accepted/invoked after review. Planning-document delivery is not application-stage activation.

- 2026-10-31: user removed local PDF synchronization from the documentation workflow. Maintain the tracked Markdown documents directly.

- AQ-D005, 2026-10-25, superseded in document placement: shared context and continuation instructions were initially placed in proposal revision 0.4, with the local PDF synchronized.
- AQ-W001, 2026-10-25, approved, replaces AQ-D005 placement: agent/developer instructions, workflow decisions and continuation handoffs belong in tracked `docs/development/Agent_Guidance.md`. The proposal describes the software and retains product/engineering decisions and progress. Preserve private/public boundaries.

- AQ-W002, 2026-10-25, approved: prompt the user to switch to planning mode before implementing large, ambiguous or consequential changes; refine and accept the plan before implementation. Small, well-defined tasks and accepted plans proceed without repeated planning prompts.

- AQ-W003, 2026-10-25, approved: use standard purpose-based Git branch names and product/engineering commit messages without tool branding or AI attribution. Rename existing development branches and push the replacement names without rewriting commits.

- AQ-W004, 2026-10-26, decided: do not vendor third-party agent-skill catalogues (for example LLMQuant/skills) into this repository. They target research workflows and a hosted data source, which conflicts with the SEC-only v1 decision (AQ-D009) and the no-recommendation principle, and unreviewed instructions loaded by every agent are a supply-chain risk. Project-specific skills (metric definition review, SEC data quirks, Avalonia and testing conventions) were considered and deferred by the user; revisit when agents repeatedly repeat the same project-specific mistakes.

## Current handoff

- 2026-11-14 through-S20 checkpoint: owner requested completion of all independent work and one consolidated blocker report. PR27–PR30 delivered broker audit, diagnostics, expanded metrics and the integrated research/models/reports/manual-monitoring workbench. S13–S18 done; S02/S07/S08/S09/S11/S12/S19/S20 retain explicit external gates. Final hardening/distribution record in progress; see Roadmap_Blockers.md and S20_Traceability.md. Do not restart stages or claim full release/live/Windows acceptance. No remaining independent stage is intentionally postponed.

- 2026-11-14: owner authorized all independent work through S20 and requested one consolidated blocker report afterward. S07 audit/process smoke passed; container image and current host/provider research blocked by egress (BL-004). Continue independent S10/research stages; use dated ledger evidence, preserve all external release gates. No subagent authority added.

- 2026-11-14 delivery complete: S04–S06 merged through PR25 (`fcc2dc4`). Exact-head Windows CI 43540146614 passed all checks/package; Linux 274 tests, 19 reference tests, zero-warning build and 19 synthetic UI render/focus states passed, with relevant layouts inspected. Next: S07.1 broker operational/security audit. Live SEC/Alpaca, OAuth registration/deployment, provider rights, Windows signing and owner-PC acceptance remain pending. No new stage was started during delivery.

- 2026-11-14 resumption: owner requested continued development. PR24 is merged; API access works. History cleanup preserved file trees and remapped commit IDs/branch names; use current refs in the ledger. Resume cumulative `feat/reviewed-valuation-inputs` for S04–S06 review, exact-head Windows CI and normal delivery before new stages. Keep live finance requests, purchases and Windows signing/owner-PC work pending. Earlier dated hold/API statements below are historical and superseded by this checkpoint.

- 2026-11-13 cloud continuation: owner resumed a few stages while Windows signing/PC checks stay pending. S04 SQLite/recovery is committed on `feat/research-storage`; S05 saved/offline research on `feat/saved-research`; S06 structured session reviews on `feat/reviewed-valuation-inputs`. The latter two reuse PR24 code in development branches without merging PR24 or changing main. Latest checks: 274 synthetic .NET tests, 19 Python reference tests, zero-warning/error Release build and 19 headless UI PNG states with keyboard/focus checks. Windows x64 ZIP checksum/provider/native-library contents were inspected, not executed. GitHub read/push works; api.github.com is still absent from the enforced allowlist, so PR creation/exact-head Windows CI/normal delivery remains pending. Distribution preparation is preserved separately at `chore/windows-distribution` commit f841108. Next: enable the saved API domain setting and finish review/CI delivery, retaining the prior PR24 no-merge constraint until clarified. S07 is the next independent engineering stage; no live finance calls, purchases or fabricated gate closure.

- S01 delivered on 2026-11-13 via PR #22, merge 1e3b11b. Clean CI passed 206 application tests and all skill/build/package checks. Local compile succeeds but Windows Application Control blocks runtime DLL loads, so do not repeat blocked local tests or weaken security. S02 deterministic work introduces comparison-1.1 to reject ambiguous prior-FY bridges, independent synthetic source fixtures and the bounded SourceCapture tool. Live S02 packages remain blocked by absent configured SEC contact (BL-001), not complete. Continue S03 after exact-head CI/delivery. Ledger is authoritative for owner/branch/next package.

- 2026-11-13 execution approval: owner instructed "Proceed with next few stages and continue developing autonomously." Roadmap 1.0 is approved. The active agent begins S01 baseline audit, S02 source validation and S03 coherent comparison, with independent work continuing if live configuration is absent. Read the ledger for actual progress, owner, branch and evidence. Earlier draft/unstarted notes below describe the state before this instruction and do not reopen approval.

- 2026-11-13 quality layer: six project skills in docs/skills, selective routing/catalog, a standard-library Decimal numeric reference with 19 regression tests, scoped discovery installer and collision checks, official manifest validation and CI skill-tool checks. The numeric helper is not production logic or proof of source/price eligibility. Forward-evaluation task fixtures are provided but no independent agent trial or financial certification is claimed. No src/tests application files were changed and the roadmap remains draft with no active stage. See Skill_Routing.md for installation/use and keep skills aligned with versioned definitions and actual design tokens.

- 2026-11-13: documentation-only autonomous full-product roadmap drafted in docs/roadmap. Twenty stage contracts, a resumable execution runbook and a machine-readable ledger record prerequisites, 83 work packages, blockers, validation and Git delivery. The owner selected automatic commit/push/merge after checks, independent fallback on external blockers, delegated versioned finance definitions, local SQLite and bounded configured live calls. No stage implementation has started. Plan acceptance or a reviewed stage invocation is the next step, followed by S01. Earlier dated delivery evidence below remains historical, not a claim of a new test run.

- 2026-11-01 autonomous first-version integration is authorized by AQ-D024. Browser OAuth and comparison-1 remain authoritative (AQ-D022); teammate native UI is AQ-D023. Backend PR #17 is merged; UI PR #16 has also been merged under the autonomous development instruction; codex/first-version-integration follows both merged milestones.
- Financial analysis consumes the backend through IResearchBackend: annual/TTM peer summaries and full history, source inputs and filing links, explicit missing/unsupported/stale states. The annual test-0 workspace remains separate. Data connections wires browser OAuth, cancellation, selected-symbol access tests and Disconnect. Stream only after every selected symbol passed access testing; stop/invalidate quotes on navigation/disconnect. No token persistence or trading.
- Optional reviewed equity/split/capital JSON is session-only, explicit-field validated and atomic. Import does not certify facts. Never fabricate evidence or infer zero claims to fill valuation cells. Read First_Preview.md for schema and limitations, Financial_Definitions.md for baseline, Alpaca_OAuth_Setup.md for broker setup.
- Packaging script produces self-contained Windows x64 ZIP/SHA256; CI uses standard public-repository Windows runner and short artifact retention. No paid service, OAuth app or host was created. Binaries are unsigned and may be blocked by Smart App Control. Never weaken Windows security as a workaround.
- Current local evidence: 203 tests passed, clean build; synthetic native UI inspection without real settings/network; self-contained publish generated. Integration PR/CI contains final verification. Live account/provider/SEC acceptance, signing/distribution, independent finance review and deployed broker remain external gates.
- Delivery: PR #18 merged; final Windows CI 41314939340 passed test/build/publish/upload. Downloaded v0.1.0-preview.3 CI ZIP hash verified and extracted executable started. A draft preview release holds ZIP/checksum. Next: operator handles OAuth registration/HTTPS configuration, permitted access, signing/distribution and live acceptance using First_Preview.md. Do not claim production-connected readiness. Public docs must remain sufficient without ignored fixtures or chat history. No subagents or teammate messages authorized.
