using System;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record SetChapterPlannedReleaseDateResponse(
        Guid ChapterId,
        string StatusCode,
        DateTime PlannedReleaseDate,
        string? ValidationMessage,
        DateTime? AllowedPeriodStart,
        DateTime? AllowedPeriodEnd,
        string? FrequencyCode);
}
