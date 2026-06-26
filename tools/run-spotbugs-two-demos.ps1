param(
    [switch]$SkipCopy
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DestRoot = "C:\SpotBugsClassDemo"
$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  SpotBugs Two Demos Runner" -ForegroundColor Cyan
Write-Host "  Started: $Timestamp" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipCopy) {
    Write-Host "[INFO] Copying demo files to $DestRoot (ASCII-only path)..." -ForegroundColor Yellow

    if (Test-Path -LiteralPath $DestRoot) {
        Remove-Item -LiteralPath $DestRoot -Recurse -Force
    }

    Copy-Item -LiteralPath "$RepoRoot\tools\spotbugs-demo" -Destination "$DestRoot\spotbugs-demo" -Recurse -Force
    Copy-Item -LiteralPath "$RepoRoot\tools\spotbugs-check-demo" -Destination "$DestRoot\spotbugs-check-demo" -Recurse -Force

    Write-Host "[INFO] Copy complete." -ForegroundColor Green
    Write-Host ""
}

function Run-Demo {
    param([string]$Name, [string]$Dir, [string]$Goal)

    Write-Host "--------------------------------------------" -ForegroundColor Magenta
    Write-Host "  Running: $Name" -ForegroundColor Magenta
    Write-Host "  Goal: $Goal" -ForegroundColor Magenta
    Write-Host "--------------------------------------------" -ForegroundColor Magenta

    try {
        Push-Location -LiteralPath $Dir
        mvn clean compile spotbugs:spotbugs 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  RESULT: $Name -> COMPLETED" -ForegroundColor Green
        } else {
            Write-Host "  RESULT: $Name -> FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
        }
    } catch {
        Write-Host "  RESULT: $Name -> ERROR: $_" -ForegroundColor Red
    } finally {
        Pop-Location
    }
    Write-Host ""
}

function Run-Check {
    param([string]$Name, [string]$Dir, [string]$Goal)

    Write-Host "--------------------------------------------" -ForegroundColor Magenta
    Write-Host "  Running: $Name" -ForegroundColor Magenta
    Write-Host "  Goal: $Goal" -ForegroundColor Magenta
    Write-Host "--------------------------------------------" -ForegroundColor Magenta

    try {
        Push-Location -LiteralPath $Dir
        mvn clean compile spotbugs:check 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  RESULT: $Name -> PASSED (as expected)" -ForegroundColor Green
        } else {
            Write-Host "  RESULT: $Name -> FAILED (as expected)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  RESULT: $Name -> ERROR: $_" -ForegroundColor Red
    } finally {
        Pop-Location
    }
    Write-Host ""
}

Run-Demo -Name "Demo 1: SpotBugs report generation" `
         -Dir "$DestRoot\spotbugs-demo" `
         -Goal "Generate SpotBugs XML report"

Run-Check -Name "Demo 2a: Buggy code spotbugs:check" `
          -Dir "$DestRoot\spotbugs-check-demo\buggy" `
          -Goal "Fail the build (expected)"

Run-Check -Name "Demo 2b: Fixed code spotbugs:check" `
          -Dir "$DestRoot\spotbugs-check-demo\fixed" `
          -Goal "Pass the build (expected)"

$EndTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  All demos finished: $EndTime" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
