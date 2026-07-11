using MangaManagementSystem.Domain.ReadModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IMangakaSeriesContributorApiClient
    {
        Task<IReadOnlyList<SeriesContributorListItemDto>> GetContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantsAsync(
            Guid actorUserId,
            Guid seriesId,
            string? search,
            CancellationToken cancellationToken = default);

        Task AddAssistantAsync(
            Guid actorUserId,
            Guid seriesId,
            AddAssistantContributorRequest request,
            CancellationToken cancellationToken = default);

        Task EndAssistantAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid assistantUserId,
            EndAssistantContributorRequest request,
            CancellationToken cancellationToken = default);
    }
}
