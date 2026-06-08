using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesBoardPollService
    {
        Task<SeriesBoardPollDto> CreateSeriesBoardPollAsync(CreateSeriesBoardPollDto dto);
        Task<SeriesBoardPollDto?> GetSeriesBoardPollByIdAsync(Guid id);
        Task<IEnumerable<SeriesBoardPollDto>> GetSeriesBoardPollsBySeriesIdAsync(Guid seriesId);
        Task<SeriesBoardPollDto?> UpdateSeriesBoardPollAsync(UpdateSeriesBoardPollDto dto);
    }
}
