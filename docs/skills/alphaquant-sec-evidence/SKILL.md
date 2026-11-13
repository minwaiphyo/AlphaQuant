---
name: alphaquant-sec-evidence
description: Resolve and review AlphaQuant SEC/XBRL facts, issuer scope, annual or quarterly periods, restatements and source lineage. Use for financial ingestion and source reconciliation, not OAuth or UI styling.
---

# SEC evidence and financial comparability

Work from the current repository root. Read `docs/requirements/Financial_Definitions.md` and the relevant analyzer/parser before selecting facts. Comparison-1 is an implementation baseline, not a claim of universal issuer coverage. Read [the evidence procedure](references/evidence-procedure.md) for selection or real-filing reconciliation.

For each changed mapping or selected period:

- Identify issuer/CIK, concept, unit, start/end, form, filing date/accession and retrieval/as-of time. Distinguish filing fiscal labels from the period actually measured.
- Resolve alternatives rather than adding overlapping concepts. Make ownership scope, instant versus duration, restatements and incompatible periods explicit.
- Preserve signed source contributions and selection rationale in output evidence. Missing facts are not zero. Ticker or fact availability does not certify supported industry/currency/share-class scope.
- Compare expected values to the filing and an independently recomputed example, not just another call into production arithmetic.

Changes to mappings, period selection or scope require versioned definitions and regression fixtures. For arithmetic/model changes, also load the quantitative-model skill if available. Do not expand retrieval scope or use live calls unless the task's existing authorization/configuration permits it.

Deliver the selected/rejected facts, rationale, exact source references, reconciliation result and unresolved coverage. Never call synthetic or agent-reviewed data independently finance-certified.
