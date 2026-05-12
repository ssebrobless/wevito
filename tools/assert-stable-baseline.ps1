param(
    [string]$ArtifactRoot = ".\vnext\artifacts\c-phase-54-post-stable-baseline-lock"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LockPath = Join-Path $ProjectRoot "vnext\content\stable_release_lock.json"

if (-not (Test-Path $LockPath)) {
    throw "Missing stable release lock: $LockPath"
}

$lock = Get-Content -Raw $LockPath | ConvertFrom-Json
if ($lock.stableTag -ne "v0.1.0-desktop") {
    throw "Unexpected stableTag: $($lock.stableTag)"
}

if ($lock.stableSha256 -ne "c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc") {
    throw "Unexpected stableSha256: $($lock.stableSha256)"
}

New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null

python (Join-Path $ProjectRoot "tools\report_runtime_canvas_mismatches.py") `
    --output (Join-Path $ArtifactRoot "runtime-canvas.json") `
    --markdown (Join-Path $ArtifactRoot "runtime-canvas.md") `
    --fail-on-mismatch

python (Join-Path $ProjectRoot "tools\audit_sprite_contract.py") `
    --output (Join-Path $ArtifactRoot "sprite-contract.json")

python (Join-Path $ProjectRoot "tools\audit_optional_animation_readiness.py") `
    --output (Join-Path $ArtifactRoot "optional-readiness.json") `
    --markdown (Join-Path $ArtifactRoot "optional-readiness.md")

$summary = [ordered]@{
    stableTag = $lock.stableTag
    stableSha256 = $lock.stableSha256
    runtimeCanvas = "runtime-canvas.json"
    spriteContract = "sprite-contract.json"
    optionalReadiness = "optional-readiness.json"
}

$summary | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 -Path (Join-Path $ArtifactRoot "stable-baseline-summary.json")
Write-Host "Stable baseline assertion passed: $ArtifactRoot"
