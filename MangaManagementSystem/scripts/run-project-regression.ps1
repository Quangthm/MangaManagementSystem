param(
    [string]$Configuration = "Release",
    [string]$ResultsDirectory
)

$ErrorActionPreference = "Stop"

$repoRoot =
    Split-Path `
        -Parent `
        $PSScriptRoot

$solution =
    Join-Path `
        $repoRoot `
        "MangaManagementSystem.slnx"

if (-not (Test-Path $solution)) {
    throw "Solution not found: $solution"
}

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $runStamp =
        Get-Date -Format "yyyyMMdd_HHmmss"

    $ResultsDirectory =
        Join-Path `
            $env:TEMP `
            "SWP391\MangaManagementSystem\ProjectRegression\$runStamp"
}

New-Item `
    -ItemType Directory `
    -Path $ResultsDirectory `
    -Force |
    Out-Null

$resolvedResultsDirectory =
    (Resolve-Path $ResultsDirectory).Path

Write-Host "============================================================"
Write-Host "SWP391 PROJECT REGRESSION SUITE"
Write-Host "============================================================"
Write-Host "Solution      : $solution"
Write-Host "Configuration : $Configuration"
Write-Host "Results       : $resolvedResultsDirectory"
Write-Host ""

Write-Host "=== RESTORE ===" -ForegroundColor Cyan

dotnet restore $solution

if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore FAILED with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "=== BUILD ===" -ForegroundColor Cyan

dotnet build `
    $solution `
    -c $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build FAILED with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "=== TEST + COVERAGE ===" -ForegroundColor Cyan

dotnet test `
    $solution `
    -c $Configuration `
    --no-build `
    --no-restore `
    --collect:"XPlat Code Coverage" `
    --results-directory $resolvedResultsDirectory `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    throw "dotnet test FAILED with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "=== COVERAGE FILES ===" -ForegroundColor Cyan

$coverageFiles =
    Get-ChildItem `
        -Path $resolvedResultsDirectory `
        -Recurse `
        -File `
        -Filter "coverage.cobertura.xml"

if (-not $coverageFiles) {
    throw "Tests passed, but no coverage.cobertura.xml was produced."
}

foreach ($coverageFile in $coverageFiles) {
    Write-Host $coverageFile.FullName -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================================"
Write-Host "PROJECT REGRESSION PASSED" -ForegroundColor Green
Write-Host "============================================================"
Write-Host "Build    : PASS"
Write-Host "Tests    : PASS"
Write-Host "Coverage : GENERATED"
Write-Host "Results  : $resolvedResultsDirectory"