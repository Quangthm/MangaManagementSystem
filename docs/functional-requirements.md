# Manga Creation Workflow and Publishing Management System — Revised Functional Requirements

**Document purpose:** Cleaned and revised functional requirements based on the verified business rules Markdown file.  
**Writing standard:** Requirements use the technical requirement style: **“The system shall…”**  
**MVP alignment note:** These requirements remove earlier conflicting assumptions such as `SeriesStatusHistory`, `ChapterSubmission`, `SeriesBoardDecision`, `RegionTranslation`, persistent AI job history, and direct annotation coordinates.

> **Latest alignment update — 2026-07-04:** This version replaces strict publication-period schedule validation with advisory chapter-level publication scheduling. `Series.publication_frequency_code` now provides default suggestions and warnings only. Authorized Mangaka and Tantou Editors may schedule/reschedule future planned release dates; Editors handle hold/release enforcement; held chapters require a new planned date to return to `SCHEDULED`; and auto-hold/release automation remain deferred.

---

## 3. Functional Requirements

---

## 3.1 AI-Assisted Translation

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-TRANS-001 | The system shall provide AI-assisted translation as an editing aid, not as a separate persistent translation workflow. | BR-TRANS-001 |
| FR-TRANS-002 | The system shall allow AI-assisted translation suggestions for detected text regions on manga page versions. | BR-TRANS-008 |
| FR-TRANS-003 | The system shall allow users to review, edit, override, and approve AI/OCR translation suggestions before saving the final translated page. | BR-TRANS-006, BR-TRANS-009 |
| FR-TRANS-004 | The system shall save the final translated page as a new `ChapterPageVersion`. | BR-TRANS-002 |
| FR-TRANS-005 | The system shall upload translated or edited page files as new page versions instead of overwriting previous page files. | BR-TRANS-004 |
| FR-TRANS-006 | The system shall preserve old page versions after a translated or edited page version is created. | BR-TRANS-005 |
| FR-TRANS-007 | The system shall treat the saved translated page file as the authoritative result of the translation/editing process. | BR-TRANS-007 |
| FR-TRANS-008 | The system shall not require structured rows for every OCR text value, AI translation suggestion, or manually edited translation in MVP. | BR-TRANS-003 |
| FR-TRANS-009 | The system shall not guarantee fully automatic manga localization in MVP. | BR-TRANS-008 |
| FR-TRANS-010 | The system shall make AI-assisted translation tools available to any Authorized Page Workspace User who has access to the relevant chapter/page workspace. | BR-TRANS-010, BR-WORKSPACE-006 |

---

## 3.2 Page Region and AI Detection

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-REG-001 | The system shall allow page regions to be created manually or suggested by AI. | BR-REG-006 |
| FR-REG-002 | The system shall require each `PageRegion` to belong to exactly one `ChapterPageVersion`. | BR-REG-001 |
| FR-REG-003 | The system shall restrict `PageRegion.type_code` to `PANEL`, `SPEECH_BUBBLE`, `CHARACTER`, `SFX_TEXT`, `BACKGROUND`, `FULL_PAGE`, or `OTHER`. | BR-REG-002 |
| FR-REG-004 | The system shall store page region coordinates as rectangular bounding boxes using `x`, `y`, `width`, and `height`. | BR-REG-003 |
| FR-REG-005 | The system shall require each page region width and height to be positive. | BR-REG-004 |
| FR-REG-006 | The system shall store page region coordinates relative to the original uploaded page image dimensions. | BR-REG-010 |
| FR-REG-007 | The system shall restrict `PageRegion.source_type` to `AI` or `MANUAL`. | BR-REG-005 |
| FR-REG-008 | The system shall change an AI-generated page region to `MANUAL` when a user adjusts the region. | BR-REG-007 |
| FR-REG-009 | The system shall clear or prevent AI confidence scores for manual page regions. | BR-REG-008 |
| FR-REG-010 | The system shall require stored AI confidence scores to be between 0 and 1. | BR-REG-009 |
| FR-REG-011 | The system shall represent character regions as bounding boxes in MVP, not exact outlines or masks. | BR-REG-011 |
| FR-REG-012 | The system shall keep page regions linked to the page version where they were created. | BR-REG-012 |
| FR-REG-013 | The system shall not automatically move page regions to newer page versions. | BR-REG-012 |
| FR-REG-014 | The system shall store accepted AI detection output directly as `PageRegion` records. | BR-REG-013 |
| FR-REG-015 | The system shall not require persistent AI job execution history for MVP. | BR-REG-013 |
| FR-REG-016 | The system shall prevent AI detection results from automatically approving or rejecting manga pages or chapters. | BR-REG-014 |
| FR-REG-017 | The system shall require human review before AI detection results are used in workflow decisions. | BR-REG-015 |
| FR-REG-018 | The system shall allow text-related page regions to store optional OCR-detected original text. | BR-REG-016 |
| FR-REG-019 | The system shall treat `PageRegion.original_text` as text detected from the original page image, not final translated text. | BR-REG-017 |
| FR-REG-020 | The system shall use `PageRegion` for translation/OCR support without storing final translated content as region translation rows. | BR-REG-018 |
| FR-REG-021 | The system shall load existing saved `PageRegion` records when a user opens the segmentation tool for a page version. | BR-REG-019 |
| FR-REG-022 | The system shall allow authorized users to run AI-assisted segmentation even when saved `PageRegion` records already exist for the same page version. | BR-REG-020 |
| FR-REG-023 | The system shall show newly AI-detected regions as temporary suggestions until the user chooses which regions to save. | BR-REG-021 |
| FR-REG-024 | The system shall compare newly AI-detected regions against existing saved `PageRegion` records for the same page version before saving new regions. | BR-REG-022 |
| FR-REG-025 | The system shall prevent or warn against saving duplicate AI-detected regions that substantially overlap an existing saved region of the same type. | BR-REG-023 |
| FR-REG-026 | The system shall allow users to save non-duplicate AI-detected regions as `PageRegion` records. | BR-REG-024 |
| FR-REG-027 | The system shall allow saved `PageRegion` records to be reused for annotation, task assignment, translation/OCR review, and segmentation display. | BR-REG-025 |
| FR-REG-028 | The system shall allow authorized users to adjust saved page region coordinates, labels, types, and original text when region editing is permitted. | BR-REG-026 |
| FR-REG-029 | The system shall record `updated_at_utc` and `updated_by_user_id` when a saved `PageRegion` is modified. | BR-REG-027 |
| FR-REG-030 | The system shall make AI-assisted segmentation tools available to all Authorized Page Workspace Users who have access to the relevant chapter/page version. | BR-REG-028, BR-WORKSPACE-006 |
| FR-REG-031 | The system shall allow hard deletion of a `PageRegion` only when the region is not connected to any annotation, task, or other workflow record that depends on the region. | BR-REG-033 |
| FR-REG-032 | The system shall block normal user deletion of `PageRegion` records that are connected to annotations or tasks. | BR-REG-034 |
| FR-REG-033 | The system shall preserve task-linked and annotation-linked page regions for traceability instead of deleting them. | BR-REG-034 |

---

## 3.3 File Resource and Cloudinary

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-FILE-001 | The system shall store actual uploaded media files in Cloudinary. | BR-FILE-001 |
| FR-FILE-002 | The system shall store file metadata and file relationships in SQL Server through `manga.FileResource`. | BR-FILE-001, BR-FILE-002 |
| FR-FILE-003 | The system shall require uploaded or generated business files to be referenced through `manga.FileResource` instead of raw Cloudinary URLs. | BR-FILE-002, BR-FILE-007 |
| FR-FILE-004 | The system shall reference chapter page-version files, proposal files, series cover images, user avatars, user portfolios, and editorial attachment/markup files through `FileResource`. | BR-FILE-002, BR-FILE-007 |
| FR-FILE-005 | The system shall treat a file resource as active only when `deleted_at_utc IS NULL`. | BR-FILE-004 |
| FR-FILE-006 | The system shall exclude deleted file resources from normal application queries unless the user is viewing historical or audit data. | BR-FILE-004 |
| FR-FILE-007 | The system shall delete Cloudinary assets through the application workflow instead of requiring manual deletion from the Cloudinary console. | BR-FILE-003 |
| FR-FILE-008 | The system shall allow a user avatar file to be uploaded as a `FileResource`. | BR-FILE-005 |
| FR-FILE-009 | The system shall require user avatar files to use `file_purpose_code = USER_AVATAR`. | BR-FILE-006 |
| FR-FILE-010 | The system shall display a safe placeholder when a referenced file is unavailable or deleted in a normal UI context. | BR-FILE-008 |
| FR-FILE-011 | The system shall require files used as `ChapterPageVersion` content to use `file_purpose_code = CHAPTER_PAGE_VERSION`. | BR-FILE-009 |
| FR-FILE-013 | The system shall store a required `sha256_hash` for each `FileResource`, calculated from the exact uploaded file bytes before file metadata is saved. | BR-FILE-011 |
| FR-FILE-014 | The system shall allow `sha256_hash` to support file integrity checking, duplicate detection, and audit traceability without enforcing global file uniqueness. | BR-FILE-012 |
| FR-FILE-015 | The system may optionally check for active `FileResource` records with the same `sha256_hash` before saving a new file resource. | BR-FILE-013 |
| FR-FILE-016 | The system may optionally show advisory duplicate-file warnings for repeated registration portfolio, proposal, cover, page-version, editorial attachment, or avatar files. | BR-FILE-014, BR-FILE-015 |
| FR-FILE-017 | The system shall not require every duplicate-file warning UI to be implemented in MVP, provided that `sha256_hash` is still stored for future duplicate detection, integrity checking, and audit traceability. | BR-FILE-016 |
| FR-FILE-018 | The system shall validate uploaded file extensions and content types according to the selected file purpose code before uploading to Cloudinary or saving file metadata. | BR-FILE-017 |
| FR-FILE-019 | The system shall restrict `SERIES_PROPOSAL` uploads to `.pdf`, `.doc`, and `.docx` proposal document files in MVP. | BR-FILE-018 |
| FR-FILE-020 | The system shall restrict image-only file purposes, including `SERIES_COVER`, `CHAPTER_PAGE_VERSION`, and `USER_AVATAR`, to `.jpg`, `.jpeg`, `.png`, and `.webp` in MVP. | BR-FILE-019 |
| FR-FILE-021 | The system shall allow mixed document/image purposes, including `EDITORIAL_ATTACHMENT` and `REGISTRATION_PORTFOLIO`, to use `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, or `.webp` in MVP. | BR-FILE-020 |

### MVP File Purpose Upload Format Matrix

| File purpose code | Allowed extensions | Allowed content types | Cloudinary resource type | Notes |
|---|---|---|---|---|
| `SERIES_PROPOSAL` | `.pdf`, `.doc`, `.docx` | `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `raw` | Formal series proposal documents only. Markdown, plain text, and image files are not accepted for proposal submission in MVP. |
| `SERIES_COVER` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Series cover image. The Web draft UI may upload the cropped `1000×1500` PNG result as the actual cover file. |
| `CHAPTER_PAGE_VERSION` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Official manga page image/version output. |
| `EDITORIAL_ATTACHMENT` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Editorial markup, review attachments, or supporting screenshots/documents. |
| `REGISTRATION_PORTFOLIO` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Optional portfolio submitted for account approval/profile review. |
| `USER_AVATAR` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | User profile/avatar image. |


---

## 3.4 Users and Accounts

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-USER-001 | The system shall assign exactly one MVP system role to each user account. | BR-USER-001 |
| FR-USER-002 | The system shall create new registered users with `PENDING_APPROVAL` status by default. | BR-USER-002 |
| FR-USER-003 | The system shall prevent `PENDING_APPROVAL` users from accessing protected workspace functions. | BR-USER-003 |
| FR-USER-004 | The system shall allow Admin users to activate pending users by changing their status to `ACTIVE`. | BR-USER-004 |
| FR-USER-005 | The system shall allow Admin users to disable accounts by changing their status to `DISABLED`. | BR-USER-005 |
| FR-USER-006 | The system shall prevent disabled accounts from logging in. | BR-USER-006 |
| FR-USER-007 | The system shall allow a user account to reference an optional avatar file through `FileResource`. | BR-USER-007, BR-USER-009 |
| FR-USER-008 | The system shall allow a user account or registration profile to reference an optional portfolio file through `FileResource`. | BR-USER-008, BR-USER-009 |
| FR-USER-009 | The system shall record registration approval/rejection history through current user status and audit logs instead of a separate registration request table. | BR-USER-010 |
| FR-USER-010 | The system shall allow Admin users to reject pending users by changing their status to `REJECTED`. | BR-USER-011 |
| FR-USER-011 | The system shall prevent rejected accounts from logging in. | BR-USER-012 |
| FR-USER-012 | The system shall keep rejected users' email and username reserved in MVP. | BR-USER-012 |
| FR-USER-013 | The system shall store a `display_name` for each user account. | BR-USER-013 |
| FR-USER-014 | The system shall use `display_name` for user-facing identity display in contributor lists, task assignments, annotations, notifications, board votes, and audit screens. | BR-USER-014 |
| FR-USER-015 | The system shall keep `username` as the login/system account identifier instead of using `display_name` for login. | BR-USER-015 |
| FR-USER-016 | The system shall allow multiple users to have the same `display_name`. | BR-USER-016 |
| FR-USER-017 | The system shall default `display_name` to `username` when no display name is provided during registration or external login. | BR-USER-017 |
| FR-USER-018 | The system shall allow authenticated users to update their own `display_name` without requiring their account password. | BR-USER-018 |
| FR-USER-019 | The system shall prevent a display name update from changing `username`, `email`, role, account status, password, or approval state. | BR-USER-019 |
| FR-USER-020 | The system shall record display name changes in the audit log. | BR-USER-020 |
| FR-USER-021 | The system shall reject empty or whitespace-only display names after trimming. | BR-USER-021 |
| FR-USER-022 | The system shall allow an authenticated user to update their own avatar and portfolio file through the profile workflow. | BR-USER-022 |
---

## 3.5 Series

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-SERIES-001 | The system shall allow active Mangaka users to create series draft profiles. | BR-SERIES-001, BR-SERIES-002, BR-SERIES-003, BR-SERIES-015 |
| FR-SERIES-002 | The system shall use `series_id` as the stable internal identifier for each series and shall not require a separate a separate human-readable business identifier field in MVP. | BR-SERIES-001 |
| FR-SERIES-003 | The system shall require each series to have a unique URL slug. | BR-SERIES-002 |
| FR-SERIES-004 | The system shall restrict each series to one approved lifecycle status value. | BR-SERIES-003 |
| FR-SERIES-005 | The system shall update a series to `SERIALIZED` after the proposal, editorial, and board approval workflow accepts the series into production/publication. | BR-SERIES-004 |
| FR-SERIES-006 | The system shall require each series to declare one primary content language. | BR-SERIES-005 |
| FR-SERIES-007 | The system shall manage series genres through `manga.Genre` and `manga.SeriesGenre`, allowing a series to have multiple genres. | BR-SERIES-006 |
| FR-SERIES-007A | The system shall manage series tags through `manga.Tag` and `manga.SeriesTag`, allowing a series to have multiple tags. | BR-SERIES-006A |
| FR-SERIES-007B | The system shall treat genres as broad story categories and tags as more specific tropes, themes, settings, character traits, source/context labels, or content descriptors. | BR-SERIES-006B |
| FR-SERIES-007C | The system shall treat genres and tags as current series metadata rather than proposal-history snapshot records in MVP. | BR-SERIES-006C |
| FR-SERIES-008 | The system shall allow a series to reference an optional current cover image through `FileResource`. | BR-SERIES-007 |
| FR-SERIES-009 | The system shall require series cover images to use the `SERIES_COVER` file purpose when provided. | BR-SERIES-007 |
| FR-SERIES-009A | The system shall allow Mangaka users to crop a selected series cover image in the browser before upload when the Web UI cropper is available. | BR-SERIES-007A |
| FR-SERIES-009B | The system shall lock the MVP series cover crop area to a 2:3 portrait ratio. | BR-SERIES-007C |
| FR-SERIES-009C | The system shall upload the cropped series cover image as the actual `SERIES_COVER` file instead of uploading the original selected source image. | BR-SERIES-007A, BR-SERIES-007B |
| FR-SERIES-009D | The system shall not require crop metadata or original/cropped dual-file storage for `SERIES_COVER` in MVP. | BR-SERIES-007B |
| FR-SERIES-009E | The current Web cropper shall output the MVP series cover crop as a `1000×1500` image file. | BR-SERIES-007C |
| FR-SERIES-009F | The system should warn users when the selected source image is smaller than the recommended cover output size and may look blurry after upscaling. | BR-SERIES-007D |
| FR-SERIES-010 | The system shall allow a series to optionally reference another series as its source version. | BR-SERIES-008 |
| FR-SERIES-011 | The system shall prevent a series from referencing itself as its source series. | BR-SERIES-009 |
| FR-SERIES-012 | The system shall manage series ownership and contributor membership through `SeriesContributor` instead of `lead_mangaka_user_id` on `Series`. | BR-SERIES-010 |
| FR-SERIES-013 | The system shall allow a series to have multiple contributors who can participate in managing or editing series information. | BR-SERIES-011 |
| FR-SERIES-014 | The system shall record both `updated_at_utc` and `updated_by_user_id` when editable series profile metadata is changed. | BR-SERIES-012 |
| FR-SERIES-015 | The system shall use `Series.updated_at_utc` and `Series.updated_by_user_id` for profile metadata edits, not as full status transition history. | BR-SERIES-013 |
| FR-SERIES-016 | The system shall prevent incomplete update metadata where only `updated_at_utc` or only `updated_by_user_id` is provided. | BR-SERIES-014 |
| FR-SERIES-017 | The system shall prevent Admin users from creating or updating series drafts as normal manga business workflow. | BR-SERIES-016 |
| FR-SERIES-018 | The system shall manage draft series internally by `series_id` instead of requiring public slug routing during draft workflow. | BR-SERIES-017 |
| FR-SERIES-019 | The system shall generate a unique URL slug from the series title when a Mangaka creates a series draft. | BR-SERIES-018 |
| FR-SERIES-020 | The system shall regenerate and update the slug when the Mangaka changes the title and saves while the series is still `PROPOSAL_DRAFT`. | BR-SERIES-019 |
| FR-SERIES-021 | The system shall lock the slug from normal update after the series leaves `PROPOSAL_DRAFT`. | BR-SERIES-020 |
| FR-SERIES-022 | The system shall expose `/series/{slug}` as the stable main series URL after the series becomes `SERIALIZED`. | BR-SERIES-021 |
| FR-SERIES-023 | The system shall allow normal Mangaka updates to title, synopsis, genres, tags, cover, content language, source series, publication frequency, and regenerate slug from title only while the series is in `PROPOSAL_DRAFT`. | BR-SERIES-022 |
| FR-SERIES-024 | The system shall reject normal series profile update attempts after the series leaves `PROPOSAL_DRAFT`, unless a separate controlled workflow allows the specific change. | BR-SERIES-023 |
| FR-SERIES-025 | The system shall allow Mangaka production work after serialization through chapters, pages, page versions, regions, tasks, and the authorized chapter workspace rather than normal series profile editing. | BR-SERIES-024 |
| FR-SERIES-026 | The system shall create an active `SeriesContributor` record for the Mangaka who creates a new series draft in the same backend workflow or transaction that creates the `Series` record. | BR-SERIES-025 |
---

## 3.6 Series Contributors

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-SC-001 | The system shall allow authorized users to add contributors to a series. | BR-SC-001, BR-SC-003 |
| FR-SC-002 | The system shall require each `SeriesContributor` record to link exactly one user to exactly one series. | BR-SC-001 |
| FR-SC-003 | The system shall determine a contributor’s broad role from `auth.Users.role_id`. | BR-SC-002 |
| FR-SC-004 | The system shall not require contributor specialization records for MVP. | BR-SC-002 |
| FR-SC-005 | The system shall prevent a user from being an active contributor to the same series more than once at the same time. | BR-SC-004 |
| FR-SC-006 | The system shall treat a contributor as active when `end_date IS NULL`. | BR-SC-005 |
| FR-SC-007 | The system shall preserve historical contributor rows after a contributor leaves a series. | BR-SC-006 |
| FR-SC-008 | The system shall prevent a contributor `end_date` from being earlier than the contributor `start_date`. | BR-SC-007 |
| FR-SC-009 | The system shall require at least one active Mangaka contributor before a series is submitted from `PROPOSAL_DRAFT` into `UNDER_EDITORIAL_REVIEW`, but shall not require an active Tantou Editor contributor for first proposal submission. | BR-SC-008 |

---

## 3.7 Series Proposal

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-PROP-001 | The system shall create a `SeriesProposal` row only when a proposal is formally submitted for editorial review. | BR-PROP-001, BR-PROP-006 |
| FR-PROP-002 | The system shall represent each formal proposal submission version as one `SeriesProposal` row. | BR-PROP-001 |
| FR-PROP-003 | The system shall allow a series to have multiple proposal versions over time. | BR-PROP-002 |
| FR-PROP-004 | The system shall require proposal version numbers to be positive and unique within the same series. | BR-PROP-003 |
| FR-PROP-005 | The system shall preserve submission-time snapshots of proposal title, synopsis, and proposal file. | BR-PROP-004 |
| FR-PROP-005A | The system shall not require `SeriesProposal` to snapshot genres, tags, or the current series cover file in MVP. | BR-PROP-004A |
| FR-PROP-005B | The system shall allow proposal review screens to display current genres, tags, and cover from locked series metadata rather than from proposal snapshot tables. | BR-PROP-004A |
| FR-PROP-006 | The system shall require each submitted proposal to include a proposal file stored as a `FileResource` with purpose `SERIES_PROPOSAL`. | BR-PROP-005 |
| FR-PROP-007 | The system shall prevent direct editing of submitted proposal snapshot fields after the `SeriesProposal` row is created. | BR-PROP-007 |
| FR-PROP-008 | The system shall require corrected proposal content to be submitted as a new proposal version when revision is requested. | BR-PROP-008 |
| FR-PROP-009 | The system shall store proposal status to reflect the current review stage of the submitted proposal package. | BR-PROP-009 |
| FR-PROP-010 | The system shall allow a proposal to store a withdrawal timestamp only when its status is `WITHDRAWN`. | BR-PROP-010 |
| FR-PROP-011 | The system shall allow a withdrawn proposal to exist without editorial review metadata if it was withdrawn before editorial review completion. | BR-PROP-011 |
| FR-PROP-012 | The system shall store editorial review information directly in `SeriesProposal` for MVP. | BR-PROP-012 |
| FR-PROP-013 | The system shall prevent more than one editorial review decision from being recorded for the same submitted proposal version. | BR-PROP-012 |
| FR-PROP-014 | The system shall use `UNDER_BOARD_REVIEW` to indicate that a proposal passed editorial review and is waiting for board voting/decision. | BR-PROP-013 |
| FR-PROP-015 | The system shall mark a proposal as `APPROVED` only after board approval. | BR-PROP-014 |
| FR-PROP-016 | The system shall require non-empty comments when an editor requests revision, and shall allow an optional markup file for revision feedback. | BR-PROP-015 |
| FR-PROP-016A | The system shall require both non-empty comments and a markup file when an editor cancels a proposal during editorial review. | BR-PROP-015A |
| FR-PROP-017 | The system shall handle board rejection or board cancellation reasons through board poll and board vote records instead of editorial review comments. | BR-PROP-016 |
| FR-PROP-018 | The system shall allow proposal lists to be retrieved by series, status, submitter, and reviewer. | BR-PROP-017 |
| FR-PROP-019 | The system shall allow the latest proposal version for a series to be retrieved. | BR-PROP-018 |
| FR-PROP-020 | The system shall allow editorial and board queues to be filtered by proposal status. | BR-PROP-019 |
| FR-PROP-020A | The system shall allow Mangaka proposal tracking lists to search by proposal or series title. | BR-PROP-017, BR-PROP-019 |
| FR-PROP-020B | The system shall allow Mangaka proposal tracking lists to filter by selected current series genres and tags. | BR-PROP-017, BR-PROP-019 |
| FR-PROP-020C | The system shall keep Mangaka proposal text search separate from genre/tag filters. | BR-PROP-017, BR-PROP-019 |
| FR-PROP-021 | The system shall allow reviewed proposal records to be searched by reviewer for admin/editor tracking. | BR-PROP-020 |
| FR-PROP-022 | The system shall treat the proposal file as supporting material for editor and board evaluation. | BR-PROP-021 |
| FR-PROP-023 | The system shall not require a fixed minimum number of completed manga pages for proposal submission in MVP. | BR-PROP-022 |
| FR-PROP-024 | The system shall treat any minimum page/sample requirement as editorial policy outside MVP database constraints. | BR-PROP-023 |
| FR-PROP-025 | The system shall make newly submitted proposals visible in the editorial review queue for active Tantou Editors. | BR-PROP-024 |
---

## 3.8 Board Poll

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-BOARD-POLL-001 | The system shall allow Editorial Board Chief users to create board polls for `START_SERIALIZATION` and `CANCEL_SERIALIZATION`. | BR-BOARD-POLL-001 |
| FR-BOARD-POLL-002 | The system shall allow a `START_SERIALIZATION` poll only when `Series.status_code = UNDER_BOARD_REVIEW`. | BR-BOARD-POLL-002 |
| FR-BOARD-POLL-003 | The system shall allow a `START_SERIALIZATION` poll only when the selected series has exactly one active proposal with `SeriesProposal.status_code = UNDER_BOARD_REVIEW`. | BR-BOARD-POLL-003 |
| FR-BOARD-POLL-004 | The system shall treat a `START_SERIALIZATION` poll as voting on the active under-board-review proposal for the selected series even though the poll stores only `series_id`. | BR-BOARD-POLL-004 |
| FR-BOARD-POLL-005 | The system shall allow a `CANCEL_SERIALIZATION` poll for a serialized or paused series only when the Editorial Board Chief provides a reason. | BR-BOARD-POLL-005 |
| FR-BOARD-POLL-006 | The system shall display low ranking or high cancellation risk as supporting evidence for cancellation polls when available. | BR-BOARD-POLL-006 |
| FR-BOARD-POLL-007 | The system shall not require low ranking or high cancellation risk to be the only reason for opening a cancellation poll. | BR-BOARD-POLL-006 |
| FR-BOARD-POLL-008 | The system shall require each board poll to have a non-empty reason. | BR-BOARD-POLL-007 |
| FR-BOARD-POLL-009 | The system shall allow a board poll to have a scheduled end time or be closed manually by an Editorial Board Chief. | BR-BOARD-POLL-008 |
| FR-BOARD-POLL-010 | The system shall prevent a series from having more than one open poll of the same type at the same time. | BR-BOARD-POLL-009 |
| FR-BOARD-POLL-011 | The system shall store `poll_status_code` to distinguish `OPEN`, `CLOSED`, and `CANCELLED` polls. | BR-BOARD-POLL-010 |
| FR-BOARD-POLL-012 | The system shall prevent votes from an `OPEN` poll from updating series or proposal status. | BR-BOARD-POLL-011 |
| FR-BOARD-POLL-013 | The system shall allow votes from a `CLOSED` poll to be used by Board Result requirements for applying series or proposal status changes. | BR-BOARD-POLL-012 |
| FR-BOARD-POLL-014 | The system shall preserve votes from a `CANCELLED` poll for traceability. | BR-BOARD-POLL-013, BR-BOARD-POLL-015 |
| FR-BOARD-POLL-015 | The system shall prevent votes from a `CANCELLED` poll from affecting series or proposal status. | BR-BOARD-POLL-013 |
| FR-BOARD-POLL-016 | The system shall allow Editorial Board Chief users to cancel a poll when the voting setup or process must be invalidated. | BR-BOARD-POLL-014 |
| FR-BOARD-POLL-017 | The system shall require poll results to be computed from board votes and handled by Board Result requirements instead of storing final result codes directly on `SeriesBoardPoll`. | BR-BOARD-POLL-016 |
| FR-BOARD-POLL-018 | The system shall record poll creation, cancellation, and closure in the audit log. | BR-BOARD-POLL-017 |
| FR-BOARD-POLL-019 | The system shall require a `START_SERIALIZATION` poll opened by an Editorial Board Chief to include the publication frequency to be applied if the poll is approved. | BR-BOARD-POLL-018, BR-PUB-010 |
---

## 3.9 Board Vote

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-BOARD-VOTE-001 | The system shall allow Editorial Board Members and Editorial Board Chiefs to vote only in an open `SeriesBoardPoll`. | BR-BOARD-VOTE-001 |
| FR-BOARD-VOTE-002 | The system shall restrict board voting to users with the Editorial Board Member or Editorial Board Chief role. | BR-BOARD-VOTE-002 |
| FR-BOARD-VOTE-003 | The system shall restrict board vote choices to `APPROVE`, `REJECT`, or `ABSTAIN`. | BR-BOARD-VOTE-003 |
| FR-BOARD-VOTE-004 | The system shall require a non-empty reason when a board member votes `REJECT`. | BR-BOARD-VOTE-004 |
| FR-BOARD-VOTE-005 | The system shall prevent each Editorial Board Member or Editorial Board Chief from casting more than one vote per board poll. | BR-BOARD-VOTE-005 |
| FR-BOARD-VOTE-006 | The system shall tie each board vote to one `SeriesBoardPoll`. | BR-BOARD-VOTE-006 |
| FR-BOARD-VOTE-007 | The system shall ensure the related series has exactly one proposal in `UNDER_BOARD_REVIEW` before allowing votes for a `START_SERIALIZATION` poll. | BR-BOARD-VOTE-007 |
| FR-BOARD-VOTE-008 | The system shall preserve board votes after a poll is closed or cancelled. | BR-BOARD-VOTE-008 |
| FR-BOARD-VOTE-009 | The system shall use votes from a `CLOSED` poll to determine the applicable poll result. | BR-BOARD-VOTE-009 |
| FR-BOARD-VOTE-010 | The system shall preserve votes from a `CANCELLED` poll but prevent them from being applied to status changes. | BR-BOARD-VOTE-010 |
| FR-BOARD-VOTE-011 | The system shall prevent a board vote alone from updating series or proposal status. | BR-BOARD-VOTE-011 |
| FR-BOARD-VOTE-012 | The system shall apply status changes only when an Editorial Board Chief or system process closes the poll and applies the computed result. | BR-BOARD-VOTE-011 |

---

## 3.10 Board Result

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-BOARD-RESULT-001 | The system shall compute board results from `SeriesBoardVote` records tied to a `SeriesBoardPoll`. | BR-BOARD-RESULT-001 |
| FR-BOARD-RESULT-002 | The system shall not store MVP board results in a separate `SeriesBoardDecision` table. | BR-BOARD-RESULT-001 |
| FR-BOARD-RESULT-003 | The system shall calculate aggregate approve, reject, abstain, and total vote counts from `SeriesBoardVote` records. | BR-BOARD-RESULT-002, BR-BOARD-POLL-016 |
| FR-BOARD-RESULT-004 | The system shall derive the computed poll result from approve and reject vote counts. | BR-BOARD-RESULT-003 |
| FR-BOARD-RESULT-005 | The system shall compute `APPROVED` when approve votes exceed reject votes. | BR-BOARD-RESULT-004 |
| FR-BOARD-RESULT-006 | The system shall compute `REJECTED` when reject votes exceed approve votes. | BR-BOARD-RESULT-005 |
| FR-BOARD-RESULT-007 | The system shall compute `NO_DECISION` when approve and reject votes are tied. | BR-BOARD-RESULT-006 |
| FR-BOARD-RESULT-008 | The system shall count abstain votes separately without using them to directly determine approval or rejection. | BR-BOARD-RESULT-007 |
| FR-BOARD-RESULT-009 | The system shall treat an `OPEN` poll result as `PENDING` and prevent it from being applied. | BR-BOARD-RESULT-008 |
| FR-BOARD-RESULT-010 | The system shall treat a `CANCELLED` poll result as `INVALIDATED` and prevent it from being applied. | BR-BOARD-RESULT-009 |
| FR-BOARD-RESULT-011 | The system shall produce an applicable board result only for a `CLOSED` poll. | BR-BOARD-RESULT-010 |
| FR-BOARD-RESULT-012 | The system shall apply `START_SERIALIZATION` poll results to the active proposal and series according to the computed result, including applying the board-specified publication frequency when the result is approved. | BR-BOARD-RESULT-011, BR-BOARD-RESULT-012, BR-BOARD-RESULT-013, BR-PUB-013 |
| FR-BOARD-RESULT-013 | The system shall apply `CANCEL_SERIALIZATION` poll results to the series according to the computed result. | BR-BOARD-RESULT-014, BR-BOARD-RESULT-015 |
| FR-BOARD-RESULT-014 | The system shall not require a final board decision explanation in MVP. | BR-BOARD-RESULT-016 |
| FR-BOARD-RESULT-015 | The system shall retain poll reasons and individual rejection reasons separately. | BR-BOARD-RESULT-016 |
| FR-BOARD-RESULT-016 | The system shall record board result application to proposal or series status in the audit log. | BR-BOARD-RESULT-017 |

---

## 3.11 Chapter

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-CH-001 | The system shall require each chapter to belong to exactly one series. | BR-CH-001 |
| FR-CH-002 | The system shall require chapter number labels to be unique among non-cancelled chapters within the same series. | BR-CH-002 |
| FR-CH-003 | The system shall create each new chapter with `DRAFT` status by default. | BR-CH-003 |
| FR-CH-004 | The system shall store the current chapter workflow state in `Chapter.status_code`. | BR-CH-004 |
| FR-CH-005 | The system shall restrict chapter status to `DRAFT`, `UNDER_REVIEW`, `REVISION_REQUESTED`, `APPROVED`, `SCHEDULED`, `RELEASED`, `ON_HOLD`, or `CANCELLED`. | BR-CH-005 |
| FR-CH-006 | The system shall allow `planned_release_date` to remain empty before the chapter is scheduled. | BR-CH-006 |
| FR-CH-007 | The system shall allow a chapter to become `SCHEDULED` only when `planned_release_date` is provided. | BR-CH-007 |
| FR-CH-008 | The system shall require `released_at_utc` when a chapter is marked `RELEASED`. | BR-CH-008 |
| FR-CH-009 | The system shall populate `released_at_utc` only when the chapter is actually released. | BR-CH-009 |
| FR-CH-010 | The system shall allow an editor to place a `SCHEDULED` chapter `ON_HOLD` when a valid operational or editorial reason is provided. | BR-CH-010, BR-CH-018 |
| FR-CH-011 | The system shall store the user who created the chapter in `created_by_user_id`. | BR-CH-011 |
| FR-CH-012 | The system shall update `Chapter.updated_at_utc` when the chapter row is modified for operational display. | BR-CH-012 |
| FR-CH-013 | The system shall treat scheduling as chapter-level and shall not use `Series.status_code = SCHEDULED` for publication scheduling. | BR-CH-013 |
| FR-CH-014 | The system shall allow Mangaka and Tantou Editors to set or reschedule `Chapter.planned_release_date` when chapter status and permissions allow it, as long as the chosen planned release date is not in the past. | BR-CH-014 |
| FR-CH-015 | The system shall move an `APPROVED` chapter to `SCHEDULED` when a future `planned_release_date` is set. | BR-CH-015 |
| FR-CH-016 | The system shall lock Mangaka chapter content changes when a chapter is `SCHEDULED`. | BR-CH-016 |
| FR-CH-017 | The system shall allow authorized Mangaka and Tantou Editors to reschedule a `SCHEDULED` chapter to a future date while treating publication frequency mismatch as a warning rather than a hard blocker. | BR-CH-017 |
| FR-CH-018 | The system shall require a new future planned release date when returning a chapter from `ON_HOLD` to `SCHEDULED`. | BR-CH-018 |

---

## 3.12 Chapter Page and Page Version

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-CP-001 | The system shall allow a chapter to contain many logical pages. | BR-CP-001 |
| FR-CP-002 | The system shall require each `ChapterPage` to belong to exactly one chapter. | BR-CP-002 |
| FR-CP-003 | The system shall require page numbers to be unique within the same chapter. | BR-CP-003 |
| FR-CP-004 | The system shall treat `ChapterPage` as a logical page slot, not a specific uploaded image file. | BR-CP-004 |
| FR-CP-005 | The system shall allow a `ChapterPage` to have multiple `ChapterPageVersion` records over time. | BR-CP-005 |
| FR-CP-006 | The system shall require each `ChapterPageVersion` to belong to exactly one logical `ChapterPage`. | BR-CP-006 |
| FR-CP-007 | The system shall require each `ChapterPageVersion` to reference exactly one uploaded file through `FileResource`. | BR-CP-007 |
| FR-CP-008 | The system shall derive page version upload time and uploader from the related `FileResource`. | BR-CP-008 |
| FR-CP-009 | The system shall require page version numbers to be positive. | BR-CP-009 |
| FR-CP-010 | The system shall require page version numbers to be unique within the same `ChapterPage`. | BR-CP-010 |
| FR-CP-011 | The system shall treat a higher `version_no` as a newer uploaded version of the same logical page. | BR-CP-011 |
| FR-CP-012 | The system shall allow only one page version to be marked as current for a logical page at a time. | BR-CP-012 |
| FR-CP-013 | The system shall preserve old page versions for traceability and comparison instead of overwriting them. | BR-CP-013 |
| FR-CP-014 | The system shall create a new `ChapterPageVersion` when a page is replaced or revised. | BR-CP-014, BR-CP-021 |
| FR-CP-015 | The system shall prevent replacing or revising a page by creating a new `ChapterPage` for the same logical page. | BR-CP-014 |
| FR-CP-016 | The system shall require a page task output to reference a `ChapterPageVersion` for the same logical `ChapterPage` derived from the task's linked `PageRegion` records. | BR-CP-015 |
| FR-CP-017 | The system shall allow page annotations to identify their page-version context through linked `PageRegion` records. | BR-CP-016 |
| FR-CP-018 | The system shall store page-specific feedback through annotations linked to `PageRegion` records. | BR-CP-017 |
| FR-CP-019 | The system shall perform formal editorial review at chapter level while using page annotations as supporting feedback. | BR-CP-018 |
| FR-CP-020 | The system shall allow a `ChapterPage` to be soft-deleted when it is no longer part of the active chapter draft. | BR-CP-019 |
| FR-CP-021 | The system shall preserve historical `ChapterPageVersion` records when a `ChapterPage` is soft-deleted. | BR-CP-020 |
| FR-CP-022 | The system shall allow a page task that produces a new page version to remain under review until the Mangaka accepts the submitted version. | BR-CP-022 |
| FR-CP-023 | The system shall create a `ChapterPageVersion` only after the user explicitly saves or confirms the selected page upload as an official page version. | BR-CP-023 |
| FR-CP-024 | The system shall not allow normal users to delete saved `ChapterPageVersion` records in the current MVP. | BR-CP-024 |
| FR-CP-025 | The system shall preserve wrong, outdated, or superseded saved page versions in version history and allow users to replace them only by saving a newer version. | BR-CP-024, BR-CP-025 |
| FR-CP-026 | The system shall unset the previous current page version when a newly saved page version becomes current for the same logical `ChapterPage`. | BR-CP-012, BR-CP-025 |
| FR-CP-027 | The system may support a future Admin/system retention workflow to purge old or unused page versions after chapter release, provided referenced workflow history is preserved. | BR-CP-026 |

---


## 3.12A Authorized Chapter Workspace

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-WORKSPACE-001 | The system shall provide a centralized authorized workspace at chapter level instead of treating the workspace as a page-only screen. | BR-WORKSPACE-001 |
| FR-WORKSPACE-002 | The system shall load the selected chapter and provide navigation across the related series, chapters, pages, and page versions. | BR-WORKSPACE-002 |
| FR-WORKSPACE-003 | The system shall provide a left navigation panel for series → chapter → page → version navigation. | BR-WORKSPACE-003 |
| FR-WORKSPACE-004 | The system shall display the selected page version in the main workspace viewing area with available overlays such as saved page regions and annotations. | BR-WORKSPACE-004 |
| FR-WORKSPACE-005 | The system shall provide a right tools/actions panel containing AI segmentation, AI/OCR translation support, annotation tools, page-version actions, and role-specific workflow actions. | BR-WORKSPACE-005 |
| FR-WORKSPACE-006 | The system shall allow all Authorized Page Workspace Users with access to the relevant chapter/page version to use available AI tools. | BR-WORKSPACE-006 |
| FR-WORKSPACE-007 | The system shall enforce role-specific permissions for business actions in the workspace, including task assignment, task submission, production annotations, editorial-review annotations, and chapter review actions. | BR-WORKSPACE-007 |
| FR-WORKSPACE-008 | The system shall not grant Editorial Board Members chapter workspace access by default unless a future permission explicitly allows it. | BR-WORKSPACE-008 |
| FR-WORKSPACE-009 | The system shall support workspace entry from the main series page, dashboards, review queues, assistant task lists, and other authorized workflow lists. | BR-WORKSPACE-009 |
| FR-WORKSPACE-010 | The system shall return users to `/series/{slug}` by default when they entered the workspace from the main series page. | BR-WORKSPACE-010 |
| FR-WORKSPACE-011 | The system shall support returning users to a dashboard, review queue, or task list when that was the workspace entry context. | BR-WORKSPACE-011 |
| FR-WORKSPACE-012 | The system shall use internal IDs for workspace editing context while using slug mainly for the main series page and return navigation. | BR-WORKSPACE-012 |

---

## 3.13 Chapter Page Annotation

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-ANN-001 | The system shall represent each page annotation as one `ChapterPageAnnotation` record that can be linked to one or more `PageRegion` records. | BR-ANN-001 |
| FR-ANN-002 | The system shall store annotation-region links in `ChapterPageAnnotationRegion`. | BR-ANN-003 |
| FR-ANN-003 | The system shall not store direct page-region foreign keys or direct coordinates in `ChapterPageAnnotation`. | BR-ANN-002 |
| FR-ANN-004 | The system shall represent annotation location through the `PageRegion` records linked by `ChapterPageAnnotationRegion`. | BR-ANN-002, BR-ANN-003 |
| FR-ANN-005 | The system shall require all regions linked to one annotation to belong to the same `ChapterPageVersion`. | BR-ANN-004 |
| FR-ANN-006 | The system shall derive the annotation page-version context from the linked `PageRegion.chapter_page_version_id` values. | BR-ANN-005 |
| FR-ANN-007 | The system shall represent whole-page feedback through a manually created full-page `PageRegion` linked to the annotation. | BR-ANN-006 |
| FR-ANN-008 | The system shall require each annotation to have one issue type. | BR-ANN-007 |
| FR-ANN-009 | The system shall restrict `issue_type_code` to the approved annotation issue code list. | BR-ANN-008 |
| FR-ANN-010 | The system shall show only valid annotation issue types in the UI. | BR-ANN-009 |
| FR-ANN-011 | The system shall require non-empty annotation text. | BR-ANN-010 |
| FR-ANN-012 | The system shall record the user who created each annotation. | BR-ANN-011 |
| FR-ANN-013 | The system shall record the creation time of each annotation. | BR-ANN-012 |
| FR-ANN-014 | The system shall restrict annotation creation to active Mangaka contributors and active Tantou Editor contributors with access to the owning series/page workspace in MVP. | BR-ANN-013 |
| FR-ANN-015 | The system shall allow a page annotation to be linked to existing saved regions, newly created regions, or both. | BR-ANN-014 |
| FR-ANN-016 | The system shall require both resolver user and resolved timestamp when an annotation is resolved. | BR-ANN-015 |
| FR-ANN-017 | The system shall require both `resolved_by_user_id` and `resolved_at_utc` to be `NULL` when an annotation is unresolved. | BR-ANN-016 |
| FR-ANN-018 | The system shall resolve annotations by setting resolver and timestamp fields without deleting the annotation or its region links. | BR-ANN-017 |
| FR-ANN-019 | The system shall preserve old annotations through their linked regions after a new page version is uploaded. | BR-ANN-018 |
| FR-ANN-020 | The system shall enforce fixed MVP annotation issue types by database constraint instead of a separate lookup table. | BR-ANN-019 |
| FR-ANN-021 | The system shall allow unresolved Mangaka-created annotations to be resolved by active Mangaka contributors on the same series or active Tantou Editor contributors on the same series. | BR-ANN-020 |
| FR-ANN-022 | The system shall prevent Mangaka users from resolving annotations created by Tantou Editors. | BR-ANN-021 |
| FR-ANN-023 | The system shall allow only active Tantou Editor contributors on the same series to resolve Tantou Editor-created annotations. | BR-ANN-021 |
| FR-ANN-024 | The system shall allow active Mangaka contributors to update unresolved annotation text only for Mangaka-created annotations on the same series. | BR-ANN-022 |
| FR-ANN-025 | The system shall allow active Tantou Editor contributors to update unresolved annotation text for either Mangaka-created or Tantou Editor-created annotations on the same series. | BR-ANN-023 |
| FR-ANN-026 | The system shall prevent resolved annotations from being edited in MVP. | BR-ANN-024 |
| FR-ANN-027 | The system shall determine annotation permission in MVP through stored-procedure guard checks using `annotated_by_user_id`, the creator's current role, the actor's current role, active account status, active series contributor membership, and the series context derived from linked regions. | BR-ANN-025 |
| FR-ANN-028 | The system shall audit annotation text updates with old text, new text, actor user, related series/page context, and optional update reason when available. | BR-ANN-026 |

## 3.14 Page Task

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-PGTASK-001 | The system shall derive each page task's logical `ChapterPage` context from one or more linked `PageRegion` records instead of a direct `chapter_page_id` column on `ChapterPageTask`. | BR-PGTASK-001 |
| FR-PGTASK-002 | The system shall allow a logical `ChapterPage` to have many tasks over time through linked `PageRegion` records. | BR-PGTASK-002 |
| FR-PGTASK-003 | The system shall represent one page task as one assignment of work to one assistant or authorized user. | BR-PGTASK-003 |
| FR-PGTASK-004 | The system shall require each page task to be assigned to exactly one assistant or authorized user. | BR-PGTASK-004 |
| FR-PGTASK-005 | The system shall record the user who created each page task. | BR-PGTASK-005 |
| FR-PGTASK-006 | The system shall support region-linked, page-derived task assignment in MVP. | BR-PGTASK-006 |
| FR-PGTASK-007 | The system shall not require whole-chapter task assignment in MVP. | BR-PGTASK-006 |
| FR-PGTASK-008 | The system shall allow a page task to target one or more `PageRegion` records through `ChapterPageTaskRegion`. | BR-PGTASK-007 |
| FR-PGTASK-009 | The system shall represent task target regions through linked `PageRegion` records instead of free-text target region descriptions. | BR-PGTASK-008 |
| FR-PGTASK-010 | The system shall prevent duplicate task-region pairs in `ChapterPageTaskRegion`. | BR-PGTASK-009 |
| FR-PGTASK-011 | The system shall allow a `PageRegion` to be referenced by multiple tasks when different work is needed for the same area. | BR-PGTASK-010 |
| FR-PGTASK-012 | The system shall require all `PageRegion` records linked to the same task to belong to the same logical `ChapterPage`, derived through their `ChapterPageVersion` records. | BR-PGTASK-011 |
| FR-PGTASK-013 | The system shall allow region-based annotations to be used as the basis for creating page tasks. | BR-PGTASK-012 |
| FR-PGTASK-014 | The system shall highlight linked page regions in the assistant task UI. | BR-PGTASK-013 |
| FR-PGTASK-015 | The system shall require every page task to have a due date. | BR-PGTASK-014 |
| FR-PGTASK-016 | The system shall restrict task status to `ASSIGNED`, `UNDER_REVIEW`, `COMPLETED`, or `CANCELLED`. | BR-PGTASK-015 |
| FR-PGTASK-017 | The system shall keep a task in `ASSIGNED` status until the assistant submits a completed page version for review. | BR-PGTASK-016 |
| FR-PGTASK-018 | The system shall not require assistants to manually start a task before submitting work. | BR-PGTASK-016 |
| FR-PGTASK-019 | The system shall require an uploaded page version before a task can enter `UNDER_REVIEW` or `COMPLETED` status. | BR-PGTASK-017 |
| FR-PGTASK-020 | The system shall require a completed page task to reference the `ChapterPageVersion` produced by the assigned user. | BR-PGTASK-018 |
| FR-PGTASK-021 | The system shall require the completed page version to belong to the same logical `ChapterPage` derived from the task's linked `PageRegion` records. | BR-PGTASK-019 |
| FR-PGTASK-022 | The system shall prevent assistants from approving their own task output. | BR-PGTASK-020 |
| FR-PGTASK-023 | The system shall allow Mangaka users or authorized reviewers to review submitted page task output. | BR-PGTASK-020, BR-PGTASK-021 |
| FR-PGTASK-024 | The system shall allow a task to remain `UNDER_REVIEW` until the submitted page version is accepted. | BR-PGTASK-021 |
| FR-PGTASK-025 | The system shall allow a task to be marked `COMPLETED` when the submitted page version is accepted. | BR-PGTASK-022 |
| FR-PGTASK-026 | The system shall use task status for production tracking while formal editorial approval remains chapter-level. | BR-PGTASK-023 |
| FR-PGTASK-027 | The system shall prevent normal reassignment of `assigned_to_user_id` after a task is created. | BR-PGTASK-024 |
| FR-PGTASK-028 | The system shall require reassignment to be handled by completing or cancelling the old task and creating a new task. | BR-PGTASK-025 |
| FR-PGTASK-029 | The system shall allow multiple tasks for the same chapter page when different assistants, regions, task types, or work rounds are involved. | BR-PGTASK-026 |
| FR-PGTASK-030 | The system shall preserve task history by keeping old task rows. | BR-PGTASK-027 |
| FR-PGTASK-031 | The system shall require the task description to include a cancellation reason when a page task is cancelled. | BR-PGTASK-028 |
| FR-PGTASK-032 | The system shall preserve the original task record after cancellation. | BR-PGTASK-029 |
| FR-PGTASK-033 | The system shall require cancelled work to be reassigned through a new task instead of changing the original assignee. | BR-PGTASK-030 |
| FR-PGTASK-034 | The system shall record task creation, cancellation, completion, and status changes in the audit log. | BR-PGTASK-031 |

---

## 3.15 Chapter Editorial Review and Submission

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-CH-SUB-001 | The system shall represent chapter submission by changing `Chapter.status_code` to `UNDER_REVIEW`. | BR-CH-SUB-001 |
| FR-CH-SUB-002 | The system shall not require a separate `ChapterSubmission` row in MVP. | BR-CH-SUB-001 |
| FR-CH-SUB-003 | The system shall define a submitted chapter as the current active page versions of all non-deleted chapter pages at submission time. | BR-CH-SUB-002 |
| FR-CH-SUB-004 | The system shall prevent page creation, page deletion, page version upload, assistant task output submission that creates or changes page content, and other page/content mutation workflows while a chapter is `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, `ON_HOLD`, `RELEASED`, or `CANCELLED`. | BR-CH-SUB-003 |
| FR-CH-SUB-005 | The system shall allow chapter pages and page versions to become editable again when the chapter status becomes `REVISION_REQUESTED`. | BR-CH-SUB-004 |
| FR-CH-SUB-006 | The system shall store chapter content as page-level assets through `ChapterPageVersion`. | BR-CH-SUB-005 |
| FR-CH-SUB-007 | The system shall not require one chapter-level submission file or generated PDF for MVP. | BR-CH-SUB-006 |
| FR-CH-REV-001 | The system shall record editorial reviews directly against `Chapter`. | BR-CH-REV-001 |
| FR-CH-REV-002 | The system shall require each `ChapterEditorialReview` record to belong to exactly one chapter. | BR-CH-REV-002 |
| FR-CH-REV-003 | The system shall allow a chapter to have multiple editorial review records over time across revision cycles. | BR-CH-REV-003 |
| FR-CH-REV-004 | The system shall require each editorial review to be performed by one valid reviewer user. | BR-CH-REV-004 |
| FR-CH-REV-005 | The system shall restrict chapter editorial review creation to authorized Tantou Editors or approved review roles. | BR-CH-REV-005 |
| FR-CH-REV-006 | The system shall restrict chapter editorial review decisions to `APPROVED`, `REVISION_REQUESTED`, or `CANCELLED`. | BR-CH-REV-006 |
| FR-CH-REV-007 | The system shall require non-blank meaningful comments when the review decision is `REVISION_REQUESTED` or `CANCELLED`. | BR-CH-REV-007 |
| FR-CH-REV-008 | The system shall allow an optional markup file to support chapter editorial feedback. | BR-CH-REV-008 |
| FR-CH-REV-009 | The system shall require any provided markup file to reference an existing `FileResource`. | BR-CH-REV-008 |
| FR-CH-REV-010 | The system shall store page-specific annotations separately from the chapter-level editorial review decision. | BR-CH-REV-009 |
| FR-CH-REV-011 | The system shall update chapter status when an editorial review is created. | BR-CH-REV-010 |
| FR-CH-REV-012 | The system shall change chapter status to `APPROVED` when the review decision is `APPROVED` and no `planned_release_date` exists. | BR-CH-REV-011 |
| FR-CH-REV-012A | The system shall change chapter status to `SCHEDULED` when the review decision is `APPROVED` and a valid `planned_release_date` already exists. | BR-CH-REV-011A |
| FR-CH-REV-013 | The system shall change chapter status to `REVISION_REQUESTED` and allow editing when the review decision is `REVISION_REQUESTED`. | BR-CH-REV-012 |
| FR-CH-REV-014 | The system shall change chapter status to `CANCELLED` when the review decision is `CANCELLED`. | BR-CH-REV-013 |
| FR-CH-REV-015 | The system shall record chapter editorial review creation in the audit log. | BR-CH-REV-014 |
| FR-CH-REV-016 | The system shall audit approval-to-scheduled behavior when chapter approval moves the chapter directly to `SCHEDULED`. | BR-CH-REV-015 |

---

## 3.16 Chapter Cancellation

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-CH-CANCEL-001 | The system shall allow a chapter to be cancelled through an editorial review decision when the editor determines that the chapter should not proceed. | BR-CH-CANCEL-001 |
| FR-CH-CANCEL-002 | The system shall prevent cancelled chapters from proceeding to `SCHEDULED` or `RELEASED`. | BR-CH-CANCEL-002 |
| FR-CH-CANCEL-003 | The system shall preserve pages, page versions, page regions, annotations, files, and review history when a chapter is cancelled. | BR-CH-CANCEL-003 |
| FR-CH-CANCEL-004 | The system shall allow editors to use `REVISION_REQUESTED` instead of `CANCELLED` when the chapter can still be fixed and resubmitted. | BR-CH-CANCEL-004 |
| FR-CH-CANCEL-005 | The system shall prevent chapter cancellation without a chapter editorial review decision in MVP. | BR-CH-CANCEL-005 |
| FR-CH-CANCEL-006 | The system shall treat a cancelled chapter as terminal for the current chapter attempt and prevent editing, new page-version upload, resubmission, approval, scheduling, and release for that cancelled chapter. | BR-CH-CANCEL-006 |
| FR-CH-CANCEL-007 | The system shall allow a Mangaka to create a new replacement chapter draft with the same chapter number label after the previous chapter with that label has been cancelled. | BR-CH-CANCEL-007 |
| FR-CH-CANCEL-008 | The system shall enforce chapter-number uniqueness only among non-cancelled chapters in the same series. | BR-CH-CANCEL-008 |
| FR-CH-CANCEL-009 | The system shall not require a `replacement_of_chapter_id` relationship for replacement chapter drafts in MVP. | BR-CH-CANCEL-009 |
| FR-CH-CANCEL-010 | The system shall keep cancelled chapter materials read-only and require redo work to be created under the new replacement chapter. | BR-CH-CANCEL-010 |

---


## 3.17 Publication Planning

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-PUB-001 | The system shall handle detailed publication planning at chapter level using `Chapter.planned_release_date`, `Chapter.released_at_utc`, and `Chapter.status_code`. | BR-PUB-001 |
| FR-PUB-002 | The system shall store only the current publication frequency on `Series` for MVP. | BR-PUB-002 |
| FR-PUB-003 | The system shall not require formal series-level publication policy history for MVP. | BR-PUB-002, BR-PUB-007 |
| FR-PUB-004 | The system shall allow a serialized series to have a publication frequency of `WEEKLY`, `MONTHLY`, or `IRREGULAR`. | BR-PUB-003 |
| FR-PUB-005 | The system shall treat `IRREGULAR` publication frequency as chapters being released when ready without a fixed weekly or monthly schedule. | BR-PUB-004 |
| FR-PUB-006 | The system shall allow `publication_frequency_code` to be `NULL` before the series is serialized or before the release approach is decided. | BR-PUB-003 |
| FR-PUB-007 | The system shall control actual chapter release timing through chapter-level release dates and release timestamps. | BR-PUB-001, BR-PUB-006 |
| FR-PUB-008 | The system shall require chapter scheduling and release status to follow the Chapter status rules. | BR-PUB-008 |
| FR-PUB-009 | The system shall identify delayed or overdue chapters by comparing `planned_release_date` and `released_at_utc` instead of storing a separate delay status. | BR-PUB-009 |
| FR-PUB-010 | The system shall require an Editorial Board Chief to specify publication frequency when opening a `START_SERIALIZATION` poll. | BR-PUB-010 |
| FR-PUB-011 | The system shall allow Mangaka users to provide or update their preferred publication frequency only while the series is in `PROPOSAL_DRAFT`, without requiring a separate desired publication frequency column on `Series`. | BR-PUB-011 |
| FR-PUB-012 | The system shall prevent Mangaka users from directly changing the official `Series.publication_frequency_code` after the series leaves `PROPOSAL_DRAFT`. | BR-PUB-012 |
| FR-PUB-013 | The system shall apply the board-specified publication frequency as the official `Series.publication_frequency_code` when a `START_SERIALIZATION` poll is approved, overriding the Mangaka's preference. | BR-PUB-013 |
| FR-PUB-014 | The system shall allow Mangaka users to request a publication frequency change after the board decision by sending an in-app notification to the Editorial Board Chief. | BR-PUB-014, BR-NOTIF-012 |
| FR-PUB-015 | The system shall not require a separate official publication-frequency change request table for MVP. | BR-PUB-014, BR-NOTIF-013 |
| FR-PUB-016 | The system shall allow Editorial Board Chief users to directly change `Series.publication_frequency_code` only when they provide a required audit reason. | BR-PUB-015 |
| FR-PUB-017 | The system shall treat `Series.publication_frequency_code` as an advisory source for default date suggestions and warnings, not as a hard scheduling constraint. | BR-PUB-006, BR-PUB-SCHEDULE-001 |
| FR-PUB-018 | The system shall allow authorized Mangaka and Tantou Editor users to schedule or reschedule chapters to any future planned release date when chapter status and permissions allow. | BR-PUB-SCHEDULE-001, BR-PUB-SCHEDULED-003 |
| FR-PUB-019 | The system shall block planned release dates in the past. | BR-PUB-SCHEDULE-005 |
| FR-PUB-020 | The system shall warn, not block, when a chosen planned release date does not match the advisory publication frequency pattern. | BR-PUB-SCHEDULE-006 |
| FR-PUB-021 | The system shall allow multiple chapters from the same series to share the same planned release date for bulk, catch-up, or campaign release plans. | BR-PUB-SCHEDULE-007 |
| FR-PUB-022 | The system shall ask for confirmation before schedule, reschedule, hold, return-from-hold, release, or bulk publication actions change chapter state. | BR-PUB-SCHEDULE-010 |
| FR-PUB-023 | The system shall make schedule changes audit-visible, including actor, old/new planned date, old/new status when changed, and reason when provided. | BR-PUB-SCHEDULE-008 |
| FR-PUB-024 | The system may show active contributor contact information such as email to authorized series contributors for schedule coordination. | BR-PUB-017 |
| FR-PUB-PERIOD-001 | The system shall store weekly, monthly, and yearly business calendar periods in `PublicationPeriod`. | BR-PUB-PERIOD-001, BR-PUB-PERIOD-003 |
| FR-PUB-PERIOD-002 | The system shall require each `PublicationPeriod` to have a unique `period_name`, `period_type_code`, `period_start_date`, and `period_end_date`. | BR-PUB-PERIOD-002 |
| FR-PUB-PERIOD-003 | The system shall generate weekly publication periods from Monday through Sunday. | BR-PUB-PERIOD-006 |
| FR-PUB-PERIOD-004 | The system shall assign each weekly publication period to the month that contains at least four days of that week. | BR-PUB-PERIOD-007 |
| FR-PUB-PERIOD-005 | The system shall name weekly publication periods from the owning month and the ordinal week number within that owning month. | BR-PUB-PERIOD-008, BR-PUB-PERIOD-009 |
| FR-PUB-PERIOD-006 | The system shall generate monthly publication periods from the first to the last calendar day of the month. | BR-PUB-PERIOD-004 |
| FR-PUB-PERIOD-007 | The system shall generate yearly publication periods from January 1 to December 31. | BR-PUB-PERIOD-005 |
| FR-PUB-PERIOD-008 | The system shall use `PublicationPeriod` for ranking, reports, and advisory schedule display rather than hard-blocking normal chapter scheduling. | BR-PUB-PERIOD-011 |
| FR-PUB-DATE-001 | The system shall determine publication-period membership using the publication business date instead of the raw UTC timestamp. | BR-PUB-DATE-001 |
| FR-PUB-DATE-002 | The system shall use `Chapter.planned_release_date` as the business date for scheduled chapter period membership. | BR-PUB-DATE-002 |
| FR-PUB-DATE-003 | The system shall derive released chapter business dates by converting `released_at_utc` to Vietnam publication time (UTC+7) and taking the date part. | BR-PUB-DATE-003, BR-PUB-DATE-005 |
| FR-PUB-DATE-004 | The system shall prevent ranking or publication-period reports from using `CAST(released_at_utc AS DATE)` in UTC as the business period date. | BR-PUB-DATE-004 |
| FR-PUB-DATE-005 | The system shall set `planned_release_date` to the current publication business date when releasing a chapter that has no planned date. | BR-PUB-DATE-006, BR-PUB-SCHEDULED-008 |
| FR-PUB-DATE-006 | The system shall preserve an existing `planned_release_date` when releasing a chapter so planned-versus-actual release comparison remains possible. | BR-PUB-DATE-007, BR-PUB-SCHEDULED-008 |
| FR-PUB-SUGGEST-001 | The system should suggest the same weekday in the next week for `WEEKLY` series when a useful reference date exists. | BR-PUB-SCHEDULE-002 |
| FR-PUB-SUGGEST-002 | The system should suggest the same day number in the next month for `MONTHLY` series when possible, otherwise the last valid day of the next month. | BR-PUB-SCHEDULE-003 |
| FR-PUB-SUGGEST-003 | The system shall not require a strict default date for `IRREGULAR` or `NULL` frequency. | BR-PUB-SCHEDULE-004 |
| FR-PUB-SCHEDULED-001 | The system shall prevent Mangaka users from changing chapter content when a chapter is `SCHEDULED`. | BR-PUB-SCHEDULED-001 |
| FR-PUB-SCHEDULED-002 | The system shall block page/content mutation workflows while a chapter is `SCHEDULED` or `ON_HOLD`. | BR-PUB-SCHEDULED-002 |
| FR-PUB-SCHEDULED-003 | The system shall allow authorized Mangaka and Tantou Editor users to set or reschedule planned release dates when chapter status permits. | BR-PUB-SCHEDULED-003 |
| FR-PUB-SCHEDULED-004 | The system shall require a non-blank operational or editorial reason when a Tantou Editor places a `SCHEDULED` chapter `ON_HOLD`. | BR-PUB-SCHEDULED-004 |
| FR-PUB-SCHEDULED-005 | The system shall require a new future planned release date before a chapter returns from `ON_HOLD` to `SCHEDULED`. | BR-PUB-SCHEDULED-005, BR-PUB-SCHEDULED-006 |
| FR-PUB-SCHEDULED-006 | The system shall allow Tantou Editors to release eligible approved or scheduled chapters with confirmation. | BR-PUB-SCHEDULED-007 |
| FR-PUB-SCHEDULED-007 | The system shall set `released_at_utc` to the current UTC time when an eligible chapter is released. | BR-PUB-SCHEDULED-007 |
| FR-PUB-SCHEDULED-008 | The system shall defer automatic overdue-to-ON_HOLD transitions unless a later workflow explicitly implements them. | BR-PUB-SCHEDULED-009 |
| FR-PUB-SCHEDULED-009 | The system shall defer release automation and public release visibility to later workflows. | BR-PUB-SCHEDULED-010 |
| FR-PUB-SCHEDULED-010 | The system may support bulk schedule, bulk hold, and bulk release later; when implemented, those workflows shall require confirmation and audit each affected chapter. | BR-PUB-SCHEDULED-011 |

---

## 3.18 Ranking and Series Vote Input

### 3.18.1 Series Vote Input

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-SERIES-VOTE-001 | The system shall treat series vote input as simulated or manually aggregated series-level performance data in MVP. | BR-SERIES-VOTE-001 |
| FR-SERIES-VOTE-002 | The system shall not require direct real-reader voting in MVP. | BR-SERIES-VOTE-001 |
| FR-SERIES-VOTE-003 | The system shall require each `SeriesVoteInput` to reference one `PublicationPeriod` and one `Series`. | BR-SERIES-VOTE-002 |
| FR-SERIES-VOTE-004 | The system shall prevent more than one `SeriesVoteInput` row for the same series and publication period. | BR-SERIES-VOTE-003 |
| FR-SERIES-VOTE-005 | The system shall store `rating_count`, `average_rating`, and `reading_count` for each series vote input. | BR-SERIES-VOTE-004, BR-SERIES-VOTE-005, BR-SERIES-VOTE-006 |
| FR-SERIES-VOTE-006 | The system shall require `rating_count` and `reading_count` to be greater than zero. | BR-SERIES-VOTE-007, BR-SERIES-VOTE-008 |
| FR-SERIES-VOTE-007 | The system shall require `rating_count` to be less than or equal to `reading_count`. | BR-SERIES-VOTE-009 |
| FR-SERIES-VOTE-008 | The system shall require `average_rating` to be between 0 and 10. | BR-SERIES-VOTE-010 |
| FR-SERIES-VOTE-009 | The system shall treat vote input as period-only data, so later weekly inputs do not include earlier weekly inputs. | BR-SERIES-VOTE-011 |
| FR-SERIES-VOTE-010 | The system shall calculate monthly or yearly average ratings from lower-level period data using weighted average by `rating_count` when such aggregation is needed. | BR-SERIES-VOTE-012 |
| FR-SERIES-VOTE-011 | The system shall record the user and UTC timestamp when series vote input is entered or updated. | BR-SERIES-VOTE-013 |
| FR-SERIES-VOTE-012 | The system shall restrict simulated or aggregated series vote input to Editorial Board Members or Editorial Board Chief users in MVP. | BR-SERIES-VOTE-014 |
| FR-SERIES-VOTE-013 | The system shall allow a source note to explain the external report, manual report, spreadsheet, or tracking evidence used for series vote input. | BR-SERIES-VOTE-015 |

### 3.18.2 Dynamic Series Ranking

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-RANK-001 | The system shall calculate series ranking dynamically from `SeriesVoteInput`, `PublicationPeriod`, and `Series`. | BR-RANK-001 |
| FR-RANK-002 | The system shall not require `SeriesRankingSnapshot` in MVP because there is no ranking finalization workflow. | BR-RANK-001 |
| FR-RANK-003 | The system shall compute ranking separately for each `publication_period_id`. | BR-RANK-002 |
| FR-RANK-004 | The system shall expose ranking output through a dynamic view such as `manga.vw_SeriesRanking`. | BR-RANK-003 |
| FR-RANK-005 | The system shall compute `ranking_score` using `average_rating * LOG10(1 + rating_count) + reading_count * 0.001`. | BR-RANK-004 |
| FR-RANK-006 | The system shall compute rank position using dense ranking ordered by ranking score, average rating, rating count, reading count, and series ID. | BR-RANK-005 |
| FR-RANK-007 | The system shall avoid storing `ranking_score` and `rank_position` as duplicated normal columns unless later performance profiling proves caching is required. | BR-RANK-006 |
| FR-RANK-008 | The system shall prevent ranking results from automatically cancelling a series. | BR-RANK-007 |
| FR-RANK-009 | The system shall allow ranking evidence to support board review while still requiring the applicable workflow decision for cancellation. | BR-RANK-008 |


---

---

## 3.19 Notifications

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-NOTIF-001 | The system shall address each notification to exactly one recipient user. | BR-NOTIF-001 |
| FR-NOTIF-002 | The system shall provide notifications as in-app MVP messages. | BR-NOTIF-002 |
| FR-NOTIF-003 | The system shall not require email or push notifications in MVP. | BR-NOTIF-002 |
| FR-NOTIF-004 | The system shall allow a notification to optionally reference a related business entity such as a series, chapter, page task, proposal, board poll, or ranking result. | BR-NOTIF-003 |
| FR-NOTIF-005 | The system shall require `related_entity_type` and `related_entity_id` to both be present or both be null. | BR-NOTIF-004 |
| FR-NOTIF-006 | The system shall treat a notification as unread when `read_at_utc IS NULL`. | BR-NOTIF-005 |
| FR-NOTIF-007 | The system shall record `read_at_utc` when a user reads a notification. | BR-NOTIF-006 |
| FR-NOTIF-008 | The system shall create ranking warning notifications when a ranking result shows high cancellation risk for a series. | BR-NOTIF-007 |
| FR-NOTIF-009 | The system shall send ranking warning notifications to active Mangaka contributors of the affected series. | BR-NOTIF-008 |
| FR-NOTIF-010 | The system shall send task assignment notifications to assigned users when page tasks are created. | BR-NOTIF-009 |
| FR-NOTIF-011 | The system shall send review result notifications to relevant contributors when proposal or chapter review decisions are recorded. | BR-NOTIF-010 |
| FR-NOTIF-012 | The system shall allow board poll notifications to be sent to Editorial Board Members when a new board poll is opened. | BR-NOTIF-011 |
| FR-NOTIF-013 | The system shall allow Mangaka users to send an in-app publication frequency change request notification to the Editorial Board Chief after the official frequency has been set by board decision. | BR-NOTIF-012, BR-PUB-014 |
| FR-NOTIF-014 | The system shall treat publication frequency change request notifications as communication records, not as an official request table or authoritative approval record. | BR-NOTIF-013 |
| FR-NOTIF-015 | The system shall not treat notifications as the authoritative audit trail. | BR-NOTIF-014 |
| FR-NOTIF-016 | The system shall audit-log important workflow actions that also create notifications when auditability is required. | BR-NOTIF-015 |

---

## 3.20 Status History and Auditability

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-HIST-001 | The system shall not require separate status-history tables for every workflow entity in MVP. | BR-HIST-001 |
| FR-HIST-002 | The system shall store the current workflow state of an entity directly on the main entity using `status_code`. | BR-HIST-002 |
| FR-HIST-003 | The system shall represent important workflow events through existing domain records when applicable. | BR-HIST-003 |
| FR-HIST-004 | The system shall use proposal records, board polls, board votes, chapter reviews, page versions, task records, ranking results, and notifications as workflow evidence where applicable. | BR-HIST-003 |
| FR-HIST-005 | The system shall record traceability-sensitive workflow actions in audit logs, including approvals, cancellations, status changes, task changes, board poll actions, and account actions. | BR-HIST-004 |
| FR-HIST-006 | The system shall use notifications to inform actors of important events, not as authoritative status history. | BR-HIST-005 |
| FR-HIST-007 | The system shall treat `updated_at_utc` as the latest operational update time, not as a complete status transition timeline. | BR-HIST-006 |
| FR-HIST-008 | The system shall use event-specific timestamps such as `submitted_at_utc`, `reviewed_at_utc`, `entered_at_utc`, `created_at_utc`, and `released_at_utc` for meaningful business events. | BR-HIST-007 |
| FR-HIST-009 | The system shall treat detailed status transition history tables as future scope unless required for audit demonstration or advanced workflow analytics. | BR-HIST-008 |

---