param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$testProject = Join-Path $repoRoot "vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj"

Write-Host "Running PET TASKS preview smoke probe..."
Write-Host "Repo: $repoRoot"

dotnet test $testProject `
    --configuration $Configuration `
    --filter "FullyQualifiedName~PetTasksPreviewSmokeTests"

Write-Host "PET TASKS preview smoke probe complete."
