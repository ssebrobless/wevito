param(
    [string]$Species = "",
    [string]$StartAt = "",
    [int]$GenerationTimeoutSeconds = 360,
    [switch]$SkipExisting,
    [switch]$ImportAfterEach,
    [switch]$PrepareOnly
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ManifestPath = Join-Path $ProjectRoot "tools\incoming_animal_pose_manifest.json"
$ExportAllScript = Join-Path $ProjectRoot "tools\export_all_authoring_packs.py"
$PrepareAllScript = Join-Path $ProjectRoot "tools\prepare_all_gemini_handoffs.py"
$DriveScript = Join-Path $ProjectRoot "tools\drive-live-gemini.ps1"
$ImportScript = Join-Path $ProjectRoot "tools\import-gemini-result.ps1"
$StatusRoot = Join-Path $ProjectRoot "vnext\artifacts\gemini-batch"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$StatusDir = Join-Path $StatusRoot $RunStamp
$StatusPath = Join-Path $StatusDir "status.jsonl"

New-Item -ItemType Directory -Force -Path $StatusDir | Out-Null

python $ExportAllScript
python $PrepareAllScript

if ($PrepareOnly) {
    Write-Host "Prepared all authoring packs and Gemini handoffs."
    exit 0
}

$entries = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
$variants = foreach ($entry in $entries) {
    foreach ($gender in $entry.component_order) {
        [pscustomobject]@{
            species = $entry.species
            age = $entry.age_stage
            gender = $gender
            slug = "$($entry.species)-$($entry.age_stage)-$gender"
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($Species)) {
    $variants = $variants | Where-Object { $_.species -eq $Species }
}

if (-not [string]::IsNullOrWhiteSpace($StartAt)) {
    $startIndex = ($variants | ForEach-Object slug).IndexOf($StartAt)
    if ($startIndex -lt 0) {
        throw "StartAt variant not found: $StartAt"
    }
    $variants = $variants[$startIndex..($variants.Count - 1)]
}

foreach ($variant in $variants) {
    $saveDir = Join-Path $ProjectRoot "incoming_sprites\gemini_handoff\$($variant.species)\$($variant.age)\$($variant.gender)\5-save-edited-board-here"
    $existing = Get-ChildItem -Path $saveDir -File -Filter "*.png" -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($SkipExisting -and $existing) {
        $record = [ordered]@{
            variant = $variant.slug
            status = "skipped_existing"
            timestamp = (Get-Date).ToString("o")
            path = $existing.FullName
        } | ConvertTo-Json -Compress
        Add-Content -Path $StatusPath -Value $record
        Write-Host "Skipping existing result for $($variant.slug)"
        continue
    }

    Write-Host ""
    Write-Host "=== Running $($variant.slug) ==="
    try {
        powershell -ExecutionPolicy Bypass -File $DriveScript `
            -Species $variant.species `
            -Age $variant.age `
            -Gender $variant.gender `
            -GenerationTimeoutSeconds $GenerationTimeoutSeconds

        $latest = Get-ChildItem -Path $saveDir -File -Filter "*.png" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if (-not $latest) {
            throw "No Gemini PNG result found after generation."
        }

        if ($ImportAfterEach) {
            powershell -ExecutionPolicy Bypass -File $ImportScript `
                -Species $variant.species `
                -Age $variant.age `
                -Gender $variant.gender `
                -BoardPath $latest.FullName
        }

        $record = [ordered]@{
            variant = $variant.slug
            status = "completed"
            timestamp = (Get-Date).ToString("o")
            path = $latest.FullName
            imported = [bool]$ImportAfterEach
        } | ConvertTo-Json -Compress
        Add-Content -Path $StatusPath -Value $record
    }
    catch {
        $record = [ordered]@{
            variant = $variant.slug
            status = "failed"
            timestamp = (Get-Date).ToString("o")
            error = $_.Exception.Message
        } | ConvertTo-Json -Compress
        Add-Content -Path $StatusPath -Value $record
        throw
    }
}

Write-Host ""
Write-Host "Batch complete. Status log:"
Write-Host $StatusPath
