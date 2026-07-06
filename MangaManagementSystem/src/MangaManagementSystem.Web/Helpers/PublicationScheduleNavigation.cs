using System;

namespace MangaManagementSystem.Web.Helpers
{
    public static class PublicationScheduleNavigation
    {
        public static string BuildUrl(Guid? seriesId, string? seriesSlug = null, DateTime? anchorDate = null)
        {
            var date = anchorDate ?? DateTime.UtcNow.Date;
            var dateStr = date.ToString("yyyy-MM-dd");

            if (!string.IsNullOrWhiteSpace(seriesSlug))
                return $"/publication/schedule?series={Uri.EscapeDataString(seriesSlug)}&anchorDate={dateStr}";

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
                return $"/publication/schedule?seriesId={seriesId.Value}&anchorDate={dateStr}";

            return $"/publication/schedule?anchorDate={dateStr}";
        }
    }
}
