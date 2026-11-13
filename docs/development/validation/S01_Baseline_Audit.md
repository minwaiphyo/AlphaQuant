# S01 baseline audit

Date: 2026-11-13. Starting commit: 36f4a8a. Scope: S01.1-S01.4. Skill: alphaquant-stage-verification, library 1.0.

## Environment and reproducible commands

System `dotnet` resolves to SDK 10.0.403, matching global.json. Existing local SDK remains optional. `dotnet build AlphaQuant.slnx -c Release --no-restore --verbosity minimal` succeeded with zero warnings/errors on this laptop. Restore/test used the ignored local NuGet/CLI directories.

`dotnet test AlphaQuant.slnx -c Release --verbosity minimal` built the test projects but Windows Application Control rejected project DLL loads with FileLoadException 0x800711C7. Those failures are execution-environment failures, not evidence that financial assertions passed or failed. Do not repeatedly rerun the same blocked binaries or weaken Windows security. Clean GitHub Windows CI is the executable validation route for this stage. Record its exact PR-head result in the ledger.

No saved `secContact` or process `AQ_SEC_CONTACT` candidate was found. No value was printed, manufactured or sent. This blocks configured live SEC reconciliation (E01), not synthetic work or UI integration. OAuth host/account, distribution and historical rights remain unverified external gates.

## Current data flow

Desktop App composes shared paced SecClient, local SEC cache/contact settings, ComparisonBackend and ResearchBackend. Search/ComparisonViewModel currently use annual CompanyReport/test-0. The separate FinancialAnalysisViewModel consumes ComparisonSnapshot/comparison-1 through IResearchBackend. MarketConnectionViewModel owns browser-connect/test/disconnect interaction. The Core comparison service loads financials and consumes a provider stream, while the provider/broker own token/socket/HTTP details.

The duplicate research paths make comparison filters available only for legacy metrics and make navigation/definition context inconsistent. S03 owns consolidation rather than altering test-0's financial meaning during S01.

## Defects and disposition

| ID | Severity | Reproduction / consequence | Owning work |
|---|---|---|---|
| BA-01 | High | A provider yields connected quotes then returns normally. The channel completes while the final snapshot still says Connected and may retain available P/E. A completed producer is not an active stream | Fixed in S01 with a final Disconnected event that invalidates valuation and preserves dated quote inspection. Explicit Failed/Disconnected terminal events keep their reasons |
| BA-02 | High boundary mismatch | SecClient documents SEC-only HTTPS requests, but its default handler follows redirects automatically. Initial URI validation alone does not enforce the documented destination boundary | Fixed in S01 by disabling automatic redirects and returning safe InvalidData on 3xx. Fake-handler test covers response handling, source review verifies factory configuration |
| BA-03 | High financial ambiguity | TTM YTD fallback selects the first latest prior FY by end date even if multiple annual start dates exist. Annual-history/full-year TTM paths reject that ambiguity, but the bridge path does not | S02 regression/spec review and correction before claiming source validity |
| BA-04 | Medium UX/accuracy | Search/legacy comparison and Financial analysis are separate paths, with filters tied to test-0. Financial evidence converts EPS source values through whole-number formatting | S03 unified comparison/evidence presentation |
| BA-05 | Medium lifecycle/focus | Financial-analysis refresh rebuilds company controls on every snapshot, including freshness ticks. Origin controls may detach while evidence is open. Rapid watch replacement can overlap disposal | S03 stable presentation/lifetime review and relevant regressions |
| BA-06 | Medium setup state | Financial source errors lose typed setup context, and the screen can say fundamentals loaded when all issuer loads failed | S03 explicit actionable failed/setup state while retaining prior results |

No unused package or stack replacement is needed to address these baseline findings. Financial method changes stay in S02's versioned definition work. SQLite/persistence is intentionally untouched until S04.

## Validation inventory and effort refinement

The baseline has deterministic tests for SEC parsing/cache/contact/pacing, legacy metrics, comparison annual/TTM/valuation, OAuth/broker/socket boundaries, UI view-model lifecycle and reviewed inputs. Skills have their own 19 numeric-reference checks plus resource and installer checks. These are distinct test layers, not an arbitrary target count.

S01 adds regressions for normal producer completion/explicit terminal failure and SEC redirect handling. S02 needs the ambiguous-FY bridge fixture, independent worked cases and a reproducible capture/reconciliation workflow. Its live package is blocked until E01 is configured. S03 can progress from current typed snapshot contracts while live reconciliation is pending.

Initial ranges remain conservative: S02's deterministic review/capture tooling is ready to execute, but no live completion date can be promised without contact configuration. S03 should prioritize reusing comparison-1 presentation/filter contracts and preserving lifecycle behavior rather than a visual redesign. The overall roadmap estimate is not a measured schedule.

## Completion evidence

Local compile is available. Local runtime verification is blocked by an actual OS policy, with clean remote CI as the supported alternative. S01 delivery requires the exact-head remote checks to pass, documentation/ledger delivery and normal merge. No live provider, independent finance certification or trusted distribution is implied by this audit.
