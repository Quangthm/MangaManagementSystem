namespace MangaManagementSystem.Application.Features.Editor.Annotations.Models;

public sealed record EditorAnnotationRow(
    Guid AnnotationId,
    Guid ChapterId,
    string ChapterNumberLabel,
    string? ChapterTitle,
    Guid ChapterPageId,
    int PageNumber,
    Guid ChapterPageVersionId,
    short? VersionNo,
    string IssueTypeCode,
    string? AnnotationText,
    bool IsResolved,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<EditorAnnotationRegionItem> Regions);
