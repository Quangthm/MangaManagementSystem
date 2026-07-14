using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for general series read access.
    /// Used by /series/{slug} detail page and /series/{slug}/workspace access checks.
    /// </summary>
    public interface ISeriesApiClient
    {
        Task<SeriesDetailDto?> GetSeriesDetailAsync(
            string slug,
            int chapterPage = 1,
            int chapterPageSize = 10,
            CancellationToken cancellationToken = default);

        Task<SeriesWorkspaceEntryDto?> GetWorkspaceEntryAsync(
            Guid actorUserId,
            string slug,
            CancellationToken cancellationToken = default);

        Task<SeriesLifecycleActionsDto> GetLifecycleActionsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<SeriesCompletionImpactDto> GetCompletionImpactAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<SeriesLifecycleChangedDto> SetHiatusAsync(
            Guid seriesId,
            string reason,
            CancellationToken cancellationToken = default);

        Task<SeriesLifecycleChangedDto> ResumeSerializationAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<SeriesLifecycleChangedDto> CompleteSeriesAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);
    }
}
