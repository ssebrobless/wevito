param()

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$AutomationRoot = Join-Path $ProjectRoot "tools\gemini_automation"

Push-Location $AutomationRoot
try {
    npm install
    npx playwright install chromium
}
finally {
    Pop-Location
}
