using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterPageVersionService
    {
        Task<ChapterPageVersionDto> CreateChapterPageVersionAsync(CreateChapterPageVersionDto dto);
        Task<ChapterPageVersionDto?> GetChapterPageVersionByIdAsync(Guid id);
        Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByChapterPageIdAsync(Guid chapterPageId);
        Task<ChapterPageVersionDto?> UpdateChapterPageVersionAsync(UpdateChapterPageVersionDto dto);
        Task<bool> DeleteChapterPageVersionAsync(Guid id);
        Task<bool> SetCurrentVersionAsync(Guid chapterPageId, Guid chapterPageVersionId);
    }
}
