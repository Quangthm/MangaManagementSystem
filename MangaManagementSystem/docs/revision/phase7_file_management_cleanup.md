\# Phase 7 - File Management Cleanup



\## Branch



feature/phase7-file-cleanup-no-storage-status



Created from latest origin/main. No work was done directly on main.



\## Summary



Implemented File Management Cleanup flow based on leader direction.



Main points:



1\. Removed dependency on manga.FileResource.storage\_cleanup\_status from source-code design.

2\. Added derived storage state using:

&#x20;  - deleted\_at\_utc

&#x20;  - storage\_cleaned\_at\_utc

&#x20;  - storage\_cleanup\_error

3\. Added /admin/files page.

4\. Admin file list/reload only reads database records.

5\. Backend does not call Cloudinary during list/search/reload.

6\. Cloudinary is called only when Admin clicks Cleanup or Cleanup All.

7\. Cleanup All filters database candidates first:

&#x20;  - deleted\_at\_utc IS NOT NULL

&#x20;  - storage\_cleaned\_at\_utc IS NULL

8\. Cloudinary delete result ok and not found are both treated as cleanup completed.



\## Repository Finding



Latest origin/main did not contain the old File Management Cleanup implementation.



No source references were found for:



\- storage\_cleanup\_status

\- AdminFiles

\- usp\_Admin\_FileResource\_Search

\- usp\_Admin\_FileResource\_GetById

\- usp\_FileResource\_GetStorageCleanupCandidates

\- usp\_FileResource\_UpdateStorageCleanupResult



Therefore, this branch implements the missing flow from current origin/main instead of modifying an existing /admin/files implementation.



\## Files Changed / Added



\### Domain



\- src/MangaManagementSystem.Domain/Entities/FileResource.cs

\- src/MangaManagementSystem.Domain/Interfaces/IFileResourceRepository.cs

\- src/MangaManagementSystem.Domain/Interfaces/IUnitOfWork.cs



\### Application



\- src/MangaManagementSystem.Application/DTOs/Manga/FileResourceDtos.cs

\- src/MangaManagementSystem.Application/Interfaces/IFileResourceService.cs

\- src/MangaManagementSystem.Application/Interfaces/IFileStorageService.cs

\- src/MangaManagementSystem.Application/Services/FileResourceService.cs



\### Infrastructure



\- src/MangaManagementSystem.Infrastructure/DependencyInjection.cs

\- src/MangaManagementSystem.Infrastructure/Persistence/Configurations/FileResourceConfiguration.cs

\- src/MangaManagementSystem.Infrastructure/Repositories/FileResourceRepository.cs

\- src/MangaManagementSystem.Infrastructure/Repositories/UnitOfWork.cs

\- src/MangaManagementSystem.Infrastructure/Services/CloudinaryFileStorageService.cs



\### Web



\- src/MangaManagementSystem.Web/Components/Pages/Admin/AdminFiles.razor

\- src/MangaManagementSystem.Web/Components/Pages/Admin/AdminDashboard.razor



\### Revision / SQL Note



\- docs/revision/phase7\_file\_cleanup\_schema\_note.sql

\- docs/revision/phase7\_file\_management\_cleanup.md



\## Storage State Rules



The source-code design does not use storage\_cleanup\_status.



Active:



deleted\_at\_utc IS NULL



Pending cleanup:



deleted\_at\_utc IS NOT NULL

AND storage\_cleaned\_at\_utc IS NULL

AND storage\_cleanup\_error IS NULL



Cleaned:



deleted\_at\_utc IS NOT NULL

AND storage\_cleaned\_at\_utc IS NOT NULL



Failed:



deleted\_at\_utc IS NOT NULL

AND storage\_cleaned\_at\_utc IS NULL

AND storage\_cleanup\_error IS NOT NULL



\## Database Note



No real database was modified.



The current source code expects database support for:



\- manga.FileResource.storage\_cleaned\_at\_utc

\- manga.FileResource.storage\_cleanup\_error



A SQL review note was created:



docs/revision/phase7\_file\_cleanup\_schema\_note.sql



Leader/database owner should review before applying schema changes.



\## Build Result



Command:



dotnet build .\\MangaManagementSystem.slnx



Result:



Build succeeded with 19 warning(s)



Warnings are existing project warnings unrelated to this implementation.



\## Not Done / Needs Leader Review



1\. Real database was not changed.

2\. Stored procedures were not modified because latest origin/main did not contain the referenced cleanup procedures.

3\. Schema changes must be reviewed before runtime testing against a real database.

4\. If the real database still contains storage\_cleanup\_status, dependencies must be checked before dropping it.



\## Safety Notes



\- Did not work directly on main.

\- Did not merge another branch into main.

\- Did not run database migration or SQL against a real DB.

\- Did not use git add -A.

\- Cleanup flow avoids Cloudinary checks during admin list/reload.


## Follow-up SQL Source Alignment

After checking root SQL scripts, the source SQL files were also aligned with the cleanup flow:

- MangaManagementSystem_Schema.sql
  - Added ix_file_resource_storage_cleanup_candidates.

- MangaManagementSystem_Procedures_Views_Bootstrap.sql
  - Updated manga.usp_FileResource_SoftDelete to reset storage cleanup fields when a file is soft-deleted.

No SQL was executed against a real database.
