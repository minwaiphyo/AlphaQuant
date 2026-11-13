[CmdletBinding()]
param(
    [ValidateRange(1, 168)][int]$Hours = 24,
    [string[]]$ArtifactPath = @(),
    [string]$BlockMessage = '',
    [string]$OutputPath = ''
)
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'Windows diagnostics require the affected Windows PC. No evidence was collected.'
}
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot ('artifacts/trust-diagnostics-' + [Guid]::NewGuid().ToString('N') + '.json')
}
if (Test-Path -LiteralPath $OutputPath) { throw 'Refusing to overwrite existing diagnostics.' }
$problems = [Collections.Generic.List[string]]::new()
$files = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($path in $ArtifactPath) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Artifact is not a file: $path" }
    [void]$files.Add((Resolve-Path -LiteralPath $path).Path)
}
# Inspect build output without loading assemblies or executing the application.
$binRoot = Join-Path $repoRoot 'src/AlphaQuant.Desktop/bin'
if (Test-Path -LiteralPath $binRoot) {
    Get-ChildItem -LiteralPath $binRoot -File -Recurse |
        Where-Object { $_.Extension -in '.exe', '.dll' } |
        ForEach-Object { [void]$files.Add($_.FullName) }
}
$os = Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber, OSArchitecture
$defender = $null
try {
    $defender = Get-MpComputerStatus | Select-Object SmartAppControlState, AMServiceEnabled,
        AntivirusEnabled, RealTimeProtectionEnabled, AntivirusSignatureVersion
} catch { $problems.Add('Defender status unavailable: ' + $_.Exception.Message) }
$policyState = $null
try {
    $policyState = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' `
        -Name VerifiedAndReputablePolicyState).VerifiedAndReputablePolicyState
} catch { $problems.Add('Smart App Control registry observation unavailable: ' + $_.Exception.Message) }
$events = [Collections.Generic.List[object]]::new()
$observations = [Collections.Generic.List[object]]::new()
$start = (Get-Date).AddHours(-$Hours)
$sources = @(
    @{ Log = 'Microsoft-Windows-CodeIntegrity/Operational'; IDs = @(3076, 3077, 3089); Kind = 'CodeIntegrity' },
    @{ Log = 'Microsoft-Windows-Windows Defender/Operational'; IDs = @(1116, 1117); Kind = 'Defender' }
)
foreach ($source in $sources) {
    $matched = 0
    $scanCount = 0
    $scanLimit = 5000
    $queryErrors = @()
    $recent = @(Get-WinEvent -FilterHashtable @{ LogName = $source.Log; StartTime = $start; Id = $source.IDs } `
        -MaxEvents $scanLimit -ErrorAction SilentlyContinue -ErrorVariable queryErrors)
    $failures = @($queryErrors | Where-Object { $_.FullyQualifiedErrorId -notlike 'NoMatchingEventsFound*' })
    if ($failures.Count) { $problems.Add($source.Kind + ' event query failed; absence is not evidence of no blocks.') }
    foreach ($event in $recent) {
        $scanCount++
        # Keep only repository/explicit-artifact events. Never dump unrelated system logs.
        $xml = [xml]$event.ToXml()
        $data = @($xml.Event.EventData.Data | ForEach-Object { [string]$_.'#text' })
        $message = [string]$event.Message
        $search = ($data -join "`n") + "`n" + $message
        $relevant = $search.IndexOf($repoRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0
        if (-not $relevant) {
            foreach ($path in $ArtifactPath) {
                if ($search.IndexOf([IO.Path]::GetFullPath($path), [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $relevant = $true; break
                }
            }
        }
        if (-not $relevant) { continue }
        $matched++
        $events.Add([ordered]@{ Source = $source.Kind; Id = $event.Id; RecordId = $event.RecordId;
            TimeCreated = $event.TimeCreated.ToUniversalTime().ToString('o'); Message = $message;
            EventXml = $event.ToXml() })
        foreach ($value in $data) {
            if ($value -match '^[A-Za-z]:\\' -and (Test-Path -LiteralPath $value -PathType Leaf)) {
                [void]$files.Add($value)
            }
        }
    }
    $observations.Add([ordered]@{ Source = $source.Kind; QueryFailed = ($failures.Count -gt 0);
        EventsScanned = $scanCount; RelevantEvents = $matched; PossiblyTruncated = ($scanCount -ge $scanLimit) })
}
$signatures = @(
    foreach ($path in ($files | Sort-Object)) {
        try {
            $sig = Get-AuthenticodeSignature -LiteralPath $path
            [ordered]@{ Path = $path; SHA256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();
                Status = [string]$sig.Status; StatusMessage = $sig.StatusMessage;
                SignerSubject = $(if ($sig.SignerCertificate) { $sig.SignerCertificate.Subject } else { $null });
                SignerThumbprint = $(if ($sig.SignerCertificate) { $sig.SignerCertificate.Thumbprint } else { $null });
                TimestampSubject = $(if ($sig.TimeStamperCertificate) { $sig.TimeStamperCertificate.Subject } else { $null }) }
        } catch { $problems.Add('Signature observation failed for ' + $path + ': ' + $_.Exception.Message) }
    }
)
$report = [ordered]@{ SchemaVersion = 1; CollectedAt = [DateTime]::UtcNow.ToString('o');
    Status = 'diagnosis_required'; OperatingSystem = $os; Defender = $defender;
    SmartAppControlRegistryState = $policyState; BlockMessage = $BlockMessage;
    StartTime = $start.ToUniversalTime().ToString('o'); EventQueries = @($observations.ToArray());
    RelevantEvents = @($events.ToArray()); FileSignatures = $signatures; ObservationErrors = @($problems.ToArray());
    Limitations = @('Evidence is limited to the requested time window and repository/explicit artifact paths.',
        'NT device paths and short/renamed paths may require manual event correlation.',
        'A signature status or absent event does not prove Smart App Control will allow execution.',
        '3076 is audit evidence; 3077 indicates an enforced Code Integrity block. Correlate policy identity before attribution.',
        'No policy, Defender setting, certificate store, file trust marker or application binary was changed.') }
$fullOutput = [IO.Path]::GetFullPath($OutputPath)
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($fullOutput))
# CreateNew also protects against races with a second collector.
$stream = [IO.File]::Open($fullOutput, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
try {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($report | ConvertTo-Json -Depth 12))
    $stream.Write($bytes, 0, $bytes.Length)
} finally { $stream.Dispose() }
Write-Output "Local report: $fullOutput"
Write-Output 'Review paths and event contents before sharing. No logs were uploaded. Evidence still requires diagnosis.'
