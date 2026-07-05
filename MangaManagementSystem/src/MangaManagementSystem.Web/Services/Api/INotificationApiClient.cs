using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface INotificationApiClient
    {
        Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid actorUserId, CancellationToken cancellationToken = default);
        Task<NotificationDto?> MarkAsReadAsync(Guid actorUserId, Guid notificationId, CancellationToken cancellationToken = default);
    }
}
