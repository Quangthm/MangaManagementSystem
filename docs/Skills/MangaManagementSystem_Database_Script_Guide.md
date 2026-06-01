# MangaManagementSystem Database Script Guide

**Based on uploaded schema:** `MangaManagementSystem_Schema.sql`  
**Database:** `MangaManagementDB`  
**Target platform:** SQL Server  
**Generated for:** Manga Creation Workflow and Publishing Management System MVP

---

## 1. Purpose

This guide explains how the uploaded SQL Server schema script is organized, how to execute it safely, and how future database changes should be added without breaking the MVP workflow.

The uploaded script currently defines:

| Object Type | Count |
|---|---:|
| Schemas | 3 |
| Tables | 21 |
| Views | 1 |
| Explicit indexes | 41 |
| Named constraints/defaults/FKs/checks/uniques | 160 |

---

## 2. Schema Areas

| Schema | Responsibility |
|---|---|
| `auth` | Roles, users, account status, and registration/account approval objects. |
| `manga` | Main manga workflow domain: file resources, series, proposals, board workflow, chapters, pages, regions, tasks, reviews, rankings, and notifications. |
| `audit` | Audit/event traceability objects. |

---

## 3. Object Inventory

| Schema | Object | Purpose |
|---|---|---|
| `auth` | `Roles` | Role lookup/seed table for Mangaka, Assistant, Tantou Editor, Editorial Board Member, and Admin. |
| `auth` | `Users` | Main account table with one role per user, login identity, avatar/portfolio references, and account status. |
| `audit` | `AuditEvent` | Append-only SQL Server Ledger audit event table with actor, action, entity, and JSON detail payload. |
| `manga` | `FileResource` | Cloudinary file metadata and application-level file references. |
| `manga` | `Series` | Manga series profile, slug/code, lifecycle status, language, cover, source series, and publication frequency. |
| `manga` | `SeriesContributor` | Links users to series contributor membership with active/history support through start/end dates. |
| `manga` | `SeriesProposal` | Formal submitted proposal version records with review metadata stored directly on the proposal. |
| `manga` | `SeriesBoardPoll` | Board poll container for START_SERIALIZATION and CANCEL_SERIALIZATION workflows. |
| `manga` | `SeriesBoardVote` | One board member vote per poll with approve/reject/abstain choices. |
| `manga` | `Chapter` | Chapter record with status, planned release date, release timestamp, and creator. |
| `manga` | `ChapterPage` | Logical page slot for a chapter with soft-delete support. |
| `manga` | `ChapterPageVersion` | Versioned uploaded page file; filtered index enforces one current version per page. |
| `manga` | `PageRegion` | Accepted AI/manual rectangular regions tied to one page version. |
| `manga` | `ChapterPageAnnotation` | Annotation/feedback records tied to a PageRegion. |
| `manga` | `ChapterPageTask` | Page-level task assigned to an assistant/user, with task type, status, due date, priority, compensation, and output page version. |
| `manga` | `ChapterPageTaskRegion` | Junction table linking tasks to one or more PageRegion records. |
| `manga` | `ChapterEditorialReview` | Final chapter-level editorial review decision with optional comments/markup. |
| `manga` | `SeriesRankingSnapshot` | Time-based simulated/manual ranking snapshot for series. |
| `manga` | `ChapterReaderVoteSnapshot` | Manual/simulated reader vote snapshot for a chapter. |
| `manga` | `Notification` | In-app notification records with optional related entity link. |

### Views

| View | Purpose |
|---|---|
| `manga.vw_SeriesBoardPollVoteSummary` | Computes vote counts and result code for board polls. It does not store a separate board decision table. |

---

## 4. Recommended Script Execution Order

1. Create database `MangaManagementDB` and switch context using `USE MangaManagementDB`.
2. Enable safe script behavior such as `SET NOCOUNT ON`.
3. Create schemas in dependency-neutral order: `manga`, `auth`, `audit`.
4. Create `auth.Roles` and seed the approved roles.
5. Create `auth.Users` without avatar/portfolio foreign keys first because those files live in `manga.FileResource`.
6. Create `manga.FileResource`, then add `auth.Users` avatar/portfolio foreign keys with `ALTER TABLE`.
8. Create `audit.AuditEvent` after `auth.Users` because audit events reference actors.
9. Create series/proposal/board objects: `Series`, `SeriesContributor`, `SeriesProposal`, `SeriesBoardPoll`, `SeriesBoardVote`.
10. Create board summary view `manga.vw_SeriesBoardPollVoteSummary` after polls, votes, and series exist.
11. Create chapter/page/page-version/region/annotation/task/review objects.
12. Create ranking, reader-vote snapshot, and notification objects.
13. Create nonclustered, unique, and filtered indexes immediately after the tables they support.
14. Run smoke-test inserts and FK/check constraint validation after the schema is created.

---

## 5. Script Layout Standard

Future schema scripts should follow this structure:

```sql
/* ============================================================
   MangaManagementSystem - Database Script
   File: 001_create_schema.sql
   Purpose: Create or update schema objects
   Author: <name>
   Date: <yyyy-mm-dd>
   ============================================================ */

SET NOCOUNT ON;
GO

-- 1. Schemas
-- 2. Lookup/seed tables
-- 3. Core auth tables
-- 4. FileResource
-- 5. Main manga workflow tables
-- 6. Junction tables
-- 7. Audit tables
-- 8. Views
-- 9. Indexes
-- 10. Seed data and smoke checks
```

For course/demo work, a single script is acceptable. For team development, split scripts into numbered files:

| Script | Purpose |
|---|---|
| `001_create_database.sql` | Create database and schemas. |
| `002_auth.sql` | Roles, users, account tables, account indexes. |
| `003_file_resource.sql` | FileResource and file-related FKs. |
| `004_series_proposal_board.sql` | Series, contributors, proposals, board polls/votes, board view. |
| `005_chapter_page_workflow.sql` | Chapters, pages, versions, regions, annotations, tasks, reviews. |
| `006_ranking_notification.sql` | Ranking snapshots, reader vote snapshots, notifications. |
| `007_audit.sql` | Audit event/ledger or audit chain objects. |
| `008_seed_data.sql` | Roles and demo data. |

---

## 6. Dependency Notes

### 6.1 User and FileResource cycle

`auth.Users` references avatar and portfolio files, while `manga.FileResource` references uploader/deleter users. The uploaded schema solves this by:

1. Creating `auth.Users` first without avatar/portfolio foreign keys.
2. Creating `manga.FileResource`.
3. Adding `fk_users_avatar_file` and `fk_users_portfolio_file` later with `ALTER TABLE`.

Keep this pattern. Do not force circular dependencies into table creation order.

### 6.2 Page version and task output integrity

`manga.ChapterPageTask` uses a composite FK to `manga.ChapterPageVersion(chapter_page_version_id, chapter_page_id)` so a completed task output must belong to the same logical chapter page as the task. This is a strong integrity pattern and should be preserved.

### 6.3 Soft delete and active/current row rules

The schema uses filtered unique indexes for important MVP rules:

| Rule | Implementation |
|---|---|
| Only one active page number per chapter | `ux_chapter_page_active_page_no` where `deleted_at_utc IS NULL` |
| Only one current page version per page | `ux_chapter_page_version_current` where `is_current_version = 1` |
| Only one open board poll of the same type per series | `ux_series_board_poll_open_type` where `poll_status_code = N'OPEN'` |
| Only one active contributor row for the same user/series | `ux_series_contributor_active_role` where `end_date IS NULL` |
| Only unread notification query support | `ix_notification_unread_recipient` where `read_at_utc IS NULL` |

---

## 7. Status and Code Values

Use `CHECK` constraints for stable MVP code sets. Keep code values in `SCREAMING_SNAKE_CASE`.

| Object | Current code values in uploaded schema |
|---|---|
| `auth.Users` | `PENDING_APPROVAL`, `ACTIVE`, `DISABLED` |
| `auth.UserRegistrationRequest` | `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED` |
| `manga.Series` | `PROPOSAL_DRAFT`, `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `SERIALIZED`, `HIATUS`, `CANCELLED`, `COMPLETED` |
| `manga.SeriesProposal` | `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `REVISION_REQUESTED`, `APPROVED`, `CANCELLED`, `WITHDRAWN` |
| `manga.SeriesBoardPoll` | `OPEN`, `CLOSED`, `CANCELLED` |
| `manga.SeriesBoardVote` | `APPROVE`, `REJECT`, `ABSTAIN` |
| `manga.Chapter` | `DRAFT`, `UNDER_REVIEW`, `REVISION_REQUESTED`, `APPROVED`, `SCHEDULED`, `RELEASED`, `ON_HOLD`, `CANCELLED` |
| `manga.PageRegion` | `AI`, `MANUAL`; types: `PANEL`, `SPEECH_BUBBLE`, `CHARACTER`, `SFX_TEXT`, `BACKGROUND`, `OTHER` |
| `manga.ChapterPageTask` | `ASSIGNED`, `UNDER_REVIEW`, `COMPLETED`, `CANCELLED`; types: `BACKGROUND`, `SHADING`, `EFFECTS`, `CLEANUP`, `DIALOGUE`, `TYPESETTING`, `REVIEW`, `OTHER` |
| `manga.ChapterEditorialReview` | `APPROVED`, `REVISION_REQUESTED`, `CANCELLED` |
| `manga.SeriesRankingSnapshot` | `WEEKLY`, `MONTHLY`, `SEASONAL` |
| `manga.Notification` | `PROPOSAL_REVIEW`, `PROPOSAL_DECISION`, `TASK_ASSIGNMENT`, `TASK_REVIEW`, `CHAPTER_REVIEW`, `RANKING_WARNING`, `PUBLICATION_SCHEDULE`, `SYSTEM_MESSAGE` |

---

## 8. Indexing Guide

The uploaded schema uses three useful index styles:

| Index Style | When to Use | Example from Schema |
|---|---|---|
| Normal nonclustered index | Common search/filter columns. | `ix_chapter_status_code` |
| Covering index with `INCLUDE` | Dashboard/list queries that need extra displayed columns. | `ix_chapter_page_task_assignee_status_due` |
| Filtered index | Active/current/unread/open subsets. | `ux_chapter_page_version_current` |

### Current explicit index inventory

| Table | Explicit indexes |
|---|---|
| `audit.AuditEvent` | `ix_audit_event_entity_time`, `ix_audit_event_actor_time` |
| `auth.UserRegistrationRequest` | `ix_user_registration_status`, `ux_user_registration_request_pending` |
| `auth.Users` | `ix_users_status_created`, `ix_users_role_status` |
| `manga.Chapter` | `ix_chapter_series_id`, `ix_chapter_status_code` |
| `manga.ChapterEditorialReview` | `ix_chapter_editorial_review_chapter_id`, `ix_chapter_editorial_review_reviewer`, `ix_chapter_editorial_review_decision_code` |
| `manga.ChapterPage` | `ix_chapter_page_chapter_id`, `ux_chapter_page_active_page_no` |
| `manga.ChapterPageAnnotation` | `ix_chapter_page_annotation_region` |
| `manga.ChapterPageTask` | `ix_chapter_page_task_assignee_status_due`, `ix_chapter_page_task_page_status`, `ix_chapter_page_task_status_due` |
| `manga.ChapterPageVersion` | `ux_chapter_page_version_current` |
| `manga.ChapterReaderVoteSnapshot` | `ix_chapter_reader_vote_snapshot_voted_at`, `ix_chapter_reader_vote_snapshot_entered_by` |
| `manga.FileResource` | `ix_file_resource_purpose_code`, `ix_file_resource_uploaded_by`, `ix_file_resource_active_by_purpose` |
| `manga.Notification` | `ix_notification_recipient_read_created`, `ix_notification_unread_recipient`, `ix_notification_related_entity` |
| `manga.PageRegion` | `ix_page_region_version_type`, `ix_page_region_type` |
| `manga.Series` | `ix_series_current_status_code` |
| `manga.SeriesBoardPoll` | `ux_series_board_poll_open_type` |
| `manga.SeriesContributor` | `ix_series_contributor_series_active`, `ix_series_contributor_user_active`, `ux_series_contributor_active_role` |
| `manga.SeriesProposal` | `ix_series_proposal_series_version`, `ix_series_proposal_status_submitted`, `ix_series_proposal_submitted_by`, `ix_series_proposal_reviewed_by` |
| `manga.SeriesRankingSnapshot` | `ix_series_ranking_snapshot_period_type_code`, `ix_series_ranking_snapshot_period_start`, `ix_series_ranking_snapshot_period_rank`, `ix_series_ranking_snapshot_series_period` |

---

## 9. Constraints and Validation Patterns

Use named constraints for all business rules. The uploaded schema already uses these patterns:

| Pattern | Example | Purpose |
|---|---|---|
| Primary key | `pk_series` | Stable row identity. |
| Unique constraint | `uq_series_slug` | Prevent duplicate business identifiers. |
| Default constraint | `df_chapter_status_code` | Provide default workflow status. |
| Check constraint | `ck_page_region_dimensions` | Enforce positive region dimensions. |
| Foreign key | `fk_page_region_version` | Enforce relationship integrity. |
| Pair constraint | `ck_file_resource_deleted_pair` | Require timestamp/user fields to appear together. |

---

## 10. Audit Strategy

The uploaded schema uses `audit.AuditEvent` with `LEDGER = ON (APPEND_ONLY = ON)`. Use it for important events such as:

- account approval/disable actions
- series/proposal submission and review decisions
- board poll creation/close/cancel
- board votes
- chapter submission and editorial review
- file deletion workflow
- publication schedule changes
- ranking snapshot generation

Recommended audit insert shape:

```sql
INSERT INTO audit.AuditEvent (
    actor_user_id,
    actor_role_name,
    action_code,
    entity_type,
    entity_id,
    detail_json
)
VALUES (
    @actor_user_id,
    @actor_role_name,
    N'CHAPTER_SUBMITTED',
    N'Chapter',
    CONVERT(NVARCHAR(100), @chapter_id),
    @detail_json
);
```

`detail_json` should be valid JSON because the schema checks it with `ISJSON(detail_json) = 1`.

---

## 12. Change Management Checklist

Before committing a schema change:

- [ ] Does the table belong in `auth`, `manga`, or `audit`?
- [ ] Does every table have a named primary key?
- [ ] Are all foreign keys named and created after parent tables?
- [ ] Are business code values enforced with `CHECK` constraints?
- [ ] Are timestamps using UTC naming if generated with `SYSUTCDATETIME()`?
- [ ] Are soft-delete columns paired with a check constraint?
- [ ] Do active/current/open rules need filtered unique indexes?
- [ ] Does the change conflict with the MVP scope?
- [ ] Are seed values idempotent or safe for reruns?
- [ ] Did you run a smoke test after applying the script?

---

## 13. Minimum Smoke Test Queries

```sql
USE MangaManagementDB;
GO

SELECT name AS schema_name
FROM sys.schemas
WHERE name IN (N'auth', N'manga', N'audit');

SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN (N'auth', N'manga', N'audit')
ORDER BY s.name, t.name;

SELECT role_name
FROM auth.Roles
ORDER BY role_id;

SELECT name AS view_name
FROM sys.views
WHERE OBJECT_SCHEMA_NAME(object_id) = N'manga';
```

---

## 14. External References

- Microsoft Learn — Database identifiers: SQL Server identifier rules.
- Microsoft Learn — CREATE TABLE: named constraints and table constraints.
- Microsoft Learn — Primary and foreign key constraints: relational integrity.
- Microsoft Learn — Unique and check constraints: domain and validation rules.
- Microsoft Learn — Filtered indexes: indexes over active/current/open subsets.
