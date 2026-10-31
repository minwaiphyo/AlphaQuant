# AlphaQuant

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** early spike with a native research workspace. Search SEC-listed companies, select two to five peers, compare supported annual facts/test metrics and inspect filing evidence. Metric definitions remain unapproved; there is no installer or release yet.

Read the [project proposal](docs/product/AlphaQuant_Proposal.md) for business scope, architecture diagrams, data flow, release gates and current progress. Diagrams render directly on GitHub.

## Initial direction

- C# with .NET and Avalonia desktop UI.
- Incremental, tested and documented releases.
- Source-linked financial data and explicit calculation definitions.
- Local processing and storage; start with one supported operating system.
- The next milestone direction combines fundamental comparison with current-price valuation; market-data providers will be evaluated now (AQ-D014). Definitions and implementation scope still need planning approval. DCF and scenarios remain later-phase candidates.
- Selected direction: SEC EDGAR filing data plus Alpaca Basic IEX prices, using a replaceable market-data adapter. Pilot total incremental cost must be zero; no paid subscriptions. Users may connect their own eligible free Alpaca accounts (AQ-D020); integration, connection security and permitted app use remain pending.

The precise analyses and release-one scope remain subject to scope review.

## Run the app

You need the .NET SDK pinned in `global.json` (10.0.403 or a later 10.0 feature release):

```powershell
dotnet build AlphaQuant.slnx
dotnet run --project src/AlphaQuant.Desktop
```

The SEC requires programs that download its data to identify themselves, so the app needs a working contact email. Open **Settings**, enter your email and press **Save**. It applies to the next request without a restart, is sent to the SEC in the User-Agent header, and is stored only on your computer in `%LOCALAPPDATA%\AlphaQuant\settings.json` (never in the repository). If nothing is saved, the `AQ_SEC_CONTACT` environment variable is used instead. With neither, the app shows a setup message and makes no SEC requests.

Choose **Choose companies**, search a ticker or name, and use **Add +** to select two to five peers. **Details** retains the single-company filing view. **Compare companies** opens a company-row grid with **Test metrics** (revenue growth, net profit margin, current ratio) and **Reported facts** (revenue, net income with its reported scope, current assets and current liabilities). These are existing annual test definitions (`test-0`), not approved metrics.

Use **+ Add filter** to apply numeric conditions; all conditions must match. Remove individual chips or clear filters. Sorting works through the column headers or sort selector; missing values stay last. Click a value for its inputs, periods, warnings and actual SEC filing links. Escape closes the evidence panel and restores focus. Fiscal reporting periods can differ between companies.

Selections, search, filters, sorting and loaded reports stay in memory while navigating, including returns from **Data connections**. **Reload data** uses the existing cache policy and retries failures; it does not force a fresh download. Cancel or a failed reload retains previous usable snapshots with explicit status. Downloaded SEC data is cached under `%LOCALAPPDATA%\AlphaQuant\cache\sec`.

Appearance follows the system by default; choose Light or Dark in the header. Appearance and peer-workspace state are session-only. TTM, five-year history, new financial metrics, market-provider authentication, live prices and valuation remain unavailable. Alpaca and additional provider options are labelled planned; the app does not collect provider credentials.

## Test

```powershell
dotnet test AlphaQuant.slnx
```

The tests use synthetic fixtures and never contact the SEC. Verified on Windows 11 x64 only. There is no installer or release yet.

## Contributing

Read [developer and agent guidance](docs/development/Agent_Guidance.md) for development instructions and the current continuation handoff. The proposal records the software scope, technical decisions and progress.

## UI design collaboration

The [UI design brief](docs/development/UI_Design_Brief.md) records the broader target. The accepted native UI increment (AQ-D022) uses the existing real SEC data path and existing test metrics; it does not add a production demo mode. Provider authentication and expanded financial definitions remain pending.

## Documentation

| Category | Document | Purpose |
|---|---|---|
| Product | [Project proposal](docs/product/AlphaQuant_Proposal.md) | Authoritative scope, architecture, decisions and progress |
| Requirements | [Initial user stories](docs/requirements/User_Stories.md) | Proposed workflows and acceptance criteria |
| Requirements | [Financial analysis features](docs/requirements/Financial_Analysis_Features.md) | Proposed analyst capabilities, dependencies and delivery stages |
| Development | [Developer and agent guidance](docs/development/Agent_Guidance.md) | Workflow rules and current handoff; read before working |

Feature catalogues and user stories are proposals until explicitly approved; the proposal records approval status.

Provider selection planning: [price-aware comparison and provider evaluation](docs/requirements/Provider_Evaluation_Plan.md). SEC and Alpaca are selected as the current direction; Alpaca integration and zero-cost pilot permissions remain pending.

[Stage A provider and financial feasibility findings](docs/requirements/Provider_Feasibility_Review.md) record the conditional recommendation and unresolved pilot rights.

[Connection and comparison implementation plan](docs/requirements/Comparison_Implementation_Plan.md) is a draft for scope review, not implemented behavior.
