param(
    [ValidateRange(1, 14)]
    [int]$Days = 7,

    [ValidateRange(15, 240)]
    [int]$HeartbeatMinutes = 60,

    [string]$ArtifactRoot = ".\vnext\artifacts\soak",

    [switch]$StopOnPowerSleep
)

$ErrorActionPreference = "Stop"

Write-Warning "run-soak-validation.ps1 is deprecated; delegating to run-soak-driver.ps1."
& (Join-Path $PSScriptRoot "run-soak-driver.ps1") -Days $Days -HeartbeatMinutes $HeartbeatMinutes -ArtifactRoot $ArtifactRoot -StopOnPowerSleep:$StopOnPowerSleep
exit $LASTEXITCODE
