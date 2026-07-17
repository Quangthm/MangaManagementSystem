using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.PassProposalToBoard
{
    /// <summary>
    /// Handles the Pass To Board editorial decision.
    ///
    /// Orchestration:
    ///   1. Validate inputs (comments optional; markup optional).
    ///   2. If a markup file is supplied, upload it to Cloudinary via IFileStorageService.
    ///   3. Open the shared Unit of Work transaction.
    ///   4. Call manga.usp_SeriesProposal_PassToBoard through the repository wrapper.
    ///   5. Add PROPOSAL_DECISION notifications for active Mangaka contributors.
    ///   6. Save EF notifications and commit the shared transaction.
    ///   7. If the workflow fails after a Cloudinary upload, roll back and attempt cleanup.
    ///
    /// The stored procedure transitions the proposal and series to UNDER_BOARD_REVIEW only —
    /// it never sets APPROVED.
    /// </summary>
    public sealed class PassProposalToBoardCommandHandler
        : IRequestHandler<PassProposalToBoardCommand, EditorReviewActionResultDto>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PassProposalToBoardCommandHandler> _logger;

        public PassProposalToBoardCommandHandler(
            IFileStorageService fileStorageService,
            IUnitOfWork unitOfWork,
            ILogger<PassProposalToBoardCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EditorReviewActionResultDto> Handle(
            PassProposalToBoardCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to pass a proposal to the board.");
            }

            if (command.SeriesProposalId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid proposal must be selected.");
            }

            string? comments =
                string.IsNullOrWhiteSpace(command.Comments)
                    ? null
                    : command.Comments.Trim();

            FileUploadResultDto? markup = null;

            if (command.MarkupFileBytes is { Length: > 0 })
            {
                markup = await EditorialMarkupUploader.ValidateAndUploadAsync(
                    _fileStorageService,
                    command.MarkupFileBytes!,
                    command.MarkupFileName,
                    command.MarkupContentType);
            }

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

                await _unitOfWork.SeriesProposals.PassToBoardAsync(
                    command.SeriesProposalId,
                    command.ActorUserId,
                    comments,
                    markup?.OriginalFileName,
                    markup?.PublicId,
                    markup?.SecureUrl,
                    markup?.ContentType,
                    markup?.FileSizeBytes,
                    markup?.Sha256Hash,
                    cancellationToken);

                await ProposalDecisionNotificationSupport
                    .AddForActiveMangakaContributorsAsync(
                        _unitOfWork,
                        proposal.SeriesId,
                        command.SeriesProposalId,
                        "Proposal Passed to Board",
                        "Your proposal passed editorial review and is now under board review.");

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                if (markup is not null)
                {
                    await EditorialMarkupUploader.TryCleanupAsync(
                        _fileStorageService,
                        _logger,
                        markup,
                        $"Workflow failed after markup upload for proposal {command.SeriesProposalId} (pass to board).");
                }

                _logger.LogWarning(
                    ex,
                    "Pass to board failed for proposal {SeriesProposalId} by actor {ActorUserId}.",
                    command.SeriesProposalId,
                    command.ActorUserId);

                throw;
            }

            return new EditorReviewActionResultDto(
                command.SeriesProposalId,
                "UNDER_BOARD_REVIEW");
        }
    }
}
