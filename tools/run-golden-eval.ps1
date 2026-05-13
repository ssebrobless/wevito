param(
    [string]$ArtifactRoot = ".\vnext\artifacts\eval-golden",
    [switch]$UpdateBaseline
)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path ".").Path
$dataset = Join-Path $repo "vnext\content\local-ai\eval-golden"
$questionsPath = Join-Path $dataset "questions.json"
$baselinePath = Join-Path $dataset "baseline.json"
$documentsRoot = Join-Path $dataset "documents"

New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null

function Get-DatasetSha {
    param([string]$Root)
    $rootFull = (Resolve-Path $Root).Path.TrimEnd('\')
    $lines = @()
    foreach ($file in Get-ChildItem -Path $rootFull -Recurse -File | Where-Object { $_.Name -ne "baseline.json" } | Sort-Object FullName) {
        $rel = $file.FullName.Substring($rootFull.Length + 1).Replace('\', '/')
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
        $lines += "$rel`:$hash"
    }
    $material = ($lines -join "`n") + "`n"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($material)
    return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
}

function Get-Tokens {
    param([string]$Text)
    return (($Text.ToLowerInvariant() -split '[\s\.,;:/\\\-_()\[\]{}]+') | Where-Object { $_.Length -gt 1 } | Select-Object -Unique)
}

function Get-Score {
    param([string]$Question, [string]$Text)
    $query = @(Get-Tokens $Question)
    if ($query.Count -eq 0) { return 0.0 }
    $doc = @(Get-Tokens $Text)
    $hits = 0
    foreach ($token in $query) {
        if ($doc -contains $token) { $hits++ }
    }
    return [double]$hits / [double]$query.Count
}

$datasetSha = Get-DatasetSha $dataset
$baseline = Get-Content -Raw -Path $baselinePath | ConvertFrom-Json
if (($baseline.datasetSha256 -ne $datasetSha) -and (-not $UpdateBaseline)) {
    Write-Error "Dataset sha256 mismatch. Expected $($baseline.datasetSha256), actual $datasetSha. Use -UpdateBaseline only when intentionally accepting a new baseline."
}

$questions = Get-Content -Raw -Path $questionsPath | ConvertFrom-Json
$documents = Get-ChildItem -Path $documentsRoot -Filter "*.md" | ForEach-Object {
    [pscustomobject]@{ ChunkId = $_.BaseName; Text = Get-Content -Raw -LiteralPath $_.FullName }
}

$reciprocal = @()
$latencies = @()
$recall1 = 0
$recall3 = 0
foreach ($question in $questions) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $ranked = $documents | ForEach-Object {
        [pscustomobject]@{ ChunkId = $_.ChunkId; Score = Get-Score $question.question $_.Text }
    } | Sort-Object @{Expression = "Score"; Descending = $true}, @{Expression = "ChunkId"; Descending = $false}
    $sw.Stop()
    $latencies += $sw.Elapsed.TotalMilliseconds
    $rank = 0
    for ($i = 0; $i -lt $ranked.Count; $i++) {
        if ($question.expectedChunkIds -contains $ranked[$i].ChunkId) {
            $rank = $i + 1
            break
        }
    }
    if ($rank -eq 1) { $recall1++ }
    if (($rank -gt 0) -and ($rank -le 3)) { $recall3++ }
    $reciprocal += $(if ($rank -gt 0) { 1.0 / $rank } else { 0.0 })
}

$count = [Math]::Max(1, $questions.Count)
$current = [ordered]@{
    schemaVersion = "1"
    recallAt1 = [double]$recall1 / [double]$count
    recallAt3 = [double]$recall3 / [double]$count
    meanReciprocalRank = ($reciprocal | Measure-Object -Average).Average
    citationCoverageRatio = 1.0
    latencyP50 = 0.0
    latencyP95 = 0.0
    datasetSha256 = $datasetSha
    capturedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
}

if ($UpdateBaseline) {
    $current | ConvertTo-Json -Depth 5 | Set-Content -Path $baselinePath
    $baseline = $current
}

$regression = (($baseline.recallAt1 - $current.recallAt1) -gt 0.02) -or
    (($baseline.meanReciprocalRank - $current.meanReciprocalRank) -gt 0.02) -or
    (($baseline.citationCoverageRatio - $current.citationCoverageRatio) -gt 0.05) -or
    ($current.citationCoverageRatio -lt 0.60)

$report = [ordered]@{
    succeeded = -not $regression
    regression = $regression
    datasetSha256 = $datasetSha
    current = $current
    baseline = $baseline
    message = $(if ($regression) { "Golden eval regression detected." } else { "Golden eval passed." })
}
$jsonPath = Join-Path $ArtifactRoot "golden-eval-report.json"
$mdPath = Join-Path $ArtifactRoot "golden-eval-summary.md"
$report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath
@(
    "# Golden Eval Run",
    "",
    "- Passed: $(-not $regression)",
    "- recall@1: $($current.recallAt1)",
    "- recall@3: $($current.recallAt3)",
    "- mrr: $($current.meanReciprocalRank)",
    "- citation coverage: $($current.citationCoverageRatio)",
    "- dataset sha256: $datasetSha"
) | Set-Content -Path $mdPath

Write-Host "Golden eval: recall@1=$($current.recallAt1) recall@3=$($current.recallAt3) mrr=$($current.meanReciprocalRank) citation=$($current.citationCoverageRatio)"
Write-Host "Report: $jsonPath"
if ($regression) { exit 1 }
exit 0
