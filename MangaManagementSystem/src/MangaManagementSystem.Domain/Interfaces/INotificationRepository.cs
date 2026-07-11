using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<Notification>>
            GetRecentByRecipientUserIdAsync(
                Guid recipientUserId,
                int take,
                CancellationToken cancellationToken = default);

        Task<int> CountUnreadByRecipientUserIdAsync(
            Guid recipientUserId,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            Guid recipientUserId,
            Guid notificationId,
            DateTime readAtUtc,
            CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(
            Guid recipientUserId,
            DateTime readAtUtc,
            CancellationToken cancellationToken = default);
    }
}