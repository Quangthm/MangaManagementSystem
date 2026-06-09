using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ChapterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
        {
            var entity = new Chapter
            {
                SeriesId = dto.SeriesId,
                ChapterNumberLabel = dto.ChapterNumberLabel,
                ChapterTitle = dto.ChapterTitle,
                StatusCode = dto.StatusCode,
                PlannedReleaseDate = dto.PlannedReleaseDate,
                CreatedAtUtc = System.DateTime.UtcNow
            };
            await _unitOfWork.Chapters.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterDto?> GetChapterByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.Chapters.GetAllAsync();
            return all.Where(c => c.SeriesId == seriesId).Select(MapToDto);
        }

        private static ChapterDto MapToDto(Chapter c) => new(
            c.ChapterId,
            c.SeriesId,
            c.ChapterNumberLabel,
            c.ChapterTitle,
            c.StatusCode,
            c.PlannedReleaseDate,
            c.ReleasedAtUtc,
            c.CreatedAtUtc,
            c.CreatedByUserId
        );
    }
}
