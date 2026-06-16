\# SCRUM-125 — Stream F Profile Features Session



\## Task Information



\- Jira parent task: SCRUM-125

\- Stream: Stream F — General System User Profile Features

\- Related subtasks:

&#x20; - SCRUM-126: Update display name without password

&#x20; - SCRUM-127: Show display\_name in UI

&#x20; - SCRUM-128: Avatar upload

&#x20; - SCRUM-129: Portfolio upload

&#x20; - SCRUM-130: Safe placeholder for unavailable files



\## Work Completed



\### Display Name Update



Implemented display name update without requiring password.



Behavior:

\- User can update display\_name from Creator Workspace.

\- Empty or whitespace-only display names are rejected.

\- username, email, role, password, and account status are not changed.

\- Updated display\_name is shown in the workspace UI.



Files changed:

\- `src/MangaManagementSystem.Application/DTOs/Manga/DisplayNameUpdateDto.cs`

\- `src/MangaManagementSystem.Application/Interfaces/IUserService.cs`

\- `src/MangaManagementSystem.Application/Services/UserService.cs`

\- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`



\### Avatar Upload



Implemented avatar upload in Creator Workspace.



Behavior:

\- Supports PNG, JPG, JPEG, and WEBP.

\- Uploaded avatar is stored through `FileResource`.

\- `avatar\_file\_id` is updated on the user record.

\- Avatar is displayed in the workspace UI.



Files changed:

\- `src/MangaManagementSystem.Application/Interfaces/IUserService.cs`

\- `src/MangaManagementSystem.Application/Services/UserService.cs`

\- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`



\### Portfolio Upload



Implemented portfolio upload in Creator Workspace.



Behavior:

\- Supports PDF, DOC, DOCX, PNG, JPG, JPEG, and WEBP.

\- Uploaded portfolio is stored through `FileResource`.

\- `portfolio\_file\_id` is updated on the user record.

\- Open portfolio button appears when portfolio exists.



Files changed:

\- `src/MangaManagementSystem.Application/Interfaces/IUserService.cs`

\- `src/MangaManagementSystem.Application/Services/UserService.cs`

\- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`



\### Safe Placeholder



Implemented safe placeholder behavior for unavailable files.



Behavior:

\- Avatar falls back to user initial when avatar URL is unavailable.

\- Portfolio shows safe info message instead of a broken file link when unavailable.



File changed:

\- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`



\## Configuration Notes



\### reCAPTCHA



Local development reCAPTCHA was disabled for testing login.



Files changed:

\- `src/MangaManagementSystem.Web/Options/RecaptchaOptions.cs`

\- `src/MangaManagementSystem.Web/Program.cs`

\- `src/MangaManagementSystem.Web/appsettings.Development.json`



No real reCAPTCHA secret is committed.



\### Cloudinary



Cloudinary credentials were configured with .NET User Secrets only.



Important:

\- Do not commit Cloudinary API key or API secret.

\- Do not put real Cloudinary values into `appsettings.json`.

\- `appsettings.json` keeps placeholder values only.



\## Local Testing



Test account:

\- Username: TestMangaka

\- Role: Mangaka



Test URL:

\- `/mangaka/workspace/test-series`



Test results:

\- Display name update: Passed

\- Display name shown in UI: Passed

\- Avatar upload: Passed

\- Portfolio upload: Passed

\- Safe placeholder for missing portfolio: Passed



\## Database Verification



Verified in SQL Server:



Table: `auth.Users`

\- `display\_name` updated.

\- `avatar\_file\_id` populated.

\- `portfolio\_file\_id` populated.



Table: `manga.FileResource`

\- `USER\_AVATAR` record created.

\- `REGISTRATION\_PORTFOLIO` record created.

\- `cloudinary\_secure\_url` populated.

\- `sha256\_hash` populated.

\- `uploaded\_by\_user\_id` populated.



\## Security Notes



\- Cloudinary credentials must remain in local User Secrets only.

\- Do not commit credential files.

\- Run `git diff` before commit to ensure no secrets are included.

\- Cloudinary API Secret should be rotated after task completion if it was exposed during troubleshooting.



\## Current Status



Implementation and local testing are complete.



Next steps:

\- Create feature branch.

\- Commit code changes.

\- Push branch.

\- Open Pull Request.

\- Move SCRUM-126 to SCRUM-130 to Done/In Review based on team workflow.

