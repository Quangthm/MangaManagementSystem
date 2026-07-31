using MangaManagementSystem.Domain.Entities;
using SeriesEntity = MangaManagementSystem.Domain.Entities.Series;

namespace MangaManagementSystem.Application.Features.Editor.Dashboard.Models;

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
    IReadOnlyList<SeriesEntity> RecentSeriesActivity);
