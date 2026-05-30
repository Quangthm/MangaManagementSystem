using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesProposalService
    {
        Task<SeriesProposalDto> CreateSeriesProposalAsync(CreateSeriesProposalDto dto);
        Task<SeriesProposalDto?> GetSeriesProposalByIdAsync(long id);
        Task<IEnumerable<SeriesProposalDto>> GetSeriesProposalsBySeriesIdAsync(long seriesId);
        Task<SeriesProposalDto?> UpdateSeriesProposalAsync(UpdateSeriesProposalDto dto);
        Task<bool> DeleteSeriesProposalAsync(long id);
    }
}
