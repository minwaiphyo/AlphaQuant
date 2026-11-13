---
name: alphaquant-quant-models
description: Specify and verify AlphaQuant financial ratios, TTM, enterprise value, forecasts, DCF, sensitivity and reverse-DCF calculations with precise units and independent worked examples. Use for model changes or numerical review, not merely displaying existing values.
---

# Quantitative model specification and verification

Read the applicable versioned financial definition and the current implementation. Comparison-1 is current behavior. Forecast/DCF stages are planned until invoked, and this skill does not activate them. For new models, read [the model review guide](references/model-review.md).

Before implementation write a compact model contract: formula, input units/ownership, periods/as-of date, scope, missing/non-meaningful cases, precision, provenance and definition version. Keep forecasts/user assumptions separate from reported facts. Never change missing inputs to zero or substitute a convenient denominator.

Create at least one independently derived expected example and one meaningful boundary/failure case for the changed behavior. Prefer dimensional checks, invariance and forward/backward consistency to tests that repeat the production formula.

The [reference arithmetic tool](scripts/reference_math.py) uses standard-library Decimal and only supplied numeric inputs. Read its [usage and worked examples](references/reference-examples.md) before running it. It checks arithmetic, not issuer eligibility, periods, market freshness, rights or financial certification. Do not call it the production engine or use it to bypass evidence guards.

Keep full precision until presentation. Guard overflow, non-finite input, unstable terminal denominators and solver failure. Report model version, independent expected result, observed difference/tolerance, boundary evidence and remaining limitations.
