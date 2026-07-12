using System.IO.Compression;
using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Application.Common
{
    public sealed record ValidatedRegistrationPortfolio(
        byte[] Bytes,
        string FileName,
        string ContentType);

    public sealed class RegistrationPortfolioValidationException
        : InvalidOperationException
    {
        public RegistrationPortfolioValidationException(
            string errorCode,
            string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }

    public static class RegistrationPortfolioFileValidator
    {
        public const long MaxFileSizeBytes =
            10L * 1024L * 1024L;

        public static ValidatedRegistrationPortfolio Validate(
            byte[]? fileBytes,
            string? fileName,
            string? contentType)
        {
            if (fileBytes is null || fileBytes.Length == 0)
            {
                throw Invalid(
                    "The portfolio file is empty.");
            }

            if (fileBytes.LongLength > MaxFileSizeBytes)
            {
                throw new RegistrationPortfolioValidationException(
                    AuthErrorCodes.PortfolioFileTooLarge,
                    "The portfolio file is too large. The maximum size is 10 MB.");
            }

            var safeFileName =
                NormalizeFileName(fileName);

            var extension =
                Path.GetExtension(safeFileName)
                    .ToLowerInvariant();

            var normalizedContentType =
                (contentType ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            var canonicalContentType =
                extension switch
                {
                    ".pdf" when normalizedContentType == "application/pdf"
                        && HasPrefix(
                            fileBytes,
                            new byte[]
                            {
                                0x25, 0x50, 0x44, 0x46, 0x2D
                            }) =>
                            "application/pdf",

                    ".docx" when normalizedContentType ==
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        && IsValidDocx(fileBytes) =>
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                    ".png" when normalizedContentType == "image/png"
                        && HasPrefix(
                            fileBytes,
                            new byte[]
                            {
                                0x89, 0x50, 0x4E, 0x47,
                                0x0D, 0x0A, 0x1A, 0x0A
                            }) =>
                            "image/png",

                    ".jpg" or ".jpeg"
                        when normalizedContentType is "image/jpeg" or "image/jpg"
                        && HasPrefix(
                            fileBytes,
                            new byte[] { 0xFF, 0xD8, 0xFF }) =>
                            "image/jpeg",

                    ".webp" when normalizedContentType == "image/webp"
                        && IsValidWebp(fileBytes) =>
                            "image/webp",

                    _ => throw Invalid(
                        "Unsupported or invalid portfolio file. Allowed formats are PDF, DOCX, PNG, JPG/JPEG, and WEBP.")
                };

            return new ValidatedRegistrationPortfolio(
                fileBytes,
                safeFileName,
                canonicalContentType);
        }

        private static string NormalizeFileName(
            string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw Invalid(
                    "A portfolio file name is required.");
            }

            var normalizedPath =
                fileName.Replace('\\', '/');

            var separatorIndex =
                normalizedPath.LastIndexOf('/');

            var safeFileName =
                (separatorIndex >= 0
                    ? normalizedPath[(separatorIndex + 1)..]
                    : normalizedPath)
                .Trim();

            if (safeFileName.Length == 0
                || safeFileName.Length > 260
                || safeFileName.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0)
            {
                throw Invalid(
                    "The portfolio file name is invalid.");
            }

            return safeFileName;
        }

        private static bool IsValidDocx(
            byte[] bytes)
        {
            if (!HasPrefix(
                    bytes,
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
            {
                return false;
            }

            try
            {
                using var stream =
                    new MemoryStream(
                        bytes,
                        writable: false);

                using var archive =
                    new ZipArchive(
                        stream,
                        ZipArchiveMode.Read,
                        leaveOpen: false);

                return archive.GetEntry(
                            "[Content_Types].xml") is not null
                    && archive.GetEntry(
                            "word/document.xml") is not null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        private static bool IsValidWebp(
            byte[] bytes)
        {
            return bytes.Length >= 12
                && bytes[0] == (byte)'R'
                && bytes[1] == (byte)'I'
                && bytes[2] == (byte)'F'
                && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W'
                && bytes[9] == (byte)'E'
                && bytes[10] == (byte)'B'
                && bytes[11] == (byte)'P';
        }

        private static bool HasPrefix(
            byte[] bytes,
            byte[] prefix)
        {
            return bytes.Length >= prefix.Length
                && bytes.AsSpan(0, prefix.Length)
                    .SequenceEqual(prefix);
        }

        private static RegistrationPortfolioValidationException
            Invalid(string message)
        {
            return new RegistrationPortfolioValidationException(
                AuthErrorCodes.InvalidPortfolioFile,
                message);
        }
    }
}
