[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $repoRoot ('.local/skill-install-tests/' + [Guid]::NewGuid().ToString('N'))
$installer = Join-Path $PSScriptRoot 'Install-ProjectSkills.ps1'
$catalog = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'docs/skills/skill_catalog.json') | ConvertFrom-Json
$successPath = Join-Path $testRoot 'success/skills'
& $installer -DiscoveryRoot $successPath
& $installer -DiscoveryRoot $successPath
foreach ($skill in $catalog.skills) {
    $link = Get-Item -Force -LiteralPath (Join-Path $successPath $skill.name)
    if (-not ($link.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Expected discovery link.' }
    if (-not (Test-Path -LiteralPath (Join-Path $link.FullName 'SKILL.md'))) { throw 'Linked skill not readable.' }
}

$collisionPath = Join-Path $testRoot 'collision/skills'
$collisionSkill = Join-Path $collisionPath $catalog.skills[0].name
New-Item -ItemType Directory -Force -Path $collisionSkill | Out-Null
$sentinel = Join-Path $collisionSkill 'existing-content.txt'
Set-Content -LiteralPath $sentinel -Value 'preserve me'
$rejected = $false
try { & $installer -DiscoveryRoot $collisionPath } catch { $rejected = $true }
if (-not $rejected -or (Get-Content -Raw -LiteralPath $sentinel).Trim() -ne 'preserve me') { throw 'Collision must be rejected without changing existing content.' }
if (@(Get-ChildItem -Force -LiteralPath $collisionPath).Count -ne 1) { throw 'Preflight failure must not create other skill links.' }

$alternateTarget = Join-Path $testRoot 'alternate-content'
New-Item -ItemType Directory -Force -Path $alternateTarget | Out-Null
$wrongLinkRoot = Join-Path $testRoot 'wrong-target/skills'
New-Item -ItemType Directory -Force -Path $wrongLinkRoot | Out-Null
$linkType = if ($env:OS -eq 'Windows_NT') { 'Junction' } else { 'SymbolicLink' }
$wrongLink = Join-Path $wrongLinkRoot $catalog.skills[0].name
New-Item -ItemType $linkType -Path $wrongLink -Target $alternateTarget | Out-Null
$rejected = $false
try { & $installer -DiscoveryRoot $wrongLinkRoot } catch { $rejected = $true }
if (-not $rejected -or -not (Test-Path -LiteralPath $alternateTarget)) { throw 'A link to another skill source must be preserved and rejected.' }

$redirect = Join-Path $testRoot 'redirect'
New-Item -ItemType $linkType -Path $redirect -Target $alternateTarget | Out-Null
$rejected = $false
try { & $installer -DiscoveryRoot (Join-Path $redirect 'skills') } catch { $rejected = $true }
if (-not $rejected -or (Test-Path -LiteralPath (Join-Path $alternateTarget 'skills'))) { throw 'Linked discovery ancestors must be rejected before mutation.' }

$outsidePath = Join-Path (Split-Path $repoRoot -Parent) ('skill-install-reject-' + [Guid]::NewGuid().ToString('N'))
$rejected = $false
try { & $installer -DiscoveryRoot $outsidePath } catch { $rejected = $true }
if (-not $rejected -or (Test-Path -LiteralPath $outsidePath)) { throw 'Output outside the repository must be rejected before mutation.' }
Write-Output 'Installation checks passed: readable links, idempotence, directory/wrong-link collision preservation, linked-ancestor and outside-path rejection.'
Write-Output "Isolated ignored fixtures retained for inspection: $testRoot"
