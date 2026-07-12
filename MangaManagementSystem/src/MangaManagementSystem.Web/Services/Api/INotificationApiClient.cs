using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface INotificationApiClient
    {
<<<<<<< HEAD
        Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid actorUserId, CancellationToken cancellationToken = default);
        Task<NotificationDto?> MarkAsReadAsync(Guid actorUserId, Guid notificationId, CancellationToken cancellationToken = default);
    }
}
=======
        Task<IReadOnlyList<NotificationDto>>
            GetNotificationsAsync(
                int skip = 0,
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
>>>>>>> main
