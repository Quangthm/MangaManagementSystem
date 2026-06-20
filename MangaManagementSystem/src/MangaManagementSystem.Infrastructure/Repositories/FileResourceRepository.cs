using System.Data;
using System.Data.Common;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class FileResourceRepository
        : GenericRepository<FileResource>,
          IFileResourceRepository
    {
        public FileResourceRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<(
            IReadOnlyList<AdminFileResourceListItem> Items,
            int TotalCount)> SearchAdminAsync(
                AdminFileResourceSearchCriteria criteria,
                CancellationToken cancellationToken = default)
        {
            var connection =
                _context.Database.GetDbConnection();

            var shouldClose =
                connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "manga.usp_Admin_FileResource_Search";

                command.CommandType =
                    CommandType.StoredProcedure;

                AddParameter(
                    command,
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier,
                    criteria.ActorUserId);

                AddParameter(
                    command,
                    "@search",
                    SqlDbType.NVarChar,
                    criteria.Search,
                    260);

                AddParameter(
                    command,
                    "@file_purpose_code",
                    SqlDbType.NVarChar,
                    criteria.FilePurposeCode,
                    50);

                AddParameter(
                    command,
                    "@deleted_state",
                    SqlDbType.NVarChar,
                    criteria.DeletedState,
                    20);

                AddParameter(
                    command,
                    "@from_utc",
                    SqlDbType.DateTime2,
                    criteria.FromUtc);

                AddParameter(
                    command,
                    "@to_utc",
                    SqlDbType.DateTime2,
                    criteria.ToUtc);

                AddParameter(
                    command,
                    "@page_number",
                    SqlDbType.Int,
                    criteria.PageNumber);

                AddParameter(
                    command,
                    "@page_size",
                    SqlDbType.Int,
                    criteria.PageSize);

                var items =
                    new List<AdminFileResourceListItem>();

                var totalCount = 0;

                await using var reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken);

                while (await reader.ReadAsync(
                    cancellationToken))
                {
                    items.Add(
                        ReadListItem(reader));
                }

                if (await reader.NextResultAsync(
                        cancellationToken)
                    && await reader.ReadAsync(
                        cancellationToken))
                {
                    totalCount =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "total_count"));
                }

                return (
                    items,
                    totalCount);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<AdminFileResourceDetail?>
            GetAdminByIdAsync(
                Guid actorUserId,
                Guid fileResourceId,
                CancellationToken cancellationToken = default)
        {
            var connection =
                _context.Database.GetDbConnection();

            var shouldClose =
                connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "manga.usp_Admin_FileResource_GetById";

                command.CommandType =
                    CommandType.StoredProcedure;

                AddParameter(
                    command,
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier,
                    actorUserId);

                AddParameter(
                    command,
                    "@file_resource_id",
                    SqlDbType.UniqueIdentifier,
                    fileResourceId);

                await using var reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken);

                if (!await reader.ReadAsync(
                        cancellationToken))
                {
                    return null;
                }

                return ReadDetail(reader);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SoftDeleteAdminAsync(
            Guid actorUserId,
            Guid fileResourceId,
            string deleteReason,
            CancellationToken cancellationToken = default)
        {
            var connection =
                _context.Database.GetDbConnection();

            var shouldClose =
                connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                await using var command =
                    connection.CreateCommand();

                command.CommandText =
                    "manga.usp_Admin_FileResource_SoftDelete";

                command.CommandType =
                    CommandType.StoredProcedure;

                AddParameter(
                    command,
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier,
                    actorUserId);

                AddParameter(
                    command,
                    "@file_resource_id",
                    SqlDbType.UniqueIdentifier,
                    fileResourceId);

                AddParameter(
                    command,
                    "@delete_reason",
                    SqlDbType.NVarChar,
                    deleteReason,
                    500);

                await command.ExecuteNonQueryAsync(
                    cancellationToken);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static AdminFileResourceListItem
            ReadListItem(
                DbDataReader reader)
        {
            return new AdminFileResourceListItem(
                reader.GetGuid(
                    reader.GetOrdinal(
                        "file_resource_id")),
                GetRequiredString(
                    reader,
                    "file_purpose_code"),
                GetRequiredString(
                    reader,
                    "original_file_name"),
                GetRequiredString(
                    reader,
                    "content_type"),
                reader.GetInt64(
                    reader.GetOrdinal(
                        "file_size_bytes")),
                GetRequiredString(
                    reader,
                    "sha256_hash"),
                GetNullableGuid(
                    reader,
                    "uploaded_by_user_id"),
                GetNullableString(
                    reader,
                    "uploaded_by_username"),
                GetNullableString(
                    reader,
                    "uploaded_by_display_name"),
                GetUtcDateTime(
                    reader,
                    "uploaded_at_utc"),
                GetNullableUtcDateTime(
                    reader,
                    "deleted_at_utc"),
                GetNullableGuid(
                    reader,
                    "deleted_by_user_id"),
                GetNullableString(
                    reader,
                    "deleted_by_username"),
                GetNullableString(
                    reader,
                    "deleted_by_display_name"));
        }

        private static AdminFileResourceDetail
            ReadDetail(
                DbDataReader reader)
        {
            return new AdminFileResourceDetail(
                reader.GetGuid(
                    reader.GetOrdinal(
                        "file_resource_id")),
                GetRequiredString(
                    reader,
                    "file_purpose_code"),
                GetRequiredString(
                    reader,
                    "original_file_name"),
                GetRequiredString(
                    reader,
                    "cloudinary_public_id"),
                GetRequiredString(
                    reader,
                    "cloudinary_secure_url"),
                GetRequiredString(
                    reader,
                    "content_type"),
                reader.GetInt64(
                    reader.GetOrdinal(
                        "file_size_bytes")),
                GetRequiredString(
                    reader,
                    "sha256_hash"),
                GetNullableGuid(
                    reader,
                    "uploaded_by_user_id"),
                GetNullableString(
                    reader,
                    "uploaded_by_username"),
                GetNullableString(
                    reader,
                    "uploaded_by_display_name"),
                GetUtcDateTime(
                    reader,
                    "uploaded_at_utc"),
                GetNullableUtcDateTime(
                    reader,
                    "deleted_at_utc"),
                GetNullableGuid(
                    reader,
                    "deleted_by_user_id"),
                GetNullableString(
                    reader,
                    "deleted_by_username"),
                GetNullableString(
                    reader,
                    "deleted_by_display_name"),
                reader.GetInt64(
                    reader.GetOrdinal(
                        "reference_count")));
        }

        private static void AddParameter(
            DbCommand command,
            string name,
            SqlDbType type,
            object? value,
            int? size = null)
        {
            var parameter =
                new SqlParameter(
                    name,
                    type)
                {
                    Value = value ?? DBNull.Value
                };

            if (size.HasValue)
            {
                parameter.Size = size.Value;
            }

            command.Parameters.Add(parameter);
        }

        private static string GetRequiredString(
            DbDataReader reader,
            string columnName)
        {
            var ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? string.Empty
                : reader.GetString(ordinal).Trim();
        }

        private static string? GetNullableString(
            DbDataReader reader,
            string columnName)
        {
            var ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static Guid? GetNullableGuid(
            DbDataReader reader,
            string columnName)
        {
            var ordinal =
                reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetGuid(ordinal);
        }

        private static DateTime GetUtcDateTime(
            DbDataReader reader,
            string columnName)
        {
            var value =
                reader.GetDateTime(
                    reader.GetOrdinal(
                        columnName));

            return DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
        }

        private static DateTime? GetNullableUtcDateTime(
            DbDataReader reader,
            string columnName)
        {
            var ordinal =
                reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return DateTime.SpecifyKind(
                reader.GetDateTime(ordinal),
                DateTimeKind.Utc);
        }
    }
}
