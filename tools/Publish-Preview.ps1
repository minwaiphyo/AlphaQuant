[CmdletBinding()]
param(
    [string]$Dotnet = 'dotnet',
    [string]$Python = 'python',
    [ValidateSet('Development', 'Recruiter')]
    [string]$Distribution = 'Development',
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.-]+)?$')]
    [string]$Version = '0.1.0-preview.3'
)
$ErrorActionPreference = 'Stop'
# Do not accept a bypass switch or describe an unsigned package as trusted.
# Provider eligibility, license clearance and Windows acceptance are unresolved.
if ($Distribution -eq 'Recruiter') {
    throw 'Recruiter distribution is blocked: no verified signing provider, complete license clearance or Windows trust acceptance. See docs/development/Windows_Distribution.md.'
}
$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $repoRoot 'artifacts'
$packageName = "AlphaQuant-$Version-win-x64-unsigned-development"
$publishDir = Join-Path $artifactRoot $packageName
$zipPath = Join-Path $artifactRoot "$packageName.zip"
# Refuse to reuse output, so old files can never enter a new release package.
if ((Test-Path -LiteralPath $publishDir) -or (Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath "$zipPath.sha256")) {
    throw 'This version already has output. Use a new version or manually inspect and remove the old artifact.'
}
& $Dotnet publish (Join-Path $repoRoot 'src/AlphaQuant.Desktop/AlphaQuant.Desktop.csproj') -c Release -r win-x64 --self-contained true -o $publishDir "-p:Version=$Version" -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/development/First_Preview.md') -Destination (Join-Path $publishDir 'START-HERE.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $publishDir 'LICENSE')
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $publishDir 'THIRD-PARTY-REVIEW.md')
& $Python (Join-Path $PSScriptRoot 'Collect-DistributionNotices.py') --publish-dir $publishDir --assets (Join-Path $repoRoot 'src/AlphaQuant.Desktop/obj/project.assets.json')
if ($LASTEXITCODE -ne 0) { throw 'Dependency/license evidence collection failed; no ZIP created.' }
@'
UNSIGNED DEVELOPMENT BUILD — NOT A TRUSTED RECRUITER RELEASE
Smart App Control or Defender may block this software. Keep protections enabled.
No signed publisher, Windows trust acceptance or completed license clearance is claimed.
See START-HERE.md, THIRD-PARTY-REVIEW.md and dependency-inventory.json.
'@ | Set-Content -LiteralPath (Join-Path $publishDir 'UNSIGNED-DEVELOPMENT.txt') -Encoding utf8
$sourceCommit = & git -C $repoRoot rev-parse --verify HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify package source commit.' }
$sourceChanges = @(& git -C $repoRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine source working tree state.' }
$sdkVersion = & $Dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify SDK version.' }
$fileHashes = @(
    Get-ChildItem -LiteralPath $publishDir -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ Path = $_.FullName.Substring($publishDir.Length + 1).Replace('\', '/');
            SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
)
[ordered]@{ SchemaVersion = 1; Version = $Version; SourceCommit = [string]$sourceCommit;
    SourceDirty = ($sourceChanges.Count -gt 0); SdkVersion = [string]$sdkVersion;
    CreatedAt = [DateTime]::UtcNow.ToString('o'); Distribution = 'unsigned-development';
    SigningStatus = 'not_configured'; LicenseReview = 'required'; WindowsAcceptance = 'not_verified';
    Files = $fileHashes } | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath (Join-Path $publishDir 'build-evidence.json') -Encoding utf8
Compress-Archive -LiteralPath $publishDir -DestinationPath $zipPath
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $packageName.zip" | Set-Content -LiteralPath "$zipPath.sha256" -Encoding ascii
Write-Output "Unsigned development package (not recruiter distribution): $zipPath"
Write-Output "SHA256: $hash"
