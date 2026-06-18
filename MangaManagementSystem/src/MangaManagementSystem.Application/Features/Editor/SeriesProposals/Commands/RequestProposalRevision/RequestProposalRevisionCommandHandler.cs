using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.RequestProposalRevision
{
    /// <summary>
    /// Handles the Request Revision editorial decision.
    ///
    /// Orchestration:
    ///   1. Validate inputs (comments required; markup optional).
    ///   2. If a markup file is supplied, upload it to Cloudinary via IFileStorageService.
    ///   3. Call manga.usp_SeriesProposal_RequestRevision through the repository wrapper.
    ///   4. If SQL fails after a Cloudinary upload, attempt best-effort cleanup.
    ///
    /// The stored procedure owns: comments-required guard, eligibility/contributor checks,
    /// optional EDITORIAL_ATTACHMENT FileResource creation, status transitions, and audit.
    /// </summary>
    public sealed class RequestProposalRevisionCommandHandler
        : IRequestHandler<RequestProposalRevisionCommand, EditorReviewActionResultDto>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ISeriesProposalRepository _seriesProposalRepository;
        private readonly ILogger<RequestProposalRevisionCommandHandler> _logger;

        public RequestProposalRevisionCommandHandler(
            IFileStorageService fileStorageService,
            ISeriesProposalRepository seriesProposalRepository,
            ILogger<RequestProposalRevisionCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _seriesProposalRepository = seriesProposalRepository;
            _logger = logger;
        }

        public async Task<EditorReviewActionResultDto> Handle(
            RequestProposalRevisionCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to request a revision.");
            }

            if (command.SeriesProposalId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid proposal must be selected.");
            }

            if (string.IsNullOrWhiteSpace(command.Comments))
            {
                throw new InvalidOperationException(
                    "Comments are required to request a revision.");
            }

            // Optional markup upload (Cloudinary, outside the SQL transaction).
            FileUploadResultDto? markup = null;
            bool hasMarkup = command.MarkupFileBytes is { Length: > 0 };

            if (hasMarkup)
            {
                markup = await EditorialMarkupUploader.ValidateAndUploadAsync(
                    _fileStorageService,
                    command.MarkupFileBytes!,
                    command.MarkupFileName,
                    command.MarkupContentType);
            }

            try
            {
                await _seriesProposalRepository.RequestRevisionAsync(
                    command.SeriesProposalId,
                    command.ActorUserId,
                    command.Comments.Trim(),
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
                        $"Workflow failed after markup upload for proposal {command.SeriesProposalId} (request revision).");
                }

                _logger.LogWarning(ex,
                    "Request revision failed for proposal {SeriesProposalId} by actor {ActorUserId}.",
                    command.SeriesProposalId, command.ActorUserId);
                throw;
            }

            return new EditorReviewActionResultDto(command.SeriesProposalId, "REVISION_REQUESTED");
        }
    }
}
