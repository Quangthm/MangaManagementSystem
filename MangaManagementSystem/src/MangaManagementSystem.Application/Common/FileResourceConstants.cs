using System;
using System.Collections.Generic;
using System.Linq;

namespace MangaManagementSystem.Application.Common
{
    public static class FilePurposeCodes
    {
        public const string SeriesProposal =
            "SERIES_PROPOSAL";

        public const string SeriesCover =
            "SERIES_COVER";

        public const string ChapterPageVersion =
            "CHAPTER_PAGE_VERSION";

        public const string EditorialAttachment =
            "EDITORIAL_ATTACHMENT";

        public const string RegistrationPortfolio =
            "REGISTRATION_PORTFOLIO";

        public const string UserAvatar =
            "USER_AVATAR";

        public static IReadOnlyList<string> All { get; } =
            Array.AsReadOnly(
                new[]
                {
                    SeriesProposal,
                    SeriesCover,
                    ChapterPageVersion,
                    EditorialAttachment,
                    RegistrationPortfolio,
                    UserAvatar
                });

        public static bool IsSupported(
            string? filePurposeCode)
        {
            return !string.IsNullOrWhiteSpace(
                    filePurposeCode)
                && All.Contains(
                    filePurposeCode.Trim(),
                    StringComparer.Ordinal);
        }
    }

    public static class FilePurposeLabels
    {
        public const string SeriesProposal =
            "Series Proposal";

        public const string SeriesCover =
            "Series Cover";

        public const string ChapterPageVersion =
            "Chapter Page Version";

        public const string EditorialAttachment =
            "Editorial Attachment";

        public const string RegistrationPortfolio =
            "Registration Portfolio";

        public const string UserAvatar =
            "User Avatar";

        public static string GetLabel(
            string? filePurposeCode)
        {
            if (string.IsNullOrWhiteSpace(
                    filePurposeCode))
            {
                return "Unknown";
            }

            var normalizedCode =
                filePurposeCode.Trim();

            return normalizedCode switch
            {
                FilePurposeCodes.SeriesProposal =>
                    SeriesProposal,

                FilePurposeCodes.SeriesCover =>
                    SeriesCover,

                FilePurposeCodes.ChapterPageVersion =>
                    ChapterPageVersion,

                FilePurposeCodes.EditorialAttachment =>
                    EditorialAttachment,

                FilePurposeCodes.RegistrationPortfolio =>
                    RegistrationPortfolio,

                FilePurposeCodes.UserAvatar =>
                    UserAvatar,

                _ => FormatUnknownCode(
                    normalizedCode)
            };
        }

        private static string FormatUnknownCode(
            string value)
        {
            return string.Join(
                " ",
                value.Split(
                        '_',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        part =>
                            char.ToUpperInvariant(part[0])
                            + part[1..].ToLowerInvariant()));
        }
    }

    public static class AdminFileDeletionStates
    {
        public const string Active =
            "ACTIVE";

        public const string Deleted =
            "DELETED";

        public const string All =
            "ALL";

        public const string ActiveNormalized =
            "active";

        public const string DeletedNormalized =
            "deleted";

        public const string AllNormalized =
            "all";

        public static IReadOnlyList<string> Supported { get; } =
            Array.AsReadOnly(
                new[]
                {
                    Active,
                    Deleted,
                    All
                });

        public static bool IsSupported(
            string? deletedState)
        {
            return !string.IsNullOrWhiteSpace(
                    deletedState)
                && Supported.Contains(
                    deletedState.Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class AdminFileDeletionStateLabels
    {
        public const string Active =
            "Active";

        public const string Deleted =
            "Deleted";

        public const string All =
            "All";

        public static string GetLabel(
            string? deletedState)
        {
            return deletedState?
                .Trim()
                .ToUpperInvariant() switch
            {
                AdminFileDeletionStates.Active =>
                    Active,

                AdminFileDeletionStates.Deleted =>
                    Deleted,

                AdminFileDeletionStates.All =>
                    All,

                _ => "Unknown"
            };
        }
    }

    public static class FileAvailabilityCodes
    {
        public const string Missing =
            "missing";

        public const string Deleted =
            "deleted";

        public const string Unavailable =
            "unavailable";

        public static string NormalizePlaceholderReason(
            string? reason)
        {
            return string.Equals(
                    reason?.Trim(),
                    Deleted,
                    StringComparison.OrdinalIgnoreCase)
                ? Deleted
                : Missing;
        }
    }

    public static class FileAvailabilityLabels
    {
        public const string Missing =
            "Missing";

        public const string Deleted =
            "Deleted";

        public const string Unavailable =
            "Unavailable";

        public static string GetLabel(
            string? availabilityCode)
        {
            return availabilityCode?
                .Trim()
                .ToLowerInvariant() switch
            {
                FileAvailabilityCodes.Missing =>
                    Missing,

                FileAvailabilityCodes.Deleted =>
                    Deleted,

                FileAvailabilityCodes.Unavailable =>
                    Unavailable,

                _ => Unavailable
            };
        }
    }

    public static class FileAvailabilityHeaders
    {
        public const string PlaceholderReason =
            "X-File-Placeholder";
    }
}
