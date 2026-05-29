CREATE DATABASE MangaManagementDB;
GO

USE MangaManagementDB;
GO

/*
Manga Creation Workflow and Publishing Management System
SQL Server schema script
*/
SET NOCOUNT ON;
GO

IF SCHEMA_ID(N'manga') IS NULL
	EXEC (N'CREATE SCHEMA manga');
GO

IF SCHEMA_ID(N'auth') IS NULL
	EXEC (N'CREATE SCHEMA auth');
GO

IF SCHEMA_ID(N'audit') IS NULL
	EXEC (N'CREATE SCHEMA audit');
GO

CREATE TABLE auth.Roles (
	role_id SMALLINT IDENTITY PRIMARY KEY,
	role_name NVARCHAR(30) NOT NULL,
	CONSTRAINT uq_roles_role_name UNIQUE (role_name)
	);

INSERT INTO auth.Roles (role_name)
VALUES (N'Mangaka'),
	(N'Assistant'),
	(N'Tantou Editor'),
	(N'Editorial Board Member'),
	(N'Admin');

CREATE TABLE auth.Users (
	user_id INT IDENTITY(1, 1) PRIMARY KEY,
	role_id SMALLINT NOT NULL,
	username NVARCHAR(50) NOT NULL,
	email NVARCHAR(254) NOT NULL,
	password_hash NVARCHAR(255) NOT NULL,
	avatar_file_id BIGINT NULL,
	portfolio_file_id BIGINT NULL,
	STATUS NVARCHAR(30) NOT NULL CONSTRAINT df_users_status DEFAULT 'PENDING_APPROVAL',
	created_at DATETIME2(7) NOT NULL CONSTRAINT df_users_created_at DEFAULT SYSUTCDATETIME(),
	CONSTRAINT uq_users_username UNIQUE (username),
	CONSTRAINT uq_users_email UNIQUE (email),
	CONSTRAINT ck_users_status_values CHECK (
		STATUS IN (
			'PENDING_APPROVAL',
			'ACTIVE',
			'DISABLED'
			)
		),
	CONSTRAINT fk_userrole_role FOREIGN KEY (role_id) REFERENCES auth.Roles(role_id)
	);

CREATE INDEX ix_users_status_created ON auth.Users (
	STATUS,
	created_at DESC
	) INCLUDE (
	username,
	email,
	role_id
	);

CREATE INDEX ix_users_role_status ON auth.Users (
	role_id,
	STATUS
	) INCLUDE (
	username,
	email
	);

CREATE TABLE manga.FileResource (
	file_resource_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_file_resource PRIMARY KEY,
	file_purpose_code NVARCHAR(50) NOT NULL,
	original_file_name NVARCHAR(260) NOT NULL,
	cloudinary_public_id NVARCHAR(255) NOT NULL,
	cloudinary_secure_url NVARCHAR(1000) NOT NULL,
	content_type NVARCHAR(100) NOT NULL,
	file_size_bytes BIGINT NOT NULL,
	sha256_hash CHAR(64) NULL,
	uploaded_by_user_id INT NULL,
	uploaded_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_file_resource_uploaded_at_utc DEFAULT(SYSUTCDATETIME()),
	deleted_at_utc DATETIME2(0) NULL,
	deleted_by_user_id INT NULL,
	CONSTRAINT ck_file_resource_file_purpose_code CHECK (
		file_purpose_code IN (
			N'SERIES_PROPOSAL',
			N'CHAPTER_DRAFT',
			N'CHAPTER_ASSET',
			N'TASK_REFERENCE',
			N'TASK_SUBMISSION',
			N'CHAPTER_SUBMISSION',
			N'EDITORIAL_ATTACHMENT',
			N'REGISTRATION_PORTFOLIO',
			N'USER_AVATAR'
			)
		),
	CONSTRAINT ck_file_resource_deleted_pair CHECK (
		(
			deleted_at_utc IS NULL
			AND deleted_by_user_id IS NULL
			)
		OR (
			deleted_at_utc IS NOT NULL
			AND deleted_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT uq_file_resource_cloudinary_public_id UNIQUE (cloudinary_public_id),
	CONSTRAINT fk_file_resource_deleted_by_user FOREIGN KEY (deleted_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_file_resource_uploaded_by_user FOREIGN KEY (uploaded_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE INDEX ix_file_resource_purpose_code ON manga.FileResource (file_purpose_code);

CREATE INDEX ix_file_resource_uploaded_by ON manga.FileResource (uploaded_by_user_id);

CREATE INDEX ix_file_resource_active_by_purpose ON manga.FileResource (
	file_purpose_code,
	uploaded_at_utc DESC
	)
WHERE deleted_at_utc IS NULL;

ALTER TABLE auth.Users ADD CONSTRAINT fk_users_avatar_file FOREIGN KEY (avatar_file_id) REFERENCES manga.FileResource(file_resource_id);

ALTER TABLE auth.Users ADD CONSTRAINT fk_users_portfolio_file FOREIGN KEY (portfolio_file_id) REFERENCES manga.FileResource(file_resource_id);

CREATE TABLE auth.UserRegistrationRequest (
	registration_request_id BIGINT IDENTITY(1, 1) PRIMARY KEY,
	user_id INT NOT NULL,
	requested_role_id SMALLINT NULL,
	portfolio_file_id BIGINT NULL,
	STATUS NVARCHAR(30) NOT NULL CONSTRAINT df_user_registration_request_status DEFAULT 'PENDING',
	request_note NVARCHAR(500) NULL,
	reviewed_by_user_id INT NULL,
	reviewed_at DATETIME2(7) NULL,
	review_note NVARCHAR(500) NULL,
	created_at DATETIME2(7) NOT NULL CONSTRAINT df_user_registration_request_created_at DEFAULT SYSUTCDATETIME(),
	CONSTRAINT fk_user_registration_request_user FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_user_registration_request_requested_role FOREIGN KEY (requested_role_id) REFERENCES auth.Roles(role_id),
	CONSTRAINT fk_user_registration_request_portfolio_file FOREIGN KEY (portfolio_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT fk_user_registration_request_reviewed_by FOREIGN KEY (reviewed_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT ck_user_registration_request_status CHECK (
		STATUS IN (
			'PENDING',
			'APPROVED',
			'REJECTED',
			'CANCELLED'
			)
		)
	);

CREATE INDEX ix_user_registration_status ON auth.UserRegistrationRequest (
	STATUS,
	created_at
	);

CREATE UNIQUE INDEX ux_user_registration_request_pending ON auth.UserRegistrationRequest (user_id)
WHERE STATUS = 'PENDING';

CREATE TABLE audit.AuditEvent (
	audit_event_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_audit_event PRIMARY KEY,
	occurred_at_utc DATETIME2(7) NOT NULL CONSTRAINT df_audit_event_occurred_at_utc DEFAULT SYSUTCDATETIME(),
	actor_user_id INT NULL,
	actor_role_name NVARCHAR(128) NULL,
	action_code NVARCHAR(64) NOT NULL,
	entity_type NVARCHAR(128) NOT NULL,
	entity_id NVARCHAR(100) NULL,
	detail_json NVARCHAR(MAX) NULL CONSTRAINT ck_audit_event_detail_json CHECK (
		detail_json IS NULL
		OR ISJSON(detail_json) = 1
		),
	CONSTRAINT fk_audit_event_actor_user FOREIGN KEY (actor_user_id) REFERENCES auth.Users(user_id)
	)
	WITH (LEDGER = ON (APPEND_ONLY = ON));

CREATE INDEX ix_audit_event_entity_time ON audit.AuditEvent (
	entity_type,
	occurred_at_utc DESC
	);

CREATE INDEX ix_audit_event_actor_time ON audit.AuditEvent (
	actor_user_id,
	occurred_at_utc DESC
	);

CREATE TABLE manga.Series (
	series_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series PRIMARY KEY,
	series_code NVARCHAR(50) NOT NULL,
	title NVARCHAR(200) NOT NULL,
	slug NVARCHAR(220) NOT NULL,
	synopsis NVARCHAR(MAX) NOT NULL,
	genre NVARCHAR(100) NOT NULL,
	cover_file_id BIGINT NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_current_status_code DEFAULT(N'PROPOSAL_DRAFT'),
	content_language_code NVARCHAR(10) NOT NULL CONSTRAINT df_series_content_language_code DEFAULT(N'ja'),
	source_series_id BIGINT NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_created_at_utc DEFAULT(SYSUTCDATETIME()),
	updated_at_utc DATETIME2(0) NULL,
	updated_by_user_id INT NULL,
	publication_frequency_code NVARCHAR(50) NULL,
	CONSTRAINT ck_series_current_status_code CHECK (
		status_code IN (
			N'PROPOSAL_DRAFT',
			N'UNDER_EDITORIAL_REVIEW',
			N'UNDER_BOARD_REVIEW',
			N'SERIALIZED',
			N'HIATUS',
			N'CANCELLED',
			N'COMPLETED'
			)
		),
	CONSTRAINT ck_series_content_language_code CHECK (
		content_language_code IN (
			N'ja',
			N'en',
			N'vi'
			)
		),
	CONSTRAINT ck_series_source_not_self CHECK (
		source_series_id IS NULL
		OR source_series_id <> series_id
		),
	CONSTRAINT ck_series_updated_pair CHECK (
		(
			updated_at_utc IS NULL
			AND updated_by_user_id IS NULL
			)
		OR (
			updated_at_utc IS NOT NULL
			AND updated_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT ck_series_publication_frequency_code CHECK (
		publication_frequency_code IS NULL
		OR publication_frequency_code IN (
			N'WEEKLY',
			N'MONTHLY',
			N'IRREGULAR'
			)
		),
	CONSTRAINT uq_series_series_code UNIQUE (series_code),
	CONSTRAINT uq_series_slug UNIQUE (slug),
	CONSTRAINT fk_series_cover_file FOREIGN KEY (cover_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT fk_series_source_series FOREIGN KEY (source_series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE INDEX ix_series_current_status_code ON manga.Series (status_code);

CREATE TABLE manga.SeriesContributor (
	series_contributor_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series_contributor PRIMARY KEY,
	series_id BIGINT NOT NULL,
	user_id INT NOT NULL,
	start_date DATE NOT NULL CONSTRAINT df_series_contributor_start_date DEFAULT(CONVERT(DATE, SYSUTCDATETIME())),
	end_date DATE NULL,
	notes NVARCHAR(500) NULL,
	CONSTRAINT ck_series_contributor_date_range CHECK (
		end_date IS NULL
		OR end_date >= start_date
		),
	CONSTRAINT fk_series_contributor_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_contributor_user FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_series_contributor_series_user_start UNIQUE (
		series_id,
		user_id,
		start_date
		)
	);

CREATE INDEX ix_series_contributor_series_active
ON manga.SeriesContributor (
    series_id,
    end_date
)
INCLUDE (
    user_id,
    start_date
);
CREATE INDEX ix_series_contributor_user_active
ON manga.SeriesContributor (
    user_id,
    end_date
)
INCLUDE (
    series_id,
    start_date
);
CREATE UNIQUE INDEX ux_series_contributor_active_role ON manga.SeriesContributor (
	series_id,
	user_id
	)
WHERE end_date IS NULL;

CREATE TABLE manga.SeriesProposal (
	series_proposal_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series_proposal PRIMARY KEY,
	series_id BIGINT NOT NULL,
	proposal_version_no SMALLINT NOT NULL,
	proposal_title NVARCHAR(200) NOT NULL,
	synopsis_snapshot NVARCHAR(MAX) NOT NULL,
	genre_snapshot NVARCHAR(100) NOT NULL,
	proposal_file_id BIGINT NOT NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_proposal_status_code DEFAULT(N'UNDER_EDITORIAL_REVIEW'),
	submitted_by_user_id INT NOT NULL,
	submitted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_submitted_at_utc DEFAULT SYSUTCDATETIME(),
	withdrawn_at_utc DATETIME2(0) NULL,
	reviewed_by_user_id INT NULL,
	reviewed_at_utc DATETIME2(0) NULL,
	comments NVARCHAR(MAX) NULL,
	markup_file_id BIGINT NULL,
	CONSTRAINT ck_series_proposal_status_code CHECK (
		status_code IN (
			N'UNDER_EDITORIAL_REVIEW',
			N'UNDER_BOARD_REVIEW',
			N'REVISION_REQUESTED',
			N'APPROVED',
			N'CANCELLED',
			N'WITHDRAWN'
			)
		),
	CONSTRAINT ck_series_proposal_version_positive CHECK (proposal_version_no > 0),
	CONSTRAINT ck_series_proposal_withdrawn_at_matches_status CHECK (
		(
			status_code = N'WITHDRAWN'
			AND withdrawn_at_utc IS NOT NULL
			)
		OR (
			status_code <> N'WITHDRAWN'
			AND withdrawn_at_utc IS NULL
			)
		),
	CONSTRAINT ck_series_proposal_review_pair CHECK (
		(
			reviewed_by_user_id IS NULL
			AND reviewed_at_utc IS NULL
			)
		OR (
			reviewed_by_user_id IS NOT NULL
			AND reviewed_at_utc IS NOT NULL
			)
		),
	CONSTRAINT fk_series_proposal_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_proposal_file FOREIGN KEY (proposal_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT fk_series_proposal_submitted_by FOREIGN KEY (submitted_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_series_proposal_reviewed_by FOREIGN KEY (reviewed_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_series_proposal_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT uq_series_proposal_series_version UNIQUE (
		series_id,
		proposal_version_no
		)
	);

CREATE INDEX ix_series_proposal_series_version ON manga.SeriesProposal (
	series_id,
	proposal_version_no DESC
	) INCLUDE (
	status_code,
	submitted_at_utc,
	submitted_by_user_id,
	proposal_title
	);

CREATE INDEX ix_series_proposal_status_submitted ON manga.SeriesProposal (
	status_code,
	submitted_at_utc DESC
	) INCLUDE (
	series_id,
	proposal_version_no,
	submitted_by_user_id,
	proposal_title
	);

CREATE INDEX ix_series_proposal_submitted_by ON manga.SeriesProposal (
	submitted_by_user_id,
	submitted_at_utc DESC
	) INCLUDE (
	series_id,
	proposal_version_no,
	status_code,
	proposal_title
	);

CREATE INDEX ix_series_proposal_reviewed_by ON manga.SeriesProposal (
	reviewed_by_user_id,
	reviewed_at_utc DESC
	) INCLUDE (
	series_id,
	proposal_version_no,
	status_code,
	proposal_title
	)
WHERE reviewed_by_user_id IS NOT NULL;

CREATE TABLE manga.SeriesBoardPoll (
	series_board_poll_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series_board_poll PRIMARY KEY,
	series_id BIGINT NOT NULL,
	poll_type_code NVARCHAR(50) NOT NULL,
	poll_reason NVARCHAR(MAX) NOT NULL,
	poll_status_code NVARCHAR(50) NOT NULL CONSTRAINT df_series_board_poll_status_code DEFAULT(N'OPEN'),
	created_by_user_id INT NOT NULL,
	started_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_poll_started_at_utc DEFAULT(SYSUTCDATETIME()),
	ends_at_utc DATETIME2(0) NULL,
	CONSTRAINT ck_series_board_poll_type_code CHECK (
		poll_type_code IN (
			N'START_SERIALIZATION',
			N'CANCEL_SERIALIZATION'
			)
		),
	CONSTRAINT ck_series_board_poll_status_code CHECK (
		poll_status_code IN (
			N'OPEN',
			N'CLOSED',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_series_board_poll_time_range CHECK (
		ends_at_utc IS NULL
		OR ends_at_utc > started_at_utc
		),
	CONSTRAINT fk_series_board_poll_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_board_poll_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE UNIQUE INDEX ux_series_board_poll_open_type ON manga.SeriesBoardPoll (
	series_id,
	poll_type_code
	)
WHERE poll_status_code = N'OPEN';

CREATE TABLE manga.SeriesBoardVote (
	series_board_vote_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series_board_vote PRIMARY KEY,
	series_board_poll_id BIGINT NOT NULL,
	user_id INT NOT NULL,
	choice_code NVARCHAR(50) NOT NULL,
	vote_reason NVARCHAR(500) NULL,
	voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_voted_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_series_board_vote_choice_code CHECK (
		choice_code IN (
			N'APPROVE',
			N'REJECT',
			N'ABSTAIN'
			)
		),
	CONSTRAINT ck_series_board_vote_reject_reason CHECK (
		choice_code <> N'REJECT'
		OR NULLIF(LTRIM(RTRIM(vote_reason)), N'') IS NOT NULL
		),
	CONSTRAINT fk_series_board_vote_poll FOREIGN KEY (series_board_poll_id) REFERENCES manga.SeriesBoardPoll(series_board_poll_id),
	CONSTRAINT fk_series_board_vote_board_member FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_series_board_vote_poll_board_member UNIQUE (
		series_board_poll_id,
		user_id
		)
	);
GO

CREATE VIEW manga.vw_SeriesBoardPollVoteSummary
AS
SELECT p.series_board_poll_id,
	p.series_id,
	s.title AS series_title,
	p.poll_type_code,
	p.poll_status_code,
	p.poll_reason,
	p.created_by_user_id,
	p.started_at_utc,
	p.ends_at_utc,
	SUM(CASE 
			WHEN v.choice_code = N'APPROVE'
				THEN 1
			ELSE 0
			END) AS approve_count,
	SUM(CASE 
			WHEN v.choice_code = N'REJECT'
				THEN 1
			ELSE 0
			END) AS reject_count,
	SUM(CASE 
			WHEN v.choice_code = N'ABSTAIN'
				THEN 1
			ELSE 0
			END) AS abstain_count,
	COUNT(v.series_board_vote_id) AS total_vote_count,
	CASE 
		WHEN p.poll_status_code = N'CANCELLED'
			THEN N'INVALIDATED'
		WHEN p.poll_status_code <> N'CLOSED'
			THEN N'PENDING'
		WHEN SUM(CASE 
					WHEN v.choice_code = N'APPROVE'
						THEN 1
					ELSE 0
					END) > SUM(CASE 
					WHEN v.choice_code = N'REJECT'
						THEN 1
					ELSE 0
					END)
			THEN N'APPROVED'
		WHEN SUM(CASE 
					WHEN v.choice_code = N'REJECT'
						THEN 1
					ELSE 0
					END) > SUM(CASE 
					WHEN v.choice_code = N'APPROVE'
						THEN 1
					ELSE 0
					END)
			THEN N'REJECTED'
		ELSE N'NO_DECISION'
		END AS computed_result_code,
	CASE 
		WHEN p.poll_status_code = N'CLOSED'
			THEN CAST(1 AS BIT)
		ELSE CAST(0 AS BIT)
		END AS is_applicable
FROM manga.SeriesBoardPoll p
JOIN manga.Series s ON p.series_id = s.series_id
LEFT JOIN manga.SeriesBoardVote v ON p.series_board_poll_id = v.series_board_poll_id
GROUP BY p.series_board_poll_id,
	p.series_id,
	s.title,
	p.poll_type_code,
	p.poll_status_code,
	p.poll_reason,
	p.created_by_user_id,
	p.started_at_utc,
	p.ends_at_utc;
GO

CREATE TABLE manga.Chapter (
	chapter_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter PRIMARY KEY,
	series_id BIGINT NOT NULL,
	chapter_number_label NVARCHAR(20) NOT NULL,
	chapter_title NVARCHAR(200) NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_chapter_status_code DEFAULT(N'DRAFT'),
	planned_release_date DATE NULL,
	released_at_utc DATETIME2(0) NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_created_at_utc DEFAULT(SYSUTCDATETIME()),
	created_by_user_id INT NULL,
	updated_at_utc DATETIME2(0) NULL,
	CONSTRAINT ck_chapter_status_code CHECK (
		status_code IN (
			N'DRAFT',
			N'UNDER_REVIEW',
			N'REVISION_REQUESTED',
			N'APPROVED',
			N'SCHEDULED',
			N'RELEASED',
			N'ON_HOLD',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_chapter_released_at_required CHECK (
		status_code <> N'RELEASED'
		OR released_at_utc IS NOT NULL
		),
	CONSTRAINT ck_chapter_scheduled_planned_release_required CHECK (
		status_code <> N'SCHEDULED'
		OR planned_release_date IS NOT NULL
		),
	CONSTRAINT fk_chapter_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_chapter_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_chapter_series_chapter_number UNIQUE (
		series_id,
		chapter_number_label
		)
	);

CREATE INDEX ix_chapter_series_id ON manga.Chapter (series_id);

CREATE INDEX ix_chapter_status_code ON manga.Chapter (status_code);

CREATE TABLE manga.ChapterPage (
	chapter_page_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter_page PRIMARY KEY,
	chapter_id BIGINT NOT NULL,
	page_no INT NOT NULL,
	page_notes NVARCHAR(MAX) NULL,
	deleted_at_utc DATETIME2(0) NULL,
	deleted_by_user_id INT NULL,
	CONSTRAINT ck_chapter_page_page_no_positive CHECK (page_no > 0),
	CONSTRAINT ck_chapter_page_deleted_pair CHECK (
		(
			deleted_at_utc IS NULL
			AND deleted_by_user_id IS NULL
			)
		OR (
			deleted_at_utc IS NOT NULL
			AND deleted_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT fk_chapter_page_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id)
	);

CREATE INDEX ix_chapter_page_chapter_id ON manga.ChapterPage (chapter_id);

CREATE UNIQUE INDEX ux_chapter_page_active_page_no ON manga.ChapterPage (
	chapter_id,
	page_no
	)
WHERE deleted_at_utc IS NULL;

CREATE TABLE manga.ChapterPageVersion (
	chapter_page_version_id BIGINT IDENTITY(1, 1) CONSTRAINT pk_chapter_page_version PRIMARY KEY,
	chapter_page_id BIGINT NOT NULL,
	version_no SMALLINT NOT NULL,
	page_file_id BIGINT NOT NULL,
	version_note NVARCHAR(500) NULL,
	is_current_version BIT NOT NULL CONSTRAINT df_chapter_page_version_is_current DEFAULT(0),
	CONSTRAINT fk_chapter_page_version_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
	CONSTRAINT fk_chapter_page_version_file FOREIGN KEY (page_file_id) REFERENCES manga.FileResource(file_resource_id),
	CONSTRAINT uq_chapter_page_version_no UNIQUE (
		chapter_page_id,
		version_no
		),
	CONSTRAINT uq_chapter_page_version_id_page UNIQUE (
		chapter_page_version_id,
		chapter_page_id
		)
	);

CREATE UNIQUE INDEX ux_chapter_page_version_current ON manga.ChapterPageVersion (chapter_page_id)
WHERE is_current_version = 1;

CREATE TABLE manga.PageRegion (
	page_region_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_page_region PRIMARY KEY,
	chapter_page_version_id BIGINT NOT NULL,
	type_code NVARCHAR(80) NOT NULL,
	region_label NVARCHAR(100) NULL,
	x DECIMAL(10, 2) NOT NULL,
	y DECIMAL(10, 2) NOT NULL,
	width DECIMAL(10, 2) NOT NULL,
	height DECIMAL(10, 2) NOT NULL,
	confidence_score DECIMAL(5, 4) NULL,
	source_type NVARCHAR(20) NOT NULL CONSTRAINT df_page_region_source_type DEFAULT(N'MANUAL'),
	original_text NVARCHAR(MAX) NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_page_region_created_at_utc DEFAULT SYSUTCDATETIME(),
	created_by_user_id INT NULL,
	updated_at_utc DATETIME2(0) NULL,
	updated_by_user_id INT NULL,
	CONSTRAINT ck_page_region_confidence_source CHECK (
		(
			source_type = N'AI'
			AND confidence_score IS NOT NULL
			)
		OR (
			source_type = N'MANUAL'
			AND confidence_score IS NULL
			)
		),
	CONSTRAINT ck_page_region_updated_pair CHECK (
		(
			updated_at_utc IS NULL
			AND updated_by_user_id IS NULL
			)
		OR (
			updated_at_utc IS NOT NULL
			AND updated_by_user_id IS NOT NULL
			)
		),
	CONSTRAINT ck_page_region_type_code CHECK (
		type_code IN (
			N'PANEL',
			N'SPEECH_BUBBLE',
			N'CHARACTER',
			N'SFX_TEXT',
			N'BACKGROUND',
			N'OTHER'
			)
		),
	CONSTRAINT fk_page_region_version FOREIGN KEY (chapter_page_version_id) REFERENCES manga.ChapterPageVersion(chapter_page_version_id),
	CONSTRAINT fk_page_region_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_page_region_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT ck_page_region_dimensions CHECK (
		width > 0
		AND height > 0
		),
	CONSTRAINT ck_page_region_source_type CHECK (
		source_type IN (
			N'AI',
			N'MANUAL'
			)
		),
	CONSTRAINT ck_page_region_confidence CHECK (
		confidence_score IS NULL
		OR (
			confidence_score >= 0
			AND confidence_score <= 1
			)
		)
	);

CREATE INDEX ix_page_region_version_type ON manga.PageRegion (
	chapter_page_version_id,
	type_code
	) INCLUDE (
	region_label,
	x,
	y,
	width,
	height,
	source_type,
	confidence_score
	);

CREATE INDEX ix_page_region_type ON manga.PageRegion (type_code) INCLUDE (
	chapter_page_version_id,
	source_type
	);

CREATE TABLE manga.ChapterPageAnnotation (
	chapter_page_annotation_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter_page_annotation PRIMARY KEY,
	page_region_id BIGINT NOT NULL,
	issue_type_code NVARCHAR(80) NOT NULL,
	annotated_by_user_id INT NOT NULL,
	annotation_text NVARCHAR(MAX) NOT NULL,
	resolved_at_utc DATETIME2(0) NULL,
	resolved_by_user_id INT NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_annotation_created_at_utc DEFAULT SYSUTCDATETIME(),
	CONSTRAINT ck_chapter_page_annotation_issue_type_code CHECK (
		issue_type_code IS NULL
		OR issue_type_code IN (
			N'BACKGROUND_INCONSISTENCY',
			N'SHADING_ERROR',
			N'EFFECTS_ERROR',
			N'CLEANUP_REQUIRED',
			N'DIALOGUE_ERROR',
			N'TYPESETTING_ERROR',
			N'TRANSLATION_ERROR',
			N'PANEL_ORDER_ERROR',
			N'CHARACTER_ANATOMY_ERROR',
			N'CONTINUITY_ERROR',
			N'OTHER'
			)
		),
	CONSTRAINT fk_chapter_page_annotation_region FOREIGN KEY (page_region_id) REFERENCES manga.PageRegion(page_region_id),
	CONSTRAINT fk_chapter_page_annotation_annotated_by FOREIGN KEY (annotated_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_chapter_page_annotation_resolved_by FOREIGN KEY (resolved_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT ck_chapter_page_annotation_resolved_pair CHECK (
		(
			resolved_at_utc IS NULL
			AND resolved_by_user_id IS NULL
			)
		OR (
			resolved_at_utc IS NOT NULL
			AND resolved_by_user_id IS NOT NULL
			)
		)
	);

CREATE INDEX ix_chapter_page_annotation_version_created ON manga.ChapterPageAnnotation (
	chapter_page_version_id,
	created_at_utc DESC
	) INCLUDE (
	page_region_id,
	issue_type_code,
	annotated_by_user_id,
	resolved_at_utc
	);

CREATE INDEX ix_chapter_page_annotation_region ON manga.ChapterPageAnnotation (
	page_region_id,
	created_at_utc DESC
	)
WHERE page_region_id IS NOT NULL;

CREATE INDEX ix_chapter_page_annotation_unresolved ON manga.ChapterPageAnnotation (
	chapter_page_version_id,
	created_at_utc DESC
	)
WHERE resolved_at_utc IS NULL;

CREATE TABLE manga.ChapterPageTask (
	chapter_page_task_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter_page_task PRIMARY KEY,
	chapter_page_id BIGINT NOT NULL,
	assigned_to_user_id INT NOT NULL,
	type_code NVARCHAR(50) NOT NULL,
	status_code NVARCHAR(50) NOT NULL CONSTRAINT df_chapter_page_task_status_code DEFAULT(N'ASSIGNED'),
	task_title NVARCHAR(200) NOT NULL,
	task_description NVARCHAR(MAX) NOT NULL,
	priority_level TINYINT NOT NULL CONSTRAINT df_chapter_page_task_priority_level DEFAULT(3),
	due_at_utc DATETIME2(0) NOT NULL,
	compensation_amount DECIMAL(12, 2) NULL,
	completed_page_version_id BIGINT NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_task_created_at_utc DEFAULT(SYSUTCDATETIME()),
	created_by_user_id INT NOT NULL,
	updated_at_utc DATETIME2(0) NULL,
	CONSTRAINT ck_chapter_page_task_status_code CHECK (
		status_code IN (
			N'ASSIGNED',
			N'UNDER_REVIEW',
			N'COMPLETED',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_chapter_page_task_compensation_amount CHECK (
		compensation_amount IS NULL
		OR compensation_amount >= 0
		),
	CONSTRAINT ck_chapter_page_task_priority_level CHECK (
		priority_level BETWEEN 1
			AND 5
		),
	CONSTRAINT ck_chapter_page_task_output_required CHECK (
		status_code NOT IN (
			N'UNDER_REVIEW',
			N'COMPLETED'
			)
		OR completed_page_version_id IS NOT NULL
		),
	CONSTRAINT ck_chapter_page_task_type_code CHECK (
		type_code IN (
			N'BACKGROUND',
			N'SHADING',
			N'EFFECTS',
			N'CLEANUP',
			N'DIALOGUE',
			N'TYPESETTING',
			N'REVIEW',
			N'OTHER'
			)
		),
	CONSTRAINT fk_chapter_page_task_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
	CONSTRAINT fk_chapter_page_task_assigned_to FOREIGN KEY (assigned_to_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_chapter_page_task_completed_version_same_page FOREIGN KEY (
		completed_page_version_id,
		chapter_page_id
		) REFERENCES manga.ChapterPageVersion(chapter_page_version_id, chapter_page_id),
	CONSTRAINT fk_chapter_page_task_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id)
	);

CREATE INDEX ix_chapter_page_task_assignee_status_due ON manga.ChapterPageTask (
	assigned_to_user_id,
	status_code,
	due_at_utc
	) INCLUDE (
	chapter_page_id,
	type_code,
	priority_level,
	task_title
	);

CREATE INDEX ix_chapter_page_task_page_status ON manga.ChapterPageTask (
	chapter_page_id,
	status_code
	) INCLUDE (
	assigned_to_user_id,
	type_code,
	priority_level,
	due_at_utc,
	task_title
	);

CREATE INDEX ix_chapter_page_task_status_due ON manga.ChapterPageTask (
	status_code,
	due_at_utc
	) INCLUDE (
	assigned_to_user_id,
	chapter_page_id,
	priority_level,
	task_title
	);

CREATE TABLE manga.ChapterPageTaskRegion (
	chapter_page_task_id BIGINT NOT NULL,
	page_region_id BIGINT NOT NULL,
	CONSTRAINT pk_chapter_page_task_region PRIMARY KEY (
		chapter_page_task_id,
		page_region_id
		),
	CONSTRAINT fk_chapter_page_task_region_task FOREIGN KEY (chapter_page_task_id) REFERENCES manga.ChapterPageTask(chapter_page_task_id),
	CONSTRAINT fk_chapter_page_task_region_region FOREIGN KEY (page_region_id) REFERENCES manga.PageRegion(page_region_id)
	);

CREATE TABLE manga.ChapterEditorialReview (
	chapter_editorial_review_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter_editorial_review PRIMARY KEY,
	chapter_id BIGINT NOT NULL,
	reviewer_user_id INT NOT NULL,
	decision_code NVARCHAR(50) NOT NULL,
	comments NVARCHAR(MAX) NULL,
	markup_file_id BIGINT NULL,
	reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_editorial_review_reviewed_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_chapter_editorial_review_decision_code CHECK (
		decision_code IN (
			N'APPROVED',
			N'REVISION_REQUESTED',
			N'CANCELLED'
			)
		),
	CONSTRAINT ck_chapter_editorial_review_feedback_required CHECK (
		decision_code = N'APPROVED'
		OR comments IS NOT NULL
		OR markup_file_id IS NOT NULL
		),
	CONSTRAINT fk_chapter_editorial_review_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
	CONSTRAINT fk_chapter_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT fk_chapter_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id)
	);

CREATE INDEX ix_chapter_editorial_review_chapter_id ON manga.ChapterEditorialReview (chapter_id);

CREATE INDEX ix_chapter_editorial_review_reviewer ON manga.ChapterEditorialReview (reviewer_user_id);

CREATE INDEX ix_chapter_editorial_review_decision_code ON manga.ChapterEditorialReview (decision_code);

CREATE TABLE manga.SeriesRankingSnapshot (
	series_ranking_snapshot_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_series_ranking_snapshot PRIMARY KEY,
	series_id BIGINT NOT NULL,
	ranking_period_type_code NVARCHAR(50) NOT NULL,
	period_start_date DATE NOT NULL,
	period_end_date DATE NOT NULL,
	rank_position INT NOT NULL,
	ranking_score DECIMAL(10, 2) NOT NULL,
	reader_vote_count INT NOT NULL CONSTRAINT df_series_ranking_snapshot_reader_vote_count DEFAULT(0),
	cancellation_risk_score DECIMAL(10, 2) NULL,
	generated_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_generated_at_utc DEFAULT(SYSUTCDATETIME()),
	generated_by_user_id INT NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_created_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_series_ranking_snapshot_period_type_code CHECK (
		ranking_period_type_code IN (
			N'WEEKLY',
			N'MONTHLY',
			N'SEASONAL'
			)
		),
	CONSTRAINT fk_series_ranking_snapshot_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
	CONSTRAINT fk_series_ranking_snapshot_generated_by FOREIGN KEY (generated_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT ck_series_ranking_snapshot_rank_position CHECK (rank_position >= 1),
	CONSTRAINT ck_series_ranking_snapshot_period CHECK (period_end_date >= period_start_date),
	CONSTRAINT uq_series_ranking_snapshot_series_period UNIQUE (
		series_id,
		ranking_period_type_code,
		period_start_date
		)
	);

CREATE INDEX ix_series_ranking_snapshot_period_type_code ON manga.SeriesRankingSnapshot (ranking_period_type_code);

CREATE INDEX ix_series_ranking_snapshot_period_start ON manga.SeriesRankingSnapshot (period_start_date);

CREATE INDEX ix_series_ranking_snapshot_period_rank ON manga.SeriesRankingSnapshot (
	ranking_period_type_code,
	period_start_date DESC,
	rank_position
	) INCLUDE (
	series_id,
	ranking_score,
	reader_vote_count,
	cancellation_risk_score,
	period_end_date
	);

CREATE INDEX ix_series_ranking_snapshot_series_period ON manga.SeriesRankingSnapshot (
	series_id,
	ranking_period_type_code,
	period_start_date DESC
	) INCLUDE (
	rank_position,
	ranking_score,
	reader_vote_count,
	cancellation_risk_score,
	period_end_date,
	generated_at_utc
	);

CREATE TABLE manga.ChapterReaderVoteSnapshot (
	chapter_reader_vote_snapshot_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_chapter_reader_vote_snapshot PRIMARY KEY,
	chapter_id BIGINT NOT NULL,
	reader_vote_count INT NOT NULL CONSTRAINT df_chapter_reader_vote_snapshot_reader_vote_count DEFAULT(0),
	average_rating DECIMAL(4, 2) NULL,
	positive_feedback_count INT NULL,
	negative_feedback_count INT NULL,
	data_source_note NVARCHAR(500) NULL,
	entered_by_user_id INT NOT NULL,
	voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_reader_vote_snapshot_voted_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_chapter_reader_vote_snapshot_counts CHECK (
		reader_vote_count >= 0
		AND (
			positive_feedback_count IS NULL
			OR positive_feedback_count >= 0
			)
		AND (
			negative_feedback_count IS NULL
			OR negative_feedback_count >= 0
			)
		),
	CONSTRAINT ck_chapter_reader_vote_snapshot_rating CHECK (
		average_rating IS NULL
		OR average_rating BETWEEN 0
			AND 10
		),
	CONSTRAINT fk_chapter_reader_vote_snapshot_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
	CONSTRAINT fk_chapter_reader_vote_snapshot_entered_by FOREIGN KEY (entered_by_user_id) REFERENCES auth.Users(user_id),
	CONSTRAINT uq_chapter_reader_vote_snapshot_chapter UNIQUE (chapter_id)
	);

CREATE INDEX ix_chapter_reader_vote_snapshot_voted_at ON manga.ChapterReaderVoteSnapshot (voted_at_utc) INCLUDE (
	chapter_id,
	reader_vote_count,
	average_rating,
	positive_feedback_count,
	negative_feedback_count
	);

CREATE INDEX ix_chapter_reader_vote_snapshot_entered_by ON manga.ChapterReaderVoteSnapshot (
	entered_by_user_id,
	voted_at_utc DESC
	) INCLUDE (
	chapter_id,
	reader_vote_count,
	average_rating
	);

CREATE TABLE manga.Notification (
	notification_id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_notification PRIMARY KEY,
	recipient_user_id INT NOT NULL,
	notification_type_code NVARCHAR(50) NOT NULL CONSTRAINT df_notification_type_code DEFAULT(N'SYSTEM_MESSAGE'),
	title NVARCHAR(200) NOT NULL,
	message NVARCHAR(MAX) NOT NULL,
	related_entity_type NVARCHAR(50) NULL,
	related_entity_id BIGINT NULL,
	read_at_utc DATETIME2(0) NULL,
	created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_notification_created_at_utc DEFAULT(SYSUTCDATETIME()),
	CONSTRAINT ck_notification_type_code CHECK (
		notification_type_code IN (
			N'PROPOSAL_REVIEW',
			N'PROPOSAL_DECISION',
			N'TASK_ASSIGNMENT',
			N'TASK_REVIEW',
			N'CHAPTER_REVIEW',
			N'RANKING_WARNING',
			N'PUBLICATION_SCHEDULE',
			N'SYSTEM_MESSAGE'
			)
		),
	CONSTRAINT ck_notification_related_entity_pair CHECK (
		(
			related_entity_type IS NULL
			AND related_entity_id IS NULL
			)
		OR (
			related_entity_type IS NOT NULL
			AND related_entity_id IS NOT NULL
			)
		),
	CONSTRAINT fk_notification_recipient FOREIGN KEY (recipient_user_id) REFERENCES auth.Users(user_id)
	);

CREATE INDEX ix_notification_recipient_read_created ON manga.Notification (
	recipient_user_id,
	read_at_utc,
	created_at_utc DESC
	) INCLUDE (
	notification_type_code,
	title,
	related_entity_type,
	related_entity_id
	);

CREATE INDEX ix_notification_unread_recipient ON manga.Notification (
	recipient_user_id,
	created_at_utc DESC
	)
WHERE read_at_utc IS NULL;

CREATE INDEX ix_notification_related_entity ON manga.Notification (
	related_entity_type,
	related_entity_id
	)
WHERE related_entity_type IS NOT NULL
	AND related_entity_id IS NOT NULL;