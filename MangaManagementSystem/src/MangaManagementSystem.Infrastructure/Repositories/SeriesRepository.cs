using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.ReadModels;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
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

        public SeriesRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Series?> GetSeriesWithChaptersAsync(Guid seriesId)
        {
            return await _context.Series
                .Include(s => s.Chapters)
                .Include(s => s.Genres)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.SeriesId == seriesId);
        }

        /// <summary>
        /// Returns only series where the specified actor is an active Mangaka contributor,
        /// with CoverFile eagerly loaded for the dashboard card thumbnails.
        /// Read-only EF query; no stored procedure required.
        /// </summary>
        public async Task<IReadOnlyList<Series>> GetByActiveContributorWithCoverAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Series
                .AsNoTracking()
                .Include(s => s.CoverFile)
                .Include(s => s.Genres)
                .Include(s => s.Tags)
                .Where(s => _context.SeriesContributors.Any(sc =>
                    sc.SeriesId == s.SeriesId &&
                    sc.UserId == actorUserId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == "ACTIVE" &&
                    sc.User.Role != null &&
                    sc.User.Role.RoleName == "Mangaka"))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Returns all series with CoverFile eagerly loaded so the dashboard can render
        /// cover thumbnails in a single query. Display-only — not for update workflows.
        /// </summary>
        public async Task<IReadOnlyList<Series>> GetAllWithCoverAsync()
        {
            return await _context.Series
                .Include(s => s.CoverFile)
                .Include(s => s.Genres)
                .Include(s => s.Tags)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(Guid newSeriesId, Guid? coverFileResourceId)> CreateSeriesDraftViaProcAsync(
            Guid actorUserId,
            string title,
            string slug,
            string synopsis,
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
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
            cmd.Parameters.Add(new SqlParameter("@genre_ids_json", SqlDbType.NVarChar, -1) { Value = SerializeGuidArray(genreIds) });
            cmd.Parameters.Add(new SqlParameter("@tag_ids_json", SqlDbType.NVarChar, -1) { Value = SerializeGuidArray(tagIds) });
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

        // ── Custom error numbers raised by manga.usp_Series_UpdateProfile ────────
        private const int ErrUpdateLockFailed           = 57401;
        private const int ErrUpdateSeriesNotFound       = 57402;
        private const int ErrUpdateNotProposalDraft     = 57403;
        private const int ErrUpdateNotActiveMangaka     = 57404;
        private const int ErrUpdateIncompleteCoverMeta  = 57405;

        /// <summary>
        /// Updates a PROPOSAL_DRAFT series profile through <c>manga.usp_Series_UpdateProfile</c>.
        /// Uses ADO.NET CommandType.StoredProcedure with strongly-typed SqlParameters.
        /// Maps known custom error numbers to user-safe InvalidOperationException messages.
        /// </summary>
        public async Task<Guid?> UpdateSeriesDraftViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            string title,
            string slug,
            string synopsis,
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
            string contentLanguageCode,
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
            cmd.CommandText = "manga.usp_Series_UpdateProfile";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new SqlParameter("@series_id", SqlDbType.UniqueIdentifier) { Value = seriesId });
            cmd.Parameters.Add(new SqlParameter("@title", SqlDbType.NVarChar, 200) { Value = title });
            cmd.Parameters.Add(new SqlParameter("@slug", SqlDbType.NVarChar, 220) { Value = slug });
            cmd.Parameters.Add(new SqlParameter("@synopsis", SqlDbType.NVarChar, -1) { Value = synopsis });
            cmd.Parameters.Add(new SqlParameter("@genre_ids_json", SqlDbType.NVarChar, -1) { Value = SerializeGuidArray(genreIds) });
            cmd.Parameters.Add(new SqlParameter("@tag_ids_json", SqlDbType.NVarChar, -1) { Value = SerializeGuidArray(tagIds) });
            cmd.Parameters.Add(new SqlParameter("@content_language_code", SqlDbType.NVarChar, 10) { Value = contentLanguageCode });
            cmd.Parameters.Add(new SqlParameter("@publication_frequency_code", SqlDbType.NVarChar, 50) { Value = (object?)publicationFrequencyCode ?? DBNull.Value });

            cmd.Parameters.Add(new SqlParameter("@cover_original_file_name", SqlDbType.NVarChar, 260) { Value = (object?)coverOriginalFileName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_cloudinary_public_id", SqlDbType.NVarChar, 255) { Value = (object?)coverCloudinaryPublicId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_cloudinary_secure_url", SqlDbType.NVarChar, 1000) { Value = (object?)coverCloudinarySecureUrl ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_content_type", SqlDbType.NVarChar, 100) { Value = (object?)coverContentType ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_file_size_bytes", SqlDbType.BigInt) { Value = (object?)coverFileSizeBytes ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@cover_sha256_hash", SqlDbType.Char, 64) { Value = (object?)coverSha256Hash ?? DBNull.Value });

            var outNewCoverFileResourceId = new SqlParameter("@new_cover_file_resource_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outNewCoverFileResourceId);

            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw MapUpdateSqlException(ex);
            }

            var rawValue = outNewCoverFileResourceId.Value;
            return rawValue is DBNull || rawValue == null ? null : (Guid)rawValue;
        }

        private static InvalidOperationException MapUpdateSqlException(SqlException ex) =>
            ex.Number switch
            {
                ErrUpdateLockFailed =>
                    new InvalidOperationException("Could not process the profile update right now. Please try again.", ex),
                ErrUpdateSeriesNotFound =>
                    new InvalidOperationException("The selected series could not be found.", ex),
                ErrUpdateNotProposalDraft =>
                    new InvalidOperationException("Only a series in draft status can have its profile updated here.", ex),
                ErrUpdateNotActiveMangaka =>
                    new InvalidOperationException("Only an active Mangaka contributor can update this series profile.", ex),
                ErrUpdateIncompleteCoverMeta =>
                    new InvalidOperationException("The cover file information is incomplete. Please re-select the image and try again.", ex),
                ErrDuplicateKey or ErrUniqueIndex =>
                    new InvalidOperationException("A series with this title or slug already exists. Please choose a different title.", ex),
                ErrCheckConstraint =>
                    new InvalidOperationException("Some of the series details are not valid. Please check the language and frequency values.", ex),
                _ =>
                    new InvalidOperationException("We could not update the series draft right now. Please try again.", ex)
            };

        // ── Custom error numbers raised by manga.usp_Series_CancelDraft ─────────
        private const int ErrCancelLockFailed       = 57101;
        private const int ErrCancelSeriesNotFound   = 57102;
        private const int ErrCancelNotProposalDraft = 57103;
        private const int ErrCancelNotContributor   = 57104;

        /// <summary>
        /// Cancels a PROPOSAL_DRAFT series via <c>manga.usp_Series_CancelDraft</c>.
        /// Uses ADO.NET CommandType.StoredProcedure with strongly-typed SqlParameters.
        /// The procedure has no OUTPUT parameters; success = no exception.
        /// Maps known custom SQL error numbers to user-safe messages.
        /// </summary>
        public async Task CancelSeriesDraftViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            string? reason,
            CancellationToken cancellationToken = default)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_Series_CancelDraft";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new SqlParameter("@series_id",     SqlDbType.UniqueIdentifier) { Value = seriesId });
            cmd.Parameters.Add(new SqlParameter("@reason",        SqlDbType.NVarChar, 500)    { Value = (object?)reason ?? DBNull.Value });

            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw MapCancelSqlException(ex);
            }
        }

        private static InvalidOperationException MapCancelSqlException(SqlException ex) =>
            ex.Number switch
            {
                ErrCancelLockFailed =>
                    new InvalidOperationException("Could not process the cancellation right now. Please try again.", ex),
                ErrCancelSeriesNotFound =>
                    new InvalidOperationException("The selected series could not be found.", ex),
                ErrCancelNotProposalDraft =>
                    new InvalidOperationException("Only a series in draft status can be cancelled.", ex),
                ErrCancelNotContributor =>
                    new InvalidOperationException("Only an active Mangaka contributor can cancel this draft.", ex),
                ErrDuplicateKey or ErrUniqueIndex =>
                    new InvalidOperationException("A conflict occurred. Please try again.", ex),
                _ =>
                    new InvalidOperationException("This draft could not be cancelled right now. Please try again.", ex)
            };

        // ── Series detail by slug (read model) ─────────────────────────────────────

        /// <inheritdoc />
        public async Task<(Series? Series, IReadOnlyList<SeriesContributorReadModel> Contributors, IReadOnlyList<Chapter> Chapters, int TotalChapterCount)>
            GetSeriesDetailBySlugAsync(
                string slug,
                int chapterPage,
                int chapterPageSize,
                CancellationToken cancellationToken = default)
        {
            var series = await _context.Series
                .AsNoTracking()
                .Include(s => s.CoverFile)
                .Include(s => s.Genres)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

            if (series is null)
                return (null, Array.Empty<SeriesContributorReadModel>(), Array.Empty<Chapter>(), 0);

            // All contributors (active and past) with display name, role, dates.
            var contributors = await _context.SeriesContributors
                .AsNoTracking()
                .Where(sc => sc.SeriesId == series.SeriesId && sc.User != null)
                .Select(sc => new SeriesContributorReadModel(
                    sc.User!.DisplayName,
                    sc.User.Role != null ? sc.User.Role.RoleName : "",
                    sc.StartDate,
                    sc.EndDate))
                .ToListAsync(cancellationToken);

            // Paginated chapters sorted by ChapterNumberLabel.
            int totalChapterCount = await _context.Chapters
                .CountAsync(c => c.SeriesId == series.SeriesId, cancellationToken);

            int skip = (chapterPage - 1) * chapterPageSize;

            var chapters = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.SeriesId == series.SeriesId)
                .OrderBy(c => c.ChapterNumberLabel)
                .Skip(skip)
                .Take(chapterPageSize)
                .ToListAsync(cancellationToken);

            return (series, contributors, chapters, totalChapterCount);
        }

        // ── Workspace entry access check (read model) ───────────────────────────────

        private static readonly string[] AllowedWorkspaceRoles = { "Mangaka", "Tantou Editor", "Assistant" };

        /// <inheritdoc />
        /// <summary>
        /// Returns a single series by id where the specified actor is an active Mangaka contributor,
        /// with CoverFile, Genres, and Tags eagerly loaded. Same scoping as the dashboard list query
        /// but targeted to one series id. Returns null when the series is not found or the actor is
        /// not an active contributor.
        /// </summary>
        public async Task<Series?> GetByContributorAndSeriesIdAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Series
                .AsNoTracking()
                .Include(s => s.CoverFile)
                .Include(s => s.Genres)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(
                    s => s.SeriesId == seriesId &&
                         _context.SeriesContributors.Any(sc =>
                             sc.SeriesId == s.SeriesId &&
                             sc.UserId == actorUserId &&
                             sc.EndDate == null &&
                             sc.User != null &&
                             sc.User.StatusCode == "ACTIVE" &&
                             sc.User.Role != null &&
                             sc.User.Role.RoleName == "Mangaka"),
                    cancellationToken);
        }

        public async Task<(Guid SeriesId, string Slug, string Title, bool CanAccess)?>
            GetWorkspaceEntryBySlugAsync(
                string slug,
                Guid actorUserId,
                CancellationToken cancellationToken = default)
        {
            var series = await _context.Series
                .AsNoTracking()
                .Where(s => s.Slug == slug)
                .Select(s => new { s.SeriesId, s.Slug, s.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (series is null)
                return null;

            bool canAccess = await _context.SeriesContributors
                .AsNoTracking()
                .AnyAsync(sc =>
                    sc.SeriesId == series.SeriesId &&
                    sc.UserId == actorUserId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == "ACTIVE" &&
                    sc.User.Role != null &&
                    AllowedWorkspaceRoles.Contains(sc.User.Role.RoleName),
                    cancellationToken);

            return (series.SeriesId, series.Slug, series.Title, canAccess);
        }

        private static string SerializeGuidArray(IEnumerable<Guid> ids)
        {
            Guid[] cleanedIds = ids
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            return JsonSerializer.Serialize(cleanedIds);
        }
    }
}
