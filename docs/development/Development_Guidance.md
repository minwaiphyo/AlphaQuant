# AlphaQuant development guidance

## Documentation and source authority

This guide records contribution procedures, engineering constraints and the
current project status. Keep tracked records sufficient for contributors working
across different machines. Record durable project decisions rather than
conversation transcripts.

- `docs/product/AlphaQuant_Proposal.md` records product scope, architecture,
  decisions and implementation evidence. Read sections 17–19 for current status.
- `README.md` introduces the product, setup and documentation.
- `docs/roadmap/Development_Plan.md` defines stage acceptance criteria;
  `Stage_Runbook.md` defines execution procedures; `Stage_Ledger.json` records
  progress, verification and external dependencies.
- The [engineering procedure library](../skills/Skill_Routing.md) provides
  focused evidence, modeling, UI, connection, storage and review methods.

Distinguish proposed, approved, implemented and verified work. Preserve decision
IDs and the limits of each validation result. A local save is not a pushed change;
a successful synthetic test is not a live-provider or native Windows acceptance
result. Never claim independent review for self-review.

## Starting and planning work

Read the relevant requirements, source, tests and recent commits. Inspect Git
status and preserve local changes. Fetch safely; never reset, clean, force-push or
rewrite history as a routine refresh. Check existing branches and pull requests
before starting overlapping work.

For large, ambiguous or consequential changes, request planning mode before
implementation. Agree scope, assumptions, interfaces, acceptance criteria,
validation, recovery and external dependencies. Small, well-defined edits and
already approved stage work do not require another planning round. An explicit
instruction to proceed without planning overrides this default; record material
assumptions and decisions. Do not expand financial scope or treat missing
credentials as permission to fabricate configuration.

Use versioned financial definitions, explicit units and periods, missing-data
reasons, source lineage and independently recomputed examples. Keep UI,
calculation, ingestion and persistence responsibilities separate. For domain
changes, consult only the relevant procedure guides and references.

## Verification and delivery

Use descriptive branches such as `feat/`, `fix/`, `docs/`, `test/` and `chore/`.
Commit messages should describe the product or engineering change. Preserve
existing history unless rewriting it has been explicitly authorized.

Run proportionate checks and the required CI on the final pull-request head.
Review correctness, financial meaning, cancellation/lifetime behavior, storage
safety, secret leakage and user-facing clarity before delivery. Existing stage
policy permits commit, push, pull request and normal merge after self-review and
passing required checks. Release publication and external account actions require
their own authorization.

Update the proposal and ledger after substantive changes. Record actual commands
and results, remaining acceptance criteria, branch/commit/PR references and one
exact next action. Do not reuse stale results as current evidence. Preserve
approved scope and external release requirements when independent work continues
past a blocker. Parallel work requires explicit authorization, distinct
responsibilities and coordination of shared files.

## Environment, data and access

`global.json` pins .NET SDK 10.0.403 with feature-band roll-forward. Check installed
tooling before changing the stack. If Windows uses a per-user .NET installation,
set `DOTNET_ROOT` to its actual directory before launching the built executable
directly. This addresses runtime discovery, not publisher trust.

Tests use isolated settings, synthetic sources and temporary research data.
Never overwrite existing user configuration or research. SEC contact comes from
Settings, with `AQ_SEC_CONTACT` as fallback. Never commit it. Bounded live SEC
validation with a configured contact and Alpaca validation after explicit account
connection are approved; keep live results separate from synthetic results and
respect current provider requirements. Do not extract credentials or silently
convert an offline test into a live request.

Required incremental project cost is zero. Do not purchase services or invent
account permissions. Keep OAuth application secrets server-side, user tokens
session-only, and credentials out of research storage, exports and diagnostics.
Keep Smart App Control and Defender enabled; no exclusions, trust-marker removal
or self-signed trust-root workaround.

Maintain tracked Markdown directly; local PDF synchronization was discontinued.
Private context, personal details and ignored artifacts must not become public
project dependencies. Inspect staged files before committing.

## Current engineering status — 2026-11-14

Independent implementation through S20 is delivered, with external gates still
open. See `Roadmap_Blockers.md`, `validation/S20_Traceability.md` and the ledger;
do not restart completed packages or claim full release acceptance.

The SignPath Foundation application has been submitted by the maintainer;
acceptance and trusted signing access remain pending. Windows is the intended
platform. Smart App Control blocks the unsigned Desktop DLL on the affected PC.
WSL2/WSLg is being prepared for temporary Linux development viewing and does not
close native Windows trust or acceptance requirements.

Alpaca OAuth registration, broker deployment and real account/data acceptance
remain pending. Live SEC reconciliation, provider rights and historical valuation
coverage also retain explicit external requirements.

## Workflow decision references

- AQ-W001: this guide owns development procedures; the proposal owns product decisions.
- AQ-W002: plan substantial or ambiguous changes before implementation.
- AQ-W003: use purpose-based Git names and product-focused commit messages.
- AQ-W004: review third-party workflow material for scope, source authority and supply-chain risk.
- AQ-W005: use the accepted stage runbook and ledger, preserving checkpoints and release gates.
- AQ-W006: maintain reusable domain procedures with focused references and reproducible verification helpers.
