param(
    [string]$Version = "dev",
    [string]$GodotPath = "",
    [switch]$Debug,
    [switch]$ForceRebuild
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-release.ps1"
$LatestExe = Join-Path $ProjectRoot "builds\release\WevitoDesktopPet-latest-win64.exe"
$BuildInfo = Join-Path $ProjectRoot "builds\release\last-build.txt"

if (-not (Test-Path $BuildScript)) {
    throw "Missing build script: $BuildScript"
}

function Get-LatestSourceWriteTime {
    param([string]$Root)

    $pathsToScan = @(
        (Join-Path $Root "project.godot"),
        (Join-Path $Root "export_presets.cfg"),
        (Join-Path $Root "scripts"),
        (Join-Path $Root "scenes"),
        (Join-Path $Root "sprites"),
        (Join-Path $Root "sounds"),
        (Join-Path $Root "tools\build-desktop-bridge.ps1"),
        (Join-Path $Root "tools\desktop_bridge")
    )

    $latest = [datetime]::MinValue

    foreach ($path in $pathsToScan) {
        if (-not (Test-Path $path)) {
            continue
        }

        $item = Get-Item $path
        if (-not $item.PSIsContainer) {
            if ($item.LastWriteTime -gt $latest) {
                $latest = $item.LastWriteTime
            }
            continue
        }

        $files = Get-ChildItem -Path $path -File -Recurse -ErrorAction SilentlyContinue
        foreach ($f in $files) {
            if ($f.LastWriteTime -gt $latest) {
                $latest = $f.LastWriteTime
            }
        }
    }

    return $latest
}

$needsBuild = $ForceRebuild -or -not (Test-Path $LatestExe)

if (-not $needsBuild) {
    $latestSourceTime = Get-LatestSourceWriteTime -Root $ProjectRoot
    $latestExeTime = (Get-Item $LatestExe).LastWriteTime
    $needsBuild = $latestSourceTime -gt $latestExeTime
}

if ($needsBuild) {
    Write-Host "Detected project changes. Building latest executable..."

    $buildArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $BuildScript, "-Version", $Version)
    if ($GodotPath -ne "") {
        $buildArgs += @("-GodotPath", $GodotPath)
    }
    if ($Debug) {
        $buildArgs += "-Debug"
    }

    & powershell @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Build step failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $LatestExe) -and -not (Test-Path $BuildInfo)) {
    throw "Missing latest build: $LatestExe"
}

$LaunchExe = $LatestExe
if (Test-Path $BuildInfo) {
    $infoMap = @{}
    Get-Content -Path $BuildInfo | ForEach-Object {
        $parts = $_.Split("=", 2)
        if ($parts.Count -eq 2) {
            $infoMap[$parts[0]] = $parts[1]
        }
    }

    if ($infoMap.ContainsKey("latest_available") -and (Test-Path $infoMap["latest_available"])) {
        $LaunchExe = $infoMap["latest_available"]
    }
    elseif ($infoMap.ContainsKey("output") -and (Test-Path $infoMap["output"])) {
        $LaunchExe = $infoMap["output"]
    }
}

if (-not (Test-Path $LaunchExe)) {
    throw "No runnable build output found."
}

Write-Host "Launching latest build..."
Start-Process -FilePath $LaunchExe | Out-Null
