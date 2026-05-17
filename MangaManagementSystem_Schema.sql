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
CREATE TABLE auth.Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL,
    password_hash NVARCHAR(255) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    created_at DATETIME2(7) CONSTRAINT df_users_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT ck_users_status_values 
        CHECK (status IN ('ACTIVE', 'DISABLED', 'LOCKED'))
);

CREATE TABLE auth.Roles (
    role_id INT IDENTITY PRIMARY KEY,
    role_name NVARCHAR(30)  NOT NULL,
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);

CREATE TABLE auth.UserRole (
    user_id INT NOT NULL,
    role_id INT NOT NULL,

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
    permission_id INT IDENTITY(1,1) PRIMARY KEY,
    
    module NVARCHAR(50) NOT NULL,
    
    action NVARCHAR(10) NOT NULL,

    CONSTRAINT ck_permissions_action 
        CHECK (action IN ('READ', 'WRITE')),
    CONSTRAINT uq_permissions_module_action 
        UNIQUE (module, action)
);
CREATE TABLE auth.RolePerm (
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
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
    SeriesStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_SeriesStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_SeriesStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'ProposalDraft', N'Proposal Draft', 1);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'UnderEditorialReview', N'Under Editorial Review', 2);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'BoardReview', N'Board Review', 3);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'Serialized', N'Serialized', 4);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'Active', N'Active', 5);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'AtRisk', N'At Risk', 6);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'Hiatus', N'Hiatus', 7);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'Cancelled', N'Cancelled', 8);
INSERT INTO manga.SeriesStatusLookup (Code, Name, SortOrder) VALUES (N'Completed', N'Completed', 9);
GO
CREATE TABLE manga.ProposalStatusLookup (
    ProposalStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProposalStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_ProposalStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_ProposalStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'Draft', N'Draft', 1);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'UnderEditorialReview', N'Under Editorial Review', 2);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'UnderBoardReview', N'Under Board Review', 3);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'RevisionRequired', N'Revision Required', 4);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 5);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'Rejected', N'Rejected', 6);
INSERT INTO manga.ProposalStatusLookup (Code, Name, SortOrder) VALUES (N'Withdrawn', N'Withdrawn', 7);
GO
CREATE TABLE manga.ChapterStatusLookup (
    ChapterStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_ChapterStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_ChapterStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'Draft', N'Draft', 1);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'UnderReview', N'Under Review', 2);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'RevisionRequested', N'Revision Requested', 3);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 4);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'Released', N'Released', 5);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'OnHold', N'On Hold', 6);
INSERT INTO manga.ChapterStatusLookup (Code, Name, SortOrder) VALUES (N'Cancelled', N'Cancelled', 7);
GO
CREATE TABLE manga.PageStatusLookup (
    PageStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PageStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_PageStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_PageStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'Draft', N'Draft', 1);
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'InProgress', N'In Progress', 2);
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'UnderReview', N'Under Review', 3);
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 4);
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'Released', N'Released', 5);
INSERT INTO manga.PageStatusLookup (Code, Name, SortOrder) VALUES (N'Archived', N'Archived', 6);
GO
CREATE TABLE manga.TaskStatusLookup (
    TaskStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_TaskStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_TaskStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Draft', N'Draft', 1);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Assigned', N'Assigned', 2);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'InProgress', N'In Progress', 3);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Submitted', N'Submitted', 4);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'UnderReview', N'Under Review', 5);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'RevisionRequested', N'Revision Requested', 6);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 7);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Completed', N'Completed', 8);
INSERT INTO manga.TaskStatusLookup (Code, Name, SortOrder) VALUES (N'Cancelled', N'Cancelled', 9);
GO
CREATE TABLE manga.SubmissionStatusLookup (
    SubmissionStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SubmissionStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_SubmissionStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_SubmissionStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'Draft', N'Draft', 1);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'Submitted', N'Submitted', 2);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'UnderReview', N'Under Review', 3);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'RevisionRequested', N'Revision Requested', 4);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 5);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'Rejected', N'Rejected', 6);
INSERT INTO manga.SubmissionStatusLookup (Code, Name, SortOrder) VALUES (N'Withdrawn', N'Withdrawn', 7);
GO
CREATE TABLE manga.ReviewDecisionLookup (
    ReviewDecisionLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReviewDecisionLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_ReviewDecisionLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_ReviewDecisionLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.ReviewDecisionLookup (Code, Name, SortOrder) VALUES (N'Approved', N'Approved', 1);
INSERT INTO manga.ReviewDecisionLookup (Code, Name, SortOrder) VALUES (N'RevisionRequested', N'Revision Requested', 2);
INSERT INTO manga.ReviewDecisionLookup (Code, Name, SortOrder) VALUES (N'Rejected', N'Rejected', 3);
GO
CREATE TABLE manga.VoteChoiceLookup (
    VoteChoiceLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoteChoiceLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_VoteChoiceLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_VoteChoiceLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.VoteChoiceLookup (Code, Name, SortOrder) VALUES (N'Approve', N'Approve', 1);
INSERT INTO manga.VoteChoiceLookup (Code, Name, SortOrder) VALUES (N'Reject', N'Reject', 2);
INSERT INTO manga.VoteChoiceLookup (Code, Name, SortOrder) VALUES (N'Abstain', N'Abstain', 3);
GO
CREATE TABLE manga.ContributorRoleLookup (
    ContributorRoleLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContributorRoleLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_ContributorRoleLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_ContributorRoleLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.ContributorRoleLookup (Code, Name, SortOrder) VALUES (N'LeadMangaka', N'Lead Mangaka', 1);
INSERT INTO manga.ContributorRoleLookup (Code, Name, SortOrder) VALUES (N'Assistant', N'Assistant', 2);
INSERT INTO manga.ContributorRoleLookup (Code, Name, SortOrder) VALUES (N'TantouEditor', N'Tantou Editor', 3);
GO
CREATE TABLE manga.TaskTypeLookup (
    TaskTypeLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskTypeLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_TaskTypeLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_TaskTypeLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Background', N'Background', 1);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Shading', N'Shading', 2);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Effects', N'Effects', 3);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Cleanup', N'Cleanup', 4);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Dialogue', N'Dialogue', 5);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Typesetting', N'Typesetting', 6);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Review', N'Review', 7);
INSERT INTO manga.TaskTypeLookup (Code, Name, SortOrder) VALUES (N'Other', N'Other', 8);
GO
CREATE TABLE manga.AssignmentStatusLookup (
    AssignmentStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssignmentStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_AssignmentStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_AssignmentStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Assigned', N'Assigned', 1);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Accepted', N'Accepted', 2);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Rejected', N'Rejected', 3);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'InProgress', N'In Progress', 4);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Completed', N'Completed', 5);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Cancelled', N'Cancelled', 6);
INSERT INTO manga.AssignmentStatusLookup (Code, Name, SortOrder) VALUES (N'Reassigned', N'Reassigned', 7);
GO
CREATE TABLE manga.PublicationFrequencyLookup (
    PublicationFrequencyLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PublicationFrequencyLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_PublicationFrequencyLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_PublicationFrequencyLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.PublicationFrequencyLookup (Code, Name, SortOrder) VALUES (N'Weekly', N'Weekly', 1);
INSERT INTO manga.PublicationFrequencyLookup (Code, Name, SortOrder) VALUES (N'Monthly', N'Monthly', 2);
INSERT INTO manga.PublicationFrequencyLookup (Code, Name, SortOrder) VALUES (N'Irregular', N'Irregular', 3);
GO
CREATE TABLE manga.PublicationStatusLookup (
    PublicationStatusLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PublicationStatusLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_PublicationStatusLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_PublicationStatusLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.PublicationStatusLookup (Code, Name, SortOrder) VALUES (N'Scheduled', N'Scheduled', 1);
INSERT INTO manga.PublicationStatusLookup (Code, Name, SortOrder) VALUES (N'Released', N'Released', 2);
INSERT INTO manga.PublicationStatusLookup (Code, Name, SortOrder) VALUES (N'Delayed', N'Delayed', 3);
INSERT INTO manga.PublicationStatusLookup (Code, Name, SortOrder) VALUES (N'Cancelled', N'Cancelled', 4);
GO
CREATE TABLE manga.RankingPeriodTypeLookup (
    RankingPeriodTypeLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RankingPeriodTypeLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_RankingPeriodTypeLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_RankingPeriodTypeLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.RankingPeriodTypeLookup (Code, Name, SortOrder) VALUES (N'Weekly', N'Weekly', 1);
INSERT INTO manga.RankingPeriodTypeLookup (Code, Name, SortOrder) VALUES (N'Monthly', N'Monthly', 2);
INSERT INTO manga.RankingPeriodTypeLookup (Code, Name, SortOrder) VALUES (N'Seasonal', N'Seasonal', 3);
GO
CREATE TABLE manga.NotificationTypeLookup (
    NotificationTypeLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationTypeLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_NotificationTypeLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_NotificationTypeLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'ProposalReview', N'Proposal Review', 1);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'ProposalDecision', N'Proposal Decision', 2);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'TaskAssignment', N'Task Assignment', 3);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'TaskReview', N'Task Review', 4);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'ChapterReview', N'Chapter Review', 5);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'RankingWarning', N'Ranking Warning', 6);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'PublicationSchedule', N'Publication Schedule', 7);
INSERT INTO manga.NotificationTypeLookup (Code, Name, SortOrder) VALUES (N'SystemMessage', N'System Message', 8);
GO
CREATE TABLE manga.FilePurposeLookup (
    FilePurposeLookupId SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FilePurposeLookup PRIMARY KEY,
Code VARCHAR(50) NOT NULL CONSTRAINT UQ_FilePurposeLookup_Code UNIQUE,
Name NVARCHAR(100) NOT NULL,
SortOrder SMALLINT NOT NULL,
IsActive BIT NOT NULL CONSTRAINT DF_FilePurposeLookup_IsActive DEFAULT (1)
);
GO
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'SeriesProposal', N'Series Proposal', 1);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'ChapterDraft', N'Chapter Draft', 2);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'ChapterAsset', N'Chapter Asset', 3);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'TaskReference', N'Task Reference', 4);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'TaskSubmission', N'Task Submission', 5);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'ChapterSubmission', N'Chapter Submission', 6);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'EditorialAttachment', N'Editorial Attachment', 7);
INSERT INTO manga.FilePurposeLookup (Code, Name, SortOrder) VALUES (N'PageAnnotationExport', N'Page Annotation Export', 8);
GO
CREATE TABLE manga.FileResource (
    FileResourceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FileResource PRIMARY KEY,
    FilePurposeId SMALLINT NOT NULL,
    OriginalFileName NVARCHAR(260) NOT NULL,
    StoredFileName NVARCHAR(260) NOT NULL,
    StoragePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    FileSizeBytes BIGINT NOT NULL,
    Sha256Hash CHAR(64) NULL,
    UploadedByUserId INT NULL,
    UploadedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_FileResource_UploadedAtUtc DEFAULT (SYSUTCDATETIME()),
    IsDeleted BIT NOT NULL CONSTRAINT DF_FileResource_IsDeleted DEFAULT (0),
    CONSTRAINT FK_FileResource_FilePurpose FOREIGN KEY (FilePurposeId) REFERENCES manga.FilePurposeLookup(FilePurposeLookupId),
    CONSTRAINT FK_FileResource_UploadedByUser FOREIGN KEY (UploadedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_FileResource_StoredFileName UNIQUE (StoredFileName)
);
GO
CREATE INDEX IX_FileResource_FilePurposeId ON manga.FileResource (FilePurposeId);
GO
CREATE TABLE manga.Series (
    SeriesId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Series PRIMARY KEY,
    SeriesCode NVARCHAR(50) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(220) NOT NULL,
    Synopsis NVARCHAR(MAX) NOT NULL,
    CharacterOverview NVARCHAR(MAX) NULL,
    Genre NVARCHAR(100) NOT NULL,
    TargetAudience NVARCHAR(100) NULL,
    CoverFileId BIGINT NULL,
    LeadMangakaUserId INT NOT NULL,
    CurrentStatusId SMALLINT NOT NULL,
    CurrentRankingScore DECIMAL(10,2) NULL,
    CurrentRankPosition INT NULL,
    IsArchived BIT NOT NULL CONSTRAINT DF_Series_IsArchived DEFAULT (0),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Series_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    UpdatedAtUtc DATETIME2(0) NULL,
    UpdatedByUserId INT NULL,
    CONSTRAINT UQ_Series_SeriesCode UNIQUE (SeriesCode),
    CONSTRAINT UQ_Series_Slug UNIQUE (Slug),
    CONSTRAINT FK_Series_CoverFile FOREIGN KEY (CoverFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_Series_LeadMangaka FOREIGN KEY (LeadMangakaUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_Series_Status FOREIGN KEY (CurrentStatusId) REFERENCES manga.SeriesStatusLookup(SeriesStatusLookupId),
    CONSTRAINT FK_Series_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_Series_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE INDEX IX_Series_CurrentStatusId ON manga.Series (CurrentStatusId);
CREATE INDEX IX_Series_LeadMangakaUserId ON manga.Series (LeadMangakaUserId);
GO
CREATE TABLE manga.SeriesStatusHistory (
    SeriesStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesStatusHistory PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    FromStatusId SMALLINT NULL,
    ToStatusId SMALLINT NOT NULL,
    ChangedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesStatusHistory_ChangedAtUtc DEFAULT (SYSUTCDATETIME()),
    ChangedByUserId INT NULL,
    ChangeReason NVARCHAR(500) NULL,
    RelatedProposalId BIGINT NULL,
    RelatedChapterId BIGINT NULL,
    CONSTRAINT FK_SeriesStatusHistory_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_SeriesStatusHistory_FromStatus FOREIGN KEY (FromStatusId) REFERENCES manga.SeriesStatusLookup(SeriesStatusLookupId),
    CONSTRAINT FK_SeriesStatusHistory_ToStatus FOREIGN KEY (ToStatusId) REFERENCES manga.SeriesStatusLookup(SeriesStatusLookupId),
    CONSTRAINT FK_SeriesStatusHistory_ChangedBy FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE TABLE manga.SeriesContributor (
    SeriesContributorId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesContributor PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    UserId INT NOT NULL,
    ContributorRoleId SMALLINT NOT NULL,
    StartDate DATE NOT NULL CONSTRAINT DF_SeriesContributor_StartDate DEFAULT (CONVERT(date, SYSUTCDATETIME())),
    EndDate DATE NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_SeriesContributor_IsActive DEFAULT (1),
    Notes NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesContributor_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_SeriesContributor_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_SeriesContributor_User FOREIGN KEY (UserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesContributor_Role FOREIGN KEY (ContributorRoleId) REFERENCES manga.ContributorRoleLookup(ContributorRoleLookupId),
    CONSTRAINT FK_SeriesContributor_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesContributor_Series_User_Role UNIQUE (SeriesId, UserId, ContributorRoleId, StartDate)
);
GO
CREATE INDEX IX_SeriesContributor_UserId ON manga.SeriesContributor (UserId);
GO
CREATE TABLE manga.SeriesProposal (
    SeriesProposalId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesProposal PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    ProposalVersionNo SMALLINT NOT NULL,
    ProposalTitle NVARCHAR(200) NULL,
    SynopsisSnapshot NVARCHAR(MAX) NOT NULL,
    CharacterOverviewSnapshot NVARCHAR(MAX) NULL,
    ProposalFileId BIGINT NOT NULL,
    ProposalStatusId SMALLINT NOT NULL,
    SubmittedByUserId INT NOT NULL,
    SubmittedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesProposal_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()),
    LastSubmittedAtUtc DATETIME2(0) NULL,
    RevisionRequestedReason NVARCHAR(500) NULL,
    WithdrawnAtUtc DATETIME2(0) NULL,
    IsCurrentVersion BIT NOT NULL CONSTRAINT DF_SeriesProposal_IsCurrentVersion DEFAULT (1),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesProposal_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    UpdatedAtUtc DATETIME2(0) NULL,
    UpdatedByUserId INT NULL,
    CONSTRAINT FK_SeriesProposal_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_SeriesProposal_File FOREIGN KEY (ProposalFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_SeriesProposal_Status FOREIGN KEY (ProposalStatusId) REFERENCES manga.ProposalStatusLookup(ProposalStatusLookupId),
    CONSTRAINT FK_SeriesProposal_SubmittedBy FOREIGN KEY (SubmittedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesProposal_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesProposal_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesProposal_Series_Version UNIQUE (SeriesId, ProposalVersionNo)
);
GO
CREATE INDEX IX_SeriesProposal_StatusId ON manga.SeriesProposal (ProposalStatusId);
GO
CREATE TABLE manga.SeriesProposalStatusHistory (
    SeriesProposalStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesProposalStatusHistory PRIMARY KEY,
    SeriesProposalId BIGINT NOT NULL,
    FromStatusId SMALLINT NULL,
    ToStatusId SMALLINT NOT NULL,
    ChangedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesProposalStatusHistory_ChangedAtUtc DEFAULT (SYSUTCDATETIME()),
    ChangedByUserId INT NULL,
    ChangeReason NVARCHAR(500) NULL,
    RelatedReviewId BIGINT NULL,
    RelatedBoardDecisionId BIGINT NULL,
    CONSTRAINT FK_SeriesProposalStatusHistory_Proposal FOREIGN KEY (SeriesProposalId) REFERENCES manga.SeriesProposal(SeriesProposalId),
    CONSTRAINT FK_SeriesProposalStatusHistory_FromStatus FOREIGN KEY (FromStatusId) REFERENCES manga.ProposalStatusLookup(ProposalStatusLookupId),
    CONSTRAINT FK_SeriesProposalStatusHistory_ToStatus FOREIGN KEY (ToStatusId) REFERENCES manga.ProposalStatusLookup(ProposalStatusLookupId),
    CONSTRAINT FK_SeriesProposalStatusHistory_ChangedBy FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE TABLE manga.SeriesEditorialReview (
    SeriesEditorialReviewId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesEditorialReview PRIMARY KEY,
    SeriesProposalId BIGINT NOT NULL,
    ReviewRoundNo SMALLINT NOT NULL,
    ReviewerUserId INT NOT NULL,
    ReviewDecisionId SMALLINT NOT NULL,
    Comments NVARCHAR(MAX) NULL,
    MarkupFileId BIGINT NULL,
    ReviewedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesEditorialReview_ReviewedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesEditorialReview_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_SeriesEditorialReview_Proposal FOREIGN KEY (SeriesProposalId) REFERENCES manga.SeriesProposal(SeriesProposalId),
    CONSTRAINT FK_SeriesEditorialReview_Reviewer FOREIGN KEY (ReviewerUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesEditorialReview_Decision FOREIGN KEY (ReviewDecisionId) REFERENCES manga.ReviewDecisionLookup(ReviewDecisionLookupId),
    CONSTRAINT FK_SeriesEditorialReview_MarkupFile FOREIGN KEY (MarkupFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_SeriesEditorialReview_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesEditorialReview_Proposal_Round UNIQUE (SeriesProposalId, ReviewRoundNo)
);
GO
CREATE INDEX IX_SeriesEditorialReview_ProposalId ON manga.SeriesEditorialReview (SeriesProposalId);
GO
CREATE TABLE manga.SeriesBoardVote (
    SeriesBoardVoteId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesBoardVote PRIMARY KEY,
    SeriesProposalId BIGINT NOT NULL,
    BoardMemberUserId INT NOT NULL,
    VoteChoiceId SMALLINT NOT NULL,
    VoteReason NVARCHAR(500) NULL,
    VotedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesBoardVote_VotedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesBoardVote_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_SeriesBoardVote_Proposal FOREIGN KEY (SeriesProposalId) REFERENCES manga.SeriesProposal(SeriesProposalId),
    CONSTRAINT FK_SeriesBoardVote_BoardMember FOREIGN KEY (BoardMemberUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesBoardVote_VoteChoice FOREIGN KEY (VoteChoiceId) REFERENCES manga.VoteChoiceLookup(VoteChoiceLookupId),
    CONSTRAINT FK_SeriesBoardVote_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesBoardVote_Proposal_BoardMember UNIQUE (SeriesProposalId, BoardMemberUserId)
);
GO
CREATE INDEX IX_SeriesBoardVote_ProposalId ON manga.SeriesBoardVote (SeriesProposalId);
GO
CREATE TABLE manga.SeriesBoardDecision (
    SeriesBoardDecisionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesBoardDecision PRIMARY KEY,
    SeriesProposalId BIGINT NOT NULL,
    DecisionTypeId SMALLINT NOT NULL,
    AggregateApproveCount INT NOT NULL CONSTRAINT DF_SeriesBoardDecision_AggregateApproveCount DEFAULT (0),
    AggregateRejectCount INT NOT NULL CONSTRAINT DF_SeriesBoardDecision_AggregateRejectCount DEFAULT (0),
    AggregateAbstainCount INT NOT NULL CONSTRAINT DF_SeriesBoardDecision_AggregateAbstainCount DEFAULT (0),
    DecisionSummary NVARCHAR(MAX) NULL,
    DecidedByUserId INT NULL,
    DecidedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesBoardDecision_DecidedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesBoardDecision_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_SeriesBoardDecision_Proposal FOREIGN KEY (SeriesProposalId) REFERENCES manga.SeriesProposal(SeriesProposalId),
    CONSTRAINT FK_SeriesBoardDecision_DecisionType FOREIGN KEY (DecisionTypeId) REFERENCES manga.ReviewDecisionLookup(ReviewDecisionLookupId),
    CONSTRAINT FK_SeriesBoardDecision_DecidedBy FOREIGN KEY (DecidedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesBoardDecision_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesBoardDecision_Proposal UNIQUE (SeriesProposalId)
);
GO
CREATE INDEX IX_SeriesBoardDecision_DecisionTypeId ON manga.SeriesBoardDecision (DecisionTypeId);
GO
CREATE TABLE manga.SeriesPublicationPolicy (
    SeriesPublicationPolicyId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesPublicationPolicy PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    PublicationFrequencyId SMALLINT NOT NULL,
    EffectiveFromDate DATE NOT NULL,
    EffectiveToDate DATE NULL,
    PolicyNotes NVARCHAR(MAX) NULL,
    ApprovedByUserId INT NULL,
    ApprovedAtUtc DATETIME2(0) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesPublicationPolicy_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_SeriesPublicationPolicy_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_SeriesPublicationPolicy_Frequency FOREIGN KEY (PublicationFrequencyId) REFERENCES manga.PublicationFrequencyLookup(PublicationFrequencyLookupId),
    CONSTRAINT FK_SeriesPublicationPolicy_ApprovedBy FOREIGN KEY (ApprovedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_SeriesPublicationPolicy_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesPublicationPolicy_Series_EffectiveFrom UNIQUE (SeriesId, EffectiveFromDate)
);
GO
CREATE INDEX IX_SeriesPublicationPolicy_SeriesId ON manga.SeriesPublicationPolicy (SeriesId);
GO
CREATE TABLE manga.Chapter (
    ChapterId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Chapter PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    ChapterNumberLabel NVARCHAR(20) NOT NULL,
    ChapterTitle NVARCHAR(200) NULL,
    CurrentStatusId SMALLINT NOT NULL,
    IsFinalChapter BIT NOT NULL CONSTRAINT DF_Chapter_IsFinalChapter DEFAULT (0),
    PlannedReleaseDate DATE NULL,
    ReleasedAtUtc DATETIME2(0) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Chapter_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    UpdatedAtUtc DATETIME2(0) NULL,
    UpdatedByUserId INT NULL,
    CONSTRAINT FK_Chapter_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_Chapter_Status FOREIGN KEY (CurrentStatusId) REFERENCES manga.ChapterStatusLookup(ChapterStatusLookupId),
    CONSTRAINT FK_Chapter_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_Chapter_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_Chapter_Series_ChapterNumber UNIQUE (SeriesId, ChapterNumberLabel)
);
GO
CREATE INDEX IX_Chapter_SeriesId ON manga.Chapter (SeriesId);
CREATE INDEX IX_Chapter_CurrentStatusId ON manga.Chapter (CurrentStatusId);
GO
CREATE TABLE manga.ChapterStatusHistory (
    ChapterStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterStatusHistory PRIMARY KEY,
    ChapterId BIGINT NOT NULL,
    FromStatusId SMALLINT NULL,
    ToStatusId SMALLINT NOT NULL,
    ChangedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterStatusHistory_ChangedAtUtc DEFAULT (SYSUTCDATETIME()),
    ChangedByUserId INT NULL,
    ChangeReason NVARCHAR(500) NULL,
    RelatedSubmissionId BIGINT NULL,
    RelatedReleaseId BIGINT NULL,
    CONSTRAINT FK_ChapterStatusHistory_Chapter FOREIGN KEY (ChapterId) REFERENCES manga.Chapter(ChapterId),
    CONSTRAINT FK_ChapterStatusHistory_FromStatus FOREIGN KEY (FromStatusId) REFERENCES manga.ChapterStatusLookup(ChapterStatusLookupId),
    CONSTRAINT FK_ChapterStatusHistory_ToStatus FOREIGN KEY (ToStatusId) REFERENCES manga.ChapterStatusLookup(ChapterStatusLookupId),
    CONSTRAINT FK_ChapterStatusHistory_ChangedBy FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE TABLE manga.ChapterPage (
    ChapterPageId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterPage PRIMARY KEY,
    ChapterId BIGINT NOT NULL,
    PageNo INT NOT NULL,
    CurrentStatusId SMALLINT NOT NULL,
    PageFileId BIGINT NULL,
    PageNotes NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterPage_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    UpdatedAtUtc DATETIME2(0) NULL,
    UpdatedByUserId INT NULL,
    CONSTRAINT FK_ChapterPage_Chapter FOREIGN KEY (ChapterId) REFERENCES manga.Chapter(ChapterId),
    CONSTRAINT FK_ChapterPage_Status FOREIGN KEY (CurrentStatusId) REFERENCES manga.PageStatusLookup(PageStatusLookupId),
    CONSTRAINT FK_ChapterPage_File FOREIGN KEY (PageFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_ChapterPage_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterPage_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterPage_Chapter_PageNo UNIQUE (ChapterId, PageNo)
);
GO
CREATE INDEX IX_ChapterPage_ChapterId ON manga.ChapterPage (ChapterId);
CREATE INDEX IX_ChapterPage_CurrentStatusId ON manga.ChapterPage (CurrentStatusId);
GO
CREATE TABLE manga.ChapterPageStatusHistory (
    ChapterPageStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterPageStatusHistory PRIMARY KEY,
    ChapterPageId BIGINT NOT NULL,
    FromStatusId SMALLINT NULL,
    ToStatusId SMALLINT NOT NULL,
    ChangedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterPageStatusHistory_ChangedAtUtc DEFAULT (SYSUTCDATETIME()),
    ChangedByUserId INT NULL,
    ChangeReason NVARCHAR(500) NULL,
    CONSTRAINT FK_ChapterPageStatusHistory_Page FOREIGN KEY (ChapterPageId) REFERENCES manga.ChapterPage(ChapterPageId),
    CONSTRAINT FK_ChapterPageStatusHistory_FromStatus FOREIGN KEY (FromStatusId) REFERENCES manga.PageStatusLookup(PageStatusLookupId),
    CONSTRAINT FK_ChapterPageStatusHistory_ToStatus FOREIGN KEY (ToStatusId) REFERENCES manga.PageStatusLookup(PageStatusLookupId),
    CONSTRAINT FK_ChapterPageStatusHistory_ChangedBy FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE TABLE manga.ChapterPageAnnotation (
    ChapterPageAnnotationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterPageAnnotation PRIMARY KEY,
    ChapterPageId BIGINT NOT NULL,
    AnnotatedByUserId INT NOT NULL,
    AnnotationText NVARCHAR(MAX) NOT NULL,
    X DECIMAL(10,2) NULL,
    Y DECIMAL(10,2) NULL,
    Width DECIMAL(10,2) NULL,
    Height DECIMAL(10,2) NULL,
    IsResolved BIT NOT NULL CONSTRAINT DF_ChapterPageAnnotation_IsResolved DEFAULT (0),
    ResolvedAtUtc DATETIME2(0) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterPageAnnotation_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ChapterPageAnnotation_Page FOREIGN KEY (ChapterPageId) REFERENCES manga.ChapterPage(ChapterPageId),
    CONSTRAINT FK_ChapterPageAnnotation_AnnotatedBy FOREIGN KEY (AnnotatedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE INDEX IX_ChapterPageAnnotation_PageId ON manga.ChapterPageAnnotation (ChapterPageId);
GO
CREATE TABLE manga.ChapterTask (
    ChapterTaskId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterTask PRIMARY KEY,
    ChapterId BIGINT NOT NULL,
    TaskTypeId SMALLINT NOT NULL,
    TaskTitle NVARCHAR(200) NOT NULL,
    TaskDescription NVARCHAR(MAX) NOT NULL,
    TargetPageId BIGINT NULL,
    TargetRegionDescription NVARCHAR(250) NULL,
    PriorityLevel TINYINT NOT NULL CONSTRAINT DF_ChapterTask_PriorityLevel DEFAULT (3),
    CurrentStatusId SMALLINT NOT NULL,
    DueAtUtc DATETIME2(0) NULL,
    CompensationAmount DECIMAL(12,2) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTask_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NOT NULL,
    UpdatedAtUtc DATETIME2(0) NULL,
    UpdatedByUserId INT NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_ChapterTask_IsDeleted DEFAULT (0),
    CONSTRAINT FK_ChapterTask_Chapter FOREIGN KEY (ChapterId) REFERENCES manga.Chapter(ChapterId),
    CONSTRAINT FK_ChapterTask_Type FOREIGN KEY (TaskTypeId) REFERENCES manga.TaskTypeLookup(TaskTypeLookupId),
    CONSTRAINT FK_ChapterTask_TargetPage FOREIGN KEY (TargetPageId) REFERENCES manga.ChapterPage(ChapterPageId),
    CONSTRAINT FK_ChapterTask_Status FOREIGN KEY (CurrentStatusId) REFERENCES manga.TaskStatusLookup(TaskStatusLookupId),
    CONSTRAINT FK_ChapterTask_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterTask_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE INDEX IX_ChapterTask_ChapterId_StatusId ON manga.ChapterTask (ChapterId, CurrentStatusId);
CREATE INDEX IX_ChapterTask_TargetPageId ON manga.ChapterTask (TargetPageId);
GO
CREATE TABLE manga.ChapterTaskStatusHistory (
    ChapterTaskStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterTaskStatusHistory PRIMARY KEY,
    ChapterTaskId BIGINT NOT NULL,
    FromStatusId SMALLINT NULL,
    ToStatusId SMALLINT NOT NULL,
    ChangedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskStatusHistory_ChangedAtUtc DEFAULT (SYSUTCDATETIME()),
    ChangedByUserId INT NULL,
    ChangeReason NVARCHAR(500) NULL,
    CONSTRAINT FK_ChapterTaskStatusHistory_Task FOREIGN KEY (ChapterTaskId) REFERENCES manga.ChapterTask(ChapterTaskId),
    CONSTRAINT FK_ChapterTaskStatusHistory_FromStatus FOREIGN KEY (FromStatusId) REFERENCES manga.TaskStatusLookup(TaskStatusLookupId),
    CONSTRAINT FK_ChapterTaskStatusHistory_ToStatus FOREIGN KEY (ToStatusId) REFERENCES manga.TaskStatusLookup(TaskStatusLookupId),
    CONSTRAINT FK_ChapterTaskStatusHistory_ChangedBy FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE TABLE manga.ChapterTaskAssignment (
    ChapterTaskAssignmentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterTaskAssignment PRIMARY KEY,
    ChapterTaskId BIGINT NOT NULL,
    AssignedToUserId INT NOT NULL,
    AssignedByUserId INT NOT NULL,
    AssignmentStatusId SMALLINT NOT NULL,
    AssignedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskAssignment_AssignedAtUtc DEFAULT (SYSUTCDATETIME()),
    AcceptedAtUtc DATETIME2(0) NULL,
    CompletedAtUtc DATETIME2(0) NULL,
    UnassignedAtUtc DATETIME2(0) NULL,
    Remarks NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskAssignment_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterTaskAssignment_Task FOREIGN KEY (ChapterTaskId) REFERENCES manga.ChapterTask(ChapterTaskId),
    CONSTRAINT FK_ChapterTaskAssignment_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterTaskAssignment_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterTaskAssignment_Status FOREIGN KEY (AssignmentStatusId) REFERENCES manga.AssignmentStatusLookup(AssignmentStatusLookupId),
    CONSTRAINT FK_ChapterTaskAssignment_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id)
);
GO
CREATE INDEX IX_ChapterTaskAssignment_AssignedToUserId ON manga.ChapterTaskAssignment (AssignedToUserId, AssignmentStatusId);
GO
CREATE TABLE manga.ChapterTaskSubmission (
    ChapterTaskSubmissionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterTaskSubmission PRIMARY KEY,
    ChapterTaskAssignmentId BIGINT NOT NULL,
    SubmissionVersionNo SMALLINT NOT NULL,
    SubmittedByUserId INT NOT NULL,
    SubmissionFileId BIGINT NOT NULL,
    SubmissionStatusId SMALLINT NOT NULL,
    SubmittedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskSubmission_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()),
    Remarks NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskSubmission_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterTaskSubmission_Assignment FOREIGN KEY (ChapterTaskAssignmentId) REFERENCES manga.ChapterTaskAssignment(ChapterTaskAssignmentId),
    CONSTRAINT FK_ChapterTaskSubmission_SubmittedBy FOREIGN KEY (SubmittedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterTaskSubmission_File FOREIGN KEY (SubmissionFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_ChapterTaskSubmission_Status FOREIGN KEY (SubmissionStatusId) REFERENCES manga.SubmissionStatusLookup(SubmissionStatusLookupId),
    CONSTRAINT FK_ChapterTaskSubmission_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterTaskSubmission_Assignment_Version UNIQUE (ChapterTaskAssignmentId, SubmissionVersionNo)
);
GO
CREATE INDEX IX_ChapterTaskSubmission_AssignmentId ON manga.ChapterTaskSubmission (ChapterTaskAssignmentId);
GO
CREATE TABLE manga.ChapterTaskReview (
    ChapterTaskReviewId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterTaskReview PRIMARY KEY,
    ChapterTaskSubmissionId BIGINT NOT NULL,
    ReviewerUserId INT NOT NULL,
    ReviewDecisionId SMALLINT NOT NULL,
    Comments NVARCHAR(MAX) NULL,
    ReviewedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskReview_ReviewedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterTaskReview_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterTaskReview_Submission FOREIGN KEY (ChapterTaskSubmissionId) REFERENCES manga.ChapterTaskSubmission(ChapterTaskSubmissionId),
    CONSTRAINT FK_ChapterTaskReview_Reviewer FOREIGN KEY (ReviewerUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterTaskReview_Decision FOREIGN KEY (ReviewDecisionId) REFERENCES manga.ReviewDecisionLookup(ReviewDecisionLookupId),
    CONSTRAINT FK_ChapterTaskReview_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterTaskReview_Submission UNIQUE (ChapterTaskSubmissionId)
);
GO
CREATE TABLE manga.ChapterSubmission (
    ChapterSubmissionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterSubmission PRIMARY KEY,
    ChapterId BIGINT NOT NULL,
    SubmissionVersionNo SMALLINT NOT NULL,
    SubmittedByUserId INT NOT NULL,
    SubmissionFileId BIGINT NOT NULL,
    SubmissionStatusId SMALLINT NOT NULL,
    SubmittedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterSubmission_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()),
    Remarks NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterSubmission_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterSubmission_Chapter FOREIGN KEY (ChapterId) REFERENCES manga.Chapter(ChapterId),
    CONSTRAINT FK_ChapterSubmission_SubmittedBy FOREIGN KEY (SubmittedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterSubmission_File FOREIGN KEY (SubmissionFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_ChapterSubmission_Status FOREIGN KEY (SubmissionStatusId) REFERENCES manga.SubmissionStatusLookup(SubmissionStatusLookupId),
    CONSTRAINT FK_ChapterSubmission_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterSubmission_Chapter_Version UNIQUE (ChapterId, SubmissionVersionNo)
);
GO
CREATE INDEX IX_ChapterSubmission_ChapterId ON manga.ChapterSubmission (ChapterId);
GO
CREATE TABLE manga.ChapterEditorialReview (
    ChapterEditorialReviewId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterEditorialReview PRIMARY KEY,
    ChapterSubmissionId BIGINT NOT NULL,
    ReviewRoundNo SMALLINT NOT NULL,
    ReviewerUserId INT NOT NULL,
    ReviewDecisionId SMALLINT NOT NULL,
    Comments NVARCHAR(MAX) NULL,
    MarkupFileId BIGINT NULL,
    ReviewedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterEditorialReview_ReviewedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterEditorialReview_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterEditorialReview_Submission FOREIGN KEY (ChapterSubmissionId) REFERENCES manga.ChapterSubmission(ChapterSubmissionId),
    CONSTRAINT FK_ChapterEditorialReview_Reviewer FOREIGN KEY (ReviewerUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterEditorialReview_Decision FOREIGN KEY (ReviewDecisionId) REFERENCES manga.ReviewDecisionLookup(ReviewDecisionLookupId),
    CONSTRAINT FK_ChapterEditorialReview_MarkupFile FOREIGN KEY (MarkupFileId) REFERENCES manga.FileResource(FileResourceId),
    CONSTRAINT FK_ChapterEditorialReview_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterEditorialReview_Submission_Round UNIQUE (ChapterSubmissionId, ReviewRoundNo)
);
GO
CREATE INDEX IX_ChapterEditorialReview_SubmissionId ON manga.ChapterEditorialReview (ChapterSubmissionId);
GO
CREATE TABLE manga.ChapterRelease (
    ChapterReleaseId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChapterRelease PRIMARY KEY,
    ChapterId BIGINT NOT NULL,
    ReleaseVersionNo SMALLINT NOT NULL,
    ScheduledReleaseAtUtc DATETIME2(0) NULL,
    ReleasedAtUtc DATETIME2(0) NULL,
    ReleaseStatusId SMALLINT NOT NULL,
    ReleaseNotes NVARCHAR(MAX) NULL,
    ApprovedByUserId INT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ChapterRelease_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedByUserId INT NULL,
    CONSTRAINT FK_ChapterRelease_Chapter FOREIGN KEY (ChapterId) REFERENCES manga.Chapter(ChapterId),
    CONSTRAINT FK_ChapterRelease_Status FOREIGN KEY (ReleaseStatusId) REFERENCES manga.PublicationStatusLookup(PublicationStatusLookupId),
    CONSTRAINT FK_ChapterRelease_ApprovedBy FOREIGN KEY (ApprovedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_ChapterRelease_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_ChapterRelease_Chapter_Version UNIQUE (ChapterId, ReleaseVersionNo)
);
GO
CREATE INDEX IX_ChapterRelease_ChapterId ON manga.ChapterRelease (ChapterId);
GO
CREATE TABLE manga.SeriesRankingSnapshot (
    SeriesRankingSnapshotId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesRankingSnapshot PRIMARY KEY,
    SeriesId BIGINT NOT NULL,
    RankingPeriodTypeId SMALLINT NOT NULL,
    PeriodStartDate DATE NOT NULL,
    PeriodEndDate DATE NOT NULL,
    RankPosition INT NOT NULL,
    RankingScore DECIMAL(10,2) NOT NULL,
    ReaderVoteCount INT NOT NULL CONSTRAINT DF_SeriesRankingSnapshot_ReaderVoteCount DEFAULT (0),
    CancellationRiskScore DECIMAL(10,2) NULL,
    IsAtRisk BIT NOT NULL CONSTRAINT DF_SeriesRankingSnapshot_IsAtRisk DEFAULT (0),
    GeneratedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesRankingSnapshot_GeneratedAtUtc DEFAULT (SYSUTCDATETIME()),
    GeneratedByUserId INT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SeriesRankingSnapshot_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_SeriesRankingSnapshot_Series FOREIGN KEY (SeriesId) REFERENCES manga.Series(SeriesId),
    CONSTRAINT FK_SeriesRankingSnapshot_PeriodType FOREIGN KEY (RankingPeriodTypeId) REFERENCES manga.RankingPeriodTypeLookup(RankingPeriodTypeLookupId),
    CONSTRAINT FK_SeriesRankingSnapshot_GeneratedBy FOREIGN KEY (GeneratedByUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT UQ_SeriesRankingSnapshot_Series_Period UNIQUE (SeriesId, RankingPeriodTypeId, PeriodStartDate, PeriodEndDate),
    CONSTRAINT CK_SeriesRankingSnapshot_RankPosition CHECK (RankPosition >= 1),
    CONSTRAINT CK_SeriesRankingSnapshot_Period CHECK (PeriodEndDate >= PeriodStartDate)
);
GO
CREATE INDEX IX_SeriesRankingSnapshot_SeriesId_Period ON manga.SeriesRankingSnapshot (SeriesId, RankingPeriodTypeId, PeriodStartDate DESC);
GO
CREATE TABLE manga.Notification (
    NotificationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notification PRIMARY KEY,
    RecipientUserId INT NOT NULL,
    NotificationTypeId SMALLINT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    RelatedEntityType NVARCHAR(50) NULL,
    RelatedEntityId BIGINT NULL,
    IsRead BIT NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT (0),
    ReadAtUtc DATETIME2(0) NULL,
    SentAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Notification_SentAtUtc DEFAULT (SYSUTCDATETIME()),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Notification_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Notification_Recipient FOREIGN KEY (RecipientUserId) REFERENCES auth.Users(user_id),
    CONSTRAINT FK_Notification_Type FOREIGN KEY (NotificationTypeId) REFERENCES manga.NotificationTypeLookup(NotificationTypeLookupId)
);
GO
CREATE INDEX IX_Notification_Recipient_IsRead ON manga.Notification (RecipientUserId, IsRead, SentAtUtc DESC);
GO
-- End of schema
-- Add late-bound foreign keys for history references
ALTER TABLE manga.SeriesStatusHistory
    ADD CONSTRAINT FK_SeriesStatusHistory_RelatedProposal FOREIGN KEY (RelatedProposalId) REFERENCES manga.SeriesProposal(SeriesProposalId);
GO
ALTER TABLE manga.SeriesStatusHistory
    ADD CONSTRAINT FK_SeriesStatusHistory_RelatedChapter FOREIGN KEY (RelatedChapterId) REFERENCES manga.Chapter(ChapterId);
GO
ALTER TABLE manga.SeriesProposalStatusHistory
    ADD CONSTRAINT FK_SeriesProposalStatusHistory_RelatedReview FOREIGN KEY (RelatedReviewId) REFERENCES manga.SeriesEditorialReview(SeriesEditorialReviewId);
GO
ALTER TABLE manga.SeriesProposalStatusHistory
    ADD CONSTRAINT FK_SeriesProposalStatusHistory_RelatedBoardDecision FOREIGN KEY (RelatedBoardDecisionId) REFERENCES manga.SeriesBoardDecision(SeriesBoardDecisionId);
GO
ALTER TABLE manga.ChapterStatusHistory
    ADD CONSTRAINT FK_ChapterStatusHistory_RelatedSubmission FOREIGN KEY (RelatedSubmissionId) REFERENCES manga.ChapterSubmission(ChapterSubmissionId);
GO
ALTER TABLE manga.ChapterStatusHistory
    ADD CONSTRAINT FK_ChapterStatusHistory_RelatedRelease FOREIGN KEY (RelatedReleaseId) REFERENCES manga.ChapterRelease(ChapterReleaseId);
GO








