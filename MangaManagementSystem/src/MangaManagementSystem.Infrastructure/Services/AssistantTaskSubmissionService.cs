using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class AssistantTaskSubmissionService : IAssistantTaskSubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssistantTaskSubmissionService> _logger;

        public AssistantTaskSubmissionService(
            ApplicationDbContext context,
            ILogger<AssistantTaskSubmissionService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AssistantTaskSubmitResultDto> SubmitTaskWorkAsync(
            AssistantTaskSubmitRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _logger.LogInformation(
                "SubmitTaskWork started. TaskId={TaskId}, ActorUserId={ActorUserId}",
                request.ChapterPageTaskId, request.ActorUserId);

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            // All steps (derive page, create FileResource, create ChapterPageVersion,
            // call usp_ChapterPageTask_SubmitForReview) run inside one transaction.
            await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                // ----------------------------------------------------------------
                // 1. Derive chapter_page_id from the task's linked regions
                //    and validate all regions belong to the same ChapterPage.
                // ----------------------------------------------------------------
                Guid chapterPageId;
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                    cmd.CommandText = @"
                        SELECT DISTINCT cpv.chapter_page_id
                        FROM manga.ChapterPageTaskRegion tr
                        INNER JOIN manga.PageRegion pr
                            ON pr.page_region_id = tr.page_region_id
                        INNER JOIN manga.ChapterPageVersion cpv
                            ON cpv.chapter_page_version_id = pr.chapter_page_version_id
                        WHERE tr.chapter_page_task_id = @taskId;";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(new SqlParameter("@taskId", SqlDbType.UniqueIdentifier)
                    {
                        Value = request.ChapterPageTaskId
                    });

                    var pageIds = new System.Collections.Generic.List<Guid>();
                    await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            pageIds.Add(reader.GetGuid(0));
                        }
                    }

                    if (pageIds.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Task must have at least one linked page region.");
                    }

                    if (pageIds.Count > 1)
                    {
                        throw new InvalidOperationException(
                            "All task page regions must belong to the same ChapterPage.");
                    }

                    chapterPageId = pageIds[0];
                }

                _logger.LogInformation(
                    "Derived ChapterPageId={ChapterPageId} for TaskId={TaskId}",
                    chapterPageId, request.ChapterPageTaskId);

                // ----------------------------------------------------------------
                // 2. Create FileResource via manga.usp_FileResource_Create
                // ----------------------------------------------------------------
                Guid newFileResourceId;
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                    cmd.CommandText = "manga.usp_FileResource_Create";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@file_purpose_code", SqlDbType.NVarChar, 50)
                    {
                        Value = "CHAPTER_PAGE_VERSION"
                    });
                    cmd.Parameters.Add(new SqlParameter("@original_file_name", SqlDbType.NVarChar, 260)
                    {
                        Value = request.OriginalFileName
                    });
                    cmd.Parameters.Add(new SqlParameter("@cloudinary_public_id", SqlDbType.NVarChar, 255)
                    {
                        Value = request.PublicId
                    });
                    cmd.Parameters.Add(new SqlParameter("@cloudinary_secure_url", SqlDbType.NVarChar, 1000)
                    {
                        Value = request.SecureUrl
                    });
                    cmd.Parameters.Add(new SqlParameter("@content_type", SqlDbType.NVarChar, 100)
                    {
                        Value = request.ContentType
                    });
                    cmd.Parameters.Add(new SqlParameter("@file_size_bytes", SqlDbType.BigInt)
                    {
                        Value = request.FileSizeBytes
                    });
                    cmd.Parameters.Add(new SqlParameter("@sha256_hash", SqlDbType.Char, 64)
                    {
                        Value = request.Sha256Hash
                    });
                    cmd.Parameters.Add(new SqlParameter("@uploaded_by_user_id", SqlDbType.UniqueIdentifier)
                    {
                        Value = request.ActorUserId
                    });

                    var outputFileId = new SqlParameter("@file_resource_id", SqlDbType.UniqueIdentifier)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputFileId);

                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                    newFileResourceId = outputFileId.Value == DBNull.Value
                        ? throw new InvalidOperationException("usp_FileResource_Create did not return a file_resource_id.")
                        : (Guid)outputFileId.Value;
                }

                _logger.LogInformation(
                    "Created FileResource. FileResourceId={FileResourceId}, TaskId={TaskId}",
                    newFileResourceId, request.ChapterPageTaskId);

                // ----------------------------------------------------------------
                // 3. Create new ChapterPageVersion
                //    a. Flip previous current version to not-current
                //    b. Compute next version_no
                //    c. Insert new version row and capture the generated PK
                // ----------------------------------------------------------------
                Guid newPageVersionId;
                short newVersionNo;
                {
                    // 3a. Unset is_current_version on existing current version
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                        cmd.CommandText = @"
                            UPDATE manga.ChapterPageVersion
                            SET is_current_version = 0
                            WHERE chapter_page_id = @cpId
                              AND is_current_version = 1;";
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add(new SqlParameter("@cpId", SqlDbType.UniqueIdentifier)
                        {
                            Value = chapterPageId
                        });
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    // 3b. Compute next version_no
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                        cmd.CommandText = @"
                            SELECT CONVERT(SMALLINT, ISNULL(MAX(version_no), 0) + 1)
                            FROM manga.ChapterPageVersion WITH (UPDLOCK, HOLDLOCK)
                            WHERE chapter_page_id = @cpId;";
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add(new SqlParameter("@cpId", SqlDbType.UniqueIdentifier)
                        {
                            Value = chapterPageId
                        });

                        var result = await cmd.ExecuteScalarAsync(cancellationToken);
                        newVersionNo = Convert.ToInt16(result);
                    }

                    // 3c. Insert new ChapterPageVersion and capture generated PK
                    await using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                        cmd.CommandText = @"
                            DECLARE @created TABLE (chapter_page_version_id UNIQUEIDENTIFIER);

                            INSERT INTO manga.ChapterPageVersion
                            (
                                chapter_page_id,
                                version_no,
                                page_file_id,
                                version_note,
                                is_current_version
                            )
                            OUTPUT inserted.chapter_page_version_id
                            INTO @created(chapter_page_version_id)
                            VALUES
                            (
                                @cpId,
                                @versionNo,
                                @fileId,
                                @versionNote,
                                1
                            );

                            SELECT chapter_page_version_id FROM @created;";
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add(new SqlParameter("@cpId", SqlDbType.UniqueIdentifier)
                        {
                            Value = chapterPageId
                        });
                        cmd.Parameters.Add(new SqlParameter("@versionNo", SqlDbType.SmallInt)
                        {
                            Value = newVersionNo
                        });
                        cmd.Parameters.Add(new SqlParameter("@fileId", SqlDbType.UniqueIdentifier)
                        {
                            Value = newFileResourceId
                        });
                        cmd.Parameters.Add(new SqlParameter("@versionNote", SqlDbType.NVarChar, 500)
                        {
                            Value = string.IsNullOrWhiteSpace(request.VersionNote)
                                ? (object)DBNull.Value
                                : request.VersionNote
                        });

                        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                        newPageVersionId = scalar == null || scalar == DBNull.Value
                            ? throw new InvalidOperationException("Failed to create ChapterPageVersion row.")
                            : (Guid)scalar;
                    }
                }

                _logger.LogInformation(
                    "Created ChapterPageVersion. PageVersionId={PageVersionId}, VersionNo={VersionNo}, ChapterPageId={ChapterPageId}, TaskId={TaskId}",
                    newPageVersionId, newVersionNo, chapterPageId, request.ChapterPageTaskId);

                // ----------------------------------------------------------------
                // 4. Call manga.usp_ChapterPageTask_SubmitForReview
                // ----------------------------------------------------------------
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.Transaction = (System.Data.Common.DbTransaction)transaction;
                    cmd.CommandText = "manga.usp_ChapterPageTask_SubmitForReview";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier)
                    {
                        Value = request.ActorUserId
                    });
                    cmd.Parameters.Add(new SqlParameter("@chapter_page_task_id", SqlDbType.UniqueIdentifier)
                    {
                        Value = request.ChapterPageTaskId
                    });
                    cmd.Parameters.Add(new SqlParameter("@completed_page_version_id", SqlDbType.UniqueIdentifier)
                    {
                        Value = newPageVersionId
                    });
                    cmd.Parameters.Add(new SqlParameter("@submission_note", SqlDbType.NVarChar, -1)
                    {
                        Value = string.IsNullOrWhiteSpace(request.VersionNote)
                            ? (object)DBNull.Value
                            : request.VersionNote
                    });

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Called usp_ChapterPageTask_SubmitForReview successfully. TaskId={TaskId}, PageVersionId={PageVersionId}, ActorUserId={ActorUserId}",
                    request.ChapterPageTaskId, newPageVersionId, request.ActorUserId);

                // ----------------------------------------------------------------
                // 5. Commit transaction
                // ----------------------------------------------------------------
                await transaction.CommitAsync(cancellationToken);

                return new AssistantTaskSubmitResultDto(
                    ChapterPageTaskId: request.ChapterPageTaskId,
                    FileResourceId: newFileResourceId,
                    CompletedPageVersionId: newPageVersionId,
                    StatusCode: "UNDER_REVIEW",
                    VersionNo: newVersionNo
                );
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error during SubmitTaskWork. TaskId={TaskId}, ActorUserId={ActorUserId}, SqlErrorNumber={SqlErrorNumber}, SqlMessage={SqlMessage}",
                    request.ChapterPageTaskId, request.ActorUserId, ex.Number, ex.Message);

                try { await transaction.RollbackAsync(cancellationToken); }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "Rollback failed after SQL error for TaskId={TaskId}.", request.ChapterPageTaskId);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during SubmitTaskWork. TaskId={TaskId}, ActorUserId={ActorUserId}",
                    request.ChapterPageTaskId, request.ActorUserId);

                try { await transaction.RollbackAsync(cancellationToken); }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "Rollback failed after error for TaskId={TaskId}.", request.ChapterPageTaskId);
                }

                throw;
            }
        }
    }
}
