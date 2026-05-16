param(
    [string]$DocsRoot = ".\docs"
)

$ErrorActionPreference = "Stop"
$docs = Resolve-Path -LiteralPath $DocsRoot
$pausePath = Join-Path $docs "codex-loop-paused.flag"
$historyPath = Join-Path $docs "codex-phase-history.jsonl"
if (Test-Path -LiteralPath $pausePath) {
    Remove-Item -LiteralPath $pausePath
}
if (-not (Test-Path -LiteralPath $historyPath)) {
    New-Item -ItemType File -Path $historyPath | Out-Null
}
$row = [ordered]@{
    phaseId = ""
    eventKind = "codex_loop_resumed"
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    summary = "loop resumed"
    status = "Idle"
}
($row | ConvertTo-Json -Compress) | Add-Content -LiteralPath $historyPath -Encoding UTF8
Write-Host "Codex loop resumed."
