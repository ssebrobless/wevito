param(
    [int]$Port = 9333,
    [string]$Url = "https://gemini.google.com/",
    [switch]$ReuseIfRunning
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProfileRoot = Join-Path $ProjectRoot ".codex-cache\chrome-debug-profile"
$ChromePath = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
if (-not (Test-Path $ChromePath)) {
    $ChromePath = "C:\Program Files\Google\Chrome\Application\chrome.exe"
}
if (-not (Test-Path $ChromePath)) {
    throw "Chrome executable not found."
}

New-Item -ItemType Directory -Force -Path $ProfileRoot | Out-Null

if (-not $ReuseIfRunning) {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq 'chrome.exe' -and
            $_.CommandLine -like "*$ProfileRoot*" -and
            $_.CommandLine -like "*--remote-debugging-port=$Port*"
        } |
        ForEach-Object {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
    Start-Sleep -Seconds 1
}

& $ChromePath "--remote-debugging-port=$Port" "--remote-debugging-address=127.0.0.1" "--user-data-dir=$ProfileRoot" "--no-first-run" "--no-default-browser-check" $Url

for ($attempt = 0; $attempt -lt 20; $attempt++) {
    Start-Sleep -Milliseconds 500
    try {
        $content = (Invoke-WebRequest "http://127.0.0.1:$Port/json/version" -UseBasicParsing).Content
        Write-Host $content
        exit 0
    }
    catch {
    }
}

throw "Chrome launched but the debug endpoint on port $Port did not become ready."
