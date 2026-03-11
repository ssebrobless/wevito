param(
    [ValidateSet("toggle", "pin", "release", "show")]
    [string]$Command = "toggle"
)

$ErrorActionPreference = "Stop"

$userDataRoot = Join-Path $env:APPDATA "Godot\app_userdata\Wevito"
$commandPath = Join-Path $userDataRoot "overlay_command.json"

New-Item -ItemType Directory -Path $userDataRoot -Force | Out-Null

$mappedCommand = switch ($Command) {
    "toggle" { "toggle_overlay_ui" }
    "pin" { "pin_overlay_ui" }
    "release" { "release_overlay_ui" }
    "show" { "show_window" }
}

$payload = @{
    command = $mappedCommand
    issued_at = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
} | ConvertTo-Json -Compress

Set-Content -Path $commandPath -Value $payload -Encoding UTF8
Write-Host "Queued overlay command: $Command"
