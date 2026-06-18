using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class AssistantTaskSubmissionService : IAssistantTaskSubmissionService
    {
        private readonly ApplicationDbContext _context;

        public AssistantTaskSubmissionService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AssistantTaskSubmitResultDto> SubmitTaskWorkAsync(
            AssistantTaskSubmitRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_AssistantTask_SubmitWork";
            cmd.CommandType = CommandType.StoredProcedure;

            // Input parameters
            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier)
            {
                Value = request.ActorUserId
            });

            cmd.Parameters.Add(new SqlParameter("@chapter_page_task_id", SqlDbType.UniqueIdentifier)
            {
                Value = request.ChapterPageTaskId
            });

            cmd.Parameters.Add(new SqlParameter("@storage_provider_code", SqlDbType.NVarChar, 50)
            {
                Value = request.StorageProviderCode
            });

            cmd.Parameters.Add(new SqlParameter("@public_id", SqlDbType.NVarChar, 255)
            {
                Value = request.PublicId
            });

            cmd.Parameters.Add(new SqlParameter("@secure_url", SqlDbType.NVarChar, 1000)
            {
                Value = request.SecureUrl
            });

            cmd.Parameters.Add(new SqlParameter("@original_file_name", SqlDbType.NVarChar, 260)
            {
                Value = request.OriginalFileName
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

            cmd.Parameters.Add(new SqlParameter("@version_note", SqlDbType.NVarChar, 500)
            {
                Value = string.IsNullOrWhiteSpace(request.VersionNote) ? DBNull.Value : request.VersionNote
            });

            // Output parameters
            var outputFileResourceId = new SqlParameter("@new_file_resource_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputFileResourceId);

            var outputPageVersionId = new SqlParameter("@new_page_version_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputPageVersionId);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            var fileResourceId = outputFileResourceId.Value == DBNull.Value ? Guid.Empty : (Guid)outputFileResourceId.Value;
            var pageVersionId = outputPageVersionId.Value == DBNull.Value ? Guid.Empty : (Guid)outputPageVersionId.Value;

            // Read result set from procedure
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Assistant task submission failed: no result returned from stored procedure.");
            }

            var result = new AssistantTaskSubmitResultDto(
                ChapterPageTaskId: request.ChapterPageTaskId,
                FileResourceId: fileResourceId,
                CompletedPageVersionId: pageVersionId,
                StatusCode: reader.GetString(reader.GetOrdinal("status_code")),
                VersionNo: reader.GetInt32(reader.GetOrdinal("version_no"))
            );

            // Reload task entity to ensure it's tracked with updated state
            var trackedTask = _context.ChapterPageTasks.Local.FirstOrDefault(t => t.ChapterPageTaskId == request.ChapterPageTaskId);
            if (trackedTask != null)
            {
                await _context.Entry(trackedTask).ReloadAsync(cancellationToken);
            }

            return result;
        }
    }
}
