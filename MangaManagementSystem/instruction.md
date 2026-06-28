# Local Secrets Configuration Instructions

To protect API keys and sensitive credentials in local development, this project uses .NET User Secrets. This prevents keys from being committed to GitHub.

## Initial Setup for Developers

When you clone this project, run the helper PowerShell script to automatically import the default development credentials into your local machine's User Secrets storage.

### Steps to Run:

1. Open a PowerShell terminal in the root of the project repository.
2. Execute the setup script:
   ```powershell
   ./setup-secrets.ps1
   ```
   *Note: If you encounter an execution policy error, you can run:*
   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   ./setup-secrets.ps1
   ```

### Overriding Values Manually:

You can update or customize any credential key at any time using the .NET CLI:
```bash
dotnet user-secrets set "Smtp:Password" "YOUR_NEW_PASSWORD" --project src/MangaManagementSystem.Web
dotnet user-secrets set "Cloudinary:ApiSecret" "YOUR_NEW_SECRET" --project src/MangaManagementSystem.Web
```

To list your current local secrets:
```bash
dotnet user-secrets list --project src/MangaManagementSystem.Web
```

---

# Docker Local Development Instructions

This project also includes a helper PowerShell script for running the local Docker development environment.

The script starts the required Docker containers for SQL Server, the API, optional Web, and optional SonarQube.

## Prerequisites

Before running Docker locally, make sure you have:

- Docker Desktop installed and running.
- .NET SDK installed.
- PowerShell terminal opened at the project repository root.
- A local `.env` file created from `.env.example`.

## First-Time Docker Setup

From the project root, copy `.env.example` to `.env`:

```powershell
Copy-Item .env.example .env
```

Then open `.env` and update the values for your local machine.

At minimum, check these values:

```env
SA_PASSWORD=ChangeThisSql_2026!Dev
DB_NAME=MangaManagementDB

JWT_KEY=change_this_to_a_long_random_key_at_least_32_characters
JWT_ISSUER=MangaManagementSystem
JWT_AUDIENCE=MangaManagementSystemUsers

CLOUDINARY_CLOUD_NAME=change_me
CLOUDINARY_API_KEY=change_me
CLOUDINARY_API_SECRET=change_me

SONARQUBE_JDBC_USERNAME=sonar
SONARQUBE_JDBC_PASSWORD=SonarDb_2026!Dev
SONARQUBE_DB=sonarqube
```

Do not commit `.env` to Git.

## Running the Docker Helper Script

The helper script is located at:

```powershell
.\scripts\docker-dev.ps1
```

If PowerShell blocks script execution, run this first:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

## Start SQL Server and API

Use this command for the normal backend Docker setup:

```powershell
.\scripts\docker-dev.ps1 -Build
```

This starts:

```text
manga-sqlserver
manga-api
```

After it starts, open:

```text
API Swagger: http://localhost:7256/swagger
SQL Server:  localhost,14333
```

For SSMS, use:

```text
Server: localhost,14333
Authentication: SQL Server Authentication
Login: sa
Password: value from SA_PASSWORD in .env
Trust Server Certificate: checked
```

## Start SQL Server, API, and Web

If the Web project is ready to run through Docker, use:

```powershell
.\scripts\docker-dev.ps1 -Build -Web
```

This starts:

```text
manga-sqlserver
manga-api
manga-web
```

Open:

```text
Web:         http://localhost:5244
API Swagger: http://localhost:7256/swagger
```

Important Docker networking rule:

```text
Browser on Windows -> API:
http://localhost:7256

Web container -> API container:
http://manga-api:8080

API container -> SQL Server container:
manga-sqlserver,1433

SSMS on Windows -> Docker SQL Server:
localhost,14333
```

## Start SonarQube

To start SonarQube locally:

```powershell
.\scripts\docker-dev.ps1 -Sonar
```

Open:

```text
http://localhost:9000
```

First login:

```text
Username: admin
Password: admin
```

SonarQube will ask you to change the admin password after first login.

## Start Full Local Stack

To start SQL Server, API, Web, and SonarQube together:

```powershell
.\scripts\docker-dev.ps1 -Build -Web -Sonar
```

To include logs after startup:

```powershell
.\scripts\docker-dev.ps1 -Build -Web -Sonar -Logs
```

## Check Container Status

```powershell
.\scripts\docker-dev.ps1 -Status
```

You can also check manually with Docker:

```powershell
docker ps
```

## View Logs

Show logs through the helper script:

```powershell
.\scripts\docker-dev.ps1 -Logs
```

Or view specific containers manually:

```powershell
docker logs manga-sqlserver --tail 80
docker logs manga-api --tail 100
docker logs manga-web --tail 100
docker logs sonarqube --tail 100
```

## Stop Containers

Stop SQL Server and API:

```powershell
.\scripts\docker-dev.ps1 -Stop
```

Stop SQL Server, API, and Web:

```powershell
.\scripts\docker-dev.ps1 -Stop -Web
```

Stop SQL Server, API, Web, and SonarQube:

```powershell
.\scripts\docker-dev.ps1 -Stop -Web -Sonar
```

## Reset SonarQube Only

If SonarQube fails because of an old database volume or upgrade issue, reset only SonarQube:

```powershell
.\scripts\docker-dev.ps1 -ResetSonar
```

This removes SonarQube containers and SonarQube volumes, then recreates them.

Do not use this unless you are okay losing local SonarQube project data and tokens.

## SonarQube Project Scan

After SonarQube is running, create a project manually in the SonarQube UI.

Recommended values:

```text
Project display name: MangaManagementSystem
Project key: MangaManagementSystem
Main branch: main
```

Then choose local analysis and generate a token.

Install the SonarScanner tool:

```powershell
dotnet tool install --global dotnet-sonarscanner
```

If it is already installed, update it:

```powershell
dotnet tool update --global dotnet-sonarscanner
```

Run the scan from the project root:

```powershell
dotnet sonarscanner begin `
  /k:"MangaManagementSystem" `
  /d:sonar.host.url="http://localhost:9000" `
  /d:sonar.token="PASTE_TOKEN_HERE"

dotnet build .\MangaManagementSystem.slnx --no-incremental

dotnet sonarscanner end `
  /d:sonar.token="PASTE_TOKEN_HERE"
```

Then refresh the SonarQube project page.

## SonarQube Coverage Note

If SonarQube shows `0% Coverage`, it means no test coverage report was generated or imported.

The normal scan command checks code quality, but it does not automatically create test coverage.

Coverage requires:

```text
1. Test projects exist.
2. Tests are executed.
3. A coverage report is generated.
4. SonarQube imports the coverage report.
```

## Files That Should Be Committed

Commit these files:

```text
docker-compose.yml
.dockerignore
.gitignore
.env.example
scripts/docker-dev.ps1
src/MangaManagementSystem.API/Dockerfile
src/MangaManagementSystem.Web/Dockerfile
```

Do not commit these files:

```text
.env
*.bak
.sonarqube/
.scannerwork/
coverage.xml
TestResults/
SonarQube token
SQL password
Cloudinary secret
Docker volume data
```

Suggested commit command:

```powershell
git add docker-compose.yml .dockerignore .gitignore .env.example scripts/docker-dev.ps1
git add src/MangaManagementSystem.API/Dockerfile
git add src/MangaManagementSystem.Web/Dockerfile
git commit -m "Add Docker and SonarQube local setup"
```
