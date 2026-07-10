using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record SetChapterPlannedReleaseDateResponse(
        Guid ChapterId,
        string StatusCode,
        DateTime PlannedReleaseDate,
        string? ValidationMessage,
        DateTime? AllowedPeriodStart,
        DateTime? AllowedPeriodEnd,
        string? FrequencyCode,
        DateTime? SuggestedReleaseDate = null,
        string? WarningMessage = null);
}
