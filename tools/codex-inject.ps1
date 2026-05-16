param(
    [Parameter(Mandatory = $true)]
    [string]$PhaseId,
    [string]$PromptPath = "",
    [string]$BranchName = "",
    [switch]$AutoContinue,
    [string]$DocsRoot = ".\docs"
)

$ErrorActionPreference = "Stop"
$docs = Resolve-Path -LiteralPath $DocsRoot
$queuePath = Join-Path $docs "codex-phase-queue.json"
$historyPath = Join-Path $docs "codex-phase-history.jsonl"

if (-not (Test-Path -LiteralPath $queuePath)) {
    "[]" | Set-Content -LiteralPath $queuePath -Encoding UTF8
}
if (-not (Test-Path -LiteralPath $historyPath)) {
    New-Item -ItemType File -Path $historyPath | Out-Null
}

$completed = Get-Content -LiteralPath $historyPath | Where-Object {
    $_ -match '"phaseId":"([^"]+)"' -and $Matches[1] -eq $PhaseId -and $_ -match '"eventKind":"codex_phase_completed"'
}
if ($completed) {
    throw "Phase $PhaseId is already completed and cannot be re-injected."
}

$queue = @(Get-Content -Raw -LiteralPath $queuePath | ConvertFrom-Json)
$entry = [ordered]@{
    phaseId = $PhaseId
    promptPath = $PromptPath
    branchName = $BranchName
    autoContinue = [bool]$AutoContinue
    addedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    status = "pending"
    attemptCount = 0
}
$next = @($entry) + @($queue | Where-Object { $_.phaseId -ne $PhaseId })
$next | ConvertTo-Json | Set-Content -LiteralPath $queuePath -Encoding UTF8
Write-Host "Injected $PhaseId at the front of $queuePath"
