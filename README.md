# AlphaQuant

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** SEC desktop spike plus an implemented browser-OAuth/IEX/comparison backend. The backend is verified with synthetic/fake-provider tests; new screens are owned by the UI teammate and are not wired yet. OAuth registration, hosting and live validation are pending. No installer or release.

Read the [project proposal](docs/product/AlphaQuant_Proposal.md) for business scope, architecture diagrams, data flow, release gates and current progress. Diagrams render directly on GitHub.

## Initial direction

- C# with .NET and Avalonia desktop UI.
- Incremental, tested and documented releases.
- Source-linked financial data and explicit calculation definitions.
- Local processing and storage; start with one supported operating system.
- First comparison backend combines fundamental history/current TTM with guarded P/E and EV/sales under the finalized comparison-1 baseline. DCF/scenarios remain deferred.
- Selected direction: SEC EDGAR filing data plus Alpaca Basic IEX prices, using a replaceable market-data adapter. Pilot total incremental cost must be zero; no paid subscriptions. Users connect their own eligible free Alpaca accounts through browser OAuth (AQ-D022); a small server-side token broker protects the application secret. Registration/deployment/permitted use and live checks remain pending.

The precise analyses and release-one scope remain subject to scope review.

## Run the app

You need the .NET SDK pinned in `global.json` (10.0.403 or a later 10.0 feature release):

```powershell
dotnet build AlphaQuant.slnx
dotnet run --project src/AlphaQuant.Desktop
```

The SEC requires programs that download its data to identify themselves, so the app needs a working contact email. Open **Settings**, enter your email and press **Save**. It applies to the next request without a restart, is sent to the SEC in the User-Agent header, and is stored only on your computer in `%LOCALAPPDATA%\AlphaQuant\settings.json` (never in the repository). If nothing is saved, the `AQ_SEC_CONTACT` environment variable is used instead. With neither, the app shows a setup message and makes no SEC requests.

Then choose **Search Companies**, type a ticker or name (for example `AAPL`), and select a result to see the latest annual revenue, net income, current assets and current liabilities with their SEC filing, plus revenue growth, net profit margin and current ratio. Those three metrics use test definitions (version `test-0`) and are not approved metrics. Downloaded SEC data is cached under `%LOCALAPPDATA%\AlphaQuant\cache\sec`.

## Test

```powershell
dotnet test AlphaQuant.slnx
```

The tests use synthetic fixtures and never contact the SEC. Verified on Windows 11 x64 only. There is no installer or release yet.

## Contributing

Read [developer and agent guidance](docs/development/Agent_Guidance.md) for development instructions and the current continuation handoff. The proposal records the software scope, technical decisions and progress.

## UI design collaboration

Start with the [UI design brief](docs/development/UI_Design_Brief.md) for agreed analyses, browser-OAuth connection flow, screens, fake-data examples and loading/error states. UI integration can use the implemented ComparisonBackend facade with synthetic data; browser OAuth is selected and comparison-1 is finalized for implementation. Live setup and external finance review remain pending. The UI teammate owns flows; coordinate shared contracts before production integration.

## Documentation

| Category | Document | Purpose |
|---|---|---|
| Product | [Project proposal](docs/product/AlphaQuant_Proposal.md) | Authoritative scope, architecture, decisions and progress |
| Requirements | [Initial user stories](docs/requirements/User_Stories.md) | Proposed workflows and acceptance criteria |
| Requirements | [Financial analysis features](docs/requirements/Financial_Analysis_Features.md) | Proposed analyst capabilities, dependencies and delivery stages |
| Development | [Developer and agent guidance](docs/development/Agent_Guidance.md) | Workflow rules and current handoff; read before working |

Feature catalogues and user stories are proposals until explicitly approved; the proposal records approval status.

Provider selection planning: [price-aware comparison and provider evaluation](docs/requirements/Provider_Evaluation_Plan.md). SEC and Alpaca are selected as the current direction; Backend integration exists; deployed OAuth setup, UI wiring and permitted pilot access remain pending.

[Stage A provider and financial feasibility findings](docs/requirements/Provider_Feasibility_Review.md) record the conditional recommendation and unresolved pilot rights.

[Connection/comparison plan](docs/requirements/Comparison_Implementation_Plan.md) retains the planning history; current implemented contracts and pending work are in the [OAuth/UI setup guide](docs/development/Alpaca_OAuth_Setup.md) and [comparison-1 definitions](docs/requirements/Financial_Definitions.md).

## New comparison backend

`ComparisonBackend` (Infrastructure) combines an injected SEC fact source, browser OAuth, access testing, one IEX stream and two-to-five-company snapshots. The UI teammate can consume it without changing provider/calculation logic in views. No Desktop source files were changed for this backend increment. Historical cash facts can display directly; debt, P/E and EV/sales require the explicit review evidence described in the definitions.

See [registration, no-cost preview hosting and UI composition](docs/development/Alpaca_OAuth_Setup.md). Nothing has been deployed. The broker runs with server-side configuration using `dotnet run --project src/AlphaQuant.OAuthBroker`; running the Desktop alone does not expose the new flow yet.

Verified on 2026-10-28 using SDK 10.0.403: solution build succeeded with zero warnings/errors; 170 tests passed without live SEC/Alpaca calls. Loopback broker HTTP smoke checks passed. Docker deployment, real browser consent, real IEX prices, real-filing/independent finance review and end-user screen integration remain unverified.
