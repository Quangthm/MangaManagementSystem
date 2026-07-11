using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface INotificationApiClient
    {
        Task<IReadOnlyList<NotificationDto>>
            GetNotificationsAsync(
                int take = 20,
                CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            Guid notificationId,
            CancellationToken cancellationToken = default);

        Task<MarkAllNotificationsReadResultDto>
            MarkAllAsReadAsync(
                CancellationToken cancellationToken = default);
    }
}