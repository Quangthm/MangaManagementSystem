using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.Notifications
{
    public sealed class
        MarkNotificationAsReadCommandHandler
        : IRequestHandler<
            MarkNotificationAsReadCommand,
            bool>
    {
        private readonly INotificationRepository
            _notificationRepository;

        public MarkNotificationAsReadCommandHandler(
            INotificationRepository notificationRepository)
        {
            _notificationRepository =
                notificationRepository
                ?? throw new ArgumentNullException(
                    nameof(notificationRepository));
        }

        public Task<bool> Handle(
            MarkNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var recipientUserId =
                NotificationRequestValidation
                    .ValidateRecipientUserId(
                        request.RecipientUserId);

            var notificationId =
                NotificationRequestValidation
                    .ValidateNotificationId(
                        request.NotificationId);

            return _notificationRepository
                .MarkAsReadAsync(
                    recipientUserId,
                    notificationId,
                    DateTime.UtcNow,
                    cancellationToken);
        }
    }

    public sealed class
        MarkAllNotificationsAsReadCommandHandler
        : IRequestHandler<
            MarkAllNotificationsAsReadCommand,
            MarkAllNotificationsReadResultDto>
    {
        private readonly INotificationRepository
            _notificationRepository;

        public MarkAllNotificationsAsReadCommandHandler(
            INotificationRepository notificationRepository)
        {
            _notificationRepository =
                notificationRepository
                ?? throw new ArgumentNullException(
                    nameof(notificationRepository));
        }

        public async Task<MarkAllNotificationsReadResultDto>
            Handle(
                MarkAllNotificationsAsReadCommand request,
                CancellationToken cancellationToken)
        {
            var recipientUserId =
                NotificationRequestValidation
                    .ValidateRecipientUserId(
                        request.RecipientUserId);

            var updatedCount =
                await _notificationRepository
                    .MarkAllAsReadAsync(
                        recipientUserId,
                        DateTime.UtcNow,
                        cancellationToken);

            return new MarkAllNotificationsReadResultDto(
                updatedCount);
        }
    }
}
