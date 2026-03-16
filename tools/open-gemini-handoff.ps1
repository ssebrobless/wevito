param(
    [string]$Species = "rat",
    [string]$Age = "adult",
    [string]$Gender = "male",
    [switch]$OpenGemini
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PythonScript = Join-Path $ProjectRoot "tools\prepare_gemini_handoff.py"
$HandoffRoot = Join-Path $ProjectRoot "incoming_sprites\gemini_handoff"

python $PythonScript --species $Species | Out-Null

$VariantFolder = Join-Path $HandoffRoot "$Species\$Age\$Gender"
$PromptPath = Join-Path $VariantFolder "4-prompt.txt"

if (-not (Test-Path $VariantFolder)) {
    throw "Variant folder not found: $VariantFolder"
}

if (Test-Path $PromptPath) {
    Set-Clipboard -Value (Get-Content -Path $PromptPath -Raw)
}

Start-Process explorer.exe $VariantFolder

if ($OpenGemini) {
    Start-Process "https://gemini.google.com/"
}

Write-Host "Opened Gemini handoff folder:"
Write-Host $VariantFolder
Write-Host ""
Write-Host "Prompt copied to clipboard from:"
Write-Host $PromptPath
