using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageService
    {
        Task<ChapterPageDto> CreateChapterPageAsync(CreateChapterPageDto dto);
        Task<ChapterPageDto?> GetChapterPageByIdAsync(Guid id);
        Task<IEnumerable<ChapterPageDto>> GetChapterPagesByChapterIdAsync(Guid chapterId);
        Task<ChapterPageDto?> UpdateChapterPageAsync(UpdateChapterPageDto dto);
        Task<bool> DeleteChapterPageAsync(Guid id, Guid? deletedByUserId = null);
    }
}
