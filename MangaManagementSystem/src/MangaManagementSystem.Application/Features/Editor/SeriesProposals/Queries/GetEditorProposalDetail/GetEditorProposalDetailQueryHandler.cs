using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorProposalDetail
{
    /// <summary>
    /// Builds the editor proposal detail read model.
    ///
    /// Claim state rule: the editorial-review stored procedures represent "claimed" through an
    /// active Tantou Editor <see cref="SeriesContributor"/> membership, NOT through
    /// ReviewedByUserId. ReviewedByUserId / ReviewedAtUtc are only set when a final editorial
    /// decision (revision/pass/cancel) is recorded. Therefore:
    ///   - CurrentActorHasClaimed  = active Tantou Editor contributor membership
    ///   - HasEditorialDecision    = ReviewedAtUtc has a value
    ///
    /// Eligibility base = status is UNDER_EDITORIAL_REVIEW AND no editorial decision yet.
    /// </summary>
    public sealed class GetEditorProposalDetailQueryHandler
        : IRequestHandler<GetEditorProposalDetailQuery, EditorProposalDetailDto?>
    {
        private const string StatusUnderEditorialReview = "UNDER_EDITORIAL_REVIEW";

        private readonly ISeriesProposalRepository _seriesProposalRepository;

        public GetEditorProposalDetailQueryHandler(ISeriesProposalRepository seriesProposalRepository)
        {
            _seriesProposalRepository = seriesProposalRepository;
        }

        public async Task<EditorProposalDetailDto?> Handle(
            GetEditorProposalDetailQuery request,
            CancellationToken cancellationToken)
        {
            var proposal = await _seriesProposalRepository.GetByIdWithDetailsAsync(
                request.SeriesProposalId, cancellationToken);

            if (proposal is null)
            {
                return null;
            }

            bool currentActorIsActiveTantouEditorContributor =
                request.ActorUserId != Guid.Empty &&
                await _seriesProposalRepository.IsActiveTantouEditorContributorAsync(
                    proposal.SeriesId, request.ActorUserId, cancellationToken);

            // Claim is represented by active Tantou Editor contributor membership.
            bool currentActorHasClaimed = currentActorIsActiveTantouEditorContributor;

            // A completed editorial decision is marked by ReviewedAtUtc, never by mere claiming.
            bool hasEditorialDecision = proposal.ReviewedAtUtc.HasValue;

            bool isUnderEditorialReview =
                string.Equals(proposal.StatusCode, StatusUnderEditorialReview, StringComparison.Ordinal);

            bool seriesIsUnderEditorialReview =
                string.Equals(proposal.Series?.StatusCode, StatusUnderEditorialReview, StringComparison.Ordinal);

            // Actionable only when both the proposal row AND the current series are in
            // UNDER_EDITORIAL_REVIEW. A stale proposal row on a PROPOSAL_DRAFT series must
            // NOT show review actions.
            bool eligible = isUnderEditorialReview && seriesIsUnderEditorialReview && !hasEditorialDecision;

            // Claim: eligible, not yet claimed by this actor. The page is already restricted to
            // the Tantou Editor role; the claim stored procedure performs the authoritative check.
            bool canClaim = eligible && !currentActorHasClaimed;

            // Decision actions require the actor to be the active Tantou Editor contributor.
            bool canActOnDecision = eligible && currentActorHasClaimed;

            return new EditorProposalDetailDto(
                SeriesProposalId: proposal.SeriesProposalId,
                SeriesId: proposal.SeriesId,
                SeriesTitle: proposal.Series?.Title ?? string.Empty,
                SeriesSlug: proposal.Series?.Slug ?? string.Empty,
                SeriesCoverUrl: proposal.Series?.CoverFile?.CloudinarySecureUrl,
                ProposalVersionNo: proposal.ProposalVersionNo,
                ProposalTitle: proposal.ProposalTitle,
                Genres: MapGenres(proposal.Series?.Genres),
                Tags: MapTags(proposal.Series?.Tags),
                SynopsisSnapshot: proposal.SynopsisSnapshot,
                ProposalStatusCode: proposal.StatusCode,
                SeriesStatusCode: proposal.Series?.StatusCode,
                SubmittedByUserId: proposal.SubmittedByUserId,
                SubmitterDisplayName: proposal.SubmittedByUser?.DisplayName ?? string.Empty,
                SubmittedAtUtc: proposal.SubmittedAtUtc,
                ReviewedByUserId: proposal.ReviewedByUserId,
                ReviewerDisplayName: proposal.ReviewedByUser?.DisplayName,
                ReviewedAtUtc: proposal.ReviewedAtUtc,
                Comments: proposal.Comments,
                ProposalFileId: proposal.ProposalFileId,
                ProposalFileName: proposal.ProposalFile?.OriginalFileName,
                ProposalFileUrl: proposal.ProposalFile?.CloudinarySecureUrl,
                MarkupFileId: proposal.MarkupFileId,
                MarkupFileName: proposal.MarkupFile?.OriginalFileName,
                MarkupFileUrl: proposal.MarkupFile?.CloudinarySecureUrl,
                CurrentActorIsActiveTantouEditorContributor: currentActorIsActiveTantouEditorContributor,
                CurrentActorHasClaimed: currentActorHasClaimed,
                HasEditorialDecision: hasEditorialDecision,
                CanClaim: canClaim,
                CanRequestRevision: canActOnDecision,
                CanPassToBoard: canActOnDecision,
                CanCancel: canActOnDecision);
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
