using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga;

public record ChapterPageAnnotationDto(
    Guid ChapterPageAnnotationId,
    string IssueTypeCode,
    Guid AnnotatedByUserId,
    string? AnnotationText,
    Guid? ResolvedByUserId,
    IReadOnlyList<PageRegionDto> PageRegions,
    DateTime? CreatedAtUtc = null,
    string? AnnotatedByDisplayName = null,
    DateTime? ResolvedAtUtc = null
);

public record CreateChapterPageAnnotationDto(
    [Required][MaxLength(50)] string IssueTypeCode,
    [Required] Guid AnnotatedByUserId,
    string? AnnotationText,
    [Required] IReadOnlyList<Guid> PageRegionIds
);

/// <summary>Web → API body to create an annotation. Author is taken from the X-Actor-User-Id header.</summary>
public sealed record CreateMangakaAnnotationRequest(
    string IssueTypeCode,
    string? AnnotationText,
    IReadOnlyList<Guid> PageRegionIds
);

/// <summary>Web → API body to resolve an annotation (optional note).</summary>
public sealed record ResolveAnnotationRequest(string? ResolutionNote);

public record UpdateChapterPageAnnotationDto(
    [Required] Guid ChapterPageAnnotationId,
    [Required][MaxLength(50)] string IssueTypeCode,
    [Required] Guid AnnotatedByUserId,
    string? AnnotationText,
    Guid? ResolvedByUserId,
    [Required] IReadOnlyList<Guid> PageRegionIds
);
