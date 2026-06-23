using MangaManagementSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IFileResourceRepository : IGenericRepository<FileResource>
    {
        Task<IReadOnlyList<FileResource>> SearchAdminFilesAsync(
            string? keyword,
            string? purposeCode,
            string? storageState,
            int page,
            int pageSize);

        Task<int> CountAdminFilesAsync(
            string? keyword,
            string? purposeCode,
            string? storageState);

        Task<IReadOnlyList<FileResource>> GetStorageCleanupCandidatesAsync(
            int batchSize);
    }
}
