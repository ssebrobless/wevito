param(
    [string]$Species,
    [string]$Age,
    [string]$Gender,
    [string]$Family = "",
    [string]$BoardPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Species) -or [string]::IsNullOrWhiteSpace($Age) -or [string]::IsNullOrWhiteSpace($Gender)) {
    throw "Specify -Species, -Age, and -Gender."
}

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$SaveDir = if ([string]::IsNullOrWhiteSpace($Family)) {
    Join-Path $ProjectRoot "incoming_sprites\gemini_handoff\$Species\$Age\$Gender\5-save-edited-board-here"
}
else {
    Join-Path $ProjectRoot "incoming_sprites\gemini_handoff_motion\$Species\$Age\$Gender\$Family\5-save-edited-board-here"
}

if ([string]::IsNullOrWhiteSpace($BoardPath)) {
    $board = Get-ChildItem -Path $SaveDir -File -Filter "*.png" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $board) {
        throw "No PNG board found in $SaveDir"
    }
    $BoardPath = $board.FullName
}

$OutputDir = Join-Path $ProjectRoot "sprites_authored_verified\$Species\$Age\$Gender\blue"
$ImportScript = if ([string]::IsNullOrWhiteSpace($Family)) {
    Join-Path $ProjectRoot "tools\import_authored_animation_board.py"
}
else {
    Join-Path $ProjectRoot "tools\import_authored_family_board.py"
}
$PropagateScript = Join-Path $ProjectRoot "tools\propagate_authored_colors.py"
$ExtractBoardScript = Join-Path $ProjectRoot "tools\extract_board_from_gemini_pack.py"
$AuthoredRoot = Join-Path $ProjectRoot "sprites_authored_verified"
$VariantSourceDir = if ([string]::IsNullOrWhiteSpace($Family)) {
    Join-Path $ProjectRoot "vnext\artifacts\authoring-packs\$Species\$Age\$Gender"
}
else {
    Join-Path $ProjectRoot "incoming_sprites\gemini_handoff_motion\$Species\$Age\$Gender\$Family"
}

$boardItem = Get-Item -LiteralPath $BoardPath
if ($boardItem.Length -gt 1000000) {
    $boardImageName = if ([string]::IsNullOrWhiteSpace($Family)) {
        "editable-board.png"
    }
    else {
        "$Family-editable-board.png"
    }
    if (-not (Test-Path (Join-Path $VariantSourceDir $boardImageName))) {
        throw "Missing source board image for extraction: $(Join-Path $VariantSourceDir $boardImageName)"
    }
    $extractSlug = if ([string]::IsNullOrWhiteSpace($Family)) {
        "$Species-$Age-$Gender-extracted-board-import.png"
    }
    else {
        "$Species-$Age-$Gender-$Family-extracted-board-import.png"
    }
    $extractedBoardPath = Join-Path $SaveDir $extractSlug
    if (Test-Path $extractedBoardPath) {
        Remove-Item -LiteralPath $extractedBoardPath -Force
    }
    python $ExtractBoardScript --result-pack $BoardPath --source-dir $VariantSourceDir --output-board $extractedBoardPath --board-image-name $boardImageName
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $extractedBoardPath)) {
        throw "Failed to extract editable board from Gemini result pack: $BoardPath"
    }
    $BoardPath = $extractedBoardPath
}

if ([string]::IsNullOrWhiteSpace($Family)) {
    python $ImportScript --board $BoardPath --output-dir $OutputDir --species $Species --age-stage $Age
}
else {
    python $ImportScript --board $BoardPath --output-dir $OutputDir --species $Species --age-stage $Age --family $Family
}
python $PropagateScript --source-dir $OutputDir --output-root $AuthoredRoot

Write-Host "Imported Gemini board:"
Write-Host $BoardPath
Write-Host ""
Write-Host "Authored output root:"
Write-Host $OutputDir
