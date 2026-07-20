using System;
using System.Globalization;

namespace MangaManagementSystem.Application.Features.Publication.Schedule
{
    public enum PublicationScheduleNotificationEvent
    {
        None,
        Scheduled,
        Rescheduled
    }

    public sealed record PublicationScheduleNotificationContent(
        string Title,
        string Message);

    public static class PublicationScheduleNotificationSupport
    {
        private const string ScheduledStatus = "SCHEDULED";

        public static PublicationScheduleNotificationEvent Classify(
            string oldStatusCode,
            string newStatusCode,
            DateTime? oldPlannedReleaseDate,
            DateTime? newPlannedReleaseDate)
        {
            if (!string.Equals(oldStatusCode, ScheduledStatus, StringComparison.Ordinal)
                && string.Equals(newStatusCode, ScheduledStatus, StringComparison.Ordinal))
            {
                return PublicationScheduleNotificationEvent.Scheduled;
            }

            if (string.Equals(oldStatusCode, ScheduledStatus, StringComparison.Ordinal)
                && string.Equals(newStatusCode, ScheduledStatus, StringComparison.Ordinal)
                && NormalizeDate(oldPlannedReleaseDate) != NormalizeDate(newPlannedReleaseDate))
            {
                return PublicationScheduleNotificationEvent.Rescheduled;
            }

            return PublicationScheduleNotificationEvent.None;
        }

        public static PublicationScheduleNotificationContent BuildContent(
            PublicationScheduleNotificationEvent notificationEvent,
            string seriesTitle,
            string chapterNumberLabel,
            string? chapterTitle,
            DateTime plannedReleaseDate)
        {
            if (notificationEvent == PublicationScheduleNotificationEvent.None)
                throw new ArgumentOutOfRangeException(nameof(notificationEvent));

            var chapterLabel = string.IsNullOrWhiteSpace(chapterTitle)
                ? $"Chapter {chapterNumberLabel}"
                : $"Chapter {chapterNumberLabel}: {chapterTitle.Trim()}";
            var releaseDate = plannedReleaseDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return notificationEvent == PublicationScheduleNotificationEvent.Scheduled
                ? new PublicationScheduleNotificationContent(
                    "Chapter Scheduled",
                    $"{chapterLabel} of '{seriesTitle}' is scheduled for release on {releaseDate}.")
                : new PublicationScheduleNotificationContent(
                    "Chapter Rescheduled",
                    $"{chapterLabel} of '{seriesTitle}' was rescheduled for release on {releaseDate}.");
        }

        private static DateTime? NormalizeDate(DateTime? value) =>
            value?.Date;
    }
}
