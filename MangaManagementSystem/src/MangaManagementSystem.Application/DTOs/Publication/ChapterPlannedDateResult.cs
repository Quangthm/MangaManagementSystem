using System;

namespace MangaManagementSystem.Application.DTOs.Publication
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
