using MangaManagementSystem.Application.Common;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.PublicationFrequencyRequests
{
    public sealed class RequestPublicationFrequencyChangeCommandHandler
        : IRequestHandler<
            RequestPublicationFrequencyChangeCommand,
            PublicationFrequencyChangeRequestResultDto>
    {
        private const string ActiveUserStatusCode =
            "ACTIVE";

        private const string MangakaRoleName =
            "Mangaka";

        private const string EditorialBoardChiefRoleName =
            "Editorial Board Chief";

        private const string AuditActionCode =
            "PUBLICATION_FREQUENCY_CHANGE_REQUESTED";

        private readonly IPublicationFrequencyRequestRepository
            _repository;

        public RequestPublicationFrequencyChangeCommandHandler(
            IPublicationFrequencyRequestRepository repository)
        {
            _repository =
                repository
                ?? throw new ArgumentNullException(
                    nameof(repository));
        }

        public Task<PublicationFrequencyChangeRequestResultDto>
            Handle(
                RequestPublicationFrequencyChangeCommand request,
                CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in Mangaka is required.");
            }

            if (request.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series is required.");
            }

            var reason =
                request.Reason?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException(
                    "A reason for the publication frequency change request is required.");
            }

            var notificationPlan =
                new PublicationFrequencyRequestNotificationPlan(
                    RecipientRoleName:
                        EditorialBoardChiefRoleName,
                    RecipientStatusCode:
                        ActiveUserStatusCode,
                    ActorRoleName:
                        MangakaRoleName,
                    NotificationTypeCode:
                        NotificationTypeCodes
                            .PublicationFrequencyRequest,
                    NotificationTitle:
                        "Publication Frequency Change Request",
                    NotificationMessageFormat:
                        "{0} requested a publication frequency change for \"{1}\". "
                        + "Current official frequency: {2}. Reason: {3}",
                    RelatedEntityType:
                        NotificationRelatedEntityTypes.Series,
                    AuditActionCode:
                        AuditActionCode,
                    AuditEntityType:
                        NotificationRelatedEntityTypes.Series);

            return _repository
                .SendPublicationFrequencyChangeRequestAsync(
                    request.ActorUserId,
                    request.SeriesId,
                    reason,
                    notificationPlan,
                    cancellationToken);
        }
    }
}
