using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MangaManagementSystem.Application.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly IUnitOfWork _unitOfWork;
        public SeriesService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto)
        {
            var entity = new Series
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Synopsis = dto.Synopsis,
                Genre = dto.Genre,
                CoverFileId = dto.CoverFileId,
                StatusCode = dto.StatusCode,
                ContentLanguageCode = dto.ContentLanguageCode,
                SourceSeriesId = dto.SourceSeriesId,
                PublicationFrequencyCode = dto.PublicationFrequencyCode,
                CreatedAtUtc = System.DateTime.UtcNow
            };
            await _unitOfWork.Series.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesDto?> GetSeriesByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesDto>> GetAllSeriesAsync()
        {
            var entities = await _unitOfWork.Series.GetAllAsync();
            return entities.Select(MapToDto);
        }

        public async Task<SeriesDto?> UpdateSeriesAsync(UpdateSeriesDto dto)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(dto.SeriesId);
            if (entity == null) return null;

            entity.Title = dto.Title;
            entity.Slug = dto.Slug;
            entity.Synopsis = dto.Synopsis;
            entity.Genre = dto.Genre;
            entity.CoverFileId = dto.CoverFileId;
            entity.StatusCode = dto.StatusCode;
            entity.ContentLanguageCode = dto.ContentLanguageCode;
            entity.SourceSeriesId = dto.SourceSeriesId;
            entity.PublicationFrequencyCode = dto.PublicationFrequencyCode;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.UpdatedByUserId = dto.UpdatedByUserId;

            _unitOfWork.Series.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        private static SeriesDto MapToDto(Series s) => new(
            s.SeriesId,
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
