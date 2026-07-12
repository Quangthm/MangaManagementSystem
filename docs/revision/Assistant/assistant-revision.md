# Assistant Feature — Clean Architecture Revision

**Author:** Nhutuan593
**Date:** 2026-07-11
**Commit:** 2c37246 (not committed yet — working tree only)
**Type:** Architecture refactor + Unit tests

---

## 1. Why

The Assistant feature had several architectural smells and hidden bug risks:

| # | Issue | Severity |
|---|-------|----------|
| 1 | Read model (`AssistantCompletedTaskRow`) in `Domain/Entities` violated Clean Architecture — it is a passive DTO, not a domain entity | High |
| 2 | `ChapterPageTaskService` had 3 duplicate mapping methods (50+ lines each, 90% overlap) — maintenance nightmare | High |
| 3 | SQL Server error codes hardcoded as magic numbers (58101–58508) in two controllers — if stored procedures change error numbers, the mapping breaks silently | Medium |
| 4 | Duplicate using directive in `DependencyInjection.cs` (CS0105) | Low |
| 5 | No unit tests existed for the Assistant feature — zero regression safety | High |

---

## 2. Changes

### 2.1. Move `AssistantCompletedTaskRow` → Application Layer

- `AssistantCompletedTaskRow` is a flat read-only projection with no behavior, identity, or business logic — it belongs in `Application/DTOs`, not `Domain/Entities`.
- `IAssistantCompletedWorkRepository` is a query-only read-model interface — moved to `Application/Interfaces`.

**Files affected:**

| File | Action |
|------|--------|
| `src/.../Application/DTOs/Manga/AssistantCompletedWorkDtos.cs` | Added `AssistantCompletedTaskRow` class |
| `src/.../Application/Interfaces/IAssistantCompletedWorkRepository.cs` | **Created** (interface + `AssistantCompletedWorkReadModel` record) |
| `src/.../Domain/Entities/AssistantCompletedTaskRow.cs` | **Deleted** |
| `src/.../Domain/Interfaces/IAssistantCompletedWorkRepository.cs` | **Deleted** |
| `src/.../Infrastructure/Repositories/AssistantCompletedWorkRepository.cs` | Updated namespace imports |
| `src/.../Application/Features/.../GetAssistantCompletedWorkQueryHandler.cs` | Updated import, removed `Domain.Entities` reference |

### 2.2. Extract Mapping Logic in `ChapterPageTaskService`

The three mapping methods (`MapToDto`, `MapToDtoWithFullContext`, `MapToDtoWithAssistantContext`) shared:
- Identical `PageRegion → PageRegionDto` mapping (13 lines × 3 copies)
- Nearly identical page-context extraction logic (7 lines × 2 copies)

**Fix:**
- `MapRegions(IEnumerable<PageRegion>)` — shared region mapping (13 lines, called by all 3)
- `PageContext` sealed record — shared context object (SeriesId, SeriesTitle, ChapterTitle, etc.)
- `ExtractPageContext(ChapterPageTask)` — shared extraction logic

**Files affected:**

| File | Action |
|------|--------|
| `src/.../Application/Services/ChapterPageTaskService.cs` | Added 3 helpers; refactored 3 mapping methods (~40 lines of duplication removed) |

### 2.3. Extract SQL Error Codes → Constants

Controllers caught `SqlException` and switched on hardcoded integers.

**Fix:** Created `Application/Common/Constants/ChapterPageTaskErrorCodes.cs` with 3 static classes:

| Class | Prefix | Source | Code Range |
|-------|--------|--------|------------|
| `SubmitForReviewErrors` | 581 | `usp_ChapterPageTask_SubmitForReview` | 58101–58106 |
| `MangakaTaskErrors` | 582–585 | cancel/approve/return/reassign SPs | 58201–58508 |
| `SqlErrors` | — | generic SQL | 2627, 2601 |

**Files affected:**

| File | Action |
|------|--------|
| `src/.../Application/Common/Constants/ChapterPageTaskErrorCodes.cs` | **Created** |
| `src/.../API/Controllers/Assistant/AssistantTaskController.cs` | `58101` → `SubmitForReviewErrors.TaskLocked`, etc. |
| `src/.../API/Controllers/Mangaka/MangakaTaskController.cs` | `58201 or 58301 or ...` → `MangakaTaskErrors.LockAcquisitionFailed or ...` |

### 2.4. Remove Duplicate Using Directive

- `DependencyInjection.cs` had `using MangaManagementSystem.Domain.Interfaces;` declared twice (line 2 and line 13), causing CS0105.

**Files affected:**

| File | Action |
|------|--------|
| `src/.../Infrastructure/DependencyInjection.cs` | Removed duplicate using (line 13) |

### 2.5. Unit Tests (xUnit + Moq, 37 tests)

Test project created at `tests/MangaManagementSystem.Application.UnitTests/`.

```
tests/MangaManagementSystem.Application.UnitTests/
├── Mappers/
│   └── AssistantTaskRateMapperTests.cs           13 tests
├── Services/
│   ├── ChapterPageTaskServiceTests.cs            12 tests
│   └── ChapterPageVersionServiceTests.cs          7 tests
└── Features/
    └── GetAssistantCompletedWorkQueryHandlerTests.cs  5 tests
```

**Key coverage:**
- Mapper: known rates, default fallback, case-insensitivity, compensation override
- Task Service: CRUD, authorization guard (ownership check, role check), status validation (only `UNDER_REVIEW` can be returned), reassign input validation (6 validation rules)
- Version Service: create, delete guard (must throw), `DeleteVersionImageAsync` guard (empty actor, not found, unresolved annotations block), soft-delete success
- Query Handler: summary counts, breakdown grouping, recent-items limit (10), empty-handling

**Test project dependencies added to `.csproj`:**
- `MediatR` (12.4.1)
- `Moq` (4.20.72)
- Project reference to `MangaManagementSystem.Application`
- Project reference to `MangaManagementSystem.Domain`
- `InternalsVisibleTo` added to `MangaManagementSystem.Application.csproj` để test project truy cập internal types

---

## 3. Remaining Work

| Item | Type | Effort |
|------|------|--------|
| Integration tests for `AssistantTaskSubmissionService` (DB-dependent) | Tests | 1 session |
| Integration tests for stored-procedure error-code mapping (verify C# constants match SP RAISERROR numbers) | Tests | 0.5 session |
| Controller unit tests (`AssistantTaskController`, `MangakaTaskController`) | Tests | 1 session |
| API integration tests (full HTTP request/response) | Tests | 1 session |
| `CancellationToken` propagation in remaining service methods | Refactor | 0.5 session |

---

## 4. Build Status

```
Build succeeded.
    0 Error(s)
    0 Warning(s)   (Web project — incremental build)
   25 Warning(s)   (Infrastructure project — all pre-existing,
                    not introduced by this change)

Tests: 37 passed, 0 failed, 0 skipped.
```
