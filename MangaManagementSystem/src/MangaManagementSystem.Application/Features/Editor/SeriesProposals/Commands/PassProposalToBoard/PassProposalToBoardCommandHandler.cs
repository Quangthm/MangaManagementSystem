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
    ///   3. Call manga.usp_SeriesProposal_PassToBoard through the repository wrapper.
    ///   4. If SQL fails after a Cloudinary upload, attempt best-effort cleanup.
    ///
    /// The stored procedure transitions the proposal and series to UNDER_BOARD_REVIEW only —
    /// it never sets APPROVED.
    /// </summary>
    public sealed class PassProposalToBoardCommandHandler
        : IRequestHandler<PassProposalToBoardCommand, EditorReviewActionResultDto>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ISeriesProposalRepository _seriesProposalRepository;
        private readonly ILogger<PassProposalToBoardCommandHandler> _logger;

        public PassProposalToBoardCommandHandler(
            IFileStorageService fileStorageService,
            ISeriesProposalRepository seriesProposalRepository,
            ILogger<PassProposalToBoardCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _seriesProposalRepository = seriesProposalRepository;
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

            // Comments are optional for Pass To Board.
            string? comments = string.IsNullOrWhiteSpace(command.Comments) ? null : command.Comments.Trim();

            // Optional markup upload (Cloudinary, outside the SQL transaction).
            FileUploadResultDto? markup = null;
            if (command.MarkupFileBytes is { Length: > 0 })
            {
                markup = await EditorialMarkupUploader.ValidateAndUploadAsync(
                    _fileStorageService,
                    command.MarkupFileBytes!,
                    command.MarkupFileName,
                    command.MarkupContentType);
            }

            try
            {
                await _seriesProposalRepository.PassToBoardAsync(
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
            }
            catch (Exception ex)
            {
                // The repository maps known SQL errors to user-safe InvalidOperationException.
                // Either way, clean up the orphaned Cloudinary upload before rethrowing.
                if (markup is not null)
                {
                    await EditorialMarkupUploader.TryCleanupAsync(
                        _fileStorageService, _logger, markup,
                        $"Workflow failed after markup upload for proposal {command.SeriesProposalId} (pass to board).");
                }

                _logger.LogWarning(ex,
                    "Pass to board failed for proposal {SeriesProposalId} by actor {ActorUserId}.",
                    command.SeriesProposalId, command.ActorUserId);
                throw;
            }

            return new EditorReviewActionResultDto(command.SeriesProposalId, "UNDER_BOARD_REVIEW");
        }
    }
}
