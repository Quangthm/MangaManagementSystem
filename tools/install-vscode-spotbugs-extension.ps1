$ErrorActionPreference = "Stop"

$Extensions = @(
    "shblue21.vscode-spotbugs",
    "redhat.java",
    "vscjava.vscode-java-pack"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Installing VS Code Extensions for SpotBugs" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

foreach ($ext in $Extensions) {
    Write-Host "[INFO] Installing: $ext ..." -ForegroundColor Yellow
    try {
        $output = code --install-extension $ext 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  => SUCCESS" -ForegroundColor Green
        } else {
            Write-Host "  => FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
            Write-Host "  => $output" -ForegroundColor Red
        }
    } catch {
        Write-Host "  => ERROR: $_" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "[INFO] VS Code SpotBugs extension setup complete." -ForegroundColor Cyan
Write-Host ""
Write-Host "Recommended extensions installed:" -ForegroundColor White
Write-Host "  - shblue21.vscode-spotbugs   : SpotBugs integration" -ForegroundColor Gray
Write-Host "  - redhat.java                : Language Support for Java" -ForegroundColor Gray
Write-Host "  - vscjava.vscode-java-pack   : Java Extension Pack" -ForegroundColor Gray
Write-Host ""
Write-Host "To install manually, press Ctrl+Shift+X in VS Code and search for each extension." -ForegroundColor Yellow
