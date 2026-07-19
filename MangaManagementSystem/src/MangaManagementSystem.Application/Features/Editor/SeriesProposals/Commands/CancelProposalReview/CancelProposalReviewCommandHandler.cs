using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.CancelProposalReview
{
    /// <summary>
    /// Handles the Cancel Proposal editorial decision.
    ///
    /// Orchestration:
    ///   1. Validate inputs (comments required; markup file required).
    ///   2. Upload the markup file to Cloudinary via IFileStorageService.
    ///   3. Open the shared Unit of Work transaction.
    ///   4. Call manga.usp_SeriesProposal_CancelEditorialReview through the repository wrapper.
    ///   5. Add PROPOSAL_DECISION notifications for active Mangaka contributors.
    ///   6. Save EF notifications and commit the shared transaction.
    ///   7. If the workflow fails, roll back and attempt Cloudinary cleanup.
    ///
    /// The stored procedure owns: comments-required guard, eligibility/contributor checks,
    /// required EDITORIAL_ATTACHMENT FileResource creation, status transitions, and audit.
    /// Cancellation links the markup to SeriesProposal.MarkupFileId.
    /// </summary>
    public sealed class CancelProposalReviewCommandHandler
        : IRequestHandler<CancelProposalReviewCommand, EditorReviewActionResultDto>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelProposalReviewCommandHandler> _logger;

        public CancelProposalReviewCommandHandler(
            IFileStorageService fileStorageService,
            IUnitOfWork unitOfWork,
            ILogger<CancelProposalReviewCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EditorReviewActionResultDto> Handle(
            CancelProposalReviewCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to cancel a proposal.");
            }

            if (command.SeriesProposalId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid proposal must be selected.");
            }

            if (string.IsNullOrWhiteSpace(command.Comments))
            {
                throw new InvalidOperationException(
                    "Comments are required to cancel a proposal.");
            }

            if (command.MarkupFileBytes is not { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "A markup file is required to cancel a proposal.");
            }

            FileUploadResultDto markup =
                await EditorialMarkupUploader.ValidateAndUploadAsync(
                    _fileStorageService,
                    command.MarkupFileBytes,
                    command.MarkupFileName,
                    command.MarkupContentType);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var proposal =
                    await _unitOfWork.SeriesProposals.GetByIdWithDetailsAsync(
                        command.SeriesProposalId,
                        cancellationToken);

                if (proposal is null)
                {
                    throw new InvalidOperationException(
                        "The selected proposal could not be found.");
                }

                await _unitOfWork.SeriesProposals.CancelProposalAsync(
                    command.SeriesProposalId,
                    command.ActorUserId,
                    command.Comments.Trim(),
                    markup.OriginalFileName,
                    markup.PublicId,
                    markup.SecureUrl,
                    markup.ContentType,
                    markup.FileSizeBytes,
                    markup.Sha256Hash,
                    cancellationToken);

                await ProposalDecisionNotificationSupport
                    .AddForActiveMangakaContributorsAsync(
                        _unitOfWork,
                        proposal.SeriesId,
                        command.SeriesProposalId,
                        "Proposal Cancelled",
                        "Your proposal was cancelled during editorial review. Open the proposal detail to review the editor feedback.");

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                await EditorialMarkupUploader.TryCleanupAsync(
                    _fileStorageService,
                    _logger,
                    markup,
                    $"Workflow failed after markup upload for proposal {command.SeriesProposalId} (cancel).");

                _logger.LogError(
                    ex,
                    "Failed to cancel proposal {SeriesProposalId} by actor {ActorUserId}.",
                    command.SeriesProposalId,
                    command.ActorUserId);

                throw;
            }

            return new EditorReviewActionResultDto(
                command.SeriesProposalId,
                "CANCELLED");
        }
    }
}
