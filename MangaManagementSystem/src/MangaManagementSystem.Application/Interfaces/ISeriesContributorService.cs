using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesContributorService
    {
        Task<SeriesContributorDto> CreateSeriesContributorAsync(CreateSeriesContributorDto dto);
        Task<SeriesContributorDto?> GetSeriesContributorByIdAsync(Guid id);
        Task<IEnumerable<SeriesContributorDto>> GetSeriesContributorsBySeriesIdAsync(Guid seriesId);
        Task<SeriesContributorDto?> UpdateSeriesContributorAsync(UpdateSeriesContributorDto dto);
        Task<bool> DeleteSeriesContributorAsync(Guid id);
    }
}
