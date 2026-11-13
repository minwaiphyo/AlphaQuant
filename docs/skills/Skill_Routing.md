# AlphaQuant domain skill library

Version: 1.0. Updated: 2026-11-13. Canonical skills live here and travel with Git. Project discovery links live under ignored `.agents/skills`, preserving the repository's private-config boundary. No global skill or configuration is overwritten.

| Skill | Load when the package changes |
|---|---|
| alphaquant-sec-evidence | SEC mappings, periods, issuer/ownership scope, filing selection or source reconciliation |
| alphaquant-quant-models | Ratios/TTM/EV, forecast/DCF arithmetic, assumptions, numerical behavior or solver validation |
| alphaquant-avalonia-design | Desktop layouts, interaction states, financial presentation, keyboard/focus or design tokens |
| alphaquant-data-connections | Browser OAuth, broker boundary, entitlements, quotes, socket/retry/cancel lifecycle |
| alphaquant-research-storage | SQLite schema, snapshots/theses/scenarios, migrations, offline research or recovery |
| alphaquant-stage-verification | Stage acceptance, self-review, checkpoints, release gates and Git delivery |

Use `skill_catalog.json` for stage hints. Choose by actual work package, not stage number alone. A wording-only change does not need all domain skills. Do not load every reference or give one agent an artificial persona for each skill. One agent can apply several relevant skills without spawning other agents.

## Discovery and portability

From the repository root, run `powershell -File tools/Install-ProjectSkills.ps1`. The script creates only named links to this repository's canonical skill folders, verifies existing links and refuses collisions. On Windows it uses directory junctions, requiring no change to Developer Mode or security settings. It does not write user/global configuration, install a model or contact a service.

Codex supports repository `.agents/skills` discovery and linked skill directories according to [official skill documentation](https://learn.chatgpt.com/docs/build-skills). If discovery does not refresh, restart Codex. The project may be in a sandbox where creating the local links requires a tool approval. That does not prevent reading the canonical `SKILL.md` files directly.

For another agent product, load the relevant canonical `SKILL.md` and requested reference files using its supported mechanism. This library is not a guarantee that every harness automatically discovers arbitrary folders. Do not assume it is installed on a teammate's laptop simply because it is committed.

Direct invocation examples in a skill-aware session:

```text
Use $alphaquant-sec-evidence to reconcile the S02 source cases.
Use $alphaquant-quant-models to verify the valuation changes.
Use $alphaquant-avalonia-design for the comparison-screen work.
```

## Source authority and freshness

Skills provide reusable methods and project-specific pitfalls. Current source/versioned definitions remain authoritative for implemented behavior. The roadmap still determines what is authorized to build. No skill activates a draft stage, grants account access, raises the zero-cost budget or certifies financial results.

Keep semantic theme roles in source rather than duplicating every color into a skill. Keep exact financial policy in versioned definition documents rather than changing it only in instructions. When either evolves, update affected references and examples in the same work package. External provider documentation/terms must be checked when the corresponding interface changes.

## Validation and improvement

`python tools/Validate-ProjectSkills.py` verifies catalog names, stage hints, linked resources and project paths. The official skill-creator `quick_validate.py` validates each manifest when that tool and its PyYAML dependency are available. Run `python -m unittest discover -s docs/skills/tests -v` for the independent numeric helper. `powershell -File tools/Test-ProjectSkillInstallation.ps1` checks installer isolation and collision preservation.

Structure and helper tests do not prove that an agent follows the skill. [Behavioral evaluation cases](Evaluation_Cases.md) define realistic tasks, expected evidence and failure conditions. Use them in a fresh authorized session for forward evaluation, or an independent agent only when delegation is explicitly authorized. Record the actual model/session, loaded skill version and outcomes. Never label self-review as independent evaluation.

For a demonstrated skill failure, save the task/input and observed incorrect decision, make a narrow correction, rerun the affected case and check unrelated trigger boundaries. Do not expand every skill into a general development manual.
