# MangaFlow / MangaManagementSystem — Prompt Playbook

## Purpose

This file contains reusable prompts for OpenCode, Claude, ChatGPT, Antigravity, GitHub Copilot, or any other AI coding agent working on MangaFlow / MangaManagementSystem.

Use this file to start sessions faster, reduce repeated explanation, and prevent agents from:

* using old schema assumptions,
* bypassing Clean Architecture,
* adding direct Web-to-Application calls,
* skipping verification,
* forgetting to write `docs/revision` handoffs,
* making broad unrelated refactors,
* reintroducing mock data into production workflow pages.

---

## Canonical context files

For most implementation sessions, tell the agent to read these files first:

```text
docs/agents/AGENTS.md
docs/agents/AI_AGENT_SKILLS_GUIDE.md
docs/agents/SESSION_RULE.md
docs/agents/RESUME_PACK.md
docs/context.md
docs/business-rules.md
docs/business-flows-use-cases.md
docs/functional-requirements.md
docs/ui-spec.md
docs/user-stories.md
latest relevant file(s) under docs/revision/
```

If a task is very small, the agent may only need:

```text
docs/agents/AGENTS.md
docs/agents/RESUME_PACK.md
latest relevant docs/revision handoff
```

But for schema, stored procedure, workflow, role permission, or UI workflow changes, use the full context list.

---

## Global non-negotiables

Use these in almost every prompt.

```text
[NON-NEGOTIABLES]

- Preserve the Clean Architecture flow:
  Blazor Web -> typed API client -> API controller -> IMediator.Send -> Application handler -> Infrastructure repository/SP wrapper -> SQL Server.
- Do not add new direct Razor/Web -> Application/Infrastructure/EF/SP workflow calls.
- Do not put business workflow logic in Razor components or API controllers.
- Do not bypass stored procedures for important multi-table writes, workflow transitions, file linking, audit events, or concurrency-sensitive rules.
- Do not reintroduce old schema assumptions:
  - no Series.genre column
  - no SeriesProposal.genre_snapshot
  - no SeriesProposal.tag_snapshot
  - use Genre/SeriesGenre and Tag/SeriesTag
- SeriesProposal snapshots only proposal-specific content: proposal title, proposal synopsis, proposal file, version/status.
- Proposal review screens should display current locked Series metadata for cover, genres, and tags.
- Do not add payroll, e-commerce, public reader accounts, payment, salary, full drawing software, generic status-history tables, ChapterSubmission, SeriesBoardDecision, ChapterTranslation, PageRegionTranslation, or persistent AI job history unless explicitly requested.
- Do not print, commit, or copy secrets.
- Do not commit, push, reset, force-push, drop DB objects, run destructive migrations, or kill processes unless explicitly requested.
- Inspect existing code before creating new files or patterns.
- Keep changes focused on the requested task.
- Update or create docs/revision/_CURRENT_SESSION.md for meaningful tasks.
- Create a final docs/revision/yyyy-MM-dd-short-task-slug.md handoff when done or paused.
- Run build verification unless impossible; if not run, say clearly why.
```

---

## Global verification commands

Use the actual solution path in the repository.

```powershell
git status --short --branch --untracked-files=all
```

```powershell
dotnet build .\MangaManagementSystem.sln --no-incremental
```

If the solution is nested:

```powershell
dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

Known baseline:

```text
0 errors
60 pre-existing warnings
```

When checking whether a task introduced warnings, compare changed-file warnings. Do not treat all baseline warnings as newly introduced.

---

# Prompt 1 — Default build prompt

Use this for most implementation tasks.

```text
You are a senior .NET 8 Clean Architecture developer working on MangaFlow / MangaManagementSystem.

[CONTEXT]
Read these files first:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/SESSION_RULE.md
4. docs/agents/RESUME_PACK.md
5. docs/context.md
6. docs/business-rules.md
7. docs/business-flows-use-cases.md
8. docs/functional-requirements.md
9. docs/ui-spec.md
10. docs/user-stories.md
11. The newest relevant handoff under docs/revision/

[VERIFY GATE]
Before editing:
- Run git status --short --branch --untracked-files=all.
- Identify the current branch.
- Inspect the existing implementation paths related to the task.
- Check whether docs/revision/_CURRENT_SESSION.md exists.
- Create or update docs/revision/_CURRENT_SESSION.md for this task.

[NON-NEGOTIABLES]
- Preserve Web -> typed API client -> API controller -> IMediator.Send -> Application handler -> Infrastructure repository/SP wrapper -> SQL Server.
- Do not add direct Web/Razor -> Application/Infrastructure/EF/SP workflow calls.
- Keep API controllers thin.
- Keep business rules in Application/Domain/SQL procedures, not Razor pages.
- Use stored procedures or SP wrappers for important workflow writes, status transitions, FileResource linking, and audit behavior.
- Use EF AsNoTracking for read-only query projections where appropriate.
- Do not reintroduce Series.genre, SeriesProposal.genre_snapshot, or SeriesProposal.tag_snapshot.
- Genres/tags are normalized using Genre/SeriesGenre and Tag/SeriesTag.
- Do not broaden scope or refactor unrelated files.
- Do not commit or push unless I explicitly ask.

[TASK]
<describe the exact task here>

[REQUIRED PLAN BEFORE EDITING]
Give a compact plan first:

Clean Architecture placement:
- Web:
- API:
- Application:
- Infrastructure:
- Domain:
- Database/SP:

Flow:
Razor Page -> Typed API Client -> API Controller -> IMediator.Send -> Handler -> Repository/SP -> SQL Server

Files likely to change:
- ...

DB/SP impact:
- None / existing proc / new proc / changed proc / migration

Verification:
- Build command
- Manual smoke checklist

After the plan, implement the task. Continue until build passes or until a blocker requires user confirmation.

[FINAL RESPONSE REQUIRED]
Report:
- Summary
- Files changed
- Architecture flow
- DB/SP impact
- Build/test result
- Manual smoke result or not run
- Known issues
- docs/revision handoff path
```

---

# Prompt 2 — Review-only prompt

Use this when you want the agent to inspect code but not edit anything.

```text
You are reviewing MangaFlow / MangaManagementSystem as a senior .NET Clean Architecture reviewer.

[MODE]
REVIEW ONLY. Do not edit, create, delete, format, or run destructive commands.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/RESUME_PACK.md
4. latest relevant docs/revision handoff
5. relevant source files for the task

[TASK]
Review this area:
<describe area, files, bug, feature, or concern>

[CHECK FOR]
- Direct Web/Razor -> Application/Infrastructure/EF/SP calls
- Business logic inside Razor/API controllers
- Missing typed API client
- Missing MediatR command/query
- Incorrect Domain/Application/Infrastructure placement
- Stored procedure bypass for workflow writes
- Missing audit behavior
- Missing Cloudinary cleanup on SQL failure
- Old schema assumptions such as Series.genre or proposal genre/tag snapshots
- Mock data in production workflow screens
- Route/returnUrl safety issues
- Missing loading/error/empty states in UI
- Build warning risks in changed files

[OUTPUT]
Give:
1. Verdict: OK / Needs fixes / Risky
2. Findings table: severity, file, issue, recommended fix
3. Suggested implementation order
4. Whether the task needs DB/SP changes
5. Exact prompt I can give a coding agent to fix it
```

---

# Prompt 3 — Bugfix prompt

Use this when you have screenshots, errors, logs, or broken behavior.

```text
You are debugging MangaFlow / MangaManagementSystem.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/SESSION_RULE.md
4. docs/agents/RESUME_PACK.md
5. latest relevant docs/revision handoff
6. relevant files from the error stack or UI route

[VERIFY GATE]
- Run git status --short --branch --untracked-files=all.
- Inspect the exact file/route/service/procedure related to the bug.
- Do not guess from memory.
- Update docs/revision/_CURRENT_SESSION.md.

[BUG]
<describe the bug here>
<include screenshot/log/error text if available>

[REQUIREMENTS]
- Identify likely root cause.
- Inspect existing implementation before editing.
- Make the smallest safe fix.
- Preserve Clean Architecture boundaries.
- Do not add new architecture patterns.
- Do not hide errors with broad try/catch unless there is a clear user-safe error mapping.
- Do not suppress warnings without explaining the root cause.
- Do not commit or push.

[VERIFY]
Run:
- dotnet build using the actual solution path
- changed-file warning check if build warnings appear
- manual smoke checklist for the affected flow if possible

[FINAL RESPONSE]
Include:
- Root cause
- Fix summary
- Files changed
- Build/test result
- Manual smoke status
- Remaining risks
- docs/revision handoff path
```

---

# Prompt 4 — SQL stored procedure prompt

Use this when changing or creating SQL procedures.

```text
You are implementing SQL Server stored procedure changes for MangaFlow / MangaManagementSystem.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/SESSION_RULE.md
4. docs/agents/RESUME_PACK.md
5. docs/business-rules.md
6. docs/business-flows-use-cases.md
7. docs/functional-requirements.md
8. latest relevant docs/revision handoff
9. existing SQL scripts/procedures related to this task

[TASK]
<describe SQL/SP task here>

[NON-NEGOTIABLES]
- Follow existing SQL naming conventions: schemas such as auth, manga, audit; PascalCase tables; lower snake_case columns.
- Use UNIQUEIDENTIFIER for business/domain IDs unless existing table uses another type.
- Do not add generic status-history tables.
- Do not add ChapterSubmission or SeriesBoardDecision.
- Do not reintroduce Series.genre or proposal genre/tag snapshots.
- Important workflow writes must be transactional.
- Important status transitions must be guarded.
- Permission checks must be enforced in the stored procedure when relevant.
- Audit behavior must be included when the business event requires it.
- Do not duplicate simple CHECK/UNIQUE/FK/NOT NULL constraints unnecessarily.
- Throw safe custom SQL errors for business-rule failures.
- Do not store raw Cloudinary URLs in business tables when FileResource should be used.

[REQUIRED OUTPUT BEFORE EDITING]
Plan:
- Procedure(s) to add/change
- Tables touched
- Permission checks
- Status checks
- FileResource behavior
- Audit events
- Transaction boundaries
- Expected Application/Infrastructure wrapper impact
- Manual SQL test cases

[IMPLEMENTATION]
After plan approval or if already approved:
- Edit SQL scripts only in the intended scope.
- Update Infrastructure wrapper if needed.
- Update Application command/query if needed.
- Update API/Web only if contract changes.
- Run build.
- Provide manual SQL/API smoke checklist.

[FINAL RESPONSE]
Include:
- Procedure names changed
- Business rules enforced
- Transaction/audit behavior
- C# wrapper/API/UI impact
- Verification result
- Known risks
- docs/revision handoff path
```

---

# Prompt 5 — Blazor UI page prompt

Use this for MudBlazor pages, dialogs, layout, navigation, and workflow UI.

```text
You are implementing a Blazor Server / MudBlazor UI task for MangaFlow / MangaManagementSystem.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/SESSION_RULE.md
4. docs/agents/RESUME_PACK.md
5. docs/ui-spec.md
6. latest relevant docs/revision handoff
7. existing Razor page/component and typed API client related to this task

[TASK]
<describe UI task here>

[UI RULES]
- Do not use hardcoded mock data for production workflow screens.
- Use typed API clients.
- Do not inject Application services or repositories into Razor components for new/migrated workflows.
- Show loading state.
- Show error state with retry where appropriate.
- Show empty state.
- Preserve route safety and returnUrl behavior.
- Use /editor, not /editor/dashboard.
- For series/proposal screens, genres/tags come from normalized Series metadata, not proposal snapshots.
- Keep UI business actions permission-gated.
- Do not hide unavailable actions without explanation; use disabled buttons/tooltips or status messages where useful.
- Keep MudBlazor patterns consistent with existing pages.

[REQUIRED PLAN BEFORE EDITING]
Clean Architecture placement:
- Web Razor:
- Web typed API client:
- API:
- Application query/command:
- Infrastructure read/write:
- DB/SP:

UI states:
- Loading:
- Empty:
- Error:
- Success:
- Permission denied/locked:

Navigation:
- Route:
- returnUrl behavior:
- Back link behavior:

[VERIFY]
- Build solution.
- Check changed-file warnings.
- Manual smoke the page if app can run.

[FINAL RESPONSE]
Include:
- UI behavior changed
- API/data source used
- Files changed
- Build/manual smoke result
- Known issues
- docs/revision handoff path
```

---

# Prompt 6 — Genre/tag migration or repair prompt

Use this whenever old genre logic appears in code.

```text
You are repairing MangaFlow / MangaManagementSystem code to align with the latest normalized genre/tag design.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/AI_AGENT_SKILLS_GUIDE.md
3. docs/agents/RESUME_PACK.md
4. docs/context.md
5. docs/business-rules.md
6. docs/functional-requirements.md
7. latest relevant docs/revision handoff

[TASK]
Find and repair old genre/tag assumptions in:
<describe area or files>

[LATEST DESIGN]
- Use manga.Genre and manga.SeriesGenre for multiple broad genres.
- Use manga.Tag and manga.SeriesTag for multiple tags/tropes/themes.
- Do not use Series.genre.
- Do not use SeriesProposal.genre_snapshot.
- Do not use SeriesProposal.tag_snapshot.
- SeriesProposal snapshots only proposal title, synopsis, proposal file, version/status.
- Proposal review screens display current locked Series metadata for cover, genres, and tags.
- Series cover is FileResource with purpose SERIES_COVER.

[REQUIREMENTS]
- Inspect existing DB models, DTOs, repositories, queries, stored procedures, API contracts, and Razor components.
- Identify every old genre/snapshot assumption in the target scope.
- Propose a focused fix before editing.
- Update all layers consistently if needed.
- Do not create duplicate genre/tag DTO designs.
- Preserve existing Clean Architecture flow.
- Run build and report changed-file warnings.

[OUTPUT]
1. Old assumptions found
2. Fix plan by layer
3. Files changed
4. DB/SP impact
5. Build result
6. Manual smoke checklist
7. docs/revision handoff path
```

---

# Prompt 7 — Continue previous task prompt

Use this when an agent stopped mid-task or context was lost.

```text
Continue the previous MangaFlow / MangaManagementSystem task from the session notes.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/SESSION_RULE.md
3. docs/agents/RESUME_PACK.md
4. docs/revision/_CURRENT_SESSION.md if it exists
5. the latest relevant completed handoff under docs/revision/

[VERIFY GATE]
- Run git status --short --branch --untracked-files=all.
- Compare current files against the session note.
- Identify what is already done, what is unverified, and what remains.
- Do not redo completed verified work unless there is evidence it is broken.
- Do not overwrite unrelated dirty files.

[TASK]
Resume from the next logical step.

[OUTPUT BEFORE EDITING]
Give:
- Current branch
- Session note found or not found
- Verified completed work
- Unverified work
- Remaining plan
- Risky operations requiring confirmation, if any

Then continue until build passes or a blocker requires user confirmation.
```

---

# Prompt 8 — Final handoff prompt

Use this at the end of a feature session.

```text
Create a final MangaFlow / MangaManagementSystem handoff for the current task.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/SESSION_RULE.md
3. docs/revision/_CURRENT_SESSION.md
4. git diff / relevant changed files

[TASK]
Create a final handoff file under:

docs/revision/yyyy-MM-dd-short-task-slug.md

[HANDOFF MUST INCLUDE]
- Branch
- Date
- Task summary
- Architecture path
- Files changed by layer
- DB/SP impact
- API endpoints added/changed
- Web typed clients added/changed
- UI behavior changed
- Business rules enforced
- Build result
- Test result
- Manual smoke checklist
- Known issues
- Follow-ups
- Exact next step if another agent continues

[REQUIREMENTS]
- Do not claim manual smoke passed unless it was actually run.
- If build/test was not run, say so clearly.
- Include warning count and whether warnings are pre-existing.
- After final handoff is created, mark _CURRENT_SESSION.md as CLOSED or delete it only if the handoff fully replaces it.
```

---

# Prompt 9 — Build warning audit prompt

Use this when warning counts change.

```text
Audit MangaFlow / MangaManagementSystem build warnings.

[CONTEXT]
Read:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. latest build-warning handoff under docs/revision/ if present

[TASK]
Determine whether current changes introduced new warnings.

[COMMANDS]
Run a full non-incremental build:

dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental

Then check warnings from changed files only.

[KNOWN BASELINE]
- 0 errors
- 60 pre-existing code warnings
- Warning totals may vary due to incremental build caching or Visual Studio file locks.

[REQUIREMENTS]
- Separate code warnings from file-lock/MSBuild copy warnings.
- Identify warnings from changed files.
- Do not suppress warnings unless root cause is understood.
- Do not claim recent work introduced warnings unless evidence points to changed/new files.

[OUTPUT]
- Build command
- Total errors/warnings
- Unique code warnings
- Changed-file warning list
- Verdict: no new warnings / new warnings found
- Recommended fixes if needed
```

---

# Prompt 10 — Ask another AI agent to implement a feature

Use this when you want ChatGPT to generate a prompt for OpenCode/Claude/Antigravity.

```text
Write an implementation prompt for another AI coding agent.

[PROJECT]
MangaFlow / MangaManagementSystem.

[CONTEXT TO INCLUDE]
- Clean Architecture flow:
  Web -> typed API client -> API -> MediatR -> Application -> Infrastructure -> SQL/SP
- Latest genre/tag design:
  Genre/SeriesGenre and Tag/SeriesTag, no Series.genre, no proposal genre/tag snapshots.
- FileResource/Cloudinary rules.
- Stored procedure rules for important workflow writes.
- Build baseline: 0 errors, 60 pre-existing warnings.
- docs/revision handoff requirement.

[TASK]
<describe task>

[PROMPT REQUIREMENTS]
The prompt must tell the agent to:
1. Read docs/agents/AGENTS.md, AI_AGENT_SKILLS_GUIDE.md, SESSION_RULE.md, RESUME_PACK.md, business docs, and latest relevant docs/revision handoff.
2. Inspect existing implementation before editing.
3. Produce a short plan by Clean Architecture layer.
4. Implement only the requested scope.
5. Preserve typed API client flow.
6. Avoid direct Razor/Web -> Application/Infrastructure calls.
7. Update docs/revision/_CURRENT_SESSION.md during work.
8. Run build.
9. Create final docs/revision handoff.
10. Continue until build passes or until a blocker requires user confirmation.

[OUTPUT]
Return only the ready-to-paste prompt.
```

---

## Small prompt suffixes

Use these at the end of prompts when needed.

### Continue until done

```text
Continue until the task is complete and the solution builds, unless a risky operation, destructive change, schema uncertainty, or user decision is required.
```

### No broad refactor

```text
Do not perform broad refactors, formatting-only changes, package upgrades, or unrelated cleanup.
```

### Build-only verification is acceptable

```text
If the app cannot be run locally, perform build-only verification and provide a manual smoke checklist instead of claiming smoke passed.
```

### Stop on schema conflict

```text
If existing code or SQL conflicts with the latest docs about Genre/Tag, SeriesProposal snapshots, FileResource, or workflow statuses, stop and report the conflict before editing.
```

### Protect dirty files

```text
If git status shows dirty files unrelated to this task, list them and avoid editing them unless required. Ask before overwriting or reverting them.
```
