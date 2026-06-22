using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterService
    {
        Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto);
        Task<ChapterDto?> GetChapterByIdAsync(Guid id);
        Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId);
        Task DeleteChapterAsync(Guid id);
        Task UpdateChapterStatusAsync(Guid id, string statusCode);
        Task UpdateChapterTitleAsync(Guid id, string newTitle);
    }
}
