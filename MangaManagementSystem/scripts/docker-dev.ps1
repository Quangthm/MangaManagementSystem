param(
    [switch]$Build,
    [switch]$Web,
    [switch]$Sonar,
    [switch]$Logs,
    [switch]$Status,
    [switch]$Stop,
    [switch]$ResetSonar
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Write-Warn($message) {
    Write-Host ""
    Write-Host "WARNING: $message" -ForegroundColor Yellow
}

# Move to repo root based on script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $RepoRoot

Write-Step "Checking project files"

if (!(Test-Path ".\docker-compose.yml")) {
    throw "docker-compose.yml not found. Run this script from the repository that contains docker-compose.yml."
}

if (!(Test-Path ".\.env")) {
    if (Test-Path ".\.env.example") {
        Copy-Item ".\.env.example" ".\.env"
        Write-Warn ".env was missing, so .env.example was copied to .env."
        Write-Warn "Please edit .env first, then run this script again."
        exit 1
    }
    else {
        throw ".env not found and .env.example not found. Create .env before running Docker."
    }
}

Write-Step "Checking Docker"
docker version | Out-Null
docker compose version | Out-Null

if ($Status) {
    Write-Step "Docker container status"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    exit 0
}

if ($ResetSonar) {
    Write-Warn "This will delete only SonarQube containers and SonarQube volumes."
    Write-Warn "It should not delete the Manga SQL Server volume unless your volume names are incorrect."

    docker compose stop sonarqube sonarqube-db 2>$null
    docker compose rm -f sonarqube sonarqube-db 2>$null

    $sonarVolumes = docker volume ls --format "{{.Name}}" | Select-String "sonarqube"

    foreach ($volume in $sonarVolumes) {
        Write-Host "Removing volume: $($volume.Line)" -ForegroundColor Yellow
        docker volume rm $volume.Line
    }

    Write-Step "Recreating SonarQube"
    docker compose up -d sonarqube-db sonarqube
    docker logs -f sonarqube
    exit 0
}

$services = @("manga-sqlserver", "manga-api")

if ($Web) {
    $services += "manga-web"
}

if ($Sonar) {
    $services += @("sonarqube-db", "sonarqube")
}

if ($Stop) {
    Write-Step "Stopping selected services"
    docker compose stop $services
    exit 0
}

Write-Step "Starting services: $($services -join ', ')"

if ($Build) {
    docker compose up -d --build $services
}
else {
    docker compose up -d $services
}

Write-Step "Container status"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

if ($Logs) {
    Write-Step "SQL Server logs"
    docker logs manga-sqlserver --tail 80

    Write-Step "API logs"
    docker logs manga-api --tail 100

    if ($Web) {
        Write-Step "Web logs"
        docker logs manga-web --tail 100
    }

    if ($Sonar) {
        Write-Step "SonarQube logs"
        docker logs sonarqube --tail 120
    }
}

Write-Step "Useful URLs"

Write-Host "API Swagger:  http://localhost:7256/swagger"
Write-Host "Web:          http://localhost:5244"
Write-Host "SonarQube:    http://localhost:9000"
Write-Host "SQL Server:   localhost,14333"
Write-Host ""
Write-Host "SSMS Login:"
Write-Host "  Server: localhost,14333"
Write-Host "  User:   sa"
Write-Host "  Pass:   value from SA_PASSWORD in .env"