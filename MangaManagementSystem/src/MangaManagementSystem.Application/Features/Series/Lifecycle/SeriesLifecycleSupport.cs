using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle
{
    internal sealed record SeriesLifecycleActorContext(
        string DatabaseRoleName,
        bool IsActiveContributor);

    internal static class SeriesLifecycleSupport
    {
        internal const string ActiveStatusCode = "ACTIVE";
        internal const string MangakaRoleName = "Mangaka";
        internal const string TantouEditorRoleName = "Tantou Editor";
        internal const string SerializedStatusCode = "SERIALIZED";
        internal const string HiatusStatusCode = "HIATUS";
        internal const string CompletedStatusCode = "COMPLETED";
        internal const string CancelledStatusCode = "CANCELLED";

        internal static readonly IReadOnlySet<string> PauseResumeAllowedRoles =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                MangakaRoleName,
                TantouEditorRoleName
            };

        internal static readonly IReadOnlySet<string> MangakaOnlyAllowedRoles =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                MangakaRoleName
            };

        internal static readonly string[] CompletionCancellationStatuses =
        {
            "DRAFT",
            "REVISION_REQUESTED",
            "UNDER_REVIEW",
            "APPROVED",
            "SCHEDULED",
            "ON_HOLD"
        };

        internal static async Task<string> ValidateActorAsync(
            IUnitOfWork unitOfWork,
            Guid seriesId,
            Guid actorUserId,
            string actorRoleName,
            IReadOnlySet<string> allowedRoles,
            CancellationToken cancellationToken)
        {
            SeriesLifecycleActorContext actorContext =
                await ResolveActorContextAsync(
                    unitOfWork,
                    seriesId,
                    actorUserId,
                    actorRoleName,
                    cancellationToken);

            if (!allowedRoles.Contains(actorContext.DatabaseRoleName))
            {
                throw new UnauthorizedAccessException(
                    "The signed-in user is not permitted to perform this series operation.");
            }

            if (!actorContext.IsActiveContributor)
            {
                throw new UnauthorizedAccessException(
                    "An active series contributor assignment is required for this operation.");
            }

            return actorContext.DatabaseRoleName;
        }

        internal static async Task<SeriesLifecycleActorContext>
            ResolveActorContextAsync(
                IUnitOfWork unitOfWork,
                Guid seriesId,
                Guid actorUserId,
                string actorRoleName,
                CancellationToken cancellationToken)
        {
            if (actorUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "A valid signed-in user is required for this series operation.");
            }

            var actor = await unitOfWork.Users.GetByIdWithRoleAsync(
                actorUserId,
                cancellationToken);

            if (actor == null)
            {
                throw new UnauthorizedAccessException(
                    "The signed-in user no longer exists.");
            }

            if (!string.Equals(
                    actor.StatusCode,
                    ActiveStatusCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "An active account is required for this series operation.");
            }

            string? databaseRoleName = actor.Role?.RoleName;
            if (string.IsNullOrWhiteSpace(databaseRoleName))
            {
                throw new UnauthorizedAccessException(
                    "The signed-in user does not have a current role.");
            }

            if (!string.Equals(
                    actorRoleName,
                    databaseRoleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "The signed-in role does not match the user's current role.");
            }

            var activeContributors = await unitOfWork.SeriesContributors.FindAsync(
                seriesContributor =>
                    seriesContributor.SeriesId == seriesId
                    && seriesContributor.UserId == actorUserId
                    && seriesContributor.EndDate == null);

            return new SeriesLifecycleActorContext(
                databaseRoleName,
                activeContributors.Count > 0);
        }
    }
}
