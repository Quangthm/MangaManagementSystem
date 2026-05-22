CREATE DATABASE MangaManagementDB;
GO
USE MangaManagementDB;
GO
/*
Manga Creation Workflow and Publishing Management System
SQL Server schema script
*/
SET NOCOUNT ON;

IF SCHEMA_ID(N'manga') IS NULL
    EXEC(N'CREATE SCHEMA manga');

IF SCHEMA_ID(N'auth') IS NULL
    EXEC(N'CREATE SCHEMA auth');

IF SCHEMA_ID(N'audit') IS NULL
    EXEC(N'CREATE SCHEMA audit');

CREATE TABLE auth.Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,

    username NVARCHAR(50) NOT NULL,
    email NVARCHAR(254) NOT NULL,

    password_hash NVARCHAR(255) NOT NULL,
    avatar_url NVARCHAR(500) NULL,
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
    stored_file_name NVARCHAR(260) NOT NULL,
    storage_path NVARCHAR(500) NOT NULL,
    content_type NVARCHAR(100) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    sha256_hash CHAR(64) NULL,
    uploaded_by_user_id INT NULL,
    uploaded_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_file_resource_uploaded_at_utc DEFAULT (SYSUTCDATETIME()),
    is_deleted BIT NOT NULL CONSTRAINT df_file_resource_is_deleted DEFAULT (0),
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
         N'REGISTRATION_PORTFOLIO'
        )),
    CONSTRAINT fk_file_resource_uploaded_by_user FOREIGN KEY (uploaded_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_file_resource_stored_file_name UNIQUE (stored_file_name)
);



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
(N'Editorial Board Member');

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

CREATE TABLE auth.Permissions (
    permission_id SMALLINT IDENTITY(1,1) PRIMARY KEY,
    
    module NVARCHAR(50) NOT NULL,
    
    action NVARCHAR(10) NOT NULL,

    CONSTRAINT ck_permissions_action 
        CHECK (action IN ('READ', 'WRITE')),
    CONSTRAINT uq_permissions_module_action 
        UNIQUE (module, action)
);
CREATE TABLE auth.RolePerm (
    role_id SMALLINT NOT NULL,
    permission_id SMALLINT NOT NULL,
    granted_at DATETIME2(7) NOT NULL CONSTRAINT df_rp_granted_at DEFAULT SYSUTCDATETIME(),
    granted_by_user_id INT NOT NULL,


    CONSTRAINT pk_role_perm 
        PRIMARY KEY (role_id, permission_id),

    CONSTRAINT fk_rp_role 
        FOREIGN KEY (role_id) REFERENCES auth.Roles(role_id),
    CONSTRAINT fk_rp_permission 
        FOREIGN KEY (permission_id) REFERENCES auth.Permissions(permission_id),
    CONSTRAINT fk_rp_granted_by 
        FOREIGN KEY (granted_by_user_id) REFERENCES auth.Users(user_id)
);

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
    lead_mangaka_user_id INT NOT NULL,
    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_series_current_status_code DEFAULT (N'PROPOSAL_DRAFT'),    current_ranking_score DECIMAL(10,2) NULL,
    rank_position INT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT ck_series_current_status_code
    CHECK (current_status_code IN (
        N'PROPOSAL_DRAFT',
        N'UNDER_EDITORIAL_REVIEW',
        N'BOARD_REVIEW',
        N'SERIALIZED',
        N'ACTIVE',
        N'HIATUS',
        N'CANCELLED',
        N'COMPLETED'
    )),
    CONSTRAINT uq_series_series_code UNIQUE (series_code),
    CONSTRAINT uq_series_slug UNIQUE (slug),
    CONSTRAINT fk_series_cover_file FOREIGN KEY (cover_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_series_lead_mangaka FOREIGN KEY (lead_mangaka_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_series_current_status_id ON manga.Series (current_status_id);
CREATE INDEX ix_series_lead_mangaka_user_id ON manga.Series (lead_mangaka_user_id);


CREATE TABLE manga.SeriesContributor (
    series_contributor_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_contributor PRIMARY KEY,
    series_id BIGINT NOT NULL,
    user_id INT NOT NULL,
    contributor_role_id SMALLINT NOT NULL,
    start_date DATE NOT NULL CONSTRAINT df_series_contributor_start_date DEFAULT (CONVERT(date, SYSUTCDATETIME())),
    end_date DATE NULL,
    notes NVARCHAR(500) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_contributor_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_series_contributor_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_contributor_user FOREIGN KEY (user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_contributor_role FOREIGN KEY (contributor_role_id) REFERENCES auth.Roles(role_id),
    CONSTRAINT fk_series_contributor_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_contributor_series_user_role UNIQUE (series_id, user_id, contributor_role_id, start_date)
);

CREATE INDEX ix_series_contributor_user_id ON manga.SeriesContributor (user_id);

CREATE TABLE manga.SeriesProposal (
    series_proposal_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_series_proposal PRIMARY KEY,

    series_id BIGINT NOT NULL,

    proposal_version_no SMALLINT NOT NULL,

    proposal_title NVARCHAR(200) NULL,

    synopsis_snapshot NVARCHAR(MAX) NOT NULL,
    character_overview_snapshot NVARCHAR(MAX) NULL,

    proposal_file_id BIGINT NOT NULL,

    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_series_proposal_status_code DEFAULT (N'DRAFT'),
        
    created_by_user_id INT NOT NULL,

    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_series_proposal_created_at_utc
        DEFAULT SYSUTCDATETIME(),

    updated_by_user_id INT NULL,

    updated_at_utc DATETIME2(0) NULL,

    submitted_by_user_id INT NOT NULL,

    submitted_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_series_proposal_submitted_at_utc
        DEFAULT SYSUTCDATETIME(),

    withdrawn_at_utc DATETIME2(0) NULL,
        CONSTRAINT df_series_proposal_is_current_version
        DEFAULT (1),
    CONSTRAINT ck_series_proposal_status_code
    CHECK (status_code IN (
        N'DRAFT',
        N'UNDER_EDITORIAL_REVIEW',
        N'UNDER_BOARD_REVIEW',
        N'REVISION_REQUIRED',
        N'APPROVED',
        N'REJECTED',
        N'WITHDRAWN'
    )),
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
        UNIQUE (series_id, proposal_version_no),

    CONSTRAINT ck_series_proposal_version_no
        CHECK (proposal_version_no >= 1)
);

CREATE INDEX ix_series_proposal_series_id
ON manga.SeriesProposal (series_id);

CREATE UNIQUE INDEX ux_series_proposal_current_version
ON manga.SeriesProposal (series_id)
WHERE is_current_version = 1;

CREATE TABLE manga.SeriesEditorialReview (
    series_editorial_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_editorial_review PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    review_decision_id SMALLINT NOT NULL,
    comments NVARCHAR(MAX) NULL,
    markup_file_id BIGINT NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_editorial_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_series_editorial_review_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_editorial_review_decision FOREIGN KEY (review_decision_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT fk_series_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
);

CREATE INDEX ix_series_editorial_review_proposal_id ON manga.SeriesEditorialReview (series_proposal_id);


CREATE TABLE manga.SeriesBoardVote (
    series_board_vote_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_board_vote PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    board_member_user_id INT NOT NULL,
    vote_choice_id SMALLINT NOT NULL,
    vote_reason NVARCHAR(500) NULL,
    voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_voted_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_series_board_vote_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_board_vote_board_member FOREIGN KEY (board_member_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_board_vote_vote_choice FOREIGN KEY (vote_choice_id) REFERENCES manga.VoteChoiceLookup(id),
    CONSTRAINT uq_series_board_vote_proposal_board_member UNIQUE (series_proposal_id, board_member_user_id)
);

CREATE INDEX ix_series_board_vote_proposal_id ON manga.SeriesBoardVote (series_proposal_id);


CREATE TABLE manga.SeriesBoardDecision (
    series_board_decision_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_board_decision PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    decision_type_id SMALLINT NOT NULL,
    aggregate_approve_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_approve_count DEFAULT (0),
    aggregate_reject_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_reject_count DEFAULT (0),
    aggregate_abstain_count INT NOT NULL CONSTRAINT df_series_board_decision_aggregate_abstain_count DEFAULT (0),
    decision_summary NVARCHAR(MAX) NULL,
    decided_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_decision_decided_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_series_board_decision_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_board_decision_decision_type FOREIGN KEY (decision_type_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT uq_series_board_decision_proposal UNIQUE (series_proposal_id)
);

CREATE INDEX ix_series_board_decision_decision_type_id ON manga.SeriesBoardDecision (decision_type_id);


CREATE TABLE manga.SeriesPublicationPolicy (
    series_publication_policy_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_publication_policy PRIMARY KEY,
    series_id BIGINT NOT NULL,
    publication_frequency_id SMALLINT NOT NULL,
    effective_from_date DATE NOT NULL,
    effective_to_date DATE NULL,
    policy_notes NVARCHAR(MAX) NULL,
    approved_by_user_id INT NULL,
    approved_at_utc DATETIME2(0) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_publication_policy_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_series_publication_policy_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_publication_policy_frequency FOREIGN KEY (publication_frequency_id) REFERENCES manga.PublicationFrequencyLookup(id),
    CONSTRAINT fk_series_publication_policy_approved_by FOREIGN KEY (approved_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_publication_policy_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_publication_policy_series_effective_from UNIQUE (series_id, effective_from_date)
);

CREATE INDEX ix_series_publication_policy_series_id ON manga.SeriesPublicationPolicy (series_id);


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
    updated_by_user_id INT NULL,
    CONSTRAINT ck_chapter_status_code
        CHECK (status_code IN (
        N'DRAFT',
        N'UNDER_REVIEW',
        N'REVISION_REQUESTED',
        N'APPROVED',
        N'RELEASED',
        N'ON_HOLD',
        N'CANCELLED'
        )),
    CONSTRAINT fk_chapter_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_chapter_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_series_chapter_number UNIQUE (series_id, chapter_number_label)
);

CREATE INDEX ix_chapter_series_id ON manga.Chapter (series_id);
CREATE INDEX ix_chapter_current_status_id ON manga.Chapter (current_status_id);


CREATE TABLE manga.ChapterPage (
    chapter_page_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_page PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    page_no INT NOT NULL,
    status_code NVARCHAR(50) NOT NULL
        CONSTRAINT df_chapter_page_status_code DEFAULT (N'DRAFT'),    page_file_id BIGINT NULL,
    page_notes NVARCHAR(MAX) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_chapter_page_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT ck_chapter_page_status_code
        CHECK (status_code IN (
           N'DRAFT',
           N'IN_PROGRESS',
           N'UNDER_REVIEW',
           N'APPROVED',
           N'RELEASED',
           N'ARCHIVED'
    )),
    CONSTRAINT fk_chapter_page_file FOREIGN KEY (page_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_chapter_page_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_page_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_page_chapter_page_no UNIQUE (chapter_id, page_no)
);

CREATE INDEX ix_chapter_page_chapter_id ON manga.ChapterPage (chapter_id);
CREATE INDEX ix_chapter_page_current_status_id ON manga.ChapterPage (current_status_id);

CREATE TABLE manga.ChapterPageVersion (
    chapter_page_version_id BIGINT IDENTITY(1,1)
        CONSTRAINT pk_chapter_page_version PRIMARY KEY,

    chapter_page_id BIGINT NOT NULL,
    version_no SMALLINT NOT NULL,

    page_file_id BIGINT NOT NULL,

    version_note NVARCHAR(500) NULL,
    is_current_version BIT NOT NULL
        CONSTRAINT df_chapter_page_version_is_current DEFAULT (0),

    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_chapter_page_version_created_at_utc DEFAULT SYSUTCDATETIME(),

    created_by_user_id INT NULL,

    CONSTRAINT fk_chapter_page_version_page
        FOREIGN KEY (chapter_page_id)
        REFERENCES manga.ChapterPage(chapter_page_id),

    CONSTRAINT fk_chapter_page_version_file
        FOREIGN KEY (page_file_id)
        REFERENCES manga.FileResource(file_resource_id),

    CONSTRAINT fk_chapter_page_version_created_by
        FOREIGN KEY (created_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT uq_chapter_page_version_no
        UNIQUE (chapter_page_id, version_no)
);

CREATE TABLE manga.AiProcessingJob (
    ai_processing_job_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_ai_processing_job PRIMARY KEY,

    ai_job_type_id SMALLINT NOT NULL,
    job_status_id SMALLINT NOT NULL,

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

    CONSTRAINT fk_ai_processing_job_type
        FOREIGN KEY (ai_job_type_id)
        REFERENCES manga.AiJobTypeLookup(id),

    CONSTRAINT fk_ai_processing_job_status
        FOREIGN KEY (job_status_id)
        REFERENCES manga.AiJobStatusLookup(id),

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

CREATE INDEX ix_ai_processing_job_status_id
ON manga.AiProcessingJob (job_status_id);

CREATE INDEX ix_ai_processing_job_type_id
ON manga.AiProcessingJob (ai_job_type_id);

CREATE TABLE manga.PageRegion (
    page_region_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_page_region PRIMARY KEY,

    chapter_page_id BIGINT NOT NULL,

    region_type_id SMALLINT NOT NULL,
    ai_processing_job_id BIGINT NULL,

    region_label NVARCHAR(100) NULL,

    x DECIMAL(10,2) NOT NULL,
    y DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,

    confidence_score DECIMAL(5,4) NULL,

    panel_order INT NULL,

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

    CONSTRAINT fk_page_region_page
        FOREIGN KEY (chapter_page_id)
        REFERENCES manga.ChapterPage(chapter_page_id),

    CONSTRAINT fk_page_region_type
        FOREIGN KEY (region_type_id)
        REFERENCES manga.RegionTypeLookup(id),

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

CREATE INDEX ix_page_region_page_id
ON manga.PageRegion (chapter_page_id);


CREATE INDEX ix_page_region_type_id
ON manga.PageRegion (region_type_id);


CREATE INDEX ix_page_region_ai_job_id
ON manga.PageRegion (ai_processing_job_id);


CREATE TABLE manga.ChapterPageAnnotation (
    chapter_page_annotation_id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT pk_chapter_page_annotation PRIMARY KEY,

    chapter_page_id BIGINT NOT NULL,
    page_region_id BIGINT NULL,

    annotation_issue_type_id SMALLINT NULL,

    annotated_by_user_id INT NOT NULL,

    annotation_text NVARCHAR(MAX) NOT NULL,

    x DECIMAL(10,2) NULL,
    y DECIMAL(10,2) NULL,
    width DECIMAL(10,2) NULL,
    height DECIMAL(10,2) NULL,

    is_resolved BIT NOT NULL
        CONSTRAINT df_chapter_page_annotation_is_resolved
        DEFAULT (0),

    resolved_at_utc DATETIME2(0) NULL,
    resolved_by_user_id INT NULL,

    created_at_utc DATETIME2(0) NOT NULL
        CONSTRAINT df_chapter_page_annotation_created_at_utc
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT fk_chapter_page_annotation_page
        FOREIGN KEY (chapter_page_id)
        REFERENCES manga.ChapterPage(chapter_page_id),

    CONSTRAINT fk_chapter_page_annotation_region
        FOREIGN KEY (page_region_id)
        REFERENCES manga.PageRegion(page_region_id),

    CONSTRAINT fk_chapter_page_annotation_issue_type
        FOREIGN KEY (annotation_issue_type_id)
        REFERENCES manga.AnnotationIssueTypeLookup(id),

    CONSTRAINT fk_chapter_page_annotation_annotated_by
        FOREIGN KEY (annotated_by_user_id)
        REFERENCES auth.Users(user_id),

    CONSTRAINT fk_chapter_page_annotation_resolved_by
        FOREIGN KEY (resolved_by_user_id)
        REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_chapter_page_annotation_page_id ON manga.ChapterPageAnnotation (chapter_page_id);


CREATE TABLE manga.ChapterTask (
    chapter_task_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    task_type_id SMALLINT NOT NULL,
    task_title NVARCHAR(200) NOT NULL,
    task_description NVARCHAR(MAX) NOT NULL,
    target_page_id BIGINT NULL,
    target_region_description NVARCHAR(250) NULL,
    priority_level TINYINT NOT NULL CONSTRAINT df_chapter_task_priority_level DEFAULT (3),
    current_status_id SMALLINT NOT NULL,
    due_at_utc DATETIME2(0) NULL,
    compensation_amount DECIMAL(12,2) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NOT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_chapter_task_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_task_type FOREIGN KEY (task_type_id) REFERENCES manga.TaskTypeLookup(id),
    CONSTRAINT fk_chapter_task_target_page FOREIGN KEY (target_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
    CONSTRAINT fk_chapter_task_status FOREIGN KEY (current_status_id) REFERENCES manga.TaskStatusLookup(id),
    CONSTRAINT fk_chapter_task_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_chapter_task_chapter_id_status_id ON manga.ChapterTask (chapter_id, current_status_id);
CREATE INDEX ix_chapter_task_target_page_id ON manga.ChapterTask (target_page_id);


CREATE TABLE manga.ChapterTaskAssignment (
    chapter_task_assignment_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task_assignment PRIMARY KEY,
    chapter_task_id BIGINT NOT NULL,
    assigned_to_user_id INT NOT NULL,
    assigned_by_user_id INT NOT NULL,
    assignment_status_id SMALLINT NOT NULL,
    assigned_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_assignment_assigned_at_utc DEFAULT (SYSUTCDATETIME()),
    accepted_at_utc DATETIME2(0) NULL,
    completed_at_utc DATETIME2(0) NULL,
    unassigned_at_utc DATETIME2(0) NULL,
    remarks NVARCHAR(500) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_assignment_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_task_assignment_task FOREIGN KEY (chapter_task_id) REFERENCES manga.ChapterTask(chapter_task_id),
    CONSTRAINT fk_chapter_task_assignment_assigned_to FOREIGN KEY (assigned_to_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_assignment_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_assignment_status FOREIGN KEY (assignment_status_id) REFERENCES manga.AssignmentStatusLookup(id),
    CONSTRAINT fk_chapter_task_assignment_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id)
);

CREATE INDEX ix_chapter_task_assignment_assigned_to_user_id ON manga.ChapterTaskAssignment (assigned_to_user_id, assignment_status_id);


CREATE TABLE manga.ChapterTaskSubmission (
    chapter_task_submission_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task_submission PRIMARY KEY,
    chapter_task_assignment_id BIGINT NOT NULL,
    submission_version_no SMALLINT NOT NULL,
    submitted_by_user_id INT NOT NULL,
    submission_file_id BIGINT NOT NULL,
    submission_status_id SMALLINT NOT NULL,
    submitted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_submission_submitted_at_utc DEFAULT (SYSUTCDATETIME()),
    remarks NVARCHAR(MAX) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_submission_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_task_submission_assignment FOREIGN KEY (chapter_task_assignment_id) REFERENCES manga.ChapterTaskAssignment(chapter_task_assignment_id),
    CONSTRAINT fk_chapter_task_submission_submitted_by FOREIGN KEY (submitted_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_submission_file FOREIGN KEY (submission_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_chapter_task_submission_status FOREIGN KEY (submission_status_id) REFERENCES manga.SubmissionStatusLookup(id),
    CONSTRAINT fk_chapter_task_submission_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_task_submission_assignment_version UNIQUE (chapter_task_assignment_id, submission_version_no)
);

CREATE INDEX ix_chapter_task_submission_assignment_id ON manga.ChapterTaskSubmission (chapter_task_assignment_id);


CREATE TABLE manga.ChapterTaskReview (
    chapter_task_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task_review PRIMARY KEY,
    chapter_task_submission_id BIGINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    review_decision_id SMALLINT NOT NULL,
    comments NVARCHAR(MAX) NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_review_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_task_review_submission FOREIGN KEY (chapter_task_submission_id) REFERENCES manga.ChapterTaskSubmission(chapter_task_submission_id),
    CONSTRAINT fk_chapter_task_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_task_review_decision FOREIGN KEY (review_decision_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT fk_chapter_task_review_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_task_review_submission UNIQUE (chapter_task_submission_id)
);


CREATE TABLE manga.ChapterSubmission (
    chapter_submission_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_submission PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    submission_version_no SMALLINT NOT NULL,
    submitted_by_user_id INT NOT NULL,
    submission_file_id BIGINT NOT NULL,
    submission_status_id SMALLINT NOT NULL,
    submitted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_submission_submitted_at_utc DEFAULT (SYSUTCDATETIME()),
    remarks NVARCHAR(MAX) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_submission_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_submission_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_submission_submitted_by FOREIGN KEY (submitted_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_submission_file FOREIGN KEY (submission_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_chapter_submission_status FOREIGN KEY (submission_status_id) REFERENCES manga.SubmissionStatusLookup(id),
    CONSTRAINT fk_chapter_submission_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_submission_chapter_version UNIQUE (chapter_id, submission_version_no)
);

CREATE INDEX ix_chapter_submission_chapter_id ON manga.ChapterSubmission (chapter_id);


CREATE TABLE manga.ChapterEditorialReview (
    chapter_editorial_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_editorial_review PRIMARY KEY,
    chapter_submission_id BIGINT NOT NULL,
    review_round_no SMALLINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    review_decision_id SMALLINT NOT NULL,
    comments NVARCHAR(MAX) NULL,
    markup_file_id BIGINT NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_editorial_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_editorial_review_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_editorial_review_submission FOREIGN KEY (chapter_submission_id) REFERENCES manga.ChapterSubmission(chapter_submission_id),
    CONSTRAINT fk_chapter_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_editorial_review_decision FOREIGN KEY (review_decision_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT fk_chapter_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_chapter_editorial_review_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_editorial_review_submission_round UNIQUE (chapter_submission_id, review_round_no)
);

CREATE INDEX ix_chapter_editorial_review_submission_id ON manga.ChapterEditorialReview (chapter_submission_id);


CREATE TABLE manga.ChapterRelease (
    chapter_release_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_release PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    release_version_no SMALLINT NOT NULL,
    scheduled_release_at_utc DATETIME2(0) NULL,
    released_at_utc DATETIME2(0) NULL,
    release_status_id SMALLINT NOT NULL,
    release_notes NVARCHAR(MAX) NULL,
    approved_by_user_id INT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_release_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_chapter_release_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_release_status FOREIGN KEY (release_status_id) REFERENCES manga.PublicationStatusLookup(id),
    CONSTRAINT fk_chapter_release_approved_by FOREIGN KEY (approved_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_release_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_release_chapter_version UNIQUE (chapter_id, release_version_no)
);

CREATE INDEX ix_chapter_release_chapter_id ON manga.ChapterRelease (chapter_id);


CREATE TABLE manga.SeriesRankingSnapshot (
    series_ranking_snapshot_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_ranking_snapshot PRIMARY KEY,
    series_id BIGINT NOT NULL,
    ranking_period_type_id SMALLINT NOT NULL,
    period_start_date DATE NOT NULL,
    period_end_date DATE NOT NULL,
    rank_position INT NOT NULL,
    ranking_score DECIMAL(10,2) NOT NULL,
    reader_vote_count INT NOT NULL CONSTRAINT df_series_ranking_snapshot_reader_vote_count DEFAULT (0),
    cancellation_risk_score DECIMAL(10,2) NULL,
    generated_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_generated_at_utc DEFAULT (SYSUTCDATETIME()),
    generated_by_user_id INT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_ranking_snapshot_created_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_series_ranking_snapshot_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_ranking_snapshot_period_type FOREIGN KEY (ranking_period_type_id) REFERENCES manga.RankingPeriodTypeLookup(id),
    CONSTRAINT fk_series_ranking_snapshot_generated_by FOREIGN KEY (generated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_ranking_snapshot_series_period UNIQUE (series_id, ranking_period_type_id, period_start_date, period_end_date),
    CONSTRAINT ck_series_ranking_snapshot_rank_position CHECK (rank_position >= 1),
    CONSTRAINT ck_series_ranking_snapshot_period CHECK (period_end_date >= period_start_date)
);

CREATE INDEX ix_series_ranking_snapshot_series_id_period ON manga.SeriesRankingSnapshot (series_id, ranking_period_type_id, period_start_date DESC);


CREATE TABLE manga.Notification (
    notification_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_notification PRIMARY KEY,
    recipient_user_id INT NOT NULL,
    notification_type_id SMALLINT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    related_entity_type NVARCHAR(50) NULL,
    related_entity_id BIGINT NULL,
    read_at_utc DATETIME2(0) NULL,
    sent_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_notification_sent_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_notification_created_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_notification_recipient FOREIGN KEY (recipient_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_notification_type FOREIGN KEY (notification_type_id) REFERENCES manga.NotificationTypeLookup(id)
);

CREATE INDEX ix_notification_unread_by_recipient
ON manga.Notification (recipient_user_id, sent_at_utc DESC)
WHERE read_at_utc IS NULL;

CREATE TABLE manga.SeriesStatusHistory (
    series_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_status_history PRIMARY KEY,
    series_id BIGINT NOT NULL,
    from_status_id SMALLINT NULL,
    to_status_id SMALLINT NOT NULL,
    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
    changed_by_user_id INT NULL,
    change_reason NVARCHAR(500) NULL,
    related_proposal_id BIGINT NULL,
    related_chapter_id BIGINT NULL,
    CONSTRAINT fk_series_status_history_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.SeriesStatusLookup(id),
    CONSTRAINT fk_series_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.SeriesStatusLookup(id),
    CONSTRAINT fk_series_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_status_history_related_proposal FOREIGN KEY (related_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_status_history_related_chapter FOREIGN KEY (related_chapter_id) REFERENCES manga.Chapter(chapter_id)
);


CREATE TABLE manga.SeriesProposalStatusHistory (
    series_proposal_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_proposal_status_history PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    from_status_id SMALLINT NULL,
    to_status_id SMALLINT NOT NULL,
    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
    changed_by_user_id INT NULL,
    change_reason NVARCHAR(500) NULL,
    related_review_id BIGINT NULL,
    related_board_decision_id BIGINT NULL,
    CONSTRAINT fk_series_proposal_status_history_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_proposal_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.ProposalStatusLookup(id),
    CONSTRAINT fk_series_proposal_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.ProposalStatusLookup(id),
    CONSTRAINT fk_series_proposal_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_proposal_status_history_related_review FOREIGN KEY (related_review_id) REFERENCES manga.SeriesEditorialReview(series_editorial_review_id),
    CONSTRAINT fk_series_proposal_status_history_related_board_decision FOREIGN KEY (related_board_decision_id) REFERENCES manga.SeriesBoardDecision(series_board_decision_id)
);


CREATE TABLE manga.ChapterStatusHistory (
    chapter_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_status_history PRIMARY KEY,
    chapter_id BIGINT NOT NULL,
    from_status_id SMALLINT NULL,
    to_status_id SMALLINT NOT NULL,
    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
    changed_by_user_id INT NULL,
    change_reason NVARCHAR(500) NULL,
    related_submission_id BIGINT NULL,
    related_release_id BIGINT NULL,
    CONSTRAINT fk_chapter_status_history_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.ChapterStatusLookup(id),
    CONSTRAINT fk_chapter_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.ChapterStatusLookup(id),
    CONSTRAINT fk_chapter_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_status_history_related_submission FOREIGN KEY (related_submission_id) REFERENCES manga.ChapterSubmission(chapter_submission_id),
    CONSTRAINT fk_chapter_status_history_related_release FOREIGN KEY (related_release_id) REFERENCES manga.ChapterRelease(chapter_release_id)
);


CREATE TABLE manga.ChapterPageStatusHistory (
    chapter_page_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_page_status_history PRIMARY KEY,
    chapter_page_id BIGINT NOT NULL,
    from_status_id SMALLINT NULL,
    to_status_id SMALLINT NOT NULL,
    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
    changed_by_user_id INT NULL,
    change_reason NVARCHAR(500) NULL,
    CONSTRAINT fk_chapter_page_status_history_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
    CONSTRAINT fk_chapter_page_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.PageStatusLookup(id),
    CONSTRAINT fk_chapter_page_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.PageStatusLookup(id),
    CONSTRAINT fk_chapter_page_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id)
);


CREATE TABLE manga.ChapterTaskStatusHistory (
    chapter_task_status_history_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_task_status_history PRIMARY KEY,
    chapter_task_id BIGINT NOT NULL,
    from_status_id SMALLINT NULL,
    to_status_id SMALLINT NOT NULL,
    changed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_task_status_history_changed_at_utc DEFAULT (SYSUTCDATETIME()),
    changed_by_user_id INT NULL,
    change_reason NVARCHAR(500) NULL,
    CONSTRAINT fk_chapter_task_status_history_task FOREIGN KEY (chapter_task_id) REFERENCES manga.ChapterTask(chapter_task_id),
    CONSTRAINT fk_chapter_task_status_history_from_status FOREIGN KEY (from_status_id) REFERENCES manga.TaskStatusLookup(id),
    CONSTRAINT fk_chapter_task_status_history_to_status FOREIGN KEY (to_status_id) REFERENCES manga.TaskStatusLookup(id),
    CONSTRAINT fk_chapter_task_status_history_changed_by FOREIGN KEY (changed_by_user_id) REFERENCES auth.Users(user_id)
);

