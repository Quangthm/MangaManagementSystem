# Quick Select Task Assignment — Phase 3 Backend

## Branch

`feature/Mangaka`

## Date

2026-06-25

## Task summary

Implemented Quick Select Task Assignment backend — Phase 3 only. Three read endpoints and one POST endpoint to create multiple assigned ChapterPageTask records in a single batch. Application validates, Infrastructure persists with EF batch insert + transaction + SQL app lock. No stored procedures were created or modified for this workflow.

No Quick Select dialog UI was implemented. UI implementation is Phase 4.

## Scope

### In scope
- Quick Select read endpoints (chapters, pages/current versions, active Assistant contributors)
- POST quick-select endpoint with full validation
- Application validation and coordination
- Infrastructure EF batch persistence with transaction + app lock
- FULL_PAGE PageRegion create/reuse using Cloudinary image bounds
- One AuditEvent per created ChapterPageTask
- Backend docs + revision note update

### Out of scope
- Quick Select dialog UI
- Web API client UI wiring
- Stored procedure batch
- Single-task SP loop
- FileResource dimension columns

## Architecture path

```text
Web (future)
  -> typed API client (future)
    -> QuickSelectController (thin API controller)
      -> IQuickSelectService (Application validation + plan building)
        -> IQuickSelectRepository (Infrastructure read queries + EF batch write)
          -> IImageMetadataProvider (Cloudinary image bounds, pre-transaction)
            -> ApplicationDbContext (EF Core, transaction, app lock, SaveChangesAsync)
```

## Clean Architecture placement

| Layer | Responsibility |
|-------|---------------|
| **API** | Thin controller: actor extraction, HTTP response mapping, safe error messages |
| **Application** | Business validation, request normalization, plan building, Cloudinary coordination |
| **Infrastructure** | EF read queries (AsNoTracking), transaction ownership, app lock, guard re-checks, batch insert |
| **Domain** | Uses existing entities only (ChapterPageTask, PageRegion, AuditEvent, etc.) |

## API endpoints added

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/mangaka/series/{seriesId}/chapters/quick-select` | Returns chapters for series (actor must be active Mangaka contributor) |
| GET | `/api/mangaka/chapters/{chapterId}/pages/quick-select` | Returns pages with current version for chapter |
| GET | `/api/mangaka/series/{seriesId}/assistants/quick-select` | Returns ACTIVE Assistant contributors for series |
| POST | `/api/mangaka/tasks/quick-select` | Creates batch of ASSIGNED tasks with FULL_PAGE regions |

## Application validation

Before persistence, QuickSelectService validates:
- actorUserId is not empty
- SeriesId is not empty
- ChapterId is not empty
- AssignedToUserId is not empty
- Pages is not empty
- No duplicate ChapterPageIds
- No duplicate ChapterPageVersionIds
- TypeCode is a valid ChapterPageTask type (BACKGROUND/SHADING/EFFECTS/CLEANUP/DIALOGUE/TYPESETTING/REVIEW/OTHER)
- TaskTitlePrefix is not empty after trim
- DefaultTaskDescription is not empty after trim
- PriorityLevel is between 1 and 5
- DueAtUtc is provided
- CompensationAmount >= 0
- Actor is active Mangaka contributor of selected series
- Assigned user is ACTIVE Assistant contributor of selected series
- Chapter belongs to selected series
- All selected ChapterPageIds belong to selected Chapter
- All supplied ChapterPageVersionIds belong to their ChapterPageIds
- Each supplied ChapterPageVersionId is current
- DescriptionOverride is trimmed if supplied
- Cloudinary image bounds are available for every selected current page version

## Cloudinary image bounds usage

- Uses existing `IImageMetadataProvider` / `CloudinaryImageMetadataProvider`
- Dimensions resolved before opening SQL transaction (no DB transaction during Cloudinary call)
- `FileResource` does not store image dimensions
- Cache image metadata per request (single lookup per distinct public ID)
- Error: "Selected page image dimensions could not be loaded. No tasks were created."

## FULL_PAGE PageRegion implementation

For each selected page version:
- Find existing FULL_PAGE PageRegion (`type_code = FULL_PAGE`) for that ChapterPageVersion
- If exists, reuse it
- If not, create a new one with:
  - `type_code = FULL_PAGE`
  - `region_label = "Full page"`
  - `x = 0, y = 0`
  - `width, height = Cloudinary image dimensions`
  - `source_type = MANUAL`
  - `confidence_score = NULL`
  - `original_text = NULL`
  - `created_by_user_id = actorUserId`
  - `updated_at_utc = NULL`
  - `updated_by_user_id = NULL`
- PageRegion IDs generated in C# with `Guid.NewGuid()`

## Infrastructure EF batch persistence

1. Open transaction (`_context.Database.BeginTransactionAsync`)
2. Acquire SQL app lock (`sp_getapplock`) with resource `manga_quick_select_task_assignment_{actorUserId}_{seriesId}_{chapterId}`
3. Re-check critical guards (actor still active Mangaka contributor, assistant still ACTIVE contributor, chapter still belongs to series, pages still belong to chapter, versions still current and point to same page_file_id)
4. Find/reuse existing FULL_PAGE PageRegions
5. Create missing FULL_PAGE PageRegions
6. Create N ChapterPageTask entities with `ChapterPageTaskId` generated by `Guid.NewGuid()`
7. Link each task to its FULL_PAGE PageRegion via the skip-navigation `PageRegions` collection (EF creates ChapterPageTaskRegion junction rows)
8. Create N AuditEvent entities
9. `SaveChangesAsync` once
10. Commit

Same `createdAtUtc` value used across all entities in the batch.

## Transaction/race-condition handling

- `CreateExecutionStrategy` wrapping for retry
- `sp_getapplock` with `@LockMode = Exclusive`, `@LockOwner = Transaction`, timeout 5000ms
- Lock failure returns: "Another Quick Select assignment is already in progress for this chapter. Please wait and try again."
- All guard re-checks inside transaction protect against:
  - Assistant removal after validation
  - Actor access removal after validation
  - Page version changes after validation
  - Page file changes after Cloudinary resolution
  - Concurrent FULL_PAGE region creation
- Rollback on any guard failure prevents partial writes

## Audit design

- One `AuditEvent` per created `ChapterPageTask`
- `action_code = CHAPTER_PAGE_TASK_CREATED`
- `entity_type = ChapterPageTask`
- `entity_id = created task ID as string`
- `detail_json` matches existing SP audit shape:
  ```json
  {
    "assigned_to_user_id": "guid",
    "type_code": "CLEANUP",
    "task_title": "Cleanup - Page 1",
    "priority_level": 3,
    "due_at_utc": "2026-06-30T17:00:00Z",
    "compensation_amount": 50000.00,
    "page_region_ids": ["full-page-region-guid"]
  }
  ```
- `page_region_ids` is a JSON array, not a string
- Audit rows inserted through EF in same transaction and `SaveChangesAsync`
- `actor_role_name` set to null (consistent with previous EF audit patterns)
- No `audit.usp_AuditEvent_Append` stored procedure call

## Task title generation

Pattern: `{TaskTitlePrefix} - Page {PageNo}` (e.g., "Cleanup - Page 1")

## Manual SQL prerequisite

Manual SQL constraints for PageRegion must be applied to the target database before FULL_PAGE insert smoke testing. Code compiled successfully without them.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| **Application** | `DTOs/Manga/QuickSelectDtos.cs` | New: read/write DTOs (QuickSelectChapterDto, QuickSelectPageDto, QuickSelectAssistantDto, QuickSelectTaskAssignmentRequest, QuickSelectPageTaskRequest, QuickSelectTaskAssignmentResult, QuickSelectCreatedTaskDto) |
| **Application** | `DTOs/Manga/QuickSelectAssignmentPlan.cs` | New: internal plan DTO for Application-to-Infrastructure handoff |
| **Application** | `Interfaces/IQuickSelectService.cs` | New: service interface with 4 methods (3 reads + 1 write) |
| **Application** | `Interfaces/IQuickSelectRepository.cs` | New: repository interface with read + persist methods |
| **Application** | `Services/QuickSelectService.cs` | New: validation + Cloudinary coordination + plan building |
| **Application** | `DependencyInjection.cs` | Added `IQuickSelectService -> QuickSelectService` |
| **Infrastructure** | `Repositories/QuickSelectRepository.cs` | New: EF read queries + transaction/applock/batch insert |
| **Infrastructure** | `DependencyInjection.cs` | Added `IQuickSelectRepository -> QuickSelectRepository` |
| **API** | `Controllers/Mangaka/QuickSelectController.cs` | New: thin controller with 4 endpoints |
| **Docs** | `business-flows-use-cases.md` | Added BF-TASK-007 |
| **Docs** | `ui-spec.md` | Added section 6.z Quick Select backend |
| **Docs** | `revision/Mangaka/2026-06-25-quick-select-task-assignment-backend.md` | This revision note |

## Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded.
0 Errors
45 Warnings (all pre-existing baseline)
```

Zero new warnings from changed or newly added files.

## Runtime smoke

Not run. Requires:
1. Manual SQL PageRegion constraints applied to target database
2. Running API server
3. Valid Cloudinary credentials configured
4. Test data: series with Mangaka contributor, chapters with pages, Assistant contributors, page files in Cloudinary

## Known issues

- None from this build. No regressions to existing workflows (Approve, Return for Rework, Cancel, Reassign, Manage Series Contributors).
- `QuickSelectService` uses `_unitOfWork.SeriesContributors.GetAllAsync()` for contributor validation (loads all contributors into memory). Acceptable for MVP but should be optimized with direct DB queries for larger datasets.

## Next step

Phase 4: Implement Quick Select dialog UI on the Web layer:
- Quick Select dialog component
- Chapter/pages/assistants loading and selection
- Task type/priority/due date/compensation fields
- Per-page description override
- Confirm button calling POST endpoint
- Typed API client for Web-to-API calls
- Loading/error/empty/success states
