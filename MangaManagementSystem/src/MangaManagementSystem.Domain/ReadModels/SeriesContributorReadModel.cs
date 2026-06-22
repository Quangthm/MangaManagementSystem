using System;

namespace MangaManagementSystem.Domain.ReadModels
{
    public sealed record SeriesContributorReadModel(
        string DisplayName,
        string RoleName,
        DateTime StartDate,
        DateTime? EndDate);
}
