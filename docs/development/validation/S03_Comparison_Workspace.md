# S03 comparison workspace validation

Date: 2026-11-13. Base: S02 delivery f3a5149. Skills: stage verification, Avalonia design and data connections, library 1.0.

## Contract and implementation

The production Compare action opens one comparison-1.1 workspace. Search retains two-to-five-peer selection. Edit companies returns to Search, and Data connections returns to the initiating comparison with its numeric filter and selection retained. The legacy test-0 view model remains as a selection adapter and isolated annual filing/reference path, not the production comparison destination. No legacy net margin/assets cells were renamed to operating margin/cash.

Each comparison cell carries name/unit, annual/TTM/instant basis and dates, raw numeric value or unavailable status/reason, and an evidence command bound to its snapshot. Evidence includes current definition version, exact unrounded source values/contributions, filing links, retrieval and capture dates and review limitations. Current valuation keeps separate quote/feed/time/connection evidence. Five annual periods remain expandable when supplied.

One active numeric filter selects a named approved metric and an invariant decimal threshold, without grouping separators. Annual filtering uses the latest annual metric, not a historical matching value. Units are the displayed cell units (USD, percent points, USD/shares or multiples). Invalid input leaves the existing filter intact. Missing values cannot satisfy a numeric comparison. Sorting uses unrounded decimals and places missing values last in both directions, with stable name ties. Clearing the filter restores all selected companies.

Presentation objects are reused for the same company/metric/period through quote ticks, preserving evidence-origin controls. Commands receive the new snapshot. Removed peers disappear immediately. Stop invalidates valuation while retaining inspectable financials. Refresh waits for the previous enumerator to finish/dispose before opening another. A bounded wait timeout does not erase the still-pending close, so another refresh cannot bypass it. Typed contact failures remain typed through Core and receive actionable setup text. Failed refreshes never claim fundamentals loaded.

## Acceptance evidence

Release solution build on this laptop: zero warnings/errors. Local runtime is blocked by Windows Application Control (BL-002), so assertions/rendering run on clean Windows CI, not by disabling security.

Regression coverage includes raw sub-cent EPS filtering/evidence, missing ordering in both directions, empty/invalid/clear filter, retained financials with invalidated price, typed setup failure, late responses, stable object identity, close-timeout retry serialization and production navigation/Settings context. Existing provider/service tests cover actual adapter lifecycle and no-network unverified access.

`tools/AlphaQuant.UiChecks` uses matching Avalonia.Headless 12.1.4 (MIT, verification-only) with real Skia drawing and the shared theme. It never composes production settings/services or accesses network/browser. It renders five synthetic long-name peers in light/dark, a constrained research viewport, expanded evidence, empty-filter and retained-setup states. Evidence focus and Escape return are checked against actual controls. CI retains separate PNG artifacts for one day. These are not live financial/provider evidence or native screen-reader certification.

Initial Windows CI run 43436890942 passed 216 application tests, 19 independent skill tests, rendering/keyboard checks, build and packaging. Visual inspection of its five PNGs found that fixed filter controls crowded the minimum research/evidence viewport. Filters were moved into a collapsible section in the scrollable research area. A separate expanded-filter render now covers this state. Sub-cent EPS display precision and Core typed-error propagation were also reviewed and strengthened. Final exact-head CI and revised PNG inspection are required before merge. S02 actual source reconciliation remains open on BL-001.

## Dependency evidence

[Avalonia.Headless 12.1.4 package metadata and MIT license](https://www.nuget.org/packages/Avalonia.Headless/12.1.4) were reviewed. Matching the existing production Avalonia version avoids a framework upgrade. This dependency does not ship in the desktop publish output.
