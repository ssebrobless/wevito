param(
    [string]$ModelUrl = "",
    [string]$ExpectedSha256 = "",
    [string]$TokenizerJsonUrl = "",
    [string]$ExpectedTokenizerJsonSha256 = "",
    [string]$VocabUrl = "",
    [string]$ExpectedVocabSha256 = "",
    [string]$OutputRoot = ".\vnext\content\local-models\embeddings\bge-micro-v2",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Download-VerifiedFile {
    param(
        [Parameter(Mandatory=$true)][string]$Url,
        [Parameter(Mandatory=$true)][string]$Destination,
        [Parameter(Mandatory=$true)][string]$ExpectedSha256
    )

    if ([string]::IsNullOrWhiteSpace($ExpectedSha256)) {
        throw "Expected SHA256 is required for $Destination."
    }

    $tmpPath = "$Destination.download"
    Invoke-WebRequest -Uri $Url -OutFile $tmpPath
    $actualSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $tmpPath).Hash.ToLowerInvariant()
    if ($actualSha -ne $ExpectedSha256.ToLowerInvariant()) {
        Remove-Item -LiteralPath $tmpPath -Force
        throw "Downloaded file hash mismatch for $Destination. Expected $ExpectedSha256 but got $actualSha."
    }

    Move-Item -LiteralPath $tmpPath -Destination $Destination -Force
    return $actualSha
}

if ([string]::IsNullOrWhiteSpace($ModelUrl)) {
    Write-Host "No model URL was provided."
    Write-Host "Run again with -ModelUrl and -ExpectedSha256 after choosing an approved bge-micro-v2 ONNX source."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($ExpectedSha256)) {
    Write-Host "ExpectedSha256 is required so Wevito can verify the downloaded model bytes."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($TokenizerJsonUrl) -and [string]::IsNullOrWhiteSpace($VocabUrl)) {
    Write-Host "TokenizerJsonUrl or VocabUrl is required so Wevito can encode text locally."
    exit 1
}

$resolvedRoot = Resolve-Path -LiteralPath "." | Select-Object -ExpandProperty Path
$targetRoot = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    [System.IO.Path]::GetFullPath($OutputRoot)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot $OutputRoot))
}
$modelPath = Join-Path $targetRoot "model.onnx"
$tokenizerJsonPath = Join-Path $targetRoot "tokenizer.json"
$vocabPath = Join-Path $targetRoot "vocab.txt"

if (((Test-Path -LiteralPath $modelPath) -or (Test-Path -LiteralPath $tokenizerJsonPath) -or (Test-Path -LiteralPath $vocabPath)) -and -not $Force) {
    Write-Host "Local embedder files already exist at $targetRoot. Use -Force to replace them."
    exit 1
}

Write-Host "This will download a local ONNX embedding model into:"
Write-Host "  $targetRoot"
Write-Host ""
Write-Host "Wevito will not use hosted AI for this. The file must match:"
Write-Host "  model.onnx: $ExpectedSha256"
if (-not [string]::IsNullOrWhiteSpace($TokenizerJsonUrl)) {
    Write-Host "  tokenizer.json: $ExpectedTokenizerJsonSha256"
}
if (-not [string]::IsNullOrWhiteSpace($VocabUrl)) {
    Write-Host "  vocab.txt: $ExpectedVocabSha256"
}
Write-Host ""
$confirmation = Read-Host "Type INSTALL to continue"
if ($confirmation -ne "INSTALL") {
    Write-Host "Install cancelled."
    exit 1
}

New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null
$actualModelSha = Download-VerifiedFile -Url $ModelUrl -Destination $modelPath -ExpectedSha256 $ExpectedSha256
$actualTokenizerSha = ""
$actualVocabSha = ""
if (-not [string]::IsNullOrWhiteSpace($TokenizerJsonUrl)) {
    $actualTokenizerSha = Download-VerifiedFile -Url $TokenizerJsonUrl -Destination $tokenizerJsonPath -ExpectedSha256 $ExpectedTokenizerJsonSha256
}
if (-not [string]::IsNullOrWhiteSpace($VocabUrl)) {
    $actualVocabSha = Download-VerifiedFile -Url $VocabUrl -Destination $vocabPath -ExpectedSha256 $ExpectedVocabSha256
}

$manifest = [ordered]@{
    schemaVersion = "1"
    modelId = "bge-micro-v2"
    modelPath = $modelPath
    tokenizerJsonPath = if ($actualTokenizerSha.Length -gt 0) { $tokenizerJsonPath } else { "" }
    vocabPath = if ($actualVocabSha.Length -gt 0) { $vocabPath } else { "" }
    sourceUrl = $ModelUrl
    tokenizerJsonUrl = $TokenizerJsonUrl
    vocabUrl = $VocabUrl
    sha256 = $actualModelSha
    tokenizerJsonSha256 = $actualTokenizerSha
    vocabSha256 = $actualVocabSha
    installedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    hostedAi = $false
    silentDownload = $false
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $targetRoot "manifest.json") -Encoding UTF8
Write-Host "Local embedder installed and verified."
