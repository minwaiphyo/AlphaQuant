# Windows trust and portfolio distribution

Status: partial infrastructure implemented; trusted Windows development and
recruiter distribution remain **blocked**. No signing identity is configured.
No purchase, security-policy change, live financial request, or release publication
is authorized by this workflow. The owner resumed independent development through S20; PR24 is merged. These trust gates remain open.

The owner approved MIT licensing for all project contributions on 2026-11-13,
confirmed contributor permission, and requires zero incremental cost. See
`LICENSE` and `THIRD-PARTY-NOTICES.md`; third-party materials and financial data
are not relicensed by the repository license.

## Current local-development diagnosis (2026-11-14)

Owner reports Windows 11 Home on a personal PC. The block occurs during
`dotnet run --project src/AlphaQuant.Desktop` and refers to a Desktop DLL whose
publisher cannot be verified. Owner evidence now shows SmartAppControlState On and enforced CodeIntegrity
event 3077 when the Desktop host loads the project-owned Debug/net10.0/
AlphaQuant.Desktop.dll, policy ID 0283ac0f-fff1-49ae-ada1-8a933130cad6. This confirms
a DLL code-integrity block, not the usual downloaded-EXE SmartScreen warning.
The generic Enterprise signing-level wording does not establish corporate management.
No personal filesystem path or whole event log is retained here.

Reproduce once, then collect the read-only SmartAppControlState and relevant
CodeIntegrity event 3077. Identify the named blocked DLL and its signature status
before choosing a remedy. A signed CI/release executable does not establish that
fresh local Debug assemblies will be permitted. The owner subsequently confirmed native Windows viewing/testing is required and
authorized addressing Windows signing now. WSL/Linux viewing was rejected. A
trusted Windows development package can serve that goal; support for signing
arbitrary fresh local Debug output is a separate provider capability. No security controls, file trust markers or trust
stores are changed.

Live Microsoft/SignPath documentation is now reachable. Checked on 2026-11-14:
Microsoft currently states Smart App Control supports BOTH RSA and ECC trusted
code-signing certificates, superseding the older pinned RSA-only source below.
SignPath Foundation still requires a released, maintained OSI-licensed project,
verifiable artifact origin, review/manual signing approval and project acceptance.
The repository currently has only a draft prerelease; no public-release eligibility
or permission to sign arbitrary fresh local Debug builds is established.
Microsoft's current Store onboarding page describes free individual enrollment,
subject to identity verification/eligibility. Store delivery does not provide a
certificate for arbitrary `dotnet run` output. No account/application was submitted,
certificate obtained, service purchased or release/environment published.

## Code signing policy

No signing service or certificate is active. Only unsigned development artifacts
are produced. No contributor is currently assigned provider signing permissions.
After provider acceptance, document named author/reviewer/approver roles, require
MFA, publish accurate provider attribution and privacy/uninstall information, and
enforce release approval. Do not advertise free signing as already provided.

## Diagnose the affected Windows PC

The owner reports Smart App Control on a personal PC. Earlier repository evidence
records project DLL loads blocked with `0x800711C7`. This cloud instance is Linux;
it cannot identify the exact blocking policy, affected DLL set, or Defender state
on the owner PC. This is not yet a completed diagnosis.

Immediately after the existing failed run, open PowerShell at the checkout root:

```powershell
./tools/Get-WindowsTrustDiagnostics.ps1 -Hours 2
```

The collector reads OS version, Defender status, Smart App Control registry state,
Code Integrity events (3076/3077/3089), Defender events (1116/1117), SHA256 hashes,
and Authenticode observations for desktop build output. It writes a uniquely named
local JSON report under ignored `artifacts/`; it does not launch AlphaQuant or
change policy, trust stores, exclusions, or download-origin metadata. Run using
an existing permitted PowerShell host; do not bypass script policy to run it.

For a downloaded package or an explicitly named blocked tool, include its full
file path (multiple paths are supported). Optionally record the exact block text:

```powershell
./tools/Get-WindowsTrustDiagnostics.ps1 -Hours 2 `
  -ArtifactPath 'C:\path\to\AlphaQuant.Desktop.dll' `
  -BlockMessage 'Paste the exact Windows message locally here'
```

Only events containing the checkout or explicit artifact paths are retained.
Each source is bounded to 5,000 events; truncation, missing permissions and missing
status observations are recorded. NT device paths, renamed files and unrelated
tool locations can need manual correlation in Event Viewer. Reports may contain
personal filesystem paths and event details: review locally before sharing.
No report is automatically uploaded or committed.

Correlate enforced Code Integrity event 3077 with its policy identity and signature
events; event 3076 is an audit observation. Correlate Defender events separately.
Do not infer that every Code Integrity event is Smart App Control, or that a valid
signature/absence of events proves execution is allowed. Inspect every named
blocked EXE/DLL, including native dependencies and build/test tools. Signatures
on an EXE alone do not establish trust for DLL loads.

If collection is blocked or incomplete, use Event Viewer and Windows Security to
inspect the same observations; do not change security settings. Share only the
relevant reviewed evidence, not whole system logs or secrets.

## Signing-provider feasibility gate

Direct checks on 2026-11-13 returned proxy CONNECT 403 for Microsoft Learn and
SignPath sites. Official GitHub source repositories were then inspected successfully
as an independent supported documentation source. Required live-documentation
domains were also added to the environment draft:
`learn.microsoft.com`, `about.signpath.io`, `signpath.org`, `signpath.io`.
Saving the draft does not apply its network rules. No provider has accepted the
project, and no signing capability has been configured.

| Candidate | Verified source evidence | Decision / unresolved gate |
| --- | --- | --- |
| SignPath Foundation | Official site source at commit `df7801c96a1c6a498add70bb0827baa07eb8f820`, `docs/terms.md`, requires an OSI license, active maintenance, an existing release, no proprietary components except system libraries, MFA, documented roles/policy, verifiable binary origin and manual release approval. Certificate publisher is SignPath Foundation. The source labels its code of conduct **draft**. | Candidate for release signing only, subject to provider confirmation/acceptance and technical validation. No entitlement or permission to sign arbitrary local builds is established. Do not sign upstream binaries with the project identity. |
| Microsoft Store | Official Microsoft source `open-a-developer-account.md`, revision date 2025-09-17, describes zero registration fees through the new `storedeveloper.microsoft.com` flow and individual identity verification using ID/selfie. Legacy entry points may differ. | A zero-cost enrollment candidate, subject to actual owner eligibility and account verification. ZIP publication does not itself meet Store packaging/submission requirements. Store enrollment does not provide a certificate for local builds. No account was created. |
| Smart App Control requirement | Live Microsoft guidance checked 2026-11-14 says both RSA and ECC certificates from trusted providers are supported; this supersedes the older pinned RSA-only text. | Verify the chosen certificate/profile against current guidance and actual blocked files. This is not a promise that Defender or every Windows policy accepts a signed build. |

Pinned sources inspected (2026-11-13):

- [SignPath Foundation conditions](https://github.com/SignPath/fdn-website/blob/df7801c96a1c6a498add70bb0827baa07eb8f820/docs/terms.md), from its official repository whose CNAME is `signpath.org`.
- [Microsoft Smart App Control source](https://github.com/MicrosoftDocs/windows-dev-docs/blob/a3812316d9d6678be4e526ffb4b1c4f3958d956d/hub/apps/develop/smart-app-control/code-signing-for-smart-app-control.md).
- [Microsoft Store onboarding source](https://github.com/MicrosoftDocs/windows-dev-docs/blob/a3812316d9d6678be4e526ffb4b1c4f3958d956d/hub/apps/publish/partner-center/open-a-developer-account.md).

The preferred first release-signing inquiry is SignPath Foundation. It requires
owner application/acceptance and confirmation of profile compatibility, artifact
types, timestamping, quotas and the exact permitted build origins. Arbitrary local
development signing is **unverified**, not approved by inference from release
signing. If that requirement cannot be met at zero cost, report it blocked as
requested. Retain Store as a separate distribution candidate; no MSIX identity or
submission workflow is invented before owner enrollment and packaging eligibility.

The default `dotnet run` builds **Debug**, whose graph differs from the audited
Release ZIP. In particular, `AvaloniaUI.DiagnosticsSupport/2.2.3` is referenced in
Debug, but its restored nuspec supplies no license declaration or license text.
It is absent from the actual Release publish graph. Do not infer its open-source
eligibility or redistribution rights from Avalonia's license; verify this Debug
dependency separately before requesting Foundation signing for development builds.
Do not remove developer tooling or re-sign third-party DLLs to bypass this gate.

Live Microsoft/SignPath pages were rechecked on 2026-11-14. Complete the remaining provider-specific requirements after local policy diagnosis:

- [Microsoft Smart App Control signing guidance](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control).
- [SignPath open-source offering](https://about.signpath.io/product/open-source) and
  [SignPath Foundation](https://signpath.org/): eligibility, owner/contributor
  identity, eligible license/project hosting, fees, certificate compatibility,
  timestamping, quotas, artifact types, CI provenance and signing approval rules.
  Specifically verify whether reviewed CI-built Windows **preview packages** may
  be signed. Do not assume arbitrary fresh Debug output is eligible.
- [Microsoft Store developer registration](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account): current individual
  enrollment cost, geography/identity requirements, packaging eligibility and
  signing/distribution conditions. Store distribution does not establish trust
  for local `dotnet run` output. Do not assume registration is free.

Record checked URLs/date and provider answers before activating an integration.
If no zero-cost route supports a trusted Windows preview package, report the
provider gate as blocked; do not silently substitute a paid provider or weaken
Smart App Control. A self-signed development certificate is
not a reliable Smart App Control solution. Do not ask recruiters to install one.

## Implemented development-package workflow

### Native Windows signing application brief (2026-11-14)

The owner requires native Windows viewing/testing and has rejected WSL/Linux as a
substitute. Signing is now the immediate priority. The zero-incremental-cost
constraint remains; no provider has accepted the project and no trusted signing
identity is configured. The owner authorized pursuing SignPath Foundation and confirmed no existing
signing identity. No application, identity data or artifact has been sent.
The embedded application form is blocked by cloud proxy CONNECT HTTP 403; use
the [copy-ready application](SignPath_Application.md) on the official provider
page. No cloud network/environment configuration was changed.

The preferred zero-cost candidate is [SignPath Foundation](https://signpath.org/apply).
Its [published terms](https://signpath.org/terms), checked on 2026-11-14, require an
existing release in the form to be signed and provider acceptance. The repository
has only a draft prerelease. Do not claim eligibility or publish it merely to
satisfy that condition. Ask whether the existing draft/CI Windows package can be
reviewed and what public-release prerequisite applies.

Ready-to-review project brief for the owner to use with the provider:

> AlphaQuant: https://github.com/minwaiphyo/AlphaQuant
>
> AlphaQuant is an actively developed C#/.NET 10 Avalonia desktop financial
> research application, intended for a noncommercial portfolio. It does not trade.
> Project-owned code is MIT-licensed, and contributor permission is confirmed.
> Third-party notices are retained separately; package clearance is not complete.
>
> We need a zero-cost trusted Windows development package for native Windows 11
> testing with Smart App Control enabled. The observed enforced Code Integrity
> event 3077 names AlphaQuant.Desktop.dll. Signing only the app host is insufficient.
> A CI-built, reviewed Windows Release package is acceptable for viewing progress;
> arbitrary fresh local Debug signing is not assumed to be permitted.
>
> Existing CI builds/tests an unsigned self-contained Windows x64 ZIP. The
> repository currently has a draft prerelease, not an existing public release.
> Application-owned PE files include the Desktop app host and Desktop, Core and
> Infrastructure assemblies. The bundle also contains upstream Avalonia/native
> and Microsoft runtime files. We will not sign upstream files with the project
> identity. No signing identity or provider acceptance is configured.
>
> Please confirm:
> 1. Can this project be accepted now, or must an unsigned public release exist
>    first? What identity, reputation, role, MFA and approval requirements apply?
> 2. Are reviewed CI-built development/preview Windows packages eligible? What
>    verified-origin integration and per-build approvals are required?
> 3. Which trusted certificate profile, timestamp service, artifact types and
>    quotas are included at no cost? Can project-owned EXE and DLL files both be
>    signed for Smart App Control compatibility?
> 4. How should unsigned upstream/native dependencies be handled? Debug includes
>    AvaloniaUI.DiagnosticsSupport with unresolved license evidence; the actual
>    Release package omits that component.
>
> We understand that provider acceptance and a valid signature do not guarantee
> execution under every Windows security policy.

Enter any required identity information directly with the provider, never in Git
or chat. Review provider replies for private information before recording only
non-secret eligibility and integration requirements. Do not send keys, tokens,
certificate passwords or identity documents here.

After acceptance: use the provider's supported authenticated CI workflow to sign
project-owned PE files after the final build; verify signatures, certificate chain
and trusted timestamps; then regenerate package hashes/evidence and the ZIP.
Never rebuild after signing or re-sign upstream binaries as project-owned code.
Test the signed package on the affected Windows PC with Smart App Control still
On and correlate any new DLL blocks. Keep native acceptance open until that run
succeeds. Do not add a generic build hook before the actual provider is known.

### Build the unsigned development package

```powershell
python -m unittest discover -s tools/tests -v
./tools/Test-DistributionTools.ps1
dotnet test AlphaQuant.slnx -c Release
./tools/Publish-Preview.ps1 -Version 0.1.0-preview.4
```

The existing win-x64 self-contained ZIP remains supported. Output names include
`unsigned-development`; existing output/ZIP/checksum collisions are refused.
Python 3 is required for the standard-library notice collector. The package includes:

- `UNSIGNED-DEVELOPMENT.txt`, user instructions, project MIT license and third-party review notes;
- `dependency-inventory.json`, package-supplied `licenses/` and collected notices,
  covering the actual published dependencies and self-contained runtime;
- `build-evidence.json` with source commit, dirty-tree flag, SDK/version, explicit
  unconfigured signing/unverified Windows states, and SHA256 of every other
  package file. The external ZIP checksum covers the complete archive including
  this manifest. Checksums detect changes relative to a reference; they do not
  authenticate a publisher.

Notice collection fails on missing package metadata or invalid declared license
paths. Review remains required even when package text exists; wrapper metadata
does not resolve embedded font/native obligations. See `THIRD-PARTY-NOTICES.md`.

`-Distribution Recruiter` fails before invoking the build. There is no signing
bypass, signing credential, local build hook, protected release job or automatic
release publication while the provider and licensing gates remain unresolved.
The PR workflow retains Windows build/test/package verification without signing
credentials and uploads only unsigned development artifacts. CI success does not
prove execution on a Smart App Control machine.

## Integration after eligibility is established

Implement signing only after the above gate is satisfied. For local builds, use
an opt-in configuration with credentials outside Git and a supported identity
flow. Sign required final outputs before the exact `dotnet run --project
src/AlphaQuant.Desktop` execution; preserve vendor signatures. Authentication,
signing or verification failure must stop execution when signing is enabled.
Cache only verified signatures for identical content and invalidate after rebuild.
Ordinary cloud/CI builds must remain usable without signing configuration.

For releases, use a separate protected release environment and workflow on reviewed
commits, never PR-provided code with signing access. Pin integration actions,
minimize identity/token permissions, verify provenance, sign after all binary
modifications, verify chains and trusted timestamps, then ZIP and hash. Retain
exact source, SDK, per-file signature results and archive checksums. Do not
re-sign vendor files blindly. Require license clearance and Windows acceptance
before enabling recruiter mode. Owner approval is still required to publish.

## Acceptance and operational handoff

Pending Windows acceptance is explicit:

1. On the affected PC, capture evidence, then after a supported trust solution,
   run a clean build and the exact command. Change code and rebuild again to
   verify fresh outputs, successful rendering and no related enforced block.
   Keep Smart App Control/Defender enabled; do not make live SEC/Alpaca requests.
2. On a separate clean Windows PC with protections enabled, download the final
   signed ZIP normally, retain download-origin metadata, verify the manifest,
   signature chain and timestamp, extract and run. A single machine's result
   does not guarantee every Windows policy will accept it.
3. Test a tampered signed artifact, revoked/unavailable signing identity and
   invalid timestamp/verification failure. Reject packaging/release when signing
   is required; never fall back silently to unsigned output. These signing tests
   cannot run until an eligible identity/integration exists.

Before each release, recheck provider eligibility, certificate validity/renewal,
quotas, runtime dependency licenses and exact-commit Windows CI. Provider loss or
authentication failure blocks signed distribution. On suspected signing compromise,
stop the release workflow, revoke/rotate the identity with the provider, investigate
affected versions and communicate verified impact before reissuing packages. For
a Defender detection, retain the exact file/hash/detection evidence and use the
vendor's supported review route; never disable protection as a remedy.

Cloud implementation validation (2026-11-13): solution Release build passed with
zero warnings/errors; all 210 .NET synthetic tests passed with no skipped tests;
19 reference-arithmetic tests, 5 notice-collector tests, 4 PowerShell distribution
checks and the isolated PowerShell skill-installation suite passed. The final
cross-published unsigned Windows ZIP was inspected: 258 file hashes, complete
manifest coverage, archive checksum, 21 dependencies with retained license texts,
and evidence for all six Inter source font variants. This is packaging validation
on Linux, not Windows execution. Actual Windows Actions and diagnostic collection
have not run for this change; signed tamper/chain/timestamp tests remain pending.

No paid fallback is authorized. No Windows acceptance or signed release has yet
been verified. Current next action: use the confirmed DLL enforcement evidence and obtain
trusted signing access/provider eligibility before implementing integration.
Do not reconfigure the cloud environment to address this owner-PC signing gate.

## Current upgrade and rollback procedure (2026-11-14)

Use uniquely versioned unsigned development ZIPs in separate application folders.
Stop only the running app instance before changing binaries. Preserve the existing
settings and research directory in LocalApplicationData; never bundle, overwrite or
delete those user files as part of application installation/uninstallation. Create
a verified research backup before opening a newer schema. New schema 4 migrations
preserve verified old-schema backups and rollback on failure.

Rollback is not a database downgrade: retain the newer database, open a separately
restored verified old-schema backup at a new chosen path with a compatible binary,
and preserve both originals. Current UI does not select an alternate corrupt-file
recovery path; use the documented store API/recovery procedure without deletion.
Verify clean install, actual Windows launch, supported scaling/accessibility,
upgrade/failure/rollback and uninstall-with-data-retention on a clean supported PC
before claiming this route supported. These execution checks have not occurred on
this Linux machine. No security exclusions, policy bypass or self-signed trust root.

Current packages also include the research/model contracts and DejaVu font license.
PDFsharp 6.2.4 is MIT by inspected NuGet metadata; transitive Pkcs is pinned to patched
10.0.15. Package inventory is generated from the actual published dependency graph,
not an old static list. Audit output is evidence for review, not provider signing
or generally trusted identity. No release is published by these scripts.

Upstream notice bytes are retained exactly and SHA256-verified; .gitattributes
prevents checkout newline conversion and exempts only their upstream trailing
whitespace from cosmetic checks. Application source retains normal checks.

## Native Windows testing is required

The owner rejected the proposed WSL/Linux viewing route on 2026-11-14 and asked
to address Windows signing now. Do not request WSL setup or present Linux GUI
execution as satisfying this requirement. Native Windows signing and owner-PC
acceptance remain open at S09/S11/S20. The next dependency is an existing trusted
signing identity or provider acceptance under the zero-cost constraint.
