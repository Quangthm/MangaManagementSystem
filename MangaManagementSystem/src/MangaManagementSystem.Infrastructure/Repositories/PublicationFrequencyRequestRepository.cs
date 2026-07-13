using System.Globalization;
using System.Text.Json;
using MangaManagementSystem.Application.Features.Mangaka.Series.PublicationFrequencyRequests;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class PublicationFrequencyRequestRepository
        : IPublicationFrequencyRequestRepository
    {
        private const string SerializedSeriesStatusCode =
            "SERIALIZED";

        private readonly ApplicationDbContext _dbContext;

        public PublicationFrequencyRequestRepository(
            ApplicationDbContext dbContext)
        {
            _dbContext =
                dbContext
                ?? throw new ArgumentNullException(
                    nameof(dbContext));
        }

        public async Task<PublicationFrequencyChangeRequestResultDto>
            SendPublicationFrequencyChangeRequestAsync(
                Guid actorUserId,
                Guid seriesId,
                string reason,
                PublicationFrequencyRequestNotificationPlan notificationPlan,
                CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                notificationPlan);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var series =
                    await _dbContext.Series
                        .AsNoTracking()
                        .Where(item =>
                            item.SeriesId == seriesId)
                        .Select(item =>
                            new
                            {
                                item.SeriesId,
                                item.Title,
                                item.StatusCode,
                                item.PublicationFrequencyCode
                            })
                        .SingleOrDefaultAsync(
                            cancellationToken);

                if (series is null)
                {
                    throw new InvalidOperationException(
                        "The selected series does not exist.");
                }

                if (!string.Equals(
                        series.StatusCode,
                        SerializedSeriesStatusCode,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Publication frequency change requests are only available for serialized series.");
                }

                if (string.IsNullOrWhiteSpace(
                        series.PublicationFrequencyCode))
                {
                    throw new InvalidOperationException(
                        "The series does not have an official publication frequency.");
                }

                var actorIsAuthorized =
                    await _dbContext
                        .ActiveSeriesContributors
                        .AsNoTracking()
                        .AnyAsync(
                            contributor =>
                                contributor.SeriesId
                                    == seriesId
                                && contributor.UserId
                                    == actorUserId
                                && contributor.RoleName
                                    == notificationPlan
                                        .ActorRoleName
                                && contributor.UserStatusCode
                                    == notificationPlan
                                        .RecipientStatusCode,
                            cancellationToken);

                if (!actorIsAuthorized)
                {
                    throw new InvalidOperationException(
                        "Only an active Mangaka contributor of this series can send this request.");
                }

                var requesterDisplayName =
                    await _dbContext.Users
                        .AsNoTracking()
                        .Where(user =>
                            user.UserId == actorUserId)
                        .Select(user =>
                            user.DisplayName)
                        .SingleOrDefaultAsync(
                            cancellationToken);

                if (string.IsNullOrWhiteSpace(
                        requesterDisplayName))
                {
                    requesterDisplayName =
                        "A Mangaka";
                }

                var recipientUserIds =
                    await _dbContext.Users
                        .AsNoTracking()
                        .Where(user =>
                            user.StatusCode
                                == notificationPlan
                                    .RecipientStatusCode
                            && user.Role != null
                            && user.Role.RoleName
                                == notificationPlan
                                    .RecipientRoleName)
                        .Select(user =>
                            user.UserId)
                        .Distinct()
                        .ToListAsync(
                            cancellationToken);

                if (recipientUserIds.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No active Editorial Board Chief is available to receive this request.");
                }

                var requestedAtUtc =
                    DateTime.UtcNow;

                var message =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        notificationPlan
                            .NotificationMessageFormat,
                        requesterDisplayName.Trim(),
                        series.Title,
                        series.PublicationFrequencyCode,
                        reason);

                var notifications =
                    recipientUserIds
                        .Select(recipientUserId =>
                            new Notification
                            {
                                RecipientUserId =
                                    recipientUserId,
                                NotificationTypeCode =
                                    notificationPlan
                                        .NotificationTypeCode,
                                Title =
                                    notificationPlan
                                        .NotificationTitle,
                                Message =
                                    message,
                                RelatedEntityType =
                                    notificationPlan
                                        .RelatedEntityType,
                                RelatedEntityId =
                                    series.SeriesId,
                                ReadAtUtc =
                                    null,
                                CreatedAtUtc =
                                    requestedAtUtc
                            })
                        .ToList();

                var auditDetailJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            series_id =
                                series.SeriesId,
                            series_title =
                                series.Title,
                            current_publication_frequency_code =
                                series.PublicationFrequencyCode,
                            request_reason =
                                reason,
                            notification_type_code =
                                notificationPlan
                                    .NotificationTypeCode,
                            recipient_role_name =
                                notificationPlan
                                    .RecipientRoleName,
                            recipient_count =
                                recipientUserIds.Count
                        });

                var auditEvent =
                    new AuditEvent
                    {
                        OccurredAtUtc =
                            requestedAtUtc,
                        ActorUserId =
                            actorUserId,
                        ActorRoleName =
                            notificationPlan
                                .ActorRoleName,
                        ActionCode =
                            notificationPlan
                                .AuditActionCode,
                        EntityType =
                            notificationPlan
                                .AuditEntityType,
                        EntityId =
                            series.SeriesId.ToString(),
                        DetailJson =
                            auditDetailJson
                    };

                await _dbContext.Notifications
                    .AddRangeAsync(
                        notifications,
                        cancellationToken);

                await _dbContext.AuditEvents
                    .AddAsync(
                        auditEvent,
                        cancellationToken);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return new PublicationFrequencyChangeRequestResultDto(
                    SeriesId:
                        series.SeriesId,
                    SeriesTitle:
                        series.Title,
                    CurrentPublicationFrequencyCode:
                        series.PublicationFrequencyCode,
                    NotificationCount:
                        recipientUserIds.Count,
                    RequestedAtUtc:
                        requestedAtUtc);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }
    }
}
