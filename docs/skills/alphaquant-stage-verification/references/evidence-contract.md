# Observable quality evidence

For changed behavior, record a concise mapping from acceptance criterion to evidence. Capture the failure it protects against, expected result, observed result, test/source reference and remaining uncertainty.

| Change | Appropriate evidence | Insufficient evidence |
|---|---|---|
| Fact selection | Filing context, rejected alternatives, source-backed fixture | A number copied from the app |
| Ratio/TTM | Independently computed expectation, compatible inputs, boundary cases | Another call into the same production function |
| DCF/solver | Model contract, cash-flow/bridge reconciliation, forward residual/bounds | An optimizer returning some number |
| UI/layout | Changed state interactions and rendered layout where relevant | Build succeeds |
| Stream/auth | Safe negative paths, cancellation/disposal and access transitions | Login completed once |
| Storage | Atomic write, old-schema migration and interrupted recovery/restore | New empty DB opens |
| Release | Exact-commit CI/artifacts plus actual applicable external gates | Draft release or unsigned ZIP exists |

Useful financial checks include units, sign conventions, denominator scope, restatement/as-of behavior, scale/split invariance and missing-data semantics. Choose properties justified by the model domain, not blanket assertions about all cash flows.

Use deterministic default tests. Keep live validation separate, bounded, configured and non-secret. A source-backed expected fixture is different from an external professional review. A fixture UI is different from a real provider workflow. Record those distinctions explicitly.

Before marking done, inspect the final diff for unintended schema/definition changes, hidden arithmetic in views, stale/mixed prices, token persistence, lost cancellation and research data loss. Resolve demonstrated defects. Do not add tests that only match implementation text or repeatedly rerun unchanged suites without reason.

The stage ledger stores package completion, partial work, blockers, validation, self-review and delivery links. Populate evidence with actual outcomes, not planned commands. If verification could not run, keep the affected acceptance open. Preserving this evidence is what enables another contributor to continue safely.
