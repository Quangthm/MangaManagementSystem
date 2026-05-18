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
    password_hash NVARCHAR(255) NOT NULL,

    status NVARCHAR(20) NOT NULL
        CONSTRAINT df_users_status DEFAULT 'ACTIVE',

    password_changed_at DATETIME2(7) NOT NULL
        CONSTRAINT df_users_password_changed_at DEFAULT SYSUTCDATETIME(),

    password_last_set_by_user_id INT NULL,

    failed_login_attempts INT NOT NULL
        CONSTRAINT df_users_failed_login_attempts DEFAULT (0),

    last_failed_login_at DATETIME2(7) NULL,

    locked_until DATETIME2(7) NULL,

    last_login_at DATETIME2(7) NULL,

    created_at DATETIME2(7) NOT NULL
        CONSTRAINT df_users_created_at DEFAULT SYSUTCDATETIME(),

    updated_at DATETIME2(7) NULL,

    updated_by_user_id INT NULL,

    CONSTRAINT uq_users_username UNIQUE (username),

    CONSTRAINT ck_users_status_values
        CHECK (status IN ('ACTIVE', 'DISABLED', 'LOCKED')),

    CONSTRAINT ck_users_failed_login_attempts
        CHECK (failed_login_attempts >= 0)
);

CREATE TABLE auth.Roles (
    role_id SMALLINT IDENTITY PRIMARY KEY,
    role_name NVARCHAR(30)  NOT NULL,
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);

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

CREATE TABLE audit.AuditLog (
    audit_id INT IDENTITY(1,1) PRIMARY KEY, 

    actor_user_id INT NOT NULL,
    actor_role_name NVARCHAR(30) NOT NULL, 
    
    action NVARCHAR(50) NOT NULL, 
    object_type NVARCHAR(50) NOT NULL, 
    object_id INT NOT NULL,            
    
    diff_json NVARCHAR(MAX),        
    
    timestamp DATETIME2(7) NOT NULL 
        CONSTRAINT df_auditlog_timestamp DEFAULT SYSUTCDATETIME(),

    CONSTRAINT fk_auditlog_user 
        FOREIGN KEY (actor_user_id) REFERENCES auth.Users(user_id)
);

CREATE TABLE audit.HashChain (
    anchor_id INT IDENTITY(1,1) PRIMARY KEY,
    audit_id INT NOT NULL, 
    prev_hash CHAR(64) NULL, 
    row_hash CHAR(64) NOT NULL,
    chain_hash CHAR(64) NOT NULL,

    created_at DATETIME2(7) NOT NULL 
        CONSTRAINT df_hashchain_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT uq_hashchain_audit UNIQUE (audit_id),
    CONSTRAINT fk_hashchain_audit 
        FOREIGN KEY (audit_id) REFERENCES audit.AuditLog(audit_id)
);

CREATE TABLE manga.SeriesStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_series_status_lookup_code
        UNIQUE (code)
);


INSERT INTO manga.SeriesStatusLookup (code, name, sort_order)
VALUES
(N'PROPOSAL_DRAFT', N'Proposal Draft', 1),
(N'UNDER_EDITORIAL_REVIEW', N'Under Editorial Review', 2),
(N'BOARD_REVIEW', N'Board Review', 3),
(N'SERIALIZED', N'Serialized', 4),
(N'ACTIVE', N'Active', 5),
(N'HIATUS', N'Hiatus', 6),
(N'CANCELLED', N'Cancelled', 7),
(N'COMPLETED', N'Completed', 8);



CREATE TABLE manga.ProposalStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_proposal_status_lookup_code
        UNIQUE (code)
);


INSERT INTO manga.ProposalStatusLookup (code, name, sort_order)
VALUES
(N'DRAFT', N'Draft', 1),
(N'UNDER_EDITORIAL_REVIEW', N'Under Editorial Review', 2),
(N'UNDER_BOARD_REVIEW', N'Under Board Review', 3),
(N'REVISION_REQUIRED', N'Revision Required', 4),
(N'APPROVED', N'Approved', 5),
(N'REJECTED', N'Rejected', 6),
(N'WITHDRAWN', N'Withdrawn', 7);


CREATE TABLE manga.ChapterStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_chapter_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.ChapterStatusLookup (code, name, sort_order)
VALUES
(N'DRAFT', N'Draft', 1),
(N'UNDER_REVIEW', N'Under Review', 2),
(N'REVISION_REQUESTED', N'Revision Requested', 3),
(N'APPROVED', N'Approved', 4),
(N'RELEASED', N'Released', 5),
(N'ON_HOLD', N'On Hold', 6),
(N'CANCELLED', N'Cancelled', 7);


CREATE TABLE manga.PageStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    is_active BIT NOT NULL
        CONSTRAINT df_page_status_lookup_is_active DEFAULT (1),

    CONSTRAINT uq_page_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.PageStatusLookup (code, name, sort_order)
VALUES
(N'DRAFT', N'Draft', 1),
(N'IN_PROGRESS', N'In Progress', 2),
(N'UNDER_REVIEW', N'Under Review', 3),
(N'APPROVED', N'Approved', 4),
(N'RELEASED', N'Released', 5),
(N'ARCHIVED', N'Archived', 6);

CREATE TABLE manga.TaskStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_task_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.TaskStatusLookup (code, name, sort_order)
VALUES
(N'DRAFT', N'Draft', 1),
(N'ASSIGNED', N'Assigned', 2),
(N'IN_PROGRESS', N'In Progress', 3),
(N'SUBMITTED', N'Submitted', 4),
(N'UNDER_REVIEW', N'Under Review', 5),
(N'REVISION_REQUESTED', N'Revision Requested', 6),
(N'APPROVED', N'Approved', 7),
(N'COMPLETED', N'Completed', 8),
(N'CANCELLED', N'Cancelled', 9);

CREATE TABLE manga.SubmissionStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_submission_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.SubmissionStatusLookup (code, name, sort_order)
VALUES
(N'DRAFT', N'Draft', 1),
(N'SUBMITTED', N'Submitted', 2),
(N'UNDER_REVIEW', N'Under Review', 3),
(N'REVISION_REQUESTED', N'Revision Requested', 4),
(N'APPROVED', N'Approved', 5),
(N'REJECTED', N'Rejected', 6),
(N'WITHDRAWN', N'Withdrawn', 7);


CREATE TABLE manga.ReviewDecisionLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_review_decision_lookup_code
        UNIQUE (code)
);


INSERT INTO manga.ReviewDecisionLookup (code, name, sort_order)
VALUES
(N'APPROVED', N'Approved', 1),
(N'REVISION_REQUESTED', N'Revision Requested', 2),
(N'REJECTED', N'Rejected', 3);


CREATE TABLE manga.VoteChoiceLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,



    CONSTRAINT uq_vote_choice_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.VoteChoiceLookup (code, name)
VALUES
(N'APPROVE', N'Approve'),
(N'REJECT', N'Reject'),
(N'ABSTAIN', N'Abstain');

INSERT INTO auth.Roles(role_name)
VALUES
(N'Mangaka'),
(N'Assistant'),
(N'Tantou Editor'),
(N'Editorial Board Member');

CREATE TABLE manga.TaskTypeLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_task_type_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.TaskTypeLookup (code, name)
VALUES
(N'BACKGROUND', N'Background'),
(N'SHADING', N'Shading'),
(N'EFFECTS', N'Effects'),
(N'CLEANUP', N'Cleanup'),
(N'DIALOGUE', N'Dialogue'),
(N'TYPESETTING', N'Typesetting'),
(N'REVIEW', N'Review'),
(N'OTHER', N'Other');


CREATE TABLE manga.AssignmentStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    sort_order SMALLINT NOT NULL,

    CONSTRAINT uq_assignment_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.AssignmentStatusLookup (code, name, sort_order)
VALUES
(N'ASSIGNED', N'Assigned', 1),
(N'ACCEPTED', N'Accepted', 2),
(N'REJECTED', N'Rejected', 3),
(N'IN_PROGRESS', N'In Progress', 4),
(N'COMPLETED', N'Completed', 5),
(N'CANCELLED', N'Cancelled', 6),
(N'REASSIGNED', N'Reassigned', 7);


CREATE TABLE manga.PublicationFrequencyLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_publication_frequency_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.PublicationFrequencyLookup (code, name)
VALUES
(N'WEEKLY', N'Weekly'),
(N'MONTHLY', N'Monthly'),
(N'IRREGULAR', N'Irregular');


CREATE TABLE manga.PublicationStatusLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_publication_status_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.PublicationStatusLookup (code, name)
VALUES
(N'SCHEDULED', N'Scheduled'),
(N'RELEASED', N'Released'),
(N'DELAYED', N'Delayed'),
(N'CANCELLED', N'Cancelled');


CREATE TABLE manga.RankingPeriodTypeLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_ranking_period_type_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.RankingPeriodTypeLookup (code, name)
VALUES
(N'WEEKLY', N'Weekly'),
(N'MONTHLY', N'Monthly'),
(N'SEASONAL', N'Seasonal');


CREATE TABLE manga.NotificationTypeLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_notification_type_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.NotificationTypeLookup (code, name)
VALUES
(N'PROPOSAL_REVIEW', N'Proposal Review'),
(N'PROPOSAL_DECISION', N'Proposal Decision'),
(N'TASK_ASSIGNMENT', N'Task Assignment'),
(N'TASK_REVIEW', N'Task Review'),
(N'CHAPTER_REVIEW', N'Chapter Review'),
(N'RANKING_WARNING', N'Ranking Warning'),
(N'PUBLICATION_SCHEDULE', N'Publication Schedule'),
(N'SYSTEM_MESSAGE', N'System Message');


CREATE TABLE manga.FilePurposeLookup (
    id SMALLINT IDENTITY(1,1) PRIMARY KEY,

    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,

    CONSTRAINT uq_file_purpose_lookup_code
        UNIQUE (code)
);

INSERT INTO manga.FilePurposeLookup (code, name)
VALUES
(N'SERIES_PROPOSAL', N'Series Proposal'),
(N'CHAPTER_DRAFT', N'Chapter Draft'),
(N'CHAPTER_ASSET', N'Chapter Asset'),
(N'TASK_REFERENCE', N'Task Reference'),
(N'TASK_SUBMISSION', N'Task Submission'),
(N'CHAPTER_SUBMISSION', N'Chapter Submission'),
(N'EDITORIAL_ATTACHMENT', N'Editorial Attachment'),
(N'PAGE_ANNOTATION_EXPORT', N'Page Annotation Export');

CREATE TABLE manga.FileResource (
    file_resource_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_file_resource PRIMARY KEY,
    file_purpose_id SMALLINT NOT NULL,
    original_file_name NVARCHAR(260) NOT NULL,
    stored_file_name NVARCHAR(260) NOT NULL,
    storage_path NVARCHAR(500) NOT NULL,
    content_type NVARCHAR(100) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    sha256_hash CHAR(64) NULL,
    uploaded_by_user_id INT NULL,
    uploaded_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_file_resource_uploaded_at_utc DEFAULT (SYSUTCDATETIME()),
    is_deleted BIT NOT NULL CONSTRAINT df_file_resource_is_deleted DEFAULT (0),
    CONSTRAINT fk_file_resource_file_purpose FOREIGN KEY (file_purpose_id) REFERENCES manga.FilePurposeLookup(id),
    CONSTRAINT fk_file_resource_uploaded_by_user FOREIGN KEY (uploaded_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_file_resource_stored_file_name UNIQUE (stored_file_name)
);

CREATE INDEX ix_file_resource_file_purpose_id ON manga.FileResource (file_purpose_id);

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
    current_status_id SMALLINT NOT NULL,
    current_ranking_score DECIMAL(10,2) NULL,
    current_rank_position INT NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT uq_series_series_code UNIQUE (series_code),
    CONSTRAINT uq_series_slug UNIQUE (slug),
    CONSTRAINT fk_series_cover_file FOREIGN KEY (cover_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_series_lead_mangaka FOREIGN KEY (lead_mangaka_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_status FOREIGN KEY (current_status_id) REFERENCES manga.SeriesStatusLookup(id),
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
    series_proposal_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_proposal PRIMARY KEY,
    series_id BIGINT NOT NULL,
    proposal_version_no SMALLINT NOT NULL,
    proposal_title NVARCHAR(200) NULL,
    synopsis_snapshot NVARCHAR(MAX) NOT NULL,
    character_overview_snapshot NVARCHAR(MAX) NULL,
    proposal_file_id BIGINT NOT NULL,
    proposal_status_id SMALLINT NOT NULL,
    submitted_by_user_id INT NOT NULL,
    submitted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_submitted_at_utc DEFAULT (SYSUTCDATETIME()),
    last_submitted_at_utc DATETIME2(0) NULL,
    revision_requested_reason NVARCHAR(500) NULL,
    withdrawn_at_utc DATETIME2(0) NULL,
    is_current_version BIT NOT NULL CONSTRAINT df_series_proposal_is_current_version DEFAULT (1),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_proposal_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_series_proposal_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_series_proposal_file FOREIGN KEY (proposal_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_series_proposal_status FOREIGN KEY (proposal_status_id) REFERENCES manga.ProposalStatusLookup(id),
    CONSTRAINT fk_series_proposal_submitted_by FOREIGN KEY (submitted_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_proposal_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_proposal_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_proposal_series_version UNIQUE (series_id, proposal_version_no)
);

CREATE INDEX ix_series_proposal_status_id ON manga.SeriesProposal (proposal_status_id);


CREATE TABLE manga.SeriesEditorialReview (
    series_editorial_review_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_editorial_review PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    review_round_no SMALLINT NOT NULL,
    reviewer_user_id INT NOT NULL,
    review_decision_id SMALLINT NOT NULL,
    comments NVARCHAR(MAX) NULL,
    markup_file_id BIGINT NULL,
    reviewed_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_editorial_review_reviewed_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_editorial_review_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_series_editorial_review_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_editorial_review_reviewer FOREIGN KEY (reviewer_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_editorial_review_decision FOREIGN KEY (review_decision_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT fk_series_editorial_review_markup_file FOREIGN KEY (markup_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_series_editorial_review_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_series_editorial_review_proposal_round UNIQUE (series_proposal_id, review_round_no)
);

CREATE INDEX ix_series_editorial_review_proposal_id ON manga.SeriesEditorialReview (series_proposal_id);


CREATE TABLE manga.SeriesBoardVote (
    series_board_vote_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_series_board_vote PRIMARY KEY,
    series_proposal_id BIGINT NOT NULL,
    board_member_user_id INT NOT NULL,
    vote_choice_id SMALLINT NOT NULL,
    vote_reason NVARCHAR(500) NULL,
    voted_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_voted_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_vote_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_series_board_vote_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_board_vote_board_member FOREIGN KEY (board_member_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_board_vote_vote_choice FOREIGN KEY (vote_choice_id) REFERENCES manga.VoteChoiceLookup(id),
    CONSTRAINT fk_series_board_vote_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
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
    decided_by_user_id INT NULL,
    decided_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_decision_decided_at_utc DEFAULT (SYSUTCDATETIME()),
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_series_board_decision_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    CONSTRAINT fk_series_board_decision_proposal FOREIGN KEY (series_proposal_id) REFERENCES manga.SeriesProposal(series_proposal_id),
    CONSTRAINT fk_series_board_decision_decision_type FOREIGN KEY (decision_type_id) REFERENCES manga.ReviewDecisionLookup(id),
    CONSTRAINT fk_series_board_decision_decided_by FOREIGN KEY (decided_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_series_board_decision_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
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
    current_status_id SMALLINT NOT NULL,
    planned_release_date DATE NULL,
    released_at_utc DATETIME2(0) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_chapter_series FOREIGN KEY (series_id) REFERENCES manga.Series(series_id),
    CONSTRAINT fk_chapter_status FOREIGN KEY (current_status_id) REFERENCES manga.ChapterStatusLookup(id),
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
    current_status_id SMALLINT NOT NULL,
    page_file_id BIGINT NULL,
    page_notes NVARCHAR(MAX) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_created_at_utc DEFAULT (SYSUTCDATETIME()),
    created_by_user_id INT NULL,
    updated_at_utc DATETIME2(0) NULL,
    updated_by_user_id INT NULL,
    CONSTRAINT fk_chapter_page_chapter FOREIGN KEY (chapter_id) REFERENCES manga.Chapter(chapter_id),
    CONSTRAINT fk_chapter_page_status FOREIGN KEY (current_status_id) REFERENCES manga.PageStatusLookup(id),
    CONSTRAINT fk_chapter_page_file FOREIGN KEY (page_file_id) REFERENCES manga.FileResource(file_resource_id),
    CONSTRAINT fk_chapter_page_created_by FOREIGN KEY (created_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT fk_chapter_page_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES auth.Users(user_id),
    CONSTRAINT uq_chapter_page_chapter_page_no UNIQUE (chapter_id, page_no)
);

CREATE INDEX ix_chapter_page_chapter_id ON manga.ChapterPage (chapter_id);
CREATE INDEX ix_chapter_page_current_status_id ON manga.ChapterPage (current_status_id);


CREATE TABLE manga.ChapterPageAnnotation (
    chapter_page_annotation_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_chapter_page_annotation PRIMARY KEY,
    chapter_page_id BIGINT NOT NULL,
    annotated_by_user_id INT NOT NULL,
    annotation_text NVARCHAR(MAX) NOT NULL,
    x DECIMAL(10,2) NULL,
    y DECIMAL(10,2) NULL,
    width DECIMAL(10,2) NULL,
    height DECIMAL(10,2) NULL,
    is_resolved BIT NOT NULL CONSTRAINT df_chapter_page_annotation_is_resolved DEFAULT (0),
    resolved_at_utc DATETIME2(0) NULL,
    created_at_utc DATETIME2(0) NOT NULL CONSTRAINT df_chapter_page_annotation_created_at_utc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT fk_chapter_page_annotation_page FOREIGN KEY (chapter_page_id) REFERENCES manga.ChapterPage(chapter_page_id),
    CONSTRAINT fk_chapter_page_annotation_annotated_by FOREIGN KEY (annotated_by_user_id) REFERENCES auth.Users(user_id)
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











