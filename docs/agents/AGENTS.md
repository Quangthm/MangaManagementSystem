# MangaFlow / MangaManagementSystem — OpenCode Agent Instructions

## Project snapshot

* **Project:** MangaFlow / MangaManagementSystem
* **Product:** Manga Creation Workflow and Publishing Management System
* **Current main working branch:** `feature/Mangaka`
* **Stack:** .NET 8, Blazor Server Web, ASP.NET Core Web API, Clean Architecture, MediatR/CQRS, SQL Server, Cloudinary, MudBlazor
* **Architecture style:** Distributed Monolith
* **Primary goal:** Manage manga production workflow from account approval, series proposal, chapter/page production, assistant task assignment, editorial review, board polling, publication planning, ranking simulation, notifications, and auditability.
* **Not the goal:** Do not add payroll, e-commerce, public reader accounts, payment, salary, full drawing software, or unrelated platform modules.

---

## Required context workflow

At the start of every OpenCode session, read these files first:

1. `docs/agents/AGENTS.md`
2. `docs/agents/AI_AGENT_SKILLS_GUIDE.md`
3. `docs/agents/SESSION_RULE.md`
4. `docs/agents/RESUME_PACK.md`
5. `docs/context.md`
6. `docs/business-rules.md`
7. `docs/business-flows-use-cases.md`
8. `docs/functional-requirements.md`
9. `docs/ui-spec.md`
10. `docs/user-stories.md`
11. The newest relevant handoff under `docs/revision/`

If file names differ by casing or version, use the current file names in `docs/`, `docs/agents/`, and `docs/revision/`.

`docs/agents/PROMPT_PLAYBOOK.md` is a reusable prompt-template file for the user. Agents do not need to read it at the start of every session unless the task is specifically about writing prompts for another AI agent.

Before editing code:

1. Run `git status --short --branch --untracked-files=all`.
2. Identify the current branch.
3. Inspect existing implementation before creating new files.
4. Check the latest relevant `docs/revision/*.md` handoff.
5. Create or update a live session note when the task is meaningful.

---

## Source-of-truth priority

When documents conflict, follow this order:

1. Latest explicit user instruction in the current session.
2. Latest uploaded/updated business docs in `docs/`.
3. Latest relevant handoff in `docs/revision/`.
4. `docs/agents/AI_AGENT_SKILLS_GUIDE.md`.
5. Existing code behavior.
6. Older handoffs or older generated plans.

Do not resurrect old schema or workflow assumptions if the latest docs changed them.

---

## Core architecture rule

All new or migrated business workflows must follow this flow:

```text
Blazor Web
-> typed Web API client
-> ASP.NET Core API controller
-> IMediator.Send(command/query)
-> Application command/query handler
-> Infrastructure EF Core repository / Unit of Work
-> SQL Server
```

Legacy stored-procedure-backed workflows may continue to use this transitional flow until they are deliberately refactored:

```text
Blazor Web
-> typed Web API client
-> ASP.NET Core API controller
-> IMediator.Send(command/query)
-> Application command/query handler
-> Infrastructure stored-procedure wrapper
-> SQL Server stored procedure
```

Default direction going forward:

```text
New or actively changed workflow = prefer EF Core + Unit of Work.
Existing stable stored-procedure workflow = keep for now unless the task explicitly refactors it.
Stored-procedure refactoring = defer to a later dedicated cleanup unless the current task requires it.
```

Do not implement new business workflows using this anti-pattern:

```text
Blazor Razor component
-> Application service directly
-> Infrastructure / DbContext / stored procedure
```

Legacy direct Web-to-Application calls may exist. Do not add more. When modifying a page that belongs to a migrated workflow, route it through a typed API client.

---

## Clean Architecture placement rules

### Domain

Allowed:

* Entities
* Domain concepts
* Enums/constants/value objects when appropriate
* Application-facing repository abstractions only if already established by project convention

Not allowed:

* EF Core `DbContext`
* SQL client code
* Cloudinary SDK
* SMTP SDK
* API/Web references
* Razor/UI logic

### Application

Allowed:

* CQRS commands and queries
* MediatR handlers
* DTOs/results
* Validators
* Use-case orchestration
* Application-facing abstractions for repositories, storage, email, AI Hub, etc.

Not allowed:

* Direct dependency on Infrastructure concrete classes
* EF Core concrete `DbContext`
* SQL client implementation details
* Cloudinary SDK usage
* Razor/UI code
* HTTP controller concerns

### Infrastructure

Allowed:

* EF Core mappings and repositories
* SQL Server stored-procedure wrappers
* ADO.NET / `Microsoft.Data.SqlClient`
* Cloudinary implementation
* SMTP/email implementation
* AI Hub adapter implementation
* External service implementations hidden behind Application-facing abstractions

Not allowed:

* Web/Razor dependencies
* API controller logic
* Business rules leaking upward as raw SQL errors

### API

Allowed:

* Thin controllers
* HTTP route/query/body/form binding
* Authentication/authorization boundary
* Actor ID extraction from claims or current transitional header pattern
* `IMediator.Send(...)`
* Safe HTTP response mapping

Not allowed:

* Direct EF Core access
* Direct stored-procedure calls
* Cloudinary upload logic
* Audit writes
* Workflow state transition logic
* Raw stack traces, SQL errors, secrets, OTPs, passwords, or tokens in responses

### Web

Allowed:

* Razor pages/components
* MudBlazor UI
* Typed API clients
* Local UI state
* Dialogs/snackbars/loading/error/empty states

Not allowed for new or migrated workflows:

* Direct Application service injection
* Direct MediatR usage
* Direct repository/DbContext/stored-procedure calls
* Direct Cloudinary/SMTP/AI Hub usage

---

## CQRS / MediatR rules

Use a **Command** for operations that change state, including:

* Create series draft
* Update series draft
* Submit proposal
* Cancel proposal/draft
* Upload or replace files
* Assign assistant task
* Submit assistant work
* Review task work
* Create or resolve annotations
* Open/close/cancel board poll
* Vote
* Apply board result
* Change publication frequency
* Approve/reject/disable account
* Any operation that writes audit events

Use a **Query** for read-only screens, including:

* Dashboards
* Review queues
* Series lists
* Proposal details
* Chapter review lists
* Annotation lists
* Ranking display
* Audit feed
* Notification lists

Rules:

* Do not write database changes from query handlers.
* Do not mix workflow writes into read models.
* Use `CancellationToken` on async handlers.
* Request/response contracts that are HTTP-specific should stay near API/Web boundary and map into Application commands/queries.
* If a handler temporarily delegates to an existing Application service, document it as transitional technical debt in the session note.

---

## Database, EF Core, and stored procedure rules

SQL Server is the system of record for workflow state.

### Default database interaction direction

Use EF Core as the **default** database interaction approach for new and actively changed workflows.

Prefer EF Core + Unit of Work for:

* Multi-table writes
* Status transitions
* FileResource creation and business-record linking
* Approval/rejection/cancellation
* Audit event creation
* UI-driven workflows that create or update object graphs
* Batch/multi-row workflows such as Quick Select
* Workflows with business rules that are still changing
* Workflows where maintainability and C# debugging are more important than SQL-side procedural logic

Business rules should primarily live in C# Application command/query handlers and validators. SQL constraints remain the final data-integrity guard.

### Unit of Work rule

Do not implement important multi-table workflows as separate uncoordinated `SaveChangesAsync` calls.

For important writes:

```text
Application handler
-> load required entities
-> validate business rules
-> apply state changes
-> create audit event when needed
-> SaveChangesAsync once inside one EF transaction / Unit of Work
```

Use explicit EF transactions when one command must atomically update several records or combine business rows with audit rows.

### Legacy stored procedure policy

Existing stored-procedure-backed workflows may remain in place for now.

Do not refactor old stored procedures just because this document now prefers EF Core. Defer broad stored-procedure migration to a later dedicated cleanup unless the current task explicitly requires it or the stored-procedure shape blocks the requested feature.

Keep or use stored procedures only when clearly justified, such as:

* Stable legacy workflows that already work
* Highly set-based database operations
* Concurrency-sensitive operations where SQL locking is simpler
* Reporting/aggregation queries that are significantly clearer or faster in SQL
* One optimized database operation that would otherwise require many round trips
* Transitional compatibility with existing code

Stored-procedure call wrappers belong in Infrastructure.

Required SQL wrapper conventions for legacy/still-used procedures:

* Use `CommandType.StoredProcedure`.
* Use strongly typed `SqlParameter`.
* Use `DBNull.Value` for optional parameters.
* Capture output parameters explicitly.
* Translate known SQL workflow errors into safe Application/API errors.
* Do not expose raw SQL exception details to UI.

---

## Cloudinary and FileResource rules

Cloudinary stores actual files. SQL Server stores metadata through `manga.FileResource`.

Rules:

* Business tables should reference files through `file_resource_id`.
* Do not store raw Cloudinary URLs directly in business tables when a `FileResource` relationship is available.
* Backend validates files before upload.
* Backend uploads to Cloudinary before the database workflow.
* EF Core Unit of Work should link `FileResource` and business records atomically for new or actively changed workflows.
* Legacy stored-procedure workflows may continue to link `FileResource` and business records through SQL wrappers until deliberately refactored.
* If Cloudinary upload succeeds but the later database workflow fails, backend must attempt Cloudinary cleanup.
* Never keep a SQL transaction open while uploading to Cloudinary.
* `sha256_hash` is required for FileResource metadata.

Important file purposes include:

```text
SERIES_PROPOSAL
SERIES_COVER
CHAPTER_PAGE_VERSION
EDITORIAL_ATTACHMENT
REGISTRATION_PORTFOLIO
USER_AVATAR
```

---

## Current schema and domain reminders

### Genre and tag model

Genres and tags are normalized. Do not use old single-column genre design.

Use:

```text
manga.Genre
manga.SeriesGenre
manga.Tag
manga.SeriesTag
```

Do not add or reintroduce:

```text
Series.genre
SeriesProposal.genre_snapshot
SeriesProposal.tag_snapshot
```

Genre/tag metadata belongs to the current `Series` profile.

`SeriesProposal` snapshots only the submitted proposal-specific content:

```text
proposal title
proposal synopsis
proposal file
proposal version
proposal status
```

Proposal review screens should display cover, genres, and tags from the current locked `Series` metadata, not from proposal snapshot fields.

### Series cover

Series cover is current series metadata via `manga.FileResource` with purpose:

```text
SERIES_COVER
```

Do not add cover snapshot fields to `SeriesProposal`.

### Series identity and slug

* `series_id` is the internal backend identity.
* `slug` is the stable URL identity after serialization.
* Draft management should use internal IDs.
* `/series/{slug}` is only for statuses allowed by `SeriesNavigationPolicy`.

Allowed slug page statuses:

```text
SERIALIZED
HIATUS
COMPLETED
```

`CANCELLED` is allowed only when the latest proposal exists and the latest proposal status is:

```text
APPROVED
```

Disallowed from direct `/series/{slug}` access:

```text
PROPOSAL_DRAFT
UNDER_EDITORIAL_REVIEW
UNDER_BOARD_REVIEW
CANCELLED without latest APPROVED proposal
```

The direct slug read path must enforce this rule and return not found/null when disallowed.

---

## Actor model

Use only the current MVP roles:

```text
Mangaka
Assistant
Tantou Editor
Editorial Board Member
Editorial Board Chief
System Administrator / Admin
```

Do not invent new roles unless the user explicitly changes the scope.

Important boundaries:

* Mangaka owns normal series draft creation/editing while in `PROPOSAL_DRAFT`.
* Tantou Editor handles editorial proposal/chapter review.
* Assistant works on assigned tasks only.
* Editorial Board Chief opens/closes/cancels board polls and controls official publication frequency decisions.
* Editorial Board Members vote in board polls.
* Admin manages accounts and system-level administration, not normal board decision ownership.

---

## UI rules

Use MudBlazor consistently.

For data-driven pages:

* No hardcoded mock rows in production workflow screens.
* Use typed API clients.
* Show loading state.
* Show error state with retry where appropriate.
* Show empty state.
* Do not fake counts if real API data exists.
* Preserve route safety and return navigation.

Current editor route reminder:

```text
/editor
```

Do not use:

```text
/editor/dashboard
```

Safe return URLs should only allow trusted local paths such as:

```text
/editor
/editor/...
/series/...
```

Reject unsafe values such as external URLs, protocol-relative URLs, backslashes, or JavaScript URLs.

---

## AI Hub rules

The AI Hub is advisory only.

AI output may suggest:

```text
region type
x
y
width
height
confidence
```

AI output must not automatically:

* Approve pages
* Reject pages
* Approve chapters
* Reject chapters
* Approve proposals
* Cancel workflows
* Serialize series
* Publish chapters
* Change board results

Human users must review AI suggestions before accepted output becomes saved `PageRegion` records.

---

## Session note rule

For meaningful tasks, update or create a session note under:

```text
docs/revision/
```

Recommended live file:

```text
docs/revision/_CURRENT_SESSION.md
```

When the task is finished or paused, close it as:

```text
docs/revision/yyyy-MM-dd-short-task-slug.md
```

Every session note should record:

* Branch name
* Goal/scope
* Context files read
* Files changed
* Architecture flow
* CQRS/MediatR changes
* API endpoints added/changed
* Web typed clients added/changed
* DB/stored-procedure behavior
* Build result
* Manual test checklist
* Known issues
* Remaining follow-ups
* Next step if the session crashes or pauses

Do not claim a task is complete without evidence.

Evidence can be:

* Command output
* Build result
* Test result
* File diff summary
* Manual smoke checklist
* Specific paths changed

---

## Safety rules

Never print, commit, or copy secrets, including:

* Connection strings
* Cloudinary API secrets
* SMTP credentials
* JWT secrets
* OTP values
* Passwords
* API keys
* User tokens

Do not do these unless the user explicitly asks:

* `git commit`
* `git push`
* force push
* branch reset
* destructive checkout
* deleting files broadly
* dropping database objects
* running destructive migrations
* overwriting unrelated dirty files
* changing package versions
* changing authentication flow
* changing deployment/secrets configuration

Before risky operations:

1. Explain the risk.
2. Show the exact command or file impact.
3. Ask for confirmation.
4. Log the decision in the session note.

---

## Common verification commands

Run from the repository root unless the solution path differs.

```powershell
git status --short --branch --untracked-files=all
```

```powershell
dotnet restore .\MangaManagementSystem.sln
```

```powershell
dotnet build .\MangaManagementSystem.sln --no-incremental
```

If the solution is inside a nested folder, use the actual solution path, for example:

```powershell
dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

To inspect whether ports are already in use before starting API/Web:

```powershell
Get-NetTCPConnection -LocalPort 5244,7256 -ErrorAction SilentlyContinue |
    Format-Table -AutoSize LocalAddress,LocalPort,State,OwningProcess
```

Do not kill processes blindly. Identify the owning process first.

---

## Build baseline rule

Known baseline from recent work:

```text
0 errors
60 pre-existing code warnings
```

Warning counts may vary because of incremental build caching or Visual Studio file locks.

When checking whether a task introduced new warnings:

1. Run a full non-incremental build.
2. Compare changed-file warnings.
3. Do not claim “new warnings” unless the warning points to a changed file or newly added file.
4. Do not hide warnings with suppressions unless the root cause is understood and the user agrees.

---

## Required planning output for implementation tasks

Before editing a non-trivial feature, produce a compact plan with:

```text
Clean Architecture placement:
- Web:
- API:
- Application:
- Infrastructure:
- Domain:
- Database/EF/SP:

Web-to-API flow:
Razor Page -> Typed API Client -> API Controller -> IMediator.Send -> Handler -> EF Repository/Unit of Work -> SQL Server

Legacy SP flow if applicable:
Razor Page -> Typed API Client -> API Controller -> IMediator.Send -> Handler -> Stored-procedure wrapper -> SQL Server stored procedure

Files likely to change:
- ...

DB/EF/SP impact:
- EF Core default / existing legacy proc / new proc only if justified / changed proc only if explicitly required

Verification:
- build command
- manual smoke checklist
```

For simple bug fixes, a shorter plan is acceptable, but still mention the architecture boundary if the fix touches workflow code.

---

## Coding behavior

* Inspect existing code before creating new patterns.
* Prefer small focused edits.
* Do not create duplicate implementations of the same workflow.
* Do not introduce “God controllers” or universal API clients.
* Use feature-based controllers and typed clients.
* Keep UI state in Razor components, not business workflow rules.
* Keep API controllers thin.
* Keep important workflow validation primarily in Application command handlers/validators; use SQL constraints as final guards and legacy stored procedures only where still intentionally retained.
* Keep external service implementation in Infrastructure.
* Prefer `AsNoTracking` for read-only EF queries.
* Use `CancellationToken` for async operations.
* Follow existing naming conventions and folder structure.
* Do not broad-refactor unrelated code.
* Do not change formatting across unrelated files.

---

## Expected final response after a coding task

When finishing a task, report:

```text
Summary
- What changed

Files changed
- path: purpose

Architecture flow
- Web -> typed API client -> API -> MediatR -> Application -> Infrastructure EF Core/UoW -> SQL Server
- Mention legacy SP wrapper only if the task still uses one

Verification
- Build/test command and result

Known issues
- Remaining warnings/errors/risks

Session note
- docs/revision/yyyy-MM-dd-short-task-slug.md
```

If build/test was not run, say so clearly and explain why.

---

## Out-of-scope reminders

Do not implement unless explicitly requested:

* Public manga reader portal
* Public reader accounts
* E-commerce
* Subscriptions
* Payments
* Payroll
* Salary calculation
* Full drawing/inking/layer editor
* Generic status-history tables
* `ChapterSubmission`
* `SeriesBoardDecision`
* Persistent AI job history when accepted AI output is enough
* `ChapterTranslation`
* `PageRegionTranslation`
* Full localization asset workflow
* Automatic AI approval/rejection of workflow records

---

## Quick task interpretation rule

When the user asks for a new feature:

1. Map the request to the MVP actor and business flow.
2. Check whether it is in scope.
3. Check whether it touches schema, EF Core mappings/repositories, legacy stored procedures, API, Web, or all layers.
4. State the implementation path and prefer EF Core for new/changed workflows unless a stored procedure is clearly justified.
5. Execute only the requested scope.
6. Update `docs/revision` with evidence.

When the user says “continue”:

* Continue the next logical step from the current plan.
* Do not ask for confirmation unless the next action is risky, destructive, schema-changing, or ambiguous.

When the user asks for a prompt for another AI agent:

* Include the latest architecture rules.
* Include current schema reminders.
* Include exact files/areas to inspect.
* Tell the agent to continue until build passes or until a blocker requires user confirmation.
* Tell the agent to write/update a `docs/revision` handoff.
