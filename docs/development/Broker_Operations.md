# Broker operational verification

S07, 2026-11-14. Uses data-only paper OAuth. Application credentials stay on the
server; tokens and pending sessions are in memory, not research or logs.

Audit verified random state and verifier binding, constant-time verifier checking,
one-time retrieval, callback replay rejection, five-minute expiry, a 256-session
cap, bounded HTTP exchange, cancellation/late-delivery rejection, exact redirect
validation, no redirects in the provider HTTP client and safe request errors.
Application request logging is disabled. The shared HTTP client is now disposed
when the host exits. Shutdown/restart deliberately loses pending authorization;
users must start Connect again. Disconnect clears local tokens, not provider grants.

`python tools/Test-BrokerSmoke.py` launches only an owned loopback process with
synthetic server configuration. It checks ready/no-store/security headers, invalid
challenges, wrong-verifier rejection, denial, one-time results, rate limiting and
restart loss. It never opens a browser or exchanges an Alpaca code. Linux passed;
standard Windows CI runs the same smoke. Added service regressions for capacity,
expiry reclamation, late cancellation and malformed token responses.

Container build was attempted against the managed Docker daemon. It failed before
compilation because Microsoft registry metadata redirected to
`centralus.data.mcr.microsoft.com`, denied by egress policy. Container runtime
verification therefore remains open; native process smoke is separately verified.
The Dockerfile now includes global.json so builds respect the SDK policy.

Current Render free-host and Alpaca documentation rechecks were blocked at
`render.com` and `docs.alpaca.markets`. Their current no-cost terms/eligibility are
not verified. No host/account/secret was created. See Alpaca_OAuth_Setup.md for
configuration and cold-start/single-instance limitations; its older claims are
historical and do not establish current terms or live acceptance.

Deployment checklist: confirm current free-tier and spending safeguards, exact
registered HTTPS callback, edge TLS, single instance, callback-query redaction in
edge logs, protected runtime-only secret injection, /health with no-store, cold
start within the desktop timeout and restart/recovery. Perform actual provider
consent/data acceptance only after explicit user connection. Health alone does not
prove registration, permission or IEX access. No trusted deployment is claimed.
