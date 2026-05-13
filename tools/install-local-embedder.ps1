param(
    [string]$ModelUrl = "",
    [string]$ExpectedSha256 = "",
    [string]$OutputRoot = ".\vnext\content\local-models\embeddings\bge-micro-v2",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ModelUrl)) {
    Write-Host "No model URL was provided."
    Write-Host "Run again with -ModelUrl and -ExpectedSha256 after choosing an approved bge-micro-v2 ONNX source."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($ExpectedSha256)) {
    Write-Host "ExpectedSha256 is required so Wevito can verify the downloaded model bytes."
    exit 1
}

$resolvedRoot = Resolve-Path -LiteralPath "." | Select-Object -ExpandProperty Path
$targetRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot $OutputRoot))
$modelPath = Join-Path $targetRoot "model.onnx"

if ((Test-Path -LiteralPath $modelPath) -and -not $Force) {
    Write-Host "Model already exists at $modelPath. Use -Force to replace it."
    exit 1
}

Write-Host "This will download a local ONNX embedding model into:"
Write-Host "  $targetRoot"
Write-Host ""
Write-Host "Wevito will not use hosted AI for this. The file must match:"
Write-Host "  $ExpectedSha256"
Write-Host ""
$confirmation = Read-Host "Type INSTALL to continue"
if ($confirmation -ne "INSTALL") {
    Write-Host "Install cancelled."
    exit 1
}

New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null
$tmpPath = "$modelPath.download"
Invoke-WebRequest -Uri $ModelUrl -OutFile $tmpPath

$actualSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $tmpPath).Hash.ToLowerInvariant()
if ($actualSha -ne $ExpectedSha256.ToLowerInvariant()) {
    Remove-Item -LiteralPath $tmpPath -Force
    throw "Downloaded model hash mismatch. Expected $ExpectedSha256 but got $actualSha."
}

Move-Item -LiteralPath $tmpPath -Destination $modelPath -Force
$manifest = [ordered]@{
    schemaVersion = "1"
    modelId = "bge-micro-v2"
    modelPath = $modelPath
    sourceUrl = $ModelUrl
    sha256 = $actualSha
    installedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    hostedAi = $false
    silentDownload = $false
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $targetRoot "manifest.json") -Encoding UTF8
Write-Host "Local embedder installed and verified."
