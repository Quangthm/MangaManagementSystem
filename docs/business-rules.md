# Manga Creation Workflow and Publishing Management System — Business Rules

**Document purpose:** Cleaned business rules for the MVP database/workflow design.  
**Scope:** Manga workflow management, proposal review, board polling, page versioning, annotations, page tasks, ranking simulation, notifications, and auditability.  
**Important MVP design direction:** Avoid unnecessary history/submission/policy tables unless the business event is important enough to store directly.

> **Latest alignment update — 2026-07-04:** This version replaces strict publication-period scheduling enforcement with the finalized advisory scheduling direction. Publication scheduling remains chapter-level: planned release dates belong to `Chapter`, `SCHEDULED` applies to `Chapter.status_code`, and page/content workflows remain locked for scheduled/on-hold chapters. `Series.publication_frequency_code` now drives default suggestions and warnings only; Mangaka and Tantou Editors may choose any planned release date on the current publication business date or later when permissions/status allow it. Editors remain the final release enforcer, on-hold recovery requires a new planned date, and auto-hold for overdue scheduled chapters is deferred.

> **Latest series lifecycle alignment — 2026-07-19:** `HIATUS` is the schema term for a paused series. Active Mangaka or Tantou Editor contributors may move a `SERIALIZED` series to `HIATUS` and resume it back to `SERIALIZED`. `HIATUS` blocks chapter release only; drafting, editing, review, scheduling, and rescheduling remain allowed when normal chapter rules allow them. Only active Mangaka contributors may mark their `SERIALIZED` or `HIATUS` series as `COMPLETED`. A completed series is final, immutable for normal business changes, cancels unreleased chapters and their distinct active `ASSIGNED`/`UNDER_REVIEW` page tasks after warning and confirmation, preserves released chapters and terminal task history, and remains visible in rankings when ranking input exists.

> **Latest ranking and notification alignment — 2026-07-21:** Existing approved notification behavior for proposal, board, task, chapter, publication scheduling, and account approval remains unchanged. Dynamic ranking now uses the weighted formula `ranking_score = (v / (v + m)) * R + (m / (v + m)) * C`; `reading_count` is no longer a direct score boost. The `RANKING_WARNING` contract is finalized: a weekly result fails only when both the weighted score is below the approved `6.5` baseline and the series is in the bottom 25% for that completed week; high risk requires failure in at least 2 of the latest 3 consecutive completed weekly periods including the latest. Recipients are all distinct active contributors of the exact affected series.

> **Latest chapter submission validation — 2026-07-21:** Chapter submission is allowed only for an active Mangaka contributor, from `DRAFT` or `REVISION_REQUESTED`, when zero distinct associated page tasks are still `ASSIGNED` or `UNDER_REVIEW`. `COMPLETED` and `CANCELLED` tasks do not block. Association is derived through task-region/page-region/page-version/page/chapter links and deduplicated by `ChapterPageTaskId`. A blocked submission performs no chapter transition, successful-submission audit, `CHAPTER_REVIEW` notification, or automatic task mutation; same-chapter task creation and submission must be concurrency-safe.

> **Latest implementation-alignment decisions — 2026-07-23:** Email/password self-registration follows the current repository flow: the user must pass reCAPTCHA before a 6-digit email OTP is sent, and the pending account is created only after successful OTP verification; Google sign-up remains a separate verified-identity path and still creates `PENDING_APPROVAL`. The current MVP has no Mangaka proposal-withdrawal workflow. Assistants are allowed to view dynamic rankings, while manual ranking input remains restricted to Editorial Board Member/Chief roles. A `CANCELLED` chapter does not reserve its chapter number label: a new non-cancelled chapter may reuse the same label while the cancelled row keeps its original label, enforced by uniqueness among non-cancelled chapters only. Scheduling accepts `planned_release_date >=` the current publication business date (today in the configured publication timezone); past dates are invalid. `PageRegion` geometry supports either a DOT (`width = 0` and `height = 0`) or an area rectangle (`width > 0` and `height > 0`), and mixed zero/non-zero dimensions are invalid. Ranking preserves true ties: equal `ranking_score` values share the same `DENSE_RANK`; deterministic secondary ordering may be used only to display rows within the same rank and must not change `rank_position`.

---

## 1. AI-Assisted Translation

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-TRANS-001 | AI-assisted translation is treated as an editing aid, not as a separate persistent translation workflow. | Active draft |
| BR-TRANS-002 | The final translated page is stored as a new `ChapterPageVersion`. | Active draft |
| BR-TRANS-003 | The system does not need to store every OCR text, AI translation suggestion, or manually edited translation in structured translation rows for MVP. | Active draft |
| BR-TRANS-004 | If a translated page replaces or improves the previous page image, it must be uploaded as a new page version rather than overwriting the previous file. | Active draft |
| BR-TRANS-005 | Old page versions remain available for traceability and comparison. | Active draft |
| BR-TRANS-006 | AI translation suggestions require human review before becoming part of the final page file. | Active draft |
| BR-TRANS-007 | The saved translated page file is the authoritative result of the translation/editing process. | Active draft |
| BR-TRANS-008 | The system provides AI-assisted translation suggestions for detected text regions, but it does not guarantee fully automatic manga localization in the MVP. | Active draft |
| BR-TRANS-009 | Users may review, edit, override, and approve AI/OCR translation suggestions before saving the final translated page version. | Active draft |
| BR-TRANS-010 | AI-assisted translation tools are available to any Authorized Page Workspace User who has access to the relevant chapter/page workspace; final save/approval behavior remains controlled by the user's role and workflow permission. | Active draft |

---

## 2. Page Region and AI Detection

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-REG-001 | Each `PageRegion` belongs to exactly one `ChapterPageVersion`. | Active draft |
| BR-REG-002 | A page region must have one valid region type: `PANEL`, `SPEECH_BUBBLE`, `CHARACTER`, `SFX_TEXT`, `BACKGROUND`, `FULL_PAGE`, or `OTHER`. | Active draft |
| BR-REG-003 | Page regions are stored as pixel-based geometry using x, y, width, and height, relative to the original page image dimensions. | Active draft |
| BR-REG-004 | `width = 0` and `height = 0` represents a DOT/point region. `width > 0` and `height > 0` represents an area/rectangle region. Both must be non-negative. Mixed states such as `width > 0, height = 0` or `width = 0, height > 0` are not allowed. | Active draft |
| BR-REG-005 | A region source must be either `AI` or `MANUAL`. | Active draft |
| BR-REG-006 | A page region may be created manually or suggested by AI. | Active draft |
| BR-REG-007 | If an AI-generated region is adjusted by a user, it becomes a manual region. | Active draft |
| BR-REG-008 | Manual regions should not keep an AI confidence score. | Active draft |
| BR-REG-009 | AI confidence scores, when stored, must be between 0 and 1. | Active draft |
| BR-REG-010 | Page region coordinates are stored relative to the original uploaded page image dimensions. | Active draft |
| BR-REG-011 | Character regions are represented by bounding boxes in MVP, not exact body outlines or masks. | Active draft |
| BR-REG-012 | Page regions remain linked to the page version where they were created and should not automatically move to newer page versions. | Active draft |
| BR-REG-013 | AI job execution history is not stored in MVP; accepted AI output is stored directly as page regions. | Active draft |
| BR-REG-014 | AI results should not automatically approve or reject manga pages or chapters. | Active draft |
| BR-REG-015 | AI detection is advisory and requires human review before being used in workflow decisions. | Active draft |
| BR-REG-016 | Original OCR text may be stored on text-related regions when available. | Active draft |
| BR-REG-017 | `PageRegion.original_text` represents text detected from the original page image, not final translated text. | Active draft |
| BR-REG-018 | `PageRegion` may support translation/OCR review, but final translated content is not stored as region translation rows. | Active draft |
| BR-REG-019 | When a user opens the segmentation tool for a page version, the system should load existing saved `PageRegion` records for that page version when available. | Active draft |
| BR-REG-020 | The system may allow AI-assisted segmentation to run even when saved `PageRegion` records already exist for the same page version. | Active draft |
| BR-REG-021 | AI-detected regions may be shown as temporary suggestions before the user chooses which ones to save. | Active draft |
| BR-REG-022 | The system should compare newly detected AI regions against existing saved `PageRegion` records for the same page version to reduce duplicate saved regions. | Active draft |
| BR-REG-023 | The system should not automatically save duplicate AI-detected regions that substantially overlap an existing saved region of the same type. | Active draft |
| BR-REG-024 | Users may save non-duplicate AI-detected regions as new `PageRegion` records when they are useful for annotation, task assignment, translation, OCR review, or segmentation display. | Active draft |
| BR-REG-025 | Saved `PageRegion` records may be reused later for annotation, task assignment, translation/OCR review, or page segmentation display. | Active draft |
| BR-REG-026 | A saved `PageRegion` may be adjusted by authorized users when region editing is permitted. | Active draft |
| BR-REG-027 | When a saved `PageRegion` is adjusted, the system should record the last update time and the user who made the update. | Active draft |
| BR-REG-028 | AI-assisted segmentation tools are available to all Authorized Page Workspace Users who can access the relevant workspace; saving, editing, assigning, or using detected regions must still follow role-specific permissions. | Active draft |
| BR-REG-029 | A `FULL_PAGE` region represents the entire page image area. It uses `type_code = FULL_PAGE`, `x = 0`, `y = 0`, `width > 0`, `height > 0`, `source_type = MANUAL`, and `confidence_score = NULL`. | Active draft |
| BR-REG-030 | Whole-page regions are used by Quick Select and other workflows that assign tasks or annotations to an entire page without requiring manual region selection. | Active draft |
| BR-REG-031 | The system should find and reuse an existing whole-page `PageRegion` for a given `ChapterPageVersion` before creating a new one. | Active draft |
| BR-REG-032 | `FileResource` does not store image width/height. Page image dimensions must be resolved from Cloudinary metadata when needed (e.g. for creating whole-page regions). Cloudinary metadata lookups must not hold an open database transaction. | Active draft |
| BR-REG-033 | A `PageRegion` may be hard-deleted only when it is not connected to any `ChapterPageAnnotationRegion`, `ChapterPageTaskRegion`, or other workflow record that depends on the region. | Active draft |
| BR-REG-034 | If a `PageRegion` is connected to an annotation or task, normal user deletion must be blocked and the region must be preserved for traceability. | Active draft |

---

## 3. File Resource and Cloudinary

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-FILE-001 | Cloudinary stores the actual media files, while SQL Server stores file metadata and references. | Active draft |
| BR-FILE-002 | All uploaded or generated files used by business records must be referenced through `manga.FileResource`. Tables such as `ChapterPageVersion`, `SeriesProposal`, `Series`, `Users`, and editorial review/markup records should store `file_resource_id` references instead of raw Cloudinary URLs. | Active draft |
| BR-FILE-003 | Cloudinary assets used by the system should be deleted through the application workflow, not manually from the Cloudinary console. | Active draft |
| BR-FILE-004 | A file resource is considered active when `deleted_at_utc IS NULL`. Application queries must exclude deleted file resources unless the user is viewing historical or audit data. | Active draft |
| BR-FILE-005 | A user avatar, when uploaded, is stored as a `FileResource` and referenced from the user account. | Active draft |
| BR-FILE-006 | A user avatar file must use `file_purpose_code = USER_AVATAR`. | Active draft |
| BR-FILE-007 | Series cover images, proposal files, chapter page-version files, editorial attachment/markup files, portfolio files, and user avatars should all use `FileResource` instead of storing Cloudinary fields directly in business tables. | Active draft |
| BR-FILE-008 | In normal UI contexts, when a referenced file is deleted or unavailable, the system should display a safe placeholder instead of exposing a broken file reference. | Active draft |
| BR-FILE-009 | Files used as `ChapterPageVersion` content, including accepted AI/translation output or accepted assistant output, must use `file_purpose_code = CHAPTER_PAGE_VERSION`. | Active draft |
| BR-FILE-011 | Every `FileResource` must store a required `sha256_hash` calculated from the exact uploaded file bytes before the file metadata is saved. | Active draft |
| BR-FILE-012 | `sha256_hash` may be used for file integrity checking, duplicate detection, and audit traceability, but it should not be treated as a global uniqueness rule because the same file may be validly reused in different workflow contexts. | Active draft |
| BR-FILE-013 | The system may optionally check `sha256_hash` before creating a new `FileResource` to detect whether an active file with the same content already exists. | Optional MVP |
| BR-FILE-014 | Optional duplicate-file warnings may be shown for repeated registration portfolios, repeated proposal files, repeated series covers, repeated chapter page-version files, repeated editorial attachments, or repeated avatars. | Optional MVP |
| BR-FILE-015 | Duplicate-file warnings are advisory usability messages; they should not automatically block uploads unless a specific workflow later defines blocking behavior. | Optional MVP |
| BR-FILE-016 | Some duplicate-file warnings may be omitted from the MVP UI when implementation time is limited, as long as `sha256_hash` is still stored for future detection, integrity checks, and audit traceability. | Optional MVP |
| BR-FILE-017 | The system must validate uploaded file extensions and content types according to the file purpose code before uploading to Cloudinary or saving file metadata. | Active draft |
| BR-FILE-018 | `SERIES_PROPOSAL` files must be formal proposal documents only and may use `.pdf`, `.doc`, or `.docx` in MVP. Markdown, plain text, and image files are not accepted for this purpose. | Active draft |
| BR-FILE-019 | Image-only file purposes, including `SERIES_COVER`, `CHAPTER_PAGE_VERSION`, and `USER_AVATAR`, may use `.jpg`, `.jpeg`, `.png`, or `.webp` in MVP. | Active draft |
| BR-FILE-020 | Mixed document/image purposes, including `EDITORIAL_ATTACHMENT` and `REGISTRATION_PORTFOLIO`, may use `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, or `.webp` in MVP. | Active draft |

### MVP File Purpose Upload Format Matrix

| File purpose code | Allowed extensions | Allowed content types | Cloudinary resource type | Notes |
|---|---|---|---|---|
| `SERIES_PROPOSAL` | `.pdf`, `.doc`, `.docx` | `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `raw` | Formal series proposal documents only. Markdown, plain text, and image files are not accepted for proposal submission in MVP. |
| `SERIES_COVER` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Series cover image. The Web draft UI may upload a cropped `1000×1500` PNG result as the actual cover while still accepting standard image source types. |
| `CHAPTER_PAGE_VERSION` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Official manga page image/version output. |
| `EDITORIAL_ATTACHMENT` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Editorial markup, review attachments, or supporting screenshots/documents. |
| `REGISTRATION_PORTFOLIO` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Optional portfolio submitted for account approval/profile review. |
| `USER_AVATAR` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | User profile/avatar image. |


---

## 4. Users and Accounts

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-USER-001 | Each user account has exactly one MVP system role. | Active draft |
| BR-USER-002 | New registered users are created with `PENDING_APPROVAL` status by default. | Active draft |
| BR-USER-003 | A `PENDING_APPROVAL` user cannot access protected workspace functions until approved. | Active draft |
| BR-USER-004 | Admin can activate a pending user by changing status to `ACTIVE`. | Active draft |
| BR-USER-005 | Admin can disable an account by changing status to `DISABLED`. | Active draft |
| BR-USER-006 | A disabled account cannot log in. | Active draft |
| BR-USER-007 | A user may optionally have an avatar file. | Active draft |
| BR-USER-008 | A user may optionally have a portfolio file for admin review. | Active draft |
| BR-USER-009 | Avatar and portfolio files are stored through `FileResource`; Cloudinary details stay inside `FileResource`. | Active draft |
| BR-USER-010 | Registration approval/rejection history is handled through current user status and audit log in MVP, not a separate registration request table. | Active draft |
| BR-USER-011 | Admin can reject a pending user by changing status to `REJECTED`. | Active draft |
| BR-USER-012 | A rejected account cannot log in, and its email and username remain reserved in MVP. | Active draft |
| BR-USER-013 | Each user account must have a `display_name` for user-facing identity display. | Active draft |
| BR-USER-014 | `display_name` is used on user-facing screens such as contributor lists, task assignments, annotations, notifications, board votes, and audit display. | Active draft |
| BR-USER-015 | `display_name` is not used as the login identifier; `username` remains the login/system account identifier. | Active draft |
| BR-USER-016 | `display_name` does not need to be unique. Multiple users may have the same display name. | Active draft |
| BR-USER-017 | If a user does not provide a display name during registration or external login, the system uses the username as the default display name. | Active draft |
| BR-USER-018 | A user may update their own display name without providing the account password. | Active draft |
| BR-USER-019 | Updating `display_name` must not change `username`, `email`, role, account status, password, or account approval state. | Active draft |
| BR-USER-020 | Display name changes should be recorded in the audit log for traceability. | Active draft |
| BR-USER-021 | The system should reject empty or whitespace-only display names after trimming. | Active draft |
| BR-USER-022 | A user may update their own avatar and portfolio file | Active draft |
| BR-USER-023 | Email/password self-registration must pass reCAPTCHA verification before the system sends a registration OTP. | Active draft |
| BR-USER-024 | Email/password self-registration must verify the 6-digit OTP sent to the entered email before the system creates the user account. Successful OTP verification creates the account as `PENDING_APPROVAL` and does not bypass Admin approval. Google sign-up uses verified Google identity instead of this email-OTP step and also creates `PENDING_APPROVAL`. | Active draft |
---

## 5. Series

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-SERIES-001 | Each series is identified internally by immutable `series_id` GUID; no separate human-readable business identifier is used in the MVP schema. | Active draft |
| BR-SERIES-002 | Each series must have a unique URL slug. | Active draft |
| BR-SERIES-003 | Each series must have one lifecycle status from the approved status list. | Active draft |
| BR-SERIES-004 | A series becomes `SERIALIZED` after it has passed proposal, editorial, and board approval and is accepted into the production/publication workflow. | Active draft |
| BR-SERIES-005 | Each series must declare one primary content language. | Active draft |
| BR-SERIES-006 | Series genres are normalized through `manga.Genre` and `manga.SeriesGenre`; a series may have multiple genres. | Active draft |
| BR-SERIES-006A | Series tags are normalized through `manga.Tag` and `manga.SeriesTag`; a series may have multiple tags. | Active draft |
| BR-SERIES-006B | Genres represent broad story categories, while tags represent more specific tropes, themes, settings, character traits, source/context labels, or content descriptors. | Active draft |
| BR-SERIES-006C | Genres and tags are current series metadata and are not stored as proposal-history snapshot tables in MVP. | Active draft |
| BR-SERIES-007 | A series cover image is current series metadata, must be stored as a `FileResource`, and should have the file purpose `SERIES_COVER` when provided. | Active draft |
| BR-SERIES-007A | When the Web UI supports series cover cropping, the cropped image file is the authoritative series cover file uploaded to the backend. | Active draft |
| BR-SERIES-007B | The system should not store original/cropped dual files or crop metadata for `SERIES_COVER` in MVP. | Active draft |
| BR-SERIES-007C | The MVP series cover crop target is a 2:3 portrait image, currently output as `1000×1500` `image/png` before upload. | Active draft |
| BR-SERIES-007D | Source images smaller than the recommended cover output size may still be accepted, but the UI should warn that the final cover may look blurry after upscaling. | Active draft |
| BR-SERIES-008 | A series may optionally reference another series as its source version. | Active draft |
| BR-SERIES-009 | A series cannot reference itself as its own source series. | Active draft |
| BR-SERIES-010 | Series ownership and contributor membership are managed through `SeriesContributor` instead of storing a lead Mangaka directly in `Series`. | Active draft |
| BR-SERIES-011 | A series may have multiple contributors who can participate in managing or editing series information. | Active draft |
| BR-SERIES-012 | When editable series profile metadata is changed, the system must record both the update time and the user who made the change. | Active draft |
| BR-SERIES-013 | `Series.updated_at_utc` and `Series.updated_by_user_id` are used for profile metadata edits, not as the main history mechanism for status transitions. | Active draft |
| BR-SERIES-014 | If `updated_by_user_id` is provided, `updated_at_utc` must also be provided, and vice versa. | Active draft |
| BR-SERIES-015 | Normal series draft creation and profile editing is a Mangaka workflow, not an Admin workflow. | Active draft |
| BR-SERIES-016 | Admin should not create or update series drafts as normal business flow; Admin remains responsible for account, system, audit, traceability, and file-management responsibilities. | Active draft |
| BR-SERIES-017 | Draft series are managed internally by `series_id`, not by the public slug URL. | Active draft |
| BR-SERIES-018 | The backend should generate the series slug from the title when a draft is created. | Active draft |
| BR-SERIES-019 | While a series is still in `PROPOSAL_DRAFT`, the backend may automatically regenerate the slug when the Mangaka changes the title and saves the draft. | Active draft |
| BR-SERIES-020 | Once a series leaves `PROPOSAL_DRAFT`, the slug must be locked for normal workflow so the future `/series/{slug}` URL remains stable. | Active draft |
| BR-SERIES-021 | The stable main series URL uses `/series/{slug}` and becomes meaningful primarily after the series reaches `SERIALIZED`. | Active draft |
| BR-SERIES-022 | Normal Mangaka profile updates to title, slug, synopsis, genres, tags, cover, content language, source series, and publication frequency are allowed only while the series is in `PROPOSAL_DRAFT`. | Active draft |
| BR-SERIES-023 | After a series leaves `PROPOSAL_DRAFT`, normal profile editing is locked; later changes require a separate controlled workflow, board/chief procedure, or future administrative correction process. | Active draft |
| BR-SERIES-024 | After serialization, Mangaka production work continues through chapters, pages, page versions, regions, tasks, and the authorized chapter workspace rather than by editing the locked series profile. | Active draft |
| BR-SERIES-025 | When a Mangaka creates a new series draft, the system must create the `Series` row and an active `SeriesContributor` row for that Mangaka in the same backend workflow or transaction, so that the creator is immediately recognized as an active Mangaka contributor for the draft. | Active draft |

| BR-SERIES-026 | The approved MVP series lifecycle status list is `PROPOSAL_DRAFT`, `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `SERIALIZED`, `HIATUS`, `COMPLETED`, and `CANCELLED`. | Active draft |
| BR-SERIES-027 | `HIATUS` is the schema status for a paused series; do not add a separate `PAUSED` series status in MVP. | Active draft |
| BR-SERIES-028 | Only active Mangaka contributors or active Tantou Editor contributors of the series may change `Series.status_code` from `SERIALIZED` to `HIATUS`. | Active draft |
| BR-SERIES-029 | Only active Mangaka contributors or active Tantou Editor contributors of the series may change `Series.status_code` from `HIATUS` back to `SERIALIZED`. | Active draft |
| BR-SERIES-030 | A `HIATUS` series blocks chapter release actions, but it does not block drafting, chapter creation, page work, review, scheduling, or rescheduling when the normal chapter status and role permissions allow those actions. | Active draft |
| BR-SERIES-031 | Moving a series to `HIATUS` must not automatically change existing scheduled chapters to `ON_HOLD`; scheduled chapters keep their chapter-level status until a valid chapter workflow changes them. | Active draft |
| BR-SERIES-032 | To release a chapter under a `HIATUS` series, an authorized active Mangaka or Tantou Editor contributor must first resume the series back to `SERIALIZED`. | Active draft |
| BR-SERIES-033 | Only an active Mangaka contributor of the series may change `Series.status_code` from `SERIALIZED` or `HIATUS` to `COMPLETED`. | Active draft |
| BR-SERIES-034 | `COMPLETED` means the author-side series has naturally ended; it is distinct from `CANCELLED`, which represents board/business cancellation. | Active draft |
| BR-SERIES-035 | Marking a series as `COMPLETED` requires a clear warning and explicit confirmation because it is final and normally irreversible through normal workflow. | Active draft |
| BR-SERIES-036 | When a series becomes `COMPLETED`, normal business mutations under the series must be blocked, including changes to series profile/status, new chapters, chapter content changes, page/page-version changes, region edits, task changes, review changes, scheduling, rescheduling, hold, and release actions. | Active draft |
| BR-SERIES-037 | Completed-series immutability does not delete or hide existing records; released chapters, cancelled chapters, page versions, regions, annotations, tasks, review records, notifications, and audit logs remain preserved for viewing and traceability where authorized. | Active draft |
| BR-SERIES-038 | When a Mangaka confirms series completion, already `RELEASED` chapters remain `RELEASED`. | Active draft |
| BR-SERIES-039 | When a Mangaka confirms series completion, unreleased active chapters should be changed to `CANCELLED` with an explicit completion-related reason. | Active draft |
| BR-SERIES-040 | Unreleased active chapters for completion cancellation include `DRAFT`, `REVISION_REQUESTED`, `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, and `ON_HOLD` chapters; already `RELEASED` chapters and already `CANCELLED` chapters must not be changed. | Active draft |
| BR-SERIES-041 | Completion-cancelled chapters remain preserved as read-only historical records and must follow the normal terminal behavior of cancelled chapters. | Active draft |
| BR-SERIES-042 | A `CANCEL_SERIALIZATION` board poll may target a `SERIALIZED` or `HIATUS` series, but not a `COMPLETED` series through normal MVP workflow. | Active draft |
| BR-SERIES-043 | Before confirming series completion, the system should show the affected unreleased chapters and the count of distinct active page tasks that will be cancelled; execution must recalculate authoritative impact rather than trusting the preview. | Active draft |
| BR-SERIES-044 | When a series is completed, each distinct `ASSIGNED` or `UNDER_REVIEW` page task linked to a completion-cancelled chapter must be changed to `CANCELLED`. | Active draft |
| BR-SERIES-045 | Series completion must preserve `COMPLETED` tasks, already `CANCELLED` tasks, and tasks linked only to unaffected chapters such as `RELEASED` chapters. | Active draft |
| BR-SERIES-046 | A page task affected by series completion must be counted, changed, and cancellation-audited at most once even when the task is linked to multiple `PageRegion` records under affected chapters. | Active draft |
| BR-SERIES-047 | Series completion must apply required task cancellations, chapter cancellations, the `Series.status_code = COMPLETED` change, and required audit records atomically so that a failure rolls back the entire cascade. | Active draft |

### Future Information

| Rule ID | Future Information | Review Status |
|---|---|---|
| FI-SERIES-001 | Additional public-facing discovery metadata, moderation workflows, or controlled vocabulary management may be expanded later if the MVP needs richer catalogue/search behavior. | Future information |

---

## 6. Series Contributors

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-SC-001 | A `SeriesContributor` record links one user to one series. | Active draft |
| BR-SC-002 | Contributor specialization is outside MVP scope; the contributor’s broad role is determined by `auth.Users.role_id`. | Active draft |
| BR-SC-003 | A user may be added as a contributor to a series based on their single system role in `auth.Users`. | Active draft |
| BR-SC-004 | A user cannot be an active contributor to the same series more than once at the same time. | Active draft |
| BR-SC-005 | A contributor with `end_date IS NULL` is considered an active contributor. | Active draft |
| BR-SC-006 | Historical contributor rows should be preserved after a contributor leaves a series. | Active draft |
| BR-SC-007 | A contributor’s `end_date` cannot be earlier than their `start_date`. | Active draft |
| BR-SC-008 | A series submitted from `PROPOSAL_DRAFT` into `UNDER_EDITORIAL_REVIEW` must have at least one active Mangaka contributor, but it does not require an active Tantou Editor contributor at first submission. Submitted proposals appear in the editorial review queue for active Tantou Editors to claim or handle later. | Active draft |

---

## 7. Series Proposal

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PROP-001 | A `SeriesProposal` row represents one formal submitted proposal version for a series. | Active draft |
| BR-PROP-002 | A series may have multiple proposal versions over time. | Active draft |
| BR-PROP-003 | Proposal version numbers must be positive and unique within the same series. | Active draft |
| BR-PROP-004 | A submitted proposal must preserve submission-time snapshots of review-relevant submitted content, including proposal title, synopsis, and the proposal file. | Active draft |
| BR-PROP-004A | `SeriesProposal` does not snapshot genres, tags, or the current series cover file in MVP; reviewers read current genres, tags, and cover from the locked `Series` metadata during review. | Active draft |
| BR-PROP-005 | A submitted series proposal must include a proposal file stored as a `FileResource` with purpose `SERIES_PROPOSAL`. | Active draft |
| BR-PROP-006 | `SeriesProposal` does not store draft proposal editing; a row is created only when the proposal is formally submitted for editorial review. | Active draft |
| BR-PROP-007 | Once a `SeriesProposal` row is created, its submitted snapshot fields should remain locked. | Active draft |
| BR-PROP-008 | If revision is requested, the corrected proposal must be submitted as a new proposal version. | Active draft |
| BR-PROP-009 | Proposal status should reflect the current review stage of the submitted proposal package. | Active draft |
| BR-PROP-012 | Editorial review information may be stored directly in `SeriesProposal` because each proposal version receives at most one editorial review. | Active draft |
| BR-PROP-013 | `UNDER_BOARD_REVIEW` means the proposal passed editorial review and is waiting for board voting/decision. | Active draft |
| BR-PROP-014 | `APPROVED` means the proposal was approved by the board, not merely by the editor. | Active draft |
| BR-PROP-015 | Editorial revision requires non-empty comments and may optionally include a markup file. | Active draft |
| BR-PROP-015A | Editorial cancellation requires both non-empty comments and a markup file. | Active draft |
| BR-PROP-016 | Board rejection or board cancellation reasons are handled by board poll/vote records, not by editorial review comments. | Active draft |
| BR-PROP-017 | Proposal lists should support retrieval by series, status, submitter, and reviewer. | Active draft |
| BR-PROP-018 | The latest proposal version for a series should be quickly retrievable. | Active draft |
| BR-PROP-019 | Editorial and board queues should be filterable by proposal status. | Active draft |
| BR-PROP-020 | Reviewed proposal records should be searchable by reviewer for admin/editor tracking. | Active draft |
| BR-PROP-021 | The proposal file represents the supporting material used by editors and board members to evaluate the series concept. | Active draft |
| BR-PROP-022 | The system does not require a fixed minimum number of completed manga pages for proposal submission in MVP. | Active draft |
| BR-PROP-023 | Any minimum page/sample requirement is treated as an editorial policy outside the MVP database constraints. | Active draft |
| BR-PROP-024 | First proposal submission does not require an assigned active Tantou Editor. The submitted proposal must become visible in the editorial review queue for active Tantou Editors. | Active draft |
---

## 8. Board Poll

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-BOARD-POLL-001 | Editorial Board Chief may create board polls for `START_SERIALIZATION` or `CANCEL_SERIALIZATION`. | Active draft |
| BR-BOARD-POLL-002 | A `START_SERIALIZATION` poll may only be opened for a series with `Series.status_code = UNDER_BOARD_REVIEW`. | Active draft |
| BR-BOARD-POLL-003 | A `START_SERIALIZATION` poll may only be opened when the selected series has exactly one active proposal with `SeriesProposal.status_code = UNDER_BOARD_REVIEW`. | Active draft |
| BR-BOARD-POLL-004 | A `START_SERIALIZATION` poll represents board voting on the active under-board-review proposal for the selected series, even though the poll stores only `series_id`. | Active draft |
| BR-BOARD-POLL-005 | A `CANCEL_SERIALIZATION` poll may be opened for a `SERIALIZED` or `HIATUS` series when the Editorial Board Chief provides a reason. | Active draft |
| BR-BOARD-POLL-006 | Low ranking or high cancellation risk should be displayed as supporting evidence for cancellation polls when available, but it should not be the only allowed reason. | Active draft |
| BR-BOARD-POLL-007 | A poll must have a non-empty reason explaining why the poll was opened. | Active draft |
| BR-BOARD-POLL-008 | A poll may have a scheduled end time or may be closed manually by the Editorial Board Chief. | Active draft |
| BR-BOARD-POLL-009 | A series cannot have more than one open poll of the same type at the same time. | Active draft |
| BR-BOARD-POLL-010 | `poll_status_code` exists to distinguish open polls, valid closed polls, and cancelled/invalidated polls. | Active draft |
| BR-BOARD-POLL-011 | Votes from an `OPEN` poll are stored but are not final and must not update series or proposal status. | Active draft |
| BR-BOARD-POLL-012 | Votes from a `CLOSED` poll are valid and may be used by the Board Result rules to apply series or proposal status changes. | Active draft |
| BR-BOARD-POLL-013 | Votes from a `CANCELLED` poll remain stored for traceability but must not affect series or proposal status. | Active draft |
| BR-BOARD-POLL-014 | Editorial Board Chief may cancel a poll when the voting process should be invalidated, such as wrong series, incorrect poll type, administrative mistake, or invalid voting setup. | Active draft |
| BR-BOARD-POLL-015 | Cancelling a poll does not delete votes; it marks the poll result as invalid. | Active draft |
| BR-BOARD-POLL-016 | Poll results are computed from board votes and handled by the Board Result rules, not stored directly on the poll table. | Active draft |
| BR-BOARD-POLL-017 | Poll creation, cancellation, and closure should be recorded in the audit log. | Active draft |
| BR-BOARD-POLL-018 | When opening a `START_SERIALIZATION` poll, the Editorial Board Chief must specify the publication frequency to be applied if the poll is approved. | Active draft |

---

## 9. Board Vote

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-BOARD-VOTE-001 | Editorial Board Members and Editorial Board Chiefs may vote only in an open `SeriesBoardPoll`. | Active draft |
| BR-BOARD-VOTE-002 | Only users with the Editorial Board Member or Editorial Board Chief role may cast board votes. | Active draft |
| BR-BOARD-VOTE-003 | A board vote choice must be `APPROVE`, `REJECT`, or `ABSTAIN`. | Active draft |
| BR-BOARD-VOTE-004 | A rejection vote must include a non-empty reason. | Active draft |
| BR-BOARD-VOTE-005 | Each Editorial Board Member or Editorial Board Chief may cast at most one vote per board poll. | Active draft |
| BR-BOARD-VOTE-006 | Board votes are tied to a `SeriesBoardPoll`, and each poll is tied to a specific series. | Active draft |
| BR-BOARD-VOTE-007 | For a `START_SERIALIZATION` poll, the related series must have exactly one proposal currently in `UNDER_BOARD_REVIEW`. | Active draft |
| BR-BOARD-VOTE-008 | Board votes should be preserved for traceability even after the poll is closed or cancelled. | Active draft |
| BR-BOARD-VOTE-009 | Votes from a `CLOSED` poll may be used to determine the applicable poll result. | Active draft |
| BR-BOARD-VOTE-010 | Votes from a `CANCELLED` poll are preserved but must not be applied to series or proposal status changes. | Active draft |
| BR-BOARD-VOTE-011 | A board vote alone does not update series or proposal status; status changes occur only when an Editorial Board Chief or system process closes the poll and applies the computed result. | Active draft |

---

## 10. Board Result

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-BOARD-RESULT-001 | In MVP, board results are computed from `SeriesBoardVote` records tied to a `SeriesBoardPoll`, not stored in a separate `SeriesBoardDecision` table. | Active draft |
| BR-BOARD-RESULT-002 | Aggregate vote counts must be calculated from `SeriesBoardVote` records. | Active draft |
| BR-BOARD-RESULT-003 | The computed result of a board poll is derived from approve and reject vote counts. | Active draft |
| BR-BOARD-RESULT-004 | If approve votes exceed reject votes, the computed poll result is `APPROVED`. | Active draft |
| BR-BOARD-RESULT-005 | If reject votes exceed approve votes, the computed poll result is `REJECTED`. | Active draft |
| BR-BOARD-RESULT-006 | If approve and reject votes are tied, the computed poll result is `NO_DECISION`. | Active draft |
| BR-BOARD-RESULT-007 | Abstain votes are preserved and counted separately, but they do not directly determine approval or rejection in MVP. | Active draft |
| BR-BOARD-RESULT-008 | If a poll is `OPEN`, the computed result is treated as `PENDING` and must not be applied. | Active draft |
| BR-BOARD-RESULT-009 | If a poll is `CANCELLED`, the computed result is treated as `INVALIDATED` and must not be applied. | Active draft |
| BR-BOARD-RESULT-010 | Only a `CLOSED` poll can produce an applicable board result. | Active draft |
| BR-BOARD-RESULT-011 | A `START_SERIALIZATION` poll with an applicable `APPROVED` result changes the series to `SERIALIZED`, changes the active under-board-review proposal to `APPROVED`, and applies the publication frequency specified by the Editorial Board Chief. | Active draft |
| BR-BOARD-RESULT-012 | A `START_SERIALIZATION` poll with an applicable `REJECTED` result prevents the proposal from proceeding to serialization and should cancel the active proposal and series according to MVP workflow policy. | Active draft |
| BR-BOARD-RESULT-013 | A `START_SERIALIZATION` poll with `NO_DECISION` leaves the series in `UNDER_BOARD_REVIEW` and the active proposal in `UNDER_BOARD_REVIEW`. | Active draft |
| BR-BOARD-RESULT-014 | A `CANCEL_SERIALIZATION` poll with an applicable `APPROVED` result changes a `SERIALIZED` or `HIATUS` series status to `CANCELLED`. | Active draft |
| BR-BOARD-RESULT-015 | A `CANCEL_SERIALIZATION` poll with `REJECTED` or `NO_DECISION` leaves the series status unchanged. | Active draft |
| BR-BOARD-RESULT-016 | The system does not store a final board decision explanation in MVP; poll reason and individual rejection reasons are kept separately. | Active draft |
| BR-BOARD-RESULT-017 | Applying a board poll result to proposal or series status must be recorded in the audit log. | Active draft |

---

## 11. Chapter

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-CH-001 | Each chapter belongs to exactly one series. | Active draft |
| BR-CH-002 | Chapter number labels must be unique among **non-cancelled** chapters within the same series. A `CANCELLED` chapter does **not** reserve its number and keeps its original `chapter_number_label`; therefore a new non-cancelled replacement chapter may use the same label (for example, a cancelled Chapter 3 and a new active Chapter 3 may coexist). The database should enforce this with a filtered unique index such as `(series_id, chapter_number_label) WHERE status_code <> N'CANCELLED'`. | Active draft |
| BR-CH-003 | A chapter starts with `DRAFT` status when it is created. | Active draft |
| BR-CH-004 | `Chapter.status_code` stores only the current workflow status of the chapter. | Active draft |
| BR-CH-005 | A chapter may move through statuses such as `DRAFT`, `UNDER_REVIEW`, `REVISION_REQUESTED`, `APPROVED`, `SCHEDULED`, `RELEASED`, `ON_HOLD`, and `CANCELLED`. | Active draft |
| BR-CH-006 | `planned_release_date` is optional because a chapter may be created before it is scheduled for release. | Active draft |
| BR-CH-007 | A chapter can only be marked as `SCHEDULED` if it has a planned release date. | Active draft |
| BR-CH-008 | A chapter marked `RELEASED` must have `released_at_utc` populated. | Active draft |
| BR-CH-009 | `released_at_utc` should only be filled when the chapter is actually released. | Active draft |
| BR-CH-010 | A Tantou Editor may place a `SCHEDULED` chapter `ON_HOLD` when there is a valid operational or editorial reason. | Active draft |
| BR-CH-011 | `created_by_user_id` should identify the user who created the chapter record, usually a Mangaka or authorized contributor. | Active draft |
| BR-CH-012 | `updated_at_utc` records the last time the chapter row was modified for operational display. | Active draft |
| BR-CH-013 | Scheduling is chapter-level, not series-level; `SCHEDULED` is a chapter status and must not be applied to `Series.status_code`. | Active draft |
| BR-CH-014 | Mangaka and Tantou Editors may set or reschedule `Chapter.planned_release_date` when chapter status and permissions allow it, as long as the chosen planned release date is on or after the current publication business date. | Active draft |
| BR-CH-015 | If an `APPROVED` chapter receives a `planned_release_date` on the current publication business date or later, the chapter should move to `SCHEDULED`. | Active draft |
| BR-CH-016 | When a chapter is `SCHEDULED`, Mangaka users must not change chapter content or perform page/content mutation workflows for that chapter. | Active draft |
| BR-CH-017 | When a chapter is `SCHEDULED`, authorized Mangaka and Tantou Editors may reschedule `planned_release_date` to the current publication business date or a later date; the system may warn about frequency mismatch but must not hard-block it. | Active draft |
| BR-CH-018 | When a chapter is `SCHEDULED`, a Tantou Editor may move the chapter to `ON_HOLD` with a required reason; returning from `ON_HOLD` to `SCHEDULED` requires setting a new planned release date on the current publication business date or later. | Active draft |
| BR-CH-019 | A chapter cannot be released while its parent series is `HIATUS`, `COMPLETED`, or `CANCELLED`. | Active draft |
| BR-CH-020 | If the parent series is `COMPLETED`, normal chapter creation and mutation workflows must be blocked for all chapters under that series. | Active draft |
| BR-CH-021 | Normal new chapter creation is allowed only when the parent series is `SERIALIZED` or `HIATUS` and the creator is an `ACTIVE` Mangaka account with an active Mangaka contributor relationship to that series. Proposal/review states, `COMPLETED`, `CANCELLED`, null, and unknown series states must not allow normal chapter creation. | Active draft |

---

## 12. Chapter Page and Page Version

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-CP-001 | A chapter may contain many logical pages. | Active draft |
| BR-CP-002 | Each `ChapterPage` belongs to exactly one chapter. | Active draft |
| BR-CP-003 | Page numbers must be unique within the same chapter. | Active draft |
| BR-CP-004 | `ChapterPage` represents a logical page slot, not a specific uploaded image file. | Active draft |
| BR-CP-005 | A `ChapterPage` may have multiple `ChapterPageVersion` records over time. | Active draft |
| BR-CP-006 | Each `ChapterPageVersion` belongs to exactly one logical `ChapterPage`. | Active draft |
| BR-CP-007 | Each `ChapterPageVersion` must reference exactly one uploaded file through `FileResource`. | Active draft |
| BR-CP-008 | The upload time and uploader of a page version are derived from the related `FileResource`. | Active draft |
| BR-CP-009 | Version numbers must be positive. | Active draft |
| BR-CP-010 | Version numbers must be unique within the same `ChapterPage`. | Active draft |
| BR-CP-011 | A higher `version_no` represents a newer uploaded version of the same logical page. | Active draft |
| BR-CP-012 | Only one version of a page should be marked as the current version at a time. | Active draft |
| BR-CP-013 | Old page versions are kept for traceability and comparison, not overwritten. | Active draft |
| BR-CP-014 | Replacing or revising a page means creating a new `ChapterPageVersion`, not creating a new `ChapterPage`. | Active draft |
| BR-CP-015 | A page task output should reference the specific `ChapterPageVersion` produced for the same logical page derived from the task's linked `PageRegion` records. | Active draft |
| BR-CP-016 | Page annotations should remain linked to the exact `ChapterPageVersion` where they were created, derived through their linked `PageRegion`. | Active draft |
| BR-CP-017 | Page-specific feedback is stored through annotations linked to `PageRegion` records. | Active draft |
| BR-CP-018 | Formal editorial review is performed at chapter level, while page-level annotations provide supporting feedback on specific page versions or regions. | Active draft |
| BR-CP-019 | A `ChapterPage` can be soft-deleted when it is no longer part of the active chapter draft. | Active draft |
| BR-CP-020 | Soft-deleting a `ChapterPage` should not delete its historical `ChapterPageVersion` records. | Active draft |
| BR-CP-021 | Each revision upload creates a new `ChapterPageVersion`. | Active draft |
| BR-CP-022 | If a page task produces a new page version, the task may remain under review until the Mangaka accepts the submitted version. | Active draft |
| BR-CP-023 | Selecting or uploading a page file in the UI must not automatically create a `ChapterPageVersion`; a page version is created only when the user explicitly saves or confirms the selected file as an official version. | Active draft |
| BR-CP-024 | In the current MVP, normal users cannot delete saved `ChapterPageVersion` records. Wrong, outdated, or superseded saved versions remain in version history and may be replaced only by saving a newer version. | Active draft |
| BR-CP-025 | When a newly saved page version becomes current, the previously current version for the same `ChapterPage` must be unset as current while remaining preserved for traceability and comparison. | Active draft |
| BR-CP-026 | Future versions may add an Admin/system retention workflow to purge old or unused page versions after a chapter has been released, but this is outside MVP and must not remove versions still required by regions, annotations, tasks, reviews, release history, or audit. | Future information |

---


## 12A. Authorized Chapter Workspace

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-WORKSPACE-001 | The centralized authorized workspace is chapter-level, not page-level. | Active draft |
| BR-WORKSPACE-002 | The workspace loads a selected chapter and provides navigation across the selected series, its chapters, each chapter's pages, and each page's versions. | Active draft |
| BR-WORKSPACE-003 | The workspace left panel is responsible for navigation and context: series → chapters → pages → page versions. | Active draft |
| BR-WORKSPACE-004 | The workspace main viewing area should display the selected page version with available overlays such as page regions and annotations. | Active draft |
| BR-WORKSPACE-005 | The workspace right panel contains tools and actions such as AI segmentation, AI/OCR translation support, annotation actions, page-version actions, and role-specific workflow actions. | Active draft |
| BR-WORKSPACE-006 | AI tools in the workspace are available to all Authorized Page Workspace Users who have access to the relevant chapter/page version. | Active draft |
| BR-WORKSPACE-007 | Role-specific actions remain permission-gated; for example, Mangaka may assign page regions as assistant tasks, Assistants may work on assigned tasks, Mangaka may create production-tracking annotations, and Tantou Editors may create or manage editorial-review annotations according to annotation permission rules. | Active draft |
| BR-WORKSPACE-008 | Editorial Board Members are not considered Authorized Page Workspace Users by default unless a future permission explicitly grants workspace access. | Active draft |
| BR-WORKSPACE-009 | Workspace entry may come from the main series URL, a dashboard, an editorial review queue, an assistant task list, or another authorized workflow list. | Active draft |
| BR-WORKSPACE-010 | The workspace back action should return to the main `/series/{slug}` page by default when the user entered from the series page. | Active draft |
| BR-WORKSPACE-011 | When the user enters the workspace from a review queue, dashboard, or task list, the workspace may return to that source context instead of `/series/{slug}`. | Active draft |
| BR-WORKSPACE-012 | Workspace routing should use stable internal IDs such as `chapter_id`, `chapter_page_id`, and `chapter_page_version_id` for editing context; slug should be used mainly for returning to or displaying the main series page. | Active draft |

---

## 13. Chapter Page Annotation

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-ANN-001 | A `ChapterPageAnnotation` represents one annotation/comment record and may be linked to one or more `PageRegion` records through `ChapterPageAnnotationRegion`. | Active draft |
| BR-ANN-002 | `ChapterPageAnnotation` must not store direct page-region foreign keys or direct coordinates; annotation location is represented through its linked `PageRegion` records. | Active draft |
| BR-ANN-003 | `ChapterPageAnnotationRegion` is the junction table that links annotations to page regions. | Active draft |
| BR-ANN-004 | All `PageRegion` records linked to the same annotation must belong to the same `ChapterPageVersion`, so the annotation's page-version context remains unambiguous. | Active draft |
| BR-ANN-005 | The page version of an annotation is derived from the linked `PageRegion.chapter_page_version_id` values. | Active draft |
| BR-ANN-006 | Whole-page feedback must be represented by a manually created `PageRegion` covering the full page, then linked to the annotation through `ChapterPageAnnotationRegion`. | Active draft |
| BR-ANN-007 | Each annotation must have one issue type. | Active draft |
| BR-ANN-008 | `issue_type_code` must be selected from the approved annotation issue code list. | Active draft |
| BR-ANN-009 | The UI must only allow users to select issue types that are defined as valid annotation issue codes. | Active draft |
| BR-ANN-010 | An annotation must contain non-empty annotation text. | Active draft |
| BR-ANN-011 | Each annotation must record the user who created it. | Active draft |
| BR-ANN-012 | Each annotation must record the time it was created. | Active draft |
| BR-ANN-013 | Only active Mangaka contributors and active Tantou Editor contributors with access to the owning series/page workspace may create annotations in MVP. Assistants, Board roles, and Admin do not create annotations in MVP. | Active draft |
| BR-ANN-014 | A page annotation may be created from existing saved `PageRegion` records, newly created `PageRegion` records, or a combination of both. | Active draft |
| BR-ANN-015 | If an annotation is resolved, both resolver and resolved timestamp must be recorded. | Active draft |
| BR-ANN-016 | If an annotation is unresolved, both `resolved_by_user_id` and `resolved_at_utc` must be NULL. | Active draft |
| BR-ANN-017 | Resolving an annotation marks the feedback as handled but must not delete the annotation or its linked region records. | Active draft |
| BR-ANN-018 | When a new page version is uploaded, old annotations remain available through their linked regions for traceability and comparison. | Active draft |
| BR-ANN-019 | Annotation issue types are fixed for MVP and are enforced by database constraint rather than a separate lookup table. | Active draft |
| BR-ANN-020 | An annotation created by a Mangaka is treated as production-tracking feedback. It may be resolved by an active Mangaka contributor on the same series or by an active Tantou Editor contributor on the same series. | Active draft |
| BR-ANN-021 | An annotation created by a Tantou Editor is treated as editorial-review feedback. It may be resolved only by an active Tantou Editor contributor on the same series. Mangaka users must not resolve Tantou Editor-created annotations. | Active draft |
| BR-ANN-022 | Active Mangaka contributors may update unresolved annotation text only for annotations created by Mangaka users on the same series. | Active draft |
| BR-ANN-023 | Active Tantou Editor contributors may update unresolved annotation text for annotations created by either Mangaka users or Tantou Editors on the same series when the text needs clarification for production tracking or editorial review. | Active draft |
| BR-ANN-024 | Resolved annotations should not be edited in MVP; any correction to resolved feedback should use a new annotation or a future correction workflow. | Active draft |
| BR-ANN-025 | The MVP does not add an annotation-origin column. Stored procedures determine creation and resolution permissions by joining `ChapterPageAnnotation.annotated_by_user_id` to the creator's current role and by deriving the owning series through `ChapterPageAnnotationRegion → PageRegion → ChapterPageVersion → ChapterPage → Chapter`. | Active draft |
| BR-ANN-026 | Annotation text updates must be audit-logged with old text, new text, actor user, owning series/page context, and optional update reason when available. | Active draft |

## 14. Page Task

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PGTASK-001 | Each page task derives its logical `ChapterPage` context from one or more linked `PageRegion` records; `ChapterPageTask` does not store `chapter_page_id` directly. | Active draft |
| BR-PGTASK-002 | A logical `ChapterPage` may have many tasks over time through linked `PageRegion` records. | Active draft |
| BR-PGTASK-003 | A page task represents one assignment of work to one active Assistant contributor of the owning series. | Active draft |
| BR-PGTASK-004 | Each page task must be assigned to exactly one `ACTIVE` Assistant account that is an active Assistant contributor of the owning series. | Active draft |
| BR-PGTASK-005 | Each page task must record the user who created it. | Active draft |
| BR-PGTASK-006 | Task assignment is region-linked and page-derived in the MVP; whole-chapter task assignment is future scope. | Active draft |
| BR-PGTASK-007 | A page task may target one or more `PageRegion` records through the `ChapterPageTaskRegion` junction table. | Active draft |
| BR-PGTASK-008 | Task target regions are not stored as free-text descriptions; they are represented through linked `PageRegion` records. | Active draft |
| BR-PGTASK-009 | The same task-region pair cannot be inserted more than once. | Active draft |
| BR-PGTASK-010 | A `PageRegion` may be referenced by multiple tasks when different work is needed for the same area. | Active draft |
| BR-PGTASK-011 | All `PageRegion` records linked to the same task must belong to the same logical `ChapterPage`, derived through their `ChapterPageVersion` records. | Active draft |
| BR-PGTASK-012 | Region-based annotations can be used as the basis for creating page tasks. | Active draft |
| BR-PGTASK-013 | Assistant task UI should highlight linked page regions so the assistant knows exactly what areas to fix. | Active draft |
| BR-PGTASK-014 | Every page task must have a due date so the team can track production deadlines. | Active draft |
| BR-PGTASK-015 | Task status must be one of `ASSIGNED`, `UNDER_REVIEW`, `COMPLETED`, or `CANCELLED`. | Active draft |
| BR-PGTASK-016 | The system does not track whether an assistant has started working on a page task; a task remains `ASSIGNED` until the assistant submits a completed page version for review. | Active draft |
| BR-PGTASK-017 | A page task must have an uploaded page version before it can enter `UNDER_REVIEW` or `COMPLETED` status. | Active draft |
| BR-PGTASK-018 | A completed page task must reference the `ChapterPageVersion` produced by the assigned user. | Active draft |
| BR-PGTASK-019 | The completed page version must belong to the same logical `ChapterPage` derived from the task's linked `PageRegion` records. | Active draft |
| BR-PGTASK-020 | Assistants do not directly approve their own task output; the Mangaka or authorized reviewer reviews the submitted page version. | Active draft |
| BR-PGTASK-021 | A task may remain `UNDER_REVIEW` until the Mangaka or authorized reviewer accepts the submitted page version. | Active draft |
| BR-PGTASK-022 | When the submitted page version is accepted, the task may be marked `COMPLETED`. | Active draft |
| BR-PGTASK-023 | Task status is used for production tracking, while formal editorial approval remains chapter-level. | Active draft |
| BR-PGTASK-024 | The assigned user of a page task should not be changed after the task is created. | Active draft |
| BR-PGTASK-025 | If work must be reassigned to another assistant, the existing task should be completed or cancelled, and a new task should be created for the new assistant. | Active draft |
| BR-PGTASK-026 | Multiple tasks may exist for the same logical `ChapterPage` through different linked regions, assistants, task types, or work rounds. | Active draft |
| BR-PGTASK-027 | Task history is preserved by keeping old task rows rather than overwriting assignment ownership. | Active draft |
| BR-PGTASK-028 | If a page task is cancelled, the task description must be updated to include the cancellation reason. | Active draft |
| BR-PGTASK-029 | The system should preserve the original task record after cancellation for traceability. | Active draft |
| BR-PGTASK-030 | If cancelled work needs to be reassigned, the Mangaka should create a new task for the same page instead of changing the original assignee. | Active draft |
| BR-PGTASK-031 | Audit logging should record task creation, cancellation, completion, and status changes. | Active draft |
| BR-PGTASK-032 | New page task creation is allowed only when the owning series is `SERIALIZED` or `HIATUS` and the owning chapter is `DRAFT` or `REVISION_REQUESTED`. Later chapter states, proposal/review series states, `COMPLETED`, `CANCELLED`, null, and unknown states must not allow new task creation. | Active draft |
| BR-PGTASK-033 | The creator of a new page task must be an `ACTIVE` Mangaka account and an active Mangaka contributor of the owning series. | Active draft |
| BR-PGTASK-034 | The assignee of a new page task must be an `ACTIVE` Assistant account and an active Assistant contributor of the owning series. | Active draft |
| BR-PGTASK-035 | Single-task creation and Quick Select/batch task creation must enforce the same creator, assignee, parent-series, and parent-chapter production-eligibility rules. | Active draft |
| BR-PGTASK-036 | For series-completion cascade purposes, `ASSIGNED` and `UNDER_REVIEW` are active cancellable task statuses; `COMPLETED` and `CANCELLED` tasks are terminal and must be preserved. | Active draft |

---

## 15. Chapter Editorial Review

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-CH-SUB-001 | In the MVP, a chapter submission is represented by changing `Chapter.status_code` to `UNDER_REVIEW`, not by creating a separate `ChapterSubmission` row. | Active draft |
| BR-CH-SUB-002 | A submitted chapter consists of the current active page versions of all non-deleted chapter pages at the time it is submitted for review. | Active draft |
| BR-CH-SUB-003 | The system must prevent page creation, page deletion, page version upload, assistant task output submission that creates or changes page content, and other page/content mutation workflows while the chapter is `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, `ON_HOLD`, `RELEASED`, or `CANCELLED`. | Active draft |
| BR-CH-SUB-004 | When revision is requested, the chapter becomes editable again and new page versions may be uploaded. | Active draft |
| BR-CH-SUB-005 | Chapter content is stored as page-level assets through `ChapterPageVersion`, not as one required chapter-level submission file. | Active draft |
| BR-CH-SUB-006 | A chapter-level submission file or generated PDF is a future enhancement, not required for MVP. | Active draft |
| BR-CH-SUB-007 | Only an `ACTIVE` Mangaka who is an active Mangaka contributor of the owning series may submit a chapter for editorial review, and the chapter must be in `DRAFT` or `REVISION_REQUESTED` with zero distinct active page tasks associated with that chapter. | Active draft |
| BR-CH-SUB-008 | For chapter-submission validation, page tasks in `ASSIGNED` or `UNDER_REVIEW` are active and block submission; `COMPLETED` and `CANCELLED` tasks do not block submission. | Active draft |
| BR-CH-SUB-009 | Chapter-submission task validation must derive chapter association through `ChapterPageTask` → linked `PageRegion` → `ChapterPageVersion` → `ChapterPage` → `Chapter`, and must deduplicate by `ChapterPageTaskId` so one task linked to multiple regions counts only once. | Active draft |
| BR-CH-SUB-010 | If any distinct active page task exists for the chapter, the backend must reject submission before changing chapter status, writing the successful chapter-submission audit event, or creating `CHAPTER_REVIEW`; the rejection must not automatically complete, cancel, reassign, delete, or otherwise mutate those tasks. | Active draft |
| BR-CH-SUB-011 | A blocked chapter submission must return a clean user-facing business validation message explaining that active page tasks must be completed or cancelled before the chapter can be submitted. | Active draft |
| BR-CH-SUB-012 | Same-chapter task creation and chapter submission must be concurrency-coordinated so a new `ASSIGNED`/`UNDER_REVIEW` task cannot commit concurrently with the chapter transition to `UNDER_REVIEW`. | Active draft |
| BR-CH-SUB-013 | After all chapter-submission validations pass, the existing successful flow remains unchanged: the chapter transitions to `UNDER_REVIEW`, the normal submission audit is written, and `CHAPTER_REVIEW` is created for the correct distinct active Tantou Editor contributors of the exact series. | Active draft |
| BR-CH-REV-001 | Editorial reviews are recorded directly against `Chapter` in the MVP. | Active draft |
| BR-CH-REV-002 | Each `ChapterEditorialReview` record must belong to exactly one chapter. | Active draft |
| BR-CH-REV-003 | A chapter may have multiple editorial review records over time, especially across revision cycles. | Active draft |
| BR-CH-REV-004 | Each editorial review must be performed by one valid reviewer user. | Active draft |
| BR-CH-REV-005 | Only authorized Tantou Editors or approved review roles may create chapter editorial reviews. | Active draft |
| BR-CH-REV-006 | An editorial review decision must be one of `APPROVED`, `REVISION_REQUESTED`, or `CANCELLED`. | Active draft |
| BR-CH-REV-007 | If the review decision is `REVISION_REQUESTED` or `CANCELLED`, the review must include non-blank meaningful comments. | Active draft |
| BR-CH-REV-008 | A markup file is optional supporting feedback for chapter editorial review and must reference an existing `FileResource` when provided. | Active draft |
| BR-CH-REV-009 | Page-specific annotations are stored separately on page regions/page versions, while `ChapterEditorialReview` stores the final chapter-level review decision. | Active draft |
| BR-CH-REV-010 | Creating an editorial review should trigger a chapter status update according to the decision. | Active draft |
| BR-CH-REV-011 | If the decision is `APPROVED` and the chapter has no `planned_release_date`, the chapter status should become `APPROVED`. | Active draft |
| BR-CH-REV-011A | If the decision is `APPROVED` and the chapter already has a valid `planned_release_date`, the chapter status should become `SCHEDULED`. | Active draft |
| BR-CH-REV-012 | If the decision is `REVISION_REQUESTED`, the chapter status should become `REVISION_REQUESTED` and editing should be allowed again. | Active draft |
| BR-CH-REV-013 | If the decision is `CANCELLED`, the chapter status should become `CANCELLED`, and cancellation-specific consequences are handled by the Chapter Cancellation rules. | Active draft |
| BR-CH-REV-014 | Creating an editorial review should be recorded in the audit log. | Active draft |
| BR-CH-REV-015 | When chapter approval changes the status to `SCHEDULED`, the scheduling effect should be audited with the planned release date and old/new status values. | Active draft |

---

## 16. Chapter Cancellation

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-CH-CANCEL-001 | A chapter can be cancelled through an editorial review decision when the editor determines that the chapter should not proceed. | Active draft |
| BR-CH-CANCEL-002 | A cancelled chapter must not proceed to `SCHEDULED` or `RELEASED` status. | Active draft |
| BR-CH-CANCEL-003 | Cancelling a chapter must not automatically delete its pages, page versions, page regions, annotations, files, or review history. | Active draft |
| BR-CH-CANCEL-004 | If the chapter can still be fixed and resubmitted, the editor should use `REVISION_REQUESTED`, not `CANCELLED`. | Active draft |
| BR-CH-CANCEL-005 | Chapter cancellation without editorial review is not allowed in MVP; chapter cancellation must be recorded through a chapter editorial review decision. | Active draft |
| BR-CH-CANCEL-006 | A cancelled chapter is terminal for the current chapter attempt: it must not be edited, receive new page versions, be resubmitted for review, be approved, be scheduled, or be released. | Active draft |
| BR-CH-CANCEL-007 | A Mangaka may create a new replacement chapter draft using the same chapter number label after the previous chapter with that label has been cancelled. | Active draft |
| BR-CH-CANCEL-008 | The database should enforce chapter-label uniqueness with a filtered unique index for non-cancelled chapters, for example `(series_id, chapter_number_label) WHERE status_code <> N'CANCELLED'`, instead of a permanent unique constraint that includes cancelled chapters. | Active draft |
| BR-CH-CANCEL-009 | Replacement chapter drafts are new `Chapter` records; the MVP does not require a `replacement_of_chapter_id` relationship. | Active draft |
| BR-CH-CANCEL-010 | Cancelled chapter materials remain read-only historical reference. New pages and page versions for the redo work must belong to the new replacement chapter, so existing page-number uniqueness remains scoped within each chapter. | Active draft |

---


## 17. Publication Planning

### 17.1 Publication Frequency and Advisory Scheduling

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PUB-001 | Detailed publication planning is handled at chapter level using `Chapter.planned_release_date`, `Chapter.released_at_utc`, and `Chapter.status_code`; `SCHEDULED` is a chapter status, not a series status. | Active draft |
| BR-PUB-002 | Formal series-level publication policy history is outside MVP scope; the system only stores the current publication frequency on `Series`. | Active draft |
| BR-PUB-003 | A serialized series may have a current publication frequency of `WEEKLY`, `MONTHLY`, or `IRREGULAR`; the value may be `NULL` before the official release approach is decided. | Active draft |
| BR-PUB-004 | `IRREGULAR` means chapters are released when ready and do not follow a fixed weekly or monthly schedule. | Active draft |
| BR-PUB-005 | `publication_frequency_code` may be set by Mangaka while the series is in `PROPOSAL_DRAFT` as a proposed/preferred frequency; after board approval, the Editorial Board Chief controls the official frequency. | Active draft |
| BR-PUB-006 | `publication_frequency_code` is an advisory planning label for default date suggestions and communication, not a hard scheduling constraint. | Active draft |
| BR-PUB-007 | The MVP does not keep publication frequency history; only the current frequency is stored on `Series`. | Active draft |
| BR-PUB-008 | Chapter scheduling and release status must follow the Chapter status rules. | Active draft |
| BR-PUB-009 | Delayed or overdue chapters can be derived from `planned_release_date` and `released_at_utc`; the MVP does not need a separate delay status. | Active draft |
| BR-PUB-010 | When an Editorial Board Chief opens a `START_SERIALIZATION` poll, they must specify the publication frequency for the series as part of the poll setup. | Active draft |
| BR-PUB-011 | Mangaka may provide or update `Series.publication_frequency_code` only while the series is in `PROPOSAL_DRAFT`; before board approval, this value is treated as the Mangaka's proposed/preferred frequency and no separate desired-frequency column is required in MVP. | Active draft |
| BR-PUB-012 | After the series leaves `PROPOSAL_DRAFT`, Mangaka cannot directly change the official `Series.publication_frequency_code`. | Active draft |
| BR-PUB-013 | The publication frequency specified through the valid board serialization decision overrides the Mangaka's preference and becomes the official `Series.publication_frequency_code`. | Active draft |
| BR-PUB-015 | Editorial Board Chief may directly change `Series.publication_frequency_code` for a series after providing a required reason that must be written to the audit log. | Active draft |
| BR-PUB-016 | Mangaka and Tantou Editor scheduling collaboration is treated as an out-of-system team coordination process in MVP; the system records schedule changes and audit details but does not try to resolve interpersonal scheduling disputes. | Active draft |
| BR-PUB-017 | Active series contributors may be shown relevant contributor contact information such as display name and email for coordination, but contact visibility must remain limited to authorized contributors/workflow participants. | Active draft |

### 17.2 PublicationPeriod

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PUB-PERIOD-001 | `PublicationPeriod` represents business calendar periods used for ranking, reporting, and advisory schedule display. | Active draft |
| BR-PUB-PERIOD-002 | A `PublicationPeriod` has `period_name`, `period_type_code`, `period_start_date`, and `period_end_date`. | Active draft |
| BR-PUB-PERIOD-003 | Valid `PublicationPeriod.period_type_code` values are `WEEKLY`, `MONTHLY`, and `YEARLY`. | Active draft |
| BR-PUB-PERIOD-004 | Monthly publication periods start on the first calendar day of the month and end on the last calendar day of the month. | Active draft |
| BR-PUB-PERIOD-005 | Yearly publication periods start on January 1 and end on December 31. | Active draft |
| BR-PUB-PERIOD-006 | Weekly publication periods start on Monday and end on Sunday. | Active draft |
| BR-PUB-PERIOD-007 | A weekly publication period is assigned to the month that contains at least four days of that Monday-Sunday week. | Active draft |
| BR-PUB-PERIOD-008 | Weekly period names are generated from the owning month and the ordinal weekly period number within that owning month, such as `2026_JULY_WEEK1`. | Active draft |
| BR-PUB-PERIOD-009 | A weekly publication period may start in the previous month or end in the next month, but its `period_name` still follows the owning-month rule. | Active draft |
| BR-PUB-PERIOD-010 | `period_start_date` and `period_end_date` are business calendar dates, not raw UTC timestamp boundaries. | Active draft |
| BR-PUB-PERIOD-011 | Publication periods must not be used as hard blockers for normal chapter scheduling; they may support suggestions, calendars, reports, and ranking. | Active draft |

### 17.3 Publication Business Date and UTC Release Time

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PUB-DATE-001 | `PublicationPeriod` membership is determined by the publication business date, not by the raw UTC date. | Active draft |
| BR-PUB-DATE-002 | For scheduled chapters, the publication business date is usually `Chapter.planned_release_date`. | Active draft |
| BR-PUB-DATE-003 | For actually released chapters, the release business date is derived by converting `Chapter.released_at_utc` to the system publication timezone, currently Vietnam time UTC+7, then taking the `DATE` part. | Active draft |
| BR-PUB-DATE-004 | Ranking and publication-period reports must use the publication business date, not `CAST(released_at_utc AS DATE)` in UTC. | Active draft |
| BR-PUB-DATE-005 | `released_at_utc` records the exact actual release instant and should not be used directly as the calendar period date without publication-timezone conversion. | Active draft |
| BR-PUB-DATE-006 | When a chapter is released immediately and `planned_release_date` is missing, the system sets `planned_release_date` to the current publication business date so the planned and actual release dates remain understandable. | Active draft |
| BR-PUB-DATE-007 | When a chapter already has `planned_release_date`, releasing the chapter should preserve the planned date for planned-versus-actual comparison and set only `released_at_utc` as the actual release timestamp. | Active draft |

### 17.4 Advisory Schedule Suggestions and Hard Validation

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PUB-SCHEDULE-001 | The system may suggest planned release dates from `Series.publication_frequency_code`, but Mangaka and Tantou Editors may choose any planned release date on the current publication business date or later when the chapter status and permissions allow scheduling. | Active draft |
| BR-PUB-SCHEDULE-002 | For `WEEKLY` series, the default suggestion should usually be the same weekday in the next week when a reference date exists. | Active draft |
| BR-PUB-SCHEDULE-003 | For `MONTHLY` series, the default suggestion should usually be the same day number in the next month when possible, otherwise the last valid day of the next month. | Active draft |
| BR-PUB-SCHEDULE-004 | For `IRREGULAR` or `NULL` frequency, no strict default date is required; the UI may use a neutral suggestion such as today or the next available later date. | Active draft |
| BR-PUB-SCHEDULE-005 | The system must block planned release dates earlier than the current publication business date; today is valid. | Active draft |
| BR-PUB-SCHEDULE-006 | The system should warn, not block, when a chosen planned release date does not match the series' advisory frequency pattern. | Active draft |
| BR-PUB-SCHEDULE-007 | The system must allow multiple chapters from the same series to share the same planned release date when authorized users intentionally plan a bulk or catch-up release. | Active draft |
| BR-PUB-SCHEDULE-008 | Schedule setting and rescheduling must be audit-visible, including actor, old/new planned release date, old/new status when changed, and reason when provided. | Active draft |
| BR-PUB-SCHEDULE-009 | Scheduling must be blocked for terminal chapters such as `CANCELLED` and `RELEASED`. | Active draft |
| BR-PUB-SCHEDULE-010 | Schedule, reschedule, hold, return-from-hold, release, and bulk schedule/release actions should ask for user confirmation before changing chapter state. | Active draft |
| BR-PUB-SCHEDULE-011 | Schedule coordination issues between Mangaka and Editor are handled by the team outside the system in MVP; the system provides visibility and audit trail rather than conflict resolution. | Active draft |

### 17.5 Scheduled Chapter Lock, Reschedule, Hold, and Release

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-PUB-SCHEDULED-001 | When a chapter is `SCHEDULED`, Mangaka users cannot edit chapter content or perform page/content mutation workflows for that chapter. | Active draft |
| BR-PUB-SCHEDULED-002 | Page/content mutation workflows blocked for `SCHEDULED` and `ON_HOLD` chapters include page creation, page deletion, page-version upload, assistant task output submission that creates or changes page content, and other saved page/content changes. | Active draft |
| BR-PUB-SCHEDULED-003 | Mangaka and Tantou Editors may set or reschedule a chapter planned release date when the chapter status and permissions allow scheduling. | Active draft |
| BR-PUB-SCHEDULED-004 | Tantou Editors may place a `SCHEDULED` chapter `ON_HOLD` only with a non-blank operational or editorial reason. | Active draft |
| BR-PUB-SCHEDULED-005 | Moving a chapter to `ON_HOLD` suspends the previous release plan; the previous planned date should be preserved in audit details, while the active chapter should require a new planned release date on the current publication business date or later before returning to `SCHEDULED`. | Active draft |
| BR-PUB-SCHEDULED-006 | Returning a chapter from `ON_HOLD` to `SCHEDULED` requires setting a new planned release date on the current publication business date or later. | Active draft |
| BR-PUB-SCHEDULED-007 | Tantou Editors may release an eligible scheduled or approved chapter with confirmation; releasing sets `released_at_utc` to the current UTC time and sets `status_code = RELEASED`. | Active draft |
| BR-PUB-SCHEDULED-008 | If a chapter is released and `planned_release_date` is missing, the system sets it to the current publication business date; if it already exists, the system preserves it. | Active draft |
| BR-PUB-SCHEDULED-009 | Automatic movement of overdue `SCHEDULED` chapters to `ON_HOLD` is deferred; the MVP may show overdue warnings but should not perform automatic hold transitions unless a later task explicitly implements them. | Active draft |
| BR-PUB-SCHEDULED-010 | Release automation and public reader visibility remain outside this workflow until explicitly added. | Active draft |
| BR-PUB-SCHEDULED-011 | Bulk schedule, bulk hold, and bulk release workflows may be supported later; when implemented, they must require confirmation and write audit details for each affected chapter. | Active draft |
| BR-PUB-SCHEDULED-012 | Chapter release must be blocked when the parent series is `HIATUS`, `COMPLETED`, or `CANCELLED`; a `HIATUS` series must return to `SERIALIZED` before chapter release is allowed. | Active draft |
| BR-PUB-SCHEDULED-013 | While the parent series is `HIATUS`, authorized scheduling and rescheduling actions remain allowed when the chapter status and normal permissions allow them, because hiatus pauses release rather than production planning. | Active draft |

---

## 18. Ranking and Series Vote Input

### 18.1 Series Vote Input

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-SERIES-VOTE-001 | Series vote input in MVP is simulated/manual aggregated series-level performance data, not direct real-reader voting. | Active draft |
| BR-SERIES-VOTE-002 | `SeriesVoteInput` is tied to exactly one `PublicationPeriod` and one `Series`. | Active draft |
| BR-SERIES-VOTE-003 | A series may have at most one vote input for the same publication period. | Active draft |
| BR-SERIES-VOTE-004 | `rating_count` represents the number of rating/vote submissions for the series during that publication period only. | Active draft |
| BR-SERIES-VOTE-005 | `average_rating` represents the average score from the `rating_count` submissions for that publication period. | Active draft |
| BR-SERIES-VOTE-006 | `reading_count` represents the number of readers/views/follows reported for the series during that publication period. | Active draft |
| BR-SERIES-VOTE-007 | `rating_count` must be greater than zero. | Active draft |
| BR-SERIES-VOTE-008 | `reading_count` must be greater than zero. | Active draft |
| BR-SERIES-VOTE-009 | `rating_count` must not exceed `reading_count`. | Active draft |
| BR-SERIES-VOTE-010 | `average_rating` must be between 0 and 10. | Active draft |
| BR-SERIES-VOTE-011 | Weekly vote input is period-only; Week 2 input must not include Week 1 input. | Active draft |
| BR-SERIES-VOTE-012 | Monthly and yearly average ratings, when derived from lower-level period inputs, must use weighted averaging: `SUM(average_rating * rating_count) / SUM(rating_count)`. | Active draft |
| BR-SERIES-VOTE-013 | Series vote input should record the Editorial Board Member who entered the simulated/aggregated vote data and the UTC entry timestamp. | Active draft |
| BR-SERIES-VOTE-014 | Only Editorial Board Members or Editorial Board Chief users may enter simulated or aggregated series vote input in MVP. | Active draft |
| BR-SERIES-VOTE-015 | `data_source_note` may describe the report, tracking website, internal spreadsheet, or other evidence used to enter the series vote input. | Active draft |
| BR-SERIES-VOTE-016 | Manual MVP `SeriesVoteInput` creation/update is allowed for `WEEKLY` publication periods only. Monthly, yearly, or all-time ranking results are derived from weekly source evidence and must not require separately persisted manual aggregate `SeriesVoteInput` rows. | Active draft |

### 18.2 Dynamic Series Ranking

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-RANK-001 | Series ranking is calculated dynamically from `SeriesVoteInput` records joined to `PublicationPeriod` and `Series`; the MVP does not require `SeriesRankingSnapshot`. | Active draft |
| BR-RANK-002 | Ranking is partitioned by `publication_period_id`, so each publication period has its own rank list. | Active draft |
| BR-RANK-003 | The dynamic ranking view should expose period details, series identity, title/slug when needed for navigation, rating count, average rating, reading count, ranking score, and rank position. | Active draft |
| BR-RANK-004 | The MVP ranking score uses a weighted rating: `ranking_score = (v / (v + m)) * R + (m / (v + m)) * C`, where `R` is the series average rating, `v` is its rating count, `C` is the rating-count-weighted average rating of all eligible ranked series in the same effective ranking scope, and `m` is the median rating count of those eligible ranked series. | Active draft |
| BR-RANK-005 | Rank position is computed using `DENSE_RANK()` ordered by `ranking_score DESC` for the effective ranking scope. Series with the same `ranking_score` share the same rank position. For deterministic display only, tied rows may then be ordered by `average_rating DESC`, `rating_count DESC`, `reading_count DESC`, and `series_id ASC`; these secondary fields must not change `rank_position`. | Active draft |
| BR-RANK-006 | `ranking_score` and `rank_position` are derived values and should not be stored as normal duplicated columns unless later performance profiling proves caching is required. | Active draft |
| BR-RANK-007 | Ranking results do not automatically cancel a series. | Active draft |
| BR-RANK-008 | Ranking and cancellation-risk evidence may support board review, but any cancellation still requires the applicable board/editorial workflow decision. | Active draft |
| BR-RANK-009 | Completed series remain visible in dynamic rankings when `SeriesVoteInput` exists for the selected publication period; ranking views must not hide a series only because `Series.status_code = COMPLETED`. | Active draft |
| BR-RANK-010 | For a direct publication-period ranking, `C` must be calculated as `SUM(average_rating * rating_count) / SUM(rating_count)` across eligible ranked series in that same period, and `m` must be the median `rating_count` across those eligible ranked series. | Active draft |
| BR-RANK-011 | `reading_count` is popularity/readership evidence and must not directly increase the weighted `ranking_score`. It may be used only as a deterministic display-order field among rows that already share the same rank; it must not break a ranking-score tie or change `rank_position`. | Active draft |
| BR-RANK-012 | When a broader ranking scope such as monthly, yearly, or all-time is derived from weekly inputs, the system must first aggregate weekly source evidence by series, then recalculate that broader scope's own `R`, `v`, `C`, and `m`; it must not average weekly `ranking_score` values or reuse weekly `C`/`m`. | Active draft |
| BR-RANK-013 | All `ACTIVE` authenticated system roles, including Assistants, may view dynamic ranking results. Creating or updating manual `SeriesVoteInput` remains restricted to Editorial Board Member and Editorial Board Chief roles. | Active draft |


---

---

## 19. Notifications

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-NOTIF-001 | A notification must be addressed to exactly one recipient user. | Active draft |
| BR-NOTIF-002 | `manga.Notification` records are in-app MVP messages. A workflow may also send a separate email when explicitly required; account approval is the current approved case where an in-app `ACCOUNT_APPROVED` notification and an approval email are both sent. | Active draft |
| BR-NOTIF-003 | A notification may optionally reference a related business entity such as a series, chapter, page task, proposal, board poll, or ranking result. | Active draft |
| BR-NOTIF-004 | If `related_entity_type` is provided, `related_entity_id` must also be provided, and vice versa. | Active draft |
| BR-NOTIF-005 | A notification is considered unread when `read_at_utc IS NULL`. | Active draft |
| BR-NOTIF-006 | When a user reads a notification, the system records `read_at_utc`. | Active draft |
| BR-NOTIF-007 | A completed weekly ranking result is a failed ranking week for `RANKING_WARNING` only when **both** conditions are true: `ranking_score < 6.5` and the series is in the bottom 25% of ranked series for that same weekly period. The bottom group size is `CEILING(total_ranked_series * 0.25)`, and each evaluated week must contain at least 4 ranked series. The `6.5` value is the approved MVP low-score baseline for warning evaluation, not a statistical guarantee of cancellation. | Active draft |
| BR-NOTIF-008 | A `RANKING_WARNING` high-risk condition exists only when a `SERIALIZED` or `HIATUS` series has a failed ranking week in at least 2 of its latest 3 consecutive completed weekly publication periods, the latest completed week is also a failed ranking week, the series has ranking input in all 3 periods, and each evaluated period contains at least 4 ranked series. | Active draft |
| BR-NOTIF-009 | `TASK_ASSIGNMENT` is sent to the assigned Assistant when a page task is created. Quick Select creates one assignment notification per created task. On reassignment, the original Assistant receives a `TASK_ASSIGNMENT` notification titled as a reassignment/cancellation notice, including the required reassignment reason and linked to the original task, while the replacement Assistant receives a `TASK_ASSIGNMENT` notification linked to the replacement task. | Active draft |
| BR-NOTIF-010 | `PROPOSAL_DECISION` is created when a Tantou Editor records Request Revision, Pass To Board, or Cancel Proposal. Recipients are the distinct active Mangaka contributors of the affected series, and the notification relates to the affected `SeriesProposal`. | Active draft |
| BR-NOTIF-011 | `BOARD_POLL` is created when an Editorial Board Chief opens a real board poll. Each active user whose exact role is Editorial Board Member receives one notification; the initiating Chief is excluded and duplicate recipient IDs must be removed. | Active draft |
| BR-NOTIF-012 | `PROPOSAL_REVIEW` is created when a Mangaka submits a proposal for editorial review. Recipients are the distinct active Tantou Editor contributors of that exact series; unrelated Tantou Editors must not receive it. The notification relates to the submitted `SeriesProposal`. | Active draft |
| BR-NOTIF-013 | `CHAPTER_REVIEW` is created when a Mangaka submits a `DRAFT` or `REVISION_REQUESTED` chapter and the chapter transitions to `UNDER_REVIEW`. Recipients are the distinct active Tantou Editor contributors of that exact series; unrelated Tantou Editors must not receive it. The notification relates to the submitted `Chapter`. | Active draft |
| BR-NOTIF-014 | Notifications are user-facing awareness records and must not be treated as the authoritative audit trail. | Active draft |
| BR-NOTIF-015 | Important workflow actions that create notifications should still be recorded in the audit log when auditability is required. | Active draft |
| BR-NOTIF-016 | `TASK_REVIEW` is created when an Assistant submits task work and the task transitions from `ASSIGNED` to `UNDER_REVIEW`. Recipients are the distinct active Mangaka contributors of that exact series, and the notification relates to the submitted `ChapterPageTask`. | Active draft |
| BR-NOTIF-017 | `CHAPTER_DECISION` is created when a Tantou Editor records an Approved, Revision Requested, or Cancelled chapter editorial decision. Recipients are the distinct active Mangaka contributors of the affected series, and the notification relates to the affected `Chapter`. | Active draft |
| BR-NOTIF-018 | `BOARD_DECISION` is created when a board poll is closed with an `APPROVED`, `REJECTED`, or `NO_DECISION` outcome, and also when the Editorial Board Chief manually cancels the poll. The notification content must reflect the actual closed/no-decision/cancelled outcome and relate to the affected `SeriesBoardPoll`. | Active draft |
| BR-NOTIF-019 | `BOARD_DECISION` recipients are all distinct active contributors of the exact affected series, regardless of contributor role. Unrelated users must not receive the notification. | Active draft |
| BR-NOTIF-020 | `PUBLICATION_SCHEDULE` is created when a chapter transitions from a non-`SCHEDULED` status into `SCHEDULED`, or when a chapter already in `SCHEDULED` is rescheduled to a different normalized `planned_release_date`. | Active draft |
| BR-NOTIF-021 | `PUBLICATION_SCHEDULE` must not be created when a `DRAFT`, `REVISION_REQUESTED`, or `UNDER_REVIEW` chapter only receives or changes a planned release date while remaining in that non-`SCHEDULED` status, or when a `SCHEDULED` chapter is saved with the same normalized planned release date. | Active draft |
| BR-NOTIF-022 | `PUBLICATION_SCHEDULE` recipients are all other distinct active contributors of the exact affected series, excluding the initiating actor who performed the scheduling or rescheduling action. | Active draft |
| BR-NOTIF-023 | `ACCOUNT_APPROVED` is created for the approved user when an Admin approves/activates a pending account. | Active draft |
| BR-NOTIF-024 | When an Admin approves/activates a pending account, the system also sends an account-approval email to the approved user's email address in addition to the in-app `ACCOUNT_APPROVED` notification. | Active draft |
| BR-NOTIF-025 | `SYSTEM_MESSAGE` is a generic/reserved notification type and must not be used as a substitute when a more specific approved notification type applies. No new automatic `SYSTEM_MESSAGE` trigger should be inferred without an explicitly defined workflow. | Active draft |
| BR-NOTIF-026 | When a notification rule uses active series contributors, recipients must belong to the exact affected series, have an active contributor relationship (`end_date IS NULL`), have user status `ACTIVE`, and be deduplicated by user ID. | Active draft |
| BR-NOTIF-027 | `RANKING_WARNING` recipients are all distinct active contributors of the exact affected series, regardless of contributor role, using the active-contributor definition in BR-NOTIF-026. | Active draft |
| BR-NOTIF-028 | `RANKING_WARNING` is evaluated from completed `WEEKLY` periods only. Current/incomplete weeks must not trigger it, and monthly/yearly/all-time reporting views must not independently create additional ranking warnings. | Active draft |
| BR-NOTIF-029 | Re-evaluating the same affected series and latest completed weekly period must be idempotent: the system must not create more than one `RANKING_WARNING` per recipient for that series/evaluated week. | Active draft |
| BR-NOTIF-030 | A `RANKING_WARNING` is advisory only and must not automatically change series status, pause/cancel a series, or open a `CANCEL_SERIALIZATION` poll. Normal board workflow remains required for cancellation. | Active draft |
| BR-NOTIF-031 | Because ranking risk is derived from multiple weekly periods, `RANKING_WARNING` should relate to the affected `Series`; Bell navigation should open the existing ranking/series context for that series. | Active draft |
| BR-NOTIF-032 | Completed or cancelled series may remain visible in ranking history, but new cancellation-risk warnings apply only while the series is currently `SERIALIZED` or `HIATUS`. | Active draft |

---

## 20. Status History and Auditability

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-HIST-001 | MVP does not use separate status-history tables for every workflow entity. | Active draft |
| BR-HIST-002 | The current workflow state of an entity is stored directly on the main entity using `status_code`. | Active draft |
| BR-HIST-003 | Important workflow events should be represented through existing domain records where applicable, such as proposal records, board polls, board votes, chapter reviews, page versions, task records, ranking results, and notifications. | Active draft |
| BR-HIST-004 | Audit logs should record important workflow actions that require traceability, such as approvals, cancellations, status changes, task changes, board poll actions, and account actions. | Active draft |
| BR-HIST-005 | Notifications are used to inform actors of important events, not as the authoritative status history. | Active draft |
| BR-HIST-006 | `updated_at_utc` represents the latest update time for operational display and should not be treated as a complete status transition timeline. | Active draft |
| BR-HIST-007 | Specific workflow timestamps, such as `submitted_at_utc`, `reviewed_at_utc`, `entered_at_utc`, `created_at_utc`, and `released_at_utc`, should be used where they describe meaningful business events. | Active draft |
| BR-HIST-008 | Detailed status transition history tables are future scope unless specifically required for audit demonstration or advanced workflow analytics. | Active draft |

---
---
