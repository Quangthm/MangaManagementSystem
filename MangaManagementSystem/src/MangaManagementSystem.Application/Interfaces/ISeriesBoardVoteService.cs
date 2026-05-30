using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesBoardVoteService
    {
        Task<SeriesBoardVoteDto> CreateSeriesBoardVoteAsync(CreateSeriesBoardVoteDto dto);
        Task<SeriesBoardVoteDto?> GetSeriesBoardVoteByIdAsync(long id);
        Task<IEnumerable<SeriesBoardVoteDto>> GetSeriesBoardVotesByPollIdAsync(long seriesBoardPollId);
    }
}
