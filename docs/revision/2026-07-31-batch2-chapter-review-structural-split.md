# Batch 2 Chapter Review Structural Read/Write Split

**Started:** 2026-07-31T09:06:05Z
**Completed:** 2026-07-31T09:28:34Z
**Agent:** Codex
**Branch:** `feature/Mangaka`
**Baseline HEAD:** `f6467b1 Working 31/7 SAVED`
**Status:** PASS

## Goal and scope

Implement Batch 2 only:

- split the combined Editor Chapter Review repository abstraction into semantic
  read, review-write, and scheduling ports;
- keep hold and release as separate cohesive lifecycle ports;
- move hold/release workflow result records out of Domain;
- replace the Chapter Review projection's Domain `Series` entity with only the
  primitive fields consumed by the Application handler;
- preserve all existing query, transaction, authorization, audit,
  notification, rollback, and persistence behavior.

Batch 0 and Batch 1 were preserved. No Batch 3 or later responsibility
migration was started.

## Context inspected

- Batch 2 user handoff and verification policy.
- Project agent/session/context/business documentation loaded for the prior
  batches and `docs/agents/AGENTS.md` reconfirmed in this session.
- Both original `IEditorChapterReviewRepository` partial interface files.
- All `EditorChapterReviewRepository` implementation partials.
- All direct query and command handler consumers.
- Hold/release interfaces, implementations, handlers, and result records.
- Scheduling implementation and handler.
- Infrastructure dependency injection.
- Relevant source/test references and current test coverage.
- Historical publication scheduling handoff for scheduling ownership context.

The unrelated `docs/revision/_CURRENT_SESSION.md` was left untouched.

## Initial Git state

The required five commands showed a clean committed Batch 0/1 baseline:

```text
## feature/Mangaka...origin/feature/Mangaka
No staged, unstaged, or untracked changes.
```

No reset, stash, checkout, rebase, merge, discard, commit, or push occurred.

## Method classification

| Method | Classification | Resulting port |
|---|---|---|
| `GetReviewQueueAsync` | READ | `IEditorChapterReviewReadRepository` |
| `GetReviewDetailForEditorAsync` | READ | `IEditorChapterReviewReadRepository` |
| `GetActionableChaptersAsync` | READ | `IEditorChapterReviewReadRepository` |
| `SubmitChapterEditorialReviewAsync` | WRITE - CHAPTER REVIEW | `IEditorChapterReviewWriteRepository` |
| `SubmitChapterEditorialReviewWithSchedulingAsync` | WRITE - CHAPTER REVIEW | `IEditorChapterReviewWriteRepository` |
| `SetPlannedReleaseDateAsync` | WRITE - SCHEDULING | `IEditorChapterSchedulingRepository` |
| `PutScheduledChapterOnHoldAsync` | WRITE - HOLD | `IChapterOnHoldRepository` |
| `ReleaseChapterAsync` | WRITE - RELEASE | `IChapterReleaseRepository` |

No method required an OTHER / HUMAN DECISION classification. The uncalled
legacy `SubmitChapterEditorialReviewAsync` implementation was retained on the
review-write port to avoid deleting behavior during this structural batch.

## Structural decisions

### Scheduling

`SubmitChapterEditorialReviewWithSchedulingAsync` remains a review-write
operation. Review persistence, optional markup, conditional scheduled status,
audit events, and notifications share one atomic transaction, so separating
that method would change implementation cohesion or behavior.

`SetPlannedReleaseDateAsync` moved to the separate
`IEditorChapterSchedulingRepository`. It has its own handler, transaction,
validation, audit, and notification flow and does not participate in review
submission.

### Hold and release

Hold and release remain separate ports. Each has a separate concrete
repository and independent transaction, checks, mutation, and audit flow.
Merging either into the review-write port would broaden that contract without
implementation or transactional cohesion.

### Projection

`EditorChapterReviewChapter.Series?` was replaced with exactly:

- `string SeriesTitle`
- `string? SeriesSlug`

`SeriesId` already existed. Title and slug were the only Domain entity values
read by the queue handler. The DTO values and workspace URL behavior are
unchanged.

## Files changed

### Application ports added

- `Features/Editor/ChapterReviews/Ports/IEditorChapterReviewReadRepository.cs`
- `Features/Editor/ChapterReviews/Ports/IEditorChapterReviewWriteRepository.cs`
- `Features/Editor/ChapterReviews/Ports/IEditorChapterSchedulingRepository.cs`
- `Features/Editor/ChapterReviews/Ports/IChapterOnHoldRepository.cs`
- `Features/Editor/ChapterReviews/Ports/IChapterReleaseRepository.cs`

### Application models added or changed

- `Features/Editor/ChapterReviews/Models/ChapterOnHoldResult.cs`
- `Features/Editor/ChapterReviews/Models/ChapterReleaseResult.cs`
- `Features/Editor/ChapterReviews/Models/EditorChapterReviewChapter.cs`

### Application consumers updated

- Three Chapter Review query handlers now consume the read port.
- Editorial review submission now consumes the review-write port.
- Editor planned-date mutation now consumes the scheduling port.
- Hold and release handlers now consume the moved Application lifecycle ports.

### Infrastructure updated

- `EditorChapterReviewRepository` implements the read, review-write, and
  scheduling ports without splitting the concrete class or changing method
  bodies.
- Hold/release repositories import the moved Application ports and models.
- DI registers one scoped `EditorChapterReviewRepository` concrete and maps all
  three interfaces to that same scoped instance.

### Removed

- Both old combined `IEditorChapterReviewRepository` partial files.
- Domain-owned `IChapterOnHoldRepository.cs`.
- Domain-owned `IChapterReleaseRepository.cs`.

### API, Web, tests, and database

- API endpoints changed: none.
- Web typed clients changed: none.
- Public contracts changed: none.
- Test source changed: none; existing tests provide compilation/regression
  coverage and no focused test was required for an unchanged query body.
- Database, schema, migration, SQL, or stored procedure changes: none.
- Database access: none.

## Architecture flow

```text
Application query/command handler
-> semantic Application feature port
-> existing Infrastructure EF repository
-> unchanged SQL Server persistence behavior
```

CQRS semantics remain unchanged. Only handler dependency abstractions changed.
No MediatR handler was manually registered.

## Dependency injection

```csharp
services.AddScoped<EditorChapterReviewRepository>();

services.AddScoped<IEditorChapterReviewReadRepository>(
    serviceProvider =>
        serviceProvider.GetRequiredService<EditorChapterReviewRepository>());

services.AddScoped<IEditorChapterReviewWriteRepository>(
    serviceProvider =>
        serviceProvider.GetRequiredService<EditorChapterReviewRepository>());

services.AddScoped<IEditorChapterSchedulingRepository>(
    serviceProvider =>
        serviceProvider.GetRequiredService<EditorChapterReviewRepository>());
```

Read, review-write, and scheduling therefore resolve to the same scoped
concrete repository instance.

## Verification

### Build

The restore-enabled Release build was attempted first. It could not access
`https://api.nuget.org/v3/index.json` inside the sandbox (`NU1301`), and the
request for network escalation was declined. This was an environment restore
failure.

The restored-assets fallback passed:

```text
dotnet build MangaManagementSystem.slnx --configuration Release --no-restore
Build succeeded.
65 Warning(s)
0 Error(s)
```

The warning count matches the Batch 1 baseline. No warning points to a changed
Batch 2 file, so the determinable warning delta is zero.

### Required test projects

```text
Project regression: 30 passed, 0 failed, 0 skipped.
Application tests: 21 passed, 0 failed, 0 skipped.
```

Both were run in Release with `--no-restore`.

### Full regression wrapper

The required wrapper was attempted and reached its restore stage, where the
same sandboxed NuGet access failed with `NU1301`.

The equivalent restore-free pipeline passed:

```text
Release solution build: PASS
All solution tests: 51 passed, 0 failed, 0 skipped
Coverage: 2 coverage.cobertura.xml files generated under the system temp path
```

### Static architecture and quality checks

- `git diff --check`: pass.
- Old combined repository source/test references: zero.
- Chapter Review Application references to Domain `Series`: zero.
- Domain -> Application references: zero.
- Application -> Infrastructure references: zero.
- Hold/release interfaces and results: one definition each, all in Application.
- Read handlers depend only on the read port.
- Review submission depends on the review-write port.
- Planned-date mutation depends on the scheduling port.
- Hold/release handlers retain their separate lifecycle ports.
- API/Web/test source diff: none.
- Batch 3 symbol diff: none.
- No formatter sweep or unrelated overwrite was found.

## Behavior statement

- Intended business behavior changed: no.
- Persistence/query behavior changed: no.
- Public API contract changed: no.
- Manual UI/API testing performed: no, prohibited by Batch 2 policy.
- Application launched: no.
- Database accessed: no.

## Known issues and deferred findings

- The restore-enabled build/wrapper remains environment-blocked by sandboxed
  NuGet access; restored-assets build and all offline-equivalent tests pass.
- The 65 existing solution warnings remain outside this batch.
- `PublicationScheduleController` still directly consumes
  `IPublicationScheduleRepository`; explicitly deferred outside Batch 2.
- The uncalled legacy `SubmitChapterEditorialReviewAsync` remains available on
  the write port; deleting or reconciling it is a later human decision.
- All Batch 3 and later responsibility migration remains deferred.

## Final Git/working-state expectation

- Staged changes: none.
- Unstaged tracked changes: Batch 2 source moves/updates plus this handoff.
- Untracked changes: new Batch 2 ports, models, and this handoff.
- Commit created: no.

## Result

Batch 2 is complete. No Batch 3 work was started.
