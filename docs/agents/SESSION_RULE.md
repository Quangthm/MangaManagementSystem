# MangaFlow / MangaManagementSystem — Session Progress Rule

## Purpose

This file defines how AI agents, OpenCode, Claude, GitHub Copilot, Antigravity, or any other coding assistant must track progress while working on MangaFlow / MangaManagementSystem.

The goal is simple:

```text
If the session crashes, context is lost, the machine restarts, or another agent continues the task,
the next session should resume in under 5 minutes without guessing what happened.
```

This rule applies to all meaningful implementation, debugging, schema, stored procedure, UI, API, architecture, and documentation tasks.

---

## Required live session file

For every meaningful task, maintain one active live session file:

```text
docs/revision/_CURRENT_SESSION.md
```

If `_CURRENT_SESSION.md` is already being used by another active task, create a topic-specific live file instead:

```text
docs/revision/_CURRENT_SESSION_<short-topic>.md
```

Examples:

```text
docs/revision/_CURRENT_SESSION_editor-proposal-genres-tags.md
docs/revision/_CURRENT_SESSION_board-poll-ui.md
docs/revision/_CURRENT_SESSION_assistant-task-workflow.md
```

Do not overwrite another active session file unless the user explicitly confirms it belongs to the same task.

---

## Core principles

1. **Do not trust memory.**
   Before writing that something was done, verify it through a file read, command output, diff, build result, test result, or concrete observation.

2. **Update during the session, not only at the end.**
   The live session file must be updated after meaningful actions.

3. **Append-only progress log.**
   Do not delete old progress entries during the same session. If an entry was wrong, add a correction entry later.

4. **Evidence matters more than confidence.**
   Mark unverified claims as `[UNVERIFIED]`.

5. **Do not hide failed attempts.**
   Errors, failed builds, wrong hypotheses, and rollback decisions must be recorded because they help the next agent avoid repeating the same mistake.

6. **Do not claim completion without verification.**
   A task is not complete unless build/test/manual smoke status is recorded, or the note clearly says verification was not run.

7. **Respect user and other-agent changes.**
   Do not revert or overwrite dirty files unless the user explicitly asks.

---

## When to create or update the live session file

Update `docs/revision/_CURRENT_SESSION.md` immediately after these events:

| Trigger                                 | Required log content                                     |
| --------------------------------------- | -------------------------------------------------------- |
| Start of meaningful task                | Goal, branch, context files read, initial git status     |
| Read important context files            | File paths and why they matter                           |
| Make a plan                             | Planned architecture flow, affected layers, likely files |
| Edit/create/delete source files         | Path and short description                               |
| Modify SQL scripts or stored procedures | Procedure/table names, purpose, rollback risk            |
| Modify API contracts                    | Endpoint, request/response impact                        |
| Modify Web typed API clients            | Client/interface path and endpoint consumed              |
| Modify Razor UI                         | Page/component path and user-visible behavior            |
| Run build/test                          | Exact command and result                                 |
| Run app or manual smoke                 | What was tested and observed                             |
| Encounter error/blocker                 | Symptom, likely cause, attempted fix, next action        |
| Make a user-confirmed decision          | Decision, rationale, who confirmed                       |
| Change direction                        | Old plan, new plan, reason                               |
| Finish or pause task                    | Final status, remaining work, next recommended step      |

Do not update the session file for every tiny grep/read operation unless it changes the task direction.

---

## Required live session file structure

Use this structure for `docs/revision/_CURRENT_SESSION.md`.

````markdown
# _CURRENT_SESSION — <short task title>

**Started:** yyyy-MM-ddTHH:mm:ssZ
**Agent:** <OpenCode / Claude / ChatGPT / Copilot / Antigravity / other>
**Branch:** <current branch>
**Goal:** <1-3 lines>
**Status:** IN_PROGRESS

---

## 0. Context loaded

- [ ] `docs/agents/AGENTS.md`
- [ ] `docs/agents/AI_AGENT_SKILLS_GUIDE.md`
- [ ] `docs/context.md`
- [ ] `docs/business-rules.md`
- [ ] `docs/business-flows-use-cases.md`
- [ ] `docs/functional-requirements.md`
- [ ] `docs/ui-spec.md`
- [ ] `docs/user-stories.md`
- [ ] Latest relevant file(s) in `docs/revision/`

Notes:

- <Important context discoveries>

---

## 1. Verified state at start

Command:

```powershell
git status --short --branch --untracked-files=all
````

Result:

```text
<paste concise result>
```

Current branch:

```text
<branch name>
```

Important dirty/untracked files:

```text
<list or "None">
```

Do not overwrite these without user confirmation:

```text
<list or "None">
```

---

## 2. Task scope

### In scope

* <item>

### Out of scope

* <item>

### Architecture flow

```text
Blazor Web
-> typed API client
-> API controller
-> IMediator.Send(command/query)
-> Application handler
-> Infrastructure repository/SP wrapper
-> SQL Server
```

### DB/SP impact

```text
None / Existing procedure / New procedure / Changed procedure / Migration / Seed data
```

---

## 3. Plan

1. <step>
2. <step>
3. <step>

---

## 4. Progress log

### yyyy-MM-ddTHH:mm:ssZ — Session started

* Loaded context:

  * `<file>`
* Verified branch:

  * `<branch>`
* Initial state:

  * `<summary>`

### yyyy-MM-ddTHH:mm:ssZ — <short event title>

* Changed:

  * `<path>` — <short explanation>
* Evidence:

  * `<command/file observation/build result>`
* Next:

  * `<next action>`

---

## 5. Files changed this session

| Path     | Change      | Verified |
| -------- | ----------- | -------- |
| `<path>` | `<summary>` | Yes/No   |

---

## 6. Commands run

| Command            | Result            | Notes       |
| ------------------ | ----------------- | ----------- |
| `dotnet build ...` | Pass/Fail/Not run | `<summary>` |

---

## 7. Build/test/manual verification

### Build

Command:

```powershell
<command>
```

Result:

```text
Not run yet
```

### Tests

Command:

```powershell
<command>
```

Result:

```text
Not run yet
```

### Manual smoke

* [ ] <manual check>
* [ ] <manual check>

Observed result:

```text
Not run yet
```

---

## 8. Known issues / risks

| Issue     | Impact     | Next action     |
| --------- | ---------- | --------------- |
| `<issue>` | `<impact>` | `<next action>` |

---

## 9. Final status

**Status:** IN_PROGRESS / PAUSED / DONE / BLOCKED

Summary:

* <summary>

Remaining work:

* <item>

Next recommended step:

* <item>

````

---

## Required final handoff file

When the task is finished or paused, close the live session by creating a dated handoff file:

```text
docs/revision/yyyy-MM-dd-short-task-slug.md
````

Examples:

```text
docs/revision/2026-06-21-genre-tag-proposal-ui.md
docs/revision/2026-06-21-editor-board-poll-flow.md
docs/revision/2026-06-21-assistant-workspace-task-panel.md
```

The final handoff file must include:

````markdown
# <Task Title>

## Branch

`<branch>`

## Date

yyyy-MM-dd

## Task summary

<short summary>

## Architecture path

```text
Web -> typed API client -> API -> MediatR -> Application -> Infrastructure -> SQL/SP
````

## Files changed

| Layer          | File               | Change     |
| -------------- | ------------------ | ---------- |
| Web            | `<path>`           | `<change>` |
| API            | `<path>`           | `<change>` |
| Application    | `<path>`           | `<change>` |
| Infrastructure | `<path>`           | `<change>` |
| Database       | `<path/procedure>` | `<change>` |

## DB/SP impact

<None / changed procedures / added procedures / migration / seed changes>

## Behavior changed

* <item>

## Verification

### Build

```text
<command and result>
```

### Tests

```text
<command and result>
```

### Manual smoke

* [ ] <check>
* [ ] <check>

## Known issues

* <item or "None">

## Follow-ups

* <item or "None">

````

After the final handoff is created, either:

1. Delete `_CURRENT_SESSION.md` if all content was moved into the final handoff, or
2. Change its status to `CLOSED` and point to the final handoff.

Preferred:

```text
Create final handoff -> delete _CURRENT_SESSION.md
````

Only delete the live file after the final handoff exists.

---

## Timestamp rule

Use ISO 8601 UTC timestamps where possible:

```text
2026-06-21T03:45:00Z
```

If UTC is not available, use local time and label it clearly:

```text
2026-06-21 10:45 ICT
```

Avoid vague time phrases:

```text
just now
earlier
a while ago
last time
recently
```

---

## Verification rules

### Build verification

Use the actual solution path.

Preferred:

```powershell
dotnet build .\MangaManagementSystem.sln --no-incremental
```

If the solution is nested:

```powershell
dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

Record:

* command
* pass/fail
* error count
* warning count
* whether warnings are pre-existing or from changed files

Known baseline:

```text
0 errors
60 pre-existing code warnings
```

Do not claim a warning is new unless it points to a changed file or newly added file.

### Git verification

At the start and end of meaningful tasks, run:

```powershell
git status --short --branch --untracked-files=all
```

Record important dirty files.

### File verification

Before saying a file was changed, verify it exists and inspect the relevant section.

Useful commands:

```powershell
git diff -- <path>
```

```powershell
Get-Content <path> -TotalCount 80
```

```powershell
Select-String -Path <path> -Pattern "<pattern>"
```

### Runtime verification

Only mark manual smoke checks as passed if actually run.

Do not write:

```text
Manual smoke passed
```

unless the UI/API flow was actually executed.

Instead write:

```text
Manual smoke not run; build-only verification completed.
```

---

## Error and blocker logging

When an error happens, log:

````markdown
### yyyy-MM-ddTHH:mm:ssZ — Blocked: <short title>

Symptom:

```text
<exact error message or concise output>
````

Likely cause:

```text
<cause or "Unknown">
```

Attempted fixes:

* <attempt>

Result:

```text
<result>
```

Next action:

* <next step>

````

If the same error appears again, reference the earlier entry instead of repeating all details.

---

## Risky operation rule

The agent must ask the user before doing any risky operation.

Risky operations include:

- `git commit`
- `git push`
- force push
- reset/rebase/destructive checkout
- deleting many files
- dropping database objects
- destructive migrations
- overwriting unrelated dirty files
- changing authentication flow
- changing package versions
- changing deployment configuration
- changing secrets or appsettings containing secrets
- running commands that kill processes
- clearing local database data
- modifying production-like data

Before asking, the agent must log a pre-flight entry:

```markdown
### yyyy-MM-ddTHH:mm:ssZ — Pre-flight risky operation

Requested operation:

```text
<operation>
````

Risk:

```text
<risk>
```

Files/data affected:

```text
<paths/data>
```

Exact command or action:

```powershell
<command>
```

Status:

```text
Waiting for user confirmation.
```

````

Do not proceed until the user confirms.

---

## Dirty worktree rule

If `git status` shows dirty files that are unrelated to the current task:

1. List them in the session note.
2. Treat them as user/other-agent changes.
3. Do not format, revert, move, or overwrite them.
4. If editing a dirty file is unavoidable, inspect the diff first and mention why the edit is needed.
5. If the dirty file appears risky, ask the user.

---

## Context conflict rule

When documents disagree, follow this priority:

1. Latest explicit user instruction in the current session.
2. Latest updated business docs in `docs/`.
3. Latest relevant handoff in `docs/revision/`.
4. `docs/agents/AI_AGENT_SKILLS_GUIDE.md`.
5. Existing code.
6. Older handoffs or old generated plans.

If the conflict affects schema, stored procedures, role permissions, or workflow state transitions, stop and ask the user before changing implementation.

---

## Common session statuses

Use one of these statuses:

```text
IN_PROGRESS
PAUSED
BLOCKED
DONE
CLOSED
````

Meaning:

| Status        | Meaning                                                              |
| ------------- | -------------------------------------------------------------------- |
| `IN_PROGRESS` | Work is active.                                                      |
| `PAUSED`      | Work can continue later; next step is known.                         |
| `BLOCKED`     | Work cannot continue without user input or external fix.             |
| `DONE`        | Work is completed and verified as far as possible.                   |
| `CLOSED`      | Final handoff exists and live session file should no longer be used. |

---

## Minimum log for very small tasks

For tiny tasks, a full live session note is optional.

A task is considered tiny if all are true:

* One or two files changed.
* No schema change.
* No stored procedure change.
* No new API endpoint.
* No architecture change.
* No risky operation.
* No user workflow behavior change.
* Can be verified by a quick build or file inspection.

Even for tiny tasks, the final assistant response should still say:

```text
Build/test run or not run
Files changed
Any known risk
```

---

## Examples

### Example progress entry after editing a Razor page

```markdown
### 2026-06-21T04:10:00Z — Updated proposal review cover layout

- Changed:
  - `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor`
- Summary:
  - Increased cover display width.
  - Preserved 2:3 ratio.
  - Kept returnUrl back navigation unchanged.
- Evidence:
  - Inspected diff with `git diff -- ProposalReviewDetail.razor`.
- Next:
  - Run full build and changed-file warning check.
```

### Example progress entry after build

````markdown
### 2026-06-21T04:18:00Z — Build completed

Command:

```powershell
dotnet build .\MangaManagementSystem\MangaManagementSystem.sln --no-incremental
````

Result:

```text
Build succeeded.
0 errors.
60 warnings.
Warnings match known baseline; no warnings from changed files.
```

Next:

* Manual smoke test proposal detail page.

````

### Example blocker entry

```markdown
### 2026-06-21T04:25:00Z — Blocked: API port already in use

Symptom:

```text
Failed to bind to address http://127.0.0.1:5244: address already in use.
````

Likely cause:

```text
Previous Visual Studio debug process still owns port 5244.
```

Attempted fixes:

* Ran `Get-NetTCPConnection -LocalPort 5244`.
* Identified owning process.

Next action:

* Ask user before killing the process.

```

---

## Final reminder

The session note is not a decoration. It is part of the engineering workflow.

If another agent cannot continue from the note, the note is not good enough.
```
