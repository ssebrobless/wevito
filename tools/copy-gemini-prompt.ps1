param(
    [string]$Batch = "",
    [switch]$List,
    [switch]$OpenGemini
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PromptRoot = Join-Path $ProjectRoot "prompts\gemini"
$SharedSpecPath = Join-Path $PromptRoot "shared_spec.txt"

$BatchMap = [ordered]@{
    "icons"       = "01-icons-body.txt"
    "rat"         = "02-rat-body.txt"
    "crow"        = "03-crow-body.txt"
    "fox"         = "04-fox-body.txt"
    "snake"       = "05-snake-body.txt"
    "deer"        = "06-deer-body.txt"
    "frog"        = "07-frog-body.txt"
    "pigeon"      = "08-pigeon-body.txt"
    "raccoon"     = "09-raccoon-body.txt"
    "squirrel"    = "10-squirrel-body.txt"
    "goose"       = "11-goose-body.txt"
    "consistency" = "12-consistency-pass-body.txt"
}

if ($List) {
    Write-Host "Available Gemini prompt batches:"
    foreach ($entry in $BatchMap.GetEnumerator()) {
        Write-Host ("- " + $entry.Key)
    }
    exit 0
}

if ($Batch -eq "") {
    throw "Specify -Batch <name> or use -List."
}

$BatchKey = $Batch.Trim().ToLowerInvariant()
if (-not $BatchMap.Contains($BatchKey)) {
    throw "Unknown batch '$Batch'. Use -List to see valid names."
}

$BodyPath = Join-Path $PromptRoot $BatchMap[$BatchKey]
if (-not (Test-Path $SharedSpecPath)) {
    throw "Missing shared spec file: $SharedSpecPath"
}
if (-not (Test-Path $BodyPath)) {
    throw "Missing batch prompt file: $BodyPath"
}

$SharedSpec = Get-Content -Path $SharedSpecPath -Raw
$Body = Get-Content -Path $BodyPath -Raw
$PromptText = ($SharedSpec.TrimEnd() + [Environment]::NewLine + [Environment]::NewLine + $Body.TrimStart())

Set-Clipboard -Value $PromptText

Write-Host ("Copied Gemini prompt to clipboard: " + $BatchKey)
Write-Host ("Source: " + $BodyPath)

if ($OpenGemini) {
    Start-Process "https://gemini.google.com/"
}
