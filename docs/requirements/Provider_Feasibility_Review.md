# Stage A: Provider and Financial Feasibility Review

Current scope: SEC + per-user free Alpaca Basic IEX; zero-cost pilot; replaceable adapter; session-only credentials if API-key input is chosen. Authentication route and financial definitions remain undecided. Complex/unverifiable valuations may be unavailable. No trading. See [UI design brief](../development/UI_Design_Brief.md) and proposal revision 0.20 for current status. Older provider/budget/account-free statements below are dated review context, superseded where inconsistent with AQ-D018/AQ-D020/AQ-D021.

Date: 2026-10-28. Status: Stage A documentation review completed; no provider selected and no live-data or implementation approval. Companion: [evaluation plan](Provider_Evaluation_Plan.md). Authority: [proposal](../product/AlphaQuant_Proposal.md), AQ-D014 through AQ-D017.

## Outcome

**Conditional technical recommendation: investigate Alpaca Basic IEX first for a developer-only evaluation. No-go for starting a shared live pilot or authenticated Stage B now.** Its documented free streaming allowance fits two to five companies, but account eligibility, applicable data agreements and intended-use permissions have not been established. Do not infer pilot rights from personal developer access. No reviewed provider is cleared for the complete free shared-pilot requirement. This is a project eligibility finding from public evidence, not a legal determination that no qualifying agreement exists.

Keep SEC as the proposed source of filing evidence. A quote provider does not resolve TTM accounting, current outstanding shares or the EV bridge automatically. The current application is an annual-only spike and needs separate approved financial work.

## Review method and limits

Read official public pricing, documentation and terms; inspect Core/Infrastructure source. No signups, vendor messages, purchases, credentials, authenticated calls or application changes. Prices, availability and terms can change; recheck before access. Public-site marketing quotes were not treated as API samples. Trial-symbol/rights unknowns remain explicit. Arithmetic below was checked independently with decimal calculations, not against provider values or a finance reviewer.

## Provider eligibility matrix

| Provider | Public technical evidence | Development and pilot eligibility | Finding |
|---|---|---|---|
| Alpaca Basic | Free IEX real-time equities; 30 WebSocket symbols; 200 historical calls/minute. Basic is available for Paper/Live Trading API accounts. | Reviewed general terms describe personal/non-commercial use; applicability of account-specific data agreements to AlphaQuant development/shared pilot unresolved. Do not assume Broker API sandbox permissions apply to Trading API. | Best documented technical match for developer evaluation; conditional only. Pilot not cleared. |
| Twelve Data Basic | 8 API credits/minute, 800/day, 8 trial WebSocket credits; trial guide limits one connection and trial-list symbols. | Pricing distinguishes non-display access; terms restrict free commercial use and external display rights. Exact permitted US equity trial sample could not be verified: public exchange detail timed out twice. | Trial candidate only, not arbitrary-peer streaming solution or cleared pilot. |
| Finnhub | Official WebSocket/pricing pages did not expose usable content in this review. | Terms explicitly require written approval for sharing data/derived results and for business use of personal plans. | Technical entitlements unknown; not cleared for shared pilot. |
| Alpha Vantage | Reviewed quote documentation places real-time US quotes on premium personal access; commercial access separate. | No free real-time route established in reviewed pages. | Does not satisfy current free real-time evaluation criterion. |
| Massive | Reviewed stocks offering lists paid real-time individual access. Business FMV is an estimated price, not a reported trade or bid/ask. | No free live route or pilot permission established. | Paid alternative only; do not silently substitute estimated FMV for a market trade. |

## Streaming and operating constraints

Alpaca documents IEX versus consolidated SIP separately. Display IEX coverage prominently; it is not the full US market. Subscription capacity is not proof of liquidity or frequent price updates for a selected issuer. Exact issuer availability and US non-financial eligibility require an account/asset check later; proposed sample AAPL/MSFT is illustrative, not verified eligibility.

The WebSocket protocol documents authenticated subscription, entitlement errors and subscription-dependent connection limits (often one). Use one shared comparison stream, not one connection per row. Its synthetic FAKEPACA test stream still documents authentication and is not a live US equity acceptance sample. Event timestamps, trade conditions, session coverage, corrections/cancellations and out-of-order handling must be checked against stock-message documentation before integration. A heartbeat is not a new trade.

Twelve Data trial-symbol access remains unresolved; eight credits alone do not verify two suitable companies. No claim that provider latency, uptime, reconnect behavior, timestamps or corporate-action behavior has been measured.

For every provider: verify display, derived multiples, screenshots/reports, cache retention, redistribution, deletion and user classification. No durable quote archive/export or shared API key is proposed before rights are established. Do not promise a free production/pilot service based on evaluation entitlements.

## Financial source and mapping feasibility

The SEC API documents entity-wide non-custom taxonomy facts from annual and quarterly filings, with separate units. It is a candidate source, not a guarantee of complete comparable data. Current source inspection: ConceptMap contains revenue/net-income alternatives and current assets/liabilities; SecParser reads only that us-gaap allowlist; AnnualAnalyzer accepts 10-K/10-K/A and annual USD periods. Quarterly facts, new units/taxonomies and valuation inputs are not exposed by the existing pipeline.

| Output/input | Proposed source or mapping candidates | Status and work required |
|---|---|---|
| Five annual revenue observations and growth | Existing revenue tag candidates in ConceptMap | Extend history selection; sixth observation needed for five growth values, otherwise first growth unavailable. Review tag changes/restatements. |
| Operating margin | us-gaap OperatingIncomeLoss / compatible revenue | New allowlist/mapping; annual versus TTM labelled separately; positive revenue and same scope required. |
| Operating cash flow | us-gaap NetCashProvidedByUsedInOperatingActivities | New mapping; inspect duration/YTD contexts and exceptional presentation. Do not confuse with free cash flow. |
| Current TTM revenue | Compatible annual and 10-Q revenue or four non-overlapping quarters | New period-selection/reconciliation layer; fiscal calendars, amendments, YTD and Q4 residuals reviewed. |
| TTM diluted EPS for P/E | us-gaap EarningsPerShareDiluted and relevant shareholder/weighted-share disclosures | USD/share support; common-share scope, dilution, splits and quarter compatibility reviewed. No automatic annual-plus-YTD EPS bridge. |
| Cash | us-gaap CashAndCashEquivalentsAtCarryingValue, with restricted-cash/investment policy | New mapping and definition. Short-term investments are not automatically cash; instant date disclosed. |
| Debt | Reviewed totals versus short-/long-term components | No universal debt tag selected. Avoid double-counting overlapping totals and components; leases/convertibles require policy. |
| Market capitalization | Quote plus correctly dated outstanding shares for all relevant classes, or a sourced provider market cap | Current shares are not diluted weighted-average EPS shares. Multi-class identity and stale shares may force unavailable. |
| EV adjustments | Debt plus preferred interests and non-controlling interests less approved cash | New mappings/policy; absence of a tag is not proof of zero. Simplified bridge allowed only for verified simple capital structures. |
| Security identity | CIK entity plus provider asset/share-class identifiers | Existing ticker/CIK alone insufficient for all share classes/corporate actions. Exclude unsupported structures initially. |

All tag names are mapping candidates, not approved definitions or verified real-company coverage. Company-specific extensions and dimensional/class details may need filing inspection outside Company Facts. Initial simple single-class coverage is a proposed restriction requiring agreement, not an approved narrowing.

## Synthetic examples for definition review

All financial amounts are illustrative USD millions unless per-share; no real-company data.

1. **TTM revenue:** prior FY 1,200; current nine-month YTD 1,000; comparable prior nine-month YTD 850. TTM = 1,200 + 1,000 - 850 = **1,350**. Cross-check using current Q1 300, Q2 350, Q3 350 and prior Q4 350: sum = 1,350. The full year and both YTD periods must describe compatible scopes and exact fiscal boundaries.
2. **Operating margin:** TTM operating income 270 / TTM revenue 1,350 = **20%**. Underlying operating-income inputs must pass the same compatibility checks.
3. **P/E:** current price USD60 / already reviewed split-consistent TTM diluted EPS USD3 = **20x**. This example checks the division only; it does not demonstrate how to construct TTM EPS. Non-positive EPS produces not meaningful/unavailable, not a negative bargain ranking.
4. **EV/sales:** a verified simple single-class company with price USD60 and 100 million current shares has equity value 6,000. Debt 400 less cash 100 gives EV 6,300, assuming confirmed zero other applicable claims. EV/TTM sales = 6,300 / 1,350 = **4.6667x** displayed with agreed rounding. Debt/cash dates remain visible. If preferred/NCI status is unknown, this simplified example is not applicable.
5. **Split invariant:** a 2-for-1 split gives price 30, shares 200 million and comparable EPS 1.50. Equity value remains 6,000 and P/E remains 20x. Unadjusted mixed bases must be rejected.
6. **Missing history/input:** missing prior YTD prevents the additive TTM bridge unless four valid quarters exist. Missing debt/cash/claim evidence prevents EV/sales. Neither is replaced with zero.

No independent qualified-finance review or real-data reconciliation performed. Financial definitions remain pending.

## Go/no-go and exact next action

- Stage A: complete, with public-evidence limitations explicitly recorded.
- Developer live evaluation: conditional no-go until account eligibility and applicable permission for the intended developer display/use are established, credentials supplied securely and Stage B authorized.
- Shared pilot: no-go on current evidence; need affirmative display/derived-output and sharing rights plus an agreed cost/account model.
- Implementation: no-go until financial definitions and bounded implementation plan accepted. UI prototypes may continue with synthetic data under teammate ownership.

Recommended next decision: choose a developer-only Alpaca eligibility check as the leading route, or authorize a separate provider-rights enquiry for a shared pilot. No vendor contact is authorized by this review. Rights questions to resolve: may AlphaQuant developers display IEX prices and derived multiples internally; may named pilot users view them; must each have a separate account; what data may be cached/exported and for how long; which subscriber/exchange agreements apply? Do not ask for secrets in chat.

## Sources checked 2026-10-28

- [Alpaca plan comparison](https://docs.alpaca.markets/us/docs/about-market-data-api)
- [Alpaca streaming protocol](https://docs.alpaca.markets/us/docs/streaming-market-data)
- [Alpaca market-data FAQ](https://docs.alpaca.markets/us/docs/market-data-faq)
- [Alpaca general terms PDF](https://files.alpaca.markets/disclosures/library/TermsAndConditions.pdf) (uses older plan terminology; current account-specific applicability unverified)
- [Twelve Data pricing](https://twelvedata.com/pricing)
- [Twelve Data trial guide](https://support.twelvedata.com/en/articles/5335783-trial)
- [Twelve Data terms](https://twelvedata.com/terms)
- [Twelve Data exchange listing](https://twelvedata.com/exchanges) (US detail retrieval failed twice)
- [Finnhub terms](https://finnhub.io/terms-of-service)
- [Alpha Vantage quote documentation](https://www.alphavantage.co/documentation/)
- [Massive stocks](https://massive.com/stocks)
- [Massive business-price clarification](https://massive.com/knowledge-base/article/does-the-business-plan-include-real-time-trades-and-quotes-or-only-fair-market-value)
- [SEC EDGAR API documentation](https://www.sec.gov/search-filings/edgar-application-programming-interfaces)

Source inspection: src/AlphaQuant.Core/ConceptMap.cs, Models.cs and AnnualAnalyzer.cs; src/AlphaQuant.Infrastructure/SecParser.cs. No source files changed.


## Account-free zero-cost alternatives: follow-up review, 2026-10-28

No verified provider found in this review satisfying all three: zero total cost, account-free end users and redistributable real-time US stock prices usable in AlphaQuant calculations. This is a bounded search finding, not proof no such provider exists. SEC/Alpaca selection remains unchanged.

- [Tiingo terms](https://app.tiingo.com/tos/) require special permission and additional fees for redistribution; free individual access is not a fit.
- [FMP display guidance](https://site.financialmodelingprep.com/insights/platform/can-you-use-fmp-data-in-a-public-app-website-or-client-dashboard) requires confirming display rights for apps/sites, including free demos; no free right established.
- [Market Data pricing](https://www.marketdata.app/pricing/) offers custom commercial redistribution; the free plan is delayed and does not establish redistribution permission. Its [redistribution policy](https://www.marketdata.app/docs/account/data-policies/data-redistribution/) requires commercial licensing for app display.
- [TradingView free widgets](https://www.tradingview.com/widget/) are a display-only alternative to investigate, with branding and symbol/delay restrictions. Its [API explanation](https://www.tradingview.com/support/solutions/43000474413-i-need-access-to-your-api-in-order-to-get-data-or-indicator-values/) does not offer the raw-data access needed for custom valuation calculations. Desktop embedding/terms still require validation. This is not a replacement quote adapter or an approved UI change.
- [Cloudflare Workers free hosting](https://developers.cloudflare.com/workers/platform/limits/) has a free quota (100,000 requests/day); it supplies infrastructure, not market-data rights. Streaming/connection/resource feasibility and zero-cost operation are not established by that quota alone. No hosting choice approved.

If zero cost and account-free onboarding stay fixed, a proposed pilot fallback is SEC-based analysis with synthetic demo prices or user-entered prices, clearly labelled. This would relax automated real-time pricing and needs explicit user acceptance; it has not been selected. Delayed data also requires verified display rights and is not automatically redistributable.
