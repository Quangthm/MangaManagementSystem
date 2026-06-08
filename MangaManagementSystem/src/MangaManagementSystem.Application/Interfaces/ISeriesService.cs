using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesService
    {
        Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto);
        Task<SeriesDto?> GetSeriesByIdAsync(Guid id);
        Task<IEnumerable<SeriesDto>> GetAllSeriesAsync();
    }
}
