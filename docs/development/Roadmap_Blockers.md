# Consolidated roadmap blockers

Checked 2026-11-14. Independent engineering through S20 has been implemented or
prepared as recorded in the stage ledger. This does not complete externally gated
stages or a full-product release. Secrets must be supplied only through approved
settings/provider flows, never pasted into chat. No purchase or security bypass.

| Gate | Affected stages | Evidence / exact action required |
|---|---|---|
| SEC source identification and real reconciliation (E01) | S02, S12 source verification, S11/S20 release | SourceCapture preflight reports no configured SEC contact and sends no requests. Configure a valid local contact through Settings, allow www.sec.gov and data.sec.gov, then run the bounded paced source matrix and independently reconcile filings. No fabricated contact or synthetic substitution. |
| OAuth app and eligible user connection (E02) | S08, S11/S20 | Actual app registration, callback and eligible account consent/data access are absent. Register through Alpaca's supported flow; set the exact broker callback; user explicitly connects. Keep app secret server-side. Verify each symbol/IEX capability and dated eligible quote. |
| Permitted zero-required-cost host/provider use (E03) | S07.3, S08, S11/S20 | No deployment, current free-tier eligibility or intended-use rights established. Confirm current terms and spending safeguards, approve a zero-cost HTTPS host and protected runtime settings; then verify cold start, edge query redaction, health/restart and end-to-end consent. Health alone is insufficient. |
| Container/documentation egress (E06/BL-004) | S07.2/3 | Docker build was denied before compilation at centralus.data.mcr.microsoft.com. Current documentation requests to render.com and docs.alpaca.markets were denied. Permit those required destinations and retry build/smoke/current terms review. Native .NET broker smoke is separately passed. |
| Trusted Windows route and local/clean-PC acceptance (E04) | S09, S11/S20 | This machine is Linux. Unsigned Windows cross-publish/hash/native-library contents can be checked, but signing identity, provider acceptance, actual clean install/upgrade/failure/rollback/uninstall retention, scaling/accessibility and trust execution are not verified. Complete these on a clean supported Windows PC with a trusted signing/distribution route, including final notice/attribution review of the generated artifact inventory. Keep Smart App Control/Defender enabled; no exclusions or self-signed trust roots. |
| Historical point-in-time data and rights (E05) | S19 historical valuation, full-catalogue S20 | No permitted dated price/share/corporate-action series and then-available filing reconstruction supplied. Establish a zero-required-cost licensed source with those semantics and storage/use rights. Current restated fundamentals plus old prices are not point-in-time valuation. Sourced fundamentals charts/manual changes work independently. |

S11 connected-pilot publication and S20 full-product publication remain blocked by
the combined acceptance gates. No release was published to conceal them. A reduced
scope engineering preview is not full-catalogue completion. Current implementation,
local/CI evidence and exact PRs/commits are recorded in Stage_Ledger.json and the
validation reports. No further engineering package should be labelled done solely
because its tests are green; preserve any per-package remaining acceptance.
