param(
    [ValidateRange(1, 14)]
    [int]$Days = 7,

    [string]$ArtifactRoot = ".\vnext\artifacts\soak",

    [ValidateRange(15, 240)]
    [int]$HeartbeatMinutes = 60,

    [switch]$StopOnPowerSleep
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cli = Join-Path $repoRoot "vnext\artifacts\soak-driver\Wevito.VNext.SoakDriver.exe"
if (-not (Test-Path $cli)) {
    throw "Missing soak driver CLI. Run tools\build-vnext.ps1 -SkipAssetPrep first. Expected: $cli"
}

$process = Get-Process -Name "Wevito.VNext.Shell" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $process) {
    Write-Host "[soak] Wevito.VNext.Shell is not running. Launch Wevito before starting an evidence soak."
    exit 2
}

$artifactFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $artifactFull "$timestamp-soak-window"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$statusJson = & $cli status --artifact-root $artifactFull
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$statusPayload = $statusJson | ConvertFrom-Json
$settings = @{}
$statusPayload.settingsSnapshot.PSObject.Properties | ForEach-Object { $settings[$_.Name] = [string]$_.Value }
$blocked = @(
    "runtime_autonomous_beta_enabled",
    "pet_model_adapter_enabled",
    "web_search_enabled",
    "local_tool_exec_enabled",
    "tuning_lora_enabled",
    "runtime_kill_switch"
) | Where-Object {
    if (-not $settings.ContainsKey($_)) { return $false }
    $parsed = $false
    [System.Boolean]::TryParse($settings[$_], [ref]$parsed) -and $parsed
}
if ($blocked.Count -gt 0) {
    Write-Host "[soak] Refusing to start because default-off capability is enabled: $($blocked -join ', ')"
    exit 3
}

$manifest = [ordered]@{
    schema_version = "1"
    started_at_utc = (Get-Date).ToUniversalTime().ToString("o")
    requested_days = $Days
    heartbeat_minutes = $HeartbeatMinutes
    artifact_root = $artifactFull
    initial_settings_snapshot_sha256 = $statusPayload.settingsSnapshotSha256
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

Write-Host "[soak] Started evidence window: $runRoot"
Write-Host "[soak] Ctrl+C ends the window cleanly. No settings will be flipped."

$started = [DateTimeOffset]::Parse($manifest.started_at_utc)
$lastDay = $started.UtcDateTime.Date
try {
    while ($true) {
        $now = [DateTimeOffset]::UtcNow
        if ($now -ge $started.AddDays($Days)) {
            & $cli window-end --artifact-root $artifactFull --reason completed | Out-Host
            break
        }

        $running = Get-Process -Name "Wevito.VNext.Shell" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $running) {
            Write-Host "[soak] Wevito exited; ending soak window instead of silently continuing."
            & $cli window-end --artifact-root $artifactFull --reason wevito_process_exited | Out-Host
            exit 4
        }

        & $cli heartbeat --artifact-root $artifactFull --reason scheduled | Out-Host
        if ($now.UtcDateTime.Date -gt $lastDay) {
            & $cli day-end --artifact-root $artifactFull | Out-Host
            $lastDay = $now.UtcDateTime.Date
        }

        Start-Sleep -Seconds ($HeartbeatMinutes * 60)
    }
}
finally {
    $summaryPath = Join-Path $runRoot "run-summary.md"
    @"
# Soak Driver Run

- Started: $($manifest.started_at_utc)
- Ended: $([DateTimeOffset]::UtcNow.ToString("o"))
- Requested days: $Days
- Heartbeat minutes: $HeartbeatMinutes
- Stop on power sleep requested: $StopOnPowerSleep
- Artifact root: $artifactFull

The soak driver does not enable capabilities, call models, open network connections, mutate assets, or flip settings.
"@ | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Host "[soak] Run summary: $summaryPath"
}
