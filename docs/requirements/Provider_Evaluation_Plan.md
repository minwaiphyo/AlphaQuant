# Price-Aware Comparison: Planning and Provider Evaluation

Current scope: SEC + per-user free Alpaca Basic IEX; zero-cost pilot; replaceable adapter; session-only credentials if API-key input is chosen. Authentication route and financial definitions remain undecided. Complex/unverifiable valuations may be unavailable. No trading. See [UI design brief](../development/UI_Design_Brief.md) and proposal revision 0.20 for current status. Older provider/budget/account-free statements below are dated review context, superseded where inconsistent with AQ-D018/AQ-D020/AQ-D021.

Updated: 2026-10-28. Status: Stage A accepted and completed; Stages B/C remain conditional and unapproved.

## Latest selections

AQ-D016 (2026-10-28): a disclosed limited-venue real-time feed is acceptable for evaluation; stream while the two-to-five-company comparison screen is open, with event timestamps and connection state; use reviewed current TTM calculations separately from five annual years. This selects behavior, not a vendor or financial formula. No live evaluation or implementation authorized. Next action: review the staged evaluation plan; verify permitted trial symbols and display/pilot rights before authenticated access.

## Selected direction

AQ-D014/AQ-D015: US domestic non-financial US-GAAP/USD companies; five annual years when available, explicit gaps and comparability warnings; fundamentals plus current-price valuation; no historical valuation charts. Refine revenue growth, operating margin, operating cash flow and debt/cash balances. Initial valuation candidates: trailing P/E and EV/sales. User conclusions remain user-authored. Real-time prices required; free-tier evaluation only; paid distribution budget undecided. Audience: private development and a small pilot, with pilot rights assessed separately. No purchases, vendor contact, credentials, integration or implementation authorized.

## Scope and exclusions

Proposed first slice: compare two to five supported companies with sourced history, dated current prices, transparent multiple calculations and unavailable reasons. SEC remains the candidate filing-fact source; one replaceable market-data adapter is proposed, not selected. Exclude DCF/scenarios, historical valuation, consensus estimates, trading, app-generated recommendations and portfolio suitability.

## Open definitions before implementation

- Real-time feed: limited-venue coverage accepted for evaluation and streaming while comparison is open selected; price-event semantics (trade versus bid/ask), session coverage and tolerated age remain to verify. Never label delayed, cached or disconnected prices as live.
- Trailing P/E: propose price divided by reviewed trailing diluted EPS attributable to common shareholders. Quarter/YTD selection, loss handling, split adjustment and aggregation of EPS require finance review. Five annual observations alone do not supply current TTM earnings.
- EV/sales: agree trailing revenue, market-cap share basis, debt, cash, preferred equity, non-controlling interests, lease treatment and handling of stale balance-sheet dates. A simplistic price-times-weighted-average-EPS-share-count bridge is not acceptable without review.
- Latest public financial information versus historical point-in-time research; the latter is excluded initially. Show mixed price/fundamental dates explicitly.
- Five annual years need a sixth revenue observation for five year-over-year growth results, or an explicit unavailable first growth value.
- Pilot display, caching, derived-data/export and deletion rights; individual/professional subscriber eligibility and credentials.

## Preliminary provider evidence

Checked public official pages on 2026-10-28; no account or API tests. Marketing access is not confirmed pilot permission.

| Provider | Evidence | Evaluation status |
|---|---|---|
| Twelve Data | Basic lists real-time US equities, 8 API credits/minute, 800/day, 8 trial WebSocket credits and internal non-display usage | Investigate feed provenance, display/pilot rights, trial-symbol restrictions and achievable freshness before selection |
| Finnhub | Terms restrict sharing data and derived results without written approval; personal plans cannot be used by a business internally without approval | Not cleared for pilot; current free-feed entitlements not verified from its dynamic pricing page |
| Alpha Vantage | Documentation places real-time US quotes on premium personal access; commercial usage requires separate discussion | Does not currently satisfy free real-time evaluation requirement on reviewed pages |
| Massive | Stocks page lists paid real-time individual access; business base FMV is an estimate, with trade/quote expansions separate | Paid alternative to revisit; no free real-time fit established |

Sources: [Twelve Data pricing](https://twelvedata.com/pricing), [Finnhub terms](https://finnhub.io/terms-of-service), [Alpha Vantage documentation](https://www.alphavantage.co/documentation/), [Massive stocks](https://massive.com/stocks), [Massive business feed clarification](https://massive.com/knowledge-base/article/does-the-business-plan-include-real-time-trades-and-quotes-or-only-fair-market-value).

No provider selected. No inference that a free individual account permits a multi-user pilot. If none qualifies, return for an explicit budget, coverage or freshness decision; do not silently substitute delayed data.

## Bounded plan for review

1. Agree open feed and financial definitions and reviewed fixture examples.
2. Create a provider capability/rights matrix: supported symbols, timestamps, coverage, delay, quotas, transport, corporate actions, share data, display/cache/export rights, credentials and pilot cost.
3. Once a provider is eligible and credentials are supplied, conduct a separately authorized small evaluation with a few supported symbols. Record observed timestamp freshness, entitlement errors, quotas, splits/share-class mapping and coverage against a permitted independent reference. No broad ingestion.
4. Agree implementation interfaces between UI, Core and Infrastructure. Desktop owns comparison/loading/error states; Core owns approved financial definitions and comparability; Infrastructure owns provider adapters, pacing and licensed cache behavior.
5. After explicit plan acceptance, implement one two-to-five-company comparison slice. Preserve the teammate's UI ownership and coordinate shared contracts.
6. Validate reviewed financial fixtures and fake-provider tests; then perform a permitted manual end-to-end check and record evidence.

## Acceptance and failure cases

- Every metric exposes source facts, units, period, definition version and warnings; loss-making/invalid P/E and unsupported EV inputs are unavailable with a reason.
- Prices expose source/feed, event timestamp, retrieval time and market session. Stale/disconnected/closed-market states are explicit; consistent snapshots avoid misleading peer rankings from unequal timestamps.
- Annual trends and current trailing values are labelled separately; no mixing quarterly and annual facts, duplicate YTD inputs or unadjusted split bases.
- Rate limits, entitlement rejection, expired keys, network loss, missing symbols and cancellations preserve usable prior data with freshness labels. No silent provider or delayed-price fallback.
- Tests use synthetic or permitted fixtures and fake transports; live access is not a required automated test. Validate financial results independently, not only by reproducing implementation formulas.
- No API keys in Git or logs; desktop credential ownership and distribution model agreed before integration. Caching and exports follow confirmed rights.
- Build/test commands and manual evidence recorded only when actually run. Planning work has no new application verification.

## Exact next action

Read the [Stage A findings](Provider_Feasibility_Review.md). Decide whether to pursue developer-only Alpaca eligibility or a separately authorized shared-pilot rights enquiry. No provider selected or Stage B/C approved.


## Updated evaluation finding

Twelve Data is a candidate for the first eligibility review, not a selected supplier. Its official [trial guide](https://support.twelvedata.com/en/articles/5335783-trial) limits trial WebSockets to one connection and up to eight symbols from its trial list. The count alone therefore does not guarantee arbitrary two-to-five-company comparison. Confirm the current permitted US equity symbols before any evaluation. Its [terms](https://twelvedata.com/terms) restrict free-tier commercial usage and make external display/redistribution depend on entitlements or separate agreement. Developer access does not establish pilot rights. This candidate remains unapproved until the intended use is clearly permitted.

## Staged plan for acceptance

### Stage A: provider and financial feasibility, documentation only

Deliverables: eligibility matrix with verified trial symbols, feed provenance, timestamp semantics, streaming limits, session coverage, display/caching/derived-output rights and development-versus-pilot permissions; a source/mapping matrix for the proposed TTM/EV inputs; independently calculated synthetic examples; and a go/no-go recommendation. Do not contact vendors, open accounts, spend money or invoke authenticated APIs without authorization. If public terms leave rights ambiguous, record the exact question for the user rather than assume permission.

Proposed financial approach for review: build TTM additive revenue and income from compatible discrete quarters, or latest full year plus current YTD less comparable prior YTD; avoid summing overlapping YTD periods. Diluted EPS requires its own reviewed common-shareholder, dilution and split treatment; never blindly sum mismatched reported EPS. EV requires an agreed current share basis and latest available debt/cash/other-claim snapshot. Current price does not make historical debt/cash current. For missing or ambiguous inputs, show unavailable and preserve provenance.

Exit criteria: at least one eligible evaluation route with documented limits, or a clear no-go explaining required changes; financial input coverage and unresolved definition choices documented. This stage does not approve formulas or claim finance review has occurred.

### Stage B: tiny live evaluation, conditional

After eligible access, user authorization and credentials: subscribe to two-to-five permitted supported issuers, or explicitly report that trial restrictions prevent that sample. Observe event timing, entitlement rejection, rate limits and reconnect behavior. Do not log credentials or redistribute provider fixtures. A network connection/heartbeat alone does not prove a price is fresh; illiquid securities may have no new trade. Stop subscriptions when leaving comparison; bounded reconnect/backoff applies only while active. Proposed exact stale thresholds/session behavior require acceptance before implementation.

Exit criteria: observed data matches the documented feed/entitlements and supports the sample, with evidence and failure behavior recorded. If not, return a no-go without silently downgrading prices or expanding budget.

### Stage C: implementation plan and integration, separately accepted

Core: approved TTM/annual definitions and comparability. Infrastructure: filing extensions for quarterly/YTD facts, a narrow quote stream adapter, security/CIK/share-class mapping, credential storage and permitted cache policy. Desktop: teammate-owned flow contracts for annual/TTM panels, asynchronous quote ages and connection states. Tests: reviewed financial fixtures for overlapping YTD, restatements, splits, losses and missing EV components; fake streams for out-of-order events, duplicate events, disconnects, quota/entitlement failures and cancellation. Integrate one bounded comparison only after plan/definitions are accepted.

No application files, dependencies or runtime settings changed during planning. Provider evaluation and financial-definition review can proceed before the teammate completes UI designs, using agreed data contracts and synthetic examples.

## Stage A completion

Accepted by the user and completed on 2026-10-28. [Findings, input matrix and synthetic examples](Provider_Feasibility_Review.md): Alpaca Basic IEX is the leading conditional developer candidate; no reviewed provider is cleared for the entire free shared-pilot use case. No accounts, purchases, vendor messages, authenticated APIs or application implementation.
