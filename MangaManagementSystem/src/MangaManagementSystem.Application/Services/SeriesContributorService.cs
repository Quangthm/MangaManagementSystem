using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class SeriesContributorService : ISeriesContributorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeriesContributorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesContributorDto> CreateSeriesContributorAsync(CreateSeriesContributorDto dto)
        {
            var entity = new SeriesContributor
            {
                SeriesId = dto.SeriesId,
                UserId = dto.UserId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Notes = dto.Notes
            };
            await _unitOfWork.SeriesContributors.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesContributorDto?> GetSeriesContributorByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesContributors.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesContributorDto>> GetSeriesContributorsBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.SeriesContributors.GetAllAsync();
            return all.Where(c => c.SeriesId == seriesId).Select(MapToDto);
        }

        public async Task<SeriesContributorDto?> UpdateSeriesContributorAsync(UpdateSeriesContributorDto dto)
        {
            var entity = await _unitOfWork.SeriesContributors.GetByIdAsync(dto.SeriesContributorId);
            if (entity == null)
            {
                return null;
            }

            entity.SeriesId = dto.SeriesId;
            entity.UserId = dto.UserId;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Notes = dto.Notes;
            _unitOfWork.SeriesContributors.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteSeriesContributorAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesContributors.GetByIdAsync(id);
            if (entity == null) return false;

            _unitOfWork.SeriesContributors.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static SeriesContributorDto MapToDto(SeriesContributor c) => new(
            c.SeriesContributorId,
            c.SeriesId,
            c.UserId,
            c.StartDate,
            c.EndDate,
            c.Notes
        );
    }
}
