using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.Features.Publication.Schedule;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    internal static class PublicationScheduleNotificationPersistence
    {
        private const string ActiveUserStatus = "ACTIVE";

        public static async Task AddNotificationsAsync(
            ApplicationDbContext context,
            Guid actorUserId,
            Chapter chapter,
            PublicationScheduleNotificationEvent notificationEvent,
            DateTime createdAtUtc,
            CancellationToken cancellationToken)
        {
            if (notificationEvent == PublicationScheduleNotificationEvent.None)
                return;

            if (chapter.Series is null || !chapter.PlannedReleaseDate.HasValue)
                throw new InvalidOperationException(
                    "Scheduled chapter notification context is incomplete.");

            var recipientUserIds = await context.ActiveSeriesContributors
                .AsNoTracking()
                .Where(contributor =>
                    contributor.SeriesId == chapter.SeriesId
                    && contributor.EndDate == null
                    && contributor.UserStatusCode == ActiveUserStatus
                    && contributor.UserId != actorUserId)
                .Select(contributor => contributor.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var content = PublicationScheduleNotificationSupport.BuildContent(
                notificationEvent,
                chapter.Series.Title,
                chapter.ChapterNumberLabel,
                chapter.ChapterTitle,
                chapter.PlannedReleaseDate.Value);

            foreach (var recipientUserId in recipientUserIds)
            {
                context.Notifications.Add(new Notification
                {
                    RecipientUserId = recipientUserId,
                    NotificationTypeCode = NotificationTypeCodes.PublicationSchedule,
                    Title = content.Title,
                    Message = content.Message,
                    RelatedEntityType = NotificationRelatedEntityTypes.Chapter,
                    RelatedEntityId = chapter.ChapterId,
                    CreatedAtUtc = createdAtUtc
                });
            }
        }
    }
}
