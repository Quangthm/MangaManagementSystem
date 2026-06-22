using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorialProposalQueue
{
    /// <summary>
    /// Handles GetEditorialProposalQueueQuery by reading the editorial queue (filterable by
    /// status) and mapping each proposal to a read-only ProposalQueueItemDto.
    /// </summary>
    public sealed class GetEditorialProposalQueueQueryHandler
        : IRequestHandler<GetEditorialProposalQueueQuery, IReadOnlyList<ProposalQueueItemDto>>
    {
        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetEditorialProposalQueueQueryHandler(ISeriesProposalRepository seriesProposalRepository)
        {
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<IReadOnlyList<ProposalQueueItemDto>> Handle(
            GetEditorialProposalQueueQuery request,
            CancellationToken cancellationToken)
        {
            var statusCode = string.IsNullOrWhiteSpace(request.StatusCode) ? null : request.StatusCode;

            var proposals = await _seriesProposalRepository.GetEditorialQueueAsync(
                statusCode,
                seriesId: null,
                submittedByUserId: null,
                reviewedByUserId: null,
                cancellationToken);

            return proposals.Select(MapToDto).ToList();
        }

        private static ProposalQueueItemDto MapToDto(SeriesProposal p) => new(
            p.SeriesProposalId,
            p.SeriesId,
            p.Series?.Title ?? string.Empty,
            p.Series?.Slug ?? string.Empty,
            p.ProposalVersionNo,
            p.ProposalTitle,
            p.SynopsisSnapshot,
            MapGenres(p.Series?.Genres),
            MapTags(p.Series?.Tags),
            p.StatusCode,
            p.SubmittedByUserId,
            p.SubmittedByUser?.DisplayName ?? string.Empty,
            p.SubmittedAtUtc,
            p.ReviewedByUserId,
            p.ReviewedByUser?.DisplayName,
            p.ReviewedAtUtc,
            p.Comments,
            p.ProposalFileId,
            p.ProposalFile?.CloudinarySecureUrl,
            p.ProposalFile?.OriginalFileName,
            p.MarkupFileId,
            p.MarkupFile?.CloudinarySecureUrl);

        private static IReadOnlyList<GenreDto> MapGenres(IEnumerable<Domain.Entities.Genre>? genres)
        {
            return genres?
                .OrderBy(g => g.GenreName)
                .Select(g => new GenreDto(g.GenreId, g.GenreName))
                .ToList()
                ?? new List<GenreDto>();
        }

        private static IReadOnlyList<TagDto> MapTags(IEnumerable<Domain.Entities.Tag>? tags)
        {
            return tags?
                .OrderBy(t => t.TagName)
                .Select(t => new TagDto(t.TagId, t.TagName))
                .ToList()
                ?? new List<TagDto>();
        }
    }
}
