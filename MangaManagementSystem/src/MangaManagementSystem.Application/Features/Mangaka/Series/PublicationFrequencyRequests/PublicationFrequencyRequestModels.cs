namespace MangaManagementSystem.Application.Features.Mangaka.Series.PublicationFrequencyRequests
{
    public sealed record PublicationFrequencyRequestNotificationPlan(
        string RecipientRoleName,
        string RecipientStatusCode,
        string ActorRoleName,
        string NotificationTypeCode,
        string NotificationTitle,
        string NotificationMessageFormat,
        string RelatedEntityType,
        string AuditActionCode,
        string AuditEntityType);

    public sealed record PublicationFrequencyChangeRequestResultDto(
        Guid SeriesId,
        string SeriesTitle,
        string CurrentPublicationFrequencyCode,
        int NotificationCount,
        DateTime RequestedAtUtc);
}
