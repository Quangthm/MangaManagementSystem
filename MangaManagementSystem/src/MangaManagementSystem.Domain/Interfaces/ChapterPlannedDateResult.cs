using System;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record ChapterPlannedDateResult(
        Guid ChapterId,
        string StatusCode,
        DateTime PlannedReleaseDate,
        string? Message,
        DateTime? AllowedPeriodStart,
        DateTime? AllowedPeriodEnd,
        string? FrequencyCode);
}
