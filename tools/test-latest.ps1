param(
    [string]$Version = "dev",
    [string]$GodotPath = "",
    [switch]$Debug
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-release.ps1"
$LatestExe = Join-Path $ProjectRoot "builds\release\WevitoDesktopPet-latest-win64.exe"

if (-not (Test-Path $BuildScript)) {
    throw "Missing build script: $BuildScript"
}

Write-Host "Building latest test executable..."

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

if (-not (Test-Path $LatestExe)) {
    throw "Expected latest executable not found: $LatestExe"
}

Write-Host "Launching latest build..."
Start-Process -FilePath $LatestExe | Out-Null
