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
                .Include(sp => sp.Series)
                .Include(sp => sp.SubmittedByUser)
                .Include(sp => sp.ReviewedByUser)
                .Include(sp => sp.ProposalFile)
                .Include(sp => sp.MarkupFile)
                .FirstOrDefaultAsync(sp => sp.SeriesProposalId == seriesProposalId, ct);
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
                .Include(sp => sp.Series)
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

            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC manga.usp_SeriesProposal_ClaimEditorialReview @series_proposal_id, @actor_user_id, @notes, @new_series_contributor_id OUTPUT",
                parameters);

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

            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC manga.usp_SeriesProposal_RequestRevision @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                parameters);

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

            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC manga.usp_SeriesProposal_PassToBoard @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                parameters);

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

            await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC manga.usp_SeriesProposal_CancelEditorialReview @series_proposal_id, @actor_user_id, @comments, @markup_original_file_name, @markup_cloudinary_public_id, @markup_cloudinary_secure_url, @markup_content_type, @markup_file_size_bytes, @markup_sha256_hash, @markup_file_resource_id OUTPUT",
                parameters);

            return (Guid)outParam.Value;
        }
    }
}
