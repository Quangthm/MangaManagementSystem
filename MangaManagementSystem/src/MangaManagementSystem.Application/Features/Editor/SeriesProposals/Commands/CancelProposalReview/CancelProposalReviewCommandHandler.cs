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
    ///   3. Call manga.usp_SeriesProposal_CancelEditorialReview through the repository wrapper.
    ///   4. If SQL fails after the Cloudinary upload, attempt best-effort cleanup.
    ///
    /// The stored procedure owns: comments-required guard, eligibility/contributor checks,
    /// required EDITORIAL_ATTACHMENT FileResource creation, status transitions, and audit.
    /// Cancellation links the markup to SeriesProposal.MarkupFileId.
    /// </summary>
    public sealed class CancelProposalReviewCommandHandler
        : IRequestHandler<CancelProposalReviewCommand, EditorReviewActionResultDto>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ISeriesProposalRepository _seriesProposalRepository;
        private readonly ILogger<CancelProposalReviewCommandHandler> _logger;

        public CancelProposalReviewCommandHandler(
            IFileStorageService fileStorageService,
            ISeriesProposalRepository seriesProposalRepository,
            ILogger<CancelProposalReviewCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _seriesProposalRepository = seriesProposalRepository;
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

            // Required markup upload (Cloudinary, outside the SQL transaction).
            FileUploadResultDto markup = await EditorialMarkupUploader.ValidateAndUploadAsync(
                _fileStorageService,
                command.MarkupFileBytes,
                command.MarkupFileName,
                command.MarkupContentType);

            try
            {
                await _seriesProposalRepository.CancelProposalAsync(
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
            }
            catch (Exception ex)
            {
                // The repository maps known SQL business errors to user-safe
                // InvalidOperationException messages. Whatever the failure, clean up the
                // already-uploaded markup so it does not orphan in Cloudinary.
                await EditorialMarkupUploader.TryCleanupAsync(
                    _fileStorageService, _logger, markup,
                    $"Workflow failed after markup upload for proposal {command.SeriesProposalId} (cancel).");

                _logger.LogError(ex,
                    "Failed to cancel proposal {SeriesProposalId} by actor {ActorUserId}.",
                    command.SeriesProposalId, command.ActorUserId);
                throw;
            }

            return new EditorReviewActionResultDto(command.SeriesProposalId, "CANCELLED");
        }
    }
}
