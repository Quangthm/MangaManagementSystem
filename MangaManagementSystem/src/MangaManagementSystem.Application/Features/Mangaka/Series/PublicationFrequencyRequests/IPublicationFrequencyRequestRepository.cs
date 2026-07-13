namespace MangaManagementSystem.Application.Features.Mangaka.Series.PublicationFrequencyRequests
{
    public interface IPublicationFrequencyRequestRepository
    {
        Task<PublicationFrequencyChangeRequestResultDto>
            SendPublicationFrequencyChangeRequestAsync(
                Guid actorUserId,
                Guid seriesId,
                string reason,
                PublicationFrequencyRequestNotificationPlan notificationPlan,
                CancellationToken cancellationToken);
    }
}
