using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IMangakaSeriesContributorApiClient
    {
        Task<IReadOnlyList<SeriesContributorListItemDto>> GetContributorsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantsAsync(
            Guid seriesId,
            string? search,
            CancellationToken cancellationToken = default);

        Task AddAssistantAsync(
            Guid seriesId,
            AddAssistantContributorRequest request,
            CancellationToken cancellationToken = default);

        Task EndAssistantAsync(
            Guid seriesId,
            Guid assistantUserId,
            EndAssistantContributorRequest request,
            CancellationToken cancellationToken = default);
    }
}
