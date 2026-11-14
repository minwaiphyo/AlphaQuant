[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$checked = 0
foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' -File) {
    $tokens = $null; $parseErrors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { throw "PowerShell syntax errors in $($file.Name): $parseErrors" }
}
$checked++
try {
    & (Join-Path $PSScriptRoot 'Publish-Preview.ps1') -Distribution Recruiter -Dotnet 'must-not-run'
    throw 'Recruiter mode unexpectedly succeeded.'
} catch {
    if ($_.Exception.Message -notlike 'Recruiter distribution is blocked:*') { throw }
}
$checked++
$temp = Join-Path ([IO.Path]::GetTempPath()) ('alphaquant-packaging-test-' + [Guid]::NewGuid().ToString('N'))
try {
    [void][IO.Directory]::CreateDirectory((Join-Path $temp 'tools'))
    [void][IO.Directory]::CreateDirectory((Join-Path $temp 'artifacts'))
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Publish-Preview.ps1') -Destination (Join-Path $temp 'tools/Publish-Preview.ps1')
    $collision = Join-Path $temp 'artifacts/AlphaQuant-0.1.0-test-win-x64-unsigned-development.zip.sha256'
    Set-Content -LiteralPath $collision -Value 'preserve me'
    try {
        & (Join-Path $temp 'tools/Publish-Preview.ps1') -Version '0.1.0-test' -Dotnet 'must-not-run'
        throw 'Existing artifact unexpectedly accepted.'
    } catch {
        if ($_.Exception.Message -notlike 'This version already has output.*') { throw }
    }
    if ((Get-Content -LiteralPath $collision -Raw).Trim() -ne 'preserve me') { throw 'Collision was modified.' }
    $checked++
} finally {
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    try {
        & (Join-Path $PSScriptRoot 'Get-WindowsTrustDiagnostics.ps1')
        throw 'Non-Windows diagnostics unexpectedly succeeded.'
    } catch {
        if ($_.Exception.Message -notlike 'Windows diagnostics require*') { throw }
    }
    $checked++
}
Write-Output "Distribution tool checks passed: $checked. Windows event/signature collection and signed-package acceptance require Windows and are not claimed by this test."
