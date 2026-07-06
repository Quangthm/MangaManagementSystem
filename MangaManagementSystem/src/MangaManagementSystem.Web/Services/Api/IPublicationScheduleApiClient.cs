using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Features.Publication.Schedule.Queries.GetPublicationScheduleCalendar;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IPublicationScheduleApiClient
    {
        Task<PublicationScheduleCalendarDto> GetScheduleAsync(
            DateTime? anchorDate = null,
            Guid? seriesId = null,
            string? frequencyCode = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PublicationScheduleSeriesSuggestion>> GetSeriesSuggestionsAsync(
            string searchText,
            CancellationToken cancellationToken = default);

        Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default);

        Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionByIdAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);
    }
}
