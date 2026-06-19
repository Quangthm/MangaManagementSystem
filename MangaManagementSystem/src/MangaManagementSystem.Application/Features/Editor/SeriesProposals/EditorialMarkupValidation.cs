using System;
using System.Collections.Generic;
using System.IO;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals
{
    /// <summary>
    /// Shared validation rules for editorial markup attachments (file_purpose_code
    /// EDITORIAL_ATTACHMENT). Markup may be a document (PDF/DOC/DOCX) or an image
    /// (JPG/JPEG/PNG/WEBP). Used by the Request Revision, Pass To Board, and Cancel handlers.
    /// </summary>
    internal static class EditorialMarkupValidation
    {
        public const string EditorialAttachmentPurpose = "EDITORIAL_ATTACHMENT";
        public const long MaxMarkupFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private static readonly HashSet<string> DocumentContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        /// <summary>
        /// Validates markup file metadata. Throws InvalidOperationException with a user-safe
        /// message when the file is too large or of an unsupported type.
        /// </summary>
        public static void Validate(byte[] fileBytes, string fileName, string contentType)
        {
            if (fileBytes is not { Length: > 0 })
            {
                throw new InvalidOperationException("The markup file is empty. Please re-select the file.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new InvalidOperationException("The markup file name is missing. Please re-select the file.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new InvalidOperationException(
                    "The markup file content type could not be determined. Please re-select the file.");
            }

            if (fileBytes.Length > MaxMarkupFileSizeBytes)
            {
                throw new InvalidOperationException(
                    "The markup file exceeds the maximum allowed size of 10 MB.");
            }

            var extension = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only PDF, DOC, DOCX, JPG, PNG, and WEBP files are accepted as markup attachments.");
            }

            if (!ImageContentTypes.Contains(contentType) && !DocumentContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException(
                    "The markup file type is not accepted. Please upload a PDF, DOC, DOCX, or image file.");
            }
        }

        /// <summary>
        /// Cloudinary resource type used for upload and cleanup: "image" for image content
        /// types, otherwise "raw" for documents.
        /// </summary>
        public static string ResolveResourceType(string contentType) =>
            ImageContentTypes.Contains(contentType) ? "image" : "raw";
    }
}
