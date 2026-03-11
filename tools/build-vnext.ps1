param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Solution = Join-Path $ProjectRoot "vnext\Wevito.VNext.sln"
$BrokerProject = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Broker\Wevito.VNext.Broker.csproj"
$ShellProject = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj"
$ArtifactsRoot = Join-Path $ProjectRoot "vnext\artifacts"
$BrokerOut = Join-Path $ArtifactsRoot "broker"
$ShellOut = Join-Path $ArtifactsRoot "shell"

if (-not (Test-Path $Solution)) {
    throw "Missing vNext solution: $Solution"
}

if (Test-Path $ArtifactsRoot) {
    Remove-Item -Recurse -Force $ArtifactsRoot
}

New-Item -ItemType Directory -Path $BrokerOut | Out-Null
New-Item -ItemType Directory -Path $ShellOut | Out-Null

dotnet test $Solution -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "vNext tests failed with exit code $LASTEXITCODE"
}

dotnet publish $BrokerProject -c $Configuration -o $BrokerOut
if ($LASTEXITCODE -ne 0) {
    throw "Broker publish failed with exit code $LASTEXITCODE"
}

dotnet publish $ShellProject -c $Configuration -o $ShellOut
if ($LASTEXITCODE -ne 0) {
    throw "Shell publish failed with exit code $LASTEXITCODE"
}

Copy-Item -Path (Join-Path $BrokerOut "*") -Destination $ShellOut -Force

Write-Host "vNext publish complete:"
Write-Host "  Shell output: $ShellOut"
Write-Host "  Entry point : $(Join-Path $ShellOut 'Wevito.VNext.Shell.exe')"
