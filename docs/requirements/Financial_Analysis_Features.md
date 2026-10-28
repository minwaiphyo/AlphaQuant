# Financial Analysis Features

Status: proposed feature catalogue for review, not approved implementation or release scope.
Updated: 2026-10-28

## Purpose and authority

Help a fundamental equity analyst compare companies, evaluate the relationship between business quality, price and risk, and record a defensible investment thesis. The intended outcome is an informed user decision, including a user-authored buy, hold, sell, avoid or investigate conclusion.

The [project proposal](../product/AlphaQuant_Proposal.md) remains authoritative for decisions and release scope (AQ-D007, AQ-D009 and AQ-D013). The [initial user stories](User_Stories.md) supply existing workflow acceptance criteria. This catalogue elaborates candidates; it does not approve formulas, fact mappings, providers, recommendations or trading. Working assumption to validate: multi-year fundamental research into US domestic non-financial companies.

## FA-01: Comparable-company workspace

**Purpose:** compare businesses on consistent financial evidence, rather than isolated reported numbers.

- Select two to five companies for an initial workflow; show names, tickers, reporting dates and industry context.
- Let the user choose peers and record why they are comparable. Industry alone is insufficient: business model, geography, scale, maturity and accounting scope matter.
- Show three-to-five-year trends beside the latest annual values, with a shorter-history warning where needed.
- Compare the following candidate measures; none is an approved definition yet:

| Dimension | Candidate measures | Analyst question |
|---|---|---|
| Growth | Revenue, operating-profit and EPS growth; multi-year CAGR | Is growth sustained, profitable and visible per share? |
| Profitability | Gross, operating and net margins | How much operating performance becomes profit? |
| Capital efficiency | ROIC, ROE and asset turnover | How effectively is capital employed? |
| Cash generation | Operating cash flow, free cash flow and cash conversion | Do earnings translate into cash? |
| Financial resilience | Debt, cash, net debt, interest coverage and debt maturities | How exposed is the business to financial stress? |
| Shareholder outcomes | Diluted share count, dividends and buybacks | Does business performance benefit each share? |

**Acceptance direction:** consistent units and definitions; inspectable periods and sources; explicit unsupported/missing/stale states; no missing value treated as zero. Show meaningful limitations for fiscal-year mismatches. Banks, insurers and REITs require separate models and remain outside the initial candidate universe.

**Dependencies:** approved universe, metric definitions and mappings; historical fact selection; reviewed fixtures; peer-selection rules.

## FA-02: Financial quality and investigation flags

**Purpose:** direct attention to possible weaknesses without asserting that an accounting anomaly proves misconduct or makes a stock unattractive.

Candidate flags: earnings growth alongside weakening operating cash flow; receivables or inventory growing faster than sales; falling margins; rising leverage or weakening interest coverage; sustained dilution; and material differences between reported and adjusted earnings.

**Acceptance direction:** each flag displays inputs, periods, source, rule and limitation. Users can inspect the change, not just a red indicator. Thresholds and materiality require approval. Adjusted earnings require a sourced reconciliation and must not silently replace reported results.

**Dependencies:** FA-01 plus additional mappings, sector applicability and reviewed flag definitions. Some note-level disclosures may need manual research or ingestion beyond the current company-facts parser.

## FA-03: Price-aware valuation comparison

**Purpose:** distinguish an attractive business from an attractive investment at its current price.

Candidate measures: P/E, EV/EBITDA, EV/sales and free-cash-flow yield where meaningful; peer distributions; and the company's own historical valuation range. Present growth, profitability and leverage alongside valuation so a low multiple does not automatically imply a bargain.

Show the enterprise-to-equity bridge, including debt, cash and other claims where applicable. Distinguish historical from forecast inputs. A forward multiple is unavailable without an identified forecast source or explicit user forecast.

**Acceptance direction:** display price timestamp, financial period, share-count basis, currency and provider. Flag non-meaningful denominators instead of ranking them as cheap. Historical valuation aligns prices, shares and financial information available at the relevant time; corporate actions and publication dates must be handled.

**Dependencies:** later valuation phase; selected licensed market-data provider; approved enterprise-value and multiple definitions. SEC filing facts alone do not provide the required market prices. Not v1 under AQ-D007/AQ-D009.

## FA-04: Forecasts, valuation scenarios and reverse DCF

**Purpose:** make the expectations behind an investment case explicit and test their sensitivity.

- User-controlled revenue, margins, reinvestment, capital expenditure, working capital and projection horizon.
- Reviewed discount-rate, terminal-growth or exit-multiple assumptions; consistent cash-flow and discount-rate basis.
- Bear, base and bull cases; estimated equity-value ranges and implied upside/downside against a dated price.
- Sensitivity tables and explanation of the largest value drivers.
- Reverse DCF: solve for a selected operating assumption consistent with the observed price while holding the other stated assumptions fixed.

**Acceptance direction:** reported history, user forecasts and calculated outputs remain distinguishable. Version assumptions and explain the enterprise-to-equity bridge and dilution. A scenario is not a probability; probabilities are optional user inputs requiring separate definition. Show when a reverse-DCF solution is infeasible or not unique rather than fabricate precision. Cash flows, currency, nominal/real assumptions and terminal methods must be internally consistent.

**Dependencies:** FA-03, finance-reviewed forecast/DCF definitions and independently checked example models. Later phase only; no guaranteed returns or automated price-target claims.

## FA-05: Investment-thesis workspace

**Purpose:** connect evidence to a documented user judgment.

| Field | Required content |
|---|---|
| Thesis | Why the investment might offer an attractive risk/return tradeoff |
| Variant view | What the analyst believes market expectations overlook |
| Catalysts | Events that could change expectations, with timing and uncertainty |
| Risks | Business, financial, valuation and execution risks |
| Invalidation conditions | Evidence that would weaken or disprove the thesis |
| Horizon | Intended investment/review period |
| Conclusion | User-authored buy, hold, sell, avoid or investigate further |

Attach source evidence, comparison snapshots and scenario assumptions. Preserve earlier thesis versions. A sell assessment can record thesis deterioration, valuation or better alternatives; personalized portfolio suitability, taxes and position sizing require separate scope.

**Acceptance direction:** user conclusions are visibly distinguished from app calculations. No automatic recommendation engine is approved. Persistence, deletion, backup and export behavior need agreement before implementation.

## FA-06: Screening and shortlisting

**Purpose:** make comparison repeatable across a bounded universe.

Candidate controls: numeric thresholds; visible combinations of filters; saved peer sets and screening presets; sorting; and optional user-weighted rankings. Keep financial quality, valuation and risk visible separately. Explain score components and missing-data treatment; no unexplained overall buy/sell score.

**Acceptance direction:** missing or unsupported results do not silently pass filters; active rules remain visible; ties and unavailable ranks are explicit; an empty result is explained. Filing-only filters can be considered for v1; price-dependent filters require FA-03.

## FA-07: Research monitoring and reporting

**Purpose:** revisit a thesis as facts change and communicate the reasoning.

Candidates: watchlists, saved research snapshots, new-filing/change indicators, user-defined thesis-condition alerts, and a concise spreadsheet/PDF investment memo. Reports include data dates, definitions, assumptions, sources, limitations and the user's conclusion. Separate new filings, restatements and recalculations from actual operating changes.

**Acceptance direction:** users can review before/after evidence; alerts disclose freshness and trigger rules; exports preserve missing-data reasons. Notification scheduling, background retrieval, portfolio integrations and external sharing are not implicitly authorized.

## Suggested delivery sequence

| Stage | Candidate outcome | Approval needed |
|---|---|---|
| First bounded milestone | Select peers, compare approved annual metrics and inspect source evidence | Universe, exact metrics, periods, mappings and comparison acceptance criteria |
| Expanded financial review | Historical trends, cash quality and risk investigation | Additional mappings, flags, history policy and sector exclusions |
| Later valuation phase | Market-price comparisons, scenarios and reverse DCF | Provider/terms, model definitions, forecasts and finance review |
| Research workflow | Persistent theses, snapshots, monitoring and reporting | Persistence/recovery, output policy, export and alert scope |

The sequence is a recommendation, not a release commitment. The current spike has search, annual facts and three unapproved test metrics; it does not implement this catalogue. Existing stories US-01 to US-08 relate to analysis and comparison; US-09/US-10 relate to persistence and cached review; US-11 to US-13 remain usability/distribution prerequisites rather than evidence that these candidates are complete.

## Design prototype flow

Find companies -> choose peers -> compare fundamentals -> inspect quality/risk -> evaluate valuation (later phase) -> record thesis/conclusion -> revisit changes.

Use synthetic, clearly labelled values for UI prototypes. Include loading, empty, cancelled, missing, incomparable, stale and failed states. Prototype screens do not establish financial definitions or approve release scope.

## Decisions before implementation

1. Confirm target analyst, investment horizon and supported universe.
2. Approve the first comparison milestone and a small metric set with reviewed definitions.
3. Confirm user-authored conclusions versus any separately proposed app-generated recommendation.
4. Define additional data, provider terms and point-in-time requirements before price-aware features.
5. Agree persistence/recovery and monitoring behavior before implementing saved research.

Follow the planning gate in [developer guidance](../development/Agent_Guidance.md) before implementing substantial or financially consequential changes.

## Reference

[CFA Institute: Equity Valuation—Applications and Processes](https://www.cfainstitute.org/insights/professional-learning/refresher-readings/2026/equity-valuation-applications-and-processes) provides background on connecting company analysis, forecasts, valuation and a supported conclusion. The feature choices and delivery sequence above are project proposals, not requirements prescribed by that source.
