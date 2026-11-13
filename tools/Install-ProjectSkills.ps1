[CmdletBinding()]
param([string]$DiscoveryRoot)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent)).TrimEnd([IO.Path]::DirectorySeparatorChar)
$catalog = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'docs/skills/skill_catalog.json') | ConvertFrom-Json
if (-not $DiscoveryRoot) { $DiscoveryRoot = Join-Path $repoRoot '.agents/skills' }
if (-not [IO.Path]::IsPathRooted($DiscoveryRoot)) { $DiscoveryRoot = Join-Path $repoRoot $DiscoveryRoot }
$discoveryPath = [IO.Path]::GetFullPath($DiscoveryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
if (-not $discoveryPath.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Discovery output must stay inside this repository.'
}

# A lexical workspace check is insufficient if a parent redirects outside it.
$ancestor = $discoveryPath
while ($ancestor.Length -gt $repoRoot.Length) {
    if (Test-Path -LiteralPath $ancestor) {
        $item = Get-Item -Force -LiteralPath $ancestor
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Discovery ancestors must not be links.' }
        if (-not $item.PSIsContainer) { throw 'Discovery ancestors must be directories.' }
    }
    $ancestor = Split-Path $ancestor -Parent
}

$entries = @()
$seen = @{}
foreach ($skill in $catalog.skills) {
    if ($skill.name -notmatch '^alphaquant-[a-z0-9-]+$' -or $seen.ContainsKey($skill.name)) { throw 'Invalid or duplicate catalog skill name.' }
    $seen[$skill.name] = $true
    $source = [IO.Path]::GetFullPath((Join-Path $repoRoot $skill.path)).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $expected = [IO.Path]::GetFullPath((Join-Path $repoRoot ('docs/skills/' + $skill.name)))
    if (-not $source.Equals($expected, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath (Join-Path $source 'SKILL.md'))) {
        throw 'Catalog source must be a canonical project skill with SKILL.md.'
    }
    $destination = Join-Path $discoveryPath $skill.name
    $existing = Get-Item -Force -LiteralPath $destination -ErrorAction SilentlyContinue
    $exists = $null -ne $existing
    if ($exists) {
        if (-not ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -or @($existing.Target).Count -ne 1) {
            throw "Existing skill would be overwritten: $($skill.name). Nothing was changed."
        }
        $existingTarget = [string](@($existing.Target)[0])
        if (-not [IO.Path]::IsPathRooted($existingTarget)) { $existingTarget = Join-Path $discoveryPath $existingTarget }
        if (-not [IO.Path]::GetFullPath($existingTarget).TrimEnd([IO.Path]::DirectorySeparatorChar).Equals($source, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Existing skill points elsewhere: $($skill.name). Nothing was changed."
        }
    }
    $entries += [PSCustomObject]@{ Name = $skill.name; Source = $source; Destination = $destination; Exists = $exists }
}

# Preflight all entries before creating anything. No overwrite, deletion or global configuration.
New-Item -ItemType Directory -Force -Path $discoveryPath | Out-Null
foreach ($entry in $entries) {
    if ($entry.Exists) { Write-Output "Already linked: $($entry.Name)"; continue }
    $linkType = if ($env:OS -eq 'Windows_NT') { 'Junction' } else { 'SymbolicLink' }
    New-Item -ItemType $linkType -Path $entry.Destination -Target $entry.Source | Out-Null
    Write-Output "Linked project skill: $($entry.Name)"
}
