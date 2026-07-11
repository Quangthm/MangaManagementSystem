using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga;

public record ChapterPageDto(
    Guid ChapterPageId,
    Guid ChapterId,
    int PageNo,
    string? PageNotes,
    DateTime? DeletedAtUtc,
    Guid? DeletedByUserId
);

public record CreateChapterPageDto(
    [Required] Guid ChapterId,
    [Required] int PageNo,
    string? PageNotes
);

public record UpdateChapterPageDto(
    [Required] Guid ChapterPageId,
    [Required] Guid ChapterId,
    [Required] int PageNo,
    string? PageNotes
);

/// <summary>Web → API body to update a page's whole-page note.</summary>
public sealed record UpdatePageNotesRequest(string? PageNotes);

/// <summary>Web → API body to fetch non-deleted page counts for several chapters.</summary>
public sealed record PageCountsRequest(IReadOnlyList<Guid> ChapterIds);

/// <summary>Web → API body to atomically create a page, version 1, and its file resource.</summary>
public sealed record CreatePageWithVersionRequestDto(
    [Required] Guid ChapterId,
    [Required] int PageNo,
    string? PageNotes,
    [Required] CreateFileResourceDto FileDto,
    string? VersionNote
);

public sealed record CreatePageWithVersionResponseDto(
    ChapterPageDto Page,
    ChapterPageVersionDto Version,
    FileResourceDto FileResource
);

public sealed record CreateVersionWithFileAndRegionsRequestDto(
    [Required] Guid ChapterPageId,
    [Required] short VersionNo,
    [Required] CreateFileResourceDto FileDto,
    string? VersionNote,
    IReadOnlyList<CreatePageRegionDto>? Regions,
    bool SetAsCurrent
);

public sealed record GetVersionsByPageIdsRequest(IReadOnlyList<Guid> PageIds);

public sealed record GetFileResourcesByIdsRequest(IReadOnlyList<Guid> FileIds);

public sealed record SetCurrentVersionRequest(Guid ChapterPageVersionId);
