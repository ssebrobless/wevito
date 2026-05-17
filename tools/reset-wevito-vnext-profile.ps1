param(
    [switch]$WhatIfOnly
)

$ErrorActionPreference = "Stop"

$dataRoot = Join-Path $env:LOCALAPPDATA "WevitoVNext"
$databasePath = Join-Path $dataRoot "wevito-vnext.db"
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
$backupPath = Join-Path $dataRoot "wevito-vnext.backup-$timestamp.db"

Write-Host "Wevito vNext profile reset"
Write-Host "Live save database: $databasePath"

if (-not (Test-Path -LiteralPath $databasePath)) {
    Write-Host "No live save database exists. Fresh launch will create a new egg-choice profile."
    exit 0
}

Write-Host "Backup target: $backupPath"

if ($WhatIfOnly) {
    Write-Host "WhatIfOnly set. No files changed."
    exit 0
}

New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
Copy-Item -LiteralPath $databasePath -Destination $backupPath -Force
Remove-Item -LiteralPath $databasePath -Force

Write-Host "Profile reset complete. Backup preserved at: $backupPath"
