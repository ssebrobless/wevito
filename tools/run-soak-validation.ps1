param(
    [ValidateRange(1, 24)]
    [int]$Hours = 4,

    [string]$ArtifactRoot = ".\vnext\artifacts\soak"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedArtifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $resolvedArtifactRoot "$timestamp-manual-soak"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$summaryPath = Join-Path $runRoot "run-summary.md"
$ledgerPath = Join-Path $env:LOCALAPPDATA "Wevito\audit\ledger.sqlite"
$focusPath = Join-Path $env:LOCALAPPDATA "Wevito\audit\focus-steal.json"
$budgetPath = Join-Path $env:LOCALAPPDATA "Wevito\audit\budget-meter.json"

@"
# Manual Soak Validation

- Started: $(Get-Date -Format o)
- Requested hours: $Hours
- Artifact root: $runRoot
- Ledger path: $ledgerPath
- Focus-steal counter: $focusPath
- Budget meter: $budgetPath

This script does not flip settings, enable capabilities, call models, fetch network data, or mutate sprites/assets.
Launch Wevito separately or attach this run while Wevito is already running, then leave it open for the requested window.

Manual command for this phase:

````powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-validation.ps1 -Hours $Hours -ArtifactRoot $ArtifactRoot
````
"@ | Set-Content -Path $summaryPath -Encoding UTF8

$snapshot = [ordered]@{
    started_at = (Get-Date).ToUniversalTime().ToString("o")
    requested_hours = $Hours
    artifact_root = $runRoot
    ledger_exists = Test-Path $ledgerPath
    focus_counter_exists = Test-Path $focusPath
    budget_meter_exists = Test-Path $budgetPath
    mutates_settings = $false
    enables_default_off_capabilities = $false
    launches_model = $false
    fetches_network = $false
}

$snapshot | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $runRoot "snapshot.json") -Encoding UTF8
Write-Host "[soak] Prepared manual soak artifact folder: $runRoot"
Write-Host "[soak] This script is intentionally non-mutating and does not keep the machine busy."
Write-Host "[soak] Summary: $summaryPath"
