using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result of creating a series draft through <c>manga.usp_Series_Create</c>.
    /// Carries the new series identity plus the cover file resource id when a cover
    /// was uploaded during draft creation.
    /// </summary>
    public record SeriesDraftCreatedDto(
        Guid SeriesId,
        string Title,
        string Slug,
        string StatusCode,
        Guid? CoverFileId
    );
}
