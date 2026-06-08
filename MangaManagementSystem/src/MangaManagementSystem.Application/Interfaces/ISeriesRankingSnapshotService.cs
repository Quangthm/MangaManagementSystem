using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesRankingSnapshotService
    {
        Task<SeriesRankingSnapshotDto> CreateSeriesRankingSnapshotAsync(CreateSeriesRankingSnapshotDto dto);
        Task<SeriesRankingSnapshotDto?> GetSeriesRankingSnapshotByIdAsync(Guid id);
        Task<IEnumerable<SeriesRankingSnapshotDto>> GetSeriesRankingSnapshotsBySeriesIdAsync(Guid seriesId);
    }
}
