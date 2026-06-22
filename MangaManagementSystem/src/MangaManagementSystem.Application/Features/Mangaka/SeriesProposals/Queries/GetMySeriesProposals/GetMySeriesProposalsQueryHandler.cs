using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Queries.GetMySeriesProposals
{
    /// <summary>
    /// Handles GetMySeriesProposalsQuery by fetching proposals scoped to the actor's active
    /// Mangaka contributor memberships and mapping to read-only DTOs.
    /// </summary>
    public sealed class GetMySeriesProposalsQueryHandler
        : IRequestHandler<GetMySeriesProposalsQuery, IReadOnlyList<MangakaSeriesProposalDto>>
    {
        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetMySeriesProposalsQueryHandler(ISeriesProposalRepository seriesProposalRepository)
        {
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<IReadOnlyList<MangakaSeriesProposalDto>> Handle(
            GetMySeriesProposalsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                return Array.Empty<MangakaSeriesProposalDto>();
            }

            var proposals = await _seriesProposalRepository.GetMySeriesProposalsAsync(
                request.ActorUserId,
                cancellationToken);

            return proposals.Select(MapToDto).ToList();
        }

        private static MangakaSeriesProposalDto MapToDto(Domain.Entities.SeriesProposal proposal)
        {
            return new MangakaSeriesProposalDto(
                SeriesProposalId: proposal.SeriesProposalId,
                SeriesId: proposal.SeriesId,
                SeriesSlug: proposal.Series?.Slug ?? string.Empty,
                SeriesTitle: proposal.Series?.Title ?? string.Empty,
                ProposalVersionNo: proposal.ProposalVersionNo,
                ProposalTitle: proposal.ProposalTitle,
                SynopsisSnapshot: proposal.SynopsisSnapshot,
                Genres: MapGenres(proposal.Series?.Genres),
                Tags: MapTags(proposal.Series?.Tags),
                StatusCode: proposal.StatusCode,
                SubmittedAtUtc: proposal.SubmittedAtUtc,
                WithdrawnAtUtc: proposal.WithdrawnAtUtc,
                ReviewedAtUtc: proposal.ReviewedAtUtc,
                Comments: proposal.Comments,
                SubmittedByDisplayName: proposal.SubmittedByUser?.DisplayName ?? string.Empty,
                ReviewedByDisplayName: proposal.ReviewedByUser?.DisplayName,
                ProposalFile: new ProposalFileRefDto(
                    FileResourceId: proposal.ProposalFile!.FileResourceId,
                    OriginalFileName: proposal.ProposalFile.OriginalFileName,
                    ContentType: proposal.ProposalFile.ContentType,
                    FileSizeBytes: proposal.ProposalFile.FileSizeBytes,
                    SecureUrl: proposal.ProposalFile.CloudinarySecureUrl),
                MarkupFile: proposal.MarkupFile is null
                    ? null
                    : new ProposalFileRefDto(
                        FileResourceId: proposal.MarkupFile.FileResourceId,
                        OriginalFileName: proposal.MarkupFile.OriginalFileName,
                        ContentType: proposal.MarkupFile.ContentType,
                        FileSizeBytes: proposal.MarkupFile.FileSizeBytes,
                        SecureUrl: proposal.MarkupFile.CloudinarySecureUrl));
        }

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
