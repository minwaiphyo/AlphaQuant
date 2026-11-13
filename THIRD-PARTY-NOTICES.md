# Third-party licensing

The repository's MIT license covers owner-controlled AlphaQuant code and documentation.
The owner confirmed permission to license all project contributions under MIT on
2026-11-13. Third-party code, fonts, runtime components, provider data and
externally licensed materials retain their own terms. No financial-data rights
are granted by the repository license.

Development ZIPs include `THIRD-PARTY-NOTICES.txt`, `dependency-inventory.json`
and `licenses/`, generated from the **actual published dependency graph** and
the restored NuGet packages, including the self-contained .NET runtime.
Package-supplied license and notice files are copied without changing their text.
Supplemental upstream texts under `tools/distribution-licenses/` are pinned to
package source commits and checked by SHA256; `sources.json` records provenance.
License expressions and copyright metadata are recorded as evidence, not as
a substitute for required notices or a completed legal review.

Current audit findings:

| Component | Evidence and remaining work |
| --- | --- |
| Avalonia and CommunityToolkit | MIT; include the pinned Avalonia source license and CommunityToolkit package-supplied license. |
| Avalonia.Fonts.Inter | Wrapper is MIT. All six font variants at the package source commit identify Inter 3.019 / git-0a5106e0b and SIL OFL 1.1; include the corresponding upstream OFL text. Source hashes and font name-table evidence are retained in `sources.json`. Recheck the release's embedded resources against that source. |
| Avalonia ANGLE Windows natives | Package supplies `LICENSE`; review its included native notices. |
| SkiaSharp / HarfBuzzSharp | Include supplied license texts and native `THIRD-PARTY-NOTICES` files; do not assume the wrapper's MIT label covers every native component. |
| .NET self-contained runtime | Include package `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`. |
| Other runtime packages | Inventory contains exact versions and available license evidence for each release graph. |
| Test/build-only packages | Not intentionally bundled; for example DiagnosticsSupport is excluded from the Release publish. Verify against the generated graph rather than assuming exclusion from a project reference. |

This is a **Release distribution** inventory. The exact default `dotnet run`
command uses Debug and includes DiagnosticsSupport, whose restored package lacks
a license declaration/text. Its permission and signing-program compatibility
remain unverified; this inventory does not clear that Debug graph.

No standalone image/font asset files were found in the tracked desktop sources.
This does not waive obligations for resources embedded in dependencies. A future
release must re-audit its exact graph and resolve the inventory's review items
before recruiter distribution. This audit does not certify license compliance.
