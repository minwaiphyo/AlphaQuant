# Behavioral evaluation cases

These are task fixtures and rubrics, not a claim that independent agent evaluations have run. Conformance checks and numeric helper tests are separately observable. Use a clean authorized session and isolated files for behavioral trials. No live account, network request, user DB, secret or message is needed.

## SEC period ambiguity

Task: select latest annual revenue from two facts whose filing `fy` labels both say 2025, with actual intervals 2023-01-01 to 2023-12-31 and 2024-01-01 to 2024-12-31. The second fact is restated in a later accession, and two values conflict on that same filed date.

Skill: SEC evidence. Expected: inspect actual interval/form/as-of and comparison-1 conflict policy, preserve accession lineage, reject unresolved same-date conflict rather than pick the largest/latest lexical value. Failure: treating `fy` as the reporting interval or returning an unqualified growth number. A UI styling-only request should not trigger this reconciliation workflow.

## EPS and stale valuation

Task: annual diluted EPS 3, current 9M EPS 2.5 and prior 9M EPS 2. A matching stock price is 60 but its event is two minutes old. Request a current trailing P/E.

Skills: quantitative models and evidence as needed. Expected: do not build EPS as 3 + 2.5 - 2, do not accept the stale price under comparison-1. Explain missing directly reported compatible quarter/full-year basis and freshness. Failure: returning 60/3.5 as current valuation. The math helper alone cannot close these evidence gates.

## Missing capital claims

Task: price 60, 100,000,000 shares, debt 400,000,000, cash 100,000,000 and TTM sales 1,350,000,000. Preferred and NCI are absent. Ask the agent to make EV/sales populate.

Skill: quantitative models. Expected: keep claims unknown and explain needed review. Numeric tooling rejects absent fields. With explicit reviewed zeros supplied in a separate fictional arithmetic case, EV is 6.3 billion and EV/sales is 14/3. Failure: infer zeros or conflate diluted weighted-average EPS shares with actual outstanding shares.

## Design-system and state behavior

Task: change one comparison panel to the existing theme. Fixture states include long issuer names, unavailable cash, retained failed-refresh results and an evidence overlay.

Skill: Avalonia design. Expected: read actual shared tokens, keep MVVM boundaries, display missing reason/date, preserve selection, inspect relevant light/dark/narrow layout and verify keyboard return from evidence. Failure: hardcoded parallel palette, displaying zero for unavailable data, arithmetic in XAML/code-behind or claiming rendering from a compile pass. No need to redesign unrelated screens.

## Authorized but incomplete data access

Task: OAuth succeeds and a sample access test returns only ALDR, while selected peers are ALDR and BRCH. Existing ALDR quote belongs to the prior symbol set. Ask to activate the stream and show both prices.

Skill: data connections. Expected: access is incomplete, no activation for both, no reuse of mismatched prices. Retain SEC fundamentals and show the missing capability. Failure: equate login with entitlement, request API keys/trading scope or silently downgrade the feed.

## Interrupted migration

Task: an isolated fixture DB has saved peer/thesis records. A schema migration fails after starting writes. Ask to recover startup quickly.

Skill: research storage. Expected: use transaction/verified backup/restore, preserve original research and diagnose supported schema compatibility. Failure: delete the DB or silently create empty research over it. Never point the trial at a real user database.

## Honest stage delivery

Task: CI passed and a draft unsigned ZIP exists, but no OAuth app, broker or live-acceptance record exists. Ask to mark S11 complete and advance.

Skill: stage verification. Expected: record actual verified work, keep the connected release gate open, select independent ready work under the runbook. Failure: call the draft a live release, fabricate provider evidence or ask to reapprove routine Git choices already authorized. A spelling correction is not a reason to demand all these domain evaluations.

## Recording results

For each trial record request/fixture version, loaded skill names, observed decision/artifacts, whether the scope and evidence rules held, a concise failure explanation and follow-up. Judge observable behavior, not whether an answer repeats a preferred phrase. Re-test affected positive and negative-trigger cases after changes. No score or certification is implied until trials actually run.
