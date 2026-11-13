[CmdletBinding()]
param(
    [string]$Dotnet = 'dotnet',
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.-]+)?$')]
    [string]$Version = '0.1.0-preview.3'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $repoRoot 'artifacts'
$packageName = "AlphaQuant-$Version-win-x64"
$publishDir = Join-Path $artifactRoot $packageName
$zipPath = Join-Path $artifactRoot "$packageName.zip"
# Refuse to reuse output, so old files can never enter a new release package.
if ((Test-Path -LiteralPath $publishDir) -or (Test-Path -LiteralPath $zipPath)) {
    throw 'This version already has output. Use a new version or manually inspect and remove the old artifact.'
}
& $Dotnet publish (Join-Path $repoRoot 'src/AlphaQuant.Desktop/AlphaQuant.Desktop.csproj') -c Release -r win-x64 --self-contained true -o $publishDir "-p:Version=$Version" -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/development/First_Preview.md') -Destination (Join-Path $publishDir 'START-HERE.md')
foreach ($guide in @('Research_Storage.md', 'Valuation_Review.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/development/$guide") -Destination (Join-Path $publishDir $guide)
}
Compress-Archive -LiteralPath $publishDir -DestinationPath $zipPath
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $packageName.zip" | Set-Content -LiteralPath "$zipPath.sha256" -Encoding ascii
Write-Output "Preview package: $zipPath"
Write-Output "SHA256: $hash"
