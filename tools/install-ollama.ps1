param(
    [switch]$ShowWingetCommand
)

$ErrorActionPreference = "Stop"

Write-Host "Wevito local reasoning uses Ollama on localhost only."
Write-Host ""
Write-Host "This helper does not download or install anything automatically."
Write-Host "Install Ollama yourself, then pull Wevito's default local model."
Write-Host ""
Write-Host "1. Download Ollama:"
Write-Host "   https://ollama.com/download"
Write-Host ""
if ($ShowWingetCommand) {
    Write-Host "Optional Windows command if you approve the install yourself:"
    Write-Host "   winget install Ollama.Ollama"
    Write-Host ""
}
Write-Host "2. Pull Wevito's default reasoning model:"
Write-Host "   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\pull-default-model.ps1"
Write-Host ""
Write-Host "3. Restart Wevito. The app will probe http://127.0.0.1:11434 and fall back safely if unavailable."
