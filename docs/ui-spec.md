# MangaManagementSystem UI Specification — Series Draft, Slug, and Authorized Chapter Workspace

**Project:** Manga Creation Workflow and Publishing Management System  
**Target UI:** Blazor Server / MudBlazor MVP  
**Last updated:** 2026-06-09  
**Purpose:** Define the UI behavior for Mangaka-owned series drafting, slug usage, stable series URLs, and the centralized chapter-level workspace.

---

## 1. Design Direction Summary

| Area | Decision |
|---|---|
| Series draft ownership | Mangaka owns normal series draft creation and editing. Admin does not create or edit series drafts as business flow. |
| Draft routing | Draft series are managed internally by `series_id`, not by public slug URL. |
| Separate business identifier | No separate human-readable business identifier is used; `series_id` is the backend identity and `slug` is the URL identity. |
| Slug behavior | Backend generates slug from title during `PROPOSAL_DRAFT`; slug may auto-update when title changes during draft; slug locks after the series leaves `PROPOSAL_DRAFT`. |
| Stable series URL | `/series/{slug}` becomes the main stable series URL, especially after the series becomes `SERIALIZED`. |
| Series profile editing | Normal Mangaka edits are allowed only while `status_code = PROPOSAL_DRAFT`. |
| Publication frequency | Mangaka may set `publication_frequency_code` during draft as a proposed/preferred frequency; board serialization workflow may override it later. |
| Workspace level | The centralized authorized workspace is chapter-level, not page-level. |
| Workspace AI tools | AI segmentation and AI/OCR translation tools are available to all Authorized Page Workspace Users who have access to the relevant workspace. |
| Workspace permissions | AI tools are shared; business actions remain role-specific and permission-gated. |

---

## 2. Main Routes

| Route | Purpose | Access |
|---|---|---|
| `/mangaka/series/drafts` | Mangaka draft list and draft management entry point. | Mangaka |
| `/mangaka/series/drafts/{seriesId}` | Optional full draft detail/edit page if modal is not enough. | Mangaka contributor |
| `/editor/proposals` | Editorial proposal queue, prioritizing unclaimed submitted proposals. | Tantou Editor |
| `/editor/proposals/{proposalId}` | Proposal review detail page for request revision, cancel, or pass to board. | Tantou Editor contributor / authorized Tantou Editor |
| `/series/{slug}` | Stable main series page after serialization; future public reader URL can reuse it. | Authorized now; public in future |
| `/workspace/chapters/{chapterId}` | Central authorized chapter workspace. | Authorized Page Workspace User |
| `/workspace/chapters/{chapterId}?page={pageNumber}` | Workspace opened with selected page. | Authorized Page Workspace User |
| `/workspace/chapters/{chapterId}?page={pageNumber}&version={versionId}` | Workspace opened with selected page version. | Authorized Page Workspace User |

### Route principles

- Use `series_id` for draft management and internal workflow operations.
- Use slug mainly for the stable main series page.
- Use `chapter_id`, `chapter_page_id`, and `chapter_page_version_id` for workspace editing context.
- Do not use slug as the primary identifier for page/version editing.

---

## 3. Mangaka Series Draft List

### Route

```text
/mangaka/series/drafts
```

### Purpose

Allow Mangaka to create, view, edit, and submit their own series drafts before formal proposal review.

### Main UI elements

| Element | Behavior |
|---|---|
| Draft table/card grid | Shows series title, status, genres, tags, language, proposed frequency, last updated time, and action buttons. |
| Create Draft button | Opens the create draft modal. |
| Edit button | Opens the edit draft modal for a `PROPOSAL_DRAFT` series. |
| Submit Proposal button | Opens proposal submission flow for an eligible `PROPOSAL_DRAFT` series and requires a proposal file upload. |
| Disabled edit state | If series is not `PROPOSAL_DRAFT`, edit controls are disabled and explain that profile is locked after draft. |

### Draft row/card fields

- Cover thumbnail or placeholder
- Title
- `status_code`
- Genres
- Tags
- Content language
- `publication_frequency_code` as proposed frequency
- Slug preview
- Last updated timestamp

---

## 4. Create / Edit Series Draft Modal

### Fields

| Field | Required | Notes |
|---|---:|---|
| Title | Yes | Backend generates slug from this field. |
| Slug preview | Read-only for MVP | Shows backend-style preview; actual saved slug is computed on save. |
| Synopsis | Yes | Required by current database schema. |
| Genres | Yes | Multi-select from `manga.Genre`, saved through `manga.SeriesGenre`. |
| Tags | No | Optional multi-select from `manga.Tag`, saved through `manga.SeriesTag`. |
| Content language | Yes | `ja`, `en`, `vi`. |
| Cover image | No | Must reference `FileResource` with `SERIES_COVER` purpose if provided. |
| Source series | No | Cannot reference itself. |
| Publication frequency | No | `WEEKLY`, `MONTHLY`, `IRREGULAR`; treated as Mangaka proposed frequency during draft. |

### Cover crop behavior

When the user selects a series cover image in the create or edit draft modal, the UI should open a crop preview dialog before upload.

| Element | Behavior |
|---|---|
| Crop ratio | Locked to 2:3 portrait. |
| Crop controls | User can drag, zoom, reposition, reset, confirm, or cancel. |
| Output file | Confirming the crop produces a `1000×1500` PNG. |
| Upload behavior | The cropped PNG becomes the selected cover file and the original source image is not uploaded. |
| Preview | The modal may show a smaller preview such as `80×120`; this is display scaling only. |
| Smaller source image | Allowed, but the UI should warn that the final cover may look blurry after upscaling. |
| Cancel behavior | Cancelling the crop should not replace the current selected/current cover. |
| Storage behavior | No original/cropped dual storage and no crop metadata are required in MVP. |

Suggested helper text:

```text
Covers are displayed in a 2:3 portrait frame. Please crop your image to choose the visible cover area.
```

### Create-specific behavior

When creating a new series draft, the backend must create both:

- the `Series` row, and
- an active `SeriesContributor` row for the creating Mangaka,

in the same backend workflow or transaction.

The created contributor row should have `end_date = NULL`, making the creator an active Mangaka contributor for the draft immediately.

### Save behavior

```text
Mangaka clicks Save
→ Backend validates actor is active Mangaka contributor or creator
→ Backend checks series is PROPOSAL_DRAFT for update
→ Backend generates slug from title
→ Backend resolves slug uniqueness
→ Backend calls stored procedure
→ UI refreshes the affected draft card/detail using server-confirmed data
```

### Slug behavior

- On create: backend generates slug from title.
- On draft update: if title changes, backend regenerates slug from title.
- Slug can change freely while the series is `PROPOSAL_DRAFT` because draft workflows use `series_id`.
- After `PROPOSAL_DRAFT`, slug is locked.
- No slug history or redirect table is required for MVP.

---

## 5. Proposal Submission Modal

### Purpose

Allow a Mangaka contributor to formally submit a `PROPOSAL_DRAFT` series for editorial review with a required proposal file.

### Fields

| Field | Required | Notes |
|---|---:|---|
| Proposal file | Yes | Stored as `FileResource` with purpose `SERIES_PROPOSAL`; accepts only `.pdf`, `.doc`, and `.docx` in MVP. |
| Confirmation checkbox | Yes | Confirms the submitted proposal title, synopsis, and proposal file will be locked after submission. |

### Submission behavior

```text
Mangaka clicks Submit Proposal
→ UI opens proposal submission modal
→ Mangaka selects required proposal file
→ Backend validates actor is an active Mangaka contributor
→ Backend uploads proposal file to Cloudinary and calculates SHA-256
→ Backend calls proposal submission stored procedure with required file metadata
→ Database creates FileResource and SeriesProposal
→ Database updates Series.status_code to UNDER_EDITORIAL_REVIEW
→ UI removes normal draft editing controls and shows submitted review status
```

### Important notes

- First proposal submission does not require an active Tantou Editor contributor to already be assigned to the series.
- Proposal submission accepts formal document files only: `.pdf`, `.doc`, and `.docx`. Markdown, plain text, and image files are not accepted for `SERIES_PROPOSAL` in MVP.
- Submitted proposals should appear in the editorial proposal queue for active Tantou Editors.
- The queue may prioritize proposals that do not yet have any active Tantou Editor contributor, but the database should still allow multiple active Tantou Editor contributors for a series.
- After submission, normal series profile editing is locked until revision returns the series to `PROPOSAL_DRAFT`.
- Proposal review screens display current series cover, genres, and tags from locked series metadata during review.
- `SeriesProposal` does not snapshot the current cover file, genres, or tags in MVP.

---

## 5A. Mangaka Proposal Tracking Filters

### Purpose

Allow Mangaka users to track their own submitted proposal history and review status with filters that match the current normalized genre/tag model.

### Search and filter behavior

| Element | Behavior |
|---|---|
| Text search | Filters by proposal title and/or series title only. It should not match genre or tag names. |
| Genre filter | Uses selected genre IDs from current series metadata. |
| Tag filter | Uses selected tag IDs from current series metadata. |
| Genre/tag matching | Uses ALL-match behavior for selected genres/tags. |
| Clear filters | Clears selected genre/tag filters without necessarily clearing status chips or search text. |
| Status chips | Continue to filter by proposal workflow status. |
| Sort | Existing sort behavior remains available. |

---

## 5.1 MVP File Upload Acceptance Matrix

### MVP File Purpose Upload Format Matrix

| File purpose code | Allowed extensions | Allowed content types | Cloudinary resource type | Notes |
|---|---|---|---|---|
| `SERIES_PROPOSAL` | `.pdf`, `.doc`, `.docx` | `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `raw` | Formal series proposal documents only. Markdown, plain text, and image files are not accepted for proposal submission in MVP. |
| `SERIES_COVER` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Series cover image. In the Web draft UI, the cropped `1000×1500` PNG is uploaded as the actual cover. |
| `CHAPTER_PAGE_VERSION` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Official manga page image/version output. |
| `EDITORIAL_ATTACHMENT` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Editorial markup, review attachments, or supporting screenshots/documents. |
| `REGISTRATION_PORTFOLIO` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Optional portfolio submitted for account approval/profile review. |
| `USER_AVATAR` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | User profile/avatar image. |


---

## 6. Main Series Page

### Route

```text
/series/{slug}
```

### Purpose

A unified series page that can later become the public reader-facing series URL. In MVP, it can serve authorized users first.

### Main UI elements

| Section | Content |
|---|---|
| Header | Current cover, title, status badge, genres, tags, language, publication frequency. |
| Synopsis panel | Current series synopsis. |
| Chapter list | Chapters under the series with status and planned/released dates. |
| Role action panel | Buttons shown based on current user role and permission. |

### Role-based actions

| Role | Possible actions |
|---|---|
| Mangaka contributor | Add chapter, manage chapters, open workspace, view ranking context, request frequency change after board decision. |
| Assistant | Open assigned task or workspace context only for assigned work. |
| Tantou Editor | Open review workspace, review chapter, view annotations. |
| Editorial Board Member / Chief | View ranking and board context; no page workspace access by default unless explicitly granted. |
| Admin | No normal manga production actions. |

---
# Suggested Additions — UI Specification

---

## 2.x Implemented Mangaka Routes

| Route                                            | Purpose                                                                              | Access                                         | Notes                                                                                                   |
| ------------------------------------------------ | ------------------------------------------------------------------------------------ | ---------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `/mangaka`                                       | Mangaka dashboard landing page with Current Series and Series Proposals tab content. | Mangaka                                        | Uses custom dashboard shell and internal tab state. Do not refactor without Plan Mode first.            |
| `/mangaka/review-submissions`                    | Dedicated Assistant Review / task review management page.                            | Mangaka                                        | Uses `<MangakaLayout>`. Sidebar "Assistant Review" routes here.                                         |
| `/mangaka/contributors`                          | Dedicated Manage Series Contributors page.                                           | Mangaka                                        | Uses `<MangakaLayout>`. Sidebar "Manage Contributors" routes here.                                      |
| `/series/{slug}`                                 | Stable series page when allowed by navigation policy.                                | Authorized users now; public later if enabled. | Back navigation uses safe returnUrl.                                                                    |
| `/series/{slug}/workspace?chapterId={chapterId}` | Actual current chapter workspace route used by implementation.                       | Authorized workspace users.                    | Workspace links may include returnUrl. Page/version query support is deferred unless implemented later. |

### Routing notes

* `/mangaka/review-submissions` and `/mangaka/contributors` are dedicated pages, not embedded dashboard tabs.
* Current Series and Series Proposals remain inside `/mangaka` for now.
* Do not migrate `/mangaka/series` or `/mangaka/proposals` without a separate plan.
* Use safe local return URLs for cross-page navigation.
* The actual current workspace route is `/series/{slug}/workspace?chapterId={chapterId}`. If older spec text mentions `/workspace/chapters/{chapterId}`, mark it as planned/legacy unless the route is implemented.

---

## 3.x Mangaka Dashboard — Current Implemented Behavior

### Purpose

The Mangaka dashboard shows only series where the logged-in user is an active Mangaka contributor.

### Data scope

The dashboard must load series through:

```text
GET /api/mangaka/series/my-series
```

The backend filters by:

```text
SeriesContributor.UserId == actorUserId
AND SeriesContributor.EndDate == null
AND User.StatusCode == ACTIVE
AND User.Role == Mangaka
```

### Dashboard features

| Element                          | Behavior                                                    |
| -------------------------------- | ----------------------------------------------------------- |
| Current Series tab               | Shows Mangaka-owned/contributed series cards only.          |
| Series Proposals tab             | Existing dashboard proposal view; route migration deferred. |
| Assistant Review sidebar item    | Navigates to `/mangaka/review-submissions`.                 |
| Manage Contributors sidebar item | Navigates to `/mangaka/contributors`.                       |
| Search/filter/sort               | Client-side controls for already loaded series cards.       |

### Current Series card layout

Series cards use a left-side portrait cover layout:

```text
┌────────────┬──────────────────────────────┐
│  Cover     │ Title                        │
│  120×180   │ Genre chips +N more          │
│  portrait  │ Tag chips +N more            │
│            │ Status / updated time        │
│            │ View Draft / Submit / etc.   │
└────────────┴──────────────────────────────┘
```

Rules:

* Cover column is 2:3 portrait.
* Missing cover shows a placeholder.
* Genre chips and tag chips use `+N more` behavior.
* Card click behavior remains status-aware:

  * `PROPOSAL_DRAFT`: show draft details/edit flow.
  * `SERIALIZED` and allowed statuses: navigate to `/series/{slug}`.
  * review/locked statuses: show review/status modal or disabled state.

---

## 4.x Create / Edit Series Draft Modal — Implemented UI Details

### Create Draft modal

Add/confirm these fields:

| Field                 | Behavior                                                                 |
| --------------------- | ------------------------------------------------------------------------ |
| Title                 | Required.                                                                |
| Synopsis              | Required.                                                                |
| Genres                | Required multi-select from normalized `manga.Genre`.                     |
| Tags                  | Optional multi-select from normalized `manga.Tag`.                       |
| Content language      | Required.                                                                |
| Publication frequency | Optional proposed frequency: `WEEKLY`, `MONTHLY`, `IRREGULAR`, or unset. |
| Cover image           | Optional `SERIES_COVER`; shown as portrait preview when selected.        |

### Edit Draft modal

Rules:

* Available only while series status is `PROPOSAL_DRAFT`.
* Synopsis textarea should use a larger height, currently 6 lines.
* Existing saved publication frequency must pre-populate the dropdown.
* Cover preview appears inline beside upload zone.
* Cover preview is 120×180 portrait.
* Clicking the cover preview opens a larger preview popup.
* Empty synopsis must not be replaced with title fallback.
* Empty synopsis should be rejected before save and by backend validation.

### Cover preview popup

| Element        | Behavior                                                                         |
| -------------- | -------------------------------------------------------------------------------- |
| Overlay/card   | Centered preview modal.                                                          |
| Image          | Large contained preview, max height around 420px.                                |
| Close controls | Header close button, footer Close button, and outside click where implemented.   |
| Scope          | Works for create selected cover, edit current cover, and edit replacement cover. |

---

## 6.x Review Assistant Submissions Page

### Route

```text
/mangaka/review-submissions
```

### Layout

Uses:

```text
<MangakaLayout>
```

### Purpose

Allows a Mangaka to review assistant task submissions and manage assistant task status.

### Main UI elements

| Element                  | Behavior                                                                                                 |
| ------------------------ | -------------------------------------------------------------------------------------------------------- |
| Back button              | Returns to `/mangaka`.                                                                                   |
| Page title               | `Review Assistant Submissions` or equivalent task-review title.                                          |
| Stat cards               | Shows counts by task state.                                                                              |
| Search/filter bar        | Series/chapter/title search, task type filter, assistant filter, status filter.                          |
| Task cards               | Show task and production context.                                                                        |
| Original Page preview    | Shows source page/version preview when available.                                                        |
| Submitted Output preview | Shows completed/submitted version when available.                                                        |
| Workspace links          | Open `/series/{slug}/workspace?chapterId={chapterId}` with safe `returnUrl=/mangaka/review-submissions`. |
| Action buttons           | Approve, Return for Rework, Cancel, Reassign where allowed.                                              |

### Task card fields

Each task card should show available values for:

```text
Series title
Chapter number/title
Page number
Page version number
Assigned assistant
Task type
Task title
Task status
Due date
Priority
Compensation
Region count
Original page preview
Submitted output preview when available
```

### Action visibility rules

| Action            | Visible for                | Result                                                                                                       |
| ----------------- | -------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Approve           | `UNDER_REVIEW`             | Accepts submitted work according to task workflow.                                                           |
| Return for Rework | `UNDER_REVIEW`             | Returns the same task to the same Assistant with status `ASSIGNED`; clears submitted completed page version. |
| Cancel            | Workflow-allowed states    | Cancels the task according to backend rules.                                                                 |
| Reassign          | `ASSIGNED`, `UNDER_REVIEW` | Cancels old task and creates replacement `ASSIGNED` task for a different Assistant.                          |

### Return for Rework dialog

| Field                      | Behavior                                                                                         |
| -------------------------- | ------------------------------------------------------------------------------------------------ |
| Dialog title               | Return for Rework.                                                                               |
| Current assistant          | Display only.                                                                                    |
| Updated task instructions  | Required. Textarea with helper text "Explain what the Assistant should revise before resubmitting." |
| Submit button              | Return for Rework.                                                                               |

Behavior:

```text
Return for Rework keeps the same assigned Assistant.
It does not create a new task.
It updates the same ChapterPageTask row.
It changes the task from UNDER_REVIEW back to ASSIGNED.
It clears completed_page_version_id.
It replaces task_description with the updated instruction.
It reloads the task list after success.
```

### Reassign dialog

| Field                    | Behavior                                                                 |
| ------------------------ | ------------------------------------------------------------------------ |
| Current assignee         | Display only.                                                            |
| New assistant            | Search/select eligible active Assistant contributor for the same series. |
| Reason                   | Required, max 500 characters.                                            |
| Updated task description | Optional if supported.                                                   |

Behavior:

```text
Reassign changes ownership to a different Assistant.
It cancels the old task.
It creates a new replacement task with status ASSIGNED.
It copies task-region links.
It reloads the task list after success because the task id changes.
```

### Post-action refresh

After Approve, Return for Rework, Cancel, or Reassign:

```text
Reload task list
Clear dialog/action state
Re-render page
Keep current filters applied
Refresh stat cards and action buttons
```

### Important distinction

Return for Rework and Reassign must remain separate UI concepts:

| Return for Rework                                | Reassign                                               |
| ------------------------------------------------ | ------------------------------------------------------ |
| Same Assistant                                   | Different Assistant                                    |
| Same task row                                    | New replacement task row                               |
| Only from `UNDER_REVIEW`                         | From `ASSIGNED` or `UNDER_REVIEW`                      |
| Clears submitted completed page version          | Cancels old task and creates replacement               |
| Uses `manga.usp_ChapterPageTask_ReturnForRework` | Uses `manga.usp_ChapterPageTask_AssignToDifferentUser` |

---

## 6.z Quick Select Task Assignment (Backend Only)

### Purpose

Allow a Mangaka to quickly create multiple assigned tasks at once by selecting pages, an assistant, and common task defaults. Each task links to one whole-page `PageRegion`.

### API endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/mangaka/series/{seriesId}/chapters/quick-select` | Load chapters for Quick Select. |
| GET | `/api/mangaka/chapters/{chapterId}/pages/quick-select` | Load pages/current versions for Quick Select. |
| GET | `/api/mangaka/series/{seriesId}/assistants/quick-select` | Load active Assistant contributors for Quick Select. |
| POST | `/api/mangaka/tasks/quick-select` | Create batch of assigned tasks. |

### Backend behavior

- Quick Select creates multiple new ASSIGNED tasks in one batch.
- One task per selected page/current page version.
- User selects pages, not regions.
- Backend creates or reuses one `FULL_PAGE` `PageRegion` per selected page version.
- `FULL_PAGE` dimensions come from Cloudinary via `IImageMetadataProvider`.
- `FileResource` does not store image dimensions.
- Each task links to its `FULL_PAGE` region.
- Application validates the whole batch before persistence.
- Infrastructure persists with EF batch insert and one `SaveChangesAsync`.
- Transaction/app-lock prevents overlapping writes.
- Rollback prevents partial tasks, regions, or audit rows.
- Audit writes one `CHAPTER_PAGE_TASK_CREATED` event per created task.
- No stored procedure is called for this workflow.

### Request DTO

```csharp
QuickSelectTaskAssignmentRequest
- SeriesId
- ChapterId
- AssignedToUserId
- TypeCode
- TaskTitlePrefix
- DefaultTaskDescription
- PriorityLevel (1-5)
- DueAtUtc
- CompensationAmount (>= 0)
- Pages: QuickSelectPageTaskRequest[]
    - ChapterPageId
    - ChapterPageVersionId
    - DescriptionOverride? (optional per-page override)
```

### Response DTO

```csharp
QuickSelectTaskAssignmentResult
- CreatedTaskCount
- CreatedTasks: QuickSelectCreatedTaskDto[]
    - ChapterPageTaskId
    - ChapterPageId
    - ChapterPageVersionId
    - PageNo
```

### UI scope

Quick Select dialog UI is not yet implemented. This session implements backend only.

---

## 6.y Manage Series Contributors Page

### Route

```text
/mangaka/contributors
```

### Layout

Uses:

```text
<MangakaLayout>
```

### Purpose

Allows a Mangaka to view contributor history and manage Assistant contributors for their own series.

### Main UI elements

| Element              | Behavior                                                                                     |
| -------------------- | -------------------------------------------------------------------------------------------- |
| Back button          | Returns to `/mangaka`.                                                                       |
| Title                | `Manage Series Contributors`.                                                                |
| Description          | Explains viewing contributor history, adding assistants, and ending assistant contributions. |
| Stat cards           | My Series, Active Contributors, Active Assistants, Former Contributors.                      |
| Series selector      | Uses the existing Mangaka `my-series` data.                                                  |
| Search box           | Filters contributor table by contributor/series text.                                        |
| Role filter          | All, Mangaka, Assistant, Tantou Editor.                                                      |
| Status filter        | Active, Former, All.                                                                         |
| Contributor table    | Shows series, contributor, role, status, start date, end date, actions.                      |
| Add Assistant button | Opens Add Assistant dialog for selected series.                                              |

### Contributor table behavior

| Contributor type | UI behavior       |
| ---------------- | ----------------- |
| Active Assistant | Shows End action. |
| Former Assistant | Read-only.        |
| Mangaka          | Read-only.        |
| Tantou Editor    | Read-only.        |

### Add Assistant dialog

| Element                 | Behavior                                                                                                      |
| ----------------------- | ------------------------------------------------------------------------------------------------------------- |
| Selected series display | Shows the selected series title.                                                                              |
| Assistant autocomplete  | Searches eligible ACTIVE Assistant users.                                                                     |
| Search behavior         | Searches DisplayName, Username, and Email.                                                                    |
| Eligibility             | Excludes only current active contributors of selected series. Historical ended rows do not block eligibility. |
| Dropdown behavior       | Autocomplete inside dialog should use fixed dropdown positioning to avoid clipping.                           |
| Add button              | Disabled until an assistant is selected.                                                                      |
| Success                 | Adds assistant, closes dialog, refreshes contributor list and eligible search.                                |

### End Assistant dialog

| Element            | Behavior                                                                                         |
| ------------------ | ------------------------------------------------------------------------------------------------ |
| Target display     | Shows assistant and series.                                                                      |
| Reason             | Required, max 500 characters.                                                                    |
| Success            | Sets contributor end date; does not delete row; refreshes list.                                  |
| Blocking condition | If assistant has `ASSIGNED` or `UNDER_REVIEW` tasks on the series, show a safe blocking message. |

### Important implementation note

The End Assistant flow requires `manga.usp_SeriesContributor_EndAssistant` to be applied to the target database before runtime remove/end smoke testing.

---

## 7.x Safe Return URL Behavior

### Purpose

Preserve safe back-navigation when users open series pages or workspaces from different role areas.

### Allowed local prefixes

```text
/mangaka
/assistant
/editor
/board-chief
/board
/admin
/series
/dashboard
```

### Rejected values

```text
external URLs
protocol-relative URLs starting with //
URLs containing backslash
javascript:
data:
/api/
/signout
```

### UI usage

| Source                             | Behavior                                                       |
| ---------------------------------- | -------------------------------------------------------------- |
| Mangaka dashboard series card      | Serialized/allowed series links may pass `returnUrl=/mangaka`. |
| Review Submissions workspace links | Pass `returnUrl=/mangaka/review-submissions`.                  |
| SeriesPage                         | Resolves safe returnUrl; fallback should be `/dashboard`.      |
| Workspace links                    | Preserve source page context when returning.                   |

---

## 8.x Series Detail Contributor Sidebar

### Purpose

Show contributors on `/series/{slug}` without using a slide-out drawer.

### Layout

Use a page-level two-column grid when contributors exist:

```text
Left: series detail card + chapter list
Right: contributor sidebar card
```

### Behavior

| Element                       | Behavior                                             |
| ----------------------------- | ---------------------------------------------------- |
| Contributor sidebar card      | Appears on the right side of the series detail area. |
| View all contributors trigger | Toggles contributor list open/closed.                |
| Chevron                       | ExpandMore when closed, ExpandLess when open.        |
| Contributor rows              | Scrollable when list is tall.                        |
| Empty contributor list        | Hide sidebar card if no contributors exist.          |
| Mobile layout                 | Sidebar stacks below main series content.            |

### Notes

* Do not use `MudDrawer` for this contributor panel.
* Do not show overlay or popup for the contributor list.
* Contributor management is handled separately through `/mangaka/contributors`.

---

## 7. Tantou Editor Proposal Queue

### Purpose

Allow active Tantou Editors to find newly submitted proposals and choose/claim proposals for editorial handling.

### Main UI elements

| Element | Behavior |
|---|---|
| Proposal queue table | Shows submitted proposals with status `UNDER_EDITORIAL_REVIEW`. |
| Unclaimed priority filter | Prioritizes proposals without active Tantou Editor contributors, but does not hide already-claimed proposals. |
| Claim / Join Review button | Adds the Tantou Editor as a `SeriesContributor` when permitted. Multiple Tantou Editors may contribute to the same series. |
| Open Review button | Opens the proposal review detail page. |

### Review actions

| Action | Required input | Result |
|---|---|---|
| Request revision | Non-empty comments; optional markup file | Proposal becomes `REVISION_REQUESTED`; series returns to `PROPOSAL_DRAFT`. |
| Cancel proposal | Non-empty comments and required markup file | Proposal and series become `CANCELLED`. |
| Pass to board | Optional comments/markup depending on workflow | Proposal and series become `UNDER_BOARD_REVIEW`. |

## 8. Authorized Chapter Workspace

### Route

```text
/workspace/chapters/{chapterId}
```

### Purpose

- Provide one central workspace for authorized users to navigate chapter content and use production/review tools.
- The centralized workspace is opened at chapter level for navigation and workflow context, but all page editing, AI segmentation, OCR/translation support, region editing, annotation, and task-region assignment operate on the currently selected ChapterPageVersion.
### Layout

```text
┌──────────────────────┬──────────────────────────────┬────────────────────────┐
│ Left navigation      │ Main page/version canvas       │ Right tools/actions     │
│ Series/chapter tree  │ Image + regions + annotations  │ AI + role actions       │
└──────────────────────┴──────────────────────────────┴────────────────────────┘
```

---

## 7. Workspace Left Navigation Panel

### Responsibility

The left panel provides context and navigation.

### Tree structure

```text
Selected Series
├── Chapter 1
│   ├── Page 1
│   │   ├── Version 1
│   │   └── Version 2 Current
│   ├── Page 2
│   └── Page 3
├── Chapter 2
└── Chapter 3
```

### Required behavior

- Highlight selected chapter.
- Highlight selected page.
- Highlight selected page version.
- Mark current page version clearly.
- Show locked/read-only state when chapter/page status prevents editing.
- Allow authorized navigation between chapters in the selected series.
- Allow navigation between pages in a selected chapter.
- Allow viewing historical page versions when permitted.

---

## 8. Workspace Main Viewing Area

### Responsibility

Display selected content.

### Required behavior

- Show selected `ChapterPageVersion` image/file.
- Render saved `PageRegion` overlays when available.
- Render annotation indicators linked to one or more regions through annotation-region links.
- Allow zoom/pan if feasible for MVP.
- Show empty state if the selected page has no version yet.
- Show read-only state when user lacks edit permission.

---

## 9. Workspace Right Tools / Actions Panel

### Shared AI tools for Authorized Page Workspace Users

| Tool | Availability | Notes |
|---|---|---|
| AI segmentation | All Authorized Page Workspace Users with access to the workspace | Suggestions require human review before saving as `PageRegion`. |
| AI/OCR translation support | All Authorized Page Workspace Users with access to the workspace | Suggestions do not become final page content automatically. |
| Temporary page modification/download | Authorized users when tool is available | Does not create `FileResource` or `ChapterPageVersion` by default. |

### Role-specific actions

| Role | Actions |
|---|---|
| Mangaka | Save/adjust regions when permitted, create production-tracking annotations, update/resolve Mangaka-created annotations, assign selected page regions as tasks to Assistants, review task output, upload new page versions, submit chapter for review. Mangaka cannot update or resolve Tantou Editor-created annotations. Task page context is derived from selected regions, not from a direct task `chapter_page_id`. |
| Assistant | View assigned regions/tasks, upload task output as a page version for the same logical page derived from the linked task regions when allowed, mark work ready for review. |
| Tantou Editor | Add editorial-review annotations linked to one or more page regions, update unresolved annotation text when permitted, resolve Mangaka-created or Tantou Editor-created annotations, review regions/page context, request revision or approve/cancel chapter through chapter review workflow. |
| Editorial Board Member | No workspace access by default. |
| Editorial Board Chief | No workspace access by default unless future permission grants it. |
| Admin | No manga production actions. |

---

## 10. Back Navigation

### Default behavior

When the user enters from the main series page:

```text
/series/{slug}
→ /workspace/chapters/{chapterId}
→ Back
→ /series/{slug}
```

### Contextual return behavior

When the user enters from a role workflow queue:

```text
/editor/chapters/review
→ /workspace/chapters/{chapterId}
→ Back
→ /editor/chapters/review
```

```text
/assistant/tasks
→ /workspace/chapters/{chapterId}
→ Back
→ /assistant/tasks
```

### Implementation recommendation

Use either:

```text
returnUrl=/local/path
```

with local URL validation, or use safer symbolic contexts:

```text
returnContext=series
returnContext=editor-review
returnContext=assistant-task
returnContext=dashboard
```

For MVP, symbolic `returnContext` is safer and easier to avoid open-redirect mistakes.

---

## 11. Permission Summary

| Feature | Mangaka | Assistant | Tantou Editor | Board Member | Board Chief | Admin |
|---|---:|---:|---:|---:|---:|---:|
| Create series draft | Yes | No | No | No | No | No |
| Edit series draft profile | Yes, if contributor and `PROPOSAL_DRAFT` | No | No | No | No | No |
| Auto-regenerate slug on title save | Yes, while draft | No | No | No | No | No |
| Access `/series/{slug}` | Yes | Yes, if relevant | Yes | Yes | Yes | Yes/read-only |
| Open chapter workspace | Yes | Assigned work only | Review work only | No by default | No by default | No production actions |
| Use AI segmentation | Yes | Yes, in accessible workspace | Yes | No by default | No by default | No production actions |
| Use AI/OCR translation support | Yes | Yes, in accessible workspace | Yes | No by default | No by default | No production actions |
| Assign page-region task | Yes | No | No | No | No | No |
| Create annotation | Yes, as production-tracking annotation when active contributor | No | Yes, as editorial-review annotation when active contributor | No | No | No |
| Update annotation text | Mangaka-created unresolved annotations only | No | Mangaka-created or Tantou Editor-created unresolved annotations | No | No | No |
| Resolve annotation | Mangaka-created annotations only | No | Mangaka-created or Tantou Editor-created annotations | No | No | No |
| Submit chapter for review | Yes | No | No | No | No | No |
| Final chapter review decision | No | No | Yes | No | No | No |

---

## 12. MVP Non-Goals for This UI

- No public reader account workflow yet.
- No slug history or redirect table.
- No full drawing/editor application.
- No automatic AI approval of pages, chapters, proposals, or board decisions.
- No Admin ownership of manga production content.
- No page-only isolated workspace as the main design.

---

## 13. Acceptance Checklist

- Mangaka can create and edit draft series only while `PROPOSAL_DRAFT`.
- Backend generates slug on create and on title change during draft.
- Slug is locked after the series leaves `PROPOSAL_DRAFT`.
- `/series/{slug}` works as main series URL after serialization.
- Chapter workspace route uses internal chapter/page/version IDs.
- Workspace has left navigation for chapters/pages/versions.
- Workspace has right tools/actions panel.
- AI tools are available to all Authorized Page Workspace Users with access.
- Role-specific actions remain permission-gated.
- Mangaka can create production-tracking annotations and update/resolve Mangaka-created annotations.
- Mangaka cannot update or resolve Tantou Editor-created annotations.
- Tantou Editors can create editorial-review annotations and update/resolve both Mangaka-created and Tantou Editor-created annotations when they are active contributors for the series.
- Back navigation returns to `/series/{slug}` by default or to the original workflow context when provided.
