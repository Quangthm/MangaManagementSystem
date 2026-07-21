# MangaManagementSystem UI Specification — Series Draft, Slug, and Authorized Chapter Workspace

**Project:** Manga Creation Workflow and Publishing Management System  
**Target UI:** Blazor Server / MudBlazor MVP  
**Last updated:** 2026-07-21

**Latest lifecycle update:** `HIATUS` is the paused-series status. Active Mangaka or Tantou Editor contributors may pause/resume serialized series. Only active Mangaka contributors may mark serialized/hiatus series as `COMPLETED`. `HIATUS` blocks release only; `COMPLETED` makes the series read-only/immutable for normal business actions, cancels unreleased chapters and distinct active `ASSIGNED`/`UNDER_REVIEW` tasks under those chapters after warning and confirmation, and remains ranking-visible.  

**Latest ranking/notification update — 2026-07-21:** Existing Bell behavior for proposal/chapter/task notifications, board decisions, publication scheduling, and account approval remains unchanged. Ranking uses the weighted score `(v / (v + m)) * R + (m / (v + m)) * C`; `reading_count` remains popularity evidence rather than a direct score boost. `RANKING_WARNING` uses the finalized hybrid weekly rule: both `ranking_score < 6.5` and bottom-25% rank must fail in the same completed week, with failure in at least 2 of the latest 3 consecutive completed weeks including the latest. All distinct active contributors of the exact affected series receive the warning.  

**Latest chapter submission validation — 2026-07-21:** The chapter workspace must surface the backend-authoritative submission gate: a Mangaka may submit only from `DRAFT` or `REVISION_REQUESTED` when zero distinct associated page tasks remain `ASSIGNED` or `UNDER_REVIEW`. If blocked, keep the chapter unchanged and show the clean backend message directing the Mangaka to complete or cancel active tasks; do not auto-cancel tasks or show a successful submission state.
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
| Series slug presentation | `/series/{slug}` uses an independent responsive cover column beside a separate metadata card; Genres and Tags use a 6-item preview with `Show all (N)` / `Show less` instead of arrow/page-number pagination. |
| Series profile editing | Normal Mangaka edits are allowed only while `status_code = PROPOSAL_DRAFT`. |
| Series hiatus | Active Mangaka or Tantou Editor contributors may set `SERIALIZED` series to `HIATUS` and resume `HIATUS` series to `SERIALIZED`; hiatus blocks release only. |
| Series completion | Only active Mangaka contributors may mark a `SERIALIZED` or `HIATUS` series as `COMPLETED`; completion requires warning/confirmation, shows affected unreleased chapters and distinct active-task impact, cancels both within the completion cascade, and makes the series read-only. |
| Publication frequency | Mangaka may set `publication_frequency_code` during draft as a proposed/preferred frequency; board serialization workflow may override it later. |
| Workspace level | The centralized authorized workspace is chapter-level, not page-level. |
| Workspace AI tools | AI segmentation and AI/OCR translation tools are available to all Authorized Page Workspace Users who have access to the relevant workspace. |
| Workspace permissions | AI tools are shared; business actions remain role-specific and permission-gated. |
| Chapter submission gate | Mangaka chapter submission is backend-authoritative and allowed only from `DRAFT`/`REVISION_REQUESTED` when zero distinct associated tasks are `ASSIGNED` or `UNDER_REVIEW`; blocked attempts must show a clean error and must not change chapter/task state. |
| Publication schedule page | `/publication/schedule` is a shared weekly calendar. Read-only roles load only scheduled/released schedule data; scheduling actors may open a role-gated action drawer. |
| Publication action drawer | Mangaka and Tantou Editor scheduling actions use a restricted actionable chapter feed, full-card drag/drop, and confirmation dialogs. Assistant, Board, Admin, and future public viewers must not load unscheduled/actionable drawer data. |

---

## 2. Main Routes

| Route | Purpose | Access |
|---|---|---|
| `/mangaka/series/drafts` | Mangaka draft list and draft management entry point. | Mangaka |
| `/mangaka/series/drafts/{seriesId}` | Optional full draft detail/edit page if modal is not enough. | Mangaka contributor |
| `/editor/proposals` | Editorial proposal queue, prioritizing unclaimed submitted proposals. | Tantou Editor |
| `/editor/proposals/{proposalId}` | Proposal review detail page for request revision, cancel, or pass to board. | Tantou Editor contributor / authorized Tantou Editor |
| `/series/{slug}` | Stable main series page after serialization; future public reader URL can reuse it. | Authorized now; public in future |
| `/publication/schedule` | Shared weekly publication calendar with optional series slug filter and role-gated scheduling action drawer. | Authorized users for schedule viewing; Mangaka/Tantou Editor actions only |
| `/workspace/chapters/{chapterId}` | Central authorized chapter workspace. | Authorized Page Workspace User |
| `/workspace/chapters/{chapterId}?page={pageNumber}` | Workspace opened with selected page. | Authorized Page Workspace User |
| `/workspace/chapters/{chapterId}?page={pageNumber}&version={versionId}` | Workspace opened with selected page version. | Authorized Page Workspace User |

### Route principles

- Use `series_id` for draft management and internal workflow operations.
- Use slug mainly for the stable main series page.
- Use `chapter_id`, `chapter_page_id`, and `chapter_page_version_id` for workspace editing context.
- Do not use slug as the primary identifier for page/version editing.
- Use `?series={slug}` as the preferred publication schedule series filter URL identity; keep legacy `?seriesId={guid}` support only for backward-compatible navigation.
- Use `anchorDate=yyyy-MM-dd` for publication schedule week navigation; the selected week is derived from the anchor date.

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
| Header | Current cover, title, status badge, genres, tags, language, publication frequency. The `SERIALIZED` status badge must display **Serialized**, not **Active**. |
| Synopsis panel | Current series synopsis. |
| Chapter list | Chapters under the series with status and planned/released dates. |
| Role action panel | Buttons shown based on current user role and permission. |

### Series header layout

The `/series/{slug}` page uses an independent cover-and-metadata layout.

```text
Series header area
├── Independent cover column
│   └── Series cover or portrait placeholder
│
└── Series metadata card
    ├── Title
    ├── Status / language / publication frequency
    ├── Genres
    ├── Tags
    ├── Active Mangaka display
    ├── Synopsis
    ├── Workspace action
    └── Role-gated lifecycle actions
```

#### Cover behavior

- The cover is visually separate from the white metadata card.
- On wider screens, the cover appears to the left of the metadata card.
- The cover column grows responsively when horizontal space allows, while preserving sufficient width for metadata.
- The real cover image preserves the full stored artwork using responsive width and natural image height; it must not stretch to match the metadata-card height.
- Cover height is independent from Genre/Tag expansion, synopsis length, lifecycle actions, and other metadata-card content.
- Real cover images must not use dynamic full-card-height cropping.
- When no cover exists, show a stable portrait placeholder with the existing neutral background and centered book icon.
- On narrow screens, the cover stacks above the metadata card.
- The cover and metadata card remain siblings in the page layout; the cover must not be placed back inside the metadata card.

### Genre and Tag display behavior

Genres and Tags are displayed as compact pill/chip metadata inside the series metadata card.

| State | Behavior |
|---|---|
| No items | Do not render the corresponding Genres or Tags section. |
| 1–6 items | Show all available chips and no expand control. |
| More than 6 items, collapsed | Show the first 6 chips and a `Show all (N)` action. |
| Expanded | Show all chips and replace the expand action with `Show less`. |
| Show less | Return the corresponding section to its 6-item preview state. |

Additional rules:

- Genres and Tags have independent expand/collapse state.
- Expanding Genres must not change the Tags state.
- Expanding Tags must not change the Genres state.
- Do not show previous/next arrows, page numbers, or pagination indicators for Genres or Tags.
- Genre and Tag chips should preserve the established pill styling and wrap normally within the metadata card.
- Genre/Tag expansion is client-side presentation behavior only and must not trigger additional API requests.

### Role-based actions

| Role | Possible actions |
|---|---|
| Mangaka contributor | Add chapter, manage chapters, open workspace, view ranking context, set/resume hiatus, and mark eligible own series as completed. |
| Assistant | Open assigned task or workspace context only for assigned work. |
| Tantou Editor | Open review workspace, review chapter, view annotations, set/resume hiatus for contributed series. |
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
  * The user-facing label for `SERIALIZED` is **Serialized**; do not display **Active** as the lifecycle status label.
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

Quick Select dialog is implemented on `/mangaka/review-submissions`. A "Quick Select" button opens a dialog with progressive sections:
1. Select Series (from existing Mangaka series)
2. Select Chapter (loads chapters for selected series)
3. Select multiple Pages (multi-select checkboxes with Select All/Clear)
4. Select Assistant (loads ACTIVE Assistant contributors for selected series)
5. Task Details (type, title prefix, priority, due date, compensation)
6. Default description + optional per-page overrides
7. Submit — creates batch of ASSIGNED tasks

Backend endpoints used:
- `GET /api/mangaka/series/my-series` (existing, for series selector)
- `GET /api/mangaka/series/{seriesId}/chapters/quick-select`
- `GET /api/mangaka/chapters/{chapterId}/pages/quick-select`
- `GET /api/mangaka/series/{seriesId}/assistants/quick-select`
- `POST /api/mangaka/tasks/quick-select`

Typed API client methods added to `IMangakaTaskApiClient` / `MangakaTaskApiClient`.

On success: dialog closes, snackbar shows `"Created N task(s)."`, task list and stat cards refresh via existing `RefreshTasksAfterMutationAsync()`.

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


## 6A. Series Lifecycle Actions

### Purpose

Define the UI behavior for changing a serialized series to `HIATUS`, resuming it back to `SERIALIZED`, and marking it as `COMPLETED`.

### Status meanings

| Series status | UI meaning |
|---|---|
| `SERIALIZED` | Display label: **Serialized**. Meaning: active production/publication series. |
| `HIATUS` | Paused series. Production and scheduling may continue, but chapter release is blocked until the series returns to `SERIALIZED`. |
| `COMPLETED` | Author-ended final series. The series and related production records are read-only for normal business actions. |
| `CANCELLED` | Board/business-cancelled final series. |

### Hiatus actions

| Action | Actor | Availability | UI behavior |
|---|---|---|---|
| Set to Hiatus | Active Mangaka contributor or active Tantou Editor contributor | Series status is `SERIALIZED` | Show confirmation and optional/required reason according to implementation. On success, show `HIATUS` badge and refresh actions. |
| Resume Serialization | Active Mangaka contributor or active Tantou Editor contributor | Series status is `HIATUS` | Show confirmation and optional/required reason according to implementation. On success, show `SERIALIZED` badge and refresh actions. |

Hiatus UI rules:

- `HIATUS` must be shown as a series-level status badge, not as a chapter status.
- While a series is `HIATUS`, the UI may still show chapter creation, drafting, review, scheduling, and rescheduling actions when the normal chapter status and role permissions allow those actions.
- While a series is `HIATUS`, release actions must be hidden or disabled with helper text explaining that the series must be resumed to `SERIALIZED` first.
- Setting a series to `HIATUS` must not automatically change scheduled chapter badges to `ON_HOLD`.

### Completion action

| Action | Actor | Availability | UI behavior |
|---|---|---|---|
| Mark as Completed | Active Mangaka contributor only | Series status is `SERIALIZED` or `HIATUS` | Show a strong confirmation dialog with the affected unreleased chapter list/count and the count of distinct active tasks that will be cancelled. On success, show `COMPLETED` badge and read-only state. |

Completion confirmation dialog should explain the current backend-provided impact, for example:

```text
This will mark the series as completed.

Affected unreleased chapters: X
Active tasks that will be cancelled: Y

Unreleased DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, and ON_HOLD chapters will be cancelled.
Distinct ASSIGNED and UNDER_REVIEW tasks under those affected chapters will also be cancelled.
Released chapters, completed/cancelled tasks, and tasks under unaffected chapters will be preserved.
This action cannot be undone through normal workflow.
```

The preview is advisory. The backend must revalidate and recalculate authoritative completion impact when the user confirms.

Completion UI rules:

- `COMPLETED` series pages remain viewable to authorized users.
- `COMPLETED` series should hide or disable normal business mutation actions, including edit series details/status, add chapter, edit chapter/page content, upload page versions, edit regions, create/update tasks, review, schedule, reschedule, hold, and release.
- Released chapters remain visible as released.
- Completion-cancelled chapters remain visible as cancelled/read-only historical records.
- `ASSIGNED` and `UNDER_REVIEW` tasks under completion-cancelled chapters become `CANCELLED` and remain visible as historical task records where authorized.
- `COMPLETED` and already `CANCELLED` tasks remain unchanged, and tasks under unaffected chapters remain unchanged.
- Completed series remain visible in the ranking UI when ranking input exists.

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
Left:
  series header area
  ├── independent cover
  └── series metadata card
  +
  chapter list

Right:
  contributor sidebar card
```

The cover is part of the left-side series header area but remains visually separate from the metadata card.

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
- Hide soft-deleted `ChapterPage` records from normal active page navigation by default, while allowing historical/admin views to include them when needed.
- Do not provide a normal user action to delete a saved `ChapterPageVersion` in the current MVP.

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
- Show page-region delete/remove action only for regions that are not linked to annotations, tasks, or other workflow records.
- If a selected region is linked to an annotation or task, explain that it cannot be deleted because it is part of workflow history.

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
| Mangaka | Save/adjust regions when permitted, delete only unused regions that are not linked to tasks or annotations, create production-tracking annotations, update/resolve Mangaka-created annotations, assign selected page regions as tasks to Assistants, review task output, explicitly save new page versions, soft-delete chapter pages when permitted, and submit chapters for review. Mangaka cannot delete saved page versions in the current MVP and cannot update or resolve Tantou Editor-created annotations. Task page context is derived from selected regions, not from a direct task `chapter_page_id`. |
| Assistant | View assigned regions/tasks, upload task output as a page version for the same logical page derived from the linked task regions when allowed, mark work ready for review. |
| Tantou Editor | Add editorial-review annotations linked to one or more page regions, update unresolved annotation text when permitted, resolve Mangaka-created or Tantou Editor-created annotations, review regions/page context, approve chapters, request revision with required comments, or cancel chapters with required comments and optional markup through the chapter review workflow. |
| Editorial Board Member | No workspace access by default. |
| Editorial Board Chief | No workspace access by default unless future permission grants it. |
| Admin | No manga production actions. |

### Chapter submission validation UI behavior

The **Submit Chapter for Review** action is available only within the existing Mangaka chapter workflow, but the backend remains authoritative for final eligibility.

| Condition | UI / system behavior |
|---|---|
| Chapter is `DRAFT` or `REVISION_REQUESTED` and zero distinct associated tasks are `ASSIGNED`/`UNDER_REVIEW` | Allow the existing submit action. On success, refresh the chapter as `UNDER_REVIEW`. |
| One or more associated tasks are `ASSIGNED` or `UNDER_REVIEW` | Backend blocks submission. Keep the current chapter status and show the clean validation response. |
| Associated tasks are only `COMPLETED` and/or `CANCELLED` | These tasks do not block submission; continue with the normal remaining validations. |
| UI already has active-task information loaded | The UI may disable or warn proactively for convenience, but it must still call/rely on backend validation as the authoritative rule. |
| Backend rejects submission | Do not show success state, do not navigate as though submission succeeded, and do not mutate task state locally. |

Recommended blocked-submission message:

```text
This chapter cannot be submitted for editorial review while active page tasks are still assigned or under review. Complete or cancel those tasks before submitting the chapter.
```

Additional UI rules:

- Do not add previous/parallel client-side logic that attempts to derive the authoritative chapter/task relationship independently from the backend.
- Do not automatically complete, cancel, reassign, or delete tasks when submission is blocked.
- A blocked attempt must not display a successful `UNDER_REVIEW` state or imply that Tantou Editors were notified.
- `CHAPTER_REVIEW` is created only after the backend successfully transitions the chapter to `UNDER_REVIEW`.
- If the submit API returns the approved user-safe business validation message, surface that message cleanly to the Mangaka.

### Page, version, and region retention UI behavior

| UI action | MVP behavior |
|---|---|
| Select/upload page file | Preview only until the user explicitly clicks Save/Confirm; it must not create a `ChapterPageVersion` automatically. |
| Save page version | Creates a new `ChapterPageVersion`, marks it current when appropriate, and keeps previous versions in history. |
| Delete saved page version | Not available to normal users in the current MVP. Users should save a newer version instead. |
| Delete page region | Available only when the region is not linked to any task, annotation, or other workflow record. |
| Delete linked page region | Blocked; show an explanation that linked regions are preserved for workflow traceability. |
| Soft-delete chapter page | Allowed only when chapter/page workflow rules permit; hides the logical page from active draft navigation but preserves page-version history. |
| Admin/system purge of old page versions | Future scope after chapter release; not part of normal MVP workspace actions. |

### Chapter editorial decision UI

| Decision action | Required input | Result | UI meaning |
|---|---|---|---|
| Approve Chapter with no planned release date | None required | `Chapter.status_code = APPROVED` | Chapter is accepted but still needs a planned release date before it becomes scheduled or released. |
| Approve Chapter with an existing future planned release date | None required | `Chapter.status_code = SCHEDULED` | Chapter is accepted, scheduled, and locked from Mangaka/page content mutation workflows. |
| Request Revision | Non-blank comments; optional markup file | `Chapter.status_code = REVISION_REQUESTED` | Same chapter becomes editable again and can receive new page versions. |
| Cancel Chapter | Non-blank comments; optional markup file | `Chapter.status_code = CANCELLED` | Current chapter attempt is terminal/read-only and cannot be edited or resubmitted. |

### Scheduled and on-hold chapter UI behavior

| Actor / status | UI behavior |
|---|---|
| Mangaka viewing `SCHEDULED` chapter | Show read-only state and message: `This chapter is scheduled. Content changes are locked. Schedule changes are audit-visible.` |
| Mangaka viewing `ON_HOLD` chapter | Show read-only/on-hold state and hold reason when available. Explain that returning to schedule requires a new planned release date. |
| Mangaka scheduling/rescheduling an allowed chapter | Allow selecting any future planned release date; show frequency-based suggestions/warnings but do not hard-block mismatches. Require confirmation. |
| Tantou Editor viewing `SCHEDULED` chapter | Show planned release date, advisory frequency information, Reschedule action, Put On Hold action, and Release action when eligible. |
| Tantou Editor scheduling/rescheduling an allowed chapter | Allow selecting any future planned release date; show frequency-based suggestions/warnings but do not hard-block mismatches. Require confirmation. |
| Tantou Editor putting `SCHEDULED` chapter on hold | Require a non-blank operational/editorial reason and confirmation. |
| Tantou Editor returning `ON_HOLD` chapter to schedule | Require a new future planned release date and confirmation. |
| Tantou Editor releasing a chapter | Require confirmation; set `released_at_utc` to the current UTC time and preserve existing `planned_release_date` unless it is missing. |
| Any user viewing `SCHEDULED` or `ON_HOLD` workspace | Keep read-only viewing available where authorized, but hide/disable page/content mutation actions. |

### Cancelled chapter UI behavior

- Cancelled chapters should show a clear read-only state.
- Cancelled chapters must not show normal edit, page upload, resubmit, approve, schedule, or release actions.
- Mangaka users may be offered a **Create Replacement Draft** action for a cancelled chapter.
- A replacement draft creates a new `Chapter` row with the same chapter number label under the same series.
- The cancelled chapter remains visible as historical reference; no `replacement_of_chapter_id` relationship is required in MVP.

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
| Submit chapter for review | Yes, if active Mangaka contributor, chapter is `DRAFT`/`REVISION_REQUESTED`, and zero distinct associated tasks are `ASSIGNED`/`UNDER_REVIEW` | No | No | No | No | No |
| Create replacement draft for cancelled chapter | Yes, if contributor | No | No | No | No | No |
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
- `/series/{slug}` displays the series cover independently from the metadata card; metadata height must not stretch or crop the cover.
- Genres and Tags on `/series/{slug}` use a 6-item preview with `Show all (N)` / `Show less`, with independent expand state and no arrow/page-number pagination.
- Chapter workspace route uses internal chapter/page/version IDs.
- Workspace has left navigation for chapters/pages/versions.
- Workspace has right tools/actions panel.
- AI tools are available to all Authorized Page Workspace Users with access.
- Role-specific actions remain permission-gated.
- Mangaka chapter submission is blocked when any distinct associated page task is `ASSIGNED` or `UNDER_REVIEW`; `COMPLETED` and `CANCELLED` tasks do not block.
- A blocked chapter submission leaves chapter/task state unchanged and shows a clean user-facing validation message instead of a success state.
- Successful chapter submission still transitions the chapter to `UNDER_REVIEW` and triggers the existing successful audit/notification behavior.
- Mangaka can create production-tracking annotations and update/resolve Mangaka-created annotations.
- Mangaka cannot update or resolve Tantou Editor-created annotations.
- Tantou Editors can create editorial-review annotations and update/resolve both Mangaka-created and Tantou Editor-created annotations when they are active contributors for the series.
- Back navigation returns to `/series/{slug}` by default or to the original workflow context when provided.
- Chapter revision and cancellation decisions require non-blank comments, while markup files are optional.
- Cancelled chapter screens are read-only and do not allow edit, upload, resubmit, schedule, or release actions.
- Mangaka contributors can create a replacement draft for a cancelled chapter using the same chapter number label.

---

## 9. Publication Scheduling UI

### Purpose

Support a shared weekly publication calendar for planned and released chapters, plus role-gated scheduling actions for users who are allowed to modify chapter release plans.

Scheduling is chapter-level. The UI must use `Chapter.planned_release_date`, `Chapter.released_at_utc`, and `Chapter.status_code`; it must not present `SCHEDULED` as a series status.

`Series.publication_frequency_code` is advisory. It may provide suggested dates and warning messages, but it must not hard-block authorized users from choosing a different future date.

### Current route and URL behavior

| Route / query | Behavior |
|---|---|
| `/publication/schedule` | Main shared publication calendar page. |
| `?anchorDate=yyyy-MM-dd` | Selects the week to display. Week navigation updates this value. |
| `?series={slug}` | Preferred series filter identity for the schedule page. |
| `?seriesId={guid}` | Legacy series filter support only. When resolvable, the UI should upgrade navigation/state to slug identity. |

URL and state rules:

- The schedule page should resolve a selected series slug to the internal `series_id` for API filtering.
- The UI may keep `series_id` internally, but public/shared navigation should prefer `series` slug.
- If an incoming slug or legacy `seriesId` cannot be resolved, the UI should clear the selected series state, show a safe warning, and load the calendar unfiltered.
- Clearing the series filter must clear both the visible autocomplete value and the URL query value.

### Data feeds and authorization boundaries

The schedule page has two different data feeds and they must not be mixed.

| Feed | Loaded for | Contains | Notes |
|---|---|---|---|
| Calendar schedule feed | Authorized schedule viewers | Chapters that are scheduled or released for the selected week/filter from `SERIALIZED`, `HIATUS`, or historically relevant `COMPLETED` series. | Safe for read-only schedule viewing. Completed series are read-only. |
| Actionable chapter feed | Mangaka and Tantou Editor only | Role-authorized chapters that can be scheduled/rescheduled/held/released or used as drag payloads. | Must not be loaded for Assistant, Board Member, Board Chief, Admin read-only views, or future public viewers. |

Loading rules:

- Always load the calendar schedule feed for users who may view the schedule page.
- Load the actionable chapter feed only when the current actor can use schedule actions.
- Prefer lazy-loading the actionable feed when the scheduling action drawer opens, then refresh it after successful schedule/hold/release actions.
- The drawer must not query or display unscheduled/actionable chapters for roles that cannot schedule.
- Frontend gating is a performance and UX guard only; backend endpoints must still enforce actor authorization.

### Actor behavior

| Actor | Calendar access | Action drawer | Allowed behavior |
|---|---|---|---|
| Mangaka | Yes, authorized schedule view. | Yes, when authenticated as allowed Mangaka actor. | May set or reschedule a planned release date for allowed chapters, including under `HIATUS` series. Cannot release chapters and cannot mutate page/content when a chapter is `SCHEDULED` or `ON_HOLD`. |
| Tantou Editor | Yes. | Yes. | May set/reschedule a planned date, put `SCHEDULED` chapters on hold, return held chapters to schedule by selecting a new planned date, and release eligible chapters only when the parent series is `SERIALIZED`. |
| Assistant | Read-only schedule view if route access is granted. | No. | Must not load actionable/unscheduled drawer data. |
| Editorial Board Member / Chief | Read-only schedule view if route access is granted. | No. | Board workflows control series-level/frequency decisions elsewhere, not chapter scheduling through this drawer. |
| Admin | Read-only/system oversight only if route access is granted. | No. | No normal manga production actions. |
| Future public viewer | Public schedule view only if enabled later. | No. | Must load only public-safe schedule data. |

### Calendar layout behavior

- The calendar displays one week at a time as day columns.
- The day grid must render even when the selected week has no scheduled/released chapter cards.
- Empty weeks may show an informational empty-state alert, but that alert must not replace the day grid.
- Empty day columns should show a safe empty label such as `No releases`.
- When a scheduling placement is active, empty day columns may show placement helper text such as `Click to place here`.
- Week navigation through previous/next buttons remains available by click.
- Hover-over-arrow week navigation while dragging is not implemented in the current MVP and should be treated as deferred.

### Calendar card placement behavior

- The shared publication calendar should show schedule/release cards for `SERIALIZED` and `HIATUS` series, and may show historical released cards for `COMPLETED` series; mutation actions remain status- and permission-gated.
- A chapter card is placed on its release business date when `released_at_utc` exists.
- If `released_at_utc` is missing, the card is placed on `planned_release_date`.
- `released_at_utc` must be converted to Vietnam publication time (UTC+7), then date-only, before business-date placement.
- Chapter cards should prioritize series cover, series title, chapter number label, and status badge.
- Missing covers should use a safe placeholder.
- Calendar cards are draggable only when the current actor has an authorized actionable payload for that chapter.
- Calendar-card drag must not be enabled for read-only roles.

### Search and filter behavior

| Element | Behavior |
|---|---|
| Series filter | Uses series-title autocomplete/typeahead and stores the selected internal `series_id`. |
| URL series filter | Uses slug as preferred external URL identity through `?series={slug}`. |
| Legacy series filter | Supports `?seriesId={guid}` only as backward-compatible input. |
| Clear filter | Clears selected series, visible autocomplete text/chip, and URL query state. |
| Frequency filter/badges | Allowed for display/filtering, but frequency remains advisory. |
| Chapter-title search | Not part of the shared calendar filter; the calendar search should remain series-focused. |

### Action drawer behavior

The scheduling action drawer is a role-gated UI for Mangaka and Tantou Editor scheduling actions.

| Element | Behavior |
|---|---|
| Drawer visibility | Render/open only for actors who can use schedule actions. |
| Data source | Uses the restricted actionable chapter feed for the current actor/role. |
| Unauthorized roles | Do not render the drawer and do not load actionable chapter data. |
| Drawer chapter cards | Full card is draggable when scheduling is allowed for that chapter. |
| Custom drag preview | JavaScript may create a small card-style drag preview for browser drag image only. It must not contain business logic. |
| Schedule/Reschedule action | Opens confirmation/date dialog before saving. |
| Put On Hold action | Tantou Editor only; requires non-blank reason and confirmation. |
| Release action | Tantou Editor only; requires confirmation. |
| Refresh after mutation | Reload the weekly calendar data and refresh actionable drawer data so status/action buttons stay consistent. |

### Drag/drop scheduling behavior

- Drag/drop is used only to set or change a chapter planned release date.
- Dragging from the action drawer to a calendar day is allowed for authorized actionable chapters.
- Dragging an existing calendar card is allowed only when the actor has an authorized actionable payload for that chapter.
- Dropping onto a future date opens or completes the same scheduling confirmation flow used by normal schedule/reschedule actions.
- Dropping onto past dates must be blocked or rejected with a clear error.
- There is no unschedule-by-drag behavior in the MVP.
- The drawer is not a drop target for clearing planned dates.
- Changing week by hovering over arrows while dragging is deferred and should not be documented as implemented.
- Pending placement state may be used as a UI safety net after a drag starts; it should be cleared after successful scheduling or explicit cancellation.

### Advisory scheduling behavior

| Frequency | UI suggestion behavior |
|---|---|
| `WEEKLY` | Suggest the same weekday in the next week when a useful reference date exists. |
| `MONTHLY` | Suggest the same day number in the next month when possible, otherwise the last valid day of the next month. |
| `IRREGULAR` | Do not require a strict default; user chooses a future date. |
| `NULL` | Show that the official release approach has not been decided; user chooses a future date. |

### Hard validation behavior

| Case | UI/backend behavior |
|---|---|
| User chooses a past planned release date | Block with a clear error. |
| User chooses a date that does not match frequency suggestion | Show a warning, then allow the authorized user to continue. |
| Multiple chapters use the same planned release date | Allow; bulk/catch-up release plans may be intentional. |
| Chapter is `CANCELLED` or `RELEASED` | Hide or disable schedule/reschedule/hold/release actions that are no longer valid. |
| Parent series is `HIATUS` | Allow schedule/reschedule when normal chapter rules allow it, but hide/disable release until the series is resumed to `SERIALIZED`. |
| Parent series is `COMPLETED` or `CANCELLED` | Hide/disable all normal scheduling, hold, release, and mutation actions. |
| Actor lacks permission | Hide action where possible and rely on backend permission checks. |

### Status behavior

| Current chapter state/action | Result |
|---|---|
| `DRAFT` or `REVISION_REQUESTED` + future planned date set | Planned date is saved; chapter remains editable/plannable in its current status. |
| `UNDER_REVIEW` + editor approves + no planned date | Chapter becomes `APPROVED`. |
| `UNDER_REVIEW` + editor approves + future planned date exists | Chapter becomes `SCHEDULED`. |
| `APPROVED` + future planned date set | Chapter becomes `SCHEDULED`. |
| `SCHEDULED` + Mangaka/Editor reschedules | Chapter remains `SCHEDULED`; planned date is updated. |
| `SCHEDULED` + Editor puts on hold | Chapter becomes `ON_HOLD`; the previous release plan is suspended and audit details preserve the old date/reason. |
| `ON_HOLD` + Editor returns to schedule | Requires a new future planned release date; chapter becomes `SCHEDULED`. |
| `APPROVED` or `SCHEDULED` + Editor releases while parent series is `SERIALIZED` | Chapter becomes `RELEASED`; `released_at_utc` is set to the current UTC time. |
| `RELEASED` or `CANCELLED` chapter, or parent series `HIATUS`/`COMPLETED`/`CANCELLED` for release action | Blocked. |

### Confirmation behavior

The UI should ask for confirmation before:

- setting or changing a planned release date,
- putting a chapter on hold,
- returning a held chapter to schedule,
- releasing a chapter,
- bulk scheduling, bulk holding, or bulk releasing chapters when those workflows are implemented.

### Release behavior

- Releasing a chapter sets `released_at_utc` to the current UTC timestamp.
- If `planned_release_date` is missing, the UI/backend sets it to the current publication business date.
- If `planned_release_date` already exists, preserve it for planned-versus-actual comparison.
- Release automation is not part of MVP unless explicitly implemented later.

### Business date note

- Scheduled chapter period membership uses `Chapter.planned_release_date`.
- Released chapter period reports use `released_at_utc` converted to Vietnam publication time (UTC+7), then the date part.
- The UI must not show raw UTC date as the publication business date when period membership matters.

### Scheduled and on-hold lock display

- A `SCHEDULED` chapter should show a clear locked state for Mangaka users.
- An `ON_HOLD` chapter should show a clear paused/on-hold state with reason when available.
- The workspace may remain viewable if the user is authorized, but saved page/content mutation controls should be hidden or disabled.
- On-hold recovery requires a new future planned release date.
- Automatic overdue-to-on-hold behavior is deferred. The UI may show overdue warnings, but it should not trigger writes from read-only page loading.

### Current non-goals / deferred items

- No public reader release automation.
- No hover-over-arrow week navigation while dragging.
- No unschedule-by-drag or drawer-drop unscheduling behavior.
- No bulk scheduling/holding/releasing unless implemented later.
- No loading of actionable/unscheduled chapter data for read-only schedule roles.


## 9A. Notification Bell and Approved Notification Behavior

### General behavior

- The shared Notification Bell displays unread in-app notifications for authenticated users in the layouts available to their role.
- Opening or marking a notification as read records `read_at_utc` and refreshes the unread badge.
- Notifications are awareness/navigation aids and are not the authoritative audit trail.
- When a specific notification type has a dedicated destination, the Bell should reuse that destination rather than inventing a parallel workflow page.
- When recipient rules are series-contributor-scoped, only distinct active contributors of the exact affected series are eligible.

### Approved notification UI mapping

| Type | User-facing behavior / destination |
|---|---|
| `PROPOSAL_REVIEW` | Active Tantou Editor contributors of the exact series receive the submitted proposal notification. Bell opens `/editor/proposals/{seriesProposalId}`. |
| `PROPOSAL_DECISION` | Active Mangaka contributors of the affected series receive decision-specific content for Request Revision, Pass To Board, or Cancel Proposal. Bell opens `/mangaka/proposals/{proposalId}`. |
| `BOARD_POLL` | Active Editorial Board Members receive a new-poll notification; the initiating Chief is excluded. Bell reuses the existing board-poll workflow destination. |
| `BOARD_DECISION` | All active contributors of the exact series receive outcome-specific content when the poll closes with approved/rejected/no-decision or when the Chief manually cancels it. Bell opens the existing Board Decision notification detail route when available. |
| `TASK_ASSIGNMENT` | Assigned Assistant receives new assignment. On reassignment, the original Assistant sees a `Task Reassigned` notice with the reason linked to the original task; the replacement Assistant sees the replacement assignment. Bell opens `/assistant/task/{taskId}` for the related task. |
| `TASK_REVIEW` | Active Mangaka contributors of the exact series are notified when Assistant work enters `UNDER_REVIEW`. Bell opens `/mangaka/review-submissions`. |
| `CHAPTER_REVIEW` | Active Tantou Editor contributors of the exact series are notified when a chapter enters `UNDER_REVIEW`. Bell opens `/editor/chapters`. |
| `CHAPTER_DECISION` | Active Mangaka contributors of the affected series receive Approved / Revision Requested / Cancelled decision content. Bell opens `/mangaka/chapters`. |
| `PUBLICATION_SCHEDULE` | All other active contributors of the exact series, excluding the initiating actor, are notified when a chapter enters `SCHEDULED` or an already scheduled chapter moves to a different normalized planned date. The Bell should open the relevant chapter/publication-schedule context using the existing scheduling navigation pattern; no new parallel scheduling page is required. |
| `ACCOUNT_APPROVED` | The approved user receives the in-app approval notification; the approval workflow also sends a separate email to the user's registered email address. |
| `RANKING_WARNING` | All distinct active contributors of the exact affected `SERIALIZED` or `HIATUS` series receive the warning when the hybrid ranking-risk rule is met. Bell opens the existing ranking/series context. The warning should identify the latest evaluated week, current weighted score, rank/total, and that the series failed both the `< 6.5` score baseline and bottom-25% check; it is advisory and must not imply automatic cancellation. |
| `SYSTEM_MESSAGE` | Generic/reserved only; do not use it when a more specific approved notification type applies. |

### Publication schedule notification exclusions

Do not show a new `PUBLICATION_SCHEDULE` notification when:

- a `DRAFT` chapter only receives or changes a planned release date and remains `DRAFT`;
- a `REVISION_REQUESTED` chapter only receives or changes a planned release date and remains `REVISION_REQUESTED`;
- an `UNDER_REVIEW` chapter only receives or changes a planned release date and remains `UNDER_REVIEW`;
- a `SCHEDULED` chapter is saved with the same normalized planned release date.

The scheduling/rescheduling actor is excluded from `PUBLICATION_SCHEDULE` recipients.

---

## 10. Series Ranking UI

### Purpose

Display dynamic series rankings from `manga.vw_SeriesRanking` for a selected `PublicationPeriod`.

### Ranking list fields

| Field | Notes |
|---|---|
| Rank position | Computed by the ranking view using `DENSE_RANK()`. |
| Series title | Display name of the ranked series. |
| Slug | Optional; use only if the row links to `/series/{slug}`. |
| Average rating | Score from period vote input. |
| Rating count | Number of rating/vote submissions in the period. |
| Reading count | Number of readers/views/follows in the period. |
| Ranking score | Weighted score computed from the ranking formula. |

### Ranking score behavior

```text
ranking_score =
    (v / (v + m)) * R
    +
    (m / (v + m)) * C
```

- `R` = series average rating for the effective ranking scope.
- `v` = series rating count for the effective ranking scope.
- `C` = rating-count-weighted average rating across eligible ranked series in that same scope.
- `m` = median rating count across eligible ranked series in that same scope.
- `reading_count` is displayed as popularity/readership evidence and is not directly added to `ranking_score`.
- The UI does not need to expose `C` or `m`; they are calculation inputs.
- For monthly/yearly/all-time derived views, aggregate weekly source evidence by series first, then recalculate the broader scope's own `R`, `v`, `C`, `m`, and weighted score. Do not average weekly ranking scores.

### Ranking warning UI behavior

- A completed weekly result fails the warning checks only when **both** `ranking_score < 6.5` and bottom-25% rank are true.
- High ranking risk requires failed checks in at least 2 of the latest 3 consecutive completed weekly periods, including the latest.
- The series must have ranking input in all 3 evaluated weeks and each week must contain at least 4 ranked series.
- Current/incomplete weeks and monthly/yearly/all-time reporting views do not independently generate warnings.
- The Bell warning is sent to all distinct active contributors of the exact affected series and is advisory only.

### Filtering behavior

- Ranking should be queried by `publication_period_id`.
- Completed series must remain visible in ranking results when `SeriesVoteInput` exists for the selected publication period; the UI must not hide a row only because the series status is `COMPLETED`.
- `period_name` is display-friendly and unique, but backend filtering should prefer the stable ID.
- The ranking UI does not need to show entered/updated timestamps or source notes; those belong to the vote input management screen.

### Vote input management behavior

- Editorial Board Members and Editorial Board Chiefs may enter or update `SeriesVoteInput` for `WEEKLY` publication periods when allowed; monthly/yearly/all-time views are derived from weekly source evidence and do not accept separate manual aggregate input.
- Input fields are `rating_count`, `average_rating`, `reading_count`, and optional `data_source_note`.
- Validation should reject non-positive counts, `average_rating` outside 0 to 10, and `rating_count > reading_count`.
- The vote input screen should explain that weekly input is period-only and must not include earlier weeks.