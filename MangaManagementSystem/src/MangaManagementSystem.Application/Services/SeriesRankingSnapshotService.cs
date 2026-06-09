using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class SeriesRankingSnapshotService : ISeriesRankingSnapshotService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeriesRankingSnapshotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesRankingSnapshotDto> CreateSeriesRankingSnapshotAsync(CreateSeriesRankingSnapshotDto dto)
        {
            var entity = new SeriesRankingSnapshot
            {
                SeriesId = dto.SeriesId,
                RankingPeriodTypeCode = dto.RankingPeriodTypeCode,
                PeriodStartDate = dto.PeriodStartDate,
                PeriodEndDate = dto.PeriodEndDate,
                RankPosition = dto.RankPosition,
                RankingScore = dto.RankingScore,
                GeneratedByUserId = dto.GeneratedByUserId
            };
            await _unitOfWork.SeriesRankingSnapshots.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesRankingSnapshotDto?> GetSeriesRankingSnapshotByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesRankingSnapshots.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesRankingSnapshotDto>> GetSeriesRankingSnapshotsBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.SeriesRankingSnapshots.GetAllAsync();
            return all.Where(s => s.SeriesId == seriesId).Select(MapToDto);
        }

        private static SeriesRankingSnapshotDto MapToDto(SeriesRankingSnapshot s) => new(
            s.SeriesRankingSnapshotId,
            s.SeriesId,
            s.RankingPeriodTypeCode,
            s.PeriodStartDate,
            s.PeriodEndDate,
            s.RankPosition,
            s.RankingScore,
            s.GeneratedByUserId
        );
    }
}
