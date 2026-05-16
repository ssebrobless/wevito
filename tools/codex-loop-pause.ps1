param(
    [string]$Reason = "user_pause",
    [string]$DocsRoot = ".\docs"
)

$ErrorActionPreference = "Stop"
$docs = Resolve-Path -LiteralPath $DocsRoot
$pausePath = Join-Path $docs "codex-loop-paused.flag"
$historyPath = Join-Path $docs "codex-phase-history.jsonl"
if (-not (Test-Path -LiteralPath $historyPath)) {
    New-Item -ItemType File -Path $historyPath | Out-Null
}
$Reason | Set-Content -LiteralPath $pausePath -Encoding UTF8
$row = [ordered]@{
    phaseId = ""
    eventKind = "codex_loop_paused"
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    summary = $Reason
    status = "Paused"
}
($row | ConvertTo-Json -Compress) | Add-Content -LiteralPath $historyPath -Encoding UTF8
Write-Host "Codex loop paused."
