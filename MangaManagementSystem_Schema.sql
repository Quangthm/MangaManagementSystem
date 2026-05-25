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
    EXEC(N'CREATE SCHEMA manga');
GO
IF SCHEMA_ID(N'auth') IS NULL
    EXEC(N'CREATE SCHEMA auth');
GO
IF SCHEMA_ID(N'audit') IS NULL
    EXEC(N'CREATE SCHEMA audit');
GO
CREATE TABLE manga.AiJobType (
    code NVARCHAR(80) PRIMARY KEY,

    name NVARCHAR(120) NOT NULL,

    description NVARCHAR(500) NULL
);

INSERT INTO manga.AiJobType (
    code,
    name,
    description
)
VALUES
(N'SEGMENTATION', N'AI-Assisted Segmentation', N'Detect panels, speech bubbles, characters, and other page regions.'),
(N'TRANSLATION',  N'AI-Assisted Translation',  N'Suggest translated text for detected text regions.');

CREATE TABLE manga.RegionType (
    code NVARCHAR(80) PRIMARY KEY,

    name NVARCHAR(120) NOT NULL,

    description NVARCHAR(500) NULL

);

INSERT INTO manga.RegionType (
    code,
    name,
    description
)
VALUES
(N'PANEL',         N'Panel',             N'Manga panel or frame region.'),
(N'SPEECH_BUBBLE', N'Speech Bubble',     N'Bubble containing dialogue text.'),
(N'CHARACTER',     N'Character',         N'Character region detected or marked on page.'),
(N'SFX_TEXT',      N'Sound Effect Text', N'Sound effect or stylized text region.'),
(N'BACKGROUND',    N'Background',        N'Background area or scene region.'),
(N'OTHER',         N'Other',             N'Other manually marked region.');

CREATE TABLE manga.TaskType (
    code NVARCHAR(50) PRIMARY KEY,

    name NVARCHAR(100) NOT NULL
);

INSERT INTO manga.TaskType (
    code,
    name
)
VALUES
(N'BACKGROUND', N'Background'),
(N'SHADING', N'Shading'),
(N'EFFECTS', N'Effects'),
(N'CLEANUP', N'Cleanup'),
(N'DIALOGUE', N'Dialogue'),
(N'TYPESETTING', N'Typesetting'),
(N'REVIEW', N'Review'),
(N'OTHER', N'Other');

CREATE TABLE manga.AnnotationIssueType (
    code NVARCHAR(80) PRIMARY KEY,

    name NVARCHAR(120) NOT NULL
);

INSERT INTO manga.AnnotationIssueType (
    code,
    name
)
VALUES
(N'BACKGROUND_INCONSISTENCY', N'Background Inconsistency'),
(N'SHADING_ERROR', N'Shading Error'),
(N'EFFECTS_ERROR', N'Effects Error'),
(N'CLEANUP_REQUIRED', N'Cleanup Required'),
(N'DIALOGUE_ERROR', N'Dialogue Error'),
(N'TYPESETTING_ERROR', N'Typesetting Error'),
(N'TRANSLATION_ERROR', N'Translation Error'),
(N'PANEL_ORDER_ERROR', N'Panel Order Error'),
(N'CHARACTER_ANATOMY_ERROR', N'Character Anatomy Error'),
(N'CONTINUITY_ERROR', N'Continuity Error'),
(N'OTHER', N'Other');

CREATE TABLE auth.Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,

    username NVARCHAR(50) NOT NULL,
    email NVARCHAR(254) NOT NULL,

    password_hash NVARCHAR(255) NOT NULL,
    avatar_file_id BIGINT NULL,
    status NVARCHAR(30) NOT NULL
        CONSTRAINT df_users_status DEFAULT 'PENDING_APPROVAL',

    failed_login_attempts INT NOT NULL
        CONSTRAINT df_users_failed_login_attempts DEFAULT (0),

    last_failed_login_at DATETIME2(7) NULL,

    locked_until DATETIME2(7) NULL,

    last_login_at DATETIME2(7) NULL,

    created_at DATETIME2(7) NOT NULL
        CONSTRAINT df_users_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT uq_users_username
        UNIQUE (username),

    CONSTRAINT uq_users_email
        UNIQUE (email),

    CONSTRAINT ck_users_status_values
        CHECK (
            status IN (
                'PENDING_APPROVAL',
                'ACTIVE',
                'DISABLED',
                'LOCKED'
            )
        ),

    CONSTRAINT ck_users_failed_login_attempts
        CHECK (failed_login_attempts >= 0)
);

CREATE TABLE manga.FileResource (
    file_resource_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_file_resource PRIMARY KEY,
    file_purpose_code NVARCHAR(50) NOT NULL,
    original_file_name NVARCHAR(260) NOT NULL,
    cloudinary_public_id NVARCHAR(255) NOT NULL,
    cloudinary_secure_url NVARCHAR(1000) NOT NULL,
    content_type NVARCHAR(100) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    sha256_hash CHAR(64) NULL,
    uploaded_by_user_id INT NULL,
    uploaded_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_file_resource_uploaded_at_utc DEFAULT (SYSUTCDATETIME()),
    deleted_at_utc DATETIME2(0) NULL,
    deleted_by_user_id INT NULL,
    CONSTRAINT ck_file_resource_file_purpose_code
        CHECK (file_purpose_code IN (
         N'SERIES_PROPOSAL',
         N'CHAPTER_DRAFT',
         N'CHAPTER_ASSET',
         N'TASK_REFERENCE',
         N'TASK_SUBMISSION',
         N'CHAPTER_SUBMISSION',
         N'EDITORIAL_ATTACHMENT',
         N'PAGE_ANNOTATION_EXPORT',
         N'REGISTRATION_PORTFOLIO',
         N'USER_AVATAR'
        )),
    CONSTRAINT ck_file_resource_deleted_pair
        CHECK (
            (deleted_at_utc IS NULL AND deleted_by_user_id IS NULL)
            OR
            (deleted_at_utc IS NOT NULL AND deleted_by_user_id IS NOT NULL)
        ),
    CONSTRAINT uq_file_resource_cloudinary_public_id
        UNIQUE (cloudinary_public_id),
    CONSTRAINT fk_file_resource_deleted_by_user FOREIGN KEY (deleted_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_file_resource_uploaded_by_user FOREIGN KEY (uploaded_by_user_id) REFERENCES auth.Users(user_id)
);
CREATE INDEX ix_file_resource_purpose_code
ON manga.FileResource(file_purpose_code);

CREATE INDEX ix_file_resource_uploaded_by
ON manga.FileResource(uploaded_by_user_id);

CREATE INDEX ix_file_resource_active_by_purpose
ON manga.FileResource (file_purpose_code, uploaded_at_utc DESC)
WHERE deleted_at_utc IS NULL;

ALTER TABLE auth.Users
ADD CONSTRAINT fk_users_avatar_file
FOREIGN KEY (avatar_file_id)
REFERENCES manga.FileResource(file_resource_id);

CREATE TABLE auth.Roles (
    role_id SMALLINT IDENTITY PRIMARY KEY,
    role_name NVARCHAR(30)  NOT NULL,
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);
INSERT INTO auth.Roles(role_name)
VALUES
(N'Mangaka'),
(N'Assistant'),
(N'Tantou Editor'),
(N'Editorial Board Member'),
(N'Admin');

CREATE TABLE auth.UserRole (
    user_id INT NOT NULL,
    role_id SMALLINT NOT NULL,

    assigned_at DATETIME2(7) NOT NULL CONSTRAINT df_user_role_assigned_at DEFAULT SYSUTCDATETIME(),
    assigned_by_user_id INT NOT NULL,
    revoked_at DATETIME2(7) NULL,

    CONSTRAINT pk_user_role
        PRIMARY KEY (user_id, role_id, assigned_at),

    CONSTRAINT fk_userrole_user
        FOREIGN KEY (user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_userrole_role
        FOREIGN KEY (role_id)
        REFERENCES auth.Roles(role_id),

    CONSTRAINT fk_userrole_assigned_by
        FOREIGN KEY (assigned_by_user_id)
        REFERENCES auth.Users(user_id)
);

CREATE TABLE auth.UserRegistrationRequest (
    registration_request_id BIGINT IDENTITY(1,1) PRIMARY KEY,

    user_id INT NOT NULL,

    requested_role_id SMALLINT NULL,
    portfolio_file_id BIGINT NULL,
    status NVARCHAR(30) NOT NULL
        CONSTRAINT df_user_registration_request_status DEFAULT 'PENDING',

    request_note NVARCHAR(500) NULL,

    reviewed_by_user_id INT NULL,
    reviewed_at DATETIME2(7) NULL,
    review_note NVARCHAR(500) NULL,

    created_at DATETIME2(7) NOT NULL
        CONSTRAINT df_user_registration_request_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT fk_user_registration_request_user
        FOREIGN KEY (user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_user_registration_request_requested_role
        FOREIGN KEY (requested_role_id)
        REFERENCES auth.Roles(role_id),
    CONSTRAINT fk_user_registration_request_portfolio_file
        FOREIGN KEY (portfolio_file_id)
        REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_user_registration_request_reviewed_by
        FOREIGN KEY (reviewed_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT ck_user_registration_request_status
        CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED', 'CANCELLED'))
);


CREATE INDEX ix_user_registration_status
ON auth.UserRegistrationRequest (status, created_at);


CREATE UNIQUE INDEX ux_user_registration_request_pending
ON auth.UserRegistrationRequest (user_id)
WHERE status = 'PENDING';


CREATE UNIQUE INDEX ux_userrole_active
ON auth.UserRole (user_id, role_id)
WHERE revoked_at IS NULL;

CREATE TABLE audit.AuditEvent
(
    audit_event_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_audit_event PRIMARY KEY,

    occurred_at_utc DATETIME2(7) NOT NULL
        CONSTRAINT df_audit_event_occurred_at_utc DEFAULT SYSUTCDATETIME(),

    actor_user_id INT NULL,
    actor_role_name NVARCHAR(128) NULL,

    action_code NVARCHAR(64) NOT NULL,

    entity_type NVARCHAR(128) NOT NULL,
    entity_id NVARCHAR(100) NULL,

    detail_json NVARCHAR(MAX) NULL
        CONSTRAINT ck_audit_event_detail_json
        CHECK (detail_json IS NULL OR ISJSON(detail_json) = 1),

    CONSTRAINT fk_audit_event_actor_user
        FOREIGN KEY (actor_user_id)
        REFERENCES auth.Users(user_id)
)
WITH (LEDGER = ON (APPEND_ONLY = ON));

CREATE INDEX ix_audit_event_entity_time
ON audit.AuditEvent (entity_type, occurred_at_utc DESC);

CREATE INDEX ix_audit_event_actor_time
ON audit.AuditEvent (actor_user_id, occurred_at_utc DESC);

CREATE TABLE audit.AuditHashChain
(
    audit_hash_chain_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_audit_hash_chain PRIMARY KEY,

    audit_event_id BIGINT NOT NULL,

    prev_chain_hash CHAR(64) NULL,
    row_hash CHAR(64) NOT NULL,
    chain_hash CHAR(64) NOT NULL,

    created_at_utc DATETIME2(7) NOT NULL
        CONSTRAINT df_audit_hash_chain_created_at_utc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT uq_audit_hash_chain_event
        UNIQUE (audit_event_id),

    CONSTRAINT fk_audit_hash_chain_event
        FOREIGN KEY (audit_event_id)
        REFERENCES audit.AuditEvent(audit_event_id)
);

CREATE TABLE manga.Series (
    series_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series PRIMARY KEY,
    series_code NVARCHAR(50) NOT NULL,
    title NVARCHAR(200) NOT NULL,
    slug NVARCHAR(220) NOT NULL,
    synopsis NVARCHAR(MAX) NOT NULL,
    character_overview NVARCHAR(MAX) NULL,
    genre NVARCHAR(100) NOT NULL,
    target_audience NVARCHAR(100) NULL,
    cover_file_id BIGINT NULL,
    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_series_current_status_code DEFAULT (N'PROPOSAL_DRAFT'),
    content_language_code NVARCHAR(10) NOT NULL
        CONSTRAINT df_series_content_language_code DEFAULT (N'ja'),
    source_series_id BIGINT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_created_at_utc DEFAULT (SYSUTCDATETIME()),
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT ck_series_current_status_code
    CHECK (status_code IN (
        N'PROPOSAL_DRAFT',
        N'UNDER_EDITORIAL_REVIEW',
        N'BOARD_REVIEW',
        N'SERIALIZED',
        N'HIATUS',
        N'CANCELLED',
        N'COMPLETED'
    )),
    CONSTRAINT ck_series_content_language_code
        CHECK (content_language_code IN (
            N'ja',
            N'en',
            N'vi'
    )),
    CONSTRAINT ck_series_source_not_self
        CHECK (
            source_series_id IS NULL
            OR source_series_id <> series_id
    ),
    CONSTRAINT ck_series_updated_pair
        CHECK (
            (updated_at_utc IS NULL AND updated_by_user_id IS NULL)
            OR
            (updated_at_utc IS NOT NULL AND updated_by_user_id IS NOT NULL)
        ),
    CONSTRAINT uq_series_series_code UNIQUE (series_code),
    CONSTRAINT uq_series_slug UNIQUE (slug),
    CONSTRAINT fk_series_cover_file FOREIGN KEY (cover_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_series_source_series FOREIGN KEY (source_series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_series_current_status_code
ON manga.Series(status_code);
CREATE INDEX ix_series_lead_mangaka_user_id ON manga.Series (lead_mangaka_user_id);

CREATE TABLE manga.SeriesContributor (
    series_contributor_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_contributor PRIMARY KEY,
    series_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    contributor_role_id SMALLINT NOT NULL,
    start_date DATE NOT NULL CONSTRAINT df_series_contributor_start_date DEFAULT (CONVERT(date, SYSUTCDATETIME())),
    end_date DATE NULL,
    notes NVARCHAR(500) NULL,
    CONSTRAINT ck_series_contributor_date_range
    CHECK (
        end_date IS NULL
        OR end_date >= start_date
        ),
    CONSTRAINT fk_series_contributor_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_contributor_user FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_contributor_role FOREIGN KEY (contributor_role_id) REFERENCES auth.Roles(role_id),
    CONSTRAINT uq_series_contributor_series_user_role UNIQUE (series_id, user_id, contributor_role_id, start_date)
);

CREATE INDEX ix_series_contributor_user_id ON manga.SeriesContributor (user_id);
CREATE UNIQUE INDEX ux_series_contributor_active_role
ON manga.SeriesContributor (series_id, user_id, contributor_role_id)
WHERE end_date IS NULL;
CREATE TABLE manga.SeriesProposal (
    series_proposal_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_series_proposal PRIMARY KEY,

    series_id BIGINT NOT NULL,

    proposal_version_no SMALLINT NOT NULL,

    proposal_title NVARCHAR(200) NOT NULL,

    synopsis_snapshot NVARCHAR(MAX) NOT NULL,
    character_overview_snapshot NVARCHAR(MAX) NULL,

    proposal_file_id BIGINT NOT NULL,

    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_series_proposal_status_code DEFAULT (N'UNDER_EDITORIAL_REVIEW'),

    submitted_by_user_id INT NOT NULL,

    submitted_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_series_proposal_submitted_at_utc
        DEFAULT SYSUTCDATETIME(),

    withdrawn_at_utc DATETIME2(0) NULL,
    CONSTRAINT ck_series_proposal_status_code
    CHECK (status_code IN (
        N'UNDER_EDITORIAL_REVIEW',
        N'UNDER_BOARD_REVIEW',
        N'REVISION_REQUIRED',
        N'APPROVED',
        N'CANCELLED',
        N'WITHDRAWN'
    )),
    CONSTRAINT ck_series_proposal_version_positive
        CHECK (proposal_version_no > 0),

    CONSTRAINT ck_series_proposal_withdrawn_at_matches_status
        CHECK (
            (status_code = N'WITHDRAWN' AND withdrawn_at_utc IS NOT NULL)
            OR
            (status_code <> N'WITHDRAWN' AND withdrawn_at_utc IS NULL)
        ),

    CONSTRAINT fk_series_proposal_series
        FOREIGN KEY (series_id)
        REFERENCES manga.Series(series_id),

    CONSTRAINT fk_series_proposal_file
        FOREIGN KEY (proposal_file_id)
        REFERENCES manga.FileResource(file_resource_id),


    CONSTRAINT fk_series_proposal_submitted_by
        FOREIGN KEY (submitted_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT uq_series_proposal_series_version
        UNIQUE (series_id, proposal_version_no)
);

CREATE INDEX ix_series_proposal_series_id
ON manga.SeriesProposal (series_id);

CREATE INDEX ix_series_proposal_status_code
ON manga.SeriesProposal(status_code);

CREATE INDEX ix_series_proposal_submitted_by
ON manga.SeriesProposal(submitted_by_user_id);
CREATE INDEX ix_series_proposal_series_version
ON manga.SeriesProposal (
    series_id,
    proposal_version_no DESC
)
INCLUDE (
    status_code,
    submitted_at_utc,
    submitted_by_user_id,
    proposal_title
);
CREATE TABLE manga.SeriesEditorialReview (
    series_editorial_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_editorial_review PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    decision_code NVARCHAR(50) NOT NULL,
    comments NVARCHAR(MAX) NULL,
    markup_file_id BIGINT NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_editorial_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_series_editorial_review_decision_code
        CHECK (decision_code IN (
            N'APPROVED',
            N'REVISION_REQUESTED',
            N'CANCELLED'
    )),
    CONSTRAINT ck_series_editorial_review_feedback_required
        CHECK (
            decision_code = N'APPROVED'
            OR comments IS NOT NULL
            OR markup_file_id IS NOT NULL
        ),
    CONSTRAINT fk_series_editorial_review_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT uq_series_editorial_review_proposal
    UNIQUE (series_proposal_id)
);

CREATE INDEX ix_series_editorial_review_proposal_id ON manga.SeriesEditorialReview (series_proposal_id);

CREATE INDEX ix_series_editorial_review_decision_code
ON manga.SeriesEditorialReview(decision_code);

CREATE INDEX ix_series_editorial_review_reviewer
ON manga.SeriesEditorialReview(reviewer_user_id);

CREATE TABLE manga.SeriesBoardVote (
    series_board_vote_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_board_vote PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    board_member_user_id INT NOT NULL,
    choice_code NVARCHAR(50) NOT NULL,
    vote_reason NVARCHAR(500) NULL,
    voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_voted_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_series_board_vote_choice_code
    CHECK (choice_code IN (
        N'APPROVE',
        N'REJECT',
        N'ABSTAIN'
    )),
    CONSTRAINT ck_series_board_vote_reject_reason
    CHECK (
        choice_code <> N'REJECT'
        OR vote_reason IS NOT NULL
    ),
    CONSTRAINT fk_series_board_vote_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_board_vote_board_member FOREIGN KEY (board_member_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_board_vote_proposal_board_member UNIQUE (series_proposal_id, board_member_user_id)
);

CREATE INDEX ix_series_board_vote_proposal_id ON manga.SeriesBoardVote (series_proposal_id);
CREATE INDEX ix_series_board_vote_member ON manga.SeriesBoardVote(board_member_user_id);
CREATE INDEX ix_series_board_vote_choice_code ON manga.SeriesBoardVote(choice_code);


CREATE TABLE manga.SeriesBoardDecision (
    series_board_decision_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_board_decision PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    decision_code NVARCHAR(50) NOT NULL,
    aggregate_approve_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_approve_count DEFAULT (0),
    aggregate_reject_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_reject_count DEFAULT (0),
    aggregate_abstain_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_abstain_count DEFAULT (0),
    decided_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_decision_decided_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_series_board_decision_code
        CHECK (decision_code IN (
            N'APPROVED',
            N'REVISION_REQUESTED',
            N'REJECTED'
    )),
    CONSTRAINT ck_series_board_decision_counts_nonnegative
        CHECK (
            aggregate_approve_count >= 0
            AND aggregate_reject_count >= 0
            AND aggregate_abstain_count >= 0
        ),
    CONSTRAINT fk_series_board_decision_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT uq_series_board_decision_proposal UNIQUE (series_proposal_id)
);

CREATE INDEX ix_series_board_decision_decision_type_id ON manga.SeriesBoardDecision (decision_code);
CREATE INDEX ix_series_board_decision_proposal ON manga.SeriesBoardDecision(series_proposal_id);

CREATE TABLE manga.Chapter (
    chapter_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter PRIMARY KEY,
    series_id BIGINT NOT NULL,
    chapter_number_label NVARCHAR(20) NOT NULL,
    chapter_title NVARCHAR(200) NULL,
    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_chapter_status_code DEFAULT (N'DRAFT'),
    planned_release_date DATE NULL,
    released_at_utc DATETIME2(0) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    CONSTRAINT ck_chapter_status_code
        CHECK (status_code IN (
        N'DRAFT',
        N'UNDER_REVIEW',
        N'REVISION_REQUESTED',
        N'APPROVED',
        N'SCHEDULED',
        N'RELEASED',
        N'ON_HOLD',
        N'CANCELLED'
        )),
    CONSTRAINT ck_chapter_released_at_required
        CHECK (
        status_code <> N'RELEASED'
        OR released_at_utc IS NOT NULL
    ),
    CONSTRAINT ck_chapter_scheduled_planned_release_required
    CHECK (
    status_code <> N'SCHEDULED'
    OR planned_release_date IS NOT NULL
    ),   
    CONSTRAINT fk_chapter_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_chapter_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_series_chapter_number UNIQUE (series_id, chapter_number_label)
);

CREATE INDEX ix_chapter_series_id ON manga.Chapter (series_id);
CREATE INDEX ix_chapter_status_code ON manga.Chapter(status_code);


CREATE TABLE manga.ChapterPage (
    chapter_page_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_page PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    page_no INT NOT NULL,
    page_notes NVARCHAR(MAX) NULL,
    deleted_at_utc DATETIME2(0) NULL,
    deleted_by_user_id INT NULL,
    CONSTRAINT ck_chapter_page_page_no_positive
        CHECK (page_no > 0),
    CONSTRAINT ck_chapter_page_deleted_pair
        CHECK (
            (deleted_at_utc IS NULL AND deleted_by_user_id IS NULL)
            OR
            (deleted_at_utc IS NOT NULL AND deleted_by_user_id IS NOT NULL)
        ),
    CONSTRAINT fk_chapter_page_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id)
);
CREATE INDEX ix_chapter_page_chapter_id ON manga.ChapterPage (chapter_id);
CREATE UNIQUE INDEX ux_chapter_page_active_page_no
ON manga.ChapterPage (chapter_id, page_no)
WHERE deleted_at_utc IS NULL;
CREATE TABLE manga.ChapterPageVersion (
    chapter_page_version_id BIGINT IDENTITY(1,1)
        CONSTRAINT pk_chapter_page_version PRIMARY KEY,
    chapter_page_id BIGINT NOT NULL,
    version_no SMALLINT NOT NULL,
    page_file_id BIGINT NOT NULL,
    version_note NVARCHAR(500) NULL,
    is_current_version BIT NOT NULL
        CONSTRAINT df_chapter_page_version_is_current DEFAULT (0),
    CONSTRAINT fk_chapter_page_version_page
        FOREIGN KEY (chapter_page_id)
        REFERENCES manga.ChapterPage(chapter_page_id),
    CONSTRAINT fk_chapter_page_version_file
        FOREIGN KEY (page_file_id)
        REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT uq_chapter_page_version_no
        UNIQUE (chapter_page_id, version_no),
    CONSTRAINT uq_chapter_page_version_id_page
        UNIQUE (chapter_page_version_id, chapter_page_id)
);
CREATE UNIQUE INDEX ux_chapter_page_version_current
ON manga.ChapterPageVersion (chapter_page_id)
WHERE is_current_version = 1;

CREATE TABLE manga.AiProcessingJob (
    ai_processing_job_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_ai_processing_job PRIMARY KEY,

    type_code NVARCHAR(80) NOT NULL,
    job_status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_ai_processing_job_status_code DEFAULT (N'QUEUED'),
    target_page_id BIGINT NULL,

    requested_by_user_id INT NULL,

    model_name NVARCHAR(150) NULL,
    model_version NVARCHAR(80) NULL,

    input_file_id BIGINT NULL,
    output_file_id BIGINT NULL,

    result_json NVARCHAR(MAX) NULL
        CONSTRAINT ck_ai_processing_job_result_json
        CHECK (result_json IS NULL OR ISJSON(result_json) = 1),

    error_message NVARCHAR(MAX) NULL,

    requested_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_ai_processing_job_requested_at_utc
        DEFAULT SYSUTCDATETIME(),

    started_at_utc DATETIME2(0) NULL,
    completed_at_utc DATETIME2(0) NULL,

    CONSTRAINT ck_ai_processing_job_status_code
    CHECK (job_status_code IN (
        N'QUEUED',
        N'RUNNING',
        N'COMPLETED',
        N'FAILED',
        N'CANCELLED'
    )),
    CONSTRAINT fk_ai_processing_job_type
        FOREIGN KEY (type_code)
        REFERENCES manga.AiJobType(code),

    CONSTRAINT fk_ai_processing_job_page
        FOREIGN KEY (target_page_id)
        REFERENCES manga.ChapterPage(chapter_page_id),

    CONSTRAINT fk_ai_processing_job_requested_by
        FOREIGN KEY (requested_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_ai_processing_job_input_file
        FOREIGN KEY (input_file_id)
        REFERENCES manga.FileResource(file_resource_id),

    CONSTRAINT fk_ai_processing_job_output_file
        FOREIGN KEY (output_file_id)
        REFERENCES manga.FileResource(file_resource_id)
);

CREATE INDEX ix_ai_processing_job_page_id
ON manga.AiProcessingJob (target_page_id);

CREATE INDEX ix_ai_processing_job_type_code
ON manga.AiProcessingJob(type_code);


CREATE TABLE manga.PageRegion (
    page_region_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_page_region PRIMARY KEY,

    chapter_page_version_id BIGINT NOT NULL,

    type_code NVARCHAR(80) NOT NULL,
    ai_processing_job_id BIGINT NULL,

    region_label NVARCHAR(100) NULL,

    x DECIMAL(10,2) NOT NULL,
    y DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,

    confidence_score DECIMAL(5,4) NULL,

    source_type NVARCHAR(20) NOT NULL
        CONSTRAINT df_page_region_source_type
        DEFAULT (N'MANUAL'),

    original_text NVARCHAR(MAX) NULL,

    is_manually_adjusted BIT NOT NULL
        CONSTRAINT df_page_region_is_manually_adjusted
        DEFAULT (0),

    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_page_region_created_at_utc
        DEFAULT SYSUTCDATETIME(),

    created_by_user_id INT NULL,

    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_page_region_type FOREIGN KEY (type_code) REFERENCES manga.RegionType(code),
    CONSTRAINT fk_page_region_version
        FOREIGN KEY (chapter_page_version_id)
        REFERENCES manga.ChapterPageVersion(chapter_page_version_id),
    CONSTRAINT fk_page_region_ai_job
        FOREIGN KEY (ai_processing_job_id)
        REFERENCES manga.AiProcessingJob(ai_processing_job_id),

    CONSTRAINT fk_page_region_created_by
        FOREIGN KEY (created_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_page_region_updated_by
        FOREIGN KEY (updated_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT ck_page_region_dimensions
        CHECK (width > 0 AND height > 0),

    CONSTRAINT ck_page_region_source_type
        CHECK (source_type IN (N'AI', N'MANUAL')),

    CONSTRAINT ck_page_region_confidence
        CHECK (
            confidence_score IS NULL
            OR (confidence_score >= 0 AND confidence_score <= 1)
        )
);

CREATE INDEX ix_page_region_type_code
ON manga.PageRegion(type_code);
CREATE INDEX ix_page_region_ai_job
ON manga.PageRegion(ai_processing_job_id)
WHERE ai_processing_job_id IS NOT NULL;


CREATE TABLE manga.ChapterPageAnnotation (
    chapter_page_annotation_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_chapter_page_annotation PRIMARY KEY,

    chapter_page_version_id BIGINT NOT NULL,
    page_region_id BIGINT NULL,

    issue_type_code NVARCHAR(80) NULL,
    annotated_by_user_id INT NOT NULL,

    annotation_text NVARCHAR(MAX) NOT NULL,

    x DECIMAL(10,2) NULL,
    y DECIMAL(10,2) NULL,
    width DECIMAL(10,2) NULL,
    height DECIMAL(10,2) NULL,

    resolved_at_utc DATETIME2(0) NULL,
    resolved_by_user_id INT NULL,

    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_chapter_page_annotation_created_at_utc
        DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_chapter_page_annotation_issue_type
        FOREIGN KEY (issue_type_code)
        REFERENCES manga.AnnotationIssueType(code),
     CONSTRAINT fk_chapter_page_annotation_version
        FOREIGN KEY (chapter_page_version_id)
        REFERENCES manga.ChapterPageVersion(chapter_page_version_id),

    CONSTRAINT fk_chapter_page_annotation_region
        FOREIGN KEY (page_region_id)
        REFERENCES manga.PageRegion(page_region_id),

    CONSTRAINT fk_chapter_page_annotation_annotated_by
        FOREIGN KEY (annotated_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_chapter_page_annotation_resolved_by
        FOREIGN KEY (resolved_by_user_id)
        REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_chapter_page_annotation_page_id ON manga.ChapterPageAnnotation (chapter_page_version_id);
CREATE INDEX ix_chapter_page_annotation_region
ON manga.ChapterPageAnnotation(page_region_id)
WHERE page_region_id IS NOT NULL;
CREATE INDEX ix_chapter_page_annotation_issue_type_code
ON manga.ChapterPageAnnotation(issue_type_code)
WHERE issue_type_code IS NOT NULL;
CREATE INDEX ix_chapter_page_annotation_annotated_by
ON manga.ChapterPageAnnotation(annotated_by_user_id);

CREATE TABLE manga.ChapterPageTask (
    chapter_page_task_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task PRIMARY KEY,
    chapter_page_id BIGINT NOT NULL,
    assigned_to_user_id INT NOT NULL,
    type_code NVARCHAR(50) NOT NULL,
    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_chapter_task_status_code DEFAULT (N'ASSIGNED'),
    task_title NVARCHAR(200) NOT NULL,
    task_description NVARCHAR(MAX) NOT NULL,
    target_region_description NVARCHAR(250) NULL,
    priority_level TINYINT NOT NULL CONSTRAINT df_chapter_task_priority_level DEFAULT (3),
    due_at_utc DATETIME2(0) NULL,
    compensation_amount DECIMAL(12,2) NULL,
    completed_page_version_id BIGINT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NOT NULL,
    updated_at_utc DATETIME2(0) NULL,
    CONSTRAINT ck_chapter_task_status_code
        CHECK (status_code IN (
         N'ASSIGNED',
         N'UNDER_REVIEW',
         N'COMPLETED',
         N'CANCELLED'
    )),
    CONSTRAINT ck_chapter_task_compensation_amount
        CHECK (compensation_amount IS NULL OR compensation_amount >= 0),
    CONSTRAINT ck_chapter_page_task_priority_level
        CHECK (priority_level BETWEEN 1 AND 5),
    CONSTRAINT ck_chapter_page_task_output_required
        CHECK (
        status_code NOT IN (N'UNDER_REVIEW', N'COMPLETED')
        OR completed_page_version_id IS NOT NULL
        ),
    CONSTRAINT fk_chapter_page_task_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
    CONSTRAINT fk_chapter_page_task_assigned_to FOREIGN KEY (assigned_to_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_type FOREIGN KEY (type_code) REFERENCES manga.TaskType(code),
    CONSTRAINT fk_chapter_page_task_completed_version_same_page
    FOREIGN KEY (completed_page_version_id, chapter_page_id)
        REFERENCES manga.ChapterPageVersion(chapter_page_version_id, chapter_page_id),
    CONSTRAINT fk_chapter_task_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id)
);
CREATE INDEX ix_chapter_page_task_assignee_status_due
ON manga.ChapterPageTask (
    assigned_to_user_id,
    status_code,
    due_at_utc
)
INCLUDE (
    chapter_page_id,
    type_code,
    priority_level,
    task_title
);

CREATE INDEX ix_chapter_page_task_page_status
ON manga.ChapterPageTask (
    chapter_page_id,
    status_code
)
INCLUDE (
    assigned_to_user_id,
    type_code,
    priority_level,
    due_at_utc,
    task_title
);

CREATE INDEX ix_chapter_page_task_status_due
ON manga.ChapterPageTask (
    status_code,
    due_at_utc
)
INCLUDE (
    assigned_to_user_id,
    chapter_page_id,
    priority_level,
    task_title
);

CREATE TABLE manga.ChapterEditorialReview (
    chapter_editorial_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_editorial_review PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    decision_code NVARCHAR(50) NOT NULL,
    comments NVARCHAR(MAX) NULL,
    markup_file_id BIGINT NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_editorial_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_chapter_editorial_review_decision_code
        CHECK (decision_code IN (
            N'APPROVED',
            N'REVISION_REQUESTED',
            N'CANCELLED'
    )),
     CONSTRAINT ck_chapter_editorial_review_feedback_required
        CHECK (
            decision_code = N'APPROVED'
            OR comments IS NOT NULL
            OR markup_file_id IS NOT NULL
        ),
    CONSTRAINT fk_chapter_editorial_review_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id)
);

CREATE INDEX ix_chapter_editorial_review_chapter_id ON manga.ChapterEditorialReview (chapter_id);
CREATE INDEX ix_chapter_editorial_review_reviewer
ON manga.ChapterEditorialReview(reviewer_user_id);
CREATE INDEX ix_chapter_editorial_review_decision_code
ON manga.ChapterEditorialReview(decision_code);

CREATE TABLE manga.SeriesRankingSnapshot (
    series_ranking_snapshot_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_ranking_snapshot PRIMARY KEY,
    series_id BIGINT NOT NULL,
    ranking_period_type_code NVARCHAR(50) NOT NULL,
    period_start_date DATE NOT NULL,
    period_end_date DATE NOT NULL,
    rank_position INT NOT NULL,
    ranking_score DECIMAL(10,2) NOT NULL,
    reader_vote_count INT NOT NULL CONSTRAINT df_series_ranking_snapshot_reader_vote_count DEFAULT (0),
    cancellation_risk_score DECIMAL(10,2) NULL,
    generated_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_generated_at_utc DEFAULT (SYSUTCDATETIME()),
    generated_by_user_id INT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_created_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_series_ranking_snapshot_period_type_code
    CHECK (ranking_period_type_code IN (
        N'WEEKLY',
        N'MONTHLY',
        N'SEASONAL'
    )),
    CONSTRAINT fk_series_ranking_snapshot_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_ranking_snapshot_generated_by FOREIGN KEY (generated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT ck_series_ranking_snapshot_rank_position CHECK (rank_position >= 1),
    CONSTRAINT ck_series_ranking_snapshot_period CHECK (period_end_date >= period_start_date),
    CONSTRAINT uq_series_ranking_snapshot_series_period
    UNIQUE (series_id, ranking_period_type_code, period_start_date)
);

CREATE INDEX ix_series_ranking_snapshot_period_type_code
ON manga.SeriesRankingSnapshot(ranking_period_type_code);

CREATE INDEX ix_series_ranking_snapshot_period_start
ON manga.SeriesRankingSnapshot(period_start_date);

CREATE INDEX ix_series_ranking_snapshot_period_rank
ON manga.SeriesRankingSnapshot (
    ranking_period_type_code,
    period_start_date DESC,
    rank_position
)
INCLUDE (
    series_id,
    ranking_score,
    reader_vote_count,
    cancellation_risk_score,
    period_end_date
);

CREATE INDEX ix_series_ranking_snapshot_series_period
ON manga.SeriesRankingSnapshot (
    series_id,
    ranking_period_type_code,
    period_start_date DESC
)
INCLUDE (
    rank_position,
    ranking_score,
    reader_vote_count,
    cancellation_risk_score,
    period_end_date,
    generated_at_utc
);

CREATE TABLE manga.ChapterReaderVoteSnapshot (
    chapter_reader_vote_snapshot_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_chapter_reader_vote_snapshot PRIMARY KEY,

    chapter_id BIGINT NOT NULL,

    reader_vote_count INT NOT NULL
        CONSTRAINT df_chapter_reader_vote_snapshot_reader_vote_count DEFAULT (0),

    average_rating DECIMAL(4,2) NULL,

    positive_feedback_count INT NULL,
    negative_feedback_count INT NULL,

    data_source_note NVARCHAR(500) NULL,

    entered_by_user_id INT NOT NULL,

    voted_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_chapter_reader_vote_snapshot_voted_at_utc
        DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT ck_chapter_reader_vote_snapshot_counts
    CHECK (
        reader_vote_count >= 0
        AND (positive_feedback_count IS NULL OR positive_feedback_count >= 0)
        AND (negative_feedback_count IS NULL OR negative_feedback_count >= 0)
    ),

    CONSTRAINT ck_chapter_reader_vote_snapshot_rating
    CHECK (
        average_rating IS NULL
        OR average_rating BETWEEN 0 AND 10
    ),

    CONSTRAINT fk_chapter_reader_vote_snapshot_chapter
        FOREIGN KEY (chapter_id)
        REFERENCES manga.Chapter(chapter_id),

    CONSTRAINT fk_chapter_reader_vote_snapshot_entered_by
        FOREIGN KEY (entered_by_user_id)
        REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_reader_vote_snapshot_chapter
        UNIQUE (chapter_id)
);

CREATE INDEX ix_chapter_reader_vote_snapshot_voted_at
ON manga.ChapterReaderVoteSnapshot (voted_at_utc)
INCLUDE (
    chapter_id,
    reader_vote_count,
    average_rating,
    positive_feedback_count,
    negative_feedback_count
);
CREATE INDEX ix_chapter_reader_vote_snapshot_entered_by
ON manga.ChapterReaderVoteSnapshot (
    entered_by_user_id,
    voted_at_utc DESC
)
INCLUDE (
    chapter_id,
    reader_vote_count,
    average_rating
);

CREATE TABLE manga.Notification (
    notification_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_notification PRIMARY KEY,
    recipient_user_id INT NOT NULL,
    notification_type_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_notification_type_code DEFAULT (N'SYSTEM_MESSAGE'),
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    related_entity_type NVARCHAR(50) NULL,
    related_entity_id BIGINT NULL,
    read_at_utc DATETIME2(0) NULL,
    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_notification_created_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT ck_notification_type_code
    CHECK (notification_type_code IN (
        N'PROPOSAL_REVIEW',
        N'PROPOSAL_DECISION',
        N'TASK_ASSIGNMENT',
        N'TASK_REVIEW',
        N'CHAPTER_REVIEW',
        N'RANKING_WARNING',
        N'PUBLICATION_SCHEDULE',
        N'SYSTEM_MESSAGE'
    )),
    CONSTRAINT ck_notification_related_entity_pair
    CHECK (
        (related_entity_type IS NULL AND related_entity_id IS NULL)
        OR
        (related_entity_type IS NOT NULL AND related_entity_id IS NOT NULL)
    ),
    CONSTRAINT fk_notification_recipient
        FOREIGN KEY (recipient_user_id)
        REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_notification_recipient_read_created
ON manga.Notification (
    recipient_user_id,
    read_at_utc,
    created_at_utc DESC
)
INCLUDE (
    notification_type_code,
    title,
    related_entity_type,
    related_entity_id
);

CREATE INDEX ix_notification_unread_recipient
ON manga.Notification (
    recipient_user_id,
    created_at_utc DESC
)
WHERE read_at_utc IS NULL;

CREATE INDEX ix_notification_related_entity
ON manga.Notification (
    related_entity_type,
    related_entity_id
)
WHERE related_entity_type IS NOT NULL
  AND related_entity_id IS NOT NULL;


--CREATE TABLE manga.SeriesStatusHistory (
--    series_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_status_history PRIMARY KEY,
--    series_id BIGINT NOT NULL,
--    from_status_id SMALLINT NULL,
--    to_status_id SMALLINT NOT NULL,
--    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
--    changed_by_user_id INT NULL,
--    change_reason NVARCHAR(500) NULL,
--    related_proposal_id BIGINT NULL,
--    related_chapter_id BIGINT NULL,
--    CONSTRAINT fk_series_status_history_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
--    CONSTRAINT fk_series_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.SeriesStatusLookup(id),
--    CONSTRAINT fk_series_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.SeriesStatusLookup(id),
--    CONSTRAINT fk_series_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
--    CONSTRAINT fk_series_status_history_related_proposal FOREIGN KEY (related_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
--    CONSTRAINT fk_series_status_history_related_chapter FOREIGN KEY (related_chapter_id) REFERENCES manga.Chapter(chapter_id)
--);


--CREATE TABLE manga.SeriesProposalStatusHistory (
--    series_proposal_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_proposal_status_history PRIMARY KEY,
--    series_proposal_id BIGINT NOT NULL,
--    from_status_id SMALLINT NULL,
--    to_status_id SMALLINT NOT NULL,
--    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
--    changed_by_user_id INT NULL,
--    change_reason NVARCHAR(500) NULL,
--    related_review_id BIGINT NULL,
--    related_board_decision_id BIGINT NULL,
--    CONSTRAINT fk_series_proposal_status_history_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
--    CONSTRAINT fk_series_proposal_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.ProposalStatusLookup(id),
--    CONSTRAINT fk_series_proposal_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.ProposalStatusLookup(id),
--    CONSTRAINT fk_series_proposal_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
--    CONSTRAINT fk_series_proposal_status_history_related_review FOREIGN KEY (related_review_id) REFERENCES manga.SeriesEditorialReview(series_editorial_review_id),
--    CONSTRAINT fk_series_proposal_status_history_related_board_decision FOREIGN KEY (related_board_decision_id) REFERENCES manga.SeriesBoardDecision(series_board_decision_id)
--);


--CREATE TABLE manga.ChapterStatusHistory (
--    chapter_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_status_history PRIMARY KEY,
--    chapter_id BIGINT NOT NULL,
--    from_status_id SMALLINT NULL,
--    to_status_id SMALLINT NOT NULL,
--    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
--    changed_by_user_id INT NULL,
--    change_reason NVARCHAR(500) NULL,
--    related_submission_id BIGINT NULL,
--    related_release_id BIGINT NULL,
--CONSTRAINT ck_chapter_status_history_reason_required
--CHECK (
--    to_status_code NOT IN (N'ON_HOLD', N'CANCELLED')
--    OR change_reason IS NOT NULL
--)
--    CONSTRAINT fk_chapter_status_history_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
--    CONSTRAINT fk_chapter_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.ChapterStatusLookup(id),
--    CONSTRAINT fk_chapter_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.ChapterStatusLookup(id),
--    CONSTRAINT fk_chapter_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
--    CONSTRAINT fk_chapter_status_history_related_submission FOREIGN KEY (related_submission_id) REFERENCES manga.ChapterSubmission(chapter_submission_id),
--    CONSTRAINT fk_chapter_status_history_related_release FOREIGN KEY (related_release_id) REFERENCES manga.ChapterRelease(chapter_release_id)
--);


--CREATE TABLE manga.ChapterPageStatusHistory (
--    chapter_page_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_page_status_history PRIMARY KEY,
--    chapter_page_id BIGINT NOT NULL,
--    from_status_id SMALLINT NULL,
--    to_status_id SMALLINT NOT NULL,
--    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
--    changed_by_user_id INT NULL,
--    change_reason NVARCHAR(500) NULL,
--    CONSTRAINT fk_chapter_page_status_history_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
--    CONSTRAINT fk_chapter_page_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.PageStatusLookup(id),
--    CONSTRAINT fk_chapter_page_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.PageStatusLookup(id),
--    CONSTRAINT fk_chapter_page_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id)
--);


--CREATE TABLE manga.ChapterTaskStatusHistory (
--    chapter_task_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task_status_history PRIMARY KEY,
--    chapter_task_id BIGINT NOT NULL,
--    from_status_id SMALLINT NULL,
--    to_status_id SMALLINT NOT NULL,
--    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
--    changed_by_user_id INT NULL,
--    change_reason NVARCHAR(500) NULL,
--    CONSTRAINT fk_chapter_task_status_history_task FOREIGN KEY (chapter_task_id) REFERENCES manga.ChapterTask(chapter_task_id),
--    CONSTRAINT fk_chapter_task_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.TaskStatusLookup(id),
--    CONSTRAINT fk_chapter_task_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.TaskStatusLookup(id),
--    CONSTRAINT fk_chapter_task_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id)
--);

