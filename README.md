# AlphaQuant

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** early spike. The app can search SEC-listed companies and show test metrics, but the metric definitions are not approved and there is no installer or release yet.

Read the [project proposal](AlphaQuant_Proposal.md) for business scope, architecture diagrams, data flow, release gates and current progress. Diagrams render directly on GitHub.

## Initial direction

- C# with .NET and Avalonia desktop UI.
- Incremental, tested and documented releases.
- Source-linked financial data and explicit calculation definitions.
- Local processing and storage; start with one supported operating system.
- Version 1 focuses on screening. Valuation analysis (such as DCF and scenarios) is a planned later phase.
- SEC filings plus external data providers are the planned data sources; no provider is selected yet.

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

Read [AGENTS.md](AGENTS.md) for development instructions and the current continuation handoff. The proposal records the software scope, technical decisions and progress.
