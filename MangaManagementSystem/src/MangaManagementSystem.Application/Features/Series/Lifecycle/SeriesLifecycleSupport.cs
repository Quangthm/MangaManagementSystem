using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Features.Series.Lifecycle
{
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

        internal static async Task<string> ValidateActorAsync(
            IUnitOfWork unitOfWork,
            Guid seriesId,
            Guid actorUserId,
            string actorRoleName,
            IReadOnlySet<string> allowedRoles,
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

            if (actor.StatusCode != ActiveStatusCode)
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

            if (!allowedRoles.Contains(databaseRoleName))
            {
                throw new UnauthorizedAccessException(
                    "The signed-in user is not permitted to perform this series operation.");
            }

            var activeContributors = await unitOfWork.SeriesContributors.FindAsync(
                seriesContributor =>
                    seriesContributor.SeriesId == seriesId
                    && seriesContributor.UserId == actorUserId
                    && seriesContributor.EndDate == null);

            if (activeContributors.Count == 0)
            {
                throw new UnauthorizedAccessException(
                    "An active series contributor assignment is required for this operation.");
            }

            return databaseRoleName;
        }
    }
}
