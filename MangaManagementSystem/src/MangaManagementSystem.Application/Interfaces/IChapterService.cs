using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterService
    {
        Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto);
        Task<ChapterDto?> GetChapterByIdAsync(Guid id);
        Task<IEnumerable<ChapterDto>> GetAllChaptersAsync();
        Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId);
        Task<ChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto);
        Task<bool> DeleteChapterAsync(Guid id);
    }
}