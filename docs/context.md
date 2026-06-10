# Manga Creation Workflow and Publishing Management System — Updated Project Context

> **Purpose of this file:** Give teammates and AI assistants a single aligned reference for the current MVP scope, actor model, business rules, functional behavior, and implementation boundaries.  
> **Source of truth:** This context is aligned with the latest `business-rules.md`, `functional-requirements.md`, and `user-stories.md` files.  
> **Important warning:** This is **not** a payroll, salary, public reader, e-commerce, or full drawing application. Do **not** add modules such as salary calculation, payment processing, public reader accounts, monetization, or full professional drawing tools unless the team leader explicitly changes the scope.

---

## 1. Project Summary

**Project name:** Manga Creation Workflow and Publishing Management System

**Project type:** University software engineering MVP project

**Main goal:**  
Build a web-based workflow management system that helps manga production teams manage the process from **series proposal**, **chapter/page production**, **page versioning**, **assistant task assignment**, **editorial review**, **board polling**, **publication scheduling**, **ranking simulation**, **notifications**, and **auditability**.

The system supports manga production management. It does **not** replace professional drawing tools such as Clip Studio Paint, Photoshop, Krita, or Photoshop-style illustration workspaces.

---

## 2. MVP Scope Direction

The MVP should stay focused and avoid unnecessary tables unless a table represents an important business event.

### 2.1 Included MVP Areas

| Area | MVP Decision |
|---|---|
| Users and accounts | Use one MVP role per account. New users start as `PENDING_APPROVAL`. Admin activates, rejects, or disables accounts. Each user has a non-unique `display_name` for UI display; if not provided during registration or external login, it defaults to the username. Users may update their own display name without entering their account password. |
| File management | Store actual media in Cloudinary; store metadata and references in `manga.FileResource`. Every file resource must store a backend-calculated `sha256_hash`; duplicate-file warnings based on this hash are optional MVP usability behavior and may be implemented only where time allows. |
| Series management | Manage series profile, unique slug, lifecycle status, primary language, genre text, cover image, publication frequency, and optional source series reference. `series_id` is the internal backend identity; `slug` is the stable URL identity after serialization. No separate `series_code` is used in MVP. |
| Series contributors | Manage team membership through `SeriesContributor`, not a direct lead Mangaka column on `Series`. |
| Series proposals | Store formal submitted proposal versions in `SeriesProposal`; revisions create new proposal rows. |
| Board workflow | Use `SeriesBoardPoll` and `SeriesBoardVote`; Editorial Board Chief opens, closes, and cancels board polls, specifies publication frequency when opening `START_SERIALIZATION` polls, may also vote, and board results are computed from votes. Do **not** use a separate `SeriesBoardDecision` table. |
| Chapters and pages | Use `Chapter`, `ChapterPage`, and `ChapterPageVersion`. `ChapterPage` is a logical page slot; `ChapterPageVersion` stores uploaded/revised files. |
| Chapter submission | Submit a chapter by changing `Chapter.status_code` to `UNDER_REVIEW`; do **not** create a `ChapterSubmission` table. |
| Page regions | Store accepted AI/manual regions directly as `PageRegion` records linked to `ChapterPageVersion`. |
| Page annotations | Store annotation headers in `ChapterPageAnnotation` and link them to one or more `PageRegion` records through `ChapterPageAnnotationRegion`; do not store direct annotation coordinates. |
| Page tasks | Use `ChapterPageTask` as the task header and `ChapterPageTaskRegion` to link one or more target regions; the task's page context is derived from linked `PageRegion` records, not from a direct `chapter_page_id` column on `ChapterPageTask`. |
| Editorial review | Store final chapter-level review decisions in `ChapterEditorialReview`. Page annotations support the review but do not replace chapter-level decisions. |
| Publication planning | Use chapter-level planned release dates and release timestamps. Mangaka may provide/update preferred publication frequency only while the series is in `PROPOSAL_DRAFT`; Editorial Board Chief specifies the official frequency in a `START_SERIALIZATION` poll, and an approved poll applies that frequency to `Series.publication_frequency_code`. After board decision, Mangaka may request a frequency change through in-app notification, but only Editorial Board Chief may directly change the official frequency with a required audit reason. |
| Ranking | Use simulated/manual reader vote input entered by Editorial Board Members and time-based `SeriesRankingSnapshot`. No public reader module in MVP. |
| Notifications | Use in-app notifications only. Notifications are not the audit trail. |
| Auditability | Use current status on main records plus domain records and audit logs. Avoid separate status-history tables. |
| AI support | AI suggestions are advisory and human-reviewed. Accepted region output is saved as `PageRegion`; final translated pages are saved as `ChapterPageVersion`. |

### 2.2 Explicitly Out of Scope

Do **not** implement these for the MVP:

- Full drawing/inking/layer editor
- Public manga reader portal
- Public reader accounts
- E-commerce, subscription, payment, or payroll features
- Salary calculation
- Full manga localization workflow
- Separate `ChapterTranslation`, `PageRegionTranslation`, or localized asset tables
- Persistent AI job execution history if accepted AI output is enough
- `ChapterSubmission`
- `SeriesBoardDecision`
- Generic status-history tables for every entity
- Full AI model comparison dashboard
- Automatic AI approval/rejection of pages, chapters, proposals, or board decisions

---

## 3. Actor Model

The project uses **permission-based actor grouping** for shared features and role-specific user stories.

### 3.1 Main Actors

| Actor | Meaning / Responsibilities |
|---|---|
| New User | Registers an account and waits for approval before accessing protected workspace functions. |
| General System User | Any approved authenticated user using common features such as file display, display-name profile updates, status visibility, timestamps, and notifications. |
| Authorized Workflow Participant | A user allowed to view a specific workflow list, queue, or dashboard for their role. |
| Authorized Page Workspace User | A user permitted to access page-level editing, annotation, segmentation, translation-support, or page-version feedback tools. Normally includes Mangaka and Tantou Editor; Assistants may access assigned task/page work only. Editorial Board Members are excluded unless explicitly granted page workspace permissions. |
| Mangaka | Creates and manages series, proposals, chapters, pages, page versions, regions for production, task assignments, assistant task review, chapter submission, ranking monitoring, and response to editorial feedback. |
| Assistant | Views assigned page tasks, sees linked regions, uploads completed output as a new page version, and tracks task history. |
| Tantou Editor | Reviews proposals and chapters, uses page regions and annotations for feedback, records chapter-level editorial decisions, may review translation-related issues, and monitors publication/ranking context. |
| Editorial Board Member | Views board polls, votes approve/reject/abstain, provides rejection reasons, enters simulated/aggregated reader vote input, and views ranking/cancellation-risk evidence. |
| Editorial Board Chief | Opens, closes, and cancels board polls; specifies publication frequency when opening `START_SERIALIZATION` polls; may directly change official series publication frequency with a required audit reason; may also vote approve/reject/abstain; provides rejection reasons when voting reject; and views ranking/cancellation-risk evidence. |
| Admin | Manages accounts, file deletion workflow, audit visibility, traceability, and system-level management. Admin does not own chapter cancellation overrides, publication scheduling, or simulated reader vote input in MVP. |

### 3.2 Actor Consolidation Decisions

| Previous / Older Actor Term | Current Handling |
|---|---|
| Authorized Contributor | Merged into Mangaka for MVP user stories. |
| Reviewer / Authorized Reviewer | Merged into Tantou Editor for MVP user stories. |
| Auditor | Merged into Admin for this context; audit visibility is treated as an Admin responsibility unless the team later reintroduces a separate read-only Auditor role. |
| System Admin | Merged into Admin for this context; system-level traceability and audit responsibilities are included under Admin. |

---

## 4. Key Business Rules and Functional Behavior

## 4.1 AI-Assisted Translation

- AI-assisted translation is an **editing aid**, not a separate persistent translation workflow.
- The system may provide AI/OCR translation suggestions for detected text regions.
- Users must review, edit, override, and approve AI/OCR suggestions before they become part of final page content.
- The final translated or edited page is saved as a new `ChapterPageVersion`.
- Previous page versions are preserved for traceability and comparison.
- The saved translated page file is the authoritative result.
- The system does not store every OCR text, suggestion, or manual edit in structured translation rows for MVP.
- The system does not guarantee fully automatic manga localization.

## 4.2 Page Region and AI Detection

- Each `PageRegion` belongs to exactly one `ChapterPageVersion`.
- Valid region types are `PANEL`, `SPEECH_BUBBLE`, `CHARACTER`, `SFX_TEXT`, `BACKGROUND`, and `OTHER`.
- Regions are rectangular bounding boxes using `x`, `y`, `width`, and `height`.
- Width and height must be positive.
- Region source must be `AI` or `MANUAL`.
- If a user adjusts an AI-generated region, it becomes manual.
- Manual regions should not keep an AI confidence score.
- AI confidence scores must be between 0 and 1 when stored.
- Region coordinates are relative to the original uploaded page image dimensions.
- Character regions are bounding boxes in MVP, not exact masks or body outlines.
- Page regions remain linked to the page version where they were created.
- Regions should not automatically move to newer page versions.
- AI execution history is not stored in MVP; accepted AI output is stored directly as `PageRegion`.
- AI detection is advisory and requires human review before workflow use.
- `PageRegion.original_text` stores original OCR text, not final translated text.
- Saved regions may be reused for annotation, task assignment, OCR/translation review, and segmentation display.
- When opening the segmentation tool, the system should load existing saved regions.
- The system may run AI segmentation again even when saved regions exist.
- Newly detected AI regions can be temporary suggestions until the user chooses what to save.
- Duplicate or substantially overlapping regions of the same type should be prevented or warned against.
- Saved regions may be adjusted by authorized users, and updates should record `updated_at_utc` and `updated_by_user_id`.

## 4.3 File Resource and Cloudinary

- Cloudinary stores actual media files.
- SQL Server stores file metadata and references through `manga.FileResource`.
- Business records should reference `FileResource` IDs rather than raw Cloudinary URLs.
- File resources are active when `deleted_at_utc IS NULL`.
- Normal application queries should exclude deleted files unless viewing historical/audit data.
- File deletion should happen through the application workflow, not directly in Cloudinary.
- User avatars, portfolios, series covers, proposal files, chapter page-version files, editorial attachment/markup files, and task reference files should use `FileResource`.
- Supported `file_purpose_code` values for MVP are `SERIES_PROPOSAL`, `SERIES_COVER`, `CHAPTER_PAGE_VERSION`, `TASK_REFERENCE`, `EDITORIAL_ATTACHMENT`, `REGISTRATION_PORTFOLIO`, and `USER_AVATAR`.
- Every `FileResource` must store a required `sha256_hash`.
- `sha256_hash` should be calculated by the backend from the exact uploaded file bytes before file metadata is saved.
- `sha256_hash` supports file integrity checking, duplicate detection, and audit traceability.
- `sha256_hash` should not be treated as a global uniqueness rule because the same exact file may be validly reused in different workflow contexts.
- The system may optionally check `sha256_hash` before saving a file and show advisory duplicate warnings for repeated registration portfolio, proposal, cover, chapter page-version, task reference, editorial attachment, or avatar files.
- Duplicate-file warnings are optional MVP usability behavior; some UI warnings may be omitted when implementation time is limited.
- Accepted AI/translation output or assistant task output that becomes an official page file should be saved as a new `ChapterPageVersion` and use `file_purpose_code = CHAPTER_PAGE_VERSION`.
- Files attached only as task instructions, examples, or reference material should use `file_purpose_code = TASK_REFERENCE`.
- In normal UI contexts, unavailable or deleted files should show a safe placeholder instead of a broken file reference.

## 4.4 Users and Accounts

- Each account has exactly one MVP role.
- New registered users are created with `PENDING_APPROVAL` by default.
- Pending users cannot access protected workspace functions.
- Admin activates pending users by changing status to `ACTIVE`.
- Admin rejects pending users by changing status to `REJECTED`.
- Admin disables accounts by changing status to `DISABLED`.
- Rejected and disabled accounts cannot log in.
- A rejected user account keeps its email and username reserved in MVP.
- Each user account has a `display_name` for user-facing identity display.
- If a user does not provide a display name during registration or external login, the system uses the username as the default display name.
- Users may update their own display name without entering their account password.
- Updating display name does not change username, email, password, role, account status, or approval state.
- Display name does not need to be unique and should be used for UI readability in contributor lists, tasks, annotations, notifications, board votes, and audit screens.
- Display name changes should be recorded in the audit log for traceability.
- Users may optionally have avatar and portfolio files stored through `FileResource`.
- Registration approval/rejection history is handled through current user status and audit logs, not a separate registration request table.

## 4.5 Series and Contributors

- Each series must have a unique system code and URL slug.
- Each series has one lifecycle status from the approved list.
- A series becomes `SERIALIZED` after passing proposal, editorial, and board approval.
- Each series declares one primary content language.
- Genre is simple text metadata for MVP.
- Cover images use `FileResource` and should use file purpose `SERIES_COVER`.
- A series may reference another series as its source version but cannot reference itself.
- Series ownership and contributor membership are managed through `SeriesContributor`.
- A series may have multiple contributors.
- `Series.updated_at_utc` and `Series.updated_by_user_id` track profile metadata edits, not full status history.
- A `SeriesContributor` links one user to one series.
- Contributor broad role is determined by `auth.Users.role_id`.
- A user cannot be an active contributor to the same series more than once at the same time.
- `end_date IS NULL` means the contributor is active.
- Historical contributor rows are preserved.
- Before formal review or production workflow, the series should have at least one active Mangaka contributor and one active Editor contributor.

## 4.6 Series Proposal

- `SeriesProposal` stores one formal submitted proposal version.
- A series can have multiple proposal versions.
- Proposal version numbers must be positive and unique within the same series.
- A submitted proposal preserves snapshots of proposal title, synopsis, genre, and proposal file.
- A submitted proposal must include a `FileResource` with purpose `SERIES_PROPOSAL`.
- `SeriesProposal` does not store draft proposal editing.
- Once created, submitted snapshot fields should remain locked.
- If revision is requested, the corrected proposal is submitted as a new proposal version.
- Proposal status reflects the current review stage.
- Withdrawal timestamp is allowed only when proposal status is `WITHDRAWN`.
- Editorial review information may be stored directly in `SeriesProposal` because each proposal version receives at most one editorial review.
- `UNDER_BOARD_REVIEW` means the proposal passed editorial review and is waiting for board voting.
- `APPROVED` means board approval, not merely editor approval.
- Editorial revision or cancellation requires comments or markup.
- Board rejection/cancellation reasons are handled through board poll/vote records.
- Proposal lists should be retrievable/filterable by series, status, submitter, reviewer, and queue needs.
- MVP does not require a fixed minimum number of completed manga pages for proposal submission.

## 4.7 Board Poll, Vote, and Result

### Board Poll

- Editorial Board Chief may create board polls for `START_SERIALIZATION` or `CANCEL_SERIALIZATION`.
- When opening a `START_SERIALIZATION` poll, the Editorial Board Chief must specify the publication frequency to apply if the poll is approved.
- A `START_SERIALIZATION` poll may open only when `Series.status_code = UNDER_BOARD_REVIEW`.
- A `START_SERIALIZATION` poll may open only when the series has exactly one active proposal with `SeriesProposal.status_code = UNDER_BOARD_REVIEW`.
- A `START_SERIALIZATION` poll represents voting on the active under-board-review proposal even though the poll stores only `series_id`.
- A `CANCEL_SERIALIZATION` poll may open for serialized or paused series when Editorial Board Chief provides a reason.
- Low ranking or high cancellation risk may support cancellation polls but cannot be the only allowed reason.
- Each poll must have a non-empty reason.
- A poll may have an end time or be closed manually by the Editorial Board Chief.
- A series cannot have more than one open poll of the same type at the same time.
- `poll_status_code` distinguishes `OPEN`, `CLOSED`, and `CANCELLED`.
- Votes from `OPEN` polls are stored but do not update status.
- Votes from `CLOSED` polls may be used by Board Result rules.
- Votes from `CANCELLED` polls remain traceable but must not affect status.
- Poll creation, cancellation, and closure should be audit-logged.

### Board Vote

- Editorial Board Members and Editorial Board Chiefs may vote only in open polls.
- Only Editorial Board Members and Editorial Board Chiefs may cast board votes.
- Vote choices are `APPROVE`, `REJECT`, or `ABSTAIN`.
- A `REJECT` vote requires a non-empty reason.
- Each Editorial Board Member or Editorial Board Chief may vote at most once per poll.
- Votes remain preserved after closure or cancellation.
- A board vote alone does not update series/proposal status.

### Board Result

- Board results are computed from `SeriesBoardVote`, not stored in a separate `SeriesBoardDecision`.
- Vote counts are aggregated from board votes.
- `APPROVED` means approve votes exceed reject votes.
- `REJECTED` means reject votes exceed approve votes.
- `NO_DECISION` means approve and reject votes are tied.
- Abstain votes are counted separately but do not directly determine approval/rejection.
- `OPEN` poll result is treated as `PENDING`.
- `CANCELLED` poll result is treated as `INVALIDATED`.
- Only a `CLOSED` poll can produce an applicable board result.
- `START_SERIALIZATION` + `APPROVED` changes series to `SERIALIZED`, changes the active proposal to `APPROVED`, and applies the board-specified publication frequency as the official `Series.publication_frequency_code`.
- `START_SERIALIZATION` + `REJECTED` cancels the active proposal and series according to MVP workflow policy.
- `START_SERIALIZATION` + `NO_DECISION` leaves series/proposal under board review.
- `CANCEL_SERIALIZATION` + `APPROVED` changes series to `CANCELLED`.
- `CANCEL_SERIALIZATION` + `REJECTED` or `NO_DECISION` leaves series unchanged.
- Applying board poll results must be audit-logged.

## 4.8 Chapter, Page, and Page Version

### Chapter

- Each chapter belongs to exactly one series.
- Chapter number labels must be unique within the same series.
- New chapters start with `DRAFT`.
- `Chapter.status_code` stores the current workflow status only.
- Chapter statuses include `DRAFT`, `UNDER_REVIEW`, `REVISION_REQUESTED`, `APPROVED`, `SCHEDULED`, `RELEASED`, `ON_HOLD`, and `CANCELLED`.
- `planned_release_date` is optional until scheduling.
- A chapter can be `SCHEDULED` only if it has a planned release date.
- A chapter can be `RELEASED` only if it has `released_at_utc`.
- Editors may place a chapter `ON_HOLD` with a valid operational/editorial reason.
- `created_by_user_id` identifies the creator.
- `updated_at_utc` is for operational display, not full transition history.

### Chapter Page and Page Version

- A chapter may contain many logical pages.
- `ChapterPage` belongs to exactly one chapter.
- Page numbers are unique within the chapter.
- `ChapterPage` is a logical page slot, not a file.
- `ChapterPageVersion` stores uploaded/revised files for a logical page.
- Each `ChapterPageVersion` references exactly one `FileResource`.
- Upload time and uploader come from the related `FileResource`.
- Version numbers are positive and unique per logical page.
- Higher version number means newer uploaded version.
- Only one version should be current at a time.
- Old versions remain preserved.
- Replacing/revising a page creates a new `ChapterPageVersion`, not a new `ChapterPage`.
- A `ChapterPage` may be soft-deleted from active drafts without deleting historical versions.
- Page task output should reference the produced `ChapterPageVersion`.
- Page annotations remain linked to page versions through one or more linked `PageRegion` records.

## 4.9 Chapter Page Annotation

- `ChapterPageAnnotation` stores the annotation header: issue type, annotation text, annotated-by user, creation time, and resolution fields.
- `ChapterPageAnnotation` does not store direct coordinates or a direct `page_region_id` column.
- `ChapterPageAnnotationRegion` links one annotation to one or more `PageRegion` records.
- Annotation location is represented through linked `PageRegion` records.
- The page version of an annotation is derived from the linked `PageRegion.chapter_page_version_id` values.
- All regions linked to one annotation must belong to the same `ChapterPageVersion` so the annotation context remains unambiguous.
- Whole-page feedback uses a manually created full-page `PageRegion` linked through `ChapterPageAnnotationRegion`.
- Each annotation must have a valid issue type.
- Annotation text must be non-empty.
- Creator and created time must be recorded.
- Authorized users such as Tantou Editors, Mangaka reviewers, or assigned users may create annotations according to permissions.
- A page annotation may be created from existing saved regions, newly created regions, or both.
- Resolved annotations must record resolver and resolved timestamp.
- Unresolved annotations must have `resolved_by_user_id` and `resolved_at_utc` as `NULL`.
- Resolving an annotation does not delete the annotation or its linked region records.
- Old annotations remain linked to the original region/page version and should not automatically move to newer page versions.
- Annotation issue types are fixed for MVP and enforced by database constraint rather than a separate lookup table.

## 4.10 Page Task

- Each page task targets one logical page context through one or more linked `PageRegion` records; `ChapterPageTask` does not store `chapter_page_id` directly.
- A page task represents one assignment to one assistant or authorized user.
- Task assignment is region-linked and page-derived in MVP; whole-chapter task assignment is future scope.
- A task may target one or more `PageRegion` records through `ChapterPageTaskRegion`.
- Task target regions are not stored as free-text descriptions.
- The same task-region pair cannot be inserted more than once.
- All `PageRegion` records linked to the same task must belong to the same logical `ChapterPage`, derived through their `ChapterPageVersion` records.
- Region-based annotations can be used as the basis for page tasks.
- Assistant task UI should highlight linked page regions.
- Every task must have a due date.
- Task statuses are `ASSIGNED`, `UNDER_REVIEW`, `COMPLETED`, and `CANCELLED`.
- There is no `IN_PROGRESS` status in MVP.
- A task must have an uploaded page version before it enters `UNDER_REVIEW` or `COMPLETED`.
- Completed task output must reference the produced `ChapterPageVersion` for the same logical page derived from the task's linked regions.
- Assistants do not approve their own task output.
- Mangaka or permitted reviewer accepts submitted page versions before task completion.
- Assigned user should not be changed after task creation.
- Reassignment is handled by cancelling/completing the old task and creating a new one.
- Cancelled task description must include the cancellation reason.
- Task rows are preserved for traceability.
- Task creation, cancellation, completion, and status changes should be audit-logged.

## 4.11 Chapter Editorial Review and Cancellation

### Submission by Status

- MVP chapter submission is represented by changing `Chapter.status_code` to `UNDER_REVIEW`.
- A submitted chapter consists of current active page versions of non-deleted chapter pages.
- Page creation, deletion, and version upload are blocked while chapter is `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, or `RELEASED`.
- When revision is requested, the chapter becomes editable again.
- Chapter content is stored as page-level assets through `ChapterPageVersion`.
- Chapter-level submission file/PDF is future enhancement.

### Editorial Review

- Editorial reviews are stored directly against `Chapter`.
- Each `ChapterEditorialReview` belongs to one chapter and one reviewer.
- A chapter may have multiple editorial review records over revision cycles.
- Only authorized Tantou Editors or approved review roles may create chapter reviews.
- Decision values are `APPROVED`, `REVISION_REQUESTED`, and `CANCELLED`.
- `REVISION_REQUESTED` and `CANCELLED` require meaningful comments or a markup file.
- Markup files reference `FileResource`.
- Page annotations support review; `ChapterEditorialReview` stores final chapter-level decision.
- Creating a review updates chapter status according to decision and should be audit-logged.

### Cancellation

- Chapter cancellation is normally done through editorial review with `decision_code = CANCELLED`.
- Cancelled chapters cannot proceed to `SCHEDULED` or `RELEASED`.
- Cancellation does not delete pages, versions, regions, annotations, files, or review history.
- If fixable, use `REVISION_REQUESTED` instead of `CANCELLED`.
- Admin cancellation without editorial review is not allowed in MVP.

## 4.12 Publication Planning

- Detailed publication planning is chapter-level through `Chapter.planned_release_date` and `Chapter.status_code`.
- Series-level publication frequency is only the current high-level label stored as `Series.publication_frequency_code`.
- Frequency values may be `WEEKLY`, `MONTHLY`, `IRREGULAR`, or `NULL`.
- `IRREGULAR` means chapters are released when ready and do not follow a fixed weekly or monthly schedule.
- Frequency may be `NULL` before serialization or before the release approach has been decided.
- Mangaka may provide or update their preferred publication frequency only while the series is in `PROPOSAL_DRAFT`; MVP does not require a separate desired publication frequency column on `Series`.
- After the series leaves `PROPOSAL_DRAFT`, Mangaka cannot directly change the official `Series.publication_frequency_code`.
- When opening a `START_SERIALIZATION` poll, the Editorial Board Chief must specify the publication frequency for the series as part of the poll setup.
- If the `START_SERIALIZATION` poll is approved, the board-specified frequency overrides the Mangaka preference and becomes the official `Series.publication_frequency_code`.
- After a board decision has set the official frequency, Mangaka may request a publication frequency change by sending an in-app notification to the Editorial Board Chief; MVP does not require a separate official frequency-change request table.
- Editorial Board Chief may directly change `Series.publication_frequency_code` only after providing a required reason that must be written to the audit log.
- The MVP does not store publication frequency history.
- Delayed chapters can be derived from planned release date rather than a separate delay status.

## 4.13 Ranking and Reader Vote Input

### Ranking Snapshot

- Ranking data is stored as time-based `SeriesRankingSnapshot`, not permanent `Series` attributes.
- Each snapshot represents one series for one ranking period.
- A series may have many snapshots over time.
- One series may have only one snapshot per ranking period type and period start date.
- Current rank is derived from the latest relevant snapshot.
- If no snapshot exists, the series has no current ranking.
- Ranking snapshots do not automatically cancel a series.
- Cancellation risk indicators may be stored as supporting information.
- Ranking snapshots are generated from simulated/manual reader vote input in MVP.

### Reader Vote Input

- There is no public reader voting module in MVP.
- Editorial Board Members may enter simulated or aggregated reader vote input for demo purposes.
- Reader vote input is recorded for released chapters.
- A released chapter should have at most one aggregated reader vote input record in MVP.
- Vote count and feedback count cannot be negative.
- Average rating must be within the allowed range when provided.
- Reader vote input records the timestamp and entered-by user.

## 4.14 Notifications

- Notifications are in-app only for MVP.
- Each notification has exactly one recipient.
- Notifications may reference related entity type and ID.
- If `related_entity_type` is set, `related_entity_id` must also be set, and vice versa.
- Unread notification means `read_at_utc IS NULL`.
- Notifications may be sent for ranking warnings, task assignments, review results, board polls, publication-frequency change requests, and other workflow events.
- Notifications are not the authoritative audit trail.
- Important notification-triggering workflow actions should also be audit-logged when auditability is required.

## 4.15 Status History and Auditability

- Avoid separate status-history tables unless specifically required.
- Current workflow status is stored directly on main records using `status_code`.
- Important events are represented by domain records such as proposals, board polls, votes, chapter reviews, page versions, task records, ranking snapshots, notifications, and audit logs.
- Notifications are not authoritative history.
- `updated_at_utc` is operational metadata, not a full timeline.
- Specific event timestamps should be used where meaningful.
- Audit logs record important approvals, cancellations, status changes, board actions, task actions, and account actions.
- Audit logs should be queryable by actor, action, entity type, entity ID, and date range.

---

## 5. Functional Requirement Groups

The functional requirements are organized into these groups:

1. AI-Assisted Translation
2. Page Region and AI Detection
3. File Resource and Cloudinary
4. Users and Accounts
5. Series
6. Series Contributors
7. Series Proposal
8. Board Poll
9. Board Vote
10. Board Result
11. Chapter
12. Chapter Page and Page Version
13. Chapter Page Annotation
14. Page Task
15. Chapter Editorial Review and Submission
16. Chapter Cancellation
17. Publication Planning
18. Ranking and Reader Vote Input
19. Notifications
20. Status History and Auditability

When implementing, functional requirements should remain traceable to source business rules using the `BR-*` references.

---

## 6. Recommended Core Tables / Concepts

### 6.1 Auth

- `auth.Users`
- `auth.Roles` or equivalent one-role-per-user representation
- `auth.Permissions` if permission granularity is needed
- Role/access-control structures are not simple lookup tables and should not be removed merely because status lookup tables are reduced.

### 6.2 Manga Workflow

- `manga.FileResource`
- `manga.Series`
- `manga.SeriesContributor`
- `manga.SeriesProposal`
- `manga.SeriesBoardPoll`
- `manga.SeriesBoardVote`
- `manga.Chapter`
- `manga.ChapterPage`
- `manga.ChapterPageVersion`
- `manga.PageRegion`
- `manga.ChapterPageAnnotation`
- `manga.ChapterPageTask`
- `manga.ChapterPageTaskRegion`
- `manga.ChapterEditorialReview`
- `manga.ReaderVoteInput`
- `manga.SeriesRankingSnapshot`
- `manga.Notification`

### 6.3 Audit

- `audit.AuditLog` or `audit.AuditEvent`
- Optional tamper-evident hash chain if required by the team
- Audit records should not be editable by normal users.

### 6.4 Tables to Avoid for MVP

- `ChapterSubmission`
- `SeriesBoardDecision`
- `SeriesStatusHistory`
- `ChapterStatusHistory`
- `ChapterTranslation`
- `PageRegionTranslation`
- `ChapterPageLocalizedAsset`
- Persistent `AiProcessingJob` execution history, unless the team later needs AI run history
- Payment, payroll, salary, or monetization tables
- Public reader account tables

---

## 7. Status and Code Lists

Use direct `status_code` or fixed code columns with database constraints where appropriate.

### 7.1 Account Status

- `PENDING_APPROVAL`
- `ACTIVE`
- `DISABLED`

### 7.2 Chapter Status

- `DRAFT`
- `UNDER_REVIEW`
- `REVISION_REQUESTED`
- `APPROVED`
- `SCHEDULED`
- `RELEASED`
- `ON_HOLD`
- `CANCELLED`

### 7.3 Page Task Status

- `ASSIGNED`
- `UNDER_REVIEW`
- `COMPLETED`
- `CANCELLED`

### 7.4 Board Poll Status

- `OPEN`
- `CLOSED`
- `CANCELLED`

### 7.5 Board Vote Choice

- `APPROVE`
- `REJECT`
- `ABSTAIN`

### 7.6 Board Computed Result

- `PENDING` for open poll
- `APPROVED`
- `REJECTED`
- `NO_DECISION`
- `INVALIDATED` for cancelled poll

### 7.7 Region Type

- `PANEL`
- `SPEECH_BUBBLE`
- `CHARACTER`
- `SFX_TEXT`
- `BACKGROUND`
- `OTHER`

### 7.8 Region Source

- `AI`
- `MANUAL`

### 7.9 Publication Frequency

- `WEEKLY`
- `MONTHLY`
- `IRREGULAR`
- `NULL` before decided

---

## 8. Recommended Core UI Pages

### 8.1 Public / Auth UI

- Landing page
- Login
- Register
- Pending approval page
- Safe unauthorized/access-denied page

### 8.2 General Logged-In UI

- Role-based dashboard
- Notification center
- Profile/avatar view
- File preview/download through controlled endpoints

### 8.3 Mangaka UI

- Series list and series detail
- Series contributor management
- Proposal submission/version history
- Chapter list and chapter detail
- Page list and page version upload
- Page workspace with regions, annotations, and translation support
- Task creation and task review
- Chapter submission for editorial review
- Ranking/cancellation-risk view

### 8.4 Assistant UI

- Assigned task list
- Task detail with linked page/regions
- Task output upload
- Task history

### 8.5 Tantou Editor UI

- Proposal review queue
- Proposal review detail with comments/markup
- Chapter review queue
- Chapter review page workspace
- Annotation tools
- Chapter editorial decision form
- Ranking/publication context view

### 8.6 Editorial Board UI

- Open board poll list
- Board poll detail
- Vote form
- Ranking/cancellation-risk evidence view
- Historical poll/vote visibility

### 8.7 Editorial Board Chief UI

- Board poll open/cancel/close actions
- Required publication frequency selection when opening `START_SERIALIZATION` polls
- Official publication frequency change form with required audit reason
- Board poll detail
- Vote form
- Ranking/cancellation-risk evidence view
- Historical poll/vote visibility

### 8.8 Admin UI

- Account approval/activation/disable management
- File deletion workflow where permitted
- Ranking snapshot view
- Audit log / traceability view
- Admin dashboard for workflow health

---

## 9. Architecture Decisions

| Layer | Current Direction |
|---|---|
| Main application | C# / .NET |
| Frontend | Blazor / MudBlazor |
| Canvas annotation | Fabric.js or HTML5 Canvas integration |
| Database | SQL Server |
| Schemas | `auth`, `manga`, `audit` |
| File storage | Cloudinary for media; SQL Server for metadata |
| AI support | Optional local/internal Python AI service |
| AI communication | JSON API between .NET backend and AI service |
| Architecture style | Main business app with optional separate AI advisory service |

### 9.1 Figma / React Prototype Note

Figma or React/TypeScript generated prototypes should be treated as UI/UX references. The production MVP should be implemented in Blazor/MudBlazor if the team follows the current stack decision.

---

## 10. Important Modeling Decisions

### 10.1 FileResource

Use `FileResource` for metadata and references. Do not scatter Cloudinary URLs across business tables.

Recommended file-related fields/concepts:

- `file_resource_id`
- `file_purpose_code`
- `original_file_name`
- Cloudinary public ID / secure URL
- content type
- file size
- optional hash
- uploaded by
- uploaded at
- deleted by
- deleted at

### 10.2 Page Versioning

`ChapterPage` and `ChapterPageVersion` are both required concepts:

- `ChapterPage` = logical slot, such as chapter 3 page 7
- `ChapterPageVersion` = actual uploaded file/version for that slot

Only one page version should be current for a logical page at a time.

### 10.3 Page Region

`PageRegion` should link to `ChapterPageVersion`, not just `ChapterPage`.

This keeps region and annotation feedback accurate even after newer page versions are uploaded.

### 10.4 Annotation

`ChapterPageAnnotation` stores the annotation header and should not store direct annotation coordinates or a direct `page_region_id`. `ChapterPageAnnotationRegion` links one annotation to one or more `PageRegion` records, and those linked regions provide the visual/page-version context.

Whole-page feedback should use a manually created full-page region linked through `ChapterPageAnnotationRegion`.

### 10.5 Board Workflow

Use:

- `SeriesBoardPoll`
- `SeriesBoardVote`
- Computed board result

Do not use `SeriesBoardDecision` for MVP.

### 10.6 Chapter Review

Use `ChapterEditorialReview` directly against `Chapter`.

Do not use `ChapterSubmission` in MVP.

### 10.7 Ranking

Use reader vote input + ranking snapshots.

Do not build public reader voting/accounts for MVP.

### 10.8 Auditability

Use domain records plus audit logs. Do not add generic status-history tables for every workflow entity.

---

## 11. Common AI / Teammate Misinterpretations to Avoid

Do not assume or add:

- Public reader portal as MVP
- Public reader voting module
- Payment, payroll, salary, or monetization features
- Full drawing editor
- Automatic AI publishing decisions
- Automatic AI page/chapter approval
- Persistent AI job history unless required
- `ChapterSubmission`
- `SeriesBoardDecision`
- Direct coordinates or direct `page_region_id` on `ChapterPageAnnotation`; use `ChapterPageAnnotationRegion` instead
- Final translated text rows in `PageRegionTranslation`
- Status-history tables for every entity
- Registration request table for MVP approval history
- Separate Auditor/System Admin actor unless explicitly reintroduced

---

## 12. Implementation Priority Suggestion

1. Users/accounts and role-based access
2. FileResource + Cloudinary upload/preview workflow
3. Series + SeriesContributor
4. SeriesProposal + editorial proposal review fields
5. BoardPoll + BoardVote + computed board result
6. Chapter + ChapterPage + ChapterPageVersion
7. PageRegion + annotation workspace
8. ChapterPageTask + task submission/review
9. ChapterEditorialReview and chapter status transitions
10. Publication scheduling and release timestamps
11. ReaderVoteInput + SeriesRankingSnapshot
12. Notifications
13. Audit logging / traceability screens
14. Optional AI segmentation/OCR/translation support

---

## 13. Final Team Rule

When a teammate or AI assistant suggests a new feature, table, or workflow, check it against these questions:

1. Is it required by the latest business rules, functional requirements, or user stories?
2. Does it belong in the MVP, or is it future scope?
3. Does an existing domain record already provide the needed history?
4. Would adding a new table reduce clarity or create overengineering?
5. Does it preserve human control over editorial, AI, and board decisions?

If the answer is unclear, keep the MVP simple and ask the team leader before adding new scope.
