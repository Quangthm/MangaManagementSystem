using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.Notifications
{
    internal static class NotificationRequestValidation
    {
        internal static Guid ValidateRecipientUserId(
            Guid recipientUserId)
        {
            if (recipientUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Recipient user id is required.");
            }

            return recipientUserId;
        }

        internal static Guid ValidateNotificationId(
            Guid notificationId)
        {
            if (notificationId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Notification id is required.");
            }

            return notificationId;
        }

        internal static int NormalizeTake(
            int take)
        {
            if (take < 1)
            {
                return 20;
            }

            return Math.Min(
                take,
                100);
        }
    }

    internal static class NotificationDtoMapper
    {
        internal static NotificationDto ToDto(
            Notification notification)
        {
            return new NotificationDto(
                notification.NotificationId,
                notification.RecipientUserId,
                notification.NotificationTypeCode,
                notification.Title,
                notification.Message,
                notification.RelatedEntityType,
                notification.RelatedEntityId,
                notification.ReadAtUtc,
                notification.CreatedAtUtc);
        }
    }

    public sealed class
        GetCurrentUserNotificationsQueryHandler
        : IRequestHandler<
            GetCurrentUserNotificationsQuery,
            IReadOnlyList<NotificationDto>>
    {
        private readonly INotificationRepository
            _notificationRepository;

        public GetCurrentUserNotificationsQueryHandler(
            INotificationRepository notificationRepository)
        {
            _notificationRepository =
                notificationRepository
                ?? throw new ArgumentNullException(
                    nameof(notificationRepository));
        }

        public async Task<IReadOnlyList<NotificationDto>>
            Handle(
                GetCurrentUserNotificationsQuery request,
                CancellationToken cancellationToken)
        {
            var recipientUserId =
                NotificationRequestValidation
                    .ValidateRecipientUserId(
                        request.RecipientUserId);

            var take =
                NotificationRequestValidation
                    .NormalizeTake(
                        request.Take);

            var notifications =
                await _notificationRepository
                    .GetRecentByRecipientUserIdAsync(
                        recipientUserId,
                        take,
                        cancellationToken);

            return notifications
                .Select(
                    NotificationDtoMapper.ToDto)
                .ToList();
        }
    }

    public sealed class
        GetUnreadNotificationCountQueryHandler
        : IRequestHandler<
            GetUnreadNotificationCountQuery,
            UnreadNotificationCountDto>
    {
        private readonly INotificationRepository
            _notificationRepository;

        public GetUnreadNotificationCountQueryHandler(
            INotificationRepository notificationRepository)
        {
            _notificationRepository =
                notificationRepository
                ?? throw new ArgumentNullException(
                    nameof(notificationRepository));
        }

        public async Task<UnreadNotificationCountDto>
            Handle(
                GetUnreadNotificationCountQuery request,
                CancellationToken cancellationToken)
        {
            var recipientUserId =
                NotificationRequestValidation
                    .ValidateRecipientUserId(
                        request.RecipientUserId);

            var unreadCount =
                await _notificationRepository
                    .CountUnreadByRecipientUserIdAsync(
                        recipientUserId,
                        cancellationToken);

            return new UnreadNotificationCountDto(
                unreadCount);
        }
    }
}
