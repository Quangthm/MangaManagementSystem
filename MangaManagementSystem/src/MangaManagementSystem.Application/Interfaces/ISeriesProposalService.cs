using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesProposalService
    {
        Task<SeriesProposalDto> CreateSeriesProposalAsync(CreateSeriesProposalDto dto);
        Task<SeriesProposalDto?> GetSeriesProposalByIdAsync(Guid id);
        Task<IEnumerable<SeriesProposalDto>> GetSeriesProposalsBySeriesIdAsync(Guid seriesId);
        Task<SeriesProposalDto?> UpdateSeriesProposalAsync(UpdateSeriesProposalDto dto);
        Task<bool> DeleteSeriesProposalAsync(Guid id);
    }
}
