using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageService
    {
        Task<ChapterPageDto> CreateChapterPageAsync(CreateChapterPageDto dto);
        Task<ChapterPageDto?> GetChapterPageByIdAsync(long id);
        Task<IEnumerable<ChapterPageDto>> GetChapterPagesByChapterIdAsync(long chapterId);
        Task<ChapterPageDto?> UpdateChapterPageAsync(UpdateChapterPageDto dto);
        Task<bool> DeleteChapterPageAsync(long id, int? deletedByUserId = null);
    }
}
