param(
    [string]$Model = "qwen2.5:7b-instruct-q4_K_M"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Model)) {
    throw "Model name is required."
}

Write-Host "Wevito will ask Ollama to pull the local reasoning model:"
Write-Host "  $Model"
Write-Host ""
Write-Host "This uses Ollama's local model store. No hosted AI provider key is read."
Write-Host "Press Ctrl+C to cancel, or wait 5 seconds to continue."
Start-Sleep -Seconds 5

if (-not (Get-Command ollama -ErrorAction SilentlyContinue)) {
    throw "Ollama CLI was not found. Run tools\install-ollama.ps1 for setup instructions."
}

& ollama pull $Model
if ($LASTEXITCODE -ne 0) {
    throw "ollama pull failed with exit code $LASTEXITCODE."
}

Write-Host "Ollama pull completed for $Model."
