param(
    [string]$ExePath = "",
    [int]$RunSeconds = 20
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$DefaultExe = Join-Path $ProjectRoot "builds\release\WevitoDesktopPet-latest-win64.exe"
if ($ExePath -eq "") {
    $ExePath = $DefaultExe
}

if (-not (Test-Path $ExePath)) {
    throw "Missing executable: $ExePath`nBuild first with tools\build-release.ps1."
}

function New-LegacySaveJson {
    return @{
        pets = @(
            @{
                name = "Legacy"
                animal_type = "rat"
                egg_color = "blue"
                gender = "male"
                age_minutes = 5
                hunger = 85
                hydration = 70
                happiness = 90
                energy = 88
                health = 92
                cleanliness = 75
                affection = 80
                grooming = 72
                fitness = 67
                conditions = @{ respiratoryProblems = 1 }
                is_sleeping = $false
                is_hatching = $false
                emotion = "happy"
                position = @{ x = 120; y = 280 }
                personality = @{
                    food_love = 5
                    cuddle_need = 10
                    pet_cleanliness = -2
                    activity_level = 12
                    cheerfulness = 4
                    social_need = 0
                    playfulness = 8
                    stubbornness = -6
                }
            }
        )
        settings = @{
            sound_effects = $true
            click_through = $false
        }
    } | ConvertTo-Json -Depth 8 -Compress
}

function Get-ProfilePaths {
    param([string]$AppDataRoot)

    $userDataRoot = Join-Path $AppDataRoot "Godot\app_userdata\Wevito"
    return @{
        UserDataRoot = $userDataRoot
        SavePath = Join-Path $userDataRoot "save_slot.json"
        ReportPath = Join-Path $userDataRoot "automation_report.json"
        LogPath = Join-Path $userDataRoot "logs\godot.log"
    }
}

function Invoke-StandaloneScenario {
    param(
        [string]$ScenarioName,
        [scriptblock]$SetupScript
    )

    $scenarioRoot = Join-Path $env:TEMP ("wevito-smoke-" + $ScenarioName + "-" + [guid]::NewGuid().ToString("N"))
    $appDataRoot = Join-Path $scenarioRoot "AppData\Roaming"
    New-Item -ItemType Directory -Path $appDataRoot -Force | Out-Null

    $paths = Get-ProfilePaths -AppDataRoot $appDataRoot
    New-Item -ItemType Directory -Path $paths.UserDataRoot -Force | Out-Null

    if ($SetupScript) {
        & $SetupScript $paths
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $ExePath
    $psi.WorkingDirectory = Split-Path -Parent $ExePath
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["APPDATA"] = $appDataRoot
    $psi.EnvironmentVariables["WEVITO_AUTOMATION"] = "1"
    $psi.EnvironmentVariables["WEVITO_AUTOMATION_SCENARIO"] = $ScenarioName
    $process = [System.Diagnostics.Process]::Start($psi)

    $completedInTime = $process.WaitForExit($RunSeconds * 1000)
    if (-not $completedInTime) {
        $process.Kill()
        $process.WaitForExit()
    }

    $logText = ""
    if (Test-Path $paths.LogPath) {
        $logText = [System.IO.File]::ReadAllText($paths.LogPath)
    }
    $errorLines = @()
    if ($logText -ne "") {
        $errorLines = ($logText -split "`0|`r|`n") | Where-Object {
            ($_ -match "SCRIPT ERROR|ERROR:") -and ($_ -notmatch "ObjectDB instances leaked")
        }
    }

    $reportJson = $null
    if (Test-Path $paths.ReportPath) {
        $reportRaw = Get-Content -Path $paths.ReportPath -Raw
        if ($reportRaw.Trim() -ne "") {
            $reportJson = $reportRaw | ConvertFrom-Json
        }
    }

    $saveJson = $null
    if (Test-Path $paths.SavePath) {
        $saveRaw = Get-Content -Path $paths.SavePath -Raw
        if ($saveRaw.Trim() -ne "") {
            $saveJson = $saveRaw | ConvertFrom-Json
        }
    }

    [PSCustomObject]@{
        Scenario = $ScenarioName
        AppDataRoot = $appDataRoot
        CompletedInTime = $completedInTime
        ExitCode = $process.ExitCode
        SaveExists = Test-Path $paths.SavePath
        ReportExists = Test-Path $paths.ReportPath
        ReportJson = $reportJson
        SaveJson = $saveJson
        ErrorLines = $errorLines
        MigratedLegacyFields = (
            $null -ne $saveJson -and
            $null -ne ($saveJson.pets | Select-Object -First 1).target_position -and
            $null -ne ($saveJson.pets | Select-Object -First 1).active_treatments -and
            $null -ne $saveJson.settings.experimental_monitor_roam
        )
    }
}

$results = @()

$results += Invoke-StandaloneScenario -ScenarioName "fresh" -SetupScript {
    param($paths)
}

$results += Invoke-StandaloneScenario -ScenarioName "legacy" -SetupScript {
    param($paths)
    Set-Content -Path $paths.SavePath -Value (New-LegacySaveJson) -Encoding UTF8
}

foreach ($result in $results) {
    Write-Host ""
    Write-Host ("Scenario: " + $result.Scenario)
    Write-Host ("- Completed within {0}s: {1}" -f $RunSeconds, $result.CompletedInTime)
    Write-Host ("- Exit code: {0}" -f $result.ExitCode)
    Write-Host ("- Report exists: {0}" -f $result.ReportExists)
    if ($null -ne $result.ReportJson) {
        Write-Host ("- Report passed: {0}" -f $result.ReportJson.passed)
    }
    Write-Host ("- Save exists: " + $result.SaveExists)
    if ($result.ErrorLines.Count -gt 0) {
        Write-Host "- Errors:"
        foreach ($line in $result.ErrorLines) {
            Write-Host ("  " + $line)
        }
    }
    else {
        Write-Host "- Errors: none"
    }

    if ($result.Scenario -eq "legacy" -and $null -ne $result.SaveJson) {
        $pet = $result.SaveJson.pets | Select-Object -First 1
        $hasTargetPosition = ($null -ne $pet.target_position)
        $hasTreatments = ($null -ne $pet.active_treatments)
        $hasSettings = ($null -ne $result.SaveJson.settings.experimental_monitor_roam)
        Write-Host ("- Migrated fields: target_position={0}, active_treatments={1}, experimental_monitor_roam={2}" -f $hasTargetPosition, $hasTreatments, $hasSettings)
    }
}

$failed = @(
    foreach ($result in $results) {
        $reportPassed = $false
        if ($null -ne $result.ReportJson) {
            $reportPassed = [bool]$result.ReportJson.passed
        }

        if (
            (-not $result.CompletedInTime) -or
            ($result.ExitCode -ne 0) -or
            (-not $result.ReportExists) -or
            ($null -eq $result.ReportJson) -or
            (-not $reportPassed) -or
            (-not $result.SaveExists) -or
            ($result.Scenario -eq "legacy" -and -not $result.MigratedLegacyFields) -or
            ($result.ErrorLines.Count -gt 0)
        ) {
            $result
        }
    }
)

if ($failed.Count -gt 0) {
    throw "Standalone smoke test failed."
}

Write-Host ""
Write-Host "Standalone smoke test passed."
