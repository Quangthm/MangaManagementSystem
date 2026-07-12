using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;
using System;

namespace MangaManagementSystem.Application.Features.Notifications
{
    public sealed record MarkNotificationAsReadCommand(
        Guid RecipientUserId,
        Guid NotificationId)
        : IRequest<bool>;

    public sealed record MarkAllNotificationsAsReadCommand(
        Guid RecipientUserId)
        : IRequest<MarkAllNotificationsReadResultDto>;
}
