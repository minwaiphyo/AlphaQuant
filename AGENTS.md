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

Before changing the stack, inspect installed .NET tooling and verify a supported stable Avalonia/runtime combination. `global.json` pins SDK 10.0.403 (roll forward to the latest feature band). Verified on 2026-10-26 on Windows 11 x64: `dotnet build AlphaQuant.slnx` succeeds with 0 warnings and 0 errors, `dotnet test AlphaQuant.slnx` passes (128 tests, no live SEC access), and `dotnet run --project src/AlphaQuant.Desktop` opens the app. Tests use synthetic fixtures and a fake HTTP handler; never make live SEC access a required test. Do not invent passed checks.

The SEC contact email (the SEC requires a declared User-Agent with a working contact) is entered on the app's Settings page and stored in `%LOCALAPPDATA%\AlphaQuant\settings.json`; the `AQ_SEC_CONTACT` environment variable is the fallback when nothing is saved. The client reads it on every request. Never commit a contact or other personal data. When testing the UI, remember that Settings writes to the real settings file: check it is absent first and remove what a test wrote, and never save a valid contact and then trigger a search, which would send a real SEC request. The SEC fair-access limit is 10 requests per second, and the client paces itself below that.

If the SDK is installed per-user (`%LOCALAPPDATA%\Microsoft\dotnet`) rather than system-wide, set `DOTNET_ROOT` to that folder before launching the built `.exe` directly. Otherwise the app host cannot find the runtime and shows a Windows error dialog instead of the app. `dotnet run` does not need this.

If an authorized private master context is available, follow its synchronization instructions and serialize edits. A missing private master must not block public project work. Never publish private context as a workaround. Keep ignored handovers consistent with this tracked guidance where available.

## Git naming

Use standard purpose-based branch prefixes such as `feat/`, `fix/`, `docs/`, `perf/`, `refactor/`, `test/` and `chore/`, followed by a concise kebab-case description. Use product/engineering commit messages, optionally with conventional prefixes such as `feat:`, `fix:` or `docs:`. Do not include tool/model names or AI attribution in branch names, commit messages or trailers. Preserve existing commit history unless the user explicitly authorizes rewriting it.

## Workflow decisions

- AQ-D005, 2026-10-25, superseded in document placement: shared context and continuation instructions were initially placed in proposal revision 0.4, with the local PDF synchronized.
- AQ-W001, 2026-10-25, approved, replaces AQ-D005 placement: agent/developer instructions, workflow decisions and continuation handoffs belong in tracked `AGENTS.md`. The proposal describes the software and retains product/engineering decisions and progress. Preserve private/public boundaries and synchronize the untracked local PDF with software documentation changes.

- AQ-W002, 2026-10-25, approved: prompt the user to switch to planning mode before implementing large, ambiguous or consequential changes; refine and accept the plan before implementation. Small, well-defined tasks and accepted plans proceed without repeated planning prompts.

- AQ-W003, 2026-10-25, approved: use standard purpose-based Git branch names and product/engineering commit messages without tool branding or AI attribution. Rename existing development branches and push the replacement names without rewriting commits.

- AQ-W004, 2026-10-26, decided: do not vendor third-party agent-skill catalogues (for example LLMQuant/skills) into this repository. They target research workflows and a hosted data source, which conflicts with the SEC-only v1 decision (AQ-D009) and the no-recommendation principle, and unreviewed instructions loaded by every agent are a supply-chain risk. Project-specific skills (metric definition review, SEC data quirks, Avalonia and testing conventions) were considered and deferred by the user; revisit when agents repeatedly repeat the same project-specific mistakes.

## Current handoff

Documentation update, 2026-10-28: branch `docs/initial-user-stories` adds `User_Stories.md` with US-01 through US-13 copied unchanged from the supplied text (exact content match verified). Proposal revision 0.8 references them as proposed scope (AQ-D012), without approving metric definitions or release contents. No application changes or new build/test results. The local PDF and editable source were not found in this checkout; synchronization remains pending. Exact next action remains review of section 5 metric definitions, mappings and period rules with the user, using the stories to bound the next milestone. Check Git for commit and push state.

State as of 2026-10-26 (check Git for the current commit, push and merge state rather than relying on this text):

- Software: an Avalonia 12.1.4 / .NET 10 desktop app with Home, Search Companies, company detail, Settings and Release Notes pages over `src/AlphaQuant.Core`, `src/AlphaQuant.Infrastructure` and `tests/AlphaQuant.Tests`. v1 is SEC-only (AQ-D009). The three metrics are unapproved test definitions (`test-0`, AQ-D010). The SEC contact email is entered in Settings and stored locally (AQ-D011). Build is clean and 128 tests pass. The user reported running a live SEC check themselves; the agent did not observe it and no values were recorded. There is no CI, SQLite, installer or release.
- Business: proposal revision 0.7. Screening is v1; valuation is a later phase (AQ-D007); external providers are a direction with no vendor selected (AQ-D008); Yahoo Finance and Koyfin were rejected as data sources. Third-party agent skills were not adopted (AQ-W004).
- Open items: approve v1 metrics, tag mappings and period rules; arrange qualified finance review (proposed, section 15); budget, licence, publication channel and Windows target; the local PDF edition was not found on the laptop used on 2026-10-26 and was never regenerated for revisions 0.6 and 0.7. Regenerate it from the private editable source if that is available on another laptop.
- Setting up another laptop: install .NET SDK 10.0.403 (the official `dotnet-install` script into `%LOCALAPPDATA%\Microsoft\dotnet` worked without admin rights, then add it to PATH or set `DOTNET_ROOT`), then use the build, test and run commands in README.md and enter your own SEC contact email in Settings. The GitHub CLI was not installed, so integration was done with local fast-forward merges; install and authenticate `gh` to use pull requests.
- Exact next action: review the test definitions (proposal section 5) with the user and decide approved v1 metrics, tag mappings and period rules; consider adding CI.
