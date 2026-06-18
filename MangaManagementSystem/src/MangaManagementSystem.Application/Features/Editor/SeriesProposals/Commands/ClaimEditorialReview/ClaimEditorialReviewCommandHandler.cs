using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.ClaimEditorialReview
{
    /// <summary>
    /// Handles ClaimEditorialReviewCommand. Validates inputs and calls
    /// manga.usp_SeriesProposal_ClaimEditorialReview through the repository. The stored procedure
    /// owns: active-Tantou-Editor check, eligibility guard (UNDER_EDITORIAL_REVIEW, not yet
    /// reviewed), contributor insert, and the audit event. No file upload involved.
    /// </summary>
    public sealed class ClaimEditorialReviewCommandHandler
        : IRequestHandler<ClaimEditorialReviewCommand, EditorReviewActionResultDto>
    {
        private readonly ISeriesProposalRepository _seriesProposalRepository;
        private readonly ILogger<ClaimEditorialReviewCommandHandler> _logger;

        public ClaimEditorialReviewCommandHandler(
            ISeriesProposalRepository seriesProposalRepository,
            ILogger<ClaimEditorialReviewCommandHandler> logger)
        {
            _seriesProposalRepository = seriesProposalRepository;
            _logger = logger;
        }

        public async Task<EditorReviewActionResultDto> Handle(
            ClaimEditorialReviewCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to claim a proposal for review.");
            }

            if (command.SeriesProposalId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid proposal must be selected to claim a review.");
            }

            // The repository maps known SQL business errors (wrong status, already reviewed,
            // not an active Tantou Editor, duplicate claim) to user-safe
            // InvalidOperationException messages, which the controller maps to HTTP 400.
            await _seriesProposalRepository.ClaimEditorialReviewAsync(
                command.SeriesProposalId,
                command.ActorUserId,
                command.Notes,
                cancellationToken);

            // Claim does not change the proposal status; it stays UNDER_EDITORIAL_REVIEW.
            return new EditorReviewActionResultDto(
                command.SeriesProposalId,
                "UNDER_EDITORIAL_REVIEW");
        }
    }
}
