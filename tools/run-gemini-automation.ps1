param(
    [string]$Species = "rat",
    [string]$Age = "adult",
    [string]$Gender = "male",
    [string]$ChatUrl = "",
    [string]$CdpUrl = "",
    [switch]$SetupOnly,
    [switch]$SendOnly,
    [switch]$KeepOpen
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$AutomationRoot = Join-Path $ProjectRoot "tools\gemini_automation"
$ScriptPath = Join-Path $AutomationRoot "gemini_automate.mjs"
$PrepareScript = Join-Path $ProjectRoot "tools\prepare_gemini_handoff.py"
$ArgsList = @(
    $ScriptPath,
    "--species", $Species,
    "--age", $Age,
    "--gender", $Gender
)

if ($ChatUrl -ne "") {
    $ArgsList += @("--url", $ChatUrl)
}
if ($CdpUrl -ne "") {
    $ArgsList += @("--cdp-url", $CdpUrl)
}
if ($SetupOnly) {
    $ArgsList += "--setup-only"
}
if ($SendOnly) {
    $ArgsList += "--send-only"
}
if ($KeepOpen) {
    $ArgsList += "--keep-open"
}

Push-Location $AutomationRoot
try {
    python $PrepareScript --species $Species | Out-Null
    node @ArgsList
}
finally {
    Pop-Location
}
