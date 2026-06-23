# _CURRENT_SESSION — Manage Series Contributors Page

**Started:** 2026-06-22T14:00:00Z
**Agent:** OpenCode
**Branch:** `feature/Mangaka`
**Goal:** Add a Mangaka-facing Manage Series Contributors page at `/mangaka/contributors`
**Status:** IN_PROGRESS

---

## 0. Context loaded

- [x] `docs/agents/AGENTS.md`
- [x] `docs/agents/AI_AGENT_SKILLS_GUIDE.md`
- [x] `docs/agents/SESSION_RULE.md`
- [x] `docs/agents/RESUME_PACK.md`
- [x] `docs/context.md`
- [x] `docs/business-rules.md` (via explorer agent)
- [x] `docs/business-flows-use-cases.md` (via explorer agent)
- [x] `docs/functional-requirements.md` (via explorer agent)
- [x] `docs/ui-spec.md` (via explorer agent)
- [x] `docs/user-stories.md` (via explorer agent)
- [x] `docs/revision/Mangaka/2026-06-22-task-management-phase1-read-list-ui.md`
- [x] `docs/revision/Mangaka/2026-06-22-link-review-submissions-navigation.md`
- [x] `docs/revision/Mangaka/2026-06-22-review-submissions-return-and-workspace-links.md`
- [x] `docs/revision/Mangaka/2026-06-22-task-refresh-and-reassign-phase2.md`
- [x] `docs/revision/Architecture/2026-06-22-cross-role-safe-return-url-navigation.md`

---

## 1. Verified state at start

Command: `git status --short --branch --untracked-files=all`
Result: `## feature/Mangaka...origin/feature/Mangaka` — clean working tree, no dirty files.

Build baseline: `0 errors, 57 warnings (all pre-existing)`

CS8602 warning check: The 3 CS8602 warnings mentioned in the previous task handoff were already fixed (replaced chained navigation with explicit joins in `GetEligibleAssistantsForTaskAsync`). Current CS8602 warnings in `ChapterPageTaskRepository.cs` are pre-existing from other methods (lines 84, 85, 100, 101, 102, 105, 120, 121, 122, 125, 194, 195, 196, 199) — not from the previous task.

---

## 2. Task scope

### In scope
- View contributors for Mangaka's own series
- Series selector/filter
- Search contributors by display name, username, email, role
- Role filter and status filter (Active/Former/All)
- Add active Assistant users as contributors
- Remove active Assistant contributors by setting end_date
- Block removal if assistant has ASSIGNED or UNDER_REVIEW tasks
- Navigation from MangakaDashboard sidebar
- Dedicated page at `/mangaka/contributors`

### Out of scope
- Tantou Editor contributor management
- Mangaka contributor management
- Role editing, specialization
- Batch operations, Quick Select
- Route migrations for /mangaka/series or /mangaka/proposals

### Architecture flow
```
Blazor Web
-> IMangakaSeriesContributorApiClient
-> MangakaSeriesContributorController
-> IMediator.Send(query/command)
-> Application handlers
-> Infrastructure repository (EF read + SP wrapper for writes)
-> SQL Server (vw_ActiveSeriesContributor view + new SP for end)
```

### DB/SP impact
- Existing SP: `manga.usp_SeriesContributor_Add` (reusable for adding assistants)
- New SP needed: `manga.usp_SeriesContributor_EndAssistant`
- Existing view: `manga.vw_ActiveSeriesContributor`

---

## 3. Plan

See Plan Mode output below.

---

## 4. Progress log

### 2026-06-22T14:00:00Z — Session started
- Loaded all required context documents
- Verified branch: `feature/Mangaka`, clean working tree
- Build baseline: 0 errors, 57 warnings
- CS8602 warnings: pre-existing, not from previous task

---

## 5. Files changed this session

| Path | Change | Verified |
|------|--------|----------|
| (none yet) | | |

---

## 6. Commands run

| Command | Result | Notes |
|---------|--------|-------|
| `git status` | Clean | `feature/Mangaka` |
| `git log --oneline -10` | OK | HEAD at 4757fc9 |
| `dotnet build --no-incremental` | Pass | 0 errors, 57 warnings |

---

## 7. Build/test/manual verification

### Build
Not run yet for new changes.

### Manual smoke
Not run yet.

---

## 8. Known issues / risks

| Issue | Impact | Next action |
|-------|--------|-------------|
| No `usp_SeriesContributor_End*` SP exists | Need new SP for end-dating contributors | Create SQL script, document as not-yet-run |
| Pre-existing CS8602 warnings in ChapterPageTaskRepository | Unrelated | Do not mix cleanup |

---

## 9. Final status

**Status:** IN_PROGRESS
