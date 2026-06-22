# run-spotbugs-demo.ps1
# Runs the SpotBugs demo Maven project and generates an HTML report.
# Requires: JDK 8+ and Apache Maven on PATH.

$ErrorActionPreference = "Stop"

Write-Host "=== SpotBugs Demo Runner ===" -ForegroundColor Cyan

# --- Check Java ---
Write-Host ""
Write-Host "Checking Java..." -ForegroundColor Yellow

$javaCmd = Get-Command java -ErrorAction SilentlyContinue
if (-not $javaCmd) {
    Write-Host "ERROR: 'java' not found on PATH." -ForegroundColor Red
    Write-Host "Please install JDK 8 (or newer) and ensure 'java' is on your PATH." -ForegroundColor Red
    exit 1
}

# Print java version (stderr is redirected to stdout for capture)
$javaVersion = cmd /c "java -version 2>&1"
Write-Host "Detected Java:"
$javaVersion | ForEach-Object { Write-Host "  $_" }

# Accept any JDK 1.8+ (including 1.8.x, 9, 11, 17, 21, etc.)
$versionLine = ($javaVersion | Select-Object -First 1) -as [string]
if ($versionLine -match '"(\d+[\.\d_]*)') {
    $ver = $Matches[1]
    Write-Host "  Parsed version: $ver" -ForegroundColor Gray
} else {
    Write-Host "WARNING: Could not parse Java version string. Continuing anyway." -ForegroundColor Yellow
}

# --- Check Maven ---
Write-Host ""
Write-Host "Checking Maven..." -ForegroundColor Yellow

$mvnCmd = Get-Command mvn -ErrorAction SilentlyContinue
if (-not $mvnCmd) {
    Write-Host "ERROR: 'mvn' (Maven) not found on PATH." -ForegroundColor Red
    Write-Host "Please install Apache Maven (https://maven.apache.org/) and ensure 'mvn' is on your PATH." -ForegroundColor Red
    exit 1
}

$mvnVersion = & mvn --version 2>&1
Write-Host "Detected Maven:"
$mvnVersion | Select-Object -First 2 | ForEach-Object { Write-Host "  $_" }

# --- Run SpotBugs ---
Write-Host ""
Write-Host "Running: mvn clean compile spotbugs:spotbugs" -ForegroundColor Cyan

$demoDir = Join-Path $PSScriptRoot "spotbugs-demo"
if (-not (Test-Path (Join-Path $demoDir "pom.xml"))) {
    Write-Host "ERROR: pom.xml not found in $demoDir" -ForegroundColor Red
    exit 1
}

Push-Location $demoDir
try {
    & mvn clean compile spotbugs:spotbugs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Maven build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

# --- Report location ---
$reportPath = Join-Path $demoDir "target\spotbugsXml.xml"
$htmlReportPath = Join-Path $demoDir "target\site\spotbugs.html"

Write-Host ""
Write-Host "=== SpotBugs Analysis Complete ===" -ForegroundColor Green

if (Test-Path $reportPath) {
    Write-Host "XML report: $reportPath" -ForegroundColor Green
} else {
    Write-Host "XML report not found at expected path: $reportPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "To generate the HTML report, run:" -ForegroundColor Cyan
Write-Host "  cd $demoDir" -ForegroundColor White
Write-Host "  mvn spotbugs:spotbugs site" -ForegroundColor White
Write-Host ""
Write-Host "Or generate HTML directly:" -ForegroundColor Cyan
Write-Host "  mvn spotbugs:gui" -ForegroundColor White
Write-Host ""
Write-Host "Expected HTML report path: $htmlReportPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Done." -ForegroundColor Green
