using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IFileResourceService
    {
        Task<FileResourceDto> CreateFileResourceAsync(
            CreateFileResourceDto dto);

        Task<FileResourceDto?> GetFileResourceByIdAsync(
            Guid id);

        Task<IEnumerable<FileResourceDto>>
            GetAllFileResourcesAsync();

        Task<bool> DeleteFileResourceAsync(
            Guid id,
            Guid actorUserId,
            string actorRoleName);
    }
}
