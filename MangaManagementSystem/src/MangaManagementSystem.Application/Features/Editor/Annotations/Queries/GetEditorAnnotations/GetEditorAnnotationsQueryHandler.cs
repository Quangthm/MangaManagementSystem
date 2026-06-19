using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common.Constants;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.Annotations.Queries.GetEditorAnnotations
{
    public sealed class GetEditorAnnotationsQueryHandler
        : IRequestHandler<GetEditorAnnotationsQuery, EditorAnnotationWorkspaceDto>
    {
        private readonly IEditorAnnotationRepository _repository;

        public GetEditorAnnotationsQueryHandler(IEditorAnnotationRepository repository)
        {
            _repository = repository;
        }

        public async Task<EditorAnnotationWorkspaceDto> Handle(
            GetEditorAnnotationsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.ActorUserId, out var actorUserId))
            {
                throw new InvalidOperationException("Could not identify the requesting user.");
            }

            Guid? seriesId = null;
            if (Guid.TryParse(request.SeriesId, out var parsedSeriesId))
            {
                seriesId = parsedSeriesId;
            }

            string? resolvedIssueType = request.IssueType;
            if (!string.IsNullOrWhiteSpace(resolvedIssueType) &&
                !string.Equals(resolvedIssueType, "all", StringComparison.OrdinalIgnoreCase) &&
                !ChapterPageAnnotationIssueTypes.All.Contains(resolvedIssueType, StringComparer.OrdinalIgnoreCase))
            {
                resolvedIssueType = null;
            }

            var data = await _repository.GetAnnotationsAsync(
                actorUserId, seriesId, resolvedIssueType, request.Status, cancellationToken);

            var seriesGroups = data.SeriesGroups
                .Select(g => new EditorAnnotationSeriesGroupDto(
                    g.SeriesId,
                    g.SeriesTitle,
                    g.SeriesSlug,
                    g.Annotations.Select(a =>
                    {
                        string? slug = g.SeriesSlug;
                        string? workspaceUrl = !string.IsNullOrWhiteSpace(slug)
                            ? $"/series/{slug}/workspace?chapterId={a.ChapterId}&returnUrl={Uri.EscapeDataString("/editor/annotations")}"
                            : null;

                        return new EditorAnnotationRowDto(
                            a.AnnotationId,
                            a.ChapterId,
                            a.ChapterNumberLabel,
                            a.ChapterTitle,
                            a.ChapterPageId,
                            a.PageNumber,
                            a.ChapterPageVersionId,
                            a.VersionNo,
                            a.IssueTypeCode,
                            a.AnnotationText,
                            a.IsResolved,
                            a.CreatedAtUtc,
                            a.ResolvedAtUtc,
                            workspaceUrl,
                            a.Regions.Select(r => new EditorAnnotationRegionDto(
                                r.PageRegionId,
                                r.RegionTypeCode,
                                r.X,
                                r.Y,
                                r.Width,
                                r.Height)).ToList());
                    }).ToList()))
                .ToList();

            return new EditorAnnotationWorkspaceDto(
                data.OpenCount,
                data.ResolvedCount,
                data.PagesWithIssuesCount,
                data.DistinctIssueTypeCount,
                data.SeriesFilters
                    .Select(s => new EditorAnnotationSeriesFilterDto(s.SeriesId, s.SeriesTitle))
                    .ToList(),
                ChapterPageAnnotationIssueTypes.All.ToList(),
                seriesGroups);
        }
    }
}
