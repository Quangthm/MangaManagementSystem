using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageTaskService
    {
        Task<ChapterPageTaskDto> CreateChapterPageTaskAsync(CreateChapterPageTaskDto dto);
        Task<ChapterPageTaskDto?> GetChapterPageTaskByIdAsync(long id);
        Task<IEnumerable<ChapterPageTaskDto>> GetChapterPageTasksByAssignedUserIdAsync(int assignedToUserId);
        Task<ChapterPageTaskDto?> UpdateChapterPageTaskAsync(UpdateChapterPageTaskDto dto);
        Task<bool> DeleteChapterPageTaskAsync(long id);
    }
}
