# Setup secrets for MangaManagementSystem
# Usage:
#   .\setup-secrets.ps1                    # Both (default)
#   .\setup-secrets.ps1 -Project Web       # Web only
#   .\setup-secrets.ps1 -Project API       # API only
#   .\setup-secrets.ps1 -Project Both      # Both explicitly
#   .\setup-secrets.ps1 -AllowPlaceholders # Allow placeholder values (for CI/template)

param(
    [ValidateSet("Web", "API", "Both")]
    [string]$Project = "Both",

    [switch]$AllowPlaceholders
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# ---------------------------------------------------------------------------
# Shared secrets table — same values applied to each selected project.
# Replace any PUT_*_HERE placeholder before running.
# ---------------------------------------------------------------------------
$secrets = @{
    "ConnectionStrings:DefaultConnection" = "Server=localhost;Database=MangaManagementDB;User Id=sa;Password=12345;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
    "Smtp:Username"                       = "qtang889706@gmail.com"
    "Smtp:Password"                       = "xckv xpjt fpmf gsqu"
    "Smtp:FromEmail"                      = "qtang889706@gmail.com"
    "Jwt:Key"                             = "46FpzM4RLUOCuoHSq6rzkJTr3iAnvjT7zT3Du3A3GNsyMyrC+2nWeNRZbpVrRHLV9wgM4BtwurkTrODPoJypSg=="
    "Jwt:Issuer"                          = "MangaManagementSystem"
    "Jwt:Audience"                        = "MangaManagementSystemUsers"
    "Authentication:Google:ClientId"      = "567365691593-d5ohser29b7plhhutt8jifqq0kg0us94.apps.googleusercontent.com"
    "Authentication:Google:ClientSecret"  = "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"
    "Cloudinary:CloudName"                = "dvpbtdju8"
    "Cloudinary:ApiKey"                   = "159224476371744"
    "Cloudinary:ApiSecret"                = "-zpW6kN0wiVxrSJbATXe3J43KMY"
    "Recaptcha:SiteKey"                   = "6Lfz5TctAAAAABH4t_FH5KMZVWA1fEaNYVOGqwlO"
    "Recaptcha:SecretKey"                 = "6Lfz5TctAAAAAGF9VlQkT2z_YwXONxFxiTno_Hxi"
}

# ---------------------------------------------------------------------------
# Placeholder validation
# ---------------------------------------------------------------------------
$placeholders = @()
foreach ($key in $secrets.Keys) {
    $val = $secrets[$key]
    if ($val -like "PUT_*" -or $val -like "*_HERE*") {
        $placeholders += $key
    }
}

if ($placeholders.Count -gt 0 -and -not $AllowPlaceholders) {
    Write-Host "ERROR: The following secrets still contain placeholder values:" -ForegroundColor Red
    foreach ($k in $placeholders) {
        Write-Host "  $k" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Edit setup-secrets.ps1 and replace the PUT_*_HERE values with real secrets," -ForegroundColor Yellow
    Write-Host "or re-run with -AllowPlaceholders to accept placeholders (for CI/template only)." -ForegroundColor Yellow
    exit 1
}

# ---------------------------------------------------------------------------
# Helper: apply secrets to one project
# ---------------------------------------------------------------------------
function Install-SecretsForProject {
    param([string]$ProjectFile, [string]$Label, [hashtable]$SecretsTable)

    $resolved = Resolve-Path $ProjectFile -ErrorAction Stop
    Write-Host ""
    Write-Host "Configuring $Label ..." -ForegroundColor Cyan

    # Ensure secrets store is initialized
    dotnet user-secrets init --project $resolved 2>&1 | Out-Null

    foreach ($key in $SecretsTable.Keys) {
        $value = $SecretsTable[$key]
        Write-Host "  $key"
        dotnet user-secrets set "$key" "$value" --project $resolved 2>&1 | Out-Null
    }

    Write-Host "  done" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Project paths
# ---------------------------------------------------------------------------
$webProj  = Join-Path $root "src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj"
$apiProj  = Join-Path $root "src/MangaManagementSystem.API/MangaManagementSystem.API.csproj"

# ---------------------------------------------------------------------------
# Apply to selected projects
# ---------------------------------------------------------------------------
Write-Host "MangaManagementSystem - User Secrets Setup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

switch ($Project) {
    "Web"  { Install-SecretsForProject -ProjectFile $webProj -Label "MangaManagementSystem.Web"  -SecretsTable $secrets }
    "API"  { Install-SecretsForProject -ProjectFile $apiProj -Label "MangaManagementSystem.API"  -SecretsTable $secrets }
    "Both" {
        Install-SecretsForProject -ProjectFile $webProj -Label "MangaManagementSystem.Web"  -SecretsTable $secrets
        Install-SecretsForProject -ProjectFile $apiProj -Label "MangaManagementSystem.API"  -SecretsTable $secrets
    }
}

Write-Host ""
Write-Host "All secrets configured successfully." -ForegroundColor Green
