using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Editor
{
    /// <summary>
    /// Read-only aggregate for the Tantou Editor dashboard. All values are sourced from the
    /// database via EF AsNoTracking read queries — no mock data. Chapters Under Review and
    /// Pending Annotations are scoped to series where the current editor is an active
    /// contributor. Serialized Series is also contributor-scoped. Pending Proposals remains a
    /// global claimable queue for all active Tantou Editors.
    /// </summary>
    public sealed record EditorDashboardDto(
        int PendingProposalCount,
        int ChaptersUnderReviewCount,
        int PendingAnnotationCount,
        int SerializedSeriesCount,
        IReadOnlyList<EditorDashboardProposalDto> ProposalReviewQueue,
        IReadOnlyList<EditorDashboardSeriesActivityDto> RecentSeriesActivity);

    /// <summary>
    /// A single proposal preview row for the dashboard's Proposal Review Queue. Carries the
    /// SeriesProposalId so the UI can navigate to /editor/proposals/{seriesProposalId}.
    /// </summary>
    public sealed record EditorDashboardProposalDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        string SeriesTitle,
        string ProposalTitle,
        short ProposalVersionNo,
        string SubmittedByDisplayName,
        DateTime SubmittedAtUtc,
        string StatusCode);

    /// <summary>
    /// A single recent-activity row for the dashboard's Recent Series Activity table.
    /// LatestChapterLabel is the chapter number label (e.g. "Ch. 45") because the schema stores
    /// chapter numbers as labels, not integers. LastActivityAtUtc is the series' UpdatedAtUtc
    /// falling back to CreatedAtUtc.
    ///
    /// LatestProposalId and LatestProposalStatusCode enable navigation to the latest proposal
    /// when the series is not yet serialized. CanOpenSeriesSlugPage is pre-computed by the
    /// Application handler using SeriesNavigationPolicy, so the Web layer does not need to
    /// call business policy directly.
    /// </summary>
    public sealed record EditorDashboardSeriesActivityDto(
        Guid SeriesId,
        string SeriesTitle,
        string SeriesSlug,
        string StatusCode,
        string? LatestChapterLabel,
        DateTime? LastActivityAtUtc,
        Guid? LatestProposalId,
        string? LatestProposalStatusCode,
        bool CanOpenSeriesSlugPage);
}
