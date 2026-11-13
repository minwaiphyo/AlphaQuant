# Reference arithmetic usage

Run from an available Python 3 environment with no third-party dependencies:

```text
python docs/skills/alphaquant-quant-models/scripts/reference_math.py --input <local-worked-example.json>
```

All amounts use the same base currency unit, actual share units and compatible per-share EPS. Use decimal strings or integers, not JSON floating-point values. Examples below are fictional. Derive an expectation independently before comparing production outputs. Do not use this script to choose facts, certify eligibility or activate a draft DCF stage.

## Additive TTM

Input: `{"kind":"ttm-flow-bridge","full_year":"1200","current_ytd":"1000","prior_ytd":"850"}`.

Hand expectation: full year plus current YTD less the comparable prior YTD is 1,350. The script checks arithmetic only. The evidence skill must establish periods, concept/unit consistency and bridge eligibility. Never apply this bridge to EPS.

EPS input: `{"kind":"eps-four-quarters","quarters":["0.5","0.75","0.75","1"]}`. Four compatible directly reported quarters sum to 3. No date compatibility is established by supplying four numbers.

## Current multiples arithmetic

```json
{
  "kind": "multiples",
  "price": "60",
  "ttm_eps": "3",
  "ttm_revenue": "1350000000",
  "shares": "100000000",
  "debt": "400000000",
  "cash": "100000000",
  "preferred": "0",
  "nci": "0"
}
```

Hand expectation: common equity capitalization 6,000,000,000. Add debt and subtract cash for enterprise value 6,300,000,000. P/E is 20. EV/sales is 14/3, approximately 4.6666667. Nonpositive EPS/revenue produce a null respective multiple. Missing claim fields are rejected instead of defaulting to zero. Negative enterprise value is retained.

A stale quote may still produce mathematically correct numbers here. Production eligibility must reject it according to the actual versioned policy. The script's `numeric_reference_only` flag and `not_validated` list are part of its output contract.

## End-year FCFF DCF arithmetic

```json
{
  "kind": "end-year-fcff-dcf",
  "fcff": ["110", "121"],
  "wacc": "0.10",
  "terminal_growth": "0.03",
  "terminal_fcff_next": "124.63",
  "shares": "100",
  "debt": "100",
  "cash": "20",
  "preferred": "10",
  "nci": "5"
}
```

Hand expectation: each explicit cash flow discounts to 100, so explicit PV is 200. Terminal cash flow is supplied explicitly rather than inferred from an unsustainable terminal-growth assumption. Terminal PV is 10,300/7. Enterprise value is 11,700/7. The net capital bridge subtracts 95, giving common equity 11,035/7 and per-share value 2,207/140, approximately 15.7642857143.

The model intentionally checks only a stated end-year numeric case, not FCFF normalization, sustainable reinvestment, professional review or market-price upside. The application DCF definition is finalized in its authorized stage. Invalid discount/growth relations and unbounded/non-finite inputs are rejected. The test suite uses rational expectations and meaningful negative/scaling cases.
