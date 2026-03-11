param()

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BridgeProject = Join-Path $ProjectRoot "tools\desktop_bridge\WevitoDesktopBridge.csproj"
$PublishDir = Join-Path $ProjectRoot "builds\desktop_bridge"

if (-not (Test-Path $BridgeProject)) {
    throw "Missing desktop bridge project: $BridgeProject"
}

if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

$publishArgs = @(
    "publish",
    $BridgeProject,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=None",
    "-o", $PublishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "Desktop bridge publish failed with exit code $LASTEXITCODE"
}

$BridgeExe = Join-Path $PublishDir "WevitoDesktopBridge.exe"
if (-not (Test-Path $BridgeExe)) {
    throw "Desktop bridge publish did not produce $BridgeExe"
}

Write-Host "Desktop bridge ready: $BridgeExe"
