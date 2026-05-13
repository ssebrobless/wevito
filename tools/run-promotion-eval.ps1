param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,
    [int]$Window = 7,
    [switch]$EmitDecisionOnly
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactRootPath = if ([System.IO.Path]::IsPathRooted($ArtifactRoot)) { $ArtifactRoot } else { Join-Path $repo $ArtifactRoot }
$settingsPath = Join-Path $env:LOCALAPPDATA "Wevito\settings.json"

if (Test-Path $settingsPath) {
    $settingsText = Get-Content -Raw -Path $settingsPath
    if ($settingsText -match '"runtime_kill_switch"\s*:\s*"?true"?' -or $settingsText -match 'runtime_kill_switch=True') {
        Write-Error "KillSwitch is active; promotion eval refused."
        exit 2
    }
}

if (-not $EmitDecisionOnly) {
    $goldenScript = Join-Path $repo "tools\run-golden-eval.ps1"
    if (Test-Path $goldenScript) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $goldenScript -ArtifactRoot (Join-Path $repo "vnext\artifacts\eval-golden")
    }
}

$soakRoot = Join-Path $repo "vnext\artifacts\soak"
$latestSoak = if (Test-Path $soakRoot) {
    Get-ChildItem -Path $soakRoot -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName "manifest.json") } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
} else {
    $null
}

if ($null -eq $latestSoak) {
    Write-Host "No soak manifest found; promotion snapshot will use ledger defaults only."
}

$driver = Join-Path $repo "vnext\artifacts\soak-driver\Wevito.VNext.SoakDriver.exe"
dotnet publish (Join-Path $repo "tools\soak-driver-cli\Wevito.VNext.SoakDriver.csproj") -c Release -o (Join-Path $repo "vnext\artifacts\soak-driver") | Out-Host

$argsList = @(
    "promotion-snapshot",
    "--window", $Window.ToString(),
    "--promotion-root", $artifactRootPath
)

& $driver @argsList
$exitCode = $LASTEXITCODE

if ($EmitDecisionOnly) {
    exit 0
}

exit $exitCode
