param(
    [ValidateSet("day", "week")]
    [string]$Window = "day",
    [string]$ArtifactRoot = ".\vnext\artifacts\pet-tasks"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$coreProject = Join-Path $repoRoot "vnext\src\Wevito.VNext.Core\Wevito.VNext.Core.csproj"
$now = [DateTimeOffset]::UtcNow
$since = if ($Window -eq "week") { $now.AddDays(-7) } else { $now.AddDays(-1) }
$artifactFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactRoot))

$tmpRoot = Join-Path $env:TEMP ("wevito-self-improvement-" + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $tmpRoot | Out-Null
dotnet new console --force --output $tmpRoot | Out-Null
$tmpProject = (Get-ChildItem -Path $tmpRoot -Filter "*.csproj" | Select-Object -First 1).FullName
dotnet add $tmpProject reference $coreProject | Out-Null

$program = @"
using Wevito.VNext.Core;

var ledger = new AuditLedgerService();
var service = new SelfImprovementReportService(ledger);
var result = service.Run(new SelfImprovementReportRequest(
    DateTimeOffset.Parse("$($since.ToString("O"))"),
    DateTimeOffset.Parse("$($now.ToString("O"))"),
    @"$artifactFull",
    DateTimeOffset.Parse("$($now.ToString("O"))")));
if (!result.Succeeded)
{
    Console.Error.WriteLine(result.Message);
    return 1;
}
Console.WriteLine(result.MarkdownPath);
Console.WriteLine(result.JsonPath);
return 0;
"@

Set-Content -Path (Join-Path $tmpRoot "Program.cs") -Value $program
try
{
    dotnet run --project $tmpProject
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally
{
    Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
}
