param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string[]]$Species = @("rat", "crow", "fox", "snake", "deer", "frog", "pigeon", "raccoon", "squirrel", "goose"),
    [ValidateSet("Baby", "Teen", "Adult")]
    [string]$Age = "Adult",
    [ValidateSet("Female", "Male")]
    [string]$Gender = "Female",
    [ValidateSet("red", "orange", "yellow", "blue", "indigo", "violet")]
    [string]$Color = "violet",
    [ValidateSet("Idle", "Walk", "Eat", "Happy", "Sad", "Sleep", "Sick", "Bathe")]
    [string]$Animation = "Walk",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-vnext.ps1"
$CaptureScript = Join-Path $ProjectRoot "tools\capture-vnext-visuals.ps1"
$AuditRoot = Join-Path $ProjectRoot "vnext\artifacts\sprite-audit-runs"
$ScenarioRoot = Join-Path $ProjectRoot "vnext\artifacts\debug\sprite-audit"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$RunDir = Join-Path $AuditRoot $RunStamp

New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
New-Item -ItemType Directory -Force -Path $ScenarioRoot | Out-Null

if (-not $SkipBuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $BuildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "vNext build failed with exit code $LASTEXITCODE"
    }
}

$results = New-Object System.Collections.Generic.List[object]
$first = $true

foreach ($speciesId in $Species) {
    $scenarioPath = Join-Path $ScenarioRoot "$speciesId-$Age-$Gender-$Color-$Animation.json"
    $scenario = [ordered]@{
        mode = "Focused"
        activeEnvironmentId = $speciesId
        clearBasket = $true
        settingsSnapshot = [ordered]@{
            show_pet_names = "false"
            show_status_summary = "true"
        }
        pets = @(
            [ordered]@{
                speciesId = $speciesId
                ageStage = $Age
                gender = $Gender
                colorVariant = $Color
                animationState = $Animation
                name = "$speciesId audit"
            }
        )
    }

    $scenario | ConvertTo-Json -Depth 8 | Set-Content -Path $scenarioPath -Encoding UTF8

    $command = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $CaptureScript,
        "-Configuration", $Configuration,
        "-ScenarioPath", $scenarioPath,
        "-SkipBasket",
        "-SkipDevTools"
    )
    if ($SkipBuild -or -not $first) {
        $command += "-SkipBuild"
    }

    $output = & powershell @command
    $summaryLine = $output | Where-Object { $_ -like "summary=*" } | Select-Object -Last 1
    if (-not $summaryLine) {
        throw "Capture script did not produce a summary line for species '$speciesId'."
    }

    $summaryPath = $summaryLine.Substring("summary=".Length)
    $summary = Get-Content -Raw $summaryPath | ConvertFrom-Json
    $results.Add([ordered]@{
        species = $speciesId
        scenario_path = $scenarioPath
        screenshot_summary = $summaryPath
        focused_home = $summary.screenshots.focused_home
        passive_roam_band = $summary.screenshots.passive_roam_band
        sprite_preview_index = $summary.sprite_previews.index
    }) | Out-Null

    $first = $false
}

$summaryOutput = [ordered]@{
    captured_at = (Get-Date).ToString("s")
    configuration = $Configuration
    age = $Age
    gender = $Gender
    color = $Color
    animation = $Animation
    species = $Species
    results = $results
}

$summaryFile = Join-Path $RunDir "summary.json"
$summaryOutput | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryFile -Encoding UTF8
Write-Host "summary=$summaryFile"
