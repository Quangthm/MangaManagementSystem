using System;

namespace MangaManagementSystem.Application.DTOs.Editor;

public sealed record RescheduleChapterResponse(
    Guid ChapterId,
    string StatusCode,
    DateTime PlannedReleaseDate,
    string? Message);
