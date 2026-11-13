# Connection boundaries and acceptance

Current source and `docs/development/Alpaca_OAuth_Setup.md` define the implemented contract. Re-read when changing code. Production secrets do not enter desktop packages, source, browser return URLs, research models, DB/backups or request/error logs.

| State | What is established | What is not established |
|---|---|---|
| Broker configured | A validated public origin exists | Actual deployment, provider permission or login |
| Authorized | User completed the approved browser flow | Every symbol's data access or fresh prices |
| Access tested | Returned capabilities/symbols meet requested workflow | Stream acknowledgement or latest eligible trade |
| Stream acknowledged | Subscription is accepted | A new qualifying trade or market-open status |
| Fresh eligible quote | Current baseline validates symbol/currency/time/conditions | Full-market coverage, capital evidence or a buy signal |

The Desktop facade clears verified symbol access on reconnect/disconnect. Changing the selected set requires coverage for the new set. A sample trade is not current streaming proof. Do not silently downgrade freshness or reuse old provider values.

## Failure matrix

- Unconfigured broker disables connection without blocking SEC research.
- Consent cancel/deny/timeout and late token delivery do not resurrect a disconnected session.
- State/challenge mismatch, one-time retrieval and callback replay stay rejected.
- Unauthorized responses clear session access as the contract requires. Error display does not expose response bodies/headers.
- Entitlement mismatch, missing symbols, rate/connection limits and network loss have distinct safe recovery behavior. No paid upgrade or retry storm.
- Subscription cancellation/disposal ends the producer/timer/socket lifetime. Changing peers or navigating cannot leave duplicate streams.
- Quote freshness is separate from socket liveness. Old quotes can remain inspectable only with original dates/status, not valid current valuation.
- Corrections/cancellations invalidate the affected symbol until a new eligible event. Older/duplicate/future events follow the defined filtering policy.
- Broker restart can lose pending login sessions in the current in-memory design. Explain retry without implying persisted token refresh/revocation exists.

Use fake handlers/socket/time providers for deterministic faults. For authorized real checks, record date, non-secret deployment/callback metadata, selected symbols, actual capabilities and observed state. Do not extract an app/browser token to make a test convenient.

Primary references: [Alpaca OAuth](https://docs.alpaca.markets/us/docs/using-oauth2-and-trading-api) and [streaming market data](https://docs.alpaca.markets/us/docs/streaming-market-data). Recheck rather than copying sample trading scopes or assuming current rights/limits from a plan name.
