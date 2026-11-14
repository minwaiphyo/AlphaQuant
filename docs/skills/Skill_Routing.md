# AlphaQuant engineering procedure library

Version: 1.0. Updated: 2026-11-14. The canonical procedure guides are tracked
with the repository so contributors can use the same evidence and review methods.

| Skill | Load when the package changes |
|---|---|
| alphaquant-sec-evidence | SEC mappings, periods, issuer/ownership scope, filing selection or source reconciliation |
| alphaquant-quant-models | Ratios/TTM/EV, forecast/DCF arithmetic, assumptions, numerical behavior or solver validation |
| alphaquant-avalonia-design | Desktop layouts, interaction states, financial presentation, keyboard/focus or design tokens |
| alphaquant-data-connections | Browser OAuth, broker boundary, entitlements, quotes, socket/retry/cancel lifecycle |
| alphaquant-research-storage | SQLite schema, snapshots/theses/scenarios, migrations, offline research or recovery |
| alphaquant-stage-verification | Stage acceptance, self-review, checkpoints, release gates and Git delivery |

Use `skill_catalog.json` for stage hints. Select procedures based on the actual
change. A wording-only change does not require every domain procedure.

## Using the guides

Read the relevant canonical `SKILL.md` and its referenced resources directly.
The guides cover evidence selection, numerical checks, UI behavior, connection
lifecycle, storage safety and stage acceptance. They do not require a particular
editor or development service.

## Source authority and freshness

Skills provide reusable methods and project-specific pitfalls. Current source/versioned definitions remain authoritative for implemented behavior. The roadmap still determines what is authorized to build. No skill activates a draft stage, grants account access, raises the zero-cost budget or certifies financial results.

Keep semantic theme roles in source rather than duplicating every color into a skill. Keep exact financial policy in versioned definition documents rather than changing it only in instructions. When either evolves, update affected references and examples in the same work package. External provider documentation/terms must be checked when the corresponding interface changes.

## Validation and improvement

`python tools/Validate-ProjectSkills.py` verifies catalog names, stage hints, linked resources and project paths. Run `python -m unittest discover -s docs/skills/tests -v` for the independent numeric helper. `powershell -File tools/Test-ProjectSkillInstallation.ps1` checks installer isolation and collision preservation.

Structure and helper tests do not establish procedure compliance. [Evaluation cases](Evaluation_Cases.md) define realistic tasks, expected evidence and failure conditions. Use isolated trials and record the procedure version, reviewer and actual outcomes. Never label self-review as independent evaluation.

For a demonstrated skill failure, save the task/input and observed incorrect decision, make a narrow correction, rerun the affected case and check unrelated trigger boundaries. Do not expand every skill into a general development manual.
