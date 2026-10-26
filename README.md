# AlphaQuant

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** proposal only. No application, installer, or verified financial calculations exist yet.

Read the [project proposal](AlphaQuant_Proposal.md) for business scope, architecture diagrams, data flow, release gates and current progress. Diagrams render directly on GitHub.

## Initial direction

- C# with .NET and Avalonia desktop UI.
- Incremental, tested and documented releases.
- Source-linked financial data and explicit calculation definitions.
- Local processing and storage; start with one supported operating system.
- Version 1 focuses on screening. Valuation analysis (such as DCF and scenarios) is a planned later phase.
- SEC filings plus external data providers are the planned data sources; no provider is selected yet.

The precise analyses and release-one scope remain subject to scope review. Setup and run instructions will be added when an executable application exists.

## Contributing

Read [AGENTS.md](AGENTS.md) for development instructions and the current continuation handoff. The proposal records the software scope, technical decisions and progress.
