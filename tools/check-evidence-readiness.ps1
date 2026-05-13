param(
    [string]$ArtifactRoot = ".\vnext\artifacts\soak"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cli = Join-Path $repoRoot "vnext\artifacts\soak-driver\Wevito.VNext.SoakDriver.exe"
if (-not (Test-Path $cli)) {
    throw "Missing soak driver CLI. Run tools\build-vnext.ps1 -SkipAssetPrep first. Expected: $cli"
}

$artifactFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
$json = & $cli status --artifact-root $artifactFull
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$payload = $json | ConvertFrom-Json
$status = $payload.status

Write-Host "Wevito evidence readiness"
Write-Host "-------------------------"
Write-Host ("Active          : {0}" -f $status.active)
Write-Host ("Readiness       : {0}" -f $status.lastReadinessLabel)
Write-Host ("Day             : {0} of {1}" -f $status.dayN, $status.dayMax)
Write-Host ("Rows today      : {0}" -f $status.rowsToday)
Write-Host ("Flagged today   : {0}" -f $status.flaggedRowsToday)
Write-Host ("Heartbeats today: {0}" -f $status.heartbeatCountToday)
Write-Host ("Focus delta     : {0}" -f $status.focusStealDeltaToday)
Write-Host ("Budget exceeded : {0}" -f $status.budgetExceededToday)
Write-Host ("Manifest        : {0}" -f $(if ([string]::IsNullOrWhiteSpace($status.manifestPath)) { "not started" } else { $status.manifestPath }))
