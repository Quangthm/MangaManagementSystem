using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Commands.SubmitSeriesProposal
{
    /// <summary>
    /// Handles BF-SERIES-003 — Submit Series Proposal for Editorial Review.
    ///
    /// Orchestration:
    ///   1. Validate command inputs.
    ///   2. Upload proposal file to Cloudinary via IFileStorageService (computes SHA-256).
    ///   3. Call manga.usp_SeriesProposal_Submit through ISeriesProposalRepository.
    ///   4. If SQL fails after Cloudinary succeeds, attempt best-effort Cloudinary cleanup.
    ///   5. Return SeriesProposalSubmittedDto on success.
    ///
    /// The stored procedure owns: FileResource creation (purpose SERIES_PROPOSAL),
    /// SeriesProposal row creation, Series status transition to UNDER_EDITORIAL_REVIEW,
    /// permission checks, and audit event. The handler must NOT pass title/synopsis/genre.
    ///
    /// SERIES_PROPOSAL files are document-only (PDF/DOC/DOCX). Cloudinary resource type
    /// for cleanup is "raw" (non-image documents). This is MVP behavior; mixed-type
    /// cleanup can be revisited when non-document proposals are allowed.
    /// </summary>
    public sealed class SubmitSeriesProposalCommandHandler
        : IRequestHandler<SubmitSeriesProposalCommand, SeriesProposalSubmittedDto>
    {
        private const string SeriesProposalPurpose = "SERIES_PROPOSAL";
        private const string CloudinaryRawResourceType = "raw";

        // Allowed proposal document extensions and MIME types (SERIES_PROPOSAL only).
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private const long MaxProposalFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly IFileStorageService _fileStorageService;
        private readonly ISeriesProposalRepository _seriesProposalRepository;
        private readonly ILogger<SubmitSeriesProposalCommandHandler> _logger;

        public SubmitSeriesProposalCommandHandler(
            IFileStorageService fileStorageService,
            ISeriesProposalRepository seriesProposalRepository,
            ILogger<SubmitSeriesProposalCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _seriesProposalRepository = seriesProposalRepository;
            _logger = logger;
        }

        public async Task<SeriesProposalSubmittedDto> Handle(
            SubmitSeriesProposalCommand command,
            CancellationToken cancellationToken)
        {
            // ── 1. Input validation ──────────────────────────────────────────────────
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to submit a series proposal.");
            }

            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to submit a proposal.");
            }

            if (command.ProposalFileBytes is not { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "A proposal document file is required to submit a series proposal.");
            }

            if (string.IsNullOrWhiteSpace(command.ProposalFileName))
            {
                throw new InvalidOperationException(
                    "The proposal file name is missing. Please re-select the file and try again.");
            }

            if (string.IsNullOrWhiteSpace(command.ProposalContentType))
            {
                throw new InvalidOperationException(
                    "The proposal file content type could not be determined. Please re-select the file.");
            }

            // ── 2. File type validation (backend enforcement) ────────────────────────
            if (command.ProposalFileBytes.Length > MaxProposalFileSizeBytes)
            {
                throw new InvalidOperationException(
                    "The proposal file exceeds the maximum allowed size of 10 MB.");
            }

            var extension = Path.GetExtension(command.ProposalFileName);
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only PDF, DOC, and DOCX files are accepted as proposal documents.");
            }

            if (!AllowedContentTypes.Contains(command.ProposalContentType))
            {
                throw new InvalidOperationException(
                    "The proposal file type is not accepted. Please upload a PDF, DOC, or DOCX document.");
            }

            // ── 3. Upload to Cloudinary (outside SQL transaction) ────────────────────
            // IFileStorageService uploads and computes SHA-256. It does NOT create a
            // FileResource row — that is done inside manga.usp_SeriesProposal_Submit.
            var uploadResult = await _fileStorageService.UploadFileAsync(
                command.ProposalFileBytes,
                command.ProposalFileName,
                command.ProposalContentType,
                SeriesProposalPurpose,
                uploadedByUserId: null);

            if (string.IsNullOrWhiteSpace(uploadResult.Sha256Hash))
            {
                // Sha256Hash is required by the stored procedure. If the upload service
                // did not compute it, we must not proceed to SQL.
                await TryCleanupCloudinaryAsync(uploadResult.PublicId,
                    "SHA-256 hash was not returned by the file storage service.");
                throw new InvalidOperationException(
                    "The proposal file integrity check could not be completed. Please try again.");
            }

            // ── 4. Call stored procedure through repository ──────────────────────────
            Guid seriesProposalId;
            short proposalVersionNo;

            try
            {
                (seriesProposalId, proposalVersionNo) =
                    await _seriesProposalRepository.SubmitSeriesProposalViaProcAsync(
                        seriesId: command.SeriesId,
                        submittedByUserId: command.ActorUserId,
                        originalFileName: uploadResult.OriginalFileName,
                        cloudinaryPublicId: uploadResult.PublicId,
                        cloudinarySecureUrl: uploadResult.SecureUrl,
                        contentType: uploadResult.ContentType,
                        fileSizeBytes: uploadResult.FileSizeBytes,
                        sha256Hash: uploadResult.Sha256Hash,
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Cloudinary upload already succeeded. Attempt best-effort cleanup
                // so the file does not become an orphan in Cloudinary storage.
                await TryCleanupCloudinaryAsync(uploadResult.PublicId,
                    $"SQL workflow failed after Cloudinary upload for series {command.SeriesId}.");

                _logger.LogError(ex,
                    "Failed to submit series proposal for series {SeriesId} by actor {ActorUserId}.",
                    command.SeriesId,
                    command.ActorUserId);

                // Re-throw: caller (controller) maps InvalidOperationException to 400/409.
                throw;
            }

            return new SeriesProposalSubmittedDto
            {
                SeriesId = command.SeriesId,
                SeriesProposalId = seriesProposalId,
                ProposalVersionNo = proposalVersionNo,
                SeriesStatusCode = "UNDER_EDITORIAL_REVIEW",
                ProposalStatusCode = "UNDER_EDITORIAL_REVIEW"
            };
        }

        /// <summary>
        /// Best-effort Cloudinary cleanup after a SQL workflow failure.
        /// SERIES_PROPOSAL files are documents — always "raw" resource type in Cloudinary.
        /// Cleanup failure is logged safely but never rethrown (the original business error
        /// is what the caller needs to handle).
        /// </summary>
        private async Task TryCleanupCloudinaryAsync(string publicId, string reason)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(publicId, CloudinaryRawResourceType);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(
                    cleanupEx,
                    "Failed to clean up uploaded proposal file {PublicId} from Cloudinary after failure. Reason: {Reason}",
                    publicId,
                    reason);
            }
        }
    }
}
