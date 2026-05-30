using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IFileResourceService
    {
        Task<FileResourceDto> CreateFileResourceAsync(CreateFileResourceDto dto);
        Task<FileResourceDto?> GetFileResourceByIdAsync(long id);
        Task<IEnumerable<FileResourceDto>> GetAllFileResourcesAsync();
        Task<bool> DeleteFileResourceAsync(long id, int? deletedByUserId = null);
    }
}
