# Architecture Cleanup Handoff — RoleName and Region-Based Task/Annotation Refactor

## 1. Session Summary

Completed a two-part architecture cleanup:

1. **RoleId → RoleName migration**: Removed all UI/business-facing RoleId usage and migrated to RoleName. Updated DTOs, services, and UI to use role name strings instead of GUIDs. Removed the fake `MapRoleName(Guid)` helper and GUID-range role validation (`MinRoleId`/`MaxRoleId`). UI now displays `RoleName` directly.

2. **ChapterPageTask and ChapterPageAnnotation region-based refactor**: Removed the explicit C# `ChapterPageTaskRegion` entity/service/DTO layer and replaced it with EF Core skip navigations. Both `ChapterPageTask` and `ChapterPageAnnotation` now use `ICollection<PageRegion> PageRegions` collections, and `PageRegion` includes reverse navigations to `Tasks` and `Annotations`. The SQL junction tables `manga.ChapterPageTaskRegion` and `manga.ChapterPageAnnotationRegion` were preserved, and EF is mapped to use them via `UsingEntity<Dictionary<string, object>>`. `ChapterPageTask.ChapterPageId` and `ChapterPageAnnotation.PageRegionId` were removed; page and region context is now derived through the `PageRegions` collection.

The database already matched the target model (no `chapter_page_id` on task, no `page_region_id` on annotation, junction tables hold only the two FK columns), so no migration was needed.

## 2. Completed Changes

### RoleId → RoleName migration
- `UserDto.CreateUserDto` and `UpdateUserDto` changed to accept `RoleName` instead of `RoleId`
- `UserDto` exposes `RoleName` (already existed, now populated from `Role` nav)
- Removed `MinRoleId`/`MaxRoleId` and `EnsureValidRoleIdAsync` GUID-range validation
- Added `AllowedRoleNames` set with six valid role names
- `UserService.CreateUserAsync` now passes `RoleName` directly to stored procedure
- `UserService.MapToDto` populates `RoleName` from `u.Role?.RoleName`
- `UserRepository` methods `Include(u => u.Role)` for role population
- `UserApproval.razor` role column uses `@(context.RoleName ?? "-")`
- Removed `MapRoleName(Guid)`, `_selectedRolesByUserId`, `GetSelectedRole`, `SetSelectedRole`

### ChapterPageTask region-based model
- Removed `ChapterPageTask.ChapterPageId` navigation property
- Added `ICollection<PageRegion> PageRegions` to `ChapterPageTask`
- Task page context derived through `PageRegions → ChapterPageVersion → ChapterPage → Chapter`
- `CreateChapterPageTaskDto` / `UpdateChapterPageTaskDto` use `IReadOnlyList<Guid> PageRegionIds`
- `ChapterPageTaskDto` read response uses `IReadOnlyList<PageRegionDto> PageRegions`
- Service attaches regions from IDs on create/update

### ChapterPageAnnotation region-based model
- Removed `ChapterPageAnnotation.PageRegionId` navigation property
- Added `ICollection<PageRegion> PageRegions` to `ChapterPageAnnotation`
- Annotation region context derived through `PageRegions`
- `CreateChapterPageAnnotationDto` / `UpdateChapterPageAnnotationDto` use `IReadOnlyList<Guid> PageRegionIds`
- `ChapterPageAnnotationDto` read response uses `IReadOnlyList<PageRegionDto> PageRegions`
- Service filters by `PageRegions.Any(...)` instead of direct FK

### EF skip-navigation mapping
- `ChapterPageTask` ↔ `PageRegion` mapped to `manga.ChapterPageTaskRegion`
- `ChapterPageAnnotation` ↔ `PageRegion` mapped to `manga.ChapterPageAnnotationRegion`
- EF configuration uses `UsingEntity<Dictionary<string, object>>` to preserve SQL table/column names
- Both use existing constraint names (e.g., `fk_chapter_page_task_region_task`, `pk_chapter_page_task_region`)

### Removed explicit pure junction C# entity/service files
- Deleted `ChapterPageTaskRegion.cs` domain entity
- Deleted `ChapterPageTaskRegionConfiguration.cs`
- Deleted `ChapterPageTaskRegionService.cs`
- Deleted `IChapterPageTaskRegionService.cs`
- Deleted `ChapterPageTaskRegionDtos.cs`
- Removed `ChapterPageTaskRegions` from `IUnitOfWork` and `UnitOfWork`
- Removed `ChapterPageTaskRegion` DbSet from `ApplicationDbContext`
- Removed `IChapterPageTaskRegionService` DI registration

### Build result
- Build succeeded
- 0 errors
- 6 warnings (pre-existing: MailKit/MimeKit NuGet vulnerabilities, lowercase placeholder type names)

## 3. Files Changed

| File | Change |
|---|---|
| `src/MangaManagementSystem.Application/DTOs/Auth/UserDtos.cs` | `CreateUserDto`/`UpdateUserDto` use `RoleName`; `UserDto` exposes `RoleName` |
| `src/MangaManagementSystem.Application/Services/UserService.cs` | RoleName validation; pass role name to proc; populate `RoleName` in DTO |
| `src/MangaManagementSystem.Infrastructure/Repositories/UserRepository.cs` | `Include(u => u.Role)` in user reads |
| `src/MangaManagementSystem.Web/Components/Pages/Admin/UserApproval.razor` | Role column uses `RoleName`; removed `MapRoleName`, selection helpers |
| `src/MangaManagementSystem.Domain/Entities/ChapterPageTask.cs` | Removed `ChapterPageId`; added `PageRegions` collection |
| `src/MangaManagementSystem.Domain/Entities/ChapterPageAnnotation.cs` | Removed `PageRegionId`; added `PageRegions` collection |
| `src/MangaManagementSystem.Domain/Entities/PageRegion.cs` | Added `Tasks` and `Annotations` navigation collections |
| `src/MangaManagementSystem.Domain/Entities/ChapterPageTaskRegion.cs` | Deleted |
| `src/MangaManagementSystem.Domain/Interfaces/IUnitOfWork.cs` | Removed `ChapterPageTaskRegions` |
| `src/MangaManagementSystem.Infrastructure/Repositories/UnitOfWork.cs` | Removed `ChapterPageTaskRegions` |
| `src/MangaManagementSystem.Infrastructure/Persistence/ApplicationDbContext.cs` | Removed `ChapterPageTaskRegion` DbSet + interface |
| `src/MangaManagementSystem.Infrastructure/Persistence/Configurations/ChapterPageTaskConfiguration.cs` | Removed `ChapterPage` FK; added skip-nav mapping |
| `src/MangaManagementSystem.Infrastructure/Persistence/Configurations/ChapterPageAnnotationConfiguration.cs` | Removed `PageRegion` FK/index; added skip-nav mapping |
| `src/MangaManagementSystem.Infrastructure/Persistence/Configurations/ChapterPageTaskRegionConfiguration.cs` | Deleted |
| `src/MangaManagementSystem.Application/DTOs/Manga/ChapterPageTaskDtos.cs` | Use `PageRegionIds` (create/update); `PageRegions` (read) |
| `src/MangaManagementSystem.Application/DTOs/Manga/ChapterPageAnnotationDtos.cs` | Use `PageRegionIds` (create/update); `PageRegions` (read) |
| `src/MangaManagementSystem.Application/Services/ChapterPageTaskService.cs` | Attach `PageRegions` from IDs; map DTO with regions |
| `src/MangaManagementSystem.Application/Services/ChapterPageAnnotationService.cs` | Attach `PageRegions`; use `PageRegions.Any()` for filtering |
| `src/MangaManagementSystem.Application/DTOs/Manga/ChapterPageTaskRegionDtos.cs` | Deleted |
| `src/MangaManagementSystem.Application/Interfaces/IChapterPageTaskRegionService.cs` | Deleted |
| `src/MangaManagementSystem.Application/Services/ChapterPageTaskRegionService.cs` | Deleted |
| `src/MangaManagementSystem.Application/DependencyInjection.cs` | Removed `IChapterPageTaskRegionService` DI |

## 4. Important Architecture Decisions Confirmed

- The separate `MangaManagementSystem.API` project was deleted and should not be recreated.
- Web is the Blazor Server host project.
- UI/business-facing code should use `RoleName`, not `RoleId`.
- Database may still keep `role_id` as internal FK if required by the database schema.
- `ChapterPageTask` must not store or rely on `ChapterPageId`.
- Task page context is derived through: `ChapterPageTask → PageRegions → ChapterPageVersion → ChapterPage → Chapter`.
- `ChapterPageAnnotation` must not store direct single `PageRegionId`.
- Annotation region context is derived through: `ChapterPageAnnotation → PageRegions`.
- SQL junction tables `manga.ChapterPageTaskRegion` and `manga.ChapterPageAnnotationRegion` must remain unchanged.
- Explicit C# junction entity classes are not needed when the junction tables have no payload columns.
- EF uses skip navigations mapped to existing SQL junction tables via `UsingEntity<Dictionary<string, object>>`.

## 5. Current Build Result

```
Build succeeded.
    6 Warning(s)
    0 Error(s)
```

Warnings are pre-existing and unrelated:
- MailKit 4.2.0 moderate severity vulnerability (GHSA-9j88-vvj5-vhgr)
- MimeKit 4.2.0 moderate severity vulnerability (GHSA-g7hc-96xr-gvvx)
- MimeKit 4.2.0 high severity vulnerability (GHSA-gmc6-fwg3-75m5)
- Lowercase placeholder type names `common`, `enums`, `features` (CS8981)

## 6. Remaining Follow-up Tasks

1. **Route task/annotation create/resolve workflows through stored procedures**: The current services use direct EF persistence; the existing stored procedures (`usp_ChapterPageTask_Create`, `usp_ChapterPageAnnotation_Create`, `usp_ChapterPageAnnotation_Resolve`) already implement contributor/permission checks, same-page-version validation, and audit trails. Consider switching to stored procedure calls to inherit these behaviors.

2. **Add Include-based read methods for task/annotation**: `ChapterPageTasks.GetByIdAsync` and `ChapterPageAnnotations.GetByIdAsync` return empty `PageRegions` collections because the generic repo uses `FindAsync`. Add custom methods that `Include` the navigation if read DTOs need populated regions.

3. **Replace in-memory annotation filtering by PageRegionId**: `GetChapterPageAnnotationsByPageRegionIdAsync` loads all annotations and filters in memory. Replace with a repository method that filters in SQL.

4. **Clean placeholder files later**: `common.cs`, `enums.cs`, `features.cs` in Domain and Application are empty placeholders with no purpose.

5. **Smoke test UI flows**: Verify user approval, task creation/assignment, and annotation workflows after the refactor.

## 7. Next Recommended Prompt

```text
Before continuing any new coding task, ensure you have read the latest handoff file at:
  MangaManagementSystem/docs/revision/Mangaka/architecture-cleanup-handoff-2026-06-11-role-region-refactor.md

Current state:
- RoleId → RoleName migration is complete.
- ChapterPageTask and ChapterPageAnnotation are now region-based.
- SQL junction tables are preserved and EF skip navigations are mapped.
- Build succeeded with 0 errors.
- Remaining: consider routing task/annotation workflows through stored procedures.

Your next task:
1. Read the handoff file above.
2. Confirm the current state matches the documented state.
3. Propose whether to:
   a. Keep direct EF persistence for task/annotation creation (simpler, immediate), or
   b. Refactor to use the existing stored procedures (preserves permission/transaction/audit behavior).
4. If (b), outline the changes needed and provide a detailed plan before proceeding.

Do not proceed until you have confirmed the handoff state and received direction.
```

## 8. Notes / Risks

- **Current task/annotation services still use direct EF persistence**. While this worked during the refactor for speed and simplicity, direct EF persistence may bypass the SQL permission checks, same-page-version validation, transactions, and audit behavior that are already implemented in the stored procedures (`usp_ChapterPageTask_Create`, `usp_ChapterPageAnnotation_Create`, `usp_ChapterPageAnnotation_Resolve`).

- **Stored procedures already accept `PageRegionIds` JSON** and should be reused if possible. The stored procedure signatures already expect an array of region IDs (`@page_region_ids_json NVARCHAR(MAX)`), so the DTO changes in this session align perfectly with the stored procedure contract.

- **EF skip navigations are only for reads/navigation convenience**. The `AttachPageRegionsAsync` method in the services fetches `PageRegion` entities individually and adds them to the `PageRegions` collection, which EF persists via the junction table on save. This is acceptable for small-scale updates but could be optimized if bulk region linking becomes a bottleneck.

- **No database migration was needed** because the existing schema already matched the target model. This is a rare win and should be kept in mind for future schema decisions.