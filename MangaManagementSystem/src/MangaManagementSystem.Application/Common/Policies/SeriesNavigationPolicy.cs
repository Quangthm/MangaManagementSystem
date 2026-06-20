using System;

namespace MangaManagementSystem.Application.Common.Policies
{
    /// <summary>
    /// Determines whether a series slug page is eligible for navigation.
    /// This is a business rule — no URLs, routes, or Web concerns.
    ///
    /// Allowed statuses:
    ///   SERIALIZED, HIATUS, COMPLETED — always allowed when slug exists.
    ///   CANCELLED — only when latest proposal exists and its status is APPROVED
    ///               (meaning the series was approved/serialized before cancellation).
    ///
    /// Not allowed:
    ///   PROPOSAL_DRAFT, UNDER_EDITORIAL_REVIEW, UNDER_BOARD_REVIEW,
    ///   CANCELLED without an approved latest proposal.
    /// </summary>
    public static class SeriesNavigationPolicy
    {
        public static bool CanOpenSeriesSlugPage(
            string? seriesStatusCode,
            string? seriesSlug,
            Guid? latestProposalId,
            string? latestProposalStatusCode)
        {
            if (string.IsNullOrWhiteSpace(seriesSlug))
            {
                return false;
            }

            if (seriesStatusCode is "SERIALIZED" or "HIATUS" or "COMPLETED")
            {
                return true;
            }

            return seriesStatusCode == "CANCELLED"
                && latestProposalId.HasValue
                && string.Equals(latestProposalStatusCode, "APPROVED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
