# MangaManagementDB - Database Structure

Source: MangaManagementSystem_Schema.sql

## Database
- Name: MangaManagementDB
- Default schemas: manga, auth, audit

## Schemas
- auth: users, roles, user registration requests
- manga: file resources, series, chapters, pages, annotations, tasks, reviews, rankings, notifications, polls, votes
- audit: audit events (ledger-enabled)

## Tables

Note: column format: name (type) — constraints

### auth.Roles
- role_id (SMALLINT) — PK, IDENTITY
- role_name (NVARCHAR(30)) — NOT NULL, UNIQUE

### auth.Users
- user_id (INT) — PK, IDENTITY
- role_id (SMALLINT) — FK -> auth.Roles(role_id)
- username (NVARCHAR(50)) — NOT NULL, UNIQUE
- email (NVARCHAR(254)) — NOT NULL, UNIQUE
- password_hash (NVARCHAR(255)) — NOT NULL
- avatar_file_id (BIGINT) — FK -> manga.FileResource(file_resource_id)
- portfolio_file_id (BIGINT) — FK -> manga.FileResource(file_resource_id)
- STATUS (NVARCHAR(30)) — NOT NULL, DEFAULT 'PENDING_APPROVAL', CHECK in {PENDING_APPROVAL, ACTIVE, DISABLED}
- created_at (DATETIME2(7)) — NOT NULL, DEFAULT SYSUTCDATETIME()

Indexes:
- ix_users_status_created (STATUS, created_at DESC) INCLUDE (username, email, role_id)
- ix_users_role_status (role_id, STATUS) INCLUDE (username, email)

### manga.FileResource
- file_resource_id (BIGINT) — PK, IDENTITY
- file_purpose_code (NVARCHAR(50)) — NOT NULL, CHECK allowed purpose codes
- original_file_name (NVARCHAR(260)) — NOT NULL
- cloudinary_public_id (NVARCHAR(255)) — NOT NULL, UNIQUE
- cloudinary_secure_url (NVARCHAR(1000)) — NOT NULL
- content_type (NVARCHAR(100)) — NOT NULL
- file_size_bytes (BIGINT) — NOT NULL
- sha256_hash (CHAR(64))
- uploaded_by_user_id (INT) — FK -> auth.Users(user_id)
- uploaded_at_utc (DATETIME2(0)) — NOT NULL DEFAULT SYSUTCDATETIME()
- deleted_at_utc (DATETIME2(0)), deleted_by_user_id (INT) — pair must be both NULL or both NOT NULL; deleted_by_user_id FK -> auth.Users

Indexes:
- ix_file_resource_purpose_code
- ix_file_resource_uploaded_by
- ix_file_resource_active_by_purpose (filtered WHERE deleted_at_utc IS NULL)

### auth.UserRegistrationRequest
- registration_request_id (BIGINT) — PK, IDENTITY
- user_id (INT) — FK -> auth.Users
- requested_role_id (SMALLINT) — FK -> auth.Roles
- portfolio_file_id (BIGINT) — FK -> manga.FileResource
- STATUS (NVARCHAR(30)) — DEFAULT 'PENDING', CHECK in {PENDING, APPROVED, REJECTED, CANCELLED}
- reviewed_by_user_id (INT) — FK -> auth.Users
- created_at DATETIME2(7) — DEFAULT SYSUTCDATETIME()

Indexes:
- ix_user_registration_status (STATUS, created_at)
- ux_user_registration_request_pending UNIQUE(user_id) WHERE STATUS = 'PENDING'

### audit.AuditEvent
- audit_event_id (BIGINT) — PK, IDENTITY
- occurred_at_utc (DATETIME2(7)) — DEFAULT SYSUTCDATETIME()
- actor_user_id (INT) — FK -> auth.Users
- actor_role_name, action_code, entity_type, entity_id
- detail_json (NVARCHAR(MAX)) — must be NULL or valid JSON (ISJSON=1)

Notes: table created WITH LEDGER = ON (APPEND_ONLY = ON)
Indexes: ix_audit_event_entity_time, ix_audit_event_actor_time

### manga.Series
- series_id (BIGINT) — PK, IDENTITY
- series_code (NVARCHAR(50)) — NOT NULL, UNIQUE
- title (NVARCHAR(200)), slug (NVARCHAR(220)) — slug UNIQUE
- synopsis (NVARCHAR(MAX)), genre (NVARCHAR(100))
- cover_file_id (BIGINT) — FK -> manga.FileResource
- status_code (NVARCHAR(50)) — DEFAULT 'PROPOSAL_DRAFT', CHECK allowed status codes
- content_language_code (NVARCHAR(10)) — DEFAULT 'ja', CHECK in {ja, en, vi}
- source_series_id (BIGINT) — FK -> manga.Series(series_id), cannot reference itself
- created_at_utc, updated_at_utc, updated_by_user_id (FK -> auth.Users)
- publication_frequency_code optional, CHECK in {WEEKLY, MONTHLY, IRREGULAR}

Indexes: ix_series_current_status_code

### manga.SeriesContributor
- series_contributor_id (BIGINT) — PK
- series_id (BIGINT) — FK -> manga.Series
- user_id (INT) — FK -> auth.Users
- start_date (DATE) — DEFAULT today, end_date optional; end_date >= start_date
- notes
- UNIQUE(series_id, user_id, start_date)
- UNIQUE filtered ux_series_contributor_active_role(series_id, user_id) WHERE end_date IS NULL

Indexes: ix_series_contributor_series_active, ix_series_contributor_user_active

### manga.SeriesProposal
- series_proposal_id (BIGINT) — PK
- series_id (BIGINT) — FK -> manga.Series
- proposal_version_no (SMALLINT) — >0
- proposal_title, synopsis_snapshot, genre_snapshot
- proposal_file_id (BIGINT) — FK -> manga.FileResource
- status_code DEFAULT 'UNDER_EDITORIAL_REVIEW' with allowed codes
- submitted_by_user_id (INT) — FK -> auth.Users, submitted_at_utc DEFAULT
- withdrawn_at_utc NULL unless status = WITHDRAWN
- reviewed_by_user_id FK -> auth.Users; markup_file_id FK -> manga.FileResource
- UNIQUE(series_id, proposal_version_no)

Indexes: several including ix_series_proposal_series_version, ix_series_proposal_status_submitted, ix_series_proposal_submitted_by, ix_series_proposal_reviewed_by (filtered)

### manga.SeriesBoardPoll, manga.SeriesBoardVote
- Poll: poll records for board decisions (type: START_SERIALIZATION, CANCEL_SERIALIZATION), status OPEN/CLOSED/CANCELLED
- Unique: ux_series_board_poll_open_type(series_id, poll_type_code) WHERE poll_status_code='OPEN'
- Vote: votes linked to poll; choice_code in {APPROVE, REJECT, ABSTAIN}; REJECT requires reason; UNIQUE(series_board_poll_id, user_id)
- View: manga.vw_SeriesBoardPollVoteSummary aggregates votes and computes result

### manga.Chapter
- chapter_id (BIGINT) — PK
- series_id FK -> manga.Series
- chapter_number_label (NVARCHAR(20)) NOT NULL
- chapter_title, status_code DEFAULT 'DRAFT' CHECK allowed status codes
- planned_release_date, released_at_utc (required when status=RELEASED)
- created_at_utc DEFAULT, created_by_user_id FK -> auth.Users
- UNIQUE(series_id, chapter_number_label)

Indexes: ix_chapter_series_id, ix_chapter_status_code

### manga.ChapterPage
- chapter_page_id (BIGINT) — PK
- chapter_id FK -> manga.Chapter
- page_no (INT) >0, page_notes
- deleted_at_utc, deleted_by_user_id with pair constraint
- UNIQUE filtered ux_chapter_page_active_page_no(chapter_id, page_no) WHERE deleted_at_utc IS NULL

Indexes: ix_chapter_page_chapter_id

### manga.ChapterPageVersion
- chapter_page_version_id (BIGINT) — PK
- chapter_page_id FK -> manga.ChapterPage
- version_no (SMALLINT) — UNIQUE per page
- page_file_id (BIGINT) — FK -> manga.FileResource
- version_note, is_current_version BIT DEFAULT 0
- UNIQUE filtered ux_chapter_page_version_current WHERE is_current_version = 1

### manga.PageRegion
- page_region_id (BIGINT) — PK
- chapter_page_version_id FK -> manga.ChapterPageVersion
- type_code (NVARCHAR(80)) — CHECK {PANEL, SPEECH_BUBBLE, CHARACTER, SFX_TEXT, BACKGROUND, OTHER}
- region_label, x, y, width, height (DECIMAL) width/height >0
- confidence_score DECIMAL(5,4) when source_type='AI'
- source_type DEFAULT 'MANUAL' CHECK {AI, MANUAL}
- original_text, timestamps, created_by/updated_by FK -> auth.Users

Indexes: ix_page_region_version_type, ix_page_region_type

### manga.ChapterPageAnnotation
- chapter_page_annotation_id (BIGINT) — PK
- page_region_id FK -> manga.PageRegion
- issue_type_code CHECK allowed types (many including TRANSLATION_ERROR)
- annotated_by_user_id FK -> auth.Users, annotation_text
- resolved_at_utc/resolved_by_user_id pair constraint
- Indexes: various, including unresolved filter

### manga.ChapterPageTask, ChapterPageTaskRegion
- Task: assigned_to_user_id FK, type_code CHECK allowed, status_code DEFAULT 'ASSIGNED', priority_level 1..5, due_at_utc
- completed_page_version_id (composite FK) references ChapterPageVersion(chapter_page_version_id, chapter_page_id)
- Multiple indexes for assignee/status/due and page status
- TaskRegion: mapping table linking tasks <-> page regions (composite PK)

### manga.ChapterEditorialReview
- chapter_editorial_review_id PK
- chapter_id FK -> manga.Chapter
- reviewer_user_id FK -> auth.Users
- decision_code CHECK {APPROVED, REVISION_REQUESTED, CANCELLED}
- feedback required unless APPROVED
- markup_file_id FK -> manga.FileResource

Indexes: ix_chapter_editorial_review_chapter_id, ix_chapter_editorial_review_reviewer, ix_chapter_editorial_review_decision_code

### manga.SeriesRankingSnapshot
- series_ranking_snapshot_id PK
- series_id FK -> manga.Series
- ranking_period_type_code CHECK {WEEKLY, MONTHLY, SEASONAL}
- period_start_date, period_end_date, rank_position >=1, ranking_score
- generated_by_user_id FK -> auth.Users
- UNIQUE(series_id, ranking_period_type_code, period_start_date)
- Indexes for queries by period and rank

### manga.ChapterReaderVoteSnapshot
- chapter_reader_vote_snapshot_id PK
- chapter_id FK -> manga.Chapter
- reader_vote_count >=0, average_rating between 0..10
- entered_by_user_id FK -> auth.Users
- UNIQUE(chapter_id)

Indexes: ix_chapter_reader_vote_snapshot_voted_at, ix_chapter_reader_vote_snapshot_entered_by

### manga.Notification
- notification_id PK
- recipient_user_id FK -> auth.Users
- notification_type_code DEFAULT 'SYSTEM_MESSAGE' CHECK allowed types
- title, message, related_entity_type + related_entity_id pair constraint
- read_at_utc, created_at_utc DEFAULT

Indexes: ix_notification_recipient_read_created, ix_notification_unread_recipient (filtered), ix_notification_related_entity (filtered)

## Views
- manga.vw_SeriesBoardPollVoteSummary — aggregates SeriesBoardPoll + Votes with computed_result_code and is_applicable

## Key relationships (summary)
- auth.Users.role_id -> auth.Roles.role_id
- auth.Users.avatar_file_id/portfolio_file_id -> manga.FileResource.file_resource_id
- Many tables reference auth.Users for actor/reviewer/creator fields
- Series -> FileResource (cover), Series -> Series (source_series_id)
- Chapters -> Series; ChapterPage -> Chapter; ChapterPageVersion -> ChapterPage -> FileResource
- PageRegion -> ChapterPageVersion
- Tasks, Annotations, Reviews link to PageRegion/ChapterPageVersion and users

## Notes / Conventions
- Multiple filtered unique indexes enforce active records (e.g., active page versions, active contributors)
- Several CHECK constraints enforce status/enum-like columns; enums are implemented as NVARCHAR codes
- Audit.AuditEvent is ledger-enabled (append-only)

---
Generated from MangaManagementSystem_Schema.sql
