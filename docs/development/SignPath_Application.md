# SignPath Foundation application preparation

Prepared 2026-11-14. The maintainer reports submitting the application.
**Provider acceptance and trusted signing access remain pending.**
The project has no existing code-signing certificate/service.

Application reference: https://signpath.org/apply. The project entries and
request below document the application scope. Keep personal account details,
identity documents, credentials and signing secrets out of Git and chat.

## Public project entries

- Project name: AlphaQuant
- Repository: https://github.com/minwaiphyo/AlphaQuant
- Project license: MIT for project-owned code; third-party terms retained separately
- Platform: Windows x64 desktop, C#/.NET 10 and Avalonia
- Requested offering: free open-source signing through SignPath Foundation
- Current availability: draft prerelease and unsigned CI Windows packages; no public release yet

## Copy-ready project description and request

AlphaQuant is an actively developed open-source Windows desktop application for
financial research, screening and valuation. It does not trade. Project-owned
code is MIT-licensed, and contributor licensing permission is confirmed.
Third-party dependencies and notices are tracked separately; final package
license clearance is still in progress.

We are requesting zero-cost SignPath Foundation signing for reviewed, CI-built
Windows preview packages so the maintainer can test the application natively on
Windows 11 with Smart App Control enabled. The current unsigned build is blocked
when loading the project-owned AlphaQuant.Desktop.dll (Code Integrity event
3077). We need trusted signing of project-owned EXE and DLL files, not just the
application host. We will not sign upstream binaries under the project identity.
We can implement the required verified-origin CI workflow and manual signing
approval after acceptance and confirmation of your requirements.

The repository currently has a draft prerelease and unsigned Windows CI
packages, but no public release. Your published terms require an existing
release in the form to be signed. Can this project be reviewed at this stage,
or must a public unsigned Windows release exist first? Please also confirm
whether reviewed development/preview packages qualify, what no-cost quotas,
certificate/timestamp profile and approval requirements apply, and how unsigned
upstream dependencies must be handled. We do not assume arbitrary local Debug
builds qualify. The Release package excludes the Debug-only diagnostics component
whose license evidence remains unresolved.

## After the provider replies

Share the non-secret acceptance decision, release prerequisite and technical
integration requirements. Keep account links, identity verification and tokens
private. If an unsigned public release is required, report that prerequisite to
the maintainer before publishing anything. This application authority does not itself
authorize a public release, payment, security-policy changes or acceptance of
unknown contractual terms on the maintainer's behalf.

Only after acceptance/configuration: integrate supported signing, verify EXE/DLL
signatures and trusted timestamps, regenerate hashes/archive after signing, and
test the signed Windows package on the affected PC with security still enabled.
A submitted application alone does not resolve the Windows launch block.
