using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Dashboard.Queries.GetEditorDashboard
{
    /// <summary>
    /// Builds the Tantou Editor dashboard read model from the dashboard repository and maps
    /// Domain entities to API-facing DTOs. Pure read; no mutations.
    /// </summary>
    public sealed class GetEditorDashboardQueryHandler
        : IRequestHandler<GetEditorDashboardQuery, EditorDashboardDto>
    {
        // Preview limits for the dashboard tables.
        private const int ProposalQueueTake = 5;
        private const int RecentSeriesTake = 5;

        private readonly IEditorDashboardRepository _editorDashboardRepository;

        public GetEditorDashboardQueryHandler(IEditorDashboardRepository editorDashboardRepository)
        {
            _editorDashboardRepository = editorDashboardRepository;
        }

        public async Task<EditorDashboardDto> Handle(
            GetEditorDashboardQuery request, CancellationToken cancellationToken)
        {
            var data = await _editorDashboardRepository.GetDashboardDataAsync(
                ProposalQueueTake, RecentSeriesTake, cancellationToken);

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

            var recentSeries = data.RecentSeriesActivity
                .Select(s => new EditorDashboardSeriesActivityDto(
                    s.SeriesId,
                    s.Title,
                    s.Slug,
                    s.StatusCode,
                    ResolveLatestChapterLabel(s),
                    s.UpdatedAtUtc ?? s.CreatedAtUtc))
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
