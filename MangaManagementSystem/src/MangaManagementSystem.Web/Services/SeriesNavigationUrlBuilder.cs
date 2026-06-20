using System;

namespace MangaManagementSystem.Web.Services
{
    /// <summary>
    /// Builds editor-facing series/proposal navigation URLs from DTO-provided flags.
    /// Does not call Application business policy directly — slug eligibility is pre-computed
    /// by the Application handler and exposed as <c>canOpenSeriesSlugPage</c>.
    ///
    /// Priority:
    ///   1. Slug page when eligible.
    ///   2. Proposal detail when a latest proposal exists.
    ///   3. Null (plain text / disabled state).
    /// </summary>
    public static class SeriesNavigationUrlBuilder
    {
        public static string? BuildEditorSeriesOrProposalHref(
            bool canOpenSeriesSlugPage,
            string? seriesSlug,
            Guid? latestProposalId,
            string returnUrl)
        {
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

            if (canOpenSeriesSlugPage && !string.IsNullOrWhiteSpace(seriesSlug))
            {
                return $"/series/{seriesSlug}?returnUrl={encodedReturnUrl}";
            }

            if (latestProposalId.HasValue)
            {
                return $"/editor/proposals/{latestProposalId.Value}?returnUrl={encodedReturnUrl}";
            }

            return null;
        }
    }
}
