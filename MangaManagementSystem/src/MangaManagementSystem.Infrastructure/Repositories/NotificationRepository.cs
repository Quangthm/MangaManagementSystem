using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class NotificationRepository
        : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(
            ApplicationDbContext context)
        {
            _context =
                context
                ?? throw new ArgumentNullException(
                    nameof(context));
        }

        public async Task<IReadOnlyList<Notification>>
            GetRecentByRecipientUserIdAsync(
                Guid recipientUserId,
                int skip,
                int take,
                CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(
                    notification =>
                        notification.RecipientUserId
                        == recipientUserId)
                .OrderByDescending(
                    notification =>
                        notification.CreatedAtUtc)
                .ThenByDescending(
                    notification =>
                        notification.NotificationId)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task<int>
            CountUnreadByRecipientUserIdAsync(
                Guid recipientUserId,
                CancellationToken cancellationToken = default)
        {
            return _context.Notifications
                .AsNoTracking()
                .CountAsync(
                    notification =>
                        notification.RecipientUserId
                            == recipientUserId
                        && notification.ReadAtUtc == null,
                    cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(
            Guid recipientUserId,
            Guid notificationId,
            DateTime readAtUtc,
            CancellationToken cancellationToken = default)
        {
            var updatedCount =
                await _context.Notifications
                    .Where(
                        notification =>
                            notification.RecipientUserId
                                == recipientUserId
                            && notification.NotificationId
                                == notificationId
                            && notification.ReadAtUtc
                                == null)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                notification =>
                                    notification.ReadAtUtc,
                                readAtUtc),
                        cancellationToken);

            if (updatedCount > 0)
            {
                return true;
            }

            // Mark-read is idempotent. An already-read notification
            // owned by the recipient is still considered successful.
            return await _context.Notifications
                .AsNoTracking()
                .AnyAsync(
                    notification =>
                        notification.RecipientUserId
                            == recipientUserId
                        && notification.NotificationId
                            == notificationId,
                    cancellationToken);
        }

        public Task<int> MarkAllAsReadAsync(
            Guid recipientUserId,
            DateTime readAtUtc,
            CancellationToken cancellationToken = default)
        {
            return _context.Notifications
                .Where(
                    notification =>
                        notification.RecipientUserId
                            == recipientUserId
                        && notification.ReadAtUtc
                            == null)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            notification =>
                                notification.ReadAtUtc,
                            readAtUtc),
                    cancellationToken);
        }
    }
}
