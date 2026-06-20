using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common.Policies;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Dashboard.Queries.GetEditorDashboard
{
    /// <summary>
    /// Builds the Tantou Editor dashboard read model from the dashboard repository and maps
    /// Domain entities to API-facing DTOs. Pure read; no mutations.
    ///
    /// Recent series activity rows are enriched with the latest proposal per series using a
    /// single batched query (no N+1). Slug-page eligibility is pre-computed via
    /// <see cref="SeriesNavigationPolicy"/> so the Web layer does not need to call business
    /// policy directly.
    /// </summary>
    public sealed class GetEditorDashboardQueryHandler
        : IRequestHandler<GetEditorDashboardQuery, EditorDashboardDto>
    {
        // Preview limits for the dashboard tables.
        private const int ProposalQueueTake = 5;
        private const int RecentSeriesTake = 5;

        private readonly IEditorDashboardRepository _editorDashboardRepository;
        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetEditorDashboardQueryHandler(
            IEditorDashboardRepository editorDashboardRepository,
            ISeriesProposalRepository seriesProposalRepository)
        {
            _editorDashboardRepository = editorDashboardRepository;
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<EditorDashboardDto> Handle(
            GetEditorDashboardQuery request, CancellationToken cancellationToken)
        {
            var data = await _editorDashboardRepository.GetDashboardDataAsync(
                request.ActorUserId, ProposalQueueTake, RecentSeriesTake, cancellationToken);

            var proposalQueue = data.ProposalReviewQueue
                .Select(sp => new EditorDashboardProposalDto(
                    sp.SeriesProposalId,
                    sp.SeriesId,
                    sp.Series?.Title ?? string.Empty,
                    sp.ProposalTitle,
                    sp.ProposalVersionNo,
                    sp.SubmittedByUser?.DisplayName ?? string.Empty,
                    sp.SubmittedAtUtc,
                    sp.StatusCode))
                .ToList();

            // ── Enrich Recent Series Activity with latest proposal (one batched query) ──
            var seriesIds = data.RecentSeriesActivity.Select(s => s.SeriesId).ToList();

            var latestProposals = seriesIds.Count > 0
                ? await _seriesProposalRepository.GetLatestForSeriesBatchAsync(
                    seriesIds, cancellationToken)
                : (IReadOnlyList<SeriesProposal>)Array.Empty<SeriesProposal>();

            var latestBySeriesId = latestProposals
                .GroupBy(p => p.SeriesId)
                .ToDictionary(g => g.Key, g => g.First());

            var recentSeries = data.RecentSeriesActivity
                .Select(s =>
                {
                    latestBySeriesId.TryGetValue(s.SeriesId, out var latest);
                    var latestProposalId = latest?.SeriesProposalId;
                    var latestProposalStatusCode = latest?.StatusCode;

                    return new EditorDashboardSeriesActivityDto(
                        s.SeriesId,
                        s.Title,
                        s.Slug,
                        s.StatusCode,
                        ResolveLatestChapterLabel(s),
                        s.UpdatedAtUtc ?? s.CreatedAtUtc,
                        latestProposalId,
                        latestProposalStatusCode,
                        SeriesNavigationPolicy.CanOpenSeriesSlugPage(
                            s.StatusCode, s.Slug, latestProposalId, latestProposalStatusCode));
                })
                .ToList();

            return new EditorDashboardDto(
                data.PendingProposalCount,
                data.ChaptersUnderReviewCount,
                data.PendingAnnotationCount,
                data.SerializedSeriesCount,
                proposalQueue,
                recentSeries);
        }

        /// <summary>
        /// Latest chapter label = the most recently created chapter's number label, or null
        /// when the series has no chapters yet.
        /// </summary>
        private static string? ResolveLatestChapterLabel(MangaManagementSystem.Domain.Entities.Series series)
        {
            if (series.Chapters is null || series.Chapters.Count == 0)
            {
                return null;
            }

            return series.Chapters
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => c.ChapterNumberLabel)
                .FirstOrDefault();
        }
    }
}
