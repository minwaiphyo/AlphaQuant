# Financial model review

## Current baseline

Use `docs/requirements/Financial_Definitions.md` for exact comparison-1 rules and `ComparisonAnalyzer`, `ComparisonValuation` and source-backed fixtures for behavior. Additive flow TTM may use reviewed FY + current YTD - comparable prior YTD. EPS must not use that subtraction. Diluted EPS and current outstanding shares are different bases.

P/E needs positive compatible TTM EPS, matching eligible price and reviewed class/split basis. EV/sales needs reviewed actual shares, debt, cash, preferred and NCI, plus supported simple scope. A mathematically correct 20x computed with a stale price is still not an available current valuation.

## New-definition record

Capture formula/version, issuer/ownership scope, metric units, annual/TTM/instant basis, source mapping, denominator policy, sign convention, normalization, as-of rules, invalid/missing treatment and a worked expectation. Explain migrations of stored results rather than rewriting prior snapshots.

For ROIC/ROE/turnover specify average versus ending capital and ownership. For FCF distinguish OCF-minus-capex from FCFF. For growth/CAGR specify positive-base requirements and time intervals. Do not turn negative denominators into a cheap/healthy ranking.

## Forecast and DCF stages

Implement only after the relevant stage is invoked. Start from explicit user assumptions, not invented WACC, consensus or tax refunds. Label normalized inputs, forecast years and source history separately.

Specify modeled tax/loss treatment and operating working capital. FCFF uses after-tax EBIT + D&A - capex - change in operating working capital. FCFF discounting uses a compatible WACC. Do not mix equity cash flows with WACC. End-year versus mid-year timing is a model choice that must be explicit.

Perpetual-growth terminal value requires year N+1 FCFF and WACC greater than growth. State terminal reinvestment/economics, not merely the algebraic denominator. The enterprise/equity bridge must not double count cash/non-operating assets or ignore other claims. Negative equity value is not automatically clamped to zero.

Sensitivity grids explain invalid cells and cap evaluations. Reverse DCF fixes all but the selected parameter, has explicit bounds/tolerance and checks existence/uniqueness. A solver's returned number must reproduce the target in the forward model. Report no solution, ambiguity or convergence failure honestly.

## Useful verification properties

- Currency/amount scaling leaves margins and compatible multiples invariant.
- A compatible stock split rescales price, EPS and actual shares without changing economic value or P/E.
- Zero/negative denominators follow explicit unavailable rules.
- Period shifts, future data, amendments and missing capital claims do not leak through valid arithmetic.
- DCF cash-flow present values plus terminal present value reconcile to enterprise value, then bridge to common equity and per-share value.
- Greater discount rate reduces value only in the domain where the selected cash-flow/model assumptions support that property. Do not assert monotonicity for arbitrary negative cash flows.
- Persisted assumption/definition versions replay deterministically. Display rounding never changes screening inputs.

Background: [Damodaran DCF questions](https://pages.stern.nyu.edu/~adamodar/New_Home_Page/valquestions/valquestions.htm). Consult primary financial references for a new model. Those references do not replace the project's complete model specification or professional review.
