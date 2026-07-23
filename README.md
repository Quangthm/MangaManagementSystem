# Manga Creation Workflow and Publishing Management System

> A university MVP project for manga production workflow management, proposal review, chapter/page versioning, assistant task coordination, editorial review, board polling, publication planning, simulated/dynamic ranking, notifications, and auditability.

---

## 1. Project Overview

**Manga Creation Workflow and Publishing Management System** is a web-based workflow platform for managing manga production from early series proposal to chapter/page production, editorial review, board decision-making, and release planning.

The system is designed to support human manga production teams. It does **not** replace professional drawing tools such as Clip Studio Paint, Photoshop, Krita, or other illustration workspaces.

The MVP focuses on:

- user accounts and role-based access,
- series and contributor management,
- formal proposal submission and review,
- chapter, page, and page-version management,
- page regions, annotations, and assistant tasks,
- chapter-level editorial review,
- board poll and vote workflow,
- publication-period-based chapter release planning,
- series-level simulated/manual vote input and dynamic ranking,
- in-app notifications,
- audit logs and workflow traceability,
- optional AI-assisted segmentation/OCR/translation suggestions.

---

## 2. Repository Status

**Private development repository**

This repository is used for internal team collaboration and MVP development. It may contain unfinished features, temporary data, incomplete documentation, experimental screens, and unstable APIs.

Do **not** treat this repository as production-ready.

---

## 3. MVP Scope

### 3.1 Included in MVP

| Area | MVP Direction |
|---|---|
| Users and accounts | One MVP role per account. Email/password registration follows the current repository flow: reCAPTCHA is validated before a 6-digit email OTP is requested, the OTP must be verified before account creation, and successful self-registration still creates a `PENDING_APPROVAL` account. Google sign-up remains supported and also creates a pending account unless approval policy changes. Admin can activate, reject, or disable accounts. Rejected accounts cannot log in and keep their email/username reserved in MVP. Users have a display name for readable UI identity. |
| File management | Store actual media in Cloudinary and store file references/metadata in the system. Uploaded files keep a content fingerprint to support traceability, integrity checks, and optional duplicate-file warnings. |
| Series management | Manage series profile, unique slug, lifecycle status, primary language, cover image, normalized genres, normalized tags, publication frequency, and optional source series reference. Genres are stored through `Genre`/`SeriesGenre`; tags are stored through `Tag`/`SeriesTag`. `series_id` is the internal identity; `slug` is the URL identity. No separate `series_code` is used in MVP. |
| Series contributors | Manage team membership through `SeriesContributor`, not a direct lead Mangaka field on `Series`. |
| Series proposals | Store formal submitted proposal versions in `SeriesProposal`. Revisions create new proposal rows. Proposal history keeps important submitted proposal content such as proposal title, synopsis, proposal file, version number, status, and submission metadata; it does not snapshot genres, tags, or cover file. Review screens read current genres, tags, and cover from the locked series metadata. |
| Board workflow | Use `SeriesBoardPoll` and `SeriesBoardVote`. Editorial Board Chief opens, closes, and cancels board polls, specifies publication frequency when opening `START_SERIALIZATION` polls, can also vote, and board results are computed from votes. |
| Chapters and pages | Use `Chapter`, `ChapterPage`, and `ChapterPageVersion`. Chapter number uniqueness applies only to non-cancelled chapters: a `CANCELLED` chapter does not reserve its chapter number, so a new non-cancelled chapter may reuse the same number while the cancelled record remains preserved for history. |
| Chapter submission | Submit chapters by changing `Chapter.status_code` to `UNDER_REVIEW`; do not create a separate `ChapterSubmission` table. |
| Page regions | Store accepted AI/manual regions as `PageRegion` records linked to `ChapterPageVersion`. A DOT region is represented by `width = 0` and `height = 0`; a rectangular area requires both values to be greater than `0`; mixed zero/non-zero width/height is invalid. |
| Annotations | Store annotations through linked `PageRegion` records, not direct annotation coordinates. |
| Page tasks | Use `ChapterPageTask` as the task header and `ChapterPageTaskRegion` to link one or more target regions; page context is derived from linked regions. |
| Editorial review | Store final chapter-level review decisions in `ChapterEditorialReview`. |
| Publication planning | Use `PublicationPeriod` for weekly, monthly, and yearly business calendar/reporting periods. Chapter scheduling remains chapter-level through `Chapter.planned_release_date` and release timestamps. An authorized schedule/reschedule date is valid when it is on or after the current publication business date (`planned_release_date >= today`); publication frequency supplies suggestions/warnings only and does not hard-block an otherwise valid date. Mangaka may provide/update preferred publication frequency only while the series is in `PROPOSAL_DRAFT`; Editorial Board Chief specifies official frequency in a `START_SERIALIZATION` poll, and an approved poll applies that frequency. After board decision, Mangaka may request a change through in-app notification, but only Editorial Board Chief may directly change the official frequency with a required audit reason. |
| Ranking | Use series-level simulated/manual vote input entered by Editorial Board Members / Editorial Board Chief in `SeriesVoteInput` for completed weekly periods, tied to `PublicationPeriod`. All authenticated roles, including Assistants, may view ranking results subject to normal authorization. Rankings are calculated dynamically through `vw_SeriesRanking`; equal `ranking_score` values share the same rank, while secondary fields only stabilize display order inside a tied rank. No `SeriesRankingSnapshot` table is used in the current MVP workflow. |
| Notifications | Use in-app notifications only. Notifications are not the audit trail. |
| Auditability | Use current status on main records plus domain records and audit logs. Avoid generic status-history tables. |
| AI support | AI suggestions are advisory and human-reviewed. Accepted regions are saved as `PageRegion`; final translated pages are saved as `ChapterPageVersion`. |

### 3.2 Out of Scope for MVP

Do **not** implement these unless the team leader explicitly changes the scope:

- full drawing, inking, brush, or layer editor,
- public manga reader portal,
- public reader accounts,
- e-commerce, subscription, payment, payroll, or salary modules,
- full automatic manga localization workflow,
- `ChapterTranslation`, `PageRegionTranslation`, or localized asset tables,
- persistent AI job execution history if accepted AI output is enough,
- `ChapterSubmission`,
- `SeriesBoardDecision`,
- generic status-history tables for every workflow entity,
- full AI model comparison dashboard,
- automatic AI approval/rejection of pages, chapters, proposals, or board decisions.

---

## 4. Main Actor Model

The project uses both role-based actors and shared permission-based actor groups.

| Actor / Group | Responsibility |
|---|---|
| New User | Registers an account and waits for approval or rejection before accessing protected workspace functions. Rejected users cannot log in or reuse the same reserved email/username in MVP. |
| General System User | Uses shared authenticated features such as file display, display-name profile updates, status visibility, timestamps, and notifications. |
| Authorized Workflow Participant | Views workflow lists, queues, dashboards, or records allowed for their role. |
| Authorized Page Workspace User | Accesses page-level editing, annotation, segmentation, translation-support, or page-version feedback tools when permitted. Editorial Board Members are excluded unless explicitly granted page workspace permissions. |
| Mangaka | Creates and manages series, proposals, chapters, pages, page versions, production regions, assistant tasks, task review, chapter submission, ranking monitoring, and responses to editorial feedback. |
| Assistant | Views assigned page tasks, sees linked regions, uploads completed output as a new page version, tracks assigned task history, and may view system ranking results. Assistants cannot create or modify ranking input. |
| Tantou Editor | Reviews proposals and chapters, creates page-region annotations, records chapter-level editorial decisions, reviews translation-related issues, and monitors publication/ranking context. |
| Editorial Board Member | Views board polls, votes `APPROVE`, `REJECT`, or `ABSTAIN`, provides rejection reasons, enters simulated/aggregated reader vote input for completed weekly periods, and reviews ranking/cancellation-risk evidence. |
| Editorial Board Chief | Opens, closes, and cancels board polls; specifies publication frequency when opening `START_SERIALIZATION` polls; may directly change official series publication frequency with a required audit reason; may also vote `APPROVE`, `REJECT`, or `ABSTAIN`; provides rejection reasons when voting `REJECT`; and reviews ranking/cancellation-risk evidence. |
| Admin | Manages accounts, including activation, rejection, and disabling; also manages file deletion workflow, audit visibility, traceability, and system-level management. Admin does not own chapter cancellation overrides, publication scheduling, board poll control, official publication-frequency changes, or simulated reader vote input in MVP. |

### Actor consolidation decisions

| Older actor term | Current MVP handling |
|---|---|
| Authorized Contributor | Merged into Mangaka. |
| Reviewer / Authorized Reviewer | Merged into Tantou Editor. |
| Auditor | Merged into Admin for MVP audit visibility. |
| System Admin | Merged into Admin for MVP account, audit, and traceability responsibilities. |

---

## 5. Core Workflow Summary

### 5.1 Account and File Setup

1. For email/password registration, the current Web flow validates reCAPTCHA before allowing the user to request an email OTP.
2. The system sends a 6-digit OTP to the registration email; the user must verify that OTP before the registration request is submitted.
3. After successful verification and account creation, the new account remains `PENDING_APPROVAL` until Admin approval. Google sign-up follows the same pending-approval policy.
4. Users may provide a display name; if not provided, the system can use the username as the display name.
5. Users may optionally attach profile files such as avatars or registration portfolios.
6. Uploaded files are stored through the system file workflow, with Cloudinary holding the actual file and the system keeping file metadata.
7. File fingerprints help support integrity checks, traceability, and optional duplicate-file warnings when the MVP UI supports them.

### 5.2 Series and Proposal Workflow

1. Mangaka creates or maintains a series profile while the series is in `PROPOSAL_DRAFT`, including current cover, normalized genres, normalized tags, language, and proposed publication frequency.
2. The series team is managed through `SeriesContributor`; the draft creator is an active Mangaka contributor.
3. Mangaka submits a formal proposal version with a required proposal file stored through `FileResource` using purpose `SERIES_PROPOSAL`.
4. Submission requires at least one active Mangaka contributor, but it does not require an active Tantou Editor to already be assigned.
5. Submission creates a `SeriesProposal` row, stores submission-time proposal-content snapshots, and moves both the proposal and series to `UNDER_EDITORIAL_REVIEW`. `SeriesProposal` does not snapshot genres, tags, or cover file; review screens read those from the current locked `Series`, `SeriesGenre`, and `SeriesTag` metadata.
6. Newly submitted proposals appear in the editorial review queue for active Tantou Editors.
7. Tantou Editors may choose/claim or be assigned to handle proposals from the queue; multiple Tantou Editors may be contributors to the same series.
8. During editorial review, a Tantou Editor may request revision, cancel the proposal, or pass the proposal to board review.
9. Revision requires non-empty comments and may include an optional markup file; the series returns to `PROPOSAL_DRAFT` for a new proposal version.
10. Editorial cancellation requires both non-empty comments and a markup file; the submitted proposal and related series become `CANCELLED`.
11. If the proposal passes editorial review, it moves to `UNDER_BOARD_REVIEW` for board voting.
12. Editorial Board Chief opens a valid `START_SERIALIZATION` board poll and specifies the publication frequency to apply if the poll is approved.
13. Editorial Board Chief and Editorial Board Members vote through `SeriesBoardVote`.
14. When the poll closes, the system computes the result from vote counts.
15. If approved, the computed result updates the series/proposal status and applies the board-specified publication frequency as the official frequency.

### 5.3 Chapter and Page Workflow

1. Mangaka creates chapters under a series.
2. Chapter number uniqueness applies only to non-cancelled chapter attempts. A `CANCELLED` chapter remains stored for history but no longer reserves its chapter number, so a new active chapter may reuse that number.
3. Mangaka creates logical pages under chapters.
4. Each page file upload or revision creates a `ChapterPageVersion`.
5. Only one page version should be current per logical page.
6. Old page versions remain available for traceability and comparison.
7. A chapter is submitted for review by changing `Chapter.status_code` to `UNDER_REVIEW` after required active page versions exist and all submission gates pass.
8. Page creation, page deletion, and page-version upload are locked while the chapter is under review, approved, scheduled, or released.
9. If revision is requested, the chapter becomes editable again.

### 5.4 Page Region, Annotation, and Task Workflow

1. Authorized page workspace users can create page regions manually or use AI-assisted segmentation suggestions.
2. Saved regions are stored as `PageRegion` records linked to a specific `ChapterPageVersion`.
3. Region geometry permits either a DOT (`width = 0` and `height = 0`) or a rectangle (`width > 0` and `height > 0`); mixed zero/non-zero dimensions are invalid.
4. AI suggestions are temporary until a user chooses which regions to save.
5. New AI regions should be compared against existing saved regions to reduce duplicates.
6. Annotations are linked to `PageRegion`, not stored as direct annotation coordinates.
7. Whole-page feedback is represented by a manually created full-page region.
8. Page tasks are created through `ChapterPageTask` and target one or more regions through `ChapterPageTaskRegion`.
9. Assistants submit task output as a new page version.
10. Mangaka or a Tantou Editor, when permitted by workflow rules, reviews the submitted page version before the task is completed.

### 5.5 Editorial Review Workflow

1. Tantou Editor reviews submitted chapters page by page.
2. Page-level annotations support review but do not replace the final chapter-level decision.
3. Final chapter review decisions are stored in `ChapterEditorialReview`.
4. Allowed decisions are `APPROVED`, `REVISION_REQUESTED`, and `CANCELLED`.
5. Revision and cancellation decisions require meaningful comments or a markup file.
6. Creating a chapter editorial review updates `Chapter.status_code` according to the decision.

### 5.6 Board Poll, Publication Frequency, Scheduling, and Ranking Workflow

1. Editorial Board Chief may open board polls for `START_SERIALIZATION` or `CANCEL_SERIALIZATION`.
2. `START_SERIALIZATION` polls require the Editorial Board Chief to specify the publication frequency to apply if approved.
3. Polls require a non-empty reason and may have an optional scheduled end time; the Chief may also close them manually.
4. Editorial Board Chief and Editorial Board Members vote only while the poll is open.
5. Votes are preserved after the poll closes or is cancelled.
6. Board results are computed from approve/reject vote counts.
7. Abstain votes are counted separately but do not directly determine approval or rejection in MVP.
8. Approved `START_SERIALIZATION` poll results apply the board-specified publication frequency as the official series frequency.
9. After board decision, Mangaka may request a publication frequency change through in-app notification to the Editorial Board Chief.
10. Editorial Board Chief may directly change official publication frequency only with a required audit reason.
11. `PublicationPeriod` defines weekly, monthly, and yearly business calendar buckets used for ranking, reporting, and publication guidance.
12. Weekly publication periods start on Monday and end on Sunday, and the owning month is the month with at least four days in that week.
13. Monthly and yearly publication periods follow normal calendar start and end dates.
14. Authorized Mangaka and Tantou Editors may schedule or reschedule an otherwise eligible chapter for any date on or after the current publication business date (`planned_release_date >= today`).
15. `Series.publication_frequency_code` is advisory for scheduling: it may provide default date suggestions and warnings, but it must not reject an otherwise valid date merely because the date is outside a suggested weekly/monthly cadence.
16. Publication-period membership uses the publication business date, not the raw UTC date. Scheduled chapters normally use `Chapter.planned_release_date`; released chapters use `released_at_utc` converted to Vietnam publication time and then cast to a date.
17. Editorial Board Members and the Editorial Board Chief may enter series-level simulated/manual ranking input only for eligible completed weekly publication periods.
18. All authenticated roles, including Assistants, may view ranking results subject to normal authorization.
19. Rankings are calculated dynamically from `SeriesVoteInput` through `vw_SeriesRanking`; the current MVP does not use `SeriesRankingSnapshot` because there is no ranking-finalization workflow.
20. Ranking position is tie-preserving: series with the same weighted `ranking_score` receive the same rank. Secondary ordering such as `average_rating`, `rating_count`, `reading_count`, and stable `series_id` ordering may be used to make the display deterministic inside a tied rank, but those fields must not split equal-score ranks.


---

## 6. System Architecture

The intended architecture is a **Distributed Monolith with an optional local AI advisory service**.

- The main application handles business workflow, access control, auditability, files, and UI.
- The AI service is separated because segmentation/OCR/translation support is easier to run in Python.
- The backend communicates with the AI service through JSON APIs.
- SQL Server stores workflow metadata and audit records.
- Cloudinary stores actual media files.

### 6.1 Planned Technology Stack

| Layer | Technology |
|---|---|
| Main application | C# / .NET 8 |
| Frontend | Blazor Web App / Blazor Server |
| UI library | MudBlazor |
| Canvas / page interaction | Fabric.js over HTML5 Canvas |
| Backend | ASP.NET Core Web API |
| Architecture style | Clean Architecture-style layers |
| Database | SQL Server |
| ORM | Entity Framework Core |
| Schemas | `auth`, `manga`, `audit` |
| File storage | Cloudinary + system file metadata |
| AI service | Optional Python FastAPI local/internal service |
| AI communication | JSON API bridge |
| Real-time updates | Optional SignalR for notifications/status updates |
| Version control | GitHub |

---

## 7. Main System Modules

### 7.1 Users and Access Control

Handles registration, account status, role-based access, login restrictions, profile files, and admin account management.

Key rules:

- Each account has exactly one MVP system role.
- Email/password registration validates reCAPTCHA, then requires verification of a 6-digit email OTP before account creation.
- Google sign-up remains supported as a separate external-identity registration path.
- New accounts created through either registration path start as `PENDING_APPROVAL`.
- Pending users cannot access protected workspace functions.
- Admin can activate, reject, or disable accounts.
- Rejected and disabled accounts cannot log in.
- Rejected accounts keep their email and username reserved in MVP.
- Users have display names for readable UI identity.
- Registration approval/rejection history is represented through current user status and audit logs, not a separate registration request table.
- Avatar and portfolio files are stored through the file workflow.

### 7.2 File Resource and Cloudinary

Handles file metadata and media storage references.

Key rules:

- Actual media files are stored in Cloudinary.
- The system stores file metadata and references for business records.
- Business tables should reference system file records, not raw Cloudinary URLs.
- File fingerprints support traceability, integrity checks, and optional duplicate-file warnings.
- Duplicate-file warnings are advisory and may be omitted from some MVP screens if implementation time is limited.
- Deleted file resources are excluded from normal UI queries unless viewing historical/audit data.
- Normal UI should show a safe placeholder when a referenced file is unavailable or deleted.

### 7.3 Series, Contributors, and Proposals

Handles series profile records, normalized genres and tags, contributor membership, and formal proposal submission versions.

Key rules:

- Series use `series_id` as the internal identity and `slug` as the URL identity; no separate `series_code` is used in MVP.
- Series ownership is handled through `SeriesContributor`.
- Genres are normalized through `manga.Genre` and `manga.SeriesGenre`.
- Tags are normalized through `manga.Tag` and `manga.SeriesTag`.
- Genres describe broad story categories such as Action, Fantasy, Romance, and Horror.
- Tags describe specific tropes, themes, settings, character traits, source labels, or story elements such as Isekai, School Life, Revenge, Magic, or Based on a Novel.
- Genre and tag editing follows the same series-profile editability rule: Mangaka may edit them while the series profile is editable, normally during `PROPOSAL_DRAFT`.
- When a series is under editorial or board review, current cover, genres, and tags are locked with the series profile and are displayed from current series metadata.
- Proposal revisions create new `SeriesProposal` rows.
- Submitted proposal snapshot fields should not be edited directly.
- `SeriesProposal` stores important submitted proposal content/history, such as proposal title, synopsis, proposal file, version number, status, and submission metadata.
- `SeriesProposal` does not snapshot genre, tag, or cover-file metadata in MVP.
- Editorial review information may be stored directly in `SeriesProposal` for MVP.

### 7.4 Page Versioning and Page Workspace

Handles logical pages, page uploads, page revisions, segmentation regions, annotations, translation support, and task outputs.

Key rules:

- `ChapterPage` is a logical page slot.
- A cancelled chapter attempt remains stored but does not reserve its chapter number; uniqueness is enforced among non-cancelled chapters so a new chapter may reuse the number of a cancelled attempt.
- `ChapterPageVersion` stores uploaded/revised page files.
- `PageRegion` belongs to exactly one `ChapterPageVersion`.
- `PageRegion` geometry supports DOT regions (`width = 0`, `height = 0`) and rectangular regions (both dimensions greater than `0`); mixed zero/non-zero dimensions are invalid.
- Annotations reference `PageRegion`.
- Final translated or edited pages are saved as new `ChapterPageVersion` records.

### 7.5 Page Tasks

Handles page-based work assignments and assistant submissions.

Key rules:

- Each task derives its logical page context from one or more linked `PageRegion` records.
- Each task is assigned to one assistant or authorized user.
- A task targets one or more page regions through `ChapterPageTaskRegion`.
- Task status values are `ASSIGNED`, `UNDER_REVIEW`, `COMPLETED`, and `CANCELLED`.
- Reassignment should create a new task instead of changing the original assignee.

### 7.6 Editorial Review

Handles proposal review, chapter review, markup files, comments, and final chapter decisions.

Key rules:

- Chapter editorial reviews are stored directly against `Chapter`.
- Page-specific annotations support chapter review.
- Final decisions are stored at chapter level.
- Revision and cancellation require meaningful comments or markup.

### 7.7 Board Polling and Voting

Handles board decisions through polls and votes.

Key rules:

- Editorial Board Chief creates, closes, and cancels board polls.
- Editorial Board Chief must specify publication frequency when opening a `START_SERIALIZATION` poll.
- Editorial Board Chief and Editorial Board Members vote only in open polls.
- Each voting board participant can vote once per poll.
- Rejection votes require a reason.
- Results are computed from votes.
- Approved `START_SERIALIZATION` results apply the board-specified frequency as the official series frequency.
- Editorial Board Chief may directly change official publication frequency only with a required audit reason.
- No separate `SeriesBoardDecision` table is required for MVP.

### 7.8 Publication Periods, Ranking, Notifications, and Auditability

Handles publication-period buckets, chapter release planning support, board-entered ranking evidence, dynamic ranking, in-app notifications, and audit evidence.

Key rules:

- `PublicationPeriod` stores weekly, monthly, and yearly business calendar periods.
- Weekly periods run Monday through Sunday and are named after the month that owns at least four days of that week.
- Monthly and yearly periods follow normal calendar start and end dates.
- Publication period membership is determined by the publication business date, not by the raw UTC date.
- Scheduled chapters normally use `Chapter.planned_release_date` as their publication business date.
- Released chapters use `released_at_utc` converted to Vietnam publication time, then cast to the date part.
- Authorized scheduling accepts `planned_release_date >= current publication business date`; a past date is invalid.
- `Series.publication_frequency_code` provides scheduling suggestions and warnings only. It does not hard-enforce weekly/monthly buckets.
- Ranking input is series-level simulated/manual aggregated evidence entered by Editorial Board Members or the Editorial Board Chief for completed weekly publication periods.
- Assistants and the other authenticated roles may view ranking results but cannot create or update ranking input unless separately authorized as a board role.
- `SeriesVoteInput` stores one row per series and input publication period, including rating count, average rating, reading count, data source note, and entry metadata.
- Ranking uses the MAL-style weighted score `(v / (v + m)) * R + (m / (v + m)) * C`; `reading_count` is popularity evidence/tie-break context and is not a direct score boost.
- Rank values preserve equal-score ties. `DENSE_RANK()` should rank by weighted `ranking_score` only; secondary ordering fields may stabilize display order within a tied rank but must not make tied scores receive different ranks.
- `vw_SeriesRanking` calculates ranking score and rank position dynamically from `SeriesVoteInput`.
- The current MVP does not use `SeriesRankingSnapshot` because there is no ranking-finalization workflow.
- Notifications help users notice workflow events, including publication-frequency change requests, but are not the audit trail.
- Important workflow actions are audit-logged.
- Avoid separate status-history tables unless a future requirement needs them.


---

## 8. AI Assistance Scope

AI features are optional/advisory support tools. They must remain human-reviewed.

AI may support:

- panel detection,
- speech bubble detection,
- character region detection,
- SFX/background region suggestions,
- OCR extraction,
- translation suggestions,
- coordinate/bounding-box suggestions,
- JSON output to the .NET backend,
- displaying detected regions on the Fabric.js canvas.

AI must **not**:

- automatically approve pages or chapters,
- automatically reject pages or chapters,
- automatically make board decisions,
- replace editors, mangaka, assistants, or board members,
- claim perfect segmentation,
- claim fully automatic professional manga localization.

---

## 9. Important Data Design Decisions

| Decision | MVP Direction |
|---|---|
| Status history | Store current status on main records and use audit/domain records for important events. Do not create generic status-history tables for every entity. |
| User account decision | Email/password self-registration follows reCAPTCHA → 6-digit email OTP verification → account creation. Google sign-up remains supported. Both paths create `PENDING_APPROVAL` accounts unless the approval policy changes. Admin may change accounts to `ACTIVE`, `REJECTED`, or `DISABLED`. `REJECTED` and `DISABLED` accounts cannot log in; rejected accounts keep email/username reserved in MVP. |
| Display identity | Use `display_name` for readable UI display while keeping `username` as the login/system identifier. |
| File references | Business tables reference system file records; Cloudinary details stay inside file metadata. |
| File fingerprints | Store a file fingerprint for uploaded business files to support traceability, integrity checks, and optional duplicate warnings without globally blocking file reuse. |
| Series genres and tags | Use normalized current metadata: `Genre`/`SeriesGenre` for broad story categories and `Tag`/`SeriesTag` for specific tropes, themes, settings, character traits, source labels, or story elements. Do not store genre/tag text as proposal history tables in MVP. |
| Proposal snapshots | `SeriesProposal` snapshots important submitted proposal content, not derived/current metadata. It does not snapshot genre, tag, or cover file; review screens load current locked series metadata for those values. |
| Chapter numbering and submission | A `CANCELLED` chapter does not reserve its chapter number; uniqueness applies to non-cancelled chapters, allowing a new active chapter to reuse a cancelled chapter's number while preserving the cancelled record. Submit review by setting `Chapter.status_code = UNDER_REVIEW`; do not create `ChapterSubmission`. |
| Board decision and publication frequency | Editorial Board Chief owns normal board poll opening, closing, and cancellation; must specify publication frequency for `START_SERIALIZATION`; approved `START_SERIALIZATION` results apply that frequency; compute result from `SeriesBoardPoll` and `SeriesBoardVote`; do not create `SeriesBoardDecision`. |
| Publication periods / scheduling | Use `PublicationPeriod` for weekly, monthly, and yearly business calendar/reporting buckets. Weekly periods are Monday-Sunday and belong to the month with at least four days in that week. Publication period membership uses publication business date, not raw UTC date. Chapter scheduling accepts dates on or after the current publication business date; frequency is advisory and must not hard-enforce a weekly/monthly bucket. |
| Dynamic ranking | Use completed-week `SeriesVoteInput` plus `vw_SeriesRanking`; do not create `SeriesRankingSnapshot` unless a future finalized-ranking workflow is introduced. Equal weighted scores share a rank; `DENSE_RANK()` ranks by `ranking_score`, with secondary fields used only for deterministic display ordering inside a tie. Ranking results are viewable by authenticated roles including Assistants; input remains board-role controlled. |
| Translation | Do not create structured translation tables for MVP; save final edited/translated page as `ChapterPageVersion`. |
| AI history | Do not store persistent AI job history if accepted AI output as `PageRegion` is enough. |
| PageRegion geometry | Support either DOT regions (`width = 0`, `height = 0`) or rectangle regions (both dimensions `> 0`). Mixed zero/non-zero dimensions are invalid. |
| Annotation coordinates | Do not store direct annotation coordinates; derive location from linked `PageRegion`. |
| Contributor ownership | Use `SeriesContributor`; do not store direct lead Mangaka ownership on `Series`. |
| Reader module | No public reader module in MVP; ranking uses series-level simulated/manual aggregated input entered by Editorial Board Members. |
| Admin scope | Admin includes account, audit, traceability, file deletion, and system-level management responsibilities for MVP, but not chapter cancellation overrides, board poll control, official publication-frequency changes, publication scheduling, or simulated reader vote input. |

---

## 10. Documentation References

For detailed traceability, `business-rules.md`, `functional-requirements.md`, and `user-stories.md` are the authoritative planning documents; this README is a high-level project summary.

The repository should keep these planning documents aligned:

- `Context.md` — teammate and AI-assistant handoff context.
- `business-rules.md` — MVP business rules and constraints.
- `functional-requirements.md` — technical “The system shall...” requirements.
- `user-stories.md` — actor-based user stories.
- `business-flows-use-cases.md` — agreed business-flow/use-case notes.

When one of these files changes, update the others if the change affects scope, actors, requirements, or implementation boundaries.

---

## 11. Development Notes

- Keep the MVP practical for a university software engineering project.
- Prefer clear workflow records over unnecessary extra tables.
- Avoid adding payroll, salary, payment, public reader, or full drawing-editor features.
- Keep AI optional and advisory.
- Keep file upload behavior traceable without making duplicate warnings mandatory on every screen.
- Enforce access control in the backend, not only by hiding frontend buttons.
- Preserve workflow history through domain records and audit logs.
- Treat cancelled chapter attempts as historical records that do not reserve their chapter number.
- Reject planned release dates before the current publication business date; allow today or later.
- Preserve equal-score ranking ties; do not use unique IDs inside the rank expression to break them.
- Do not delete important workflow evidence when status changes or revisions occur.

---

## 12. Project Status

This README describes the intended MVP direction and architecture. Implementation details may change as the team finalizes database schema, API endpoints, UI screens, and GitHub issues.
