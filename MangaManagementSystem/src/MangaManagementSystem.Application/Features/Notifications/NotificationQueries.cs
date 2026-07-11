using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.Features.Notifications
{
    public sealed record GetCurrentUserNotificationsQuery(
        Guid RecipientUserId,
        int Take = 20)
        : IRequest<IReadOnlyList<NotificationDto>>;

    public sealed record GetUnreadNotificationCountQuery(
        Guid RecipientUserId)
        : IRequest<UnreadNotificationCountDto>;
}
