param(
  [int]$Port = 9222,
  [string]$Profile = "$env:LOCALAPPDATA\Codex\GeminiChromeProfile",
  [string]$Url = "https://gemini.google.com/u/1/app"
)

$ErrorActionPreference = "Stop"

$runningChrome = Get-Process chrome -ErrorAction SilentlyContinue |
  Where-Object { $_.Path } |
  Select-Object -First 1 -ExpandProperty Path

$candidates = @(
  $runningChrome,
  "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
  "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
  "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
) | Where-Object { $_ -and (Test-Path $_) }

$chrome = $candidates | Select-Object -First 1
if (-not $chrome) {
  throw "Chrome executable not found."
}

New-Item -ItemType Directory -Force -Path $Profile | Out-Null

$args = @(
  "--remote-debugging-port=$Port",
  "--user-data-dir=$Profile",
  "--no-first-run",
  "--no-default-browser-check",
  $Url
)

Start-Process -FilePath $chrome -ArgumentList $args

$deadline = (Get-Date).AddSeconds(25)
$version = $null
while ((Get-Date) -lt $deadline -and -not $version) {
  try {
    $version = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/version" -TimeoutSec 2
  } catch {
    Start-Sleep -Milliseconds 750
  }
}

if (-not $version) {
  throw "Chrome launched, but the DevTools endpoint did not respond on port $Port."
}

[pscustomobject]@{
  Chrome = $chrome
  Profile = $Profile
  Port = $Port
  Browser = $version.Browser
  WebSocketDebuggerUrl = $version.webSocketDebuggerUrl
}
