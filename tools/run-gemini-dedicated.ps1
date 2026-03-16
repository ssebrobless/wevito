param(
    [string]$Species = "rat",
    [string]$Age = "adult",
    [string]$Gender = "male",
    [int]$Port = 9333,
    [switch]$OpenBrowser,
    [switch]$SetupOnly,
    [switch]$SendOnly,
    [switch]$KeepOpen
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LaunchScript = Join-Path $ProjectRoot "tools\launch-gemini-debug-browser.ps1"
$RunScript = Join-Path $ProjectRoot "tools\run-gemini-automation.ps1"
$CdpUrl = "http://127.0.0.1:$Port"

if ($OpenBrowser) {
    powershell -ExecutionPolicy Bypass -File $LaunchScript -Port $Port -ReuseIfRunning
}

$ArgsList = @(
    "-ExecutionPolicy", "Bypass",
    "-File", $RunScript,
    "-Species", $Species,
    "-Age", $Age,
    "-Gender", $Gender,
    "-CdpUrl", $CdpUrl
)

if ($SetupOnly) {
    $ArgsList += "-SetupOnly"
}
if ($SendOnly) {
    $ArgsList += "-SendOnly"
}
if ($KeepOpen) {
    $ArgsList += "-KeepOpen"
}

powershell @ArgsList
