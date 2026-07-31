# Batch 3 — Mangaka Chapter Repository Port Move

**Date:** 2026-07-31
**Branch:** feature/Mangaka
**Status:** CODE COMPLETE — ready for user build/functional verification

## Goal and scope

Batch 3 performed only the ownership and namespace move of the two
`IMangakaChapterRepository` partial files from the general Application interfaces
area into the Mangaka Chapters feature.

Batch 3 did **not**:

- split the repository;
- move business logic;
- alter repository method bodies;
- change API/Web contracts;
- add Batch 3.5 tests;
- begin Batch 4;
- build or run tests;
- access the database.

## Repository moved

Old:

- `Application/Interfaces/IMangakaChapterRepository.cs`
- `Application/Interfaces/IMangakaChapterRepository.Scheduling.cs`

New:

- `Application/Features/Mangaka/Chapters/Ports/IMangakaChapterRepository.cs`
- `Application/Features/Mangaka/Chapters/Ports/IMangakaChapterRepository.Scheduling.cs`

Both partial declarations use:

`MangaManagementSystem.Application.Features.Mangaka.Chapters.Ports`

Final interface shape:

- 2 partial declarations;
- 1 logical interface;
- 1 namespace;
- 8 unchanged method signatures;
- 0 remaining declarations under `Application/Interfaces`.

## Model decisions

- No models were moved.
- No `Models` folder was created.
- Shared DTOs stayed in their existing Application DTO namespaces.
- Domain entities and policies remained unchanged.

The retained shared types include:

- `MangakaChapterListItemDto`;
- `ChapterEditorialReviewSummaryDto`;
- `EditorChapterReviewHistoryDto`;
- request DTOs, including `CreateChapterDraftRequest`,
  `UpdateChapterDraftRequest`, and `SetPlannedReleaseDateRequest`;
- `SetChapterPlannedReleaseDateResponse`.

## Consumers updated

Application handlers updated to import the feature-owned port:

- `GetMyMangakaChapters`;
- `GetMangakaSeriesChapters`;
- `CreateChapterDraft`;
- `UpdateChapterDraft`;
- `SubmitChapterForReview`;
- `CancelChapterSubmission`;
- `CancelChapter`;
- `SetChapterPlannedReleaseDate`.

Infrastructure updates:

- `MangakaChapterRepository.cs` imports and implements the moved port;
- Infrastructure DI imports the new port namespace.

API, Web, and tests required no changes because they do not directly consume
`IMangakaChapterRepository`.

## DI

The existing registration was preserved:

`AddScoped<IMangakaChapterRepository, MangakaChapterRepository>()`

- The scoped lifetime is unchanged.
- The concrete implementation is unchanged.
- Only the interface namespace ownership changed.

## Behavior statement

- Intended business behavior changed: **NO**
- Persistence behavior changed: **NO**
- Public API/Web contract changed: **NO**
- Repository method bodies changed: **NO**
- Scheduling implementation changed: **NO**
- Business logic moved: **NO**

## Static verification

- No stale old interface declarations remain.
- No duplicate declarations exist beyond the intended two partials.
- Domain does not depend on Application.
- Application does not depend on Infrastructure.
- No read/write/scheduling repository split was introduced.
- No Batch 3.5 or Batch 4 code was introduced.
- `git diff --check` passed.
- API, Web, DTO, test, project, and solution files remained unchanged.

## Deferred responsibilities

Infrastructure intentionally continues to own the following responsibilities for
later Batches 4–8:

- authorization;
- contributor eligibility;
- series and chapter state checks;
- normalization;
- task and annotation blocking;
- state transitions;
- audits;
- notifications;
- transactions;
- locking;
- `sp_getapplock`;
- duplicate handling;
- final reloads and rechecks;
- scheduling decisions.

These responsibilities are deferred architecture work, not Batch 3 defects.

## User functional regression plan

| Priority | Role | Action | Expected result | Wiring protected |
|---|---|---|---|---|
| P0 | Mangaka | Open My Chapters and a known series chapter workspace. | Existing chapter data loads with the same values and statuses. | Query handlers → moved port → main repository partial → DI |
| P0 | Active Mangaka contributor | Create one valid chapter draft. | Existing creation and default behavior remain unchanged. | Create command handler → moved port → main repository partial → DI |
| P0 | Active Mangaka contributor | Use a chapter satisfying the existing schedulable conditions and set a planned release date. | Existing planned-date and status behavior remain unchanged. | Scheduling handler → moved port → scheduling partial → DI |
| P1 optional | Active Mangaka contributor | Update a chapter in an existing editable state. | Existing update behavior remains unchanged. | Update command wiring |
| P1 optional | Active Mangaka contributor | Submit a chapter satisfying the existing review-submission requirements. | Existing submission and state-transition behavior remain unchanged. | Submit-for-review command wiring |
| P1 optional | Active Mangaka contributor | Cancel an eligible `UNDER_REVIEW` submission. | Existing cancellation behavior remains unchanged. | Cancel-submission command wiring |
| P1 optional | Active Mangaka contributor | Cancel a chapter in an existing allowed cancellable state. | Existing cancellation behavior remains unchanged. | Cancel-chapter command wiring |

## Git state

- Branch: `feature/Mangaka`.
- The current worktree remained authoritative.
- No staged changes were created.
- No commit was created.
- The move currently appears as deleted old files plus untracked new files,
  together with the intended namespace import updates.
- Batch 0–2 work was preserved.

## Result

Batch 3 code changes are complete and ready for user build/functional
verification.

No Batch 3.5 or Batch 4 work was started.
