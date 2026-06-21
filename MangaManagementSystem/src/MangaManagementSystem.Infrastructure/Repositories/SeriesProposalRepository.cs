using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MangaManagementSystem.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class SeriesProposalRepository : GenericRepository<SeriesProposal>, ISeriesProposalRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SeriesProposalRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SeriesProposal?> GetByIdWithDetailsAsync(Guid seriesProposalId, CancellationToken ct = default)
        {
            return await _dbContext.Set<SeriesProposal>()
                .Include(sp => sp.Series!).ThenInclude(s => s.CoverFile)
                .Include(sp => sp.Series!).ThenInclude(s => s.Genres)
                .Include(sp => sp.Series!).ThenInclude(s => s.Tags)
                .Include(sp => sp.SubmittedByUser)
                .Include(sp => sp.ReviewedByUser)
                .Include(sp => sp.ProposalFile)
                .Include(sp => sp.MarkupFile)
                .FirstOrDefaultAsync(sp => sp.SeriesProposalId == seriesProposalId, ct);
        }

        public async Task<IReadOnlyList<SeriesProposal>> GetLatestForSeriesBatchAsync(
            IReadOnlyList<Guid> seriesIds, CancellationToken ct = default)
        {
            if (seriesIds is null || seriesIds.Count == 0)
                return Array.Empty<SeriesProposal>();

            return await _dbContext.Set<SeriesProposal>()
                .AsNoTracking()
                .Where(sp => seriesIds.Contains(sp.SeriesId))
                .OrderByDescending(sp => sp.ProposalVersionNo)
                .ThenByDescending(sp => sp.SubmittedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<SeriesProposal?> GetLatestBySeriesIdAsync(Guid seriesId, CancellationToken ct = default)
        {
            return await _dbContext.Set<SeriesProposal>()
                .Where(sp => sp.SeriesId == seriesId)
                .OrderByDescending(sp => sp.ProposalVersionNo)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<SeriesProposal>> GetEditorialQueueAsync(string? statusCode, Guid? seriesId, Guid? submittedByUserId, Guid? reviewedByUserId, CancellationToken ct = default)
        {
            var query = _dbContext.Set<SeriesProposal>()
                .Include(sp => sp.Series).ThenInclude(s => s.Genres)
                .Include(sp => sp.Series).ThenInclude(s => s.Tags)
                .Include(sp => sp.SubmittedByUser)
                .Include(sp => sp.ReviewedByUser)
                .Include(sp => sp.ProposalFile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusCode))
                query = query.Where(sp => sp.StatusCode == statusCode);

            if (seriesId.HasValue)
                query = query.Where(sp => sp.SeriesId == seriesId.Value);

            if (submittedByUserId.HasValue)
                query = query.Where(sp => sp.SubmittedByUserId == submittedByUserId.Value);

            if (reviewedByUserId.HasValue)
                query = query.Where(sp => sp.ReviewedByUserId == reviewedByUserId.Value);

            return await query.OrderByDescending(sp => sp.SubmittedAtUtc).ToListAsync(ct);
        }

        /// <summary>
        /// Returns all proposals for series where the specified actor is an active Mangaka
        /// contributor. Access scope matches GetByActiveContributorWithCoverAsync in SeriesRepository.
        /// </summary>
        public async Task<IReadOnlyList<SeriesProposal>> GetMySeriesProposalsAsync(
            Guid actorUserId, CancellationToken ct = default)
        {
            return await _dbContext.Set<SeriesProposal>()
                .AsNoTracking()
                .Include(sp => sp.Series).ThenInclude(s => s.Genres)
                .Include(sp => sp.Series).ThenInclude(s => s.Tags)
                .Include(sp => sp.SubmittedByUser)
                .Include(sp => sp.ReviewedByUser)
                .Include(sp => sp.ProposalFile)
                .Include(sp => sp.MarkupFile)
                .Where(sp => _dbContext.SeriesContributors.Any(sc =>
                    sc.SeriesId == sp.SeriesId &&
                    sc.UserId == actorUserId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == "ACTIVE" &&
                    sc.User.Role != null &&
                    sc.User.Role.RoleName == "Mangaka"))
                .OrderByDescending(sp => sp.SubmittedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<bool> IsActiveTantouEditorContributorAsync(
            Guid seriesId, Guid userId, CancellationToken ct = default)
        {
            return await _dbContext.SeriesContributors
                .AsNoTracking()
                .AnyAsync(sc =>
                    sc.SeriesId == seriesId &&
                    sc.UserId == userId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == "ACTIVE" &&
                    sc.User.Role != null &&
                    sc.User.Role.RoleName == "Tantou Editor", ct);
        }

        public async Task<Guid?> ClaimEditorialReviewAsync(Guid seriesProposalId, Guid actorUserId, string? notes, CancellationToken ct = default)
        {
            var outParam = new SqlParameter("@new_series_contributor_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            var parameters = new[]
            {
                new SqlParameter("@series_proposal_id", seriesProposalId),
                new SqlParameter("@actor_user_id", actorUserId),
                new SqlParameter("@notes", (object?)notes ?? DBNull.Value),
                outParam
            };

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC manga.usp_SeriesProposal_ClaimEditorialReview @series_proposal_id, @actor_user_id, @notes, @new_series_contributor_id OUTPUT",
                    parameters);
            }
            catch (SqlException ex)
            {
                throw MapReviewSqlException(ex);
            }

            return outParam.Value as Guid?;
        }

        public async Task<Guid?> RequestRevisionAsync(Guid seriesProposalId, Guid actorUserId, string comments,
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null,
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default)
        {
            var outParam = new SqlParameter("@markup_file_resource_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            var parameters = new[]
            {
                new SqlParameter("@series_proposal_id", seriesProposalId),
                new SqlParameter("@actor_user_id", actorUserId),
                new SqlParameter("@comments", comments),
                new SqlParameter("@markup_original_file_name", (object?)markupOriginalFileName ?? DBNull.Value),
                new SqlParameter("@markup_cloudinary_public_id", (object?)markupCloudinaryPublicId ?? DBNull.Value),
                new SqlParameter("@markup_cloudinary_secure_url", (object?)markupCloudinarySecureUrl ?? DBNull.Value),
                new SqlParameter("@markup_content_type", (object?)markupContentType ?? DBNull.Value),
                new SqlParameter("@markup_file_size_bytes", (object?)markupFileSizeBytes ?? DBNull.Value),
                new SqlParameter("@markup_sha256_hash", (object?)markupSha256Hash ?? DBNull.Value),
                outParam
            };

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC manga.usp_SeriesProposal_RequestRevision @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                    parameters);
            }
            catch (SqlException ex)
            {
                throw MapReviewSqlException(ex);
            }

            return outParam.Value as Guid?;
        }

        public async Task<Guid?> PassToBoardAsync(Guid seriesProposalId, Guid actorUserId, string? comments,
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null,
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default)
        {
            var outParam = new SqlParameter("@markup_file_resource_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            var parameters = new[]
            {
                new SqlParameter("@series_proposal_id", seriesProposalId),
                new SqlParameter("@actor_user_id", actorUserId),
                new SqlParameter("@comments", (object?)comments ?? DBNull.Value),
                new SqlParameter("@markup_original_file_name", (object?)markupOriginalFileName ?? DBNull.Value),
                new SqlParameter("@markup_cloudinary_public_id", (object?)markupCloudinaryPublicId ?? DBNull.Value),
                new SqlParameter("@markup_cloudinary_secure_url", (object?)markupCloudinarySecureUrl ?? DBNull.Value),
                new SqlParameter("@markup_content_type", (object?)markupContentType ?? DBNull.Value),
                new SqlParameter("@markup_file_size_bytes", (object?)markupFileSizeBytes ?? DBNull.Value),
                new SqlParameter("@markup_sha256_hash", (object?)markupSha256Hash ?? DBNull.Value),
                outParam
            };

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC manga.usp_SeriesProposal_PassToBoard @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                    parameters);
            }
            catch (SqlException ex)
            {
                throw MapReviewSqlException(ex);
            }

            return outParam.Value as Guid?;
        }

        public async Task<Guid> CancelProposalAsync(Guid seriesProposalId, Guid actorUserId, string comments,
            string markupOriginalFileName, string markupCloudinaryPublicId, string markupCloudinarySecureUrl,
            string markupContentType, long markupFileSizeBytes, string? markupSha256Hash = null, CancellationToken ct = default)
        {
            var outParam = new SqlParameter("@markup_file_resource_id", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
            var parameters = new[]
            {
                new SqlParameter("@series_proposal_id", seriesProposalId),
                new SqlParameter("@actor_user_id", actorUserId),
                new SqlParameter("@comments", comments),
                new SqlParameter("@markup_original_file_name", markupOriginalFileName),
                new SqlParameter("@markup_cloudinary_public_id", markupCloudinaryPublicId),
                new SqlParameter("@markup_cloudinary_secure_url", markupCloudinarySecureUrl),
                new SqlParameter("@markup_content_type", markupContentType),
                new SqlParameter("@markup_file_size_bytes", markupFileSizeBytes),
                new SqlParameter("@markup_sha256_hash", (object?)markupSha256Hash ?? DBNull.Value),
                outParam
            };

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC manga.usp_SeriesProposal_CancelEditorialReview @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                    parameters);
            }
            catch (SqlException ex)
            {
                throw MapReviewSqlException(ex);
            }

            return (Guid)outParam.Value;
        }

        /// <summary>
        /// Calls <c>manga.usp_SeriesProposal_Submit</c> via ADO.NET StoredProcedure command.
        /// Uses strongly-typed SqlParameters and output parameter capture, consistent with
        /// SeriesRepository.CreateSeriesDraftViaProcAsync.
        /// Maps known custom SQL error numbers to user-safe InvalidOperationException messages.
        /// Raw SQL error text is never surfaced to callers.
        /// </summary>
        public async Task<(Guid SeriesProposalId, short ProposalVersionNo)> SubmitSeriesProposalViaProcAsync(
            Guid seriesId,
            Guid submittedByUserId,
            string originalFileName,
            string cloudinaryPublicId,
            string cloudinarySecureUrl,
            string contentType,
            long fileSizeBytes,
            string sha256Hash,
            CancellationToken cancellationToken = default)
        {
            var conn = _dbContext.Database.GetDbConnection();
            await using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_SeriesProposal_Submit";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@series_id", SqlDbType.UniqueIdentifier) { Value = seriesId });
            cmd.Parameters.Add(new SqlParameter("@submitted_by_user_id", SqlDbType.UniqueIdentifier) { Value = submittedByUserId });
            cmd.Parameters.Add(new SqlParameter("@original_file_name", SqlDbType.NVarChar, 260) { Value = originalFileName });
            cmd.Parameters.Add(new SqlParameter("@cloudinary_public_id", SqlDbType.NVarChar, 255) { Value = cloudinaryPublicId });
            cmd.Parameters.Add(new SqlParameter("@cloudinary_secure_url", SqlDbType.NVarChar, 1000) { Value = cloudinarySecureUrl });
            cmd.Parameters.Add(new SqlParameter("@content_type", SqlDbType.NVarChar, 100) { Value = contentType });
            cmd.Parameters.Add(new SqlParameter("@file_size_bytes", SqlDbType.BigInt) { Value = fileSizeBytes });
            cmd.Parameters.Add(new SqlParameter("@sha256_hash", SqlDbType.Char, 64) { Value = sha256Hash });

            var outProposalId = new SqlParameter("@series_proposal_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outProposalId);

            var outVersionNo = new SqlParameter("@proposal_version_no", SqlDbType.SmallInt)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outVersionNo);

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
                throw MapSqlException(ex);
            }

            var proposalId = (Guid)outProposalId.Value;
            var versionNo = (short)outVersionNo.Value;

            return (proposalId, versionNo);
        }

        // ── Custom error numbers raised by manga.usp_SeriesProposal_Submit ─────────
        private const int ErrProposalSubmitLockFailed = 57001;
        private const int ErrSeriesNotFound          = 57002;
        private const int ErrNotProposalDraft        = 57003;
        private const int ErrNotActiveMangakaContrib = 57004;

        // SQL Server constraint violation numbers.
        private const int ErrDuplicateKey  = 2627;
        private const int ErrUniqueIndex   = 2601;
        private const int ErrConstraint    = 547;

        /// <summary>
        /// Translates known SqlException error numbers into user-safe messages.
        /// Raw SQL text is never propagated.
        /// </summary>
        private static InvalidOperationException MapSqlException(SqlException ex)
        {
            string message = ex.Number switch
            {
                ErrProposalSubmitLockFailed =>
                    "Could not process the proposal submission right now. Please try again.",
                ErrSeriesNotFound =>
                    "The selected series could not be found.",
                ErrNotProposalDraft =>
                    "Only a series in draft status can be submitted for editorial review.",
                ErrNotActiveMangakaContrib =>
                    "Only an active Mangaka contributor can submit a proposal for this series.",
                ErrDuplicateKey or ErrUniqueIndex =>
                    "A proposal submission conflict occurred. Please try again.",
                ErrConstraint =>
                    "The proposal submission was rejected due to an invalid value. Please check the details and try again.",
                _ =>
                    "This proposal could not be submitted right now. Please try again."
            };

            return new InvalidOperationException(message, ex);
        }

        /// <summary>
        /// Translates known SqlException error numbers raised by the editorial-review stored
        /// procedures (usp_SeriesProposal_RequestRevision / _PassToBoard / _CancelEditorialReview
        /// and the claim procedure) into user-safe messages. Raw SQL text is never propagated.
        /// Unknown numbers fall through to a generic safe message.
        /// </summary>
        private static InvalidOperationException MapReviewSqlException(SqlException ex)
        {
            string message = ex.Number switch
            {
                // Claim (573xx)
                57302 => "Only an active Tantou Editor can claim a proposal for review.",
                57303 => "The selected proposal could not be found.",
                57304 => "Only proposals under editorial review can be claimed.",
                57305 => "This series is not under editorial review.",
                57306 => "This proposal has already been reviewed.",

                // Request Revision (574xx)
                57402 => "The selected proposal could not be found.",
                57403 => "Only proposals under editorial review can request a revision.",
                57404 => "This series is not under editorial review.",
                57405 => "This proposal has already been reviewed.",
                57406 => "Only the active Tantou Editor for this series can request a revision.",

                // Pass To Board (575xx)
                57502 => "The selected proposal could not be found.",
                57503 => "Only proposals under editorial review can be passed to the board.",
                57504 => "This series is not under editorial review.",
                57505 => "This proposal has already been reviewed.",
                57506 => "Only the active Tantou Editor for this series can pass the proposal to the board.",

                // Cancel Editorial Review (576xx)
                57602 => "The selected proposal could not be found.",
                57603 => "Only proposals under editorial review can be cancelled.",
                57604 => "This series is not under editorial review.",
                57605 => "This proposal has already received an editorial decision.",
                57606 => "Only the active Tantou Editor for this series can cancel the proposal.",

                // Shared comments-required guard
                57607 => "Comments are required for this editorial decision.",

                // Lock-acquisition failures (573xx/574xx/575xx/576xx 01-variants)
                57301 or 57307 or 57401 or 57501 or 57601 =>
                    "Could not process the review right now. Please try again.",

                // Standard SQL constraint codes
                ErrDuplicateKey or ErrUniqueIndex =>
                    "A review conflict occurred. Please refresh and try again.",
                ErrConstraint =>
                    "The review was rejected due to an invalid value. Please check the details and try again.",

                _ =>
                    "This editorial action could not be completed right now. Please try again."
            };

            return new InvalidOperationException(message, ex);
        }
    }
}
