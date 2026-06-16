using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesService
    {
        Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto);
        Task<SeriesDto?> GetSeriesByIdAsync(Guid id);
        Task<IEnumerable<SeriesDto>> GetAllSeriesAsync();
        Task<SeriesDto?> UpdateSeriesAsync(UpdateSeriesDto dto);

        /// <summary>
        /// Creates a new series draft for the acting Mangaka via <c>manga.usp_Series_Create</c>.
        /// Creates only the Series (status PROPOSAL_DRAFT) plus an optional SERIES_COVER file.
        /// Does not create a SeriesProposal and does not handle proposal documents.
        /// </summary>
        Task<SeriesDraftCreatedDto> CreateSeriesDraftAsync(
            Guid actorUserId,
            CreateSeriesDraftDto dto,
            CancellationToken cancellationToken = default);
    }
}
