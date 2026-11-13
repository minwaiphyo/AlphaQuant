---
name: alphaquant-avalonia-design
description: Build or review AlphaQuant Avalonia research screens using the existing design tokens, MVVM contracts, financial data states, keyboard behavior and responsive layouts. Use for Desktop interaction/layout changes, not standalone financial arithmetic or website redesigns.
---

# Financial research interface and design system

Read `src/AlphaQuant.Desktop/Styles/ResearchTheme.axaml`, the changed views/view models and current UI brief. The theme file is the source of truth for actual tokens and styles. Read [the design contract](references/design-contract.md) for state/interaction decisions.

Reuse semantic theme resources and current components. Keep the navy navigation, teal research accents and system/light/dark behavior coherent. Do not invent a parallel theme, new frontend stack or unexplained raw colors for ordinary components.

Show financial basis, unit, definition version, source/as-of date and per-cell unavailable reasons where relevant. Current prices, historical facts and user forecasts have different dates and meanings. Preserve selections and usable data through Settings, refresh, cancel and failure. Never label fixtures live or unknown market sessions closed.

Use MVVM commands/state and typed service outputs. Keep SEC mapping, TTM, valuation arithmetic, tokens and sockets out of views. Test late-response/navigation behavior in view-model/service tests, and inspect rendered layouts with isolated synthetic services when layout changes warrant it.

Before claiming completion, check changed states with keyboard and the smallest claimed layout/scaling, plus relevant light/dark appearance. Record what was actually inspected and any tool limitation. A compile pass does not establish readable layout or correct focus.
