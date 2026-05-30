using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterService
    {
        Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto);
        Task<ChapterDto?> GetChapterByIdAsync(long id);
        Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(long seriesId);
    }
}
