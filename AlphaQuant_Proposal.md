# AlphaQuant Project Proposal and Development Record

Document revision: 0.8
Last updated: 2026-10-28 Asia/Singapore
Product status: early spike; SEC company search with unapproved test metrics, no release  
Working name: AlphaQuant  

## 1 Purpose and document authority

AlphaQuant is a proposed transparent desktop screening and valuation workspace for financial analysts and financially knowledgeable users who want to accelerate stock research without rebuilding Excel models from scratch. It will automate selected financial-data collection and make a small set of clearly defined analyses accessible through a usable GUI. Screening comes first; valuation is a later, separately approved phase (AQ-D007). The product is intended for production use, with traceable calculations, tested releases, and reliable desktop distribution.

This is both the product proposal and the living development record. Read sections 16 through 19 for current progress. Approved project decisions, proposed implementation details, and verified implementation facts must remain distinct. A planned feature is not an implemented feature.

## 2 Business problem and target users

The working problem hypothesis is that collecting filings, reconciling reporting periods, and constructing repeated screening and valuation calculations creates manual work and inconsistent spreadsheet workflows. Validate this with prospective users before promising time savings or broad differentiation. Problems to test:

- repeated manual extraction from annual reports and financial statements;
- inconsistent handling of periods, units and filing structure;
- slow preparation of screening workflows, and later valuation workflows;
- spreadsheet errors and hidden assumptions;
- inconsistent peer comparison;
- no structured, auditable record of research.

Primary audience: analysts or users comfortable interpreting financial statements, such as independent investors with a finance background, equity analysts, portfolio managers or investment associates, wealth-management and advisory professionals, and serious personal investors who keep watchlists and models. They are not assumed to be software developers and want speed, consistency and defensible analysis rather than a black-box trading engine. Initial reach: a limited pilot group. Do not assume professional adoption, willingness to pay, or broad market coverage.

Proposed positioning (provisional, AQ-D006): AlphaQuant is a transparent desktop valuation and screening workspace for investors and analysts who want to accelerate stock research without rebuilding Excel models from scratch. It is a research and decision-support tool, not a trading system, recommendation engine or source of guaranteed returns.

Proposed first workflow: select a small universe of companies, retrieve annual filing facts, inspect data quality and provenance, calculate a few comparable metrics, filter/sort results, and open the supporting filing. The exact workflow and metrics require scope approval before implementation.

The product should help users inspect evidence and assumptions. It does not promise stock-price prediction, investment performance, or replacement of professional judgment.

## 3 Value proposition and validation

Desired benefits are a clear desktop GUI, less repeated manual collection, reproducible financial calculations, and inspectable source evidence. These are hypotheses to validate against spreadsheets and existing screeners, not claims of uniqueness.

Candidate differentiators to test with users (hypotheses, not established advantages):

- Analyst-first workflow for people who know finance and want to work faster, not casual retail trading.
- Transparent, adjustable assumptions and explainable outputs. In v1 this means screening thresholds, peer/universe choice and metric definitions; adjustable growth, margin, discount-rate and terminal-value inputs belong to the later valuation phase.
- Source-linked analysis tied to filing facts, periods and quality warnings.
- A lighter local desktop workflow than institutional platforms. Verify this against real alternatives before claiming it.
- Later: scenario-based analysis without duplicating spreadsheets.

Compare against generic screeners, institutional platforms and spreadsheets in the first user interviews. Record what users already use and what they distrust.

Before expanding scope, conduct a small number of workflow interviews and prototype sessions. Ask users to show a real recurring task, required metrics, current tools, common data failures, and a result they would trust. Record findings without sensitive employer or portfolio information.

Proposed pilot success criteria, to agree before recruitment:

- Several target users complete one screening workflow without developer assistance.
- Users can identify the source and definition of each displayed metric.
- At least some pilot users return for a second screening session.
- At least one release improvement is driven by observed feedback.
- A repeatable task is benchmarked against the previous workflow before claiming time savings.

Business model is undecided. Begin with a limited pilot, not a payment platform. Options to evaluate later, none selected: one-time desktop license; a paid professional tier with the valuation phase; optional subscription for additional universes or premium data; optional support or data packages. Premium data implies data-provider licensing and cost, so model provider cost before pricing. Budget for signing/distribution, data access, hosting only if needed, and maintenance. Review provider terms before redistribution or monetization. Public accessibility does not imply unrestricted redistribution rights.

## 4 Approved direction and proposed boundaries

### Agreed project direction

- Project and local repository name: AlphaQuant.
- Choose the C# desktop option discussed: .NET with Avalonia as the intended UI framework.
- Develop through releases. Version 1 should contain basic functionality and a few basic financial analyses.
- Test and document each increment; keep the codebase structured, readable, maintainable, and easy to debug.
- Build for real users, visual usability, robustness, and clear documentation.
- Keep this proposal current as requirements, decisions and implementation evolve.
- Use the screening-and-valuation workspace direction in section 2 as the working business proposal for now (AQ-D006, provisional; subject to user validation).
- Valuation analysis (DCF, scenarios) is a later phase, not part of v1 (AQ-D007).
- Plan on external data providers for financial metrics and documents, including SEC filings. Provider selection and terms remain open (AQ-D008).
- Version 1 uses SEC EDGAR as its only data source; commercial or market-data providers are deferred to the valuation phase (AQ-D009).

### Product principles

- Show where each number comes from; separate raw filing data from calculated outputs.
- Surface missing, stale or unsupported data; never silently force an unsupported accounting structure into a result.
- Make assumptions and thresholds explicit and adjustable where a workflow uses them.
- Avoid false precision. Make no prediction, guaranteed-return or buy/sell claims, and no opaque AI judgments.
- Support research; do not automate trading.

### Proposed v1 scope for review

[User_Stories.md](User_Stories.md) records the initial user stories US-01 through US-13 and their acceptance criteria, grouped into first working prototype, initial screener and usable pilot release. These are proposed scope for review, not approved release commitments or evidence of implementation. This proposal remains authoritative for requirements and decisions; metric definitions, supported fact mappings, period rules and release scope still require approval.

- Windows x64 first; retain cross-platform boundaries without claiming other OS support.
- A small, explicitly bounded universe of US domestic non-financial companies using annual US-GAAP filing facts.
- Search/select companies, local watchlist, saved screening presets, cancellable refresh, cached results, basic screening controls (threshold rules, ranking, sorting), and source-linked company detail.
- Three candidate metrics defined below. Their inclusion is not yet approved.
- Local single-profile desktop use, without cloud accounts, shared portfolios, billing, or a hosted API.
- SEC EDGAR is the only data source in v1 (AQ-D009); no provider integration, prices or market data.

Candidates not yet placed in a release: per-company notes and recorded assumptions, and export of summaries to spreadsheet or PDF. Decide their release at scope review.

Out of v1: valuation analysis (DCF builder, peer valuation comparison, valuation multiples, terminal-value inputs, sensitivity analysis and base/bull/bear scenarios), price-dependent analytics, broad exchange coverage, real-time prices, price targets, backtesting, trading, recommendations, LLM analysis, bank/broker integrations, multi-user cloud collaboration, parameter simulations, and arbitrary custom formula execution. Valuation is a planned later phase (section 11).

## 5 Candidate financial analyses and correctness rules

Validate these with prospective users before implementing them. They are examples that avoid requiring live price data.

| Candidate | Definition | Comparability and missing-data rules |
|---|---|---|
| Annual revenue growth | (Revenue in year t / revenue in year t-1 - 1) × 100 | Require comparable annual periods and currency; prior revenue must be positive. Missing/zero prior values yield unavailable with a reason. |
| Annual net profit margin | Consolidated net income / revenue × 100 | Use compatible annual scope and periods; revenue must be positive. Distinguish consolidated profit from profit attributable to a different ownership scope. |
| Current ratio | Current assets / current liabilities | Same balance-sheet date, currency and scope; liabilities must be positive. Exclude issuers without comparable classified balance sheets. |

Each metric must have a versioned definition, supported fact mappings, units, period selection rule, exceptional cases, and reviewed example fixtures. Record which taxonomy tags are accepted; do not select any tag containing a familiar word or blindly sum alternatives. Treat unsupported industry/accounting structures as unsupported rather than forcing a value.

### Test definitions in the SEC search spike (version test-0, not approved; AQ-D010)

The spike implements these rules only to exercise the pipeline; they are open to review and must not be read as approved metric definitions.

- Annual facts come from forms 10-K and 10-K/A, in USD. Duration facts must span 350 to 380 days; periods are chosen from reported start and end dates, never from the `fy`/`fp` fields, which describe the filing.
- Per period, the most recently filed fact wins. If other filings report a different value, a restatement warning names the accession used.
- Latest period is the revenue period with the greatest end date; the prior period ends 350 to 380 days earlier. Balance-sheet facts must be at the latest period end; net income must cover exactly the revenue period.
- Tags are tried in order, never summed: revenue `RevenueFromContractWithCustomerExcludingAssessedTax`, `Revenues`, `SalesRevenueNet`; net income `ProfitLoss` (consolidated), falling back to `NetIncomeLoss` (attributable to parent) with a warning; `AssetsCurrent`; `LiabilitiesCurrent`.
- Formulas follow the table above. Non-USD, missing annual revenue, zero or negative denominators and missing inputs give Unsupported or Unavailable with a reason; a missing value is never zero.

### Later valuation phase (candidate, not approved, not in v1)

Intended capabilities, to be specified separately before any implementation: a simple DCF with adjustable assumptions, peer valuation comparison, valuation-multiple analysis, terminal-value inputs, sensitivity analysis and base/bull/bear/conservative scenarios. Prerequisites to resolve first:

- Reviewed definitions: free cash flow, projection horizon, discount rate, terminal growth or exit multiple, net debt, share count and treatment of dilution.
- Additional fact mappings, for example operating cash flow, capital expenditure, debt, cash and shares outstanding, each with units, period rules and exceptional cases.
- A market-price and peer-data source, which depends on the provider decision (AQ-D008). Price-dependent results must record price source, date and licence terms.
- Presentation rules: outputs are assumption-driven illustrative values, not price targets or recommendations. Show the assumptions, scenario and inputs beside every result.
- Reviewed example models with independently checked results, and negative tests for invalid or inconsistent assumptions.

Use C# decimal for financial values within its validated range; parse provider values without a floating-point round trip. Persist decimal values losslessly, for example as invariant strings with explicit units. Round for display only. Ratio outputs are deterministic functions of versioned inputs. Test zero/negative denominators, absent facts, overflow, restatements and inconsistent currency.

Show reporting start/end dates, filing date, form, units, accession/source URL, retrieval time, metric-definition version and quality warnings. A filing amendment or restatement must not silently replace historical evidence. Define an explicit latest-available-as-of policy. Never describe current snapshots as suitable for unbiased historical backtesting.

## 6 Technology stack and rationale

| Concern | Proposed choice | Reason and boundary |
|---|---|---|
| Language/runtime | C# on .NET 10 LTS, current servicing patch | Strong typing, async I/O, decimal arithmetic, debugging and a supported runtime. Pin the SDK in global.json when code is scaffolded. |
| Desktop UI | Avalonia, compatible supported stable version | XAML desktop UI with a path to other platforms. Verify package/runtime compatibility in the first spike; do not select a prerelease by default. |
| Presentation pattern | MVVM with CommunityToolkit.Mvvm | Keep views and business logic separate with limited boilerplate; avoid creating layers for every trivial action. |
| HTTP and JSON | HttpClient and System.Text.Json | Built-in facilities; typed provider DTOs, explicit timeouts, cancellation and validation. |
| Persistence | SQLite through Microsoft.Data.Sqlite | Local transactions, watchlists, provenance and cache metadata without running a database server. Versioned SQL migrations. |
| Testing | xUnit, .NET test tooling, suitable Avalonia headless tests | Pure calculation tests, provider fixtures, storage integration and view-model coverage. A real Windows installer smoke test remains necessary. |
| Logging | Microsoft.Extensions.Logging with a bounded local sink | Structured diagnostics and correlation IDs; exclude tokens and sensitive user details. Add a third-party sink only if needed. |
| CI/releases | GitHub Actions as implementation begins | Reproducible restore/build/test/package; release artifacts tied to commits, changelog and checksums. |

Do not add Python, a web backend, containers, Redis, queues, or microservices without a concrete requirement. Do not claim reduced bundle size from the language choice alone: measure self-contained publish output and installed footprint.

## 7 Architecture and repository structure

```mermaid
flowchart TD
    UI["Avalonia Views and ViewModels"] --> CORE["Application workflows and financial calculations"]
    CORE --> PROVIDER["Provider adapter"]
    CORE --> STORE["SQLite storage and snapshot cache"]
    PROVIDER -->|HTTPS| SEC["SEC filings and structured facts"]
    PROVIDER -.->|"HTTPS, proposed"| EXT["External metrics and market data provider (to be selected)"]
```

The dashed provider is an approved direction (AQ-D008) with no selected vendor and no implementation. Core consumes provider data only through the adapter contract, so sources stay replaceable.


Prefer three production projects and one test project initially:

```text
src/
  AlphaQuant.Desktop/       Views, ViewModels, composition, platform integration
  AlphaQuant.Core/          Models, pure metrics, screening and workflow contracts
  AlphaQuant.Infrastructure/ SEC client, parsing, SQLite and migrations
tests/
  AlphaQuant.Tests/         Unit, provider, persistence and view-model tests
    Fixtures/              Small source-attributed synthetic/redacted test inputs
docs/                      Product architecture and release documentation as needed
AlphaQuant_Proposal.md
README.md
```

This is a target layout. As of 2026-10-26 the Desktop, Core, Infrastructure and Tests projects exist as an SEC search spike; SQLite persistence, migrations and a `docs/` folder do not. Core must not depend on Avalonia or SQLite. Infrastructure implements the narrow contracts the workflows actually need. Desktop composes the application. Add projects only when independent responsibilities justify them; no generic repository framework or dependency injection container is required for its own sake.

All network and storage work must be asynchronous/cancellable or appropriately scheduled off the UI thread. Use bounded concurrency and progress reporting; a refresh must not freeze the interface. Avoid Task.Run around naturally asynchronous HTTP. Prefer a single coordinated local write path to prevent unnecessary SQLite contention.

## 8 Data ingestion and provenance

Use official SEC submissions and company-facts endpoints first. SEC JSON APIs require no API key, but identify the application and contact appropriately and comply with current automated-access policies. Confirm current rate limits before enabling bulk ingestion; use a conservative configurable limiter, Retry-After-aware backoff, and bounded retries. Never retry indefinitely or fan out requests per UI component.

Data provider direction (AQ-D008, approved direction; vendor open; not used in v1 per AQ-D009): AlphaQuant is expected to draw on external data providers for financial metrics and market data as well as filing documents such as SEC filings. SEC remains the first, authoritative source for filing facts. Provider data must carry the same lineage as SEC data: source, retrieval time, period, unit and licence-relevant metadata, and must stay separate from raw filing evidence so differences between sources are visible. Before selecting a provider, review coverage, point-in-time and restatement handling, pricing, rate limits, and terms on caching, local storage, export and redistribution. Prefer user-supplied credentials or a compliant server-side service; a developer-owned key must never ship in an installer. Provider selection and any server-side component are separate decisions that need their own scope review, since they affect the local-only operating model.

Alternatives reviewed on 2026-10-26 and rejected as data sources for the product: Yahoo Finance (no official API since 2017; yfinance uses unofficial endpoints and describes the data as personal use only) and Koyfin (an end-user tool; its FAQ says no data is available via API). Open-source libraries and MCP servers do not change the licence of the underlying data, and MCP servers serve LLM agents, not this app's calculation layer.

SEC company facts do not cover every custom taxonomy concept or make all company facts comparable. Build reviewed mappings and show unsupported results explicitly. Company identity uses CIK and source metadata rather than ticker alone. Fiscal years may differ, and reported durations must drive annual selection rather than calendar labels alone.

```mermaid
flowchart LR
    A["Fetch filing facts"] --> B["Retain source evidence"]
    B --> C["Validate periods and units"]
    C --> D["Calculate versioned metrics"]
    D --> E["Filter and inspect source-linked results"]
```

Suggested ingestion pipeline:

1. Resolve an approved company identifier and required filing metadata.
2. Reuse an eligible cached response, or fetch with bounded timeout and cancellation.
3. Store a raw response snapshot or content-addressed cached file with retrieval metadata.
4. Validate DTO shape, normalize selected facts, and attach accession, periods, unit and source lineage.
5. Calculate only metrics with valid comparable inputs.
6. Commit a consistent snapshot and surface freshness/quality to the UI.

Store raw caches in OS application-data directories, outside Git. Record content hashes, schema/parser version and a retention policy. Cache failures must not corrupt previous usable snapshots. Stale values remain labelled stale; missing values never become zero. A cancelled refresh leaves a recoverable consistent state.

For large universes, revisit SEC bulk data or a centralized compliant ingestion service. Independent desktop clients do not coordinate provider-wide limits. Review provider terms and sustainable access before scaling distribution. A future commercial provider key owned by the developers must never be embedded in an installer; use approved user-supplied credentials or a server-side service if needed.

## 9 Local data model and resilience

Suggested records: companies, filings, source_snapshots, normalized_facts, metric_results, watchlists, screening_presets, schema_versions and refresh_runs. Implement only records needed for the selected vertical slice.

- Facts record entity, concept, value, unit, period, filing/accession, source and normalization version.
- Results reference their exact inputs and metric version; invalid results include a structured reason.
- Refresh runs track start/end/status and sanitized diagnostics.
- Watchlists/presets store user choices separately from reproducible source caches. The SEC contact email is the first user setting: a small versioned `settings.json` in the local app-data folder, separate from caches (AQ-D011).

Use parameterized SQL, foreign keys and transactions. Introduce migrations before user data exists. Back up user-owned settings before risky upgrades; raw provider caches can be rebuilt. Prefer additive migrations. Older binaries must detect an incompatible schema rather than corrupting it. Test interrupted writes, unavailable network, corrupt cache entries and migration failure.

The desktop database is local storage, not a security boundary against the device owner. Store future provider tokens in the OS credential store, not plain SQLite or application configuration. No telemetry containing financial selections or identifiers should be enabled without an explicit privacy decision.

## 10 User interface and accessibility

Proposed primary screens: screener grid, company detail with evidence, watchlist, refresh status and settings/about. Keep a narrow navigation flow. The grid should allow sorting/filtering selected metrics and distinguish missing, unsupported, stale and valid values.

Company detail must explain the formula and open the source filing. Users should see data periods before interpreting metrics. Preserve screen state during refresh. Offer useful error messages, retry/cancel, empty/loading states and no unexplained spinner. Support keyboard use, readable contrast and scaling; do not communicate data quality through color alone.

Measure responsiveness and footprint on a named reference machine and dataset before claiming performance. Proposed v1 target: cached operations feel immediate on a 100-company dataset, with long refreshes running in the background. Set numerical acceptance budgets after the first measured spike rather than inventing benchmarks.

## 11 Release plan

Release contents become binding only when their scope is approved. Do not implement the whole roadmap in one change.

| Release | Intended outcome | Exit criteria |
|---|---|---|
| 0.1 foundation | Runnable UI shell and one real company data path | A clean checkout can restore/build/run; CI and a fixture-backed calculation test pass; source evidence visible. |
| 0.2 alpha | Small company universe and candidate metrics | Approved definitions/mappings, filters, cache, cancellation, migrations and meaningful negative tests work. |
| 0.3 beta | Installable pilot with a complete workflow | Clean-machine install/upgrade smoke test, readable UI, documentation, bounded diagnostics, known limitations and feedback channel. |
| 1.0 | Basic screener and a few approved analyses for real users | Release gate below passes; release review completed; pilot issues triaged and no known critical correctness/security defect. |
| Valuation phase (candidate, after 1.0) | DCF, peer valuation, multiples, scenarios and sensitivity analysis (AQ-D007) | Separate approved scope and definitions (section 5), provider decision (AQ-D008), reviewed example models, assumption display on every result, and no price-target or recommendation framing. |
| 1.1 onward | Improvements justified by usage | Each feature has scope, tests, migration/compatibility assessment and release notes. |
| Later exploration | Parameter simulations or broader data coverage | Separate domain requirements, model assumptions and validation. No claim that changing inputs predicts market price. |

```mermaid
flowchart LR
    A["Define scope"] --> B["Implement and test"]
    B --> C["CI and installer verification"]
    C --> D["Reviewed release"]
    D --> E["Pilot feedback"]
    E --> A
```

Use descriptive feature branches and cohesive commits, with peer-reviewed pull requests. Record why a tradeoff was chosen. Tag actual releases; semantic versioning should reflect documented compatibility, including data schema, exports and settings. A major version is not required for every UI change.

## 12 Verification and release gate

Every iteration must update affected tests and documentation. Use meaningful tests rather than assertions that simply mirror implementation.

- Unit: independently checked metric fixtures, decimal handling, denominators, annual periods, restatements, taxonomy alternatives, missing and unsupported inputs.
- Provider contracts: saved fixtures for normal, amended, malformed and missing facts. Test rate limiting, 429/5xx responses, backoff and cancellation using a fake handler; do not make live SEC access a required PR test.
- Integration: temporary SQLite files, migrations from supported previous versions, transaction rollback, persistence round trips and cache corruption recovery.
- UI/view models: filtering, stale/quality states, refresh progress, cancellation and error recovery. Supplement headless checks with actual desktop interaction.
- Distribution: clean supported Windows environment; install, first launch, offline cached use, upgrade preserving watchlists, and uninstall with an explicit data-retention choice.

The 1.0 gate requires reviewed formulas and source traceability, passing required CI, no known critical security/correctness issues, no secrets in tracked files, successful installer/upgrade tests, recovery instructions, README matching behavior and clearly documented scope limitations. Coverage percentages alone are not a release gate.

## 13 Distribution operations and security

The initial deployment is a desktop installer, not an AWS web server. No cloud hosting is assumed. A static download page or GitHub Releases may distribute installers when a remote, budget and visibility are agreed.

Start with manual release approval and a CI-built artifact; validate signing and Windows trust behavior before broad distribution. Signing credentials belong in protected release secrets, never in the repository. Publish checksums and release notes. Automatic updates are a later task until authenticity verification, interrupted-update recovery and package compatibility are tested. Do not claim the application is signed until it is verified.

CI should pin the .NET SDK and dependencies, restore deterministically, check formatting/build warnings, run tests and audit packages for known vulnerabilities. Confirm exact CLI commands against the pinned SDK when implementation begins. Separate untrusted PR execution from secrets-bearing release jobs and restrict release permissions. Do not run privileged pipelines for unreviewed code.

Use HTTPS and certificate validation. Do not render remote executable content or execute arbitrary user formulas in v1. Limit download size and parsing resources. Validate external URLs before opening them. Logs should rotate and redact secrets. Provide an opt-in sanitized diagnostics export; crash reporting and usage telemetry remain off until reviewed.

Record incidents with release version, impact, cause, fix and verification. A desktop rollback may not be safe after a schema migration; preserve settings backups and document supported recovery paths. Keep infrastructure proportionate to the desktop deployment model.

## 14 Documentation and product validation

Maintain an accurate README, architecture diagram, setup/run/test instructions, known limitations, changelog and release notes. Use source-attributed synthetic or public-data examples in demonstrations and verify redistribution conditions.

Measure workflow completion, repeat usage where users consent, feedback resolved and release outcomes. Publish only measured adoption, performance and reliability claims. Use small reviewable changes and record significant technical tradeoffs.

## 15 Risks and open decisions

| Risk or decision | Required action |
|---|---|
| Metrics and first workflow not selected | Interview users; approve exact v1 metrics, scope and input rules before implementing them. |
| Market/data coverage unclear | Confirm proposed US domestic non-financial annual scope and define exclusions. |
| OS and resources | Confirm Windows x64 target, reference hardware and installer expectations; other OS releases need their own QA. |
| Package/runtime compatibility | Verify stable Avalonia/.NET/toolkit combination with a minimal runnable spike. |
| Data access/redistribution | Review current SEC policies and any additional provider terms before ingestion at scale or publication. |
| Budget, licensing and public release | Agree signing costs, distribution channel, repository visibility, project license and dependency obligations before publication. |
| Wrong or incomparable financial data | Version mappings, preserve provenance, show warnings and test reviewed examples; restrict coverage when necessary. |
| Delivery and maintainability | Work in small releases; no calendar delivery promise until a vertical slice is measured. |
| Name availability | AlphaQuant is a working name; check naming/trademark availability before public branding. |
| Data provider not selected | Define required metrics and market data, compare vendors on coverage, cost, point-in-time handling and terms on caching, storage and redistribution; decide credential model and whether any server-side service is needed. Candidates mentioned so far, none evaluated against their actual terms: Financial Modeling Prep, EODHD, Intrinio, Alpha Vantage, LLMQuant Data (pricing and terms unconfirmed). |
| Independent finance and legal review (proposed) | Before metric definitions are approved or a pilot starts, have a qualified finance reviewer check definitions, mappings and reviewed fixtures, and review disclaimers and data-licence obligations. Agent-written checks do not replace this. Not yet agreed. |
| Valuation framing and liability | Present outputs as assumption-driven illustrations, not price targets or advice. Review disclaimers and any regulatory implications for advisory or wealth-management users before offering the valuation phase. |
| Unvalidated value claims | Treat time savings, lighter weight and differentiation as hypotheses until pilot interviews and benchmarks support them. |
| Scope growth into valuation | Keep v1 to screening; admit valuation features only through a separate approved scope. |

## 16 Product record scope

This proposal records software requirements, product ideas and decisions, architecture, financial definitions, implementation evidence and release status. The current state and dated records below distinguish planned capabilities from implemented and verified behavior.

## 17 Current state and next action

Documentation update, 2026-10-28: revision 0.8 adds a reference to `User_Stories.md` (US-01 through US-13), copied unchanged from the supplied stories on branch `docs/initial-user-stories`. The stories are proposed scope; no financial definition or release commitment was approved and no application behavior changed. Content was verified against the supplied text; build and tests were not rerun for this documentation-only update. The local PDF and editable source were not found in this checkout, so PDF synchronization remains pending.

As of 2026-10-26: `main` holds this proposal, README, .gitignore and a placeholder desktop app (Avalonia 12.1.4 on .NET 10, SDK 10.0.403 pinned in `global.json`, solution `AlphaQuant.slnx`) with Home and Release Notes pages. The SEC search spike below was merged into `main` from branch `feat/sec-company-search` on 2026-10-26. The public source repository is https://github.com/minwaiphyo/AlphaQuant. No application deployment exists. The .NET SDK was installed per-user on the development machine, since it was missing.

SEC search spike (branch `feat/sec-company-search`): `src/AlphaQuant.Core` (models, ticker/name search, annual-fact selection, test metrics, filing links; no external dependencies), `src/AlphaQuant.Infrastructure` (SEC client with declared User-Agent, request pacing and bounded retries, parser, plain-file cache under `%LOCALAPPDATA%\AlphaQuant\cache\sec`) and `tests/AlphaQuant.Tests` (xUnit, saved synthetic fixtures, fake HTTP handler). The desktop app adds a search page and a company detail page showing the latest annual revenue (and prior year), net income, current assets and current liabilities with period, form, filing date, accession, tag and a filing link, plus three **test metrics** (revenue growth, net profit margin, current ratio, definition version `test-0`) and data-quality warnings. The metric definitions are test definitions, not approved v1 metrics (AQ-D010). The SEC contact email is entered on a Settings page and stored in `%LOCALAPPDATA%\AlphaQuant\settings.json`, with the `AQ_SEC_CONTACT` environment variable as a fallback when nothing is saved; it is read on every request, so changes apply without a restart (AQ-D011). No SQLite, watchlist, filters or installer exist.

Verified on 2026-10-26 (Windows 11 x64): `dotnet build AlphaQuant.slnx` succeeded with 0 warnings and 0 errors; `dotnet test AlphaQuant.slnx` passed 128 tests with no live SEC access; with no contact the search page showed the setup message and no cache folder or request was made; through UI Automation, the Settings page rejected an invalid email without creating a file, saved a valid one to a versioned file, cleared it, and "Open Settings" from the search page returned to search. Live SEC check: the user reported on 2026-10-26 that they ran and checked the app against the live SEC; the agent did not observe it and no values were reported, so the comparison of displayed values with the SEC source is not independently evidenced. Not verified: the automatic retry after saving a contact from the setup message (it would need a live request in an automated run). Package audit, CI and a clean-machine run remain untested.

Business direction (provisional, AQ-D006): a transparent screening and valuation workspace. v1 covers screening only; valuation (DCF, scenarios, peers, multiples, sensitivity) is a later phase (AQ-D007). External data providers are planned for later (AQ-D008) but v1 is SEC-only (AQ-D009); no vendor is selected.

Next action: review the test definitions in section 5 with the user to decide which metrics, tag mappings and period rules become approved v1 definitions, and record the outcome as decisions. Optionally record the observed live values for AAPL against the SEC `companyconcept` endpoint as reviewed fixtures.

Pending decisions: exact metrics, fiscal/coverage rules, who provides qualified finance review, release for notes and export, Windows target, budget, license and publication channel. Blockers for public release: all implementation and release gates remain outstanding.

## 18 Decision log

| ID and date | Status | Decision and reason | Source |
|---|---|---|---|
| AQ-D012 2026-10-28 | Proposed | Record initial stories US-01 through US-13 in `User_Stories.md`, covering company selection, retrieval, metrics and provenance, screening, watchlists, offline review, accessibility, installation and feedback. Purpose: provide concrete user workflows and acceptance criteria for scope review. The groupings do not approve release contents or financial definitions. Next validation: review stories against the existing spike and approve metric definitions, mappings, period rules and bounded milestone scope. | User-supplied stories and request to save and reference them. |
| AQ-D001 2026-10-25 | Approved | Use C# desktop option; use a maintainable desktop application stack. Avalonia is the intended UI direction from the accepted option. | Architecture decision. |
| AQ-D002 2026-10-25 | Approved | Develop in tested, documented releases, with basic functionality and a few basic analyses in v1. | Release policy. |
| AQ-D003 2026-10-25 | Approved | Name project/repository AlphaQuant and create it under Projects; maintain a current product and engineering development record. | Project planning decision. |
| AQ-D004 2026-10-25 | Proposed | Windows first, .NET 10, SQLite, toolkit, small annual US filing universe and candidate metrics. | Architecture defaults in this proposal; subject to scope review. |
| AQ-D006 2026-10-26 | Approved, provisional | Use the screening-and-valuation workspace proposal (positioning, audience, principles, differentiators as hypotheses, business-model options as undecided) as the working business proposal for now. Reason: it frames a practical, transparent analyst tool without prediction claims. Refine after user validation. | User review of the proposed business direction. |
| AQ-D007 2026-10-26 | Approved | Valuation analysis (DCF, scenarios, peer valuation, multiples, sensitivity) is a later phase after 1.0, not v1. Reason: it needs additional fact mappings, price and peer data, and reviewed definitions; keeping v1 to screening bounds the first release. Specifics are candidates only. | User decision. |
| AQ-D008 2026-10-26 | Approved direction; details open | Use external data providers for financial metrics and market data as well as filing documents such as SEC filings. Reason: price-dependent and peer analyses cannot rely on SEC filings alone. Vendor, terms, credential model and any server-side component remain undecided. | User decision. |
| AQ-D009 2026-10-26 | Approved | Version 1 is SEC-only: filing facts come from SEC EDGAR APIs (company tickers, submissions, company facts) and no commercial provider is integrated. Reason: the three candidate metrics need only filing facts; SEC data is authoritative and free of licence fees; providers add licensing risk and cost, and are needed only for prices, peers and multiples. Provider selection waits for the valuation phase. Refines AQ-D008. | User decision after provider options review. |
| AQ-D010 2026-10-26 | Proposed | The SEC search spike uses test metric definitions (`test-0`, section 5) so the pipeline can be exercised end to end: latest annual revenue, net income, current assets and liabilities, plus revenue growth, net margin and current ratio. Also chosen with the user for the spike: contact from the `AQ_SEC_CONTACT` environment variable (the in-app entry in AQ-D011 now takes priority), plain-file cache (no SQLite), search over the whole SEC ticker list with unsupported companies flagged. The definitions are not approved v1 metrics and need review. | Planning session for the SEC search milestone. |
| AQ-D011 2026-10-26 | Approved (user request); details proposed | The SEC contact email is entered on an in-app Settings page and stored in a small versioned `settings.json` in the local app-data folder, so an installed user never has to set an environment variable. A valid saved contact wins; `AQ_SEC_CONTACT` is the fallback. The contact is read on every request so changes apply at once. Reason: installed users cannot be expected to edit environment variables. The setting is user-owned data kept separate from rebuildable caches. Refines AQ-D010. | User request after the environment-variable setup. |

Decision records include status, rationale, affected software scope and references to superseded decisions.

## 19 Work log and release record

| Date | Work and verification | Remaining |
|---|---|---|
| 2026-10-28 | Documentation on branch `docs/initial-user-stories`: added `User_Stories.md` with the supplied 13 stories unchanged; verified an exact content match. Proposal revision 0.8 references the stories as proposed scope (AQ-D012); updated the shared handoff. No code changed; no build or tests run. Local PDF and editable source not found in this checkout; no PDF regenerated. | Review stories and approve metric definitions, mappings and period rules; synchronize the local PDF when its source is available. |
| 2026-10-26 | Handoff and research record. Reviewed data provider options (Yahoo Finance and Koyfin rejected; candidates listed in section 15) and a third-party agent-skill catalogue (LLMQuant/skills: not adopted, see AGENTS.md AQ-W004). Discussed skills useful for an enterprise-grade version; the user deferred writing any. Refreshed the handoff and revision 0.7. Documentation only; no code changed and no checks run for this entry. | Review test definitions; approve v1 metrics; arrange qualified finance review; CI. |
| 2026-10-26 | Integration: the user reported running and checking the app against the live SEC; the agent did not observe the run and no values were reported. Branch `feat/sec-company-search` committed, pushed and fast-forward merged into `main`. Build and 128 tests were last run before this documentation-only change. | Review test definitions; approve v1 metrics; CI. |
| 2026-10-26 | Settings page for the SEC contact on branch `feat/sec-company-search` (AQ-D011). `ContactSettings` stores the contact in `%LOCALAPPDATA%\AlphaQuant\settings.json` (atomic write, version 1, unreadable or incompatible files treated as nothing saved), falls back to `AQ_SEC_CONTACT`, validates the email and reports failed writes. `SecClient` now takes a contact provider read on every request. Checks run: `dotnet build AlphaQuant.slnx` (0 warnings, 0 errors); `dotnet test AlphaQuant.slnx` (128 passed, 0 failed, including new tests for settings precedence, clearing, invalid input, corrupt or future-version files, failed writes and a contact changed at runtime); UI Automation on the Settings page (invalid email rejected with no file, valid email saved to the versioned file, cleared, and Open Settings from search returned to search; the test settings file was removed afterwards). Not run: live SEC request, retry-after-save with a real contact, package audit, CI. | Live SEC check by the user, merge, review of test definitions. |
| 2026-10-26 | SEC company search spike on branch `feat/sec-company-search` after a planning session (AQ-D010). Added Core, Infrastructure and Tests projects; a search page and company detail page. Checks run: `dotnet build AlphaQuant.slnx` (0 warnings, 0 errors); `dotnet test AlphaQuant.slnx` (107 passed, 0 failed; fake HTTP handler and synthetic fixtures, no live SEC); UI Automation run with `AQ_SEC_CONTACT` unset showing the setup message with no cache folder created and Back returning Home. An early test run exposed a User-Agent validation bug (the header value was rejected, which would have blocked every real request); fixed and covered by tests. Not run: any live SEC request, comparison of shown values with SEC data, package audit, CI. | Live SEC check, merge, review of test definitions. |
| 2026-10-26 | Provider options reviewed and v1 set to SEC-only (AQ-D009). Read-only check of SEC documentation: `data.sec.gov` offers submissions, company facts, company concept and frames JSON (no key, no CORS), plus nightly bulk ZIPs; `company_tickers.json` maps ticker, name and CIK; fair-access limit is 10 requests per second across all of a user's machines; no User-Agent format was found on the pages read. Search and metrics milestone was requested but held at the planning gate. Documentation only; nothing implemented or run against SEC. | Planning session and approved scope for the SEC search and metrics milestone. |
| 2026-10-26 | Basic navigation on branch `feat/avalonia-hello-window`. `MainWindow` hosts a `ContentControl` bound to `MainWindowViewModel.CurrentPage`; view-model-to-view data templates select `HomeView` (message and "View Release Notes" button) or `ReleaseNotesView` (static placeholder entry and Back button). Added CommunityToolkit.Mvvm 8.4.2 and enabled compiled bindings. Release notes are hard-coded placeholders, not generated from a changelog. Checks run: `dotnet build AlphaQuant.slnx` (0 warnings, 0 errors); launched the app and, through Windows UI Automation, invoked "View Release Notes" (Release Notes page content appeared) and "Back" (Home content returned). Not run: automated tests (none exist), visual inspection of layout, package audit. | Unit tests for navigation view models, a real release-notes source (changelog) once releases exist. |
| 2026-10-26 | Placeholder window on branch `feat/avalonia-hello-window`: Avalonia 12.1.4 template app targeting net10.0, `global.json` pinning SDK 10.0.403, `AlphaQuant.slnx`, a centered static message. Installed SDK 10.0.403 per-user. Checks run: `dotnet build AlphaQuant.slnx` (0 warnings, 0 errors); launched the built app and confirmed a responding Avalonia window titled "AlphaQuant". A first launch failed with a runtime-not-found dialog until `DOTNET_ROOT` was set, which is recorded in AGENTS.md. Not run: tests (none exist), package vulnerability audit, CI, clean-machine run. | Core/Infrastructure/test projects, CI, package audit and first approved milestone. |
| 2026-10-26 | Planning session. Adopted the screening-and-valuation business direction provisionally, deferred valuation to a post-1.0 phase, and recorded the external data-provider direction (AQ-D006 to AQ-D008). Updated revision 0.6 sections 1 to 5, 7, 8, 11, 15, 17 and 18 and the README. Documentation only; no application verification claimed. Checked that `dotnet` is not on PATH. Work on branch `docs/business-direction-valuation-staging`. | Choose first metric, universe and v1 provider usage; select a provider; verify .NET/Avalonia; first milestone. |
| 2026-10-25 | Maintained the architecture, ingestion and release documentation. Documentation only. | Application implementation and release gates remain outstanding. |
| 2026-10-25 | Created planning repository, proposal, README and ignore rules. Reviewed official .NET, Avalonia, MVVM and SEC references. Documentation-only work; no application verification claimed. | Scope review, implementation spike and all release gates. |

Engineering records include changed software behavior, relevant files and commits, verification evidence, limitations and remaining work.

No product releases exist yet. Future release records will identify the version, source commit, supported OS, changes, metric and schema versions, verification, known issues, artifacts, signing status and recovery notes.

## 20 Reference documentation

- [Avalonia](https://docs.avaloniaui.net/docs/welcome)
- [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
- [CommunityToolkit MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [SEC EDGAR APIs](https://www.sec.gov/search-filings/edgar-application-programming-interfaces)

References checked 2026-10-25. Recheck versions, provider policies and distribution requirements before implementation or release.
