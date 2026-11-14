# Expanded fundamentals — expanded-1

Project definition established under roadmap S12, 2026-11-14. Intended universe is
US domestic non-financial US-GAAP/USD. Outputs are research calculations, not
issuer/class eligibility certification. comparison-1.1 remains unchanged.

Use its five exact annual revenue intervals and latest-filed-as-of selection;
resolve same-date conflicts as unavailable, never sum alternative tags. A new
metric's inputs must have exactly matching duration or period-end instant.
Beginning averages use the day before the annual start. Parent net income uses
NetIncomeLoss only; consolidated ProfitLoss is not silently substituted for ROE.
Gross profit uses GrossProfit only; no guessed cost-of-sales composition.

| Measure | Formula / unit / edge policy |
|---|---|
| Gross/net margin | GrossProfit or parent NetIncomeLoss / positive revenue ×100 |
| Current ratio | nonnegative AssetsCurrent / positive LiabilitiesCurrent at end |
| EPS / operating-profit growth | (current/prior−1)×100; matching contiguous annuals and strictly positive prior; diluted EPS split comparability unverified |
| Revenue CAGR | positive same-tag endpoints over 3 years; 3 contiguous annual intervals required, (end/base)^(1/3)−1 ×100; double root converted to decimal, tolerance 1e-9 percentage points |
| FCF | reported OCF − nonnegative PaymentsToAcquirePropertyPlantAndEquipment; not FCFF |
| Cash conversion | OCF / positive parent net income; negative OCF retained |
| ROE | parent net income / positive average beginning/ending StockholdersEquity ×100; either nonpositive equity unavailable |
| ROIC | EBIT × (1−effective tax rate) / positive average invested capital ×100; tax expense / positive consolidated continuing pretax income must be 0–1; losses/refunds unavailable, no synthetic tax benefit |
| Invested capital | consolidated equity including NCI + long-term debt/finance-lease obligations including current maturities + ShortTermBorrowings − cash; both distinct debt buckets explicit, no missing-as-zero. Average beginning/end; operating leases and goodwill adjustments excluded |
| Asset turnover | revenue / positive average total assets; both endpoints positive |
| Receivable/inventory ratios | receivables or inventory / positive revenue; assets held at period end, not average turnover |
| Net debt | the two explicit debt buckets above − nonnegative cash; negative result retained |
| Interest coverage | EBIT / strictly positive InterestAndDebtExpense; nonpositive expense unavailable |
| Debt maturities | separately reported long-term principal due within 12 months and years 2–5; exact instant tags, no allocation from total debt or undisclosed zeros |
| Shares/dilution | end CommonStockSharesOutstanding, annual diluted weighted-average shares and growth in those weighted-average shares; aggregate issuer values, no current class/split guarantee |
| Dividends/buybacks | explicit nonnegative PaymentsOfDividendsCommonStock, PaymentsForRepurchaseOfCommonStock; DividendsPerShareCommonStock is separate USD/shares; no total-dividend preferred/common allocation |

Decimal arithmetic retains full precision; rational-model comparisons allow 1e-24
absolute rounding tolerance except the explicitly approximated CAGR root.

Every output includes exact raw facts, dates, units, formula version and unavailable
reason. Missing fields are an issuer-specific data gap; the engine still implements
each measure. No note-dependent maturities or capital totals are inferred. Stored
expanded arrays retain their definition and original results; older snapshots have
no expanded outputs and are never silently recomputed. Filtering uses raw decimals.
No market quote, review note or session capital object enters this model.

Worked synthetic year: revenue 200, gross profit 80, parent income 20, EBIT 40,
OCF 30, capex 12, current assets/liabilities 90/30, assets 180→220, parent equity
80→120, consolidated equity 90→130, long debt 50→60, short borrowing 10→10,
cash 20→20, tax/pretax 5/25, interest 8: gross/net margins 40%/10%, current
ratio 3, FCF18, cash conversion1.5, ROE20%, asset turnover1, invested capital
130→180 (average155), ROIC 640/31%=20.64516129…%, net debt50 and coverage5.
Real-filing reconciliation remains S02's external gate; no real issuer acceptance
or independent finance certification is implied.
