param(
    [string]$DocsRoot = ".\docs",
    [switch]$Once,
    [switch]$WhatIfLoop
)

$ErrorActionPreference = "Stop"
$docs = Resolve-Path -LiteralPath $DocsRoot
$queuePath = Join-Path $docs "codex-phase-queue.json"
$statusPath = Join-Path $docs "codex-loop-status.json"
$historyPath = Join-Path $docs "codex-phase-history.jsonl"
$pausePath = Join-Path $docs "codex-loop-paused.flag"

function Ensure-CodexLoopFiles {
    if (-not (Test-Path -LiteralPath $queuePath)) {
        "[]" | Set-Content -LiteralPath $queuePath -Encoding UTF8
    }
    if (-not (Test-Path -LiteralPath $statusPath)) {
        '{"state":"idle","currentPhaseId":"","startedAtUtc":null,"lastHeartbeatUtc":null,"lastReason":"","attemptCount":0}' | Set-Content -LiteralPath $statusPath -Encoding UTF8
    }
    if (-not (Test-Path -LiteralPath $historyPath)) {
        New-Item -ItemType File -Path $historyPath | Out-Null
    }
}

function Add-HistoryRow($phaseId, $kind, $summary, $status) {
    $row = [ordered]@{
        phaseId = $phaseId
        eventKind = $kind
        createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        summary = $summary
        status = $status
    }
    ($row | ConvertTo-Json -Compress) | Add-Content -LiteralPath $historyPath -Encoding UTF8
}

Ensure-CodexLoopFiles

if (Test-Path -LiteralPath $pausePath) {
    Add-HistoryRow "" "codex_loop_paused" "pause sentinel present" "Paused"
    Write-Host "Codex loop paused: $pausePath"
    exit 0
}

$queue = Get-Content -Raw -LiteralPath $queuePath | ConvertFrom-Json
if ($null -eq $queue -or $queue.Count -eq 0) {
    Write-Host "Codex loop queue empty."
    exit 0
}

$phase = @($queue)[0]
$status = [ordered]@{
    state = "running"
    currentPhaseId = $phase.phaseId
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    lastHeartbeatUtc = (Get-Date).ToUniversalTime().ToString("o")
    lastReason = "phase_started"
    attemptCount = $phase.attemptCount
}
$status | ConvertTo-Json | Set-Content -LiteralPath $statusPath -Encoding UTF8
Add-HistoryRow $phase.phaseId "codex_phase_started" "phase started by run-codex-loop.ps1" "Running"

if ($WhatIfLoop) {
    Write-Host "WhatIf: would invoke Codex for phase $($phase.phaseId)."
    exit 0
}

Write-Host "Codex loop runner is configured. Live Codex CLI invocation remains operator-controlled in this scaffold."
if ($Once) {
    exit 0
}
