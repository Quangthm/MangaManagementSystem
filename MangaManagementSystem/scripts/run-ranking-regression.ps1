[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $solutionRoot "MangaManagementSystem.slnx"

$testProjects = @(
    (Join-Path $solutionRoot "tests\MangaManagementSystem.Application.Tests\MangaManagementSystem.Application.Tests.csproj"),
    (Join-Path $solutionRoot "tests\MangaManagementSystem.RegressionTests\MangaManagementSystem.RegressionTests.csproj")
)

if (-not $NoBuild)
{
    Write-Host "Building solution in $Configuration mode..."

    & dotnet build $solutionPath --configuration $Configuration

    if ($LASTEXITCODE -ne 0)
    {
        throw "Solution build failed."
    }
}

foreach ($testProject in $testProjects)
{
    Write-Host ""
    Write-Host "Running: $testProject"

    & dotnet test $testProject `
        --configuration $Configuration `
        --no-build

    if ($LASTEXITCODE -ne 0)
    {
        throw "Regression test execution failed."
    }
}

Write-Host ""
Write-Host "Regression suite completed successfully."