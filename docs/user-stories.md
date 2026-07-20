# Revised User Stories by Actor

Project: Manga Creation Workflow and Publishing Management System  
Basis: Verified MVP business rules, updated PageRegion segmentation rules, and permission-based actor grouping.

> **Latest alignment update — 2026-07-04:** This version updates publication scheduling stories to the advisory scheduling model. Publication frequency now provides suggestions and warnings, not hard date enforcement; Mangaka and Tantou Editors may schedule/reschedule future planned release dates when allowed; Editors enforce hold/release actions; on-hold chapters require a new planned date to return to schedule; and auto-hold/release automation remain deferred.

> **Latest series lifecycle alignment — 2026-07-19:** `HIATUS` is the paused-series status. Active Mangaka and Tantou Editor contributors may pause/resume serialized series. Only active Mangaka contributors may mark a serialized or hiatus series as `COMPLETED`; completion cancels unreleased chapters and their distinct active `ASSIGNED`/`UNDER_REVIEW` page tasks after warning and confirmation, freezes future business mutations, preserves released chapters and terminal task history, and keeps completed series visible in rankings.

> **Latest notification alignment — 2026-07-20:** User stories now reflect exact-series notification scoping, task reassignment notices, board decision notifications for both closure and manual cancellation, publication-schedule notifications that exclude the initiating actor, and account approval through both in-app notification and email. `RANKING_WARNING` remains pending final behavior definition.

## Actor Consolidation and Shared Actor Groups Applied

| Previous Actor Group / Issue | Final Actor Group | Handling |
|---|---|---|
| Authorized Contributor | Mangaka | Merged into Mangaka; duplicate series/chapter/page stories removed. |
| Reviewer / Authorized Reviewer | Tantou Editor | Merged into Tantou Editor; duplicate annotation and task-review stories removed. |
| Auditor / System Admin | Admin | Merged into Admin; duplicate audit/traceability stories removed. |
| Broad `System User` for page tools | Authorized Page Workspace User | Page segmentation, region editing, OCR/translation assistance, and page-version feedback are moved out of `System User` so Editorial Board Members are not accidentally included. |
| Shared workflow list/filtering behavior | Authorized Workflow Participant | Generic workflow-list filtering applies only to users who have permission to view a workflow queue/list for their role. |
| Duplicated Mangaka/Editor AI segmentation stories | Authorized Page Workspace User + role-specific stories | Shared segmentation mechanics are grouped, while Mangaka and Tantou Editor keep separate purpose/value stories. |
| Duplicated Mangaka/Editor AI/OCR translation stories | Authorized Page Workspace User + role-specific stories | Shared human-reviewed AI/OCR behavior is grouped, while Mangaka keeps finalization ownership and Tantou Editor keeps review/checking context. |

## Shared Actor Definitions

| Actor Group | Meaning |
|---|---|
| General System User | Any approved authenticated user using common system features such as file display, status visibility, timestamps, and in-app notifications. |
| Authorized Workflow Participant | A user who is allowed to view a specific workflow list, queue, or dashboard for their role, such as task lists, proposal queues, review queues, board poll lists, or admin queues. |
| Authorized Page Workspace User | A user who is permitted to access the chapter-level workspace for page-level editing, review, annotation, AI segmentation, AI/OCR translation support, or page-version feedback. AI tools are available to all users in this actor group for workspaces they can access. This normally includes Mangaka and Tantou Editor, and may include Assistant only for assigned page/task work. Editorial Board Members are excluded unless explicitly granted page workspace permissions. |


### Series Lifecycle Story Notes

- Active Mangaka contributors and active Tantou Editor contributors may pause/resume a serialized series through `HIATUS`.
- Only active Mangaka contributors may mark a `SERIALIZED` or `HIATUS` series as `COMPLETED`.
- Completed series are immutable for normal business mutations, but remain visible in rankings when vote input exists.

---

## 1. New User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-NEW-001 | BR-USER-002, BR-USER-003, BR-USER-011, BR-USER-012 | As a New User, I want to register an account and receive pending approval or rejection status, so that I understand why I cannot access protected workspace functions immediately or create another account with the same email/username after rejection. |
| US-NEW-002 | BR-USER-007, BR-USER-008, BR-USER-009 | As a New User, I want to optionally attach an avatar or portfolio file during account setup, so that my profile can support identity display and admin review. |
| US-NEW-003 | BR-USER-013, BR-USER-017 | As a New User, I want the system to create a display name for me during registration or external login, so that my account has a readable name in the system even if I do not provide one manually. |
| US-NEW-004 | BR-NOTIF-023, BR-NOTIF-024 | As a pending user whose account is approved by an Admin, I want to receive an in-app account-approval notification and an approval email, so that I know I can access the protected system. |

---

## 2. General System User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-USER-001 | BR-FILE-001, BR-FILE-002, BR-FILE-007, BR-FILE-009 | As a General System User, I want uploaded and generated files to be managed through the system file workflow, so that proposals, covers, avatars, portfolios, page-version files, and editorial attachments are handled consistently. |
| US-USER-002 | BR-FILE-008 | As a General System User, I want deleted or unavailable files to show a safe placeholder, so that the page remains usable without exposing broken file references. |
| US-USER-003 | BR-HIST-002, BR-HIST-006 | As a General System User, I want current workflow status to be visible on the main record, so that I can quickly understand the latest state without needing a separate status-history page. |
| US-USER-004 | BR-HIST-007 | As a General System User, I want meaningful event timestamps to be visible where relevant, so that submitted, reviewed, voted, created, and released actions are understandable. |
| US-USER-005 | BR-NOTIF-001, BR-NOTIF-005, BR-NOTIF-006 | As a General System User, I want to view unread in-app notifications and mark them as read, so that I can manage important workflow updates. |
| US-USER-006 | BR-NOTIF-003, BR-NOTIF-004 | As a General System User, I want notifications to link to their related business record when available, so that I can navigate directly to the relevant series, chapter, task, proposal, poll, publication period, or ranking result. |
| US-USER-007 | BR-USER-014, BR-USER-015, BR-USER-016 | As a General System User, I want other users to be shown by display name instead of only username, so that task assignments, annotations, notifications, board votes, and contributor lists are easier to understand. |
| US-USER-008 | BR-USER-018, BR-USER-019, BR-USER-021 | As a General System User, I want to update my display name without entering my password, so that I can adjust my visible profile name without changing my login identity or account security settings. |
| US-USER-009 | BR-FILE-011, BR-FILE-012, BR-FILE-013, BR-FILE-014, BR-FILE-015, BR-FILE-016 | As a General System User, I may receive an optional warning when I upload a file that appears identical to an existing active file, so that I can avoid accidental duplicate uploads when the MVP UI supports this warning. |
| US-USER-010 | BR-FILE-017, BR-FILE-018, BR-FILE-019, BR-FILE-020 | As a General System User, I want the system to accept only the approved file formats for each file purpose, so that proposal documents, covers, page versions, portfolios, attachments, and avatars are stored consistently and safely. |
| US-USER-011 | BR-NOTIF-018, BR-NOTIF-019, BR-NOTIF-026 | As an active contributor of a series, I want to receive a board-decision notification when that series' poll closes with an outcome or is manually cancelled by the Editorial Board Chief, so that I know the board workflow result or cancellation. |
| US-USER-012 | BR-NOTIF-020, BR-NOTIF-021, BR-NOTIF-022, BR-NOTIF-026 | As an active contributor of a series, I want to receive publication-schedule notifications when another contributor causes a chapter to enter `SCHEDULED` or changes an existing scheduled date, while avoiding self-notifications and non-scheduled date-only noise, so that I stay aware of real publication-plan changes. |

### File Upload Acceptance Note

Uploaded business files should follow the MVP file-purpose acceptance matrix in `business-rules.md` and `functional-requirements.md`. In particular, `SERIES_PROPOSAL` accepts only `.pdf`, `.doc`, and `.docx` formal documents in MVP.


---

## 3. Authorized Workflow Participant

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-WORKFLOW-001 | BR-PGTASK-015, BR-PROP-017, BR-PROP-019 | As an Authorized Workflow Participant, I want workflow lists and queues to be filterable by relevant status and ownership fields, so that I can manage the records that my role is allowed to see. |

---

## 4. Authorized Page Workspace User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-PAGE-001 | BR-REG-014, BR-REG-015, BR-TRANS-006 | As an Authorized Page Workspace User, I want AI suggestions to remain advisory and human-reviewed, so that AI output does not automatically approve, reject, or finalize workflow decisions. |
| US-PAGE-002 | BR-REG-006, BR-REG-007, BR-REG-016, BR-REG-017, BR-REG-019, BR-REG-020, BR-REG-021, BR-REG-022, BR-REG-023, BR-REG-024, BR-REG-025, BR-REG-026, BR-REG-027, BR-REG-033, BR-REG-034 | As an Authorized Page Workspace User, I want to load saved page regions, optionally run AI-assisted segmentation again, update regions when permitted, and delete only unused regions that are not connected to tasks or annotations, so that useful workflow-linked regions remain traceable while mistaken unused regions can be removed. |
| US-PAGE-003 | BR-TRANS-001, BR-TRANS-006, BR-TRANS-008, BR-TRANS-009 | As an Authorized Page Workspace User, I want AI/OCR translation suggestions to remain editable and human-reviewed, so that translation support does not automatically become final page content. |
| US-PAGE-004 | BR-REG-012, BR-ANN-001, BR-ANN-002, BR-ANN-003, BR-ANN-004, BR-ANN-005, BR-ANN-018 | As an Authorized Page Workspace User, I want annotations to link to one or more page regions while deriving their page-version context from those regions, so that feedback remains accurate after later uploads. |
| US-PAGE-005 | BR-WORKSPACE-001, BR-WORKSPACE-002, BR-WORKSPACE-003, BR-WORKSPACE-004 | As an Authorized Page Workspace User, I want one chapter-level workspace with left navigation across chapters, pages, and versions, so that I can work on the selected chapter without jumping between disconnected page screens. |
| US-PAGE-006 | BR-WORKSPACE-005, BR-WORKSPACE-006, BR-REG-028, BR-TRANS-010 | As an Authorized Page Workspace User, I want AI segmentation and AI/OCR translation tools to be available in the workspace when I have access to the relevant chapter/page version, so that all authorized production and review roles can use AI assistance. |
| US-PAGE-007 | BR-WORKSPACE-007 | As an Authorized Page Workspace User, I want the workspace to show only the role-specific actions I am allowed to perform, so that AI tools are shared but workflow authority remains controlled. |
| US-PAGE-008 | BR-WORKSPACE-009, BR-WORKSPACE-010, BR-WORKSPACE-011, BR-WORKSPACE-012 | As an Authorized Page Workspace User, I want the workspace back action to return me to the series page, review queue, task list, or dashboard I came from, so that I can continue my original workflow after inspecting or editing chapter content. |
| US-PAGE-009 | BR-REG-024, BR-REG-025, BR-ANN-014 | As an Authorized Page Workspace User, I want to create annotations using existing saved regions, newly created regions, or both, so that feedback can be linked to the exact areas being discussed. |
| US-PAGE-010 | BR-ANN-015, BR-ANN-016, BR-ANN-017, BR-ANN-020, BR-ANN-021 | As an Authorized Page Workspace User, I want annotation resolution to preserve the original feedback and follow creator-role permissions, so that production notes and editorial feedback remain traceable and controlled. |
| US-PAGE-011 | BR-REG-033, BR-REG-034 | As an Authorized Page Workspace User, I want the system to block deletion of page regions that are linked to tasks or annotations, so that deleting a visual region does not break assignment or feedback history. |

## 5. Mangaka

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-MANGAKA-001 | BR-SERIES-003, BR-SERIES-005, BR-SERIES-006, BR-SERIES-006A, BR-SERIES-006B, BR-SERIES-007, BR-SERIES-008, BR-SERIES-009, BR-SERIES-015, BR-SERIES-022, BR-SERIES-025 | As a Mangaka, I want to create and update a series draft profile with title, synopsis, genres, tags, language, cover image, source series, and proposed publication frequency while it is still in `PROPOSAL_DRAFT`, so that the manga project is clearly represented before formal review. |
| US-MANGAKA-001C | BR-SERIES-007A, BR-SERIES-007B, BR-SERIES-007C | As a Mangaka, I want to crop and preview my series cover before saving a draft, so that the cover displays correctly in the required 2:3 portrait frame. |
| US-MANGAKA-001D | BR-SERIES-007A, BR-SERIES-007B | As a Mangaka, I want the system to upload only the cropped series cover image, so that the final displayed cover matches my confirmed crop without storing the original source image. |
| US-MANGAKA-001E | BR-SERIES-007D | As a Mangaka, I want a warning when my selected cover image is smaller than the recommended size, so that I understand the final cover may look blurry after upscaling. |
| US-MANGAKA-001A | BR-SERIES-017, BR-SERIES-018, BR-SERIES-019, BR-SERIES-020, BR-SERIES-021 | As a Mangaka, I want the system to generate and update the draft slug from my title while the series is still a draft, then lock that slug for the stable `/series/{slug}` URL after serialization, so that URLs remain clean without requiring slug history in MVP. |
| US-MANGAKA-001B | BR-SERIES-020, BR-SERIES-022, BR-SERIES-023, BR-SERIES-024 | As a Mangaka, I want normal series profile editing to lock after the series leaves `PROPOSAL_DRAFT`, so that editorial and board review use stable information while I continue production through chapters, pages, versions, regions, and tasks. |
| US-MANGAKA-002 | BR-SERIES-010, BR-SERIES-011, BR-SC-001, BR-SC-003, BR-SC-004, BR-SC-005 | As a Mangaka, I want to manage active contributors for a series, so that the production team is visible and duplicate active membership is avoided. |
| US-MANGAKA-003 | BR-SC-006, BR-SC-007, BR-SC-008 | As a Mangaka, I want my draft series to be submitted for editorial review without needing a Tantou Editor to already be assigned, so that active editors can later find and handle my proposal from the review queue. |
| US-MANGAKA-004 | BR-PROP-001, BR-PROP-004, BR-PROP-004A, BR-PROP-005, BR-PROP-006, BR-PROP-021, BR-FILE-018 | As a Mangaka, I want to submit a formal series proposal with a `.pdf`, `.doc`, or `.docx` proposal document and locked proposal title/synopsis snapshot, so that editors can evaluate the formal submitted content while current cover, genres, and tags remain series metadata. |
| US-MANGAKA-005 | BR-PROP-002, BR-PROP-003, BR-PROP-007, BR-PROP-008 | As a Mangaka, I want proposal revisions to create new proposal versions instead of overwriting old submissions, so that previous review packages remain historically accurate. |
| US-MANGAKA-006 | BR-PROP-010, BR-PROP-011 | As a Mangaka, I want to withdraw a proposal before final approval or cancellation, so that I can stop review when the submission is no longer intended to proceed. |
| US-MANGAKA-007 | BR-PROP-009, BR-PROP-017, BR-PROP-018 | As a Mangaka, I want to view proposal status and version history for my series, so that I understand previous submissions and current review progress. |
| US-MANGAKA-007A | BR-PROP-017, BR-PROP-019 | As a Mangaka, I want to filter my submitted series proposals by title, genre, tag, status, and sort order, so that I can track review progress more easily. |
| US-MANGAKA-008 | BR-CH-001, BR-CH-002, BR-CH-003, BR-CH-006, BR-CH-014, BR-PUB-SCHEDULE-001, BR-PUB-SCHEDULE-005, BR-PUB-SCHEDULE-006 | As a Mangaka, I want to create or plan chapters with future planned release dates and helpful frequency suggestions, so that active series chapters remain organized while still allowing realistic release exceptions. |
| US-MANGAKA-009 | BR-CP-001, BR-CP-002, BR-CP-003, BR-CP-004 | As a Mangaka, I want to create logical chapter pages with unique page numbers, so that each chapter can be organized page by page. |
| US-MANGAKA-010 | BR-CP-005, BR-CP-006, BR-CP-007, BR-CP-009, BR-CP-010, BR-CP-012, BR-CP-013, BR-CP-014, BR-CP-021, BR-CP-023, BR-CP-025 | As a Mangaka, I want each explicitly saved page upload or revision to create a new page version while keeping only one current version, so that selected files do not become history until I confirm them and old work remains preserved. |
| US-MANGAKA-010A | BR-CP-024, BR-CP-026 | As a Mangaka, I want saved page versions to remain undeletable in the current MVP, so that outdated or mistaken saved versions are superseded by newer versions instead of losing production history. |
| US-MANGAKA-011 | BR-CP-019, BR-CP-020 | As a Mangaka, I want to soft-delete logical pages while preserving their historical versions, so that removed draft pages no longer appear active but past work is not lost. |
| US-MANGAKA-012 | BR-REG-019, BR-REG-020, BR-REG-024, BR-REG-025, BR-PGTASK-007, BR-PGTASK-012 | As a Mangaka, I want to use saved or AI-assisted page regions for translation, production annotations, and task assignment, so that I can prepare production work areas for myself and assistants. |
| US-MANGAKA-012A | BR-ANN-013, BR-ANN-020, BR-ANN-022 | As a Mangaka, I want to create, update, and resolve Mangaka-created production-tracking annotations on my series, so that I can mark weak page regions and track internal corrections before or after review. |
| US-MANGAKA-012B | BR-ANN-021, BR-ANN-023 | As a Mangaka, I must not update or resolve Tantou Editor-created annotations, so that formal editorial feedback remains controlled by the editorial role. |
| US-MANGAKA-013 | BR-TRANS-002, BR-TRANS-004, BR-TRANS-005, BR-TRANS-006, BR-TRANS-007, BR-TRANS-009, BR-REG-018 | As a Mangaka, I want to review, edit, override, and approve AI/OCR translation suggestions before saving the final translated or edited page as a new page version, so that final page content remains human-controlled and traceable. |
| US-MANGAKA-014 | BR-PGTASK-001, BR-PGTASK-003, BR-PGTASK-004, BR-PGTASK-005, BR-PGTASK-014, BR-PGTASK-032, BR-PGTASK-033, BR-PGTASK-034, BR-PGTASK-035 | As a Mangaka, I want to create region-linked page tasks for an active Assistant contributor only while the parent series is `SERIALIZED` or `HIATUS` and the chapter is `DRAFT` or `REVISION_REQUESTED`, so that production work is assigned clearly only in editable production states without storing `chapter_page_id` directly on the task. |
| US-MANGAKA-015 | BR-PGTASK-007, BR-PGTASK-008, BR-PGTASK-009, BR-PGTASK-011, BR-PGTASK-012 | As a Mangaka, I want page tasks to target one or more linked page regions, so that feedback and production work are tied to exact areas and the task page context is derived from those regions. |
| US-MANGAKA-016 | BR-PGTASK-017, BR-PGTASK-018, BR-PGTASK-019, BR-PGTASK-020, BR-PGTASK-021, BR-PGTASK-022 | As a Mangaka, I want to review the submitted page version before completing a task, so that assistants do not approve their own task output. |
| US-MANGAKA-017 | BR-PGTASK-024, BR-PGTASK-025, BR-PGTASK-027, BR-PGTASK-028, BR-PGTASK-030 | As a Mangaka, I want reassignment to be handled by cancelling or completing the old task and creating a new one, so that assignment history is not overwritten. |
| US-MANGAKA-018 | BR-CH-SUB-001, BR-CH-SUB-002, BR-CH-SUB-005 | As a Mangaka, I want to submit a chapter for editorial review by moving it to UNDER_REVIEW after required active page versions exist, so that the editor reviews a stable chapter draft. |
| US-MANGAKA-019 | BR-CH-SUB-003, BR-CH-SUB-004, BR-CH-CANCEL-006, BR-CH-016, BR-PUB-SCHEDULED-001, BR-PUB-SCHEDULED-002 | As a Mangaka, I want chapter pages to lock during review, approved, scheduled, on-hold, released, and cancelled states, but become editable again when revision is requested, so that scheduled and terminal work stays stable while fixable work can be corrected. |
| US-MANGAKA-020 | BR-CH-REV-003, BR-CH-REV-010, BR-CH-REV-011, BR-CH-REV-012, BR-CH-REV-013 | As a Mangaka, I want to view chapter review history and final decisions, so that I understand whether the chapter was approved, requires revision, or was cancelled. |
| US-MANGAKA-021 | BR-CH-REV-007, BR-CH-CANCEL-003, BR-CH-CANCEL-004, BR-CH-CANCEL-006, BR-CH-CANCEL-007, BR-CH-CANCEL-008, BR-CH-CANCEL-009, BR-CH-CANCEL-010 | As a Mangaka, I want cancellation feedback and preserved chapter materials to remain available, and I want to create a new replacement draft with the same chapter number when a chapter is cancelled, so that previous work is not lost while redo work starts cleanly. |
| US-MANGAKA-022 | BR-RANK-005, BR-RANK-008, BR-RANK-009 | As a Mangaka, I want to track ranking trends and cancellation-risk evidence, including completed series when ranking input exists, so that I can respond to series performance issues while historical performance remains visible. Automatic `RANKING_WARNING` notification behavior remains pending final definition. |
| US-MANGAKA-023 | BR-PUB-001, BR-PUB-006, BR-PUB-009, BR-PUB-DATE-001, BR-PUB-DATE-002, BR-PUB-DATE-003, BR-CH-013, BR-CH-015 | As a Mangaka, I want to view chapter-level release planning, scheduled status, publication business dates, and derived delays, so that I understand when my approved chapters are planned for release. |
| US-MANGAKA-024 | BR-PUB-005, BR-PUB-011, BR-PUB-012, BR-PUB-013 | As a Mangaka, I want to provide or update `publication_frequency_code` while my series is still in `PROPOSAL_DRAFT`, so that the board can consider my proposed frequency before the board-approved frequency overrides it. |
| US-MANGAKA-026 | BR-PUB-SCHEDULE-001, BR-PUB-SCHEDULE-002, BR-PUB-SCHEDULE-003, BR-PUB-SCHEDULE-004, BR-PUB-SCHEDULE-005, BR-PUB-SCHEDULE-006, BR-PUB-SCHEDULE-007, BR-PUB-SCHEDULE-010 | As a Mangaka, I want frequency-based suggested dates and warnings while still being allowed to choose any future planned release date, so that I can coordinate realistic release timing with my editor. |
| US-MANGAKA-027 | BR-CH-016, BR-CH-017, BR-CH-018, BR-PUB-SCHEDULED-001, BR-PUB-SCHEDULED-002, BR-PUB-SCHEDULED-003, BR-PUB-SCHEDULED-005, BR-PUB-SCHEDULED-006 | As a Mangaka, I want scheduled and on-hold chapters to clearly show why editing is locked, how the current release plan changed, and that returning from hold requires a new planned date, so that I understand why page workflows are unavailable. |
| US-MANGAKA-028 | BR-PUB-016, BR-PUB-017, BR-PUB-SCHEDULE-011 | As a Mangaka, I want to see authorized contributor contact information and audit-visible schedule changes, so that I can coordinate release dates with my editor outside the system workflow. |
| US-MANGAKA-029 | BR-SERIES-027, BR-SERIES-028, BR-SERIES-029, BR-SERIES-030, BR-SERIES-031, BR-SERIES-032, BR-PUB-SCHEDULED-012, BR-PUB-SCHEDULED-013 | As a Mangaka contributor, I want to set my serialized series to `HIATUS` and later resume it to `SERIALIZED`, so that chapter release can pause without blocking drafting, review, scheduling, or rescheduling work. |
| US-MANGAKA-030 | BR-SERIES-033, BR-SERIES-034, BR-SERIES-035, BR-SERIES-036, BR-SERIES-037, BR-SERIES-038, BR-SERIES-039, BR-SERIES-040, BR-SERIES-041, BR-SERIES-043, BR-SERIES-044, BR-SERIES-045, BR-SERIES-046, BR-SERIES-047, BR-RANK-009 | As a Mangaka contributor, I want to mark my serialized or hiatus series as `COMPLETED` only after seeing a warning about unreleased chapters and their active tasks, so that unfinished chapter/task work is cancelled atomically while released chapters, terminal task history, historical records, and ranking visibility are preserved. |
| US-MANGAKA-031 | BR-CH-021, BR-SERIES-030 | As a Mangaka contributor, I want to create new chapter drafts only for my `SERIALIZED` or `HIATUS` series, so that proposal/review, completed, cancelled, null, or unknown series states cannot start new normal production chapters. |
| US-MANGAKA-032 | BR-NOTIF-010, BR-NOTIF-017, BR-NOTIF-026 | As an active Mangaka contributor, I want to receive proposal-decision and chapter-decision notifications for my exact series, so that I know when editorial review requests revision, passes a proposal to board review, cancels a proposal, or approves/revises/cancels a chapter. |
| US-MANGAKA-033 | BR-NOTIF-016, BR-NOTIF-026 | As an active Mangaka contributor, I want to receive a `TASK_REVIEW` notification when an Assistant submits assigned work for review on my exact series, so that I can review the submission promptly. |


---

## 6. Assistant

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-ASSISTANT-001 | BR-PGTASK-001, BR-PGTASK-003, BR-PGTASK-004 | As an Assistant, I want to see the exact logical chapter page assigned to me, so that I understand my responsibility. |
| US-ASSISTANT-002 | BR-PGTASK-007, BR-PGTASK-013 | As an Assistant, I want linked page regions to be highlighted in my task view, so that I know exactly which areas need work. |
| US-ASSISTANT-003 | BR-PGTASK-016, BR-PGTASK-017, BR-PGTASK-018, BR-CP-015, BR-CP-022 | As an Assistant, I want to submit completed task output as a new page version for the same logical page derived from the task's linked regions, so that my work enters the normal page-version review pipeline. |
| US-ASSISTANT-004 | BR-PGTASK-027, BR-PGTASK-029 | As an Assistant, I want completed and cancelled task records to remain under my account history, so that my contribution history is preserved. |
| US-ASSISTANT-005 | BR-ANN-001, BR-ANN-004, BR-ANN-005, BR-ANN-018 | As an Assistant, I want to view annotations through their linked page regions, including annotations linked to multiple regions, so that I understand which feedback applies to the page version used in my assigned work. |
| US-ASSISTANT-006 | BR-NOTIF-009 | As an Assistant, I want to receive an in-app notification when a page task is assigned to me, so that I know what work I need to do. |
| US-ASSISTANT-007 | BR-NOTIF-009 | As an Assistant, I want task reassignment notifications to tell me whether my original task was cancelled and reassigned, including the reason, or whether a replacement task was newly assigned to me, so that I understand exactly which task record and responsibility now applies. |

---

## 7. Tantou Editor

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-EDITOR-001 | BR-SERIES-003, BR-SERIES-010, BR-SC-005, BR-SC-008 | As a Tantou Editor, I want to view series status, profile information, and active contributors before review, so that I understand the work and responsible team. |
| US-EDITOR-001A | BR-PROP-024 | As a Tantou Editor, I want to view unassigned submitted proposals in an editorial review queue, so that I can choose or be assigned to handle a proposal after it is submitted. |
| US-EDITOR-002 | BR-PROP-009, BR-PROP-012, BR-PROP-013, BR-PROP-015, BR-PROP-015A, BR-PROP-017, BR-PROP-019, BR-PROP-020 | As a Tantou Editor, I want to review submitted proposal versions directly through the proposal record, so that I can request revision, cancel, or pass the proposal to board review. |
| US-EDITOR-003 | BR-PROP-015, BR-PROP-021 | As a Tantou Editor, I want to provide non-empty comments and optionally attach a markup file when requesting proposal revision, so that the Mangaka receives clear correction feedback for the next proposal version. |
| US-EDITOR-003A | BR-PROP-015A, BR-PROP-021 | As a Tantou Editor, I want proposal cancellation to require both non-empty comments and a markup file, so that terminal editorial rejection is clearly justified and traceable. |
| US-EDITOR-004 | BR-REG-019, BR-REG-020, BR-REG-022, BR-REG-023, BR-REG-026, BR-REG-027, BR-CP-018 | As a Tantou Editor, I want to use saved or AI-assisted page regions for page-level feedback and editorial review, so that review comments are tied to accurate page areas. |
| US-EDITOR-005 | BR-TRANS-001, BR-TRANS-006, BR-TRANS-009 | As a Tantou Editor, I want to review AI/OCR translation suggestions when needed during editorial review, so that translation-related issues can be checked before approval or revision. |
| US-EDITOR-006 | BR-ANN-001, BR-ANN-002, BR-ANN-003, BR-ANN-004, BR-ANN-005, BR-ANN-006, BR-ANN-013 | As a Tantou Editor, I want to create editorial-review annotations linked to one or more page regions, so that page-specific, multi-region, and whole-page feedback can be recorded without storing direct annotation coordinates. |
| US-EDITOR-007 | BR-ANN-005, BR-ANN-006, BR-ANN-007, BR-ANN-008, BR-ANN-009, BR-ANN-010 | As a Tantou Editor, I want each annotation to include a valid issue type, text, creator, and created time, so that feedback is categorized and understandable. |
| US-EDITOR-008 | BR-ANN-015, BR-ANN-016, BR-ANN-017, BR-ANN-020, BR-ANN-021 | As a Tantou Editor, I want to resolve both Mangaka-created production annotations and Tantou Editor-created review annotations without deleting them, so that completed feedback can be distinguished from unresolved feedback while remaining traceable. |
| US-EDITOR-008A | BR-ANN-023, BR-ANN-024, BR-ANN-026 | As a Tantou Editor, I want to update unresolved annotation text when clarification is needed, so that Mangaka production notes and editorial feedback can be made clearer while preserving audit history. |
| US-EDITOR-009 | BR-CP-018, BR-CH-SUB-002, BR-CH-REV-001, BR-CH-REV-002 | As a Tantou Editor, I want to review submitted chapters page by page while recording the final decision at chapter level, so that detailed feedback and formal approval remain separate. |
| US-EDITOR-010 | BR-CH-REV-006, BR-CH-REV-007, BR-CH-REV-008, BR-CH-REV-010, BR-CH-REV-011, BR-CH-REV-011A, BR-CH-REV-012, BR-CH-REV-013, BR-CH-REV-015 | As a Tantou Editor, I want to record APPROVED, REVISION_REQUESTED, or CANCELLED decisions, with approval moving a chapter to SCHEDULED when a future planned release date already exists, so that chapter outcomes are clear and enforceable. |
| US-EDITOR-011 | BR-CH-CANCEL-001, BR-CH-CANCEL-002, BR-CH-CANCEL-003, BR-CH-CANCEL-004, BR-CH-CANCEL-006, BR-CH-REV-006, BR-CH-REV-007, BR-CH-REV-013 | As a Tantou Editor, I want to cancel a chapter through an editorial review decision only when the current attempt should not proceed, so that serious rejection decisions are formally recorded, the cancelled attempt becomes terminal, and chapter materials are preserved without deletion. |
| US-EDITOR-012 | BR-CH-010, BR-CH-018, BR-PUB-SCHEDULED-004, BR-PUB-SCHEDULED-005, BR-PUB-SCHEDULED-006 | As a Tantou Editor, I want to place a scheduled chapter ON_HOLD with a reason and later return it to schedule only with a new future planned date, so that paused work can resume with an explicit new plan. |
| US-EDITOR-014 | BR-CH-017, BR-PUB-SCHEDULED-003, BR-PUB-SCHEDULE-001, BR-PUB-SCHEDULE-005, BR-PUB-SCHEDULE-006, BR-PUB-SCHEDULE-007, BR-PUB-SCHEDULE-010 | As a Tantou Editor, I want to reschedule chapters to any future planned date with advisory frequency warnings and confirmation, so that release planning can handle bulk releases, breaks, vacations, and editorial exceptions. |
| US-EDITOR-015 | BR-CH-014, BR-CH-015, BR-PUB-001, BR-PUB-SCHEDULED-007, BR-PUB-SCHEDULED-008 | As a Tantou Editor, I want to set planned release dates and release eligible chapters with confirmation, so that approved chapters can move to scheduled or released status while planned-versus-actual timing remains audit-visible. |
| US-EDITOR-017 | BR-PUB-SCHEDULE-007, BR-PUB-SCHEDULE-010, BR-PUB-SCHEDULED-011 | As a Tantou Editor, I want future bulk schedule or bulk release actions to require confirmation and remain audit-visible, so that campaign and catch-up releases can be handled intentionally. |
| US-EDITOR-013 | BR-PGTASK-020, BR-PGTASK-021, BR-PGTASK-022 | As a Tantou Editor, I want to review submitted task page versions when acting as an authorized reviewer, so that submitted page-version output is accepted before completion. |
| US-EDITOR-016 | BR-RANK-001, BR-RANK-002, BR-RANK-007, BR-RANK-008, BR-RANK-009 | As a Tantou Editor, I want to view dynamic ranking and cancellation-risk evidence, including completed series when ranking input exists, so that editorial decisions and historical review can consider series performance without automatic cancellation. |
| US-EDITOR-018 | BR-SERIES-027, BR-SERIES-028, BR-SERIES-029, BR-SERIES-030, BR-SERIES-031, BR-SERIES-032, BR-PUB-SCHEDULED-012, BR-PUB-SCHEDULED-013 | As a Tantou Editor contributor, I want to set a serialized series to `HIATUS` and resume it to `SERIALIZED`, so that release can be paused or restarted while normal drafting, review, scheduling, and rescheduling remain available when allowed. |
| US-EDITOR-019 | BR-NOTIF-012, BR-NOTIF-013, BR-NOTIF-026 | As an active Tantou Editor contributor, I want to receive proposal-review and chapter-review notifications only for my exact series, so that I see review work I am actually responsible for without notifications from unrelated series. |


---

## 8. Editorial Board Member

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-BOARD-001 | BR-PROP-013, BR-PROP-014, BR-PROP-016, BR-PROP-021 | As an Editorial Board Member, I want to view proposals under board review with supporting proposal material, so that I can participate in serialization decisions. |
| US-BOARD-002 | BR-BOARD-POLL-010, BR-BOARD-VOTE-001 | As an Editorial Board Member, I want to view open board polls, so that I can vote only while a poll is active. |
| US-BOARD-003 | BR-BOARD-VOTE-002, BR-BOARD-VOTE-003, BR-BOARD-VOTE-005 | As an Editorial Board Member, I want to vote APPROVE, REJECT, or ABSTAIN at most once per poll, so that the voting process is fair. |
| US-BOARD-004 | BR-BOARD-VOTE-004 | As an Editorial Board Member, I want to provide a non-empty reason when voting REJECT, so that my objection is clear. |
| US-BOARD-005 | BR-BOARD-VOTE-008, BR-BOARD-VOTE-009, BR-BOARD-VOTE-010, BR-BOARD-VOTE-011 | As an Editorial Board Member, I want my votes to remain visible after a poll is closed or cancelled, so that my participation is traceable but does not directly change status by itself. |
| US-BOARD-006 | BR-BOARD-POLL-006, BR-RANK-001, BR-RANK-002, BR-RANK-007, BR-RANK-008, BR-RANK-009 | As an Editorial Board Member, I want to view dynamic ranking and cancellation-risk evidence for relevant series, including completed series when ranking input exists, so that board decisions and historical performance review are supported by performance context. |
| US-BOARD-007 | BR-PUB-002, BR-PUB-003, BR-PUB-004, BR-PUB-006 | As an Editorial Board Member, I want to view the current publication frequency of serialized series, so that I understand the high-level release plan. |
| US-BOARD-008 | BR-NOTIF-011 | As an Editorial Board Member, I want to receive an in-app notification when a new board poll opens, so that I can vote on time. |
| US-BOARD-009 | BR-SERIES-VOTE-001, BR-SERIES-VOTE-002, BR-SERIES-VOTE-003, BR-SERIES-VOTE-004, BR-SERIES-VOTE-005, BR-SERIES-VOTE-006, BR-SERIES-VOTE-007, BR-SERIES-VOTE-008, BR-SERIES-VOTE-009, BR-SERIES-VOTE-010, BR-SERIES-VOTE-013, BR-SERIES-VOTE-014, BR-SERIES-VOTE-015 | As an Editorial Board Member, I want to enter one simulated or aggregated series vote input for a series and publication period with valid rating count, average rating, reading count, source note, timestamp, and entered-by user, so that ranking can be demonstrated without a public reader module. |

---

## 9. Editorial Board Chief

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-BOARDCHIEF-001 | BR-BOARD-POLL-001, BR-BOARD-POLL-002, BR-BOARD-POLL-003, BR-BOARD-POLL-004, BR-BOARD-POLL-005, BR-BOARD-POLL-007, BR-BOARD-POLL-008, BR-BOARD-POLL-009, BR-BOARD-POLL-018, BR-PUB-010 | As an Editorial Board Chief, I want to open valid `START_SERIALIZATION` or `CANCEL_SERIALIZATION` board polls with reasons, optional end times, and a required publication frequency for `START_SERIALIZATION`, so that board voting starts from a correct workflow state. |
| US-BOARDCHIEF-002 | BR-BOARD-VOTE-001, BR-BOARD-VOTE-002, BR-BOARD-VOTE-003, BR-BOARD-VOTE-004, BR-BOARD-VOTE-005 | As an Editorial Board Chief, I want to vote `APPROVE`, `REJECT`, or `ABSTAIN` at most once per poll, so that I can participate in board decisions while still following the same voting rules as other board voters. |
| US-BOARDCHIEF-003 | BR-BOARD-POLL-014, BR-BOARD-POLL-015, BR-BOARD-POLL-017 | As an Editorial Board Chief, I want to cancel invalid board polls without deleting votes, so that incorrect poll setups are invalidated but remain traceable. |
| US-BOARDCHIEF-004 | BR-BOARD-POLL-016, BR-BOARD-POLL-017, BR-BOARD-RESULT-001, BR-BOARD-RESULT-002, BR-BOARD-RESULT-003, BR-BOARD-RESULT-004, BR-BOARD-RESULT-005, BR-BOARD-RESULT-006, BR-BOARD-RESULT-007, BR-BOARD-RESULT-010 | As an Editorial Board Chief, I want to close a valid poll and view computed vote totals, so that the applicable board result can be determined without a separate decision table. |
| US-BOARDCHIEF-005 | BR-BOARD-RESULT-011, BR-BOARD-RESULT-012, BR-BOARD-RESULT-013, BR-BOARD-RESULT-014, BR-BOARD-RESULT-015, BR-BOARD-RESULT-017 | As an Editorial Board Chief, I want applicable closed poll results to update proposal or series status according to the board rules, so that board decisions affect workflow only at the correct time. |
| US-BOARDCHIEF-006 | BR-BOARD-POLL-013, BR-BOARD-POLL-015, BR-BOARD-POLL-017, BR-BOARD-RESULT-017 | As an Editorial Board Chief, I want board polls, cancelled polls, votes, and result applications to remain audit-visible, so that board-driven status changes are traceable. |
| US-BOARDCHIEF-007 | BR-PUB-015 | As an Editorial Board Chief, I want to directly change a series publication frequency only with a required audit reason, so that frequency changes remain controlled and traceable. |
| US-BOARDCHIEF-008 | BR-RANK-001, BR-RANK-002, BR-RANK-007, BR-RANK-008, BR-RANK-009 | As an Editorial Board Chief, I want to view dynamic ranking and cancellation-risk evidence, including completed series when ranking input exists, so that board oversight can use performance context without automatic cancellation. |
| US-BOARDCHIEF-009 | BR-NOTIF-018, BR-NOTIF-019, BR-NOTIF-026 | As an Editorial Board Chief, I want closing a poll or manually cancelling it to notify all active contributors of the exact affected series, so that the production team knows the board outcome or cancellation. |


---

## 10. Admin

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-ADMIN-001 | BR-USER-004, BR-USER-005, BR-USER-006, BR-USER-010, BR-USER-011, BR-USER-012, BR-HIST-004 | As an Admin, I want to view, activate, reject, disable, and audit user account actions through current account status, so that account access is controlled and account management remains traceable without a separate registration request table. |
| US-ADMIN-002 | BR-FILE-003, BR-FILE-004, BR-FILE-007 | As an Admin, I want file deletion to happen through the application workflow while historical records retain file references when needed, so that Cloudinary assets, SQL metadata, and past workflow evidence remain consistent. |
| US-ADMIN-003 | BR-SERIES-001, BR-SERIES-002, BR-HIST-004 | As an Admin, I want series identities, slug changes, and important series workflow actions to remain traceable through audit-visible records, so that series records can be reviewed reliably without requiring a separate business identifier. |
| US-ADMIN-004 | BR-SC-006, BR-SC-007 | As an Admin, I want contributor history to remain available after contributors leave a series, so that past participation can be reviewed and audited. |
| US-ADMIN-005 | BR-PROP-007, BR-PROP-012, BR-PROP-017, BR-PROP-020 | As an Admin, I want proposal versions and review metadata to remain searchable and traceable, so that proposal decisions can be reviewed later. |
| US-ADMIN-012 | BR-CH-004, BR-CH-012, BR-CH-REV-014, BR-CH-CANCEL-003, BR-CH-CANCEL-006, BR-CH-CANCEL-008 | As an Admin, I want chapter status changes, editorial decisions, cancellation decisions, active chapter-number uniqueness, and preserved chapter materials to remain traceable, so that important workflow decisions can be reviewed later. |
| US-ADMIN-013 | BR-REG-012, BR-ANN-001, BR-ANN-004, BR-ANN-005, BR-ANN-018, BR-CP-016 | As an Admin, I want page regions to preserve their page-version ownership and annotations to derive page-version context through one or more linked regions, so that review history remains accurate after new uploads. |
| US-ADMIN-014 | BR-PGTASK-027, BR-PGTASK-029, BR-PGTASK-031 | As an Admin, I want page task creation, cancellation, completion, status changes, and preserved old task records to be audit-visible, so that task workflow remains traceable. |
| US-ADMIN-017 | BR-SERIES-VOTE-013, BR-SERIES-VOTE-014, BR-SERIES-VOTE-015, BR-RANK-001 | As an Admin, I want series vote input to remain audit-visible with source evidence and entered-by information, so that simulated ranking data can be reviewed without Admin entering the vote data. |
| US-ADMIN-018 | BR-PUB-PERIOD-001, BR-SERIES-VOTE-002, BR-RANK-001, BR-RANK-002, BR-RANK-003, BR-RANK-004, BR-RANK-005 | As an Admin, I want publication periods, series vote inputs, and dynamic ranking results to remain visible for audit and traceability, so that series performance can be reviewed over time without Admin owning vote input. |
| US-ADMIN-019 | BR-NOTIF-014, BR-NOTIF-015, BR-HIST-004 | As an Admin, I want important notification-triggering workflow actions to also be audit-logged when required, so that user awareness does not replace traceability. |
| US-ADMIN-020 | BR-HIST-001, BR-HIST-002, BR-HIST-003, BR-HIST-005, BR-HIST-006, BR-HIST-008 | As an Admin, I want domain records and audit logs to serve as workflow evidence instead of separate status-history tables, so that the MVP stays simple while preserving traceability. |
| US-ADMIN-021 | BR-USER-020 | As an Admin, I want display name changes to be visible in audit logs, so that user-facing identity changes remain traceable. |
| US-ADMIN-022 | BR-USER-004, BR-NOTIF-023, BR-NOTIF-024 | As an Admin, I want approving a pending account to create an in-app `ACCOUNT_APPROVED` notification and send an approval email to that user's registered email address, so that the approved user is clearly informed through both supported channels. |

---
