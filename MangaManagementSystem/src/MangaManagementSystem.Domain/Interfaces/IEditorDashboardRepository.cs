using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    /// <summary>
    /// Read-only repository for the Tantou Editor dashboard. All queries are EF
    /// <c>AsNoTracking</c> read models; no writes or stored-procedure transitions live here.
    /// Returns Domain entities and primitive counts only — DTO shaping happens in the
    /// Application handler so the Domain layer stays free of Application dependencies.
    /// </summary>
    public interface IEditorDashboardRepository
    {
        Task<EditorDashboardData> GetDashboardDataAsync(Guid actorUserId, int proposalQueueTake, int recentSeriesTake, CancellationToken ct = default);
    }

    /// <summary>
    /// Aggregated dashboard read result. <see cref="RecentSeriesActivity"/> series have their
    /// <c>Chapters</c> collection populated so the handler can derive the latest chapter label.
    /// </summary>
    public sealed record EditorDashboardData(
        int PendingProposalCount,
        int ChaptersUnderReviewCount,
        int PendingAnnotationCount,
        int SerializedSeriesCount,
        IReadOnlyList<SeriesProposal> ProposalReviewQueue,
        IReadOnlyList<Series> RecentSeriesActivity);
}
