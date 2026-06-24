# MangaFlow / MangaManagementSystem — Resume Pack

## Purpose

This file gives AI agents a compact current-state summary so they can start or resume MangaFlow / MangaManagementSystem work without reopening old decisions or guessing from stale context.

Read this file after:

```text
docs/agents/AGENTS.md
docs/agents/AI_AGENT_SKILLS_GUIDE.md
docs/agents/SESSION_RULE.md
```

Then read the latest relevant handoff under:

```text
docs/revision/
```

---

## Project identity

| Field                       | Value                                                                                                                                                                                                                         |
| --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Project name                | MangaFlow / MangaManagementSystem                                                                                                                                                                                             |
| Product name                | Manga Creation Workflow and Publishing Management System                                                                                                                                                                      |
| Current main working branch | `feature/Mangaka`                                                                                                                                                                                                             |
| Main stack                  | .NET 8, Blazor Server, ASP.NET Core Web API, Clean Architecture, MediatR/CQRS, SQL Server, Cloudinary, MudBlazor                                                                                                              |
| Architecture style          | Distributed Monolith                                                                                                                                                                                                          |
| Main purpose                | Manage manga production workflow from account approval, series proposal, chapter/page production, assistant tasks, editorial review, board polling, publication planning, ranking simulation, notifications, and auditability |
| Not in scope                | Payroll, salary, e-commerce, public reader accounts, payment/subscription, full drawing software, full localization workflow, public manga reader module                                                                      |

---

## Required startup procedure

At the start of a meaningful coding session:

1. Read:

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
latest relevant docs/revision/*.md
```

2. Run:

```powershell
git status --short --branch --untracked-files=all
```

3. Record:

```text
current branch
dirty files
untracked files
relevant latest handoff
task scope
```

4. Create or update:

```text
docs/revision/_CURRENT_SESSION.md
```

5. Inspect existing implementation before creating new files.

---

## Source-of-truth priority

When documents, code, or handoffs conflict, follow this order:

1. Latest explicit user instruction in the current session.
2. Latest updated business docs in `docs/`.
3. Latest relevant handoff in `docs/revision/`.
4. `docs/agents/AI_AGENT_SKILLS_GUIDE.md`.
5. Existing code.
6. Older handoffs or older generated plans.

If a conflict affects schema, stored procedures, roles, workflow statuses, permissions, or FileResource behavior, stop and report before editing.

---

## Locked architecture rules

All new or migrated business workflows must use:

```text
Blazor Web
-> typed Web API client
-> ASP.NET Core API controller
-> IMediator.Send(command/query)
-> Application command/query handler
-> Infrastructure repository / adapter / stored-procedure wrapper
-> SQL Server stored procedure or EF read query
```

Do not add new:

```text
Razor/Web -> Application service direct workflow calls
Razor/Web -> Infrastructure direct calls
Razor/Web -> DbContext direct calls
Razor/Web -> stored procedure direct calls
API controller -> DbContext direct workflow calls
API controller -> stored procedure direct workflow calls
```

Legacy direct calls may exist. Do not expand them. When touching a migrated or new workflow, use the API boundary.

---

## Layer responsibilities

### Web

* Blazor Server Razor pages/components
* MudBlazor UI
* Typed API clients
* Local UI state
* Dialogs, snackbars, loading/error/empty states
* Safe returnUrl and navigation behavior

Web must not contain core business workflow rules.

### API

* Thin HTTP adapter
* Route/query/body/form binding
* Actor identity extraction
* Authorization boundary
* `IMediator.Send(...)`
* HTTP response mapping

API must not own workflow transitions or call SQL/Cloudinary directly for business workflows.

### Application

* CQRS commands/queries
* MediatR handlers
* Use-case orchestration
* DTOs/results
* Validators
* Application-facing abstractions
* Business validation that belongs outside SQL

Application must not depend on Infrastructure concrete classes, EF concrete DbContext, Cloudinary SDK, SMTP SDK, or Web/API UI details.

### Infrastructure

* EF Core repository implementations
* SQL Server stored-procedure wrappers
* ADO.NET / SqlClient implementation
* Cloudinary adapter
* SMTP/email adapter
* AI Hub adapter
* External service implementation hidden behind Application abstractions

### Domain

* Entities and core domain concepts
* Domain constants/enums/value objects if established
* No infrastructure, API, or Web dependencies

---

## CQRS / MediatR rules

Use Commands for state-changing actions:

```text
create draft
update draft
submit proposal
cancel draft/proposal
upload/replace files
assign task
submit assistant work
review task work
create/resolve annotation
open/close/cancel board poll
vote
apply board result
change official publication frequency
approve/reject/disable user
```

Use Queries for read-only screens:

```text
dashboards
review queues
series lists
proposal details
chapter review lists
annotation lists
workspace read models
ranking display
audit feed
notification lists
```

Rules:

* No database writes from Query handlers.
* No workflow transition logic in Razor components.
* No direct MediatR calls from Razor components.
* Razor components call typed API clients only.
* API controllers call `IMediator.Send(...)`.
* Use `CancellationToken` in async handlers and clients.

---

## SQL Server and stored procedure rules

SQL Server is the workflow source of truth.

Use stored procedures or stored-procedure wrappers for:

```text
multi-table writes
workflow status transitions
FileResource creation/linking
approval/rejection/cancellation
task submission/review
board poll open/close/cancel/vote/apply result
account status changes
audit event creation
concurrency-sensitive rules
permission checks that must be enforced at DB level
```

EF Core is acceptable for read-only projections:

```text
dashboard queries
list pages
review queues
detail read models
filters/search
AsNoTracking joins
```

Do not implement important multi-table workflows as many unrelated EF `SaveChangesAsync` operations.

---

## FileResource and Cloudinary decisions

Cloudinary stores actual files. SQL Server stores file metadata and relationships using:

```text
manga.FileResource
```

Important file purposes:

```text
SERIES_PROPOSAL
SERIES_COVER
CHAPTER_PAGE_VERSION
EDITORIAL_ATTACHMENT
REGISTRATION_PORTFOLIO
USER_AVATAR
```

Rules:

* Business tables reference files through `file_resource_id`.
* Do not store raw Cloudinary URLs directly in business tables if `FileResource` should be used.
* Backend validates file type/size/hash before or during upload workflow.
* `sha256_hash` is required for uploaded business files.
* Upload to Cloudinary first.
* Then call SQL workflow to create/link `FileResource`.
* If SQL fails after Cloudinary upload, attempt Cloudinary cleanup.
* Do not keep SQL transactions open during Cloudinary upload.

Retention decisions:

* `CHAPTER_PAGE_VERSION` files are retained for history.
* `USER_AVATAR` and `REGISTRATION_PORTFOLIO` may soft-delete previous files on update.
* `SERIES_COVER` replacement should soft-delete the old active cover row.

---

## Current normalized genre/tag design

The latest design uses normalized genre/tag tables.

Use:

```text
manga.Genre
manga.SeriesGenre
manga.Tag
manga.SeriesTag
```

Meaning:

* `Genre` = broad category such as Action, Romance, Fantasy.
* `Tag` = specific trope/theme/setting/descriptor such as school-life, revenge, slow-burn, isekai.

Do not add or reintroduce:

```text
Series.genre
SeriesProposal.genre_snapshot
SeriesProposal.tag_snapshot
```

Genre/tag metadata belongs to the current `Series` profile.

`SeriesProposal` snapshots only proposal-specific submitted content:

```text
proposal title
proposal synopsis
proposal file
proposal version number
proposal status
submitted/review timestamps
```

Proposal review screens should display these from current locked Series metadata:

```text
cover
genres
tags
language
publication frequency
series status
```

Series cover is current series metadata via `FileResource` purpose:

```text
SERIES_COVER
```

Do not add cover snapshots to `SeriesProposal`.

---

## Series lifecycle and slug navigation

`series_id` is the internal backend identity.

`slug` is the stable URL identity after serialization.

Draft management uses internal IDs, not public slug routing.

Allowed direct `/series/{slug}` statuses:

```text
SERIALIZED
HIATUS
COMPLETED
```

`CANCELLED` is allowed only when:

```text
LatestProposalId exists
LatestProposalStatusCode == APPROVED
SeriesSlug exists
```

Disallowed from direct `/series/{slug}`:

```text
PROPOSAL_DRAFT
UNDER_EDITORIAL_REVIEW
UNDER_BOARD_REVIEW
CANCELLED without latest APPROVED proposal
```

The Application layer owns slug eligibility via `SeriesNavigationPolicy`.

The Web layer builds routes using DTO booleans such as:

```text
CanOpenSeriesSlugPage
```

Web must not duplicate business eligibility logic.

Direct `/series/{slug}` API reads must enforce the navigation policy and return not found/null when disallowed.

---

## Current MVP actors

Use only these roles unless the user explicitly changes scope:

```text
Mangaka
Assistant
Tantou Editor
Editorial Board Member
Editorial Board Chief
System Administrator / Admin
```

Boundaries:

| Actor                        | Responsibilities                                                                                          |
| ---------------------------- | --------------------------------------------------------------------------------------------------------- |
| Mangaka                      | Create/edit series draft while `PROPOSAL_DRAFT`, submit proposal, manage own series production work       |
| Assistant                    | Work on assigned page/region tasks only                                                                   |
| Tantou Editor                | Review proposals, review chapters/pages, create annotations/tasks, manage editorial flow                  |
| Editorial Board Member       | Vote in board polls, participate in board decisions                                                       |
| Editorial Board Chief        | Open/close/cancel board polls, set official publication frequency during serialization decision, may vote |
| System Administrator / Admin | Approve/reject/disable accounts, manage admin/system concerns                                             |

Admin does not own normal series draft creation/editing.

Board decision workflow should use:

```text
SeriesBoardPoll
SeriesBoardVote
```

Do not add:

```text
SeriesBoardDecision
```

Chapter review should use chapter status and review records as designed.

Do not add:

```text
ChapterSubmission
```

---

## Key workflow status reminders

Series/proposal statuses commonly used:

```text
PROPOSAL_DRAFT
UNDER_EDITORIAL_REVIEW
UNDER_BOARD_REVIEW
SERIALIZED
HIATUS
COMPLETED
CANCELLED
APPROVED
REJECTED
```

Account statuses:

```text
PENDING_APPROVAL
ACTIVE
DISABLED
REJECTED
```

Assistant task statuses:

```text
ASSIGNED
UNDER_REVIEW
COMPLETED
CANCELLED
```

Always inspect current DB scripts/enums/constants before adding or renaming statuses.

Do not invent statuses without checking business docs and current SQL constraints.

---

## Current UI routing reminders

Editor dashboard route:

```text
/editor
```

Do not use:

```text
/editor/dashboard
```

Common editor routes:

```text
/editor
/editor/proposals
/editor/proposals/{proposalId}
/editor/series
/editor/chapters
/editor/chapters/{chapterId}
```

Series route:

```text
/series/{slug}
```

Workspace route currently used in recent flows:

```text
/series/{slug}/workspace?chapterId={chapterId}
```

Return URL behavior:

* Use safe local returnUrl only.
* Accept `/editor`, `/editor/...`, and `/series/...`.
* Reject external URLs, protocol-relative URLs, backslashes, and JavaScript URLs.
* Back buttons should prefer `Href` with resolved safe target when possible.

---

## Latest Editor UI state

Recent Editor UI work replaced several mock pages with real data through the canonical architecture path.

Real-data migrated areas include:

```text
Editor Dashboard
Proposal Queue / Proposal List
Proposal Review Detail
Chapter Review List
Chapter Review Detail / Workspace link behavior
Annotation Workspace
Series Library
```

Rules:

* Do not reintroduce hardcoded mock data into these production workflow pages.
* Use typed API clients.
* Use loading/error/empty states.
* Keep returnUrl behavior safe.
* Proposal review cover/metadata should use current Series metadata.
* Proposal review back button should resolve safe return URL and use `Href`.

Known route correction:

```text
/editor
```

not:

```text
/editor/dashboard
```

---

## Build baseline

Known recent full-build baseline:

```text
0 errors
60 pre-existing code warnings
```

Warning count may vary due to:

```text
incremental build caching
Visual Studio / running process file locks
MSB3026 copy retry warnings
MSB3061 delete failure warnings
```

When verifying a task:

1. Run full non-incremental build.
2. Record error count.
3. Record warning count.
4. Check whether warnings point to changed files.
5. Do not claim new warnings unless they are from changed or newly added files.

---

## Common verification commands

Run from repository root.

```powershell
git status --short --branch --untracked-files=all
```

Preferred build if solution is at repo root:

```powershell
dotnet build .\MangaManagementSystem.sln --no-incremental
```

Build if solution is nested:

```powershell
dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

Check common debug ports:

```powershell
Get-NetTCPConnection -LocalPort 5244,7256 -ErrorAction SilentlyContinue |
    Format-Table -AutoSize LocalAddress,LocalPort,State,OwningProcess
```

Do not kill owning processes without user confirmation.

---

## Current known risks

| Risk                                   | Impact                                                 | Rule                                                      |
| -------------------------------------- | ------------------------------------------------------ | --------------------------------------------------------- |
| Old genre design returns               | Agents may re-add `Series.genre` or proposal snapshots | Always use normalized Genre/Tag tables                    |
| Direct Web-to-Application calls return | Architecture drift                                     | New/migrated workflows must use typed API clients         |
| Mock data returns to workflow pages    | Demo becomes unreliable                                | Production workflow pages must use API real data          |
| Proposal review reads wrong metadata   | Review screen may display stale snapshots              | Use current locked Series metadata for cover/genres/tags  |
| Cloudinary orphan files                | Storage cleanup problem                                | Attempt cleanup when SQL workflow fails after upload      |
| Stored procedure bypass                | Missing audit/transaction/permission enforcement       | Important writes go through SP/wrapper                    |
| Slug page direct access leaks drafts   | Users can open draft/review series by URL              | Enforce `SeriesNavigationPolicy` in Application read path |
| Wrong editor dashboard route           | Back button/returnUrl 404                              | Use `/editor`, not `/editor/dashboard`                    |
| Warning count confusion                | Agents may report false new warnings                   | Compare changed-file warnings against baseline            |

---

## Do not implement unless explicitly requested

```text
public manga reader portal
public reader accounts
e-commerce
subscriptions
payments
payroll
salary calculation
full drawing/inking/layer editor
generic status-history tables
ChapterSubmission
SeriesBoardDecision
ChapterTranslation
PageRegionTranslation
persistent AI job history when accepted output is enough
automatic AI approval/rejection of workflow records
```

---

## Resume procedure for interrupted sessions

When continuing a task:

1. Read:

```text
docs/revision/_CURRENT_SESSION.md
```

If it does not exist, read the latest related dated handoff in `docs/revision/`.

2. Run:

```powershell
git status --short --branch --untracked-files=all
```

3. Compare:

```text
files changed in session note
actual git diff
current task request
```

4. Categorize work:

```text
Verified done
Done but unverified
Still pending
Blocked
Unrelated dirty files
```

5. Continue only from the next logical step.

6. Do not redo verified completed work unless there is evidence it is broken.

7. Do not overwrite unrelated dirty files.

---

## Required final handoff

Every meaningful completed or paused task should create:

```text
docs/revision/yyyy-MM-dd-short-task-slug.md
```

The handoff must include:

```text
Branch
Date
Task summary
Architecture path
Files changed by layer
DB/SP impact
API endpoints changed
Web typed clients changed
UI behavior changed
Business rules enforced
Build result
Test result
Manual smoke checklist
Known issues
Follow-ups
Next step if another agent continues
```

Do not claim manual smoke passed unless it was actually run.

---

## Quick current-state checklist for agents

Before editing, confirm:

```text
[ ] I know the current branch.
[ ] I checked git status.
[ ] I read AGENTS.md.
[ ] I read AI_AGENT_SKILLS_GUIDE.md.
[ ] I read SESSION_RULE.md.
[ ] I read this RESUME_PACK.md.
[ ] I read the latest relevant docs/revision handoff.
[ ] I inspected existing code before creating new files.
[ ] I know whether the task touches Web/API/Application/Infrastructure/Domain/SQL.
[ ] I know whether DB/SP changes are required.
[ ] I know how the task will be verified.
```

Before final response, confirm:

```text
[ ] I recorded files changed.
[ ] I recorded DB/SP impact.
[ ] I ran build or clearly explained why not.
[ ] I recorded manual smoke status or clearly said not run.
[ ] I created or updated docs/revision handoff.
[ ] I listed known issues/follow-ups.
```

---

## One-line memory for agents

```text
MangaFlow is a Clean Architecture .NET 8 manga workflow system: Web must call API via typed clients, API sends MediatR commands/queries, Application orchestrates use cases, Infrastructure hides SQL Server/Cloudinary, important workflow writes go through stored procedures, Genre/Tag are normalized Series metadata, and SeriesProposal only snapshots proposal title/synopsis/file.
```
