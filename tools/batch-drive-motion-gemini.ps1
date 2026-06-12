# C-PHASE 203B round-trip driver: walks the staged motion-family handoff packs
# (incoming_sprites\gemini_handoff_motion\<species>\<age>\<gender>\<family>) and
# drives each board through the user's already-open logged-in Gemini window via
# drive-live-gemini.ps1 -Family -SkipPrepare. Skips packs that already have a
# saved result, records per-pack status JSONL, and continues past failures so a
# rerun picks up only what is missing.
param(
    [string[]]$Species = @(),
    [string[]]$Families = @(),
    [int]$GenerationTimeoutSeconds = 360,
    [int]$MaxPacks = 0,
    [switch]$ListOnly
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$HandoffRoot = Join-Path $ProjectRoot "incoming_sprites\gemini_handoff_motion"
$DriveScript = Join-Path $ProjectRoot "tools\drive-live-gemini.ps1"
$StatusRoot = Join-Path $ProjectRoot "vnext\artifacts\gemini-motion-batch"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$StatusDir = Join-Path $StatusRoot $RunStamp
$StatusPath = Join-Path $StatusDir "status.jsonl"

# Default session scope = the C-202 + C-203B staged lanes (Amendment R2).
$LaneSpecies = [ordered]@{
    "goose"    = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b")
    "raccoon"  = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b")
    "squirrel" = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b")
    "deer"     = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b", "care_eat", "expression_happy")
    "fox"      = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b", "care_eat", "expression_happy")
    "frog"     = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b", "care_eat", "expression_happy")
    "pigeon"   = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b", "care_eat", "expression_happy")
    "snake"    = @("locomotion_idle", "locomotion_walk_a", "locomotion_walk_b", "care_eat", "expression_happy")
}

$packs = @()
foreach ($speciesName in $LaneSpecies.Keys) {
    if ($Species.Count -gt 0 -and $Species -notcontains $speciesName) { continue }
    foreach ($family in $LaneSpecies[$speciesName]) {
        if ($Families.Count -gt 0 -and $Families -notcontains $family) { continue }
        foreach ($age in @("baby", "teen", "adult")) {
            foreach ($gender in @("male", "female")) {
                $packDir = Join-Path $HandoffRoot "$speciesName\$age\$gender\$family"
                if (-not (Test-Path (Join-Path $packDir "4-prompt.txt"))) { continue }
                $packs += [pscustomobject]@{
                    species = $speciesName
                    age = $age
                    gender = $gender
                    family = $family
                    slug = "$speciesName-$age-$gender-$family"
                    dir = $packDir
                }
            }
        }
    }
}

if ($ListOnly) {
    $packs | ForEach-Object { Write-Host $_.slug }
    Write-Host "total: $($packs.Count)"
    exit 0
}

New-Item -ItemType Directory -Force -Path $StatusDir | Out-Null
Write-Host "Driving $($packs.Count) motion packs; status -> $StatusPath"

$done = 0
$failed = 0
$skipped = 0
foreach ($pack in $packs) {
    if ($MaxPacks -gt 0 -and ($done + $failed) -ge $MaxPacks) { break }
    $saveDir = Join-Path $pack.dir "5-save-edited-board-here"
    $existing = Get-ChildItem -Path $saveDir -File -Filter "*.png" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($existing) {
        $skipped++
        Add-Content -Path $StatusPath -Value ([ordered]@{ pack = $pack.slug; status = "skipped_existing"; timestamp = (Get-Date).ToString("o"); path = $existing.FullName } | ConvertTo-Json -Compress)
        Write-Host "[skip] $($pack.slug) (result exists)"
        continue
    }

    Write-Host ""
    Write-Host "=== [$($done + $failed + $skipped + 1)/$($packs.Count)] $($pack.slug) ==="
    try {
        powershell -NoProfile -ExecutionPolicy Bypass -File $DriveScript `
            -Species $pack.species -Age $pack.age -Gender $pack.gender -Family $pack.family `
            -GenerationTimeoutSeconds $GenerationTimeoutSeconds -SkipPrepare
        if ($LASTEXITCODE -ne 0) { throw "drive-live-gemini exited $LASTEXITCODE" }
        $result = Get-ChildItem -Path $saveDir -File -Filter "*.png" -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $result) { throw "driver reported success but no result PNG found" }
        $done++
        Add-Content -Path $StatusPath -Value ([ordered]@{ pack = $pack.slug; status = "done"; timestamp = (Get-Date).ToString("o"); path = $result.FullName } | ConvertTo-Json -Compress)
    }
    catch {
        $failed++
        Add-Content -Path $StatusPath -Value ([ordered]@{ pack = $pack.slug; status = "failed"; timestamp = (Get-Date).ToString("o"); error = $_.Exception.Message } | ConvertTo-Json -Compress)
        Write-Warning "[fail] $($pack.slug): $($_.Exception.Message)"
        Start-Sleep -Seconds 5
    }
}

Write-Host ""
Write-Host "Batch complete: done=$done failed=$failed skipped=$skipped of $($packs.Count)"
if ($failed -gt 0) { exit 1 }
