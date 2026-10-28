# AlphaQuant — Initial User Stories

## First working prototype

### US-01 — Select a company
As a financial analyst, I want to find and select a supported company so that I can examine its financial results.

Acceptance criteria:
- I can search by company name or ticker.
- Results display enough information to identify the correct company.
- Unsupported companies produce a clear message.

### US-02 — Retrieve financial data
As a financial analyst, I want to retrieve a company’s annual financial data so that I can analyse its reported performance.

Acceptance criteria:
- Refresh displays progress and allows cancellation.
- The interface remains usable during retrieval.
- Network failures show a useful message and a retry option.
- A failed or cancelled refresh preserves previously usable data.

### US-03 — View annual revenue growth
As a financial analyst, I want to see annual revenue growth so that I can compare a company’s revenue across consecutive reporting years.

Acceptance criteria:
- Growth uses the approved formula and comparable annual periods.
- The result displays both reporting periods and underlying revenue values.
- Missing inputs, incompatible periods or non-positive prior revenue produce “unavailable” with a reason.
- Display rounding does not change the underlying calculation.

### US-04 — Verify a result
As a financial analyst, I want to inspect a metric’s formula and source facts so that I can understand and verify the result.

Acceptance criteria:
- I can view the formula, input values, units and reporting periods.
- Filing date, retrieval time and metric-definition version are available.
- I can open the supporting SEC filing.
- Each result refers to the specific facts used in its calculation.

## Initial screener

### US-05 — Compare companies
As a financial analyst, I want to see supported companies in one table so that I can compare their financial metrics.

Acceptance criteria:
- Each row identifies a company.
- Metric columns clearly identify their units.
- Reporting periods are available for inspection.
- Valid, missing, unsupported and stale results are distinguishable without relying only on colour.

### US-06 — Filter and sort results
As a financial analyst, I want to filter and sort companies by financial metrics so that I can create a research shortlist.

Acceptance criteria:
- I can apply numerical thresholds to supported metrics.
- Multiple filters work together, and active filters remain visible.
- Unavailable values do not silently count as zero or pass numerical filters.
- I can sort results and clear all filters.
- An empty result explains that no companies match.

### US-07 — View profitability
As a financial analyst, I want to see annual net profit margin so that I can compare how much reported profit companies generate from revenue.

Acceptance criteria:
- The calculation uses compatible annual net income and revenue.
- Revenue must be positive.
- The income measure’s ownership scope is clearly defined.
- Invalid inputs produce an explanation.

Dependency: approve the metric definition and supported fact mappings.

### US-08 — View short-term liquidity
As a financial analyst, I want to see the current ratio so that I can compare reported current assets with current liabilities.

Acceptance criteria:
- Inputs use the same balance-sheet date, currency and reporting scope.
- Current liabilities must be positive.
- Companies without a comparable classified balance sheet show an unsupported result.
- The underlying values and source are inspectable.

Dependency: approve the metric definition and supported fact mappings.

## Usable pilot release

### US-09 — Maintain a watchlist
As a returning user, I want to save companies in a local watchlist so that I can revisit them without repeating my search.

Acceptance criteria:
- I can add and remove companies without duplicates.
- The watchlist persists after restarting the app.
- Refreshing financial data does not remove my selections.

### US-10 — Review cached results
As a returning user, I want to open previously retrieved results without a network connection so that I can continue reviewing them offline.

Acceptance criteria:
- Previously saved results remain accessible offline.
- The app displays when the data was retrieved and whether it is stale.
- An unsuccessful refresh does not replace valid cached results with empty values.
- If no cached data exists, the app explains that a connection is required.

### US-11 — Use an accessible interface
As a user, I want readable screens and keyboard navigation so that I can complete the screening workflow comfortably.

Acceptance criteria:
- Core controls are reachable and usable with the keyboard.
- Text remains readable at supported display scaling.
- Loading, error and data-quality states use text or symbols alongside colour.
- Refresh preserves my current selection and filters where possible.

### US-12 — Install and upgrade
As a Windows user, I want a straightforward installation and upgrade process so that I can use new releases while retaining my watchlist.

Acceptance criteria:
- Installation and first launch succeed on a clean supported Windows environment.
- Supported upgrades preserve watchlists and settings.
- Failed upgrades have a documented recovery path.
- Uninstallation provides an explicit choice about retaining local data.

### US-13 — Report a problem
As a pilot user, I want to report confusing behaviour or incorrect results so that the developers can investigate and improve the app.

Acceptance criteria:
- The app provides an accessible feedback route and its version number.
- Any diagnostics export requires my action and can be reviewed before sharing.
- Diagnostics exclude credentials and unnecessary personal information.