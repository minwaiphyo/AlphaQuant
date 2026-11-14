# Research rules and models — version 1

Definitions delegated by the approved S13–S19 contracts, 2026-11-14. Nominal USD,
end-year timing, decimals until display. User assumptions and conclusions are not
reported facts, investment advice or independently certified estimates.

## screening-1

Rules name one expanded-1 metric, comparison (>, >=, <, <=), explicit threshold and
positive user weight (≤100). At most 20 rules; stable unique IDs. Defaults are
inspectable investigation prompts: cash conversion<1; receivables/revenue>.25;
inventory/revenue>.25; gross margin<20%; net debt>0 USD; interest coverage<2;
dilution>5%. Users can edit/remove/combine rules and weights. Correlated prompts
can overlap; weights are not probabilities. Missing/stale/unsupported inputs never
trigger a rule or become zero. Rank is unavailable unless ALL selected inputs are
available under expanded-1; otherwise coverage and each missing reason are shown.
Complete weighted trigger score is 100×triggered weights/total weights; higher
means more user-defined investigation conditions, not better investment quality.
Ties retain issuer CIK order. Compare identical rule versions only.

## forecast-1

Explicit 1–10 annual rows: revenue growth −100%..500%, operating margin −100%..100%,
tax rate 0..100%, D&A/revenue and capex/revenue each 0..100%, operating working
capital/revenue −100%..100%. Base revenue is nonnegative; base operating working
capital is an explicit signed USD amount. No inferred forecasts/WACC/beta/tax rate.
Revenue compounds from the prior year. EBIT=revenue×margin. NOPAT=EBIT−max(EBIT,0)
×tax (no loss carryback/refund benefit). D&A=revenue×D&A rate; capex is positive
cash outflow. OWC=revenue×OWC rate; ΔOWC=current−prior. FCFF=NOPAT+D&A−capex−ΔOWC.
Rows retain every bridge and explicit source/basis notes. Immutable saved revisions
link optional frozen historical snapshots. No automatic normalization, SBC or
lease adjustment. Currency/ownership policies must match the selected history.

## dcf-1

User WACC >0 and ≤100%; terminal growth >−100% and <WACC. For stability WACC−g
must be ≥0.005 (50bp). Each forecast FCFF discounts at (1+WACC)^year. Terminal
uses explicit year-N+1 FCFF, separately entered by the user with a required
sustainable reinvestment/source note; do not infer it mechanically from last FCFF.
TV=next FCFF/(WACC−g), discounted at year N. Show terminal PV and contribution.
Negative FCFF/equity/per-share results are retained, never clamped. Bridge uses
explicit nonnegative excess cash/non-operating assets, debt, preferred and NCI,
and strictly positive actual shares; no weighted-average share substitution or
missing zero. EV=sum(PV explicit)+PV terminal; common equity=EV+excess cash+other
nonoperating assets−debt−preferred−NCI; per-share=equity/actual shares. This is an
absolute assumption-dependent model. No current-price upside is shown without an
eligible dated market quote. User target price for reverse modeling is labelled a
target assumption, never a fetched/current quote. Exit-multiple terminal basis is
not part of this version. Rational comparisons use absolute tolerance 1e-20 USD.

Worked bridge: base revenue1000/OWC100; growth10%, margin20%, tax25%, D&A2%,
capex3%, OWC10% ⇒ revenue1100, EBIT220, tax55, NOPAT165, D&A22, capex33,
OWC110/Δ10, FCFF144. A second identical year gives revenue1210 and FCFF158.4.
DCF worked reference: FCFF110,121, WACC10%, growth3%, explicit next FCFF124.63,
cash20, other assets0, debt100, preferred10, NCI5, shares100 ⇒ explicit PV200,
terminal PV10300/7, EV11700/7, equity11035/7, per-share2207/140.

## scenario-1 and reverse-1

Bear/base/bull are user labels, not probabilities. Each is an immutable saved
assumption revision; switching scenarios never overwrites another revision.
Sensitivity grid accepts ≤100 explicit WACC/growth pairs, reevaluates dcf-1, retains
invalid-cell reasons and observes cancellation between evaluations. Revenue-growth
sensitivity applies the selected driver to all explicit forecast years while every
other assumption, including terminal next-FCFF, stays fixed. State that limitation.

Reverse DCF solves ONE constant revenue-growth assumption across explicit years.
WACC, margins, tax/reinvestment, capital bridge and explicit terminal next-FCFF
stay fixed. User bounds within forecast growth range, target>0, tolerance>0,
≤200 bisections, plus 65 sampled monotonicity checks. Non-monotonic/flat sampled
paths, targets outside endpoint values or failures return no certified solution.
A conservative interval bound on the exact polynomial derivative must prove
strict monotonicity throughout the supplied growth bounds as well; otherwise
return uncertain even if samples look monotonic. This establishes uniqueness only
for this fixed-assumption, fixed-terminal model within the supplied interval.
Always forward-recompute residual and evaluation count; no precision beyond user
tolerance. Cancellation aborts. No hidden probabilities or automatic rating.

## monitoring-1

Only user-initiated comparisons/evaluations, no background schedule/notification.
Compare frozen snapshots by CIK and definition; distinguish definition changes,
source filing/accession changes, same-period value restatements, new periods and
unchanged results. Show missing companies/observations rather than zero deltas.
Thesis-condition rules use screening-1 inputs, source dates and evaluation time;
record their evaluation in an explicit immutable research revision. Charts plot
only sourced annual fundamentals. Historical valuation is blocked without dated
prices, then-available filings/shares/actions and explicit storage/use rights.

## Immutable research and reports

Schema 4 adds append-only research-document revisions with optimistic concurrency,
soft archive/recovery and immutable links to existing financial snapshots. Explicit
save is required; list reload preserves editor drafts, and stale writes fail rather
than overwriting concurrent work. A new independent copy preserves Bear/Base/Bull
research. Manual condition reviews record the exact snapshot, rules, evaluation time,
coverage and result in the next explicit revision. No external reminders are sent.

report-1 freezes one saved document revision and all linked snapshot data. Raw
financial values, units, dates, source URLs and definitions remain intact; user
conclusions/assumptions are visibly separate. No quotes/session capital/credentials
are collected. CSV cells neutralize leading spreadsheet operators; XLSX uses only
inline text, no formulas/external recalculation, with long text continued across
bounded rows. PDF embeds DejaVu Sans with its license; font coverage is Latin,
Greek/Cyrillic and available DejaVu glyphs, not a promise of every Unicode script.
CSV/XLSX retain full Unicode text. PDF is capped at 100 pages; reports at 5000 lines
and exports at 32 MiB. Failure/cancellation never replaces an existing file or source
research. PDFsharp 6.2.4 is MIT (NuGet package inspected); 6.5 has a different license
and is not silently substituted. System.Security.Cryptography.Pkcs is pinned to
10.0.15 to resolve transitive audit advisories from the older PDF dependency.

Sensitivity/solver inputs can be included explicitly in a saved model revision.
Frozen reports replay those exact grids/drivers/target bounds and record invalid
cells, residuals and evaluation counts. They never substitute a current price.

## DCF scope guard — dcf-1.1

New workbench models now use dcf-1.1. Formulas are unchanged, but computing
per-share results requires an explicit supported domestic non-financial USD/simple
common-class/complete-capital assertion and a non-future actual-share/capital review
date. No class/scope eligibility is inferred from a ticker or numeric fields.
The model review date belongs to an absolute user-assumption model, not a current
market quote; current-price upside still requires separate eligible price evidence.

Original dcf-1 saved models replay under that original definition and carry a
legacy scope limitation in reports. Loading an old model does not auto-confirm
new scope/date fields; the workbench requires explicit review to create a new
dcf-1.1 revision. Stored history is not relabelled or silently recomputed.
