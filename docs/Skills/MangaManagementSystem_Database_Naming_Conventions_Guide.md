# MangaManagementSystem Database Naming Conventions Guide

**Based on uploaded schema:** `MangaManagementSystem_Schema.sql`  
**Database:** `MangaManagementDB`  
**Target platform:** SQL Server  
**Purpose:** Keep future database scripts, EF Core entities, migrations, constraints, and indexes consistent.

---

## 1. Naming Goals

Use names that are:

- predictable
- readable in ERD diagrams
- easy to search in SQL Server Management Studio
- consistent with C#/.NET and SQL Server team work
- clear enough for presentation and grading
- aligned with the current MVP schema

---

## 2. Case Style Summary

| Object Type | Convention | Example |
|---|---|---|
| Database | PascalCase + `DB` suffix | `MangaManagementDB` |
| Schema | lowercase domain name | `auth`, `manga`, `audit` |
| Table | PascalCase entity name | `SeriesProposal`, `ChapterPageVersion` |
| View | `vw_` prefix + PascalCase purpose | `vw_SeriesBoardPollVoteSummary` |
| Column | lower `snake_case` | `series_id`, `created_at_utc` |
| Primary key column | `<entity>_id` | `series_id`, `chapter_page_id` |
| Foreign key column | referenced entity name + `_id` | `series_id`, `created_by_user_id` |
| Code/status column | `<concept>_code` or `status_code` | `poll_type_code`, `status_code` |
| Constraint | lowercase prefix + table + purpose | `ck_page_region_dimensions` |
| Index | lowercase prefix + table + key columns | `ix_chapter_status_code` |
| Code value | `SCREAMING_SNAKE_CASE` | `UNDER_BOARD_REVIEW` |

---

## 3. Database and Schema Names

### 3.1 Database

Use:

```sql
CREATE DATABASE MangaManagementDB;
```

Pattern:

```text
<ProjectName>DB
```

Avoid:

```sql
Manga_DB
manga-management-db
database1
```

### 3.2 Schemas

Use lowercase schemas by business area:

| Schema | Use for |
|---|---|
| `auth` | roles, users, account approval/security |
| `manga` | manga workflow domain records |
| `audit` | audit logs, ledger/hash-chain objects |

Do not put all objects in `dbo` for this project.

---

## 4. Table Names

### 4.1 General rule

Use **PascalCase** table names.

Good:

```sql
manga.Series
manga.SeriesProposal
manga.ChapterPageVersion
manga.PageRegion
manga.Notification
```

Avoid:

```sql
manga.series_proposal
manga.tblSeriesProposal
manga.Manga_Series_Proposal
```

### 4.2 Singular vs plural

Recommended convention for this project:

- Use **singular entity names** for domain/business tables.
- Keep `auth.Users` and `auth.Roles` as accepted auth exceptions because they already exist in the uploaded schema and are easy for the team to understand.

Preferred domain examples:

```sql
manga.Series
manga.FileResource
manga.Chapter
manga.ChapterPage
manga.PageRegion
```

Accepted auth examples:

```sql
auth.Users
auth.Roles
```

### 4.3 Junction tables

Use both related entity names in PascalCase.

Examples:

```sql
manga.ChapterPageTaskRegion
manga.SeriesContributor
```

For pure many-to-many junctions, use a composite primary key unless the relationship needs its own lifecycle fields.

---

## 5. Column Names

### 5.1 General rule

Use lowercase `snake_case`.

Good:

```sql
series_id
chapter_page_version_id
created_at_utc
uploaded_by_user_id
status_code
```

Avoid:

```sql
SeriesID
chapterPageVersionId
CreatedAt
STATUS
```

### 5.2 Primary keys

Pattern:

```text
<table_entity>_id
```

Examples:

```sql
series_id
series_proposal_id
chapter_page_id
chapter_page_version_id
page_region_id
notification_id
```

Use:

- `INT` for `auth.Users.user_id` if the team expects a moderate number of users.
- `SMALLINT` for small lookup tables such as roles.
- `BIGINT` for workflow/domain tables that may grow from files, pages, tasks, notifications, and audit records.

### 5.3 Foreign keys

Pattern:

```text
<referenced_entity_or_actor_role>_id
```

Examples:

```sql
series_id
chapter_id
page_region_id
created_by_user_id
reviewed_by_user_id
uploaded_by_user_id
deleted_by_user_id
```

Use a role/action prefix when the same table has multiple user references:

| Column | Meaning |
|---|---|
| `created_by_user_id` | User who created a record. |
| `updated_by_user_id` | User who last updated a record. |
| `reviewed_by_user_id` | User who reviewed a proposal. |
| `reviewer_user_id` | Editorial reviewer assigned/recorded on review. |
| `recipient_user_id` | Notification recipient. |
| `entered_by_user_id` | Admin/user who entered simulated reader vote data. |

### 5.4 Status and code columns

Use:

```sql
status_code
poll_type_code
choice_code
type_code
issue_type_code
notification_type_code
ranking_period_type_code
file_purpose_code
content_language_code
```

Avoid generic or uppercase names:

```sql
STATUS
type
status
```

Recommendation for current schema cleanup:

```sql
-- Current
STATUS NVARCHAR(30)

-- Recommended
status_code NVARCHAR(30)
```

### 5.5 Timestamps

Use UTC suffix when values are created with `SYSUTCDATETIME()`.

Good:

```sql
created_at_utc
updated_at_utc
submitted_at_utc
reviewed_at_utc
started_at_utc
released_at_utc
deleted_at_utc
read_at_utc
```

Avoid:

```sql
created_at
reviewed_at
date_created
```

Exception: pure calendar values should use `_date`:

```sql
start_date
end_date
planned_release_date
period_start_date
period_end_date
```

### 5.6 Boolean columns

Use `is_`, `has_`, or clear yes/no names.

Example:

```sql
is_current_version BIT NOT NULL
```

Avoid vague names:

```sql
current
flag
valid
```

### 5.7 Numeric and measurement columns

Use suffixes that explain the value:

| Suffix | Example |
|---|---|
| `_count` | `reader_vote_count` |
| `_score` | `ranking_score`, `cancellation_risk_score` |
| `_amount` | `compensation_amount` |
| `_position` | `rank_position` |
| `_no` | `page_no`, `proposal_version_no` |
| `_level` | `priority_level` |
| `_bytes` | `file_size_bytes` |

---

## 6. Constraint Naming

Use lowercase prefixes.

| Constraint Type | Prefix | Pattern | Example |
|---|---|---|---|
| Primary key | `pk_` | `pk_<table>` | `pk_series` |
| Foreign key | `fk_` | `fk_<child_table>_<parent_or_role>` | `fk_series_proposal_series` |
| Unique constraint | `uq_` | `uq_<table>_<columns>` | `uq_series_slug` |
| Check constraint | `ck_` | `ck_<table>_<rule>` | `ck_page_region_dimensions` |
| Default constraint | `df_` | `df_<table>_<column>` | `df_chapter_status_code` |

### 6.1 Primary keys

Good:

```sql
CONSTRAINT pk_series PRIMARY KEY (series_id)
CONSTRAINT pk_chapter_page_version PRIMARY KEY (chapter_page_version_id)
```

If a current table has an unnamed primary key, add a name in the next cleanup migration.

Example cleanup target:

```sql
-- Current style in some auth tables
user_id INT IDENTITY(1, 1) PRIMARY KEY

-- Recommended
user_id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_users PRIMARY KEY
```

### 6.2 Foreign keys

Pattern:

```text
fk_<child_table>_<referenced_table_or_relationship_role>
```

Good:

```sql
fk_series_proposal_series
fk_page_region_version
fk_chapter_page_task_assigned_to
fk_file_resource_uploaded_by_user
```

Review target from uploaded schema:

```sql
-- Current
fk_userrole_role

-- Clearer
fk_users_role
```

### 6.3 Unique constraints

Use `uq_` for normal uniqueness constraints inside `CREATE TABLE`.

Examples:

```sql
uq_series_series_code
uq_series_slug
uq_series_proposal_series_version
uq_series_board_vote_poll_board_member
```

Use `ux_` for unique indexes, especially filtered unique indexes.

Examples:

```sql
ux_chapter_page_active_page_no
ux_chapter_page_version_current
ux_series_board_poll_open_type
```

### 6.4 Check constraints

Name checks by business meaning, not only by column name.

Good:

```sql
ck_chapter_scheduled_planned_release_required
ck_series_board_vote_reject_reason
ck_page_region_dimensions
ck_file_resource_deleted_pair
```

Avoid:

```sql
ck1
check_status
constraint_region
```

### 6.5 Default constraints

Always name defaults.

Good:

```sql
CONSTRAINT df_chapter_status_code DEFAULT(N'DRAFT')
CONSTRAINT df_notification_created_at_utc DEFAULT(SYSUTCDATETIME())
```

Avoid unnamed defaults because SQL Server will auto-generate hard-to-maintain names.

---

## 7. Index Naming

### 7.1 Normal indexes

Pattern:

```text
ix_<table>_<leading_key_columns>
```

Examples:

```sql
ix_chapter_status_code
ix_series_proposal_status_submitted
ix_notification_recipient_read_created
```

### 7.2 Unique filtered indexes

Use `ux_` instead of `ix_`.

Pattern:

```text
ux_<table>_<business_rule>
```

Examples:

```sql
ux_chapter_page_active_page_no
ux_chapter_page_version_current
ux_series_contributor_active_role
ux_series_board_poll_open_type
```

### 7.3 Include columns

Index names should mention the search/filter keys, not every included column.

Good:

```sql
ix_chapter_page_task_assignee_status_due
```

Do not name indexes with all `INCLUDE` columns because names become too long.

---

## 8. View Naming

Use:

```text
vw_<BusinessPurpose>
```

Current schema example:

```sql
manga.vw_SeriesBoardPollVoteSummary
```

Views should be read-only query helpers unless the team explicitly designs an updatable view.

For computed board results, prefer views or computed query logic over a separate stored table when the result can be derived from votes.

---

## 9. Stored Procedure Naming

No stored procedures are present in the uploaded schema, but future procedures should use:

```text
usp_<Domain>_<VerbObject>
```

Examples:

```sql
auth.usp_User_Approve
auth.usp_User_Disable
manga.usp_Chapter_SubmitForReview
manga.usp_BoardPoll_Close
audit.usp_AuditEvent_Append
```

Do not use the `sp_` prefix for user procedures.

---

## 10. Code Value Naming

Use `SCREAMING_SNAKE_CASE` for stored code values.

Examples from uploaded schema:

```sql
UNDER_EDITORIAL_REVIEW
UNDER_BOARD_REVIEW
START_SERIALIZATION
CANCEL_SERIALIZATION
REVISION_REQUESTED
SPEECH_BUBBLE
TASK_ASSIGNMENT
```

For language codes, keep standard lowercase ISO-style values:

```sql
ja
en
vi
```

---

## 11. FileResource Purpose Codes

Current uploaded schema includes:

```text
SERIES_PROPOSAL
CHAPTER_DRAFT
CHAPTER_ASSET
TASK_REFERENCE
TASK_SUBMISSION
CHAPTER_SUBMISSION
EDITORIAL_ATTACHMENT
REGISTRATION_PORTFOLIO
USER_AVATAR
```

Recommended cleanup based on current MVP direction:

```text
SERIES_COVER
SERIES_PROPOSAL
CHAPTER_PAGE
CHAPTER_ASSET
TASK_REFERENCE
TASK_SUBMISSION
EDITORIAL_ATTACHMENT
REGISTRATION_PORTFOLIO
USER_AVATAR
ANNOTATION_EXPORT
```

Review whether `CHAPTER_SUBMISSION` is still needed, because the current MVP direction submits a chapter by changing `Chapter.status_code` rather than using a separate `ChapterSubmission` table.

---

## 12. Naming Review Checklist

Before adding a new database object:

- [ ] Table name uses PascalCase.
- [ ] Column names use lower snake_case.
- [ ] No column uses uppercase names such as `STATUS`.
- [ ] Status/code columns end with `_code`.
- [ ] Date-time columns generated by `SYSUTCDATETIME()` end with `_at_utc`.
- [ ] Primary key is named `pk_<table>`.
- [ ] Foreign keys are named `fk_<child>_<parent_or_role>`.
- [ ] Unique constraints use `uq_`.
- [ ] Unique filtered indexes use `ux_`.
- [ ] Normal indexes use `ix_`.
- [ ] Check constraints use `ck_` and describe the business rule.
- [ ] Default constraints use `df_`.
- [ ] Stored procedures do not use `sp_`.
- [ ] Code values use `SCREAMING_SNAKE_CASE`.
- [ ] Object name avoids spaces, hyphens, reserved words, and special characters.

---

## 13. Current Schema Naming Cleanup Targets

| Current object/name | Issue | Suggested target |
|---|---|---|
| `auth.Users.STATUS` | Uppercase column name; inconsistent with snake_case. | `status_code` or `status` |
| `auth.UserRegistrationRequest.STATUS` | Uppercase column name; inconsistent with snake_case. | `status_code` or `request_status_code` |
| `auth.Users.created_at` | UTC default but name does not show UTC. | `created_at_utc` |
| `auth.UserRegistrationRequest.created_at` | UTC default but name does not show UTC. | `created_at_utc` |
| `auth.UserRegistrationRequest.reviewed_at` | Timestamp naming inconsistent. | `reviewed_at_utc` |
| `fk_userrole_role` | FK name does not clearly identify child table. | `fk_users_role` |
| `df_series_current_status_code` | Default name includes `current`, while column is `status_code`. | `df_series_status_code` |
| `ix_series_current_status_code` | Index name includes `current`, while column is `status_code`. | `ix_series_status_code` |
| `CHAPTER_SUBMISSION` file purpose | May imply removed `ChapterSubmission` table. | Use only if team keeps this semantic; otherwise replace with `CHAPTER_PAGE` or `CHAPTER_DRAFT`. |
| missing `SERIES_COVER` file purpose | Series cover uses `FileResource`, but purpose code is absent. | Add `SERIES_COVER`. |

---

## 14. External References

- Microsoft Learn — Database identifiers: identifier rules and valid naming behavior.
- Microsoft Learn — T-SQL naming code analysis: avoid special characters, reserved words, and `sp_` prefix.
- Microsoft Learn — CREATE TABLE: named constraints and table constraints.
- Microsoft Learn — CREATE INDEX / filtered indexes: index filter predicates and filtered index design.
