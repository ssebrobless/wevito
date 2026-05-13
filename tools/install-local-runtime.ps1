param(
    [string]$Model = "llama3.2:3b",
    [string]$LogRoot = "$env:LOCALAPPDATA\Wevito\runtime-install",
    [switch]$PlanOnly
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $LogRoot | Out-Null
$logPath = Join-Path $LogRoot ("install-log-{0}.txt" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

function Write-Step {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format o), $Message
    $line | Tee-Object -FilePath $logPath -Append
}

Write-Step "Wevito local runtime install helper started."
Write-Step "This script only installs Ollama/model after interactive command approval from winget/ollama."
Write-Step "Model: $Model"

$wingetProbe = "winget list Ollama.Ollama --exact"
$pathProbe = "Get-Command ollama -ErrorAction SilentlyContinue"
Write-Step "Probe command: $wingetProbe"
Write-Step "Probe command: $pathProbe"

if ($PlanOnly) {
    Write-Step "PlanOnly requested; no commands executed."
    Write-Host "[local-runtime] Plan written to $logPath"
    exit 0
}

Write-Step "Running: $wingetProbe"
winget list Ollama.Ollama --exact 2>&1 | Tee-Object -FilePath $logPath -Append
Write-Step "winget list exit code: $LASTEXITCODE"

$ollamaCommand = Get-Command ollama -ErrorAction SilentlyContinue
if ($null -eq $ollamaCommand) {
    Write-Step "ollama not found in PATH; running winget install Ollama.Ollama."
    winget install Ollama.Ollama
    Write-Step "winget install exit code: $LASTEXITCODE"
} else {
    Write-Step "ollama found at $($ollamaCommand.Source)."
}

Write-Step "Running: ollama pull $Model"
ollama pull $Model
Write-Step "ollama pull exit code: $LASTEXITCODE"

Write-Step "Install helper completed."
Write-Host "[local-runtime] Log written to $logPath"
