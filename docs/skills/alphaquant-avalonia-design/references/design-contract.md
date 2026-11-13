# AlphaQuant design contract

The current source is `src/AlphaQuant.Desktop/Styles/ResearchTheme.axaml`. Read it when implementing rather than copying an old palette snapshot. Existing semantic resources include AqBackground, AqSurface, AqSubtle, AqText, AqMuted, AqLine, AqAccent, AqTint, AqOnAccent and warning resources. Existing classes include panel, notice, primary, nav, metric, h1, h2, eyebrow, muted, small and warning.

Use those semantic roles and dynamic resources. If a genuinely missing role is needed, extend the shared theme for light/dark together and document its purpose. Do not assume an accent/background combination meets contrast simply because both are existing tokens.

## Components and financial meaning

| Component | Required meaning/behavior |
|---|---|
| Metric | Label, unit and annual/TTM/instant basis. Format at display only |
| Missing metric | Unavailable plus specific reason, never zero or a successful-looking blank |
| Evidence | Formula/version, exact inputs, dates, contribution weights, filing references and review limitations |
| Quote | Provider/feed, eligible-price semantics, event/receive time, age and connection. IEX is limited venue |
| Cached snapshot | Original retrieval/source and stale/retained label, not a newly retrieved value |
| Forecast/scenario | User assumption versus reported history, selected revision and model version |
| Research conclusion | User-authored judgment, visibly separate from calculated outputs |

## State and navigation review

Cover the states the changed interaction can enter: empty, initial loading, usable data, partial missing/unsupported, refreshing retained data, failed/cancelled request, setup required, connected-without-price, stale quote, disconnected and recovery.

Use text/icons alongside color. Keep controls reachable by keyboard, errors actionable and loading cancellable when supported. Opening evidence moves focus predictably, Escape/Close returns it to a valid originating control. After collection rebuilds, do not focus a detached control. Avoid a modal-like overlay that traps or loses keyboard context.

Settings returns to the initiating research context. Leaving a live comparison stops its stream. Late responses cannot repopulate removed peers or overwrite a newer request. Switching provider/symbol set resets price-derived values and never mixes unlabeled feeds.

## Layout verification

Inspect one nominal layout and the smallest supported/claimed size for a material layout change. Check scaling/wrapping, long names, many peers, sparse data, expanded source evidence and error text. Current MainWindow bounds are an implementation fact to inspect, not a promise that every layout already works there.

Use a fixture-only harness with isolated settings and no accidental network requests. Do not read/write real contact settings to obtain screenshots. Use the available computer-use skill when native interaction is needed, following its current instructions. If the tool is unavailable, record that limit and do not claim visual verification.
