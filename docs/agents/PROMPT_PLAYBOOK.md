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
* reintroducing mock data into production workflow pages,
* over-reading the entire documentation set when only a few sections are relevant.

---

## Focused context reading — default rule

**Do not tell the coding agent to read every rules/specification file from top to bottom by default.**

The agent should first understand the task, then search the documentation for the **specific rule IDs, feature names, routes, entities, statuses, actors, or keywords involved in that task**.

### Required lightweight startup context

For most implementation sessions, read only:

```text
docs/agents/AGENTS.md
docs/agents/RESUME_PACK.md
latest relevant file(s) under docs/revision/
```

Read `docs/agents/SESSION_RULE.md` or `docs/agents/AI_AGENT_SKILLS_GUIDE.md` only when the task needs their workflow/architecture guidance or when `AGENTS.md` points to a relevant section.

### Business/specification documents are search-first, section-only

These files are **reference sources**, not mandatory cover-to-cover reading:

```text
docs/context.md
docs/business-rules.md
docs/business-flows-use-cases.md
docs/functional-requirements.md
docs/ui-spec.md
docs/user-stories.md
```

For them:

1. Search first with `rg`, repository search, headings, rule IDs, route names, status names, actor names, or feature keywords.
2. Read only the matching section(s) and enough nearby context to avoid misunderstanding.
3. Expand to another document only when the current task needs cross-checking.
4. Do not repeatedly reread the same large files after the relevant rule has already been established.
5. Do not load unrelated actors, workflows, modules, or historical rules merely because they are in the same document.
6. Read a full business/specification document only when the task is genuinely broad enough that section-level reading is unsafe.

Examples:

```text
Chapter submission bug
-> search: CHAPTER_REVIEW, active page tasks, DRAFT, REVISION_REQUESTED, SubmitChapterForReview
-> read only matching chapter/task/submission sections.

Series lifecycle change
-> search: HIATUS, COMPLETED, lifecycle, contributor permission
-> read only matching lifecycle sections.

Small MudBlazor filter/navigation fix
-> inspect the Razor page, related typed API client/DTO, existing navigation helper/pattern,
   latest relevant handoff, and only the UI/business-rule sections that constrain that behavior.
```

### Canonical context files

The canonical documents remain the source of truth, but **being canonical does not mean every file must be read in full for every task**.

Use the smallest relevant context set that is sufficient to implement the task safely.

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
- Do not over-read documentation. Search first, then read only task-relevant sections and nearby context.
- Do not read every business/specification file end-to-end unless the task genuinely requires system-wide context.
- Keep changes focused on the requested task.
- For non-trivial implementation tasks, start in PLAN MODE and do not edit until the requested plan gate is satisfied.
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

[CONTEXT — FOCUSED READING]
Do not read the whole documentation set by default.

Start with:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. The newest relevant handoff under docs/revision/

Then inspect the target implementation and search the canonical business/specification files for only the rule IDs, feature names, routes, actors, statuses, entities, or keywords relevant to this task.

Read only the matching sections plus enough nearby context to understand them.

Read AI_AGENT_SKILLS_GUIDE.md, SESSION_RULE.md, context.md, business-rules.md, business-flows-use-cases.md, functional-requirements.md, ui-spec.md, or user-stories.md only as needed.

Do not read those large files cover-to-cover unless this task genuinely spans enough of the system that focused reading would be unsafe.

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

[PLAN MODE FIRST — REQUIRED]
Start in PLAN MODE.

Do not edit files yet.

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

Stop after the plan when the user asked for plan approval first.
Only begin implementation after the plan is approved.

If the user already explicitly authorized implementation without a separate approval gate, continue after the plan until build passes or a blocker requires user confirmation.

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

[CONTEXT — FOCUSED READING]
Start with:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. latest relevant docs/revision handoff
4. relevant files from the error stack or UI route

Search business/specification docs only for the rules directly related to the bug.
Read only those matching sections and nearby context.
Do not read the full rules/specification set unless the bug crosses multiple workflows and focused reading is insufficient.

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

[CONTEXT — FOCUSED READING]
Start with:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. latest relevant docs/revision handoff
4. existing SQL scripts/procedures related to this task

Then search business-rules.md, business-flows-use-cases.md, and functional-requirements.md for the exact procedure/workflow entities, statuses, permissions, audit events, and rule IDs involved.
Read only those matching sections plus nearby context.
Expand only when a dependency or conflict requires it.

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

[CONTEXT — FOCUSED READING]
Start with:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. latest relevant docs/revision handoff
4. existing Razor page/component and typed API client related to this task

Search ui-spec.md and the relevant business/specification docs for only the route, UI behavior, actor permission, status, or workflow involved.
Read only matching sections and nearby context.
Do not read the whole UI spec or all business-rule files for a small UI task.

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

[PLAN MODE FIRST — REQUIRED]
Do not edit yet. First inspect the existing implementation and return this plan:

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

[CONTEXT — FOCUSED READING]
Start with:
1. docs/agents/AGENTS.md
2. docs/agents/RESUME_PACK.md
3. latest relevant docs/revision handoff

Search context.md, business-rules.md, and functional-requirements.md specifically for Genre, Tag, SeriesGenre, SeriesTag, proposal snapshot, and cover/FileResource rules.
Read those matching sections only.

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
1. Start with docs/agents/AGENTS.md, docs/agents/RESUME_PACK.md, and the latest relevant docs/revision handoff.
2. Do NOT read every business/specification document end-to-end. Search them for task-specific rule IDs, routes, actors, statuses, entities, and keywords; read only matching sections and nearby context.
3. Inspect the existing implementation before editing.
4. Start in PLAN MODE and produce a short plan by Clean Architecture layer before changing code.
5. Stop after the plan when the user requested plan approval first.
6. Implement only the requested scope after approval.
7. Preserve typed API client flow.
8. Avoid direct Razor/Web -> Application/Infrastructure calls.
9. Update docs/revision/_CURRENT_SESSION.md during meaningful work.
10. Run build.
11. Create final docs/revision handoff.
12. Continue until build passes or until a blocker requires user confirmation once implementation is authorized.

[OUTPUT]
Return only the ready-to-paste prompt.
```

---

## Small prompt suffixes

Use these at the end of prompts when needed.

### Focused reading only

```text
Do not over-read the documentation. Search the canonical docs for the exact rule IDs, routes, actors, statuses, entities, and feature keywords involved in this task. Read only the matching sections plus enough nearby context to interpret them safely. Do not read every business/specification file end-to-end unless focused reading is genuinely insufficient.
```

### Plan mode first — wait for approval

```text
PLAN MODE FIRST. Inspect and reason about the task, but do not edit any files yet. Return the implementation plan, likely files, architecture impact, DB/API impact, risks, and verification steps. Stop after the plan and wait for my approval before making changes.
```

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
