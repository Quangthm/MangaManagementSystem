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
