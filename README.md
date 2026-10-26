# AlphaQuant

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** proposal stage. Only a placeholder window exists. There is no installer or verified financial calculation yet.

Read the [project proposal](AlphaQuant_Proposal.md) for business scope, architecture diagrams, data flow, release gates and current progress. Diagrams render directly on GitHub.

## Initial direction

- C# with .NET and Avalonia desktop UI.
- Incremental, tested and documented releases.
- Source-linked financial data and explicit calculation definitions.
- Local processing and storage; start with one supported operating system.
- Version 1 focuses on screening. Valuation analysis (such as DCF and scenarios) is a planned later phase.
- SEC filings plus external data providers are the planned data sources; no provider is selected yet.

The precise analyses and release-one scope remain subject to scope review.

## Run the placeholder app

The only application code is a placeholder app. A Home page shows a message and a "View Release Notes" button; the Release Notes page (static placeholder text) has a Back button. There is no financial functionality. You need the .NET SDK pinned in `global.json` (10.0.403 or a later 10.0 feature release):

```powershell
dotnet build AlphaQuant.slnx
dotnet run --project src/AlphaQuant.Desktop
```

Verified on Windows 11 x64 only. There are no tests, installer or release yet.

## Contributing

Read [AGENTS.md](AGENTS.md) for development instructions and the current continuation handoff. The proposal records the software scope, technical decisions and progress.
