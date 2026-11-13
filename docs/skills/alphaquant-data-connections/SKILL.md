---
name: alphaquant-data-connections
description: Implement or review AlphaQuant browser OAuth, token-broker boundaries, Alpaca IEX access checks and streaming lifecycle. Use for connection behavior and provider adapters, not accounting mappings or generic API integrations.
---

# Per-user market connections

Read current `OAuthSession`, `OAuthBrokerService`, `AlpacaMarketDataProvider`, `ComparisonBackend` and the Desktop `ResearchBackend` boundary. Read [the connection contract](references/connection-contract.md) for state/recovery checks. Verify current official provider documentation when changing endpoints, scopes or capability assumptions.

Keep the application secret server-side and user tokens in memory only. Browser OAuth is selected, not API-key/password entry. No trading/account/portfolio scope or endpoint is needed. Skills do not grant account creation, consent, secret access, vendor messaging or spending authority.

Distinguish configuration, authorization, selected-symbol access, acknowledged stream and eligible fresh trade. Authorization alone must not activate prices. Test every selected symbol and verify the required real-time/streaming capability. IEX is limited venue, not a consolidated market or evidence of redistribution rights.

Use typed safe failures, bounded retry/cancel and a single active provider stream. Keep fundamentals usable without a market connection. Reset source-dependent prices on disconnect or replacement, and guard late responses/replayed authorization.

Exercise fake transport/state transitions before bounded authorized live checks. Deliver non-secret evidence for access, lifecycle and failure recovery, with live versus synthetic results separated. Never claim a configuration file or health endpoint proves permitted end-to-end access.
