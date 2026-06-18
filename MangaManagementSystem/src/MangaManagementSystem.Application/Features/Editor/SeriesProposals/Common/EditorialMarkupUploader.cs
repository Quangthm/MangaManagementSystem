using System;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common
{
    /// <summary>
    /// Shared orchestration for editorial markup attachments (file_purpose_code
    /// EDITORIAL_ATTACHMENT). Validates and uploads markup to Cloudinary outside the SQL
    /// transaction, and provides best-effort cleanup when the subsequent stored-procedure
    /// workflow fails. Used by the Request Revision, Pass To Board, and Cancel handlers.
    ///
    /// File-storage details (Cloudinary) live in Infrastructure behind IFileStorageService;
    /// this helper only orchestrates through that Application-facing abstraction.
    /// </summary>
    internal static class EditorialMarkupUploader
    {
        /// <summary>
        /// Validates markup metadata, then uploads the bytes to Cloudinary.
        /// Throws InvalidOperationException with a user-safe message on validation failure
        /// or when the integrity hash is missing (in which case the upload is cleaned up).
        /// </summary>
        public static async Task<FileUploadResultDto> ValidateAndUploadAsync(
            IFileStorageService fileStorageService,
            byte[] fileBytes,
            string? fileName,
            string? contentType)
        {
            EditorialMarkupValidation.Validate(fileBytes, fileName ?? string.Empty, contentType ?? string.Empty);

            var uploadResult = await fileStorageService.UploadFileAsync(
                fileBytes,
                fileName!,
                contentType!,
                EditorialMarkupValidation.EditorialAttachmentPurpose,
                uploadedByUserId: null);

            if (string.IsNullOrWhiteSpace(uploadResult.Sha256Hash))
            {
                // The stored procedure requires a hash for the FileResource. Clean up and abort.
                try
                {
                    await fileStorageService.DeleteFileAsync(
                        uploadResult.PublicId,
                        EditorialMarkupValidation.ResolveResourceType(contentType!));
                }
                catch
                {
                    // best-effort cleanup; ignore
                }

                throw new InvalidOperationException(
                    "The markup file integrity check could not be completed. Please try again.");
            }

            return uploadResult;
        }

        /// <summary>
        /// Best-effort Cloudinary cleanup after a SQL workflow failure. Never rethrows — the
        /// original business error is what the caller needs to surface.
        /// </summary>
        public static async Task TryCleanupAsync(
            IFileStorageService fileStorageService,
            ILogger logger,
            FileUploadResultDto markup,
            string reason)
        {
            try
            {
                await fileStorageService.DeleteFileAsync(
                    markup.PublicId,
                    EditorialMarkupValidation.ResolveResourceType(markup.ContentType));
            }
            catch (Exception cleanupEx)
            {
                logger.LogError(
                    cleanupEx,
                    "Failed to clean up uploaded markup file {PublicId} from Cloudinary. Reason: {Reason}",
                    markup.PublicId,
                    reason);
            }
        }
    }
}
