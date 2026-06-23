using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    /// <summary>
    /// Focused repository abstraction for Mangaka contributor-management workflows.
    /// Supplements the legacy generic contributor CRUD path without replacing it.
    /// Reads use EF Core projections; writes use stored-procedure wrappers.
    /// </summary>
    public interface ISeriesContributorManagementRepository
    {
        Task<bool> IsActiveMangakaContributorAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SeriesContributorListItemDto>> GetSeriesContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            string? search,
            CancellationToken cancellationToken = default);

        Task<(bool Exists, string? DisplayName, string? Username, string? Email, string? RoleName, string? StatusCode, bool IsActiveContributor)> GetContributorTargetSnapshotAsync(
            Guid seriesId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<bool> HasActiveTasksForSeriesAsync(
            Guid assistantUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task AddContributorViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid userId,
            string? notes,
            CancellationToken cancellationToken = default);

        Task EndAssistantContributorViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid assistantUserId,
            string reason,
            CancellationToken cancellationToken = default);
    }
}
