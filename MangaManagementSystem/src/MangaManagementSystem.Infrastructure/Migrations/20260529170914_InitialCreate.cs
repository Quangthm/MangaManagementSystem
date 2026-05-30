using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "manga");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "auth",
                columns: table => new
                {
                    RoleId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                schema: "audit",
                columns: table => new
                {
                    AuditEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorRoleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.AuditEventId);
                    table.CheckConstraint("CK_AuditEvent_DetailJson", "[DetailJson] IS NULL OR ISJSON([DetailJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Chapter",
                schema: "manga",
                columns: table => new
                {
                    ChapterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<long>(type: "bigint", nullable: false),
                    ChapterNumberLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChapterTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "DRAFT"),
                    PlannedReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapter", x => x.ChapterId);
                    table.CheckConstraint("CK_Chapter_StatusCode", "[StatusCode] IN ('DRAFT','RELEASED','ARCHIVED')");
                });

            migrationBuilder.CreateTable(
                name: "ChapterPage",
                schema: "manga",
                columns: table => new
                {
                    ChapterPageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<long>(type: "bigint", nullable: false),
                    PageNo = table.Column<int>(type: "int", nullable: false),
                    PageNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPage", x => x.ChapterPageId);
                    table.ForeignKey(
                        name: "FK_ChapterPage_Chapter_ChapterId",
                        column: x => x.ChapterId,
                        principalSchema: "manga",
                        principalTable: "Chapter",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterEditorialReview",
                schema: "manga",
                columns: table => new
                {
                    ChapterEditorialReviewId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewerUserId = table.Column<int>(type: "int", nullable: false),
                    DecisionCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkupFileId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterEditorialReview", x => x.ChapterEditorialReviewId);
                    table.CheckConstraint("CK_ChapterEditorialReview_DecisionCode", "[DecisionCode] IN ('APPROVED','REVISION_REQUESTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_ChapterEditorialReview_Chapter_ChapterId",
                        column: x => x.ChapterId,
                        principalSchema: "manga",
                        principalTable: "Chapter",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterPageAnnotation",
                schema: "manga",
                columns: table => new
                {
                    ChapterPageAnnotationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageRegionId = table.Column<long>(type: "bigint", nullable: false),
                    IssueTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnnotatedByUserId = table.Column<int>(type: "int", nullable: false),
                    AnnotationText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPageAnnotation", x => x.ChapterPageAnnotationId);
                    table.CheckConstraint("CK_ChapterPageAnnotation_IssueTypeCode", "[IssueTypeCode] IN ('TRANSLATION_ERROR','ART_ERROR','LAYOUT_ERROR','OTHER')");
                });

            migrationBuilder.CreateTable(
                name: "ChapterPageTask",
                schema: "manga",
                columns: table => new
                {
                    ChapterPageTaskId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: false),
                    TypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "ASSIGNED"),
                    PriorityLevel = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedPageVersionId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPageTask", x => x.ChapterPageTaskId);
                    table.CheckConstraint("CK_ChapterPageTask_PriorityLevel", "[PriorityLevel] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_ChapterPageTask_StatusCode", "[StatusCode] IN ('ASSIGNED','IN_PROGRESS','COMPLETED','CANCELLED')");
                });

            migrationBuilder.CreateTable(
                name: "ChapterPageTaskRegion",
                schema: "manga",
                columns: table => new
                {
                    ChapterPageTaskRegionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterPageTaskId = table.Column<long>(type: "bigint", nullable: false),
                    PageRegionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPageTaskRegion", x => x.ChapterPageTaskRegionId);
                    table.ForeignKey(
                        name: "FK_ChapterPageTaskRegion_ChapterPageTask_ChapterPageTaskId",
                        column: x => x.ChapterPageTaskId,
                        principalSchema: "manga",
                        principalTable: "ChapterPageTask",
                        principalColumn: "ChapterPageTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterPageVersion",
                schema: "manga",
                columns: table => new
                {
                    ChapterPageVersionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterPageId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNo = table.Column<short>(type: "smallint", nullable: false),
                    PageFileId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPageVersion", x => x.ChapterPageVersionId);
                    table.ForeignKey(
                        name: "FK_ChapterPageVersion_ChapterPage_ChapterPageId",
                        column: x => x.ChapterPageId,
                        principalSchema: "manga",
                        principalTable: "ChapterPage",
                        principalColumn: "ChapterPageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PageRegion",
                schema: "manga",
                columns: table => new
                {
                    PageRegionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterPageVersionId = table.Column<long>(type: "bigint", nullable: false),
                    TypeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "OTHER"),
                    RegionLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    X = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "MANUAL"),
                    OriginalText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRegion", x => x.PageRegionId);
                    table.CheckConstraint("CK_PageRegion_SourceType", "[SourceType] IN ('AI','MANUAL')");
                    table.CheckConstraint("CK_PageRegion_TypeCode", "[TypeCode] IN ('PANEL','SPEECH_BUBBLE','CHARACTER','SFX_TEXT','BACKGROUND','OTHER')");
                    table.CheckConstraint("CK_PageRegion_Width_Height", "[Width] > 0 AND [Height] > 0");
                    table.ForeignKey(
                        name: "FK_PageRegion_ChapterPageVersion_ChapterPageVersionId",
                        column: x => x.ChapterPageVersionId,
                        principalSchema: "manga",
                        principalTable: "ChapterPageVersion",
                        principalColumn: "ChapterPageVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterReaderVoteSnapshot",
                schema: "manga",
                columns: table => new
                {
                    ChapterReaderVoteSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<long>(type: "bigint", nullable: false),
                    ReaderVoteCount = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EnteredByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterReaderVoteSnapshot", x => x.ChapterReaderVoteSnapshotId);
                    table.CheckConstraint("CK_ChapterReaderVoteSnapshot_AverageRating", "[AverageRating] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_ChapterReaderVoteSnapshot_Chapter_ChapterId",
                        column: x => x.ChapterId,
                        principalSchema: "manga",
                        principalTable: "Chapter",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileResource",
                schema: "manga",
                columns: table => new
                {
                    FileResourceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilePurposeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CloudinarySecureUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileResource", x => x.FileResourceId);
                    table.CheckConstraint("CK_FileResource_PurposeCode", "[FilePurposeCode] IN ('COVER','PORTFOLIO','PAGE','MARKUP','OTHER')");
                });

            migrationBuilder.CreateTable(
                name: "Series",
                schema: "manga",
                columns: table => new
                {
                    SeriesId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Genre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoverFileId = table.Column<long>(type: "bigint", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "PROPOSAL_DRAFT"),
                    ContentLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "ja"),
                    SourceSeriesId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PublicationFrequencyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.SeriesId);
                    table.CheckConstraint("CK_Series_ContentLanguageCode", "[ContentLanguageCode] IN ('ja','en','vi')");
                    table.CheckConstraint("CK_Series_PublicationFrequencyCode", "[PublicationFrequencyCode] IS NULL OR [PublicationFrequencyCode] IN ('WEEKLY','MONTHLY','IRREGULAR')");
                    table.CheckConstraint("CK_Series_StatusCode", "[StatusCode] IN ('PROPOSAL_DRAFT','ACTIVE','CANCELLED','ARCHIVED')");
                    table.ForeignKey(
                        name: "FK_Series_FileResource_CoverFileId",
                        column: x => x.CoverFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_Series_Series_SourceSeriesId",
                        column: x => x.SourceSeriesId,
                        principalSchema: "manga",
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<short>(type: "smallint", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AvatarFileId = table.Column<long>(type: "bigint", nullable: true),
                    PortfolioFileId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING_APPROVAL"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Status", "[Status] IN ('PENDING_APPROVAL','ACTIVE','DISABLED')");
                    table.ForeignKey(
                        name: "FK_Users_FileResource_AvatarFileId",
                        column: x => x.AvatarFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_Users_FileResource_PortfolioFileId",
                        column: x => x.PortfolioFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesBoardPoll",
                schema: "manga",
                columns: table => new
                {
                    SeriesBoardPollId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<long>(type: "bigint", nullable: false),
                    PollTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PollStatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "OPEN"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesBoardPoll", x => x.SeriesBoardPollId);
                    table.ForeignKey(
                        name: "FK_SeriesBoardPoll_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "manga",
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "manga",
                columns: table => new
                {
                    NotificationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    NotificationTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "SYSTEM_MESSAGE"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notification_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesContributor",
                schema: "manga",
                columns: table => new
                {
                    SeriesContributorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesContributor", x => x.SeriesContributorId);
                    table.CheckConstraint("CK_SeriesContributor_EndDate", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_SeriesContributor_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "manga",
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesContributor_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesProposal",
                schema: "manga",
                columns: table => new
                {
                    SeriesProposalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<long>(type: "bigint", nullable: false),
                    ProposalVersionNo = table.Column<short>(type: "smallint", nullable: false),
                    ProposalTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SynopsisSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GenreSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposalFileId = table.Column<long>(type: "bigint", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "UNDER_EDITORIAL_REVIEW"),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    MarkupFileId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesProposal", x => x.SeriesProposalId);
                    table.ForeignKey(
                        name: "FK_SeriesProposal_FileResource_MarkupFileId",
                        column: x => x.MarkupFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_SeriesProposal_FileResource_ProposalFileId",
                        column: x => x.ProposalFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_SeriesProposal_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "manga",
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesProposal_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_SeriesProposal_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SeriesRankingSnapshot",
                schema: "manga",
                columns: table => new
                {
                    SeriesRankingSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesId = table.Column<long>(type: "bigint", nullable: false),
                    RankingPeriodTypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RankPosition = table.Column<int>(type: "int", nullable: false),
                    RankingScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesRankingSnapshot", x => x.SeriesRankingSnapshotId);
                    table.CheckConstraint("CK_SeriesRankingSnapshot_RankingPeriodTypeCode", "[RankingPeriodTypeCode] IN ('WEEKLY','MONTHLY','SEASONAL')");
                    table.CheckConstraint("CK_SeriesRankingSnapshot_RankPosition", "[RankPosition] >= 1");
                    table.ForeignKey(
                        name: "FK_SeriesRankingSnapshot_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "manga",
                        principalTable: "Series",
                        principalColumn: "SeriesId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesRankingSnapshot_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserRegistrationRequest",
                schema: "auth",
                columns: table => new
                {
                    RegistrationRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestedRoleId = table.Column<short>(type: "smallint", nullable: false),
                    PortfolioFileId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRegistrationRequest", x => x.RegistrationRequestId);
                    table.CheckConstraint("CK_UserRegistrationRequest_Status", "[Status] IN ('PENDING','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_UserRegistrationRequest_FileResource_PortfolioFileId",
                        column: x => x.PortfolioFileId,
                        principalSchema: "manga",
                        principalTable: "FileResource",
                        principalColumn: "FileResourceId");
                    table.ForeignKey(
                        name: "FK_UserRegistrationRequest_Roles_RequestedRoleId",
                        column: x => x.RequestedRoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK_UserRegistrationRequest_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserRegistrationRequest_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SeriesBoardVote",
                schema: "manga",
                columns: table => new
                {
                    SeriesBoardVoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeriesBoardPollId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChoiceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesBoardVote", x => x.SeriesBoardVoteId);
                    table.CheckConstraint("CK_SeriesBoardVote_ChoiceCode", "[ChoiceCode] IN ('APPROVE','REJECT','ABSTAIN')");
                    table.ForeignKey(
                        name: "FK_SeriesBoardVote_SeriesBoardPoll_SeriesBoardPollId",
                        column: x => x.SeriesBoardPollId,
                        principalSchema: "manga",
                        principalTable: "SeriesBoardPoll",
                        principalColumn: "SeriesBoardPollId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesBoardVote_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_actor_time",
                schema: "audit",
                table: "AuditEvent",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_entity_time",
                schema: "audit",
                table: "AuditEvent",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_chapter_series_id",
                schema: "manga",
                table: "Chapter",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapter_SeriesId_ChapterNumberLabel",
                schema: "manga",
                table: "Chapter",
                columns: new[] { "SeriesId", "ChapterNumberLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chapter_status_code",
                schema: "manga",
                table: "Chapter",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterEditorialReview_ChapterId",
                schema: "manga",
                table: "ChapterEditorialReview",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterEditorialReview_MarkupFileId",
                schema: "manga",
                table: "ChapterEditorialReview",
                column: "MarkupFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterEditorialReview_ReviewerUserId",
                schema: "manga",
                table: "ChapterEditorialReview",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "ix_chapter_page_chapter_id",
                schema: "manga",
                table: "ChapterPage",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "ux_chapter_page_active_page_no",
                schema: "manga",
                table: "ChapterPage",
                columns: new[] { "ChapterId", "PageNo" },
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageAnnotation_AnnotatedByUserId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "AnnotatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageAnnotation_PageRegionId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "PageRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageAnnotation_ResolvedByUserId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageTask_AssignedToUserId",
                schema: "manga",
                table: "ChapterPageTask",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageTask_CompletedPageVersionId",
                schema: "manga",
                table: "ChapterPageTask",
                column: "CompletedPageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageTaskRegion_ChapterPageTaskId",
                schema: "manga",
                table: "ChapterPageTaskRegion",
                column: "ChapterPageTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageTaskRegion_PageRegionId",
                schema: "manga",
                table: "ChapterPageTaskRegion",
                column: "PageRegionId");

            migrationBuilder.CreateIndex(
                name: "ix_chapter_page_chapter_id",
                schema: "manga",
                table: "ChapterPageVersion",
                column: "ChapterPageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageVersion_ChapterPageId_VersionNo",
                schema: "manga",
                table: "ChapterPageVersion",
                columns: new[] { "ChapterPageId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPageVersion_PageFileId",
                schema: "manga",
                table: "ChapterPageVersion",
                column: "PageFileId");

            migrationBuilder.CreateIndex(
                name: "ux_chapter_page_version_current",
                schema: "manga",
                table: "ChapterPageVersion",
                column: "IsCurrentVersion",
                unique: true,
                filter: "[IsCurrentVersion] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReaderVoteSnapshot_ChapterId",
                schema: "manga",
                table: "ChapterReaderVoteSnapshot",
                column: "ChapterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReaderVoteSnapshot_EnteredByUserId",
                schema: "manga",
                table: "ChapterReaderVoteSnapshot",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_file_resource_active_by_purpose",
                schema: "manga",
                table: "FileResource",
                columns: new[] { "FilePurposeCode", "DeletedAtUtc" },
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_file_resource_purpose_code",
                schema: "manga",
                table: "FileResource",
                column: "FilePurposeCode");

            migrationBuilder.CreateIndex(
                name: "ix_file_resource_uploaded_by",
                schema: "manga",
                table: "FileResource",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_notification_recipient_read_created",
                schema: "manga",
                table: "Notification",
                columns: new[] { "RecipientUserId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_related_entity",
                schema: "manga",
                table: "Notification",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" },
                filter: "[RelatedEntityType] IS NOT NULL AND [RelatedEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_unread_recipient",
                schema: "manga",
                table: "Notification",
                column: "RecipientUserId",
                filter: "[ReadAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_page_region_type",
                schema: "manga",
                table: "PageRegion",
                column: "TypeCode");

            migrationBuilder.CreateIndex(
                name: "ix_page_region_version_type",
                schema: "manga",
                table: "PageRegion",
                columns: new[] { "ChapterPageVersionId", "TypeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                schema: "auth",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_CoverFileId",
                schema: "manga",
                table: "Series",
                column: "CoverFileId");

            migrationBuilder.CreateIndex(
                name: "ix_series_current_status_code",
                schema: "manga",
                table: "Series",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_Series_SeriesCode",
                schema: "manga",
                table: "Series",
                column: "SeriesCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_Slug",
                schema: "manga",
                table: "Series",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Series_SourceSeriesId",
                schema: "manga",
                table: "Series",
                column: "SourceSeriesId");

            migrationBuilder.CreateIndex(
                name: "ux_series_board_poll_open_type",
                schema: "manga",
                table: "SeriesBoardPoll",
                columns: new[] { "SeriesId", "PollTypeCode" },
                unique: true,
                filter: "[PollStatusCode]='OPEN'");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesBoardVote_SeriesBoardPollId_UserId",
                schema: "manga",
                table: "SeriesBoardVote",
                columns: new[] { "SeriesBoardPollId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeriesBoardVote_UserId",
                schema: "manga",
                table: "SeriesBoardVote",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_series_contributor_series_active",
                schema: "manga",
                table: "SeriesContributor",
                column: "SeriesId",
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_series_contributor_user_active",
                schema: "manga",
                table: "SeriesContributor",
                column: "UserId",
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesContributor_SeriesId_UserId_StartDate",
                schema: "manga",
                table: "SeriesContributor",
                columns: new[] { "SeriesId", "UserId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_series_contributor_active_role",
                schema: "manga",
                table: "SeriesContributor",
                columns: new[] { "SeriesId", "UserId" },
                unique: true,
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_series_proposal_reviewed_by",
                schema: "manga",
                table: "SeriesProposal",
                column: "ReviewedByUserId",
                filter: "[ReviewedByUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_series_proposal_series_version",
                schema: "manga",
                table: "SeriesProposal",
                columns: new[] { "SeriesId", "ProposalVersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_proposal_status_submitted",
                schema: "manga",
                table: "SeriesProposal",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "ix_series_proposal_submitted_by",
                schema: "manga",
                table: "SeriesProposal",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesProposal_MarkupFileId",
                schema: "manga",
                table: "SeriesProposal",
                column: "MarkupFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesProposal_ProposalFileId",
                schema: "manga",
                table: "SeriesProposal",
                column: "ProposalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRankingSnapshot_GeneratedByUserId",
                schema: "manga",
                table: "SeriesRankingSnapshot",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRankingSnapshot_SeriesId_RankingPeriodTypeCode_PeriodStartDate",
                schema: "manga",
                table: "SeriesRankingSnapshot",
                columns: new[] { "SeriesId", "RankingPeriodTypeCode", "PeriodStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_registration_status",
                schema: "auth",
                table: "UserRegistrationRequest",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequest_PortfolioFileId",
                schema: "auth",
                table: "UserRegistrationRequest",
                column: "PortfolioFileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequest_RequestedRoleId",
                schema: "auth",
                table: "UserRegistrationRequest",
                column: "RequestedRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequest_ReviewedByUserId",
                schema: "auth",
                table: "UserRegistrationRequest",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_user_registration_request_pending",
                schema: "auth",
                table: "UserRegistrationRequest",
                column: "UserId",
                unique: true,
                filter: "[Status] = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AvatarFileId",
                schema: "auth",
                table: "Users",
                column: "AvatarFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PortfolioFileId",
                schema: "auth",
                table: "Users",
                column: "PortfolioFileId");

            migrationBuilder.CreateIndex(
                name: "ix_users_role_status",
                schema: "auth",
                table: "Users",
                columns: new[] { "RoleId", "Status" })
                .Annotation("SqlServer:Include", new[] { "Username", "Email" });

            migrationBuilder.CreateIndex(
                name: "ix_users_status_created",
                schema: "auth",
                table: "Users",
                columns: new[] { "Status", "CreatedAt" })
                .Annotation("SqlServer:Include", new[] { "Username", "Email", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                schema: "auth",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvent_Users_ActorUserId",
                schema: "audit",
                table: "AuditEvent",
                column: "ActorUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapter_Series_SeriesId",
                schema: "manga",
                table: "Chapter",
                column: "SeriesId",
                principalSchema: "manga",
                principalTable: "Series",
                principalColumn: "SeriesId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterEditorialReview_FileResource_MarkupFileId",
                schema: "manga",
                table: "ChapterEditorialReview",
                column: "MarkupFileId",
                principalSchema: "manga",
                principalTable: "FileResource",
                principalColumn: "FileResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterEditorialReview_Users_ReviewerUserId",
                schema: "manga",
                table: "ChapterEditorialReview",
                column: "ReviewerUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageAnnotation_PageRegion_PageRegionId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "PageRegionId",
                principalSchema: "manga",
                principalTable: "PageRegion",
                principalColumn: "PageRegionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageAnnotation_Users_AnnotatedByUserId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "AnnotatedByUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageAnnotation_Users_ResolvedByUserId",
                schema: "manga",
                table: "ChapterPageAnnotation",
                column: "ResolvedByUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageTask_ChapterPageVersion_CompletedPageVersionId",
                schema: "manga",
                table: "ChapterPageTask",
                column: "CompletedPageVersionId",
                principalSchema: "manga",
                principalTable: "ChapterPageVersion",
                principalColumn: "ChapterPageVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageTask_Users_AssignedToUserId",
                schema: "manga",
                table: "ChapterPageTask",
                column: "AssignedToUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageTaskRegion_PageRegion_PageRegionId",
                schema: "manga",
                table: "ChapterPageTaskRegion",
                column: "PageRegionId",
                principalSchema: "manga",
                principalTable: "PageRegion",
                principalColumn: "PageRegionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterPageVersion_FileResource_PageFileId",
                schema: "manga",
                table: "ChapterPageVersion",
                column: "PageFileId",
                principalSchema: "manga",
                principalTable: "FileResource",
                principalColumn: "FileResourceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterReaderVoteSnapshot_Users_EnteredByUserId",
                schema: "manga",
                table: "ChapterReaderVoteSnapshot",
                column: "EnteredByUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileResource_Users_UploadedByUserId",
                schema: "manga",
                table: "FileResource",
                column: "UploadedByUserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileResource_Users_UploadedByUserId",
                schema: "manga",
                table: "FileResource");

            migrationBuilder.DropTable(
                name: "AuditEvent",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ChapterEditorialReview",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "ChapterPageAnnotation",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "ChapterPageTaskRegion",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "ChapterReaderVoteSnapshot",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "SeriesBoardVote",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "SeriesContributor",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "SeriesProposal",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "SeriesRankingSnapshot",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "UserRegistrationRequest",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "ChapterPageTask",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "PageRegion",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "SeriesBoardPoll",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "ChapterPageVersion",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "ChapterPage",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "Chapter",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "Series",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "FileResource",
                schema: "manga");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "auth");
        }
    }
}
