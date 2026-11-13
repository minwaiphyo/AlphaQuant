# S02 financial source validation

Date: 2026-11-13. Skills: SEC evidence, quantitative models and stage verification, library 1.0. S01 merged at 1e3b11b with 206 application tests passing on clean CI run 43433331979. Local runtime tests remain blocked by Application Control, while compilation works.

## Case matrix

The intended live sample is bounded to MSFT, AAPL, COST, KO and AMZN. Resolve identity from the actual SEC directory before assigning a CIK. Ticker or data availability does not certify domestic/non-financial/US-GAAP/USD or simple-class eligibility. Capture annual/quarterly periods, a comparable YTD bridge, missing/unsupported facts and any actual amendments encountered. Never invent a restatement if the sample does not contain one.

| Case | Deterministic evidence | Live source status |
|---|---|---|
| Annual/TTM revenue and operating margin | s02-independent-worked.sample.json with hand-authored expectations, misleading fiscal labels and actual interval assertions | Pending configured SEC contact and independent filing inspection |
| OCF and direct-quarter diluted EPS | Same fixture, four discrete quarters and separate signed additive bridge | Pending source capture and filing reconciliation |
| Amendment/same-date conflict/future filing | Existing ComparisonBackendTests and parser fixtures | Inspect actual candidate disclosures after capture, no fabricated real example |
| Ambiguous prior FY for YTD bridge | New input-order reversal regression and identical-duplicate control | Corrected conservatively in comparison-1.1 |
| P/E and EV/sales bridge | Fictional explicitly reviewed scope/capital, date-compatible price, exact expected arithmetic | No live market account/quote or real reviewed capital evidence claimed |
| Missing, currency/period mismatch, nonpositive denominators | Existing deterministic negative cases and independent skill tests | Remain unavailable per source/evidence policy |

## Independent expected arithmetic

All values in the fixture are invented and labelled synthetic. Revenue TTM is 1.2 billion plus 1.0 billion less 0.85 billion, giving 1.35 billion. Operating income is 240 + 200 - 170 million, giving 270 million and 20 percent margin. OCF is 300 + 280 - 220 million, giving 360 million. Direct EPS quarters 0.50 + 0.75 + 0.75 + 1 sum to 3, without a YTD EPS subtraction.

Price 60 and EPS 3 give P/E 20. Actual shares 100 million give equity capitalization 6 billion. Explicit debt 400 million, cash 100 million and reviewed zero preferred/NCI give EV 6.3 billion and EV/sales 14/3. These expectations are written independently of production calculations and can be cross-checked with the Decimal skill reference. They do not prove any real issuer's scope, facts or eligibility.

## Definition correction

Comparison-1.1 rejects a YTD bridge when the newest prior FY end has conflicting annual start dates. Previously the bridge path could select by API order even though annual-history/full-year paths rejected that ambiguity. Identical disclosures of one interval remain resolvable. Preserve the version of historical snapshots instead of relabelling them.

## Reproducible capture workflow

The no-new-package console tool `tools/AlphaQuant.SourceCapture` uses existing SEC/contact/parser contracts, an isolated memory cache, a 500 ms minimum request interval, at most one retry per request, one directory plus at most five issuer lookups, bounded responses and a three-minute total timeout. It never accepts contact/secret text as a command argument or prints it.

```text
dotnet run --project tools/AlphaQuant.SourceCapture -- --preflight
dotnet run --project tools/AlphaQuant.SourceCapture -- --live-sec --output .local/source-validation/new-capture
```

Preflight never sends requests. The live command refuses missing contact before network access and refuses an existing output directory. Capture output contains public candidate facts, selected source contributions, as-of/version/URLs and warnings. It is labelled capture-only/pending-independent-source-review. Include competing facts for the bounded source horizon, not just winners. Before committing fixtures, inspect actual filings and independently derive expected values, reduce to minimal public facts and keep private settings/headers out.

## Blocker and completion policy

BL-001/E01: no saved/process contact is configured. No live SEC or Alpaca data was requested. S02.2 and full S02.4 remain blocked. S02.1 and deterministic/source-tool work in S02.3 are deliverable now. This report is partial validation, not completion of S02 or finance certification. Next: configure the contact locally, run preflight/capture in an allowed execution environment, inspect the actual filings and finish reconciliation. Meanwhile continue independent S03 work.

Primary interface/access references checked on 2026-11-13: [SEC APIs](https://www.sec.gov/search-filings/edgar-application-programming-interfaces) and [SEC developer resources](https://www.sec.gov/about/developer-resources). The latter calls for efficient bounded requests and a total limit of ten requests per second across machines. The capture tool is deliberately below that and does not run in default tests.
