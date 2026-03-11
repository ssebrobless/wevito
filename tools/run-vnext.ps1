param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-vnext.ps1"
$ShellExe = Join-Path $ProjectRoot "vnext\artifacts\shell\Wevito.VNext.Shell.exe"

& powershell -NoProfile -ExecutionPolicy Bypass -File $BuildScript -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "vNext build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $ShellExe)) {
    throw "Missing published shell executable: $ShellExe"
}

Start-Process -FilePath $ShellExe | Out-Null
