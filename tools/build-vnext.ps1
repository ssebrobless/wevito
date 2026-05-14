param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipAssetPrep,
    [switch]$SkipTests,
    [switch]$AllowAssetPrepAfterStable,
    [int]$StepTimeoutSeconds = 0
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Solution = Join-Path $ProjectRoot "vnext\Wevito.VNext.sln"
$BrokerProject = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Broker\Wevito.VNext.Broker.csproj"
$ShellProject = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj"
$DevControllerProject = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.DevController\Wevito.VNext.DevController.csproj"
$SoakDriverProject = Join-Path $ProjectRoot "tools\soak-driver-cli\Wevito.VNext.SoakDriver.csproj"
$GeneratePetRuntimeScript = Join-Path $ProjectRoot "tools\generate_runtime_pose_sprites.py"
$CleanSharedScript = Join-Path $ProjectRoot "tools\clean_shared_sprite_assets.py"
$CleanPetRuntimeScript = Join-Path $ProjectRoot "tools\clean_runtime_pet_frames.py"
$ExtractStagePropsScript = Join-Path $ProjectRoot "tools\extract_stage_safe_props.py"
$StableReleaseLock = Join-Path $ProjectRoot "vnext\content\stable_release_lock.json"
$ArtifactsRoot = Join-Path $ProjectRoot "vnext\artifacts"
$BrokerOut = Join-Path $ArtifactsRoot "broker"
$ShellOut = Join-Path $ArtifactsRoot "shell"
$DevControllerOut = Join-Path $ArtifactsRoot "dev-controller"
$SoakDriverOut = Join-Path $ArtifactsRoot "soak-driver"

function Stop-WevitoProcesses {
    $processNames = @("Wevito.VNext.Shell", "Wevito.VNext.Broker")
    foreach ($name in $processNames) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                Stop-Process -Id $_.Id -Force -ErrorAction Stop
            }
            catch {
                Write-Warning "Could not stop $name ($($_.Id)): $($_.Exception.Message)"
            }
        }
    }
    Start-Sleep -Milliseconds 350
}

function Reset-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path $Path) {
        for ($attempt = 0; $attempt -lt 6; $attempt++) {
            try {
                Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
                break
            }
            catch {
                if (-not (Test-Path $Path)) {
                    break
                }

                Stop-WevitoProcesses

                try {
                    Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
                        try {
                            $_.Attributes = [System.IO.FileAttributes]::Normal
                        }
                        catch {
                        }
                    }
                }
                catch {
                }

                if ($attempt -ge 5) {
                    $fallbackPath = "$Path-stale-$([DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))"
                    try {
                        Move-Item -Path $Path -Destination $fallbackPath -Force -ErrorAction Stop
                        break
                    }
                    catch {
                        throw
                    }
                }

                Start-Sleep -Milliseconds 700
            }
        }
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-BuildStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$Arguments = @()
    )

    Write-Host "[build-vnext] $Name"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $processInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processInfo.FileName = $FilePath
    $processInfo.UseShellExecute = $false
    $processInfo.Arguments = ($Arguments | ForEach-Object {
            $argument = [string]$_
            if ($argument -match '[\s"]') {
                '"' + $argument.Replace('"', '\"') + '"'
            }
            else {
                $argument
            }
        }) -join " "

    $process = [System.Diagnostics.Process]::Start($processInfo)
    if ($StepTimeoutSeconds -gt 0) {
        $completed = $process.WaitForExit($StepTimeoutSeconds * 1000)
        if (-not $completed) {
            try {
                $process.Kill($true)
            }
            catch {
                $process.Kill()
            }

            throw "$Name timed out after $StepTimeoutSeconds seconds."
        }
    }
    else {
        $process.WaitForExit()
    }

    $stopwatch.Stop()
    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode)"
    }

    Write-Host ("[build-vnext] {0} completed in {1:n1}s" -f $Name, $stopwatch.Elapsed.TotalSeconds)
}

if (-not (Test-Path $Solution)) {
    throw "Missing vNext solution: $Solution"
}

Stop-WevitoProcesses

New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null
Reset-Directory -Path $BrokerOut
Reset-Directory -Path $ShellOut
Reset-Directory -Path $DevControllerOut
Reset-Directory -Path $SoakDriverOut

if ($SkipAssetPrep) {
    Write-Host "[build-vnext] Skipping sprite asset preparation; using existing runtime assets."
    if (-not $SkipTests) {
        Write-Warning "Sprite runtime coverage tests will validate the existing runtime assets. Omit -SkipAssetPrep when you need to regenerate assets before testing."
    }
}
else {
    if ((Test-Path $StableReleaseLock) -and -not $AllowAssetPrepAfterStable) {
        throw "Stable release lock is present. Re-run with -SkipAssetPrep, or pass -AllowAssetPrepAfterStable only in an approved asset-prep phase."
    }

    Invoke-BuildStep -Name "Clean shared sprite assets" -FilePath "python" -Arguments @(
        $CleanSharedScript,
        "--source", (Join-Path $ProjectRoot "sprites"),
        "--output", (Join-Path $ProjectRoot "sprites_shared_runtime"),
        "--clean-folders", "environment", "items", "status",
        "--copy-folders", "icons", "celestial", "portraits"
    )

    Invoke-BuildStep -Name "Extract stage-safe props" -FilePath "python" -Arguments @(
        $ExtractStagePropsScript,
        "--source", (Join-Path $ProjectRoot "incoming_sprites"),
        "--output", (Join-Path $ProjectRoot "sprites_shared_runtime")
    )

    Invoke-BuildStep -Name "Generate runtime pet sprites" -FilePath "python" -Arguments @(
        $GeneratePetRuntimeScript,
        "--source-root", (Join-Path $ProjectRoot "incoming_sprites"),
        "--output-root", (Join-Path $ProjectRoot "sprites_runtime")
    )

    Invoke-BuildStep -Name "Clean runtime pet frames" -FilePath "python" -Arguments @(
        $CleanPetRuntimeScript,
        "--root", (Join-Path $ProjectRoot "sprites_runtime")
    )
}

if ($SkipTests) {
    Write-Host "[build-vnext] Skipping vNext tests."
}
else {
    Invoke-BuildStep -Name "Run vNext tests" -FilePath "dotnet" -Arguments @("test", $Solution, "-c", $Configuration)
}

Invoke-BuildStep -Name "Publish broker" -FilePath "dotnet" -Arguments @("publish", $BrokerProject, "-c", $Configuration, "-o", $BrokerOut)
Invoke-BuildStep -Name "Publish shell" -FilePath "dotnet" -Arguments @("publish", $ShellProject, "-c", $Configuration, "-o", $ShellOut)
Invoke-BuildStep -Name "Publish dev controller" -FilePath "dotnet" -Arguments @("publish", $DevControllerProject, "-c", $Configuration, "-o", $DevControllerOut)
Invoke-BuildStep -Name "Publish soak driver CLI" -FilePath "dotnet" -Arguments @("publish", $SoakDriverProject, "-c", $Configuration, "-o", $SoakDriverOut)

Write-Host "[build-vnext] Copying broker output into shell output"
Copy-Item -Path (Join-Path $BrokerOut "*") -Destination $ShellOut -Force

Write-Host "vNext publish complete:"
Write-Host "  Shell output: $ShellOut"
Write-Host "  Entry point : $(Join-Path $ShellOut 'Wevito.VNext.Shell.exe')"
Write-Host "  Dev cockpit : $(Join-Path $DevControllerOut 'Wevito.VNext.DevController.exe')"
Write-Host "  Soak driver: $(Join-Path $SoakDriverOut 'Wevito.VNext.SoakDriver.exe')"
