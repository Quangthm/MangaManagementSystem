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
    }
}
