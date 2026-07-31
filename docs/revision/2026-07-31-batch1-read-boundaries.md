# Batch 1 Feature Read Boundaries

## Branch

`feature/Mangaka`

## Date

2026-07-31

## Task summary

Moved five low-risk, feature-specific read repository abstractions and their
projection models from Domain into feature-local Application `Ports` and
`Models`. Updated every real consumer, including handlers, Infrastructure
implementations and DI, Publication API/Web consumers, and Assistant regression
tests.

This was a structural move only. No intended query/business behavior, EF
expression, route, JSON field, stable DTO, or UI behavior changed. No
application was launched and no database was accessed.

## Architecture path

```text
API/Web
-> Application handler / feature contract
-> Application feature read port
-> Infrastructure repository
-> existing persistence query
```

Dependency direction remains:

```text
Domain <- Application <- Infrastructure
```

## Interfaces moved

| Interface | Old path | New path |
|---|---|---|
| `IEditorDashboardRepository` | `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IEditorDashboardRepository.cs` | `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Editor/Dashboard/Ports/IEditorDashboardRepository.cs` |
| `IEditorAnnotationRepository` | `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IEditorAnnotationRepository.cs` | `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Editor/Annotations/Ports/IEditorAnnotationRepository.cs` |
| `IEditorSeriesRepository` | `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IEditorSeriesRepository.cs` | `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Editor/Series/Ports/IEditorSeriesRepository.cs` |
| `IPublicationScheduleRepository` | `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IPublicationScheduleRepository.cs` | `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Publication/Schedule/Ports/IPublicationScheduleRepository.cs` |
| `IAssistantCompletedWorkRepository` | `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IAssistantCompletedWorkRepository.cs` | `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Assistant/CompletedWork/Ports/IAssistantCompletedWorkRepository.cs` |

## Models moved

### Editor Dashboard

- `EditorDashboardData` ->
  `Application/Features/Editor/Dashboard/Models/EditorDashboardData.cs`

It still contains the existing Domain `SeriesProposal` and `Series` entities.
An explicit `SeriesEntity` alias resolves the sibling `Editor.Series`
namespace without changing the type.

### Editor Annotations

- `EditorAnnotationData`
- `EditorAnnotationSeriesFilterItem`
- `EditorAnnotationSeriesGroup`
- `EditorAnnotationRow`
- `EditorAnnotationRegionItem`

Each now has a separate file under
`Application/Features/Editor/Annotations/Models`.

### Editor Series

No projection type existed. The moved port continues returning the Domain
`Series` entity; no duplicate Application model was introduced.

### Publication Schedule

- `PublicationScheduleChapter`
- `PublicationScheduleSeriesSuggestion`

Both now live under `Application/Features/Publication/Schedule/Models`.
`Application/DTOs/Publication/ChapterPlannedDateResult.cs` was not changed.

### Assistant CompletedWork

- `AssistantCompletedWorkReadModel`
- `AssistantCompletedTaskRow`

Both now live under `Application/Features/Assistant/CompletedWork/Models`.
`AssistantCompletedTaskRow` was a flat feature query projection despite its old
placement under `Domain/Entities`.

## Consumers updated

### Application

- `GetEditorDashboardQueryHandler`
- `GetEditorAnnotationsQueryHandler`
- `GetEditorSeriesQueryHandler`
- `GetPublicationScheduleCalendarQueryHandler`
- `GetAssistantCompletedWorkQueryHandler`

### Infrastructure

- `EditorDashboardRepository`
- `EditorAnnotationRepository`
- `EditorSeriesRepository`
- `PublicationScheduleRepository`
- `AssistantCompletedWorkRepository`
- `DependencyInjection`

### API/Web

- `PublicationScheduleController`
- `IPublicationScheduleApiClient`
- `PublicationScheduleApiClient`
- `ScheduleCalendar.razor`

Editor Dashboard, Editor Annotations, Editor Series, and Assistant CompletedWork
had no additional direct API/Web consumers of the moved internal projections.

### Tests

- `AssistantCompletedWorkHandlerTests`

No other automated test directly referenced the moved ports or models.

## DI changes

Only interface imports changed. These scoped registrations and concrete
implementations remain exactly the same:

```csharp
services.AddScoped<IEditorDashboardRepository, EditorDashboardRepository>();
services.AddScoped<IEditorAnnotationRepository, EditorAnnotationRepository>();
services.AddScoped<IEditorSeriesRepository, EditorSeriesRepository>();
services.AddScoped<IPublicationScheduleRepository, PublicationScheduleRepository>();
services.AddScoped<IAssistantCompletedWorkRepository, AssistantCompletedWorkRepository>();
```

No lifetime, factory, implementation sharing, or MediatR registration changed.

## Structural verification

- All five old Domain interface files are absent.
- The old Domain `AssistantCompletedTaskRow` file is absent.
- Every moved interface/model has exactly one declaration under Application.
- No stale fully qualified Domain ownership reference remains.
- Domain has no source/project dependency on Application.
- Application has no source/project dependency on Infrastructure.
- Infrastructure implements the new Application interfaces.
- Handlers and tests import the new feature namespaces.
- Domain `Series`/`SeriesProposal` entities remain Domain-owned.
- `ChapterPlannedDateResult` remains in its existing shared DTO location.
- Complete tracked diff and every untracked new source file were inspected.
- No formatter sweep, Batch 2 change, database artifact, or manual-test artifact
  was introduced.

## Behavior changed

None intended.

Repository query bodies, actor scoping, authorization, filters, sorting,
grouping, counts, compensation calculations, timestamps, null behavior,
response mapping, routes, and serialized fields are unchanged.

The CLR namespaces of the explicitly moved feature ports/projections changed as
required. HTTP/public DTO shapes did not change.

## Verification

### Build

The requested restore-enabled command was attempted:

```text
dotnet build MangaManagementSystem.slnx --configuration Release
```

Its NuGet restore could not access `https://api.nuget.org/v3/index.json` in the
sandbox. Permission to retry with network access was declined. This was an
environment restore failure, not a compiler failure.

Offline compilation using the existing restored assets:

```text
dotnet build MangaManagementSystem.slnx --configuration Release --no-restore
PASS — 0 errors, 65 existing warnings on the complete compile.
```

A confirming incremental offline build passed with 0 errors and emitted 0
warnings. The Batch 0 handoff supplied no numeric warning count, so a numeric
warning delta is not determinable. No warning points to a moved Application or
Infrastructure source file; warnings remain in existing nullable analysis,
generated Razor, and MudBlazor analyzer locations.

### Focused tests

```text
Project Regression: 30 total, 30 passed, 0 failed, 0 skipped.
Application Tests:  21 total, 21 passed, 0 failed, 0 skipped.
```

The restore-enabled Project Regression command was also attempted and hit the
same inaccessible NuGet feed. Both suites then ran successfully with
`--no-restore --no-build` against the verified Release build.

### Full regression

The exact script was attempted:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-project-regression.ps1
```

It stopped in its initial `dotnet restore` phase because NuGet network access
was unavailable; its build/test stages did not run.

The script's remaining pipeline was reproduced offline with the verified
Release build:

```text
dotnet test MangaManagementSystem.slnx --configuration Release
  --no-build --no-restore
  --collect:"XPlat Code Coverage"

PASS — 51 total, 51 passed, 0 failed, 0 skipped.
Coverage — two coverage.cobertura.xml files generated under the system temp directory.
```

### Static checks

```text
git diff --check
PASS — no whitespace errors; LF-to-CRLF conversion notices only.
```

Repository-wide declaration, stale-namespace, consumer, DI, and dependency
direction searches passed.

## Known issues

- `PublicationScheduleController` already consumes
  `IPublicationScheduleRepository` directly for suggestion endpoints rather
  than routing those reads through MediatR. This existing architecture concern
  was intentionally left unchanged because it is outside Batch 1.
- The exact restore-enabled regression wrapper remains environment-blocked
  until NuGet is reachable or packages can be restored through an approved
  source. Offline compilation and all available automated tests pass.

## Follow-ups

- None within Batch 1.
- Do not fold the deferred Publication controller architecture concern into
  this structural batch.

## Final status

**PASS — Batch 1 is complete.**

No Batch 2 work was started.
