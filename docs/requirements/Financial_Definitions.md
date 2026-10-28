# Financial definitions: comparison-1

Status: implementation baseline finalized under the user's explicit delegation on 2026-10-28. This is project approval of a bounded definition, not independent qualified-finance certification. Reviewed synthetic tests exist; real-filing reconciliation and an independent finance review remain outstanding. The older `test-0` engine remains intact for the existing Desktop spike.

## Scope and selection

- US domestic non-financial US-GAAP/USD companies are the intended coverage. The code does not infer eligibility or a simple capital structure from a ticker: explicit reviewed scope/equity evidence is required for valuations. Operating history can be inspected while coverage is still unverified.
- Allowed forms: 10-K/10-K/A and 10-Q/10-Q/A; ignore future filing dates/period ends relative to the requested analysis date. Period boundaries come from `start`/`end`, not the filing's `fy`/`fp` labels.
- Annual periods are 350–380 days inclusive. Display at most five distinct annual revenue periods, retaining a sixth for growth. Growth requires contiguous annual boundaries and the same revenue concept. Missing inputs are unavailable, never zero.
- Revenue tag priority remains `RevenueFromContractWithCustomerExcludingAssessedTax`, `Revenues`, `SalesRevenueNet`; never sum alternatives. Resolve latest filed fact per concept/period/unit, disclose changed values as restatements, and reject conflicting values filed on the same date.
- Preserve all fact contributions, including the negative prior-YTD contribution in a TTM bridge, with units, accession, filing date and period. Unsupported or missing inputs have a reason. Keep decimal precision until display; overflow is unavailable.

## Metrics

| Output | comparison-1 definition |
|---|---|
| Annual revenue growth | `(current / prior - 1) * 100`; prior positive, same concept, contiguous years |
| Operating income | `us-gaap:OperatingIncomeLoss`, USD, exact compatible duration |
| Operating margin | operating income / positive revenue * 100; start/end must match; annual and TTM labelled separately |
| Operating cash flow | `us-gaap:NetCashProvidedByUsedInOperatingActivities`, USD, exact compatible duration; not free cash flow |
| Cash balance | `CashAndCashEquivalentsAtCarryingValue`, non-negative USD instant at the latest revenue period end; restricted cash and investments not added |
| Debt balance | explicitly reviewed total interest-bearing debt, dated and source-referenced; parser retains candidate components but the engine does not assume they form a complete non-overlapping total |
| Additive TTM revenue/income/OCF | directly reported full year ending at the latest financial period, or four contiguous directly reported 70–110-day quarters spanning 350–380 days inclusive, or latest full year + current fiscal YTD - comparable prior fiscal YTD |
| YTD bridge | current YTD must start the day after the full-year end; prior YTD starts at that full-year start, lengths differ by at most seven days and ends are 350–380 days apart inclusive; one concept/unit per bridge |
| TTM diluted EPS | `EarningsPerShareDiluted`, USD/shares: directly reported full year ending at latest period, or sum of four directly reported contiguous quarters. Never reconstruct EPS by annual-minus-YTD subtraction. Common-share scope and split consistency require reviewed evidence |
| P/E | eligible current price / positive TTM diluted EPS, only with explicit matching issuer/class/split evidence through the evaluation date; unsupported/non-positive EPS unavailable |
| Enterprise value | matching class price * reviewed current outstanding common shares + reviewed debt + preferred-equity claims + non-controlling interests - approved cash |
| EV / TTM sales | enterprise value / positive TTM revenue, only with reviewed simple single-class capital structure and supported issuer scope. Negative enterprise value is allowed and disclosed through its inputs; not a buy signal |

## Valuation evidence and freshness

`CapitalStructureEvidence` is an explicit externally reviewed input. It must identify the CIK, matching symbol, filing reference, review reference, share date, balance-sheet date, split-adjustment date, coverage and simple-structure confirmation. Unknown preferred/NCI/debt/cash does not become zero. A zero is only supplied after review. Do not use diluted weighted-average EPS shares as current shares outstanding. The code does not yet automate review of these claims or extract current class-level capital structure from all SEC disclosures.

The baseline limits shares/balance-sheet dates and the TTM end used for valuation to 135 days old; source dates remain visible. Split-basis review must be current to the UTC evaluation date. Unsupported or stale evidence leaves valuation unavailable while historical fundamentals remain available.

Eligible price: matching symbol, positive USD value; event and receive times no more than 60 seconds old and no more than five seconds ahead of the clock. This is a conservative research validity policy, not a claim that an illiquid IEX stock should trade every minute. Outside market hours an old price remains inspectable but cannot produce a current valid multiple. No market-calendar integration is claimed. UI can label the session unknown and display actual age.

The Alpaca adapter initially accepts regular-sale condition `@` (or an empty condition array), rejects other trade conditions, and does not imply this subset is the full last-sale tape. Ignore duplicate/older event timestamps and events over five seconds in the future. Trade corrections/cancellations invalidate that symbol until a new eligible event arrives. Connection liveness is separate from price age. Comparison rechecks freshness every five seconds and on every feed event.

## Validation evidence and remaining limits

Synthetic fixtures cover 1,350 TTM revenue, 270 TTM operating income/20% margin, 360 TTM OCF, EPS 3, price 60/P-E 20x and EV 6,300/sales 4.6667x in consistent units. Tests cover split invariance, six-year growth support, missing YTD, forbidden EPS reconstruction, restatements/conflicts, future data, invalid currency, stale prices/capital, unsupported scope, overflow and parser allowlisting.

No real company values or provider connection were used. The baseline deliberately returns unavailable when directly reported EPS quarters or capital review are absent; it does not promise complete P/E/EV coverage. Changes to the mapping, scope, freshness policy or EPS/capital treatment require a new definition version and reviewed examples.
