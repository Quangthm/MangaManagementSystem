using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IFileResourceService
    {
        Task<FileResourceDto> CreateFileResourceAsync(CreateFileResourceDto dto);

        Task<FileResourceDto?> GetFileResourceByIdAsync(Guid id);

        Task<IEnumerable<FileResourceDto>> GetAllFileResourcesAsync();

        Task<bool> DeleteFileResourceAsync(Guid id, Guid? deletedByUserId = null);

        Task<AdminFileResourceSearchResultDto> SearchAdminFilesAsync(
            string? keyword = null,
            string? purposeCode = null,
            string? storageState = null,
            int page = 1,
            int pageSize = 20);

        Task<FileStorageCleanupResultDto> CleanupStorageAsync(Guid fileResourceId);

        Task<IReadOnlyList<FileStorageCleanupResultDto>> CleanupDeletedStorageBatchAsync(
            int batchSize = 20);
    }
}
