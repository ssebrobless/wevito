param(
    [switch]$DryRun,
    [switch]$Approved,
    [string]$PlanPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host "Wevito LoRA pilot scaffold (C-PHASE 73)"
Write-Host "This script is plan-only. It does not invoke any interpreter, Unsloth, model downloads, or training."

if (-not $Approved) {
    Write-Host "Blocked: tuning_lora_enabled=false by default and no explicit future-phase approval was supplied."
    exit 2
}

if ($DryRun) {
    Write-Host "Dry-run approved: validating handoff shape only."
    if ($PlanPath -and -not (Test-Path -LiteralPath $PlanPath)) {
        throw "PlanPath does not exist: $PlanPath"
    }
    exit 0
}

Write-Host "Blocked: C-PHASE 73 may create LoRA plans and scripts only. Real training belongs to a later approved phase."
exit 3
