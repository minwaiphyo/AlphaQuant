# SEC and Alpaca Comparison: Bounded Implementation Plan

## Implementation update, 2026-10-28

User authorized backend implementation, selected browser OAuth and delegated financial-definition finalization (AQ-D022). Core/Infrastructure contracts, annual/TTM/valuation engine, IEX access/stream adapter, browser client and server token broker now exist, with a fake end-to-end two-company test. See [OAuth/UI composition guide](../development/Alpaca_OAuth_Setup.md) and [comparison-1 baseline](Financial_Definitions.md). 169 tests passed and solution build is clean; no real OAuth registration/deployment/provider access or independent finance review occurred. Desktop/UI files were not changed; teammate integration remains pending. Earlier undecided authentication/formula labels below are historical where superseded.


## Latest accepted scope and clarification

AQ-D021 (2026-10-28): user confirms AlphaQuant is strictly valuation/comparison, with no trading. Session-only credential retention is approved if API-key entry is selected; no optional persistent secret storage in the initial scope. Initial valuation may exclude complex/unverifiable capital structures with explicit unavailable reasons while preserving supported fundamentals. Authentication approach is not selected: user requested clarification about credential capabilities. Paper trading refers to simulated orders that a provider credential might authorize, not an app feature; AlphaQuant must implement no order-placement/cancel/modify capability. Next action: clarify least-privilege supported authentication without treating API keys as read-only or adding trading scope.


Date: 2026-10-28. Status: proposed plan for user review, not implementation approval. Current authority: [proposal](../product/AlphaQuant_Proposal.md), AQ-D014 through AQ-D020. [Feasibility evidence](Provider_Feasibility_Review.md) remains applicable except superseded account-free/provider-selection assumptions.

## Outcome and boundaries

A Windows desktop comparison of two to five supported US domestic non-financial US-GAAP/USD companies: five annual years where available, reviewed TTM fundamentals, P/E and EV/sales, and IEX price streaming while comparison is open. Sources: SEC evidence and each user's own eligible Alpaca Basic access. Zero incremental pilot cost: no paid subscriptions, exchange/hosting services, pooled key, required funded account or redistribution backend. Market data stays behind application-owned replaceable contracts.

Teammate owns visual screens, navigation and UI flows. This plan proposes data/state contracts and backend work only; no teammate UI files are changed without coordination. No DCF, forecasts, historical valuation, trading, portfolio access, automatic recommendations, broad universe ingestion, shared accounts or release installer in this milestone. A working backend slice is not a complete validated pilot release.

## Connection proposal and security decision

Preferred minimal pilot candidate for review: user enters their own paper-account API key ID and secret in Settings. Use only market-data endpoints; do not call orders, funding or live-account endpoints. Paper credentials may still authorize paper trading and account operations; do not describe them as intrinsically read-only. Never accept a live credential as a convenience fallback. A key's environment cannot be established from a prefix alone; the supported environment-validation route and errors must be verified in the authorized evaluation before claiming enforced paper-only credentials. If that cannot be established, stop this route for explicit review rather than silently accept unverified live-capable keys.

Default: credentials in process memory for the session. Persistent credential storage is excluded from the initial scope by AQ-D021. Existing ContactSettings JSON is for SEC contact and must not store Alpaca secrets. Mask input, redact logs and errors, clear saved secrets on Forget, and disconnect before replacing credentials. Never ask the user to send secrets in chat. Disconnect stops streams; it does not revoke provider keys. Explain dashboard revocation separately.

Alternative: browser OAuth with least privilege (data scope; no trading/account-write). The official OAuth code exchange requires client_secret. PKCE/public-native support was not established by the reviewed page. Do not embed an application secret, invent support or assume a backend fits zero-cost scope. A secret-safe token exchange/registration route needs a separate accepted design before choosing OAuth.

These are proposals, not an account creation, app registration, vendor contact or API-test authorization. Per-user access does not alone establish permission for all pilot uses or exports. Verify applicable free account eligibility and terms first.

## Proposed UI-facing state contract

- Disconnected: annual/cached financial evidence remains usable; valuation prices unavailable until connected.
- Connecting/authenticating/subscribing: cancellable progress; no claim that connected means new price received.
- Connected IEX: source/limited venue, event timestamp, receive time and last-price age shown per row.
- No price yet, unavailable symbol, stale price, feed disconnected, entitlement failure and connection-in-use are separate text states.
- Leaving comparison, Disconnect, app exit and cancellation stop subscriptions and reconnect work. Re-entering opens one connection for the selected symbols.
- Show last known prices after interruption only with explicit old/disconnected labels; no silent delayed/SIP/other-provider fallback.
- Proposed freshness policy for review: regular-session events older than 60 seconds are labelled old; age alone does not prove a broken connection. Session closed/unknown and clock-skew states distinct. No automatic buy/sell ranking using invalid inputs. The threshold is a proposal, not a market-data guarantee.
- Initial price semantics: eligible last trade, not midpoint or NBBO. Review trade-condition inclusion against provider rules; duplicate/out-of-order events must not replace newer state. Corrections/cancellations invalidate affected displayed values pending a verified replacement.
- Non-aligned row timestamps stay visible; no implied synchronized executable prices.

## Proposed financial policy to review

Initial safe coverage candidate: simple single-class common-equity issuers with verifiable capital structure; mark unsupported valuation for multi-class/complex cases rather than block otherwise valid annual metrics. This valuation restriction is approved by AQ-D021; exact eligibility checks and financial definitions still need review.

| Output | Proposed definition and evidence rule |
|---|---|
| Annual revenue growth | (current/prior comparable revenue - 1) x 100; prior must be positive; five growth values need six observations, otherwise first unavailable |
| Operating margin | compatible operating income / positive revenue x 100; annual and TTM separately labelled |
| Operating cash flow | reported operating cash flow with duration/units; not free cash flow |
| Debt/cash | instant facts with exact date; approved interest-bearing-debt map and cash-equivalent policy; no blind addition of total plus component tags |
| TTM revenue/operating income/OCF | compatible latest full fiscal year + current YTD - prior comparable YTD, or four non-overlapping discrete quarters; no overlapping YTD sums |
| Trailing P/E | eligible price / reviewed split-consistent TTM diluted EPS attributable to common shareholders; non-positive or unverifiable EPS unavailable |
| TTM EPS construction | reviewed directly reported discrete quarters on consistent basis; no annual-plus-YTD EPS subtraction. Missing Q4/ownership/split evidence makes result unavailable until an approved reconstruction exists |
| EV/sales | (reviewed equity market cap + interest-bearing debt + applicable preferred equity + NCI - approved cash) / positive TTM revenue; unknown adjustments are not zero |
| Equity market cap | verified latest outstanding common shares x matching class price for initial simple structures; never diluted weighted-average EPS shares; dated snapshot warning retained |

Proposed cash policy excludes restricted cash and marketable securities unless separately approved. Debt/lease/convertible mapping and materiality are not yet approved. All dates, currencies, scopes, restatement choice and definition versions are visible. Use latest publicly filed evidence for current research; this is not historical point-in-time backtesting. Show inability to bridge post-filing splits/issuance rather than imply current shares. Decimal arithmetic, no intermediate display rounding.

## Increment sequence and acceptance

1. **Contract and fixture review:** agree connection route, session-only secret handling, financial policies and teammate contracts. Deliver explicit source/tag/unit/period mappings and independently checked synthetic models. No live calls needed. Exact affected files proposed: Core Contracts/Models or small dedicated files; Infrastructure parser and narrowly scoped new adapters; tests and docs. Do not replace existing spike definitions silently.
2. **Annual/TTM foundation:** extend allowlisted parsing for approved concepts and units, 10-Q/10-Q/A selection, annual histories, YTD/discrete-quarter handling and lineage. Acceptance: reviewed normal/missing/restated/52-53-week/split fixtures produce expected outputs and clear unavailable reasons. Negative denominators handled deliberately. Existing test-0 behavior retained or migrated with explicit documentation.
3. **Market-data and credential adapter:** application-owned stream/security identity interfaces in Core; Alpaca REST/WebSocket implementation in Infrastructure; session-only credential source. Acceptance: fake transports prove auth/entitlement errors, connection cap, subscription lifecycle, backoff, duplicate/late/corrected events, redaction and cancellation. No trading methods introduced. No paid package/service requirement.
4. **Financial comparison service:** join approved snapshots by entity/class identity; calculate versioned P/E and EV/sales only when the inputs are usable. Price changes only recalculate price-dependent metrics. Acceptance: independent examples reconcile and sources/dates remain inspectable; unsupported valuation does not erase valid fundamentals.
5. **Teammate integration:** provide synthetic examples and stable contracts for connection and comparison states; coordinate changes before implementation. Verify two-to-five-company flow, leaving/reopening comparison and settings/disconnect. Backend owner does not rewrite teammate-owned screens unilaterally.
6. **Authorized manual evaluation:** after access/terms eligibility and user authorization, use a few supported issuers and securely supplied credentials. Confirm IEX timestamps, environment/entitlements, disconnect and connection-in-use behavior; compare calculations with reviewed filings. No keys/data fixtures committed. Automated tests never require live SEC/Alpaca. Record exact observed evidence and limitations.

## Verification and recovery

Run proportionate dotnet build/test commands from README after implementation when SDK is available; no current run claimed. Financial tests include currency/scope mismatch, overlapping periods, negative earnings, missing claim evidence, diluted versus outstanding shares, post-filing split mismatch and amendments. Stream tests include 401/403, 429/backoff, connection cap, stale/out-of-order data, malformed frames, corrections and cancellation. Credential tests avoid actual user vault/data unless explicitly planned and cleaned up. Preserve prior usable financial snapshots; no destructive cache reset as error recovery. Secret-store failure means session-only or explicit failure, never plaintext fallback. Errors show recovery actions without credentials.

No application files, dependencies, credential stores or runtime settings changed during this planning work. No live access or actual finance review claimed. Local PDF synchronization is tracked separately.

## Approval needed before implementation

Choose the supported connection route; session-only retention and initial complex-structure valuation exclusions are already agreed. Agree financial definitions; approve the bounded first increment rather than implicitly approve the entire roadmap. Provider eligibility/live evaluation remains a separate prerequisite, not a paid fallback. One coherent first increment recommended: contract/financial-fixture review, then the accepted annual/TTM foundation.

## Official references checked 2026-10-28

- [Alpaca OAuth guide](https://docs.alpaca.markets/us/docs/using-oauth2-and-trading-api): documented scopes and secret-based code exchange; native PKCE support not established.
- [Alpaca streaming protocol](https://docs.alpaca.markets/us/docs/streaming-market-data)
- [Alpaca stock-event definitions](https://docs.alpaca.markets/us/docs/real-time-stock-pricing-data)
- [Alpaca free IEX plan](https://docs.alpaca.markets/us/docs/about-market-data-api)

These references describe provider capabilities, not approval of AlphaQuant pilot permissions or a guarantee that the proposed desktop flow is supported without further validation.

UI companion: [teammate design brief](../development/UI_Design_Brief.md). Fields and conceptual contracts are mockup guidance, not compiled/approved integration interfaces.
