param()

$ErrorActionPreference = "Stop"

$userDataRoot = Join-Path $env:APPDATA "Godot\app_userdata\Wevito"
$commandPath = Join-Path $userDataRoot "overlay_command.json"

New-Item -ItemType Directory -Path $userDataRoot -Force | Out-Null

$payload = @{
    command = "dump_runtime_state"
    issued_at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
} | ConvertTo-Json -Compress

Set-Content -Path $commandPath -Value $payload -Encoding UTF8
Write-Host "Queued runtime state dump."
