param(
    [string]$ArtifactRoot = ".\vnext\artifacts\c-phase-127-matrix-sweep",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactRootFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))
$profileRoot = Join-Path $artifactRootFull "sandbox-profile"
$pipeName = "wevito-vnext-dev-control-cphase127-$([System.Guid]::NewGuid().ToString('N'))"
$traceRoot = Join-Path $artifactRootFull "trace"
$shellProject = Join-Path $repoRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj"
$runnerProject = Join-Path $repoRoot "vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj"

New-Item -ItemType Directory -Force -Path $artifactRootFull | Out-Null
New-Item -ItemType Directory -Force -Path $profileRoot | Out-Null
New-Item -ItemType Directory -Force -Path $traceRoot | Out-Null

dotnet build $shellProject -c $Configuration
dotnet build $runnerProject -c $Configuration

$targetFramework = "net8.0-windows10.0.19041.0"
$shellExe = Join-Path $repoRoot "vnext\src\Wevito.VNext.Shell\bin\$Configuration\$targetFramework\Wevito.VNext.Shell.exe"
if (-not (Test-Path $shellExe)) {
    throw "Shell executable not found: $shellExe"
}

$psi = [System.Diagnostics.ProcessStartInfo]::new($shellExe)
$psi.WorkingDirectory = $repoRoot
$psi.UseShellExecute = $false
$psi.Environment["WEVITO_VNEXT_DATA_ROOT"] = $profileRoot
$psi.Environment["WEVITO_PROFILE_OVERRIDE"] = $profileRoot
$psi.Environment["WEVITO_DEV_CONTROL_PIPE"] = $pipeName
$psi.Environment["WEVITO_VNEXT_TRACE_DIR"] = $traceRoot
$psi.Environment["WEVITO_SKIP_FIRST_LAUNCH_WIZARD"] = "1"
$psi.Environment["WEVITO_VISUAL_QA_FAST_MODE"] = "1"
$process = [System.Diagnostics.Process]::Start($psi)

try {
    $env:WEVITO_DEV_CONTROL_PIPE = $pipeName
    $env:WEVITO_VNEXT_TRACE_DIR = $traceRoot
    dotnet run --project $runnerProject -c $Configuration -- --sweep --out $artifactRootFull --repo-root $repoRoot
    if ($LASTEXITCODE -ne 0) {
        throw "AutomationRunner failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item Env:\WEVITO_DEV_CONTROL_PIPE -ErrorAction SilentlyContinue
    Remove-Item Env:\WEVITO_VNEXT_TRACE_DIR -ErrorAction SilentlyContinue
    if ($process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(5000) | Out-Null
    }
}
