namespace MangaManagementSystem.Application.Features.Editor.Annotations.Models;

public sealed record EditorAnnotationRegionItem(
    Guid PageRegionId,
    string RegionTypeCode,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height);
