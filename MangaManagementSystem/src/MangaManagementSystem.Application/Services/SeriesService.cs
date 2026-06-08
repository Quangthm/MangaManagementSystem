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
    public class SeriesService : ISeriesService
    {
        private static readonly string[] AllowedStatusCodes =
        {
            "PROPOSAL_DRAFT",
            "UNDER_EDITORIAL_REVIEW",
            "UNDER_BOARD_REVIEW",
            "SERIALIZED",
            "HIATUS",
            "CANCELLED",
            "COMPLETED"
        };

        private static readonly string[] AllowedContentLanguageCodes =
        {
            "ja",
            "en",
            "vi"
        };

        private static readonly string[] AllowedPublicationFrequencyCodes =
        {
            "WEEKLY",
            "MONTHLY",
            "IRREGULAR"
        };

        private readonly IUnitOfWork _unitOfWork;

        public SeriesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto)
        {
            ValidateCreateDto(dto);

            var allSeries = await _unitOfWork.Series.GetAllAsync();

            if (allSeries.Any(s => s.SeriesCode == dto.SeriesCode))
                throw new InvalidOperationException("SeriesCode already exists.");

            if (allSeries.Any(s => s.Slug == dto.Slug))
                throw new InvalidOperationException("Slug already exists.");

            if (dto.SourceSeriesId.HasValue)
            {
                var sourceSeries = await _unitOfWork.Series.GetByIdAsync(dto.SourceSeriesId.Value);

                if (sourceSeries == null)
                    throw new InvalidOperationException("Source series does not exist.");
            }

            var entity = new Series
            {
                SeriesCode = dto.SeriesCode.Trim(),
                Title = dto.Title.Trim(),
                Slug = dto.Slug.Trim(),
                Synopsis = dto.Synopsis.Trim(),
                Genre = dto.Genre.Trim(),
                CoverFileId = dto.CoverFileId,
                StatusCode = string.IsNullOrWhiteSpace(dto.StatusCode)
                    ? "PROPOSAL_DRAFT"
                    : dto.StatusCode.Trim(),
                ContentLanguageCode = string.IsNullOrWhiteSpace(dto.ContentLanguageCode)
                    ? "ja"
                    : dto.ContentLanguageCode.Trim(),
                SourceSeriesId = dto.SourceSeriesId,
                PublicationFrequencyCode = string.IsNullOrWhiteSpace(dto.PublicationFrequencyCode)
                    ? null
                    : dto.PublicationFrequencyCode.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.Series.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<SeriesDto?> GetSeriesByIdAsync(long id)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(id);

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesDto>> GetAllSeriesAsync()
        {
            var entities = await _unitOfWork.Series.GetAllAsync();

            return entities
                .OrderByDescending(s => s.CreatedAtUtc)
                .Select(MapToDto);
        }

        public async Task<SeriesDto?> UpdateSeriesAsync(long id, UpdateSeriesDto dto, int updatedByUserId)
        {
            if (id != dto.SeriesId)
                throw new InvalidOperationException("Route id does not match dto SeriesId.");

            ValidateUpdateDto(dto);

            var entity = await _unitOfWork.Series.GetByIdAsync(id);

            if (entity == null)
                return null;

            var allSeries = await _unitOfWork.Series.GetAllAsync();

            if (allSeries.Any(s => s.SeriesId != id && s.SeriesCode == dto.SeriesCode))
                throw new InvalidOperationException("SeriesCode already exists.");

            if (allSeries.Any(s => s.SeriesId != id && s.Slug == dto.Slug))
                throw new InvalidOperationException("Slug already exists.");

            if (dto.SourceSeriesId.HasValue)
            {
                if (dto.SourceSeriesId.Value == id)
                    throw new InvalidOperationException("Series cannot reference itself as source series.");

                var sourceSeries = await _unitOfWork.Series.GetByIdAsync(dto.SourceSeriesId.Value);

                if (sourceSeries == null)
                    throw new InvalidOperationException("Source series does not exist.");
            }

            entity.SeriesCode = dto.SeriesCode.Trim();
            entity.Title = dto.Title.Trim();
            entity.Slug = dto.Slug.Trim();
            entity.Synopsis = dto.Synopsis.Trim();
            entity.Genre = dto.Genre.Trim();
            entity.CoverFileId = dto.CoverFileId;
            entity.StatusCode = dto.StatusCode.Trim();
            entity.ContentLanguageCode = dto.ContentLanguageCode.Trim();
            entity.SourceSeriesId = dto.SourceSeriesId;
            entity.PublicationFrequencyCode = string.IsNullOrWhiteSpace(dto.PublicationFrequencyCode)
                ? null
                : dto.PublicationFrequencyCode.Trim();

            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.UpdatedByUserId = updatedByUserId;

            _unitOfWork.Series.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<bool> DeleteSeriesAsync(long id)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(id);

            if (entity == null)
                return false;

            _unitOfWork.Series.Delete(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static void ValidateCreateDto(CreateSeriesDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SeriesCode))
                throw new InvalidOperationException("SeriesCode is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Title is required.");

            if (string.IsNullOrWhiteSpace(dto.Slug))
                throw new InvalidOperationException("Slug is required.");

            if (string.IsNullOrWhiteSpace(dto.Synopsis))
                throw new InvalidOperationException("Synopsis is required.");

            if (string.IsNullOrWhiteSpace(dto.Genre))
                throw new InvalidOperationException("Genre is required.");

            var statusCode = string.IsNullOrWhiteSpace(dto.StatusCode)
                ? "PROPOSAL_DRAFT"
                : dto.StatusCode.Trim();

            if (!AllowedStatusCodes.Contains(statusCode))
                throw new InvalidOperationException("Invalid status code.");

            var languageCode = string.IsNullOrWhiteSpace(dto.ContentLanguageCode)
                ? "ja"
                : dto.ContentLanguageCode.Trim();

            if (!AllowedContentLanguageCodes.Contains(languageCode))
                throw new InvalidOperationException("Invalid content language code.");

            if (!string.IsNullOrWhiteSpace(dto.PublicationFrequencyCode) &&
                !AllowedPublicationFrequencyCodes.Contains(dto.PublicationFrequencyCode.Trim()))
            {
                throw new InvalidOperationException("Invalid publication frequency code.");
            }
        }

        private static void ValidateUpdateDto(UpdateSeriesDto dto)
        {
            if (dto.SeriesId <= 0)
                throw new InvalidOperationException("SeriesId is required.");

            if (string.IsNullOrWhiteSpace(dto.SeriesCode))
                throw new InvalidOperationException("SeriesCode is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("Title is required.");

            if (string.IsNullOrWhiteSpace(dto.Slug))
                throw new InvalidOperationException("Slug is required.");

            if (string.IsNullOrWhiteSpace(dto.Synopsis))
                throw new InvalidOperationException("Synopsis is required.");

            if (string.IsNullOrWhiteSpace(dto.Genre))
                throw new InvalidOperationException("Genre is required.");

            if (string.IsNullOrWhiteSpace(dto.StatusCode))
                throw new InvalidOperationException("StatusCode is required.");

            if (!AllowedStatusCodes.Contains(dto.StatusCode.Trim()))
                throw new InvalidOperationException("Invalid status code.");

            if (string.IsNullOrWhiteSpace(dto.ContentLanguageCode))
                throw new InvalidOperationException("ContentLanguageCode is required.");

            if (!AllowedContentLanguageCodes.Contains(dto.ContentLanguageCode.Trim()))
                throw new InvalidOperationException("Invalid content language code.");

            if (!string.IsNullOrWhiteSpace(dto.PublicationFrequencyCode) &&
                !AllowedPublicationFrequencyCodes.Contains(dto.PublicationFrequencyCode.Trim()))
            {
                throw new InvalidOperationException("Invalid publication frequency code.");
            }
        }

        private static SeriesDto MapToDto(Series s) => new(
            s.SeriesId,
            s.SeriesCode,
            s.Title,
            s.Slug,
            s.Synopsis,
            s.Genre,
            s.CoverFileId,
            s.StatusCode,
            s.ContentLanguageCode,
            s.SourceSeriesId,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.UpdatedByUserId,
            s.PublicationFrequencyCode
        );
    }
}