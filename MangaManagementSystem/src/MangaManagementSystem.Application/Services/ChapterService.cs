using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterService : IChapterService
    {
        private static readonly HashSet<string> AllowedStatusCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "DRAFT",
            "UNDER_REVIEW",
            "REVISION_REQUESTED",
            "APPROVED",
            "SCHEDULED",
            "RELEASED",
            "ON_HOLD",
            "CANCELLED"
        };

        private readonly IUnitOfWork _unitOfWork;

        public ChapterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
        {
            ValidateCreateDto(dto);

            var series = await _unitOfWork.Series.GetByIdAsync(dto.SeriesId);
            if (series == null)
                throw new InvalidOperationException("Series not found.");

            var allChapters = await _unitOfWork.Chapters.GetAllAsync();

            var duplicate = allChapters.Any(c =>
                c.SeriesId == dto.SeriesId &&
                string.Equals(
                    c.ChapterNumberLabel.Trim(),
                    dto.ChapterNumberLabel.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (duplicate)
                throw new InvalidOperationException("Chapter number label already exists in this series.");

            var chapter = new Chapter
            {
                ChapterId = Guid.NewGuid(),
                SeriesId = dto.SeriesId,
                ChapterNumberLabel = dto.ChapterNumberLabel.Trim(),
                ChapterTitle = string.IsNullOrWhiteSpace(dto.ChapterTitle) ? null : dto.ChapterTitle.Trim(),
                StatusCode = "DRAFT",
                PlannedReleaseDate = dto.PlannedReleaseDate,
                ReleasedAtUtc = null,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = dto.CreatedByUserId,
                UpdatedAtUtc = null
            };

            await _unitOfWork.Chapters.AddAsync(chapter);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(chapter);
        }

        public async Task<ChapterDto?> GetChapterByIdAsync(Guid id)
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);

            if (chapter == null)
                return null;

            return MapToDto(chapter);
        }

        public async Task<IEnumerable<ChapterDto>> GetAllChaptersAsync()
        {
            var chapters = await _unitOfWork.Chapters.GetAllAsync();

            return chapters
                .OrderBy(c => c.SeriesId)
                .ThenBy(c => c.ChapterNumberLabel)
                .Select(MapToDto);
        }

        public async Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId)
        {
            if (seriesId == Guid.Empty)
                throw new InvalidOperationException("SeriesId is required.");

            var chapters = await _unitOfWork.Chapters.GetAllAsync();

            return chapters
                .Where(c => c.SeriesId == seriesId)
                .OrderBy(c => c.ChapterNumberLabel)
                .Select(MapToDto);
        }

        public async Task<ChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto)
        {
            if (id != dto.ChapterId)
                throw new InvalidOperationException("Route id does not match ChapterId.");

            ValidateUpdateDto(dto);

            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (chapter == null)
                return null;

            var series = await _unitOfWork.Series.GetByIdAsync(dto.SeriesId);
            if (series == null)
                throw new InvalidOperationException("Series not found.");

            var allChapters = await _unitOfWork.Chapters.GetAllAsync();

            var duplicate = allChapters.Any(c =>
                c.ChapterId != id &&
                c.SeriesId == dto.SeriesId &&
                string.Equals(
                    c.ChapterNumberLabel.Trim(),
                    dto.ChapterNumberLabel.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (duplicate)
                throw new InvalidOperationException("Chapter number label already exists in this series.");

            chapter.SeriesId = dto.SeriesId;
            chapter.ChapterNumberLabel = dto.ChapterNumberLabel.Trim();
            chapter.ChapterTitle = string.IsNullOrWhiteSpace(dto.ChapterTitle) ? null : dto.ChapterTitle.Trim();
            chapter.StatusCode = dto.StatusCode.Trim().ToUpperInvariant();
            chapter.PlannedReleaseDate = dto.PlannedReleaseDate;
            chapter.ReleasedAtUtc = dto.ReleasedAtUtc;
            chapter.UpdatedAtUtc = DateTime.UtcNow;

            _unitOfWork.Chapters.Update(chapter);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(chapter);
        }

        public async Task<bool> DeleteChapterAsync(Guid id)
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);

            if (chapter == null)
                return false;

            _unitOfWork.Chapters.Delete(chapter);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static void ValidateCreateDto(CreateChapterDto dto)
        {
            if (dto.SeriesId == Guid.Empty)
                throw new InvalidOperationException("SeriesId is required.");

            if (string.IsNullOrWhiteSpace(dto.ChapterNumberLabel))
                throw new InvalidOperationException("ChapterNumberLabel is required.");
        }

        private static void ValidateUpdateDto(UpdateChapterDto dto)
        {
            if (dto.ChapterId == Guid.Empty)
                throw new InvalidOperationException("ChapterId is required.");

            if (dto.SeriesId == Guid.Empty)
                throw new InvalidOperationException("SeriesId is required.");

            if (string.IsNullOrWhiteSpace(dto.ChapterNumberLabel))
                throw new InvalidOperationException("ChapterNumberLabel is required.");

            if (string.IsNullOrWhiteSpace(dto.StatusCode))
                throw new InvalidOperationException("StatusCode is required.");

            var status = dto.StatusCode.Trim().ToUpperInvariant();

            if (!AllowedStatusCodes.Contains(status))
                throw new InvalidOperationException("Invalid chapter status code.");

            if (status == "SCHEDULED" && dto.PlannedReleaseDate == null)
                throw new InvalidOperationException("PlannedReleaseDate is required when chapter is SCHEDULED.");

            if (status == "RELEASED" && dto.ReleasedAtUtc == null)
                throw new InvalidOperationException("ReleasedAtUtc is required when chapter is RELEASED.");

            if (status != "RELEASED" && dto.ReleasedAtUtc != null)
                throw new InvalidOperationException("ReleasedAtUtc can only be set when chapter is RELEASED.");
        }

        private static ChapterDto MapToDto(Chapter chapter)
        {
            return new ChapterDto(
                chapter.ChapterId,
                chapter.SeriesId,
                chapter.ChapterNumberLabel,
                chapter.ChapterTitle,
                chapter.StatusCode,
                chapter.PlannedReleaseDate,
                chapter.ReleasedAtUtc,
                chapter.CreatedAtUtc,
                chapter.CreatedByUserId,
                chapter.UpdatedAtUtc
            );
        }
    }
}