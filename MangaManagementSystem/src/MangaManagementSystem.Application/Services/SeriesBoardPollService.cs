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
    public class SeriesBoardPollService : ISeriesBoardPollService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeriesBoardPollService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesBoardPollDto> CreateSeriesBoardPollAsync(CreateSeriesBoardPollDto dto)
        {
            var entity = new SeriesBoardPoll
            {
                SeriesId = dto.SeriesId,
                PollTypeCode = dto.PollTypeCode,
                PollReason = dto.PollReason,
                PollStatusCode = "OPEN",
                CreatedByUserId = dto.CreatedByUserId,
                StartedAtUtc = DateTime.UtcNow,
                EndsAtUtc = dto.EndsAtUtc
            };
            await _unitOfWork.SeriesBoardPolls.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesBoardPollDto?> GetSeriesBoardPollByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesBoardPolls.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesBoardPollDto>> GetSeriesBoardPollsBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.SeriesBoardPolls.GetAllAsync();
            return all.Where(p => p.SeriesId == seriesId).Select(MapToDto);
        }

        public async Task<SeriesBoardPollDto?> UpdateSeriesBoardPollAsync(UpdateSeriesBoardPollDto dto)
        {
            var entity = await _unitOfWork.SeriesBoardPolls.GetByIdAsync(dto.SeriesBoardPollId);
            if (entity == null)
            {
                return null;
            }

            entity.PollStatusCode = dto.PollStatusCode;
            _unitOfWork.SeriesBoardPolls.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        private static SeriesBoardPollDto MapToDto(SeriesBoardPoll p) => new(
            p.SeriesBoardPollId,
            p.SeriesId,
            p.PollTypeCode,
            p.PollReason,
            p.PollStatusCode,
            p.CreatedByUserId,
            p.StartedAtUtc,
            p.EndsAtUtc
        );
    }
}
