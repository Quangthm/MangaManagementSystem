using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class FileResourceRepository : GenericRepository<FileResource>, IFileResourceRepository
    {
        public FileResourceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<FileResource>> SearchAdminFilesAsync(
            string? keyword,
            string? purposeCode,
            string? storageState,
            int page,
            int pageSize)
        {
            var normalizedPage = NormalizePage(page);
            var normalizedPageSize = NormalizePageSize(pageSize);

            return await BuildAdminFileQuery(keyword, purposeCode, storageState)
                .OrderByDescending(f => f.UploadedAtUtc)
                .ThenByDescending(f => f.FileResourceId)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync();
        }

        public async Task<int> CountAdminFilesAsync(
            string? keyword,
            string? purposeCode,
            string? storageState)
        {
            return await BuildAdminFileQuery(keyword, purposeCode, storageState)
                .CountAsync();
        }

        public async Task<IReadOnlyList<FileResource>> GetStorageCleanupCandidatesAsync(
            int batchSize)
        {
            var normalizedBatchSize = NormalizeBatchSize(batchSize);

            return await _dbSet
                .Where(f => f.DeletedAtUtc != null && f.StorageCleanedAtUtc == null)
                .OrderBy(f => f.DeletedAtUtc)
                .ThenBy(f => f.FileResourceId)
                .Take(normalizedBatchSize)
                .ToListAsync();
        }

        private IQueryable<FileResource> BuildAdminFileQuery(
            string? keyword,
            string? purposeCode,
            string? storageState)
        {
            var query = _dbSet.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();

                query = query.Where(f =>
                    f.OriginalFileName.Contains(normalizedKeyword)
                    || f.CloudinaryPublicId.Contains(normalizedKeyword)
                    || f.CloudinarySecureUrl.Contains(normalizedKeyword)
                    || f.ContentType.Contains(normalizedKeyword)
                    || f.FilePurposeCode.Contains(normalizedKeyword));
            }

            if (!string.IsNullOrWhiteSpace(purposeCode))
            {
                var normalizedPurposeCode = purposeCode.Trim();
                query = query.Where(f => f.FilePurposeCode == normalizedPurposeCode);
            }

            if (!string.IsNullOrWhiteSpace(storageState))
            {
                var normalizedStorageState = storageState.Trim().ToUpperInvariant();

                query = normalizedStorageState switch
                {
                    "ACTIVE" => query.Where(f => f.DeletedAtUtc == null),

                    "PENDING" or "PENDING_CLEANUP" => query.Where(f =>
                        f.DeletedAtUtc != null
                        && f.StorageCleanedAtUtc == null
                        && (f.StorageCleanupError == null || f.StorageCleanupError == string.Empty)),

                    "CLEANED" => query.Where(f =>
                        f.DeletedAtUtc != null
                        && f.StorageCleanedAtUtc != null),

                    "FAILED" => query.Where(f =>
                        f.DeletedAtUtc != null
                        && f.StorageCleanedAtUtc == null
                        && f.StorageCleanupError != null
                        && f.StorageCleanupError != string.Empty),

                    "DELETED" => query.Where(f => f.DeletedAtUtc != null),

                    _ => query
                };
            }

            return query;
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1)
            {
                return 20;
            }

            return pageSize > 100 ? 100 : pageSize;
        }

        private static int NormalizeBatchSize(int batchSize)
        {
            if (batchSize < 1)
            {
                return 20;
            }

            return batchSize > 100 ? 100 : batchSize;
        }
    }
}
