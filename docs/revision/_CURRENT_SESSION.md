# _CURRENT_SESSION — Advisory Scheduling Backend Update

**Started:** 2026-07-05
**Agent:** OpenCode
**Branch:** feature/Mangaka
**Goal:** Backend-only update: remove strict PublicationPeriod enforcement, make scheduling advisory, add release function, allow Mangaka+Editor reschedule, revise ON_HOLD behavior.
**Status:** IN_PROGRESS

---

## 0. Context loaded

- [x] `docs/agents/AGENTS.md`
- [x] `docs/agents/AI_AGENT_SKILLS_GUIDE.md`
- [x] `docs/agents/SESSION_RULE.md`
- [x] `docs/context.md`
- [x] `docs/business-rules.md`
- [x] `docs/business-flows-use-cases.md`
- [x] `docs/functional-requirements.md`
- [x] `docs/ui-spec.md`
- [x] `docs/user-stories.md`
- [x] `docs/revision/PublicationScheduling/2026-07-03-chapter-scheduling-and-hold.md`
- [x] `docs/revision/PublicationScheduling/2026-07-04-editor-review-scheduling-placeholder.md`
- [x] `docs/revision/PublicationScheduling/2026-07-04-shared-release-calendar-view.md`

Notes:
- Latest user instruction (advisory scheduling) supersedes older PublicationPeriod enforcement.
- Previous `_CURRENT_SESSION.md` was DONE from calendar redesign task.

---

## 1. Verified state at start

Current branch: `feature/Mangaka`
Clean working tree.

---

## 2. Task scope

### In scope
- Refactor ChapterSchedulingValidator to advisory-only
- Update response DTO with suggested date + warning fields
- Update Mangaka set-planned-date to allow SCHEDULED/ON_HOLD (reschedule)
- Update Editor set-planned-date to allow SCHEDULED/ON_HOLD (reschedule)
- Revise ON_HOLD (already preserves planned_release_date, minor audit tweak)
- Add ReleaseChapter command/handler/repository/endpoint
- Audit events: CHAPTER_RELEASED
- Register new DI services
- Build verification

### Out of scope
- Frontend/UI implementation
- Auto-hold
- Background jobs
- Stored procedures
- DB schema changes
- Public reader visibility

### Architecture flow
```
API Controller -> IMediator.Send(command) -> Application handler -> Infrastructure EF Core repo -> SQL Server
```

