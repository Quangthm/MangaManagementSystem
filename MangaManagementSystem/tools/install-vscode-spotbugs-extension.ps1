# install-vscode-spotbugs-extension.ps1
# Installs VS Code extensions for SpotBugs and Java support.
# This is a convenience script only -- failure does not affect the repository.

$ErrorActionPreference = "Continue"

Write-Host "=== VS Code SpotBugs Extension Installer ===" -ForegroundColor Cyan

$codeCmd = Get-Command code -ErrorAction SilentlyContinue

if (-not $codeCmd) {
    Write-Host ""
    Write-Host "The 'code' CLI command was not found on your PATH." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To install the extensions manually:" -ForegroundColor Cyan
    Write-Host "  1. Open VS Code" -ForegroundColor White
    Write-Host "  2. Go to Extensions (Ctrl+Shift+X)" -ForegroundColor White
    Write-Host "  3. Search for 'SpotBugs' and install the extension by shblue21" -ForegroundColor White
    Write-Host "  4. Search for 'Language Support for Java' and install the extension by Red Hat" -ForegroundColor White
    Write-Host "  5. Optionally search for 'Java Extension Pack' by Microsoft" -ForegroundColor White
    Write-Host ""
    Write-Host "Extension IDs:" -ForegroundColor Gray
    Write-Host "  shblue21.vscode-spotbugs" -ForegroundColor Gray
    Write-Host "  redhat.java" -ForegroundColor Gray
    Write-Host "  vscjava.vscode-java-pack" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

Write-Host ""
Write-Host "Installing SpotBugs extension..." -ForegroundColor Yellow
& code --install-extension shblue21.vscode-spotbugs

Write-Host ""
Write-Host "Installing Java Language Support (Red Hat)..." -ForegroundColor Yellow
& code --install-extension redhat.java

Write-Host ""
Write-Host "Installing Java Extension Pack (optional)..." -ForegroundColor Yellow
& code --install-extension vscjava.vscode-java-pack

Write-Host ""
Write-Host "Done. VS Code extensions installed." -ForegroundColor Green
