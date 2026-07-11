using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Publication;

public sealed record PublicationScheduleCalendarDto(
    DateTime WeekStartDate,
    DateTime WeekEndDate,
    IReadOnlyList<PublicationScheduleDayDto> Days);

public sealed record PublicationScheduleDayDto(
    DateTime Date,
    string DayLabel,
    bool IsToday,
    IReadOnlyList<PublicationScheduleItemDto> Items);

public sealed record PublicationScheduleItemDto(
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? SeriesCoverUrl,
    Guid ChapterId,
    string ChapterNumberLabel,
    string StatusCode,
    string StatusBadgeLabel,
    DateTime? PlannedReleaseDate,
    DateTime? ReleasedAtUtc,
    string? PublicationFrequencyCode);
