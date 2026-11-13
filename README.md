# AlphaQuant

**Continue development:** read the [autonomous development roadmap](docs/roadmap/Development_Plan.md), [execution runbook](docs/roadmap/Stage_Runbook.md) and [stage ledger](docs/roadmap/Stage_Ledger.json). The full roadmap is a draft pending owner acceptance or a stage invocation after review. "Proceed with the next stage" then supplies the execution scope and standing Git/validation policy. It does not imply that external accounts, rights or deployment gates are already complete.

**Specialized agent skills:** the [project skill library](docs/skills/Skill_Routing.md) covers SEC evidence, quantitative models, Avalonia design, data connections, research storage and stage verification. Run `powershell -File tools/Install-ProjectSkills.ps1` for local Codex discovery links, or read the canonical `SKILL.md` files directly in another agent. The stage runbook routes only the skills relevant to each package. Installing skills does not start development stages.

A planned transparent desktop screening and valuation workspace for financially knowledgeable users. It aims to turn financial filings and data into explainable, reproducible screening results through a clear GUI, so analysts can accelerate stock research without rebuilding Excel models from scratch. It is a research tool, not trading or investment advice.

**Status:** Integrated Windows preview candidate: SEC company selection, annual test-0 workspace and comparison-1 peer summaries/history/TTM/source evidence. Browser OAuth and IEX streaming are wired with session-only tokens and explicit access tests. Reviewed valuation inputs can be loaded for the session; missing or invalid evidence remains unavailable. OAuth registration/HTTPS deployment, trusted distribution and live verification are still required for a connected pilot.

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

Provider selection planning: [price-aware comparison and provider evaluation](docs/requirements/Provider_Evaluation_Plan.md). SEC and Alpaca are selected as the current direction; Backend integration exists; deployed OAuth setup, live acceptance and permitted pilot access remain pending.

[Stage A provider and financial feasibility findings](docs/requirements/Provider_Feasibility_Review.md) record the conditional recommendation and unresolved pilot rights.

[Connection/comparison plan](docs/requirements/Comparison_Implementation_Plan.md) retains the planning history; current implemented contracts and pending work are in the [OAuth/UI setup guide](docs/development/Alpaca_OAuth_Setup.md) and [comparison-1 definitions](docs/requirements/Financial_Definitions.md).

## Integrated preview

The native UI (AQ-D023) and browser-OAuth/comparison-1 backend (AQ-D022) are merged and integrated through PR #18. Financial analysis consumes ComparisonBackend for peer summaries, annual/TTM history, price status, source evidence and guarded valuation. The existing annual test-0 workspace remains separately labelled. Data connections supports browser OAuth, access tests and session-only reviewed equity/capital inputs. No trading or token persistence is implemented.

Read [First preview setup, limitations and reviewed evidence schema](docs/development/First_Preview.md). Build a self-contained Windows ZIP with `powershell -File tools/Publish-Preview.ps1`. No SDK is needed on the receiving PC; the unsigned build may still be blocked by Windows policy. Do not disable security controls. Set the public `AQ_OAUTH_BROKER_ORIGIN` environment variable before launch after the operator has registered/deployed the broker.

Delivery evidence (2026-11-01): 203 synthetic/fake-provider tests pass; the full Release build and [Windows CI/package run](https://github.com/minwaiphyo/AlphaQuant/actions/runs/41314939340) passed. The downloaded CI ZIP checksum was verified and its executable started on this laptop. Synthetic native analysis/evidence/Settings checks used isolated data; no live SEC/Alpaca acceptance is claimed. A draft v0.1.0-preview.3 package is retained in GitHub Releases, pending external setup and distribution gates.

Before a connected pilot: complete [Alpaca registration and zero-cost HTTPS broker setup](docs/development/Alpaca_OAuth_Setup.md), confirm eligibility/permitted use, reconcile real financial data and establish trusted distribution. Nothing has been deployed to a live OAuth host. Imported review assertions are not finance certification; missing or incompatible inputs remain unavailable.
