# run-spotbugs-two-demos.ps1
# Runs both SpotBugs classroom demos.
#
# Demo 1: spotbugs-demo          -> generates a SpotBugs report
# Demo 2: spotbugs-check-demo    -> buggy FAILS check, fixed PASSES check
#
# Copies demo folders to C:\SpotBugsClassDemo to avoid Vietnamese path encoding issues.
# Requires: JDK 8+ and Apache Maven on PATH.

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  SpotBugs Classroom Demo Runner" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# -------------------------------------------------------
# Check prerequisites
# -------------------------------------------------------
Write-Host "Checking Java..." -ForegroundColor Yellow
$javaCmd = Get-Command java -ErrorAction SilentlyContinue
if (-not $javaCmd) {
    Write-Host "ERROR: 'java' not found on PATH." -ForegroundColor Red
    Write-Host "Please install JDK 8 (or newer) and add java to PATH." -ForegroundColor Red
    exit 1
}
$javaVersionOutput = cmd /c "java -version 2>&1"
Write-Host "  Detected Java:" -ForegroundColor Gray
$javaVersionOutput | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }

Write-Host ""
Write-Host "Checking Maven..." -ForegroundColor Yellow
$mvnCmd = Get-Command mvn -ErrorAction SilentlyContinue
if (-not $mvnCmd) {
    Write-Host "ERROR: 'mvn' not found on PATH." -ForegroundColor Red
    Write-Host "Please install Apache Maven and add mvn to PATH." -ForegroundColor Red
    exit 1
}
$mvnVersionOutput = & mvn --version 2>&1
Write-Host "  Detected Maven:" -ForegroundColor Gray
$mvnVersionOutput | Select-Object -First 1 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }

# -------------------------------------------------------
# Copy demo folders to ASCII-only path
# -------------------------------------------------------
$asciiRoot = "C:\SpotBugsClassDemo"
Write-Host ""
Write-Host "Copying demo folders to $asciiRoot ..." -ForegroundColor Yellow

if (Test-Path $asciiRoot) {
    Remove-Item -Recurse -Force $asciiRoot
}
New-Item -ItemType Directory -Path $asciiRoot -Force | Out-Null

$sourceDemo1 = Join-Path $PSScriptRoot "spotbugs-demo"
$sourceDemo2 = Join-Path $PSScriptRoot "spotbugs-check-demo"

Copy-Item -Recurse -Force $sourceDemo1 (Join-Path $asciiRoot "spotbugs-demo")
Copy-Item -Recurse -Force $sourceDemo2 (Join-Path $asciiRoot "spotbugs-check-demo")

Write-Host "  Done." -ForegroundColor Gray

# -------------------------------------------------------
# Demo 1: SpotBugs Report Generation
# -------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DEMO 1: SpotBugs Report Generation" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Command: mvn clean compile spotbugs:spotbugs" -ForegroundColor White
Write-Host ""

$demo1Dir = Join-Path $asciiRoot "spotbugs-demo"
$demo1Pass = $false

Push-Location $demo1Dir
try {
    & mvn clean compile spotbugs:spotbugs
    if ($LASTEXITCODE -eq 0) {
        $demo1Pass = $true
    }
} catch {
    Write-Host "  Demo 1 encountered an error: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

if ($demo1Pass) {
    Write-Host ""
    Write-Host "  Demo 1 COMPLETED - SpotBugs report generated." -ForegroundColor Green
    $xmlReport = Join-Path $demo1Dir "target\spotbugsXml.xml"
    if (Test-Path $xmlReport) {
        Write-Host "  XML report: $xmlReport" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "  Demo 1 had issues. Check the output above." -ForegroundColor Yellow
}

# -------------------------------------------------------
# Demo 2a: SpotBugs Check -- Buggy Version (expected FAIL)
# -------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DEMO 2a: SpotBugs Check - Buggy Version" -ForegroundColor Cyan
Write-Host "  (Expected: FAIL)" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Command: mvn clean compile spotbugs:check" -ForegroundColor White
Write-Host ""

$demo2BuggyDir = Join-Path $asciiRoot "spotbugs-check-demo\buggy"
$demo2BuggyFailed = $false

Push-Location $demo2BuggyDir
try {
    & mvn clean compile spotbugs:check
    if ($LASTEXITCODE -ne 0) {
        $demo2BuggyFailed = $true
    }
} catch {
    $demo2BuggyFailed = $true
} finally {
    Pop-Location
}

if ($demo2BuggyFailed) {
    Write-Host ""
    Write-Host "  Demo 2a FAILED as expected -- SpotBugs found code-quality issues." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "  WARNING: Demo 2a passed unexpectedly. Buggy code should have failed." -ForegroundColor Yellow
}

# -------------------------------------------------------
# Demo 2b: SpotBugs Check -- Fixed Version (expected PASS)
# -------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DEMO 2b: SpotBugs Check - Fixed Version" -ForegroundColor Cyan
Write-Host "  (Expected: PASS)" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Command: mvn clean compile spotbugs:check" -ForegroundColor White
Write-Host ""

$demo2FixedDir = Join-Path $asciiRoot "spotbugs-check-demo\fixed"
$demo2FixedPass = $false

Push-Location $demo2FixedDir
try {
    & mvn clean compile spotbugs:check
    if ($LASTEXITCODE -eq 0) {
        $demo2FixedPass = $true
    }
} catch {
    Write-Host "  Demo 2b encountered an error: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

if ($demo2FixedPass) {
    Write-Host ""
    Write-Host "  Demo 2b PASSED as expected -- all issues were fixed." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "  WARNING: Demo 2b failed unexpectedly. Fixed code should have passed." -ForegroundColor Yellow
}

# -------------------------------------------------------
# Summary
# -------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  CLASSROOM SUMMARY" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($demo1Pass) {
    Write-Host "  Demo 1  : SpotBugs report generation     -> COMPLETED" -ForegroundColor Green
} else {
    Write-Host "  Demo 1  : SpotBugs report generation     -> HAD ISSUES" -ForegroundColor Yellow
}

if ($demo2BuggyFailed) {
    Write-Host "  Demo 2a : Buggy code spotbugs:check      -> FAILED (as expected)" -ForegroundColor Green
} else {
    Write-Host "  Demo 2a : Buggy code spotbugs:check      -> PASSED (unexpected)" -ForegroundColor Yellow
}

if ($demo2FixedPass) {
    Write-Host "  Demo 2b : Fixed code spotbugs:check      -> PASSED (as expected)" -ForegroundColor Green
} else {
    Write-Host "  Demo 2b : Fixed code spotbugs:check      -> FAILED (unexpected)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Tool: SpotBugs only (no Checkstyle, PMD, SonarQube, etc.)" -ForegroundColor Gray
Write-Host "  SpotBugs analyzes compiled Java bytecode, not C# source." -ForegroundColor Gray
Write-Host ""
Write-Host "  Working copy: $asciiRoot" -ForegroundColor Gray
Write-Host "============================================" -ForegroundColor Cyan
