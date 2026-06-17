using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class SeriesRepository : GenericRepository<Series>, ISeriesRepository
    {
        // Custom error numbers raised by manga.usp_Series_Create (see SQL skill guide range 57000-59999).
        private const int ErrNotActiveMangaka = 57301;
        private const int ErrIncompleteCoverMetadata = 57302;

        // SQL Server unique-constraint violation numbers (duplicate slug -> uq_series_slug).
        private const int ErrDuplicateKey = 2627;
        private const int ErrUniqueIndex = 2601;

        // Constraint/foreign-key/check violation numbers.
        private const int ErrCheckConstraint = 547;

        private readonly ApplicationDbContext _context;

        public SeriesRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Series?> GetSeriesWithChaptersAsync(Guid seriesId)
        {
            return await _context.Series
                .Include(s => s.Chapters)
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId);
        }

        /// <summary>
        /// Returns all series with CoverFile eagerly loaded so the dashboard can render
        /// cover thumbnails in a single query. Display-only — not for update workflows.
        /// </summary>
        public async Task<IReadOnlyList<Series>> GetAllWithCoverAsync()
        {
            return await _context.Series
                .Include(s => s.CoverFile)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(Guid newSeriesId, Guid? coverFileResourceId)> CreateSeriesDraftViaProcAsync(
            Guid actorUserId,
            string title,
            string slug,
            string synopsis,
            string genre,
            string contentLanguageCode,
            Guid? sourceSeriesId,
            string? publicationFrequencyCode,
            string? coverOriginalFileName,
            string? coverCloudinaryPublicId,
            string? coverCloudinarySecureUrl,
            string? coverContentType,
            long? coverFileSizeBytes,
            string? coverSha256Hash,
            CancellationToken cancellationToken = default)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_Series_Create";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new SqlParameter("@title", SqlDbType.NVarChar, 200) { Value = title });
            cmd.Parameters.Add(new SqlParameter("@slug", SqlDbType.NVarChar, 220) { Value = slug });
            cmd.Parameters.Add(new SqlParameter("@synopsis", SqlDbType.NVarChar, -1) { Value = synopsis });
            cmd.Parameters.Add(new SqlParameter("@genre", SqlDbType.NVarChar, 100) { Value = genre });
            cmd.Parameters.Add(new SqlParameter("@content_language_code", SqlDbType.NVarChar, 10) { Value = contentLanguageCode });
            cmd.Parameters.Add(new SqlParameter("@source_series_id", SqlDbType.UniqueIdentifier) { Value = (object?)sourceSeriesId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@publication_frequency_code", SqlDbType.NVarChar, 50) { Value = (object?)publicationFrequencyCode ?? DBNull.Value });

            cmd.Parameters.Add(new SqlParameter("@cover_original_file_name", SqlDbType.NVarChar, 260) { Value = (object?)coverOriginalFileName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_cloudinary_public_id", SqlDbType.NVarChar, 255) { Value = (object?)coverCloudinaryPublicId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_cloudinary_secure_url", SqlDbType.NVarChar, 1000) { Value = (object?)coverCloudinarySecureUrl ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_content_type", SqlDbType.NVarChar, 100) { Value = (object?)coverContentType ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_file_size_bytes", SqlDbType.BigInt) { Value = (object?)coverFileSizeBytes ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_sha256_hash", SqlDbType.Char, 64) { Value = (object?)coverSha256Hash ?? DBNull.Value });

            var outSeriesId = new SqlParameter("@new_series_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outSeriesId);

            var outCoverFileResourceId = new SqlParameter("@cover_file_resource_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outCoverFileResourceId);

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw MapSqlException(ex);
            }

            Guid newSeriesId = outSeriesId.Value == DBNull.Value ? Guid.Empty : (Guid)outSeriesId.Value;
            Guid? coverFileResourceId = outCoverFileResourceId.Value == DBNull.Value ? (Guid?)null : (Guid)outCoverFileResourceId.Value;

            return (newSeriesId, coverFileResourceId);
        }

        /// <summary>
        /// Translates known SQL errors raised by manga.usp_Series_Create into friendly,
        /// user-safe <see cref="InvalidOperationException"/> messages. The API layer maps
        /// these to safe HTTP responses; raw SQL text is never surfaced to callers.
        /// </summary>
        private static InvalidOperationException MapSqlException(SqlException ex)
        {
            switch (ex.Number)
            {
                case ErrNotActiveMangaka:
                    return new InvalidOperationException("Only an active Mangaka can create a series draft.", ex);

                case ErrIncompleteCoverMetadata:
                    return new InvalidOperationException("The cover file information is incomplete. Please try uploading the cover again.", ex);

                case ErrDuplicateKey:
                case ErrUniqueIndex:
                    return new InvalidOperationException("A series with this title or slug already exists. Please choose a different title.", ex);

                case ErrCheckConstraint:
                    return new InvalidOperationException("Some of the series details are not valid. Please review the language, frequency, and source series values.", ex);

                default:
                    return new InvalidOperationException("We could not create the series draft right now. Please try again.", ex);
            }
        }
    }
}
