using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record GenreDto(Guid GenreId, string GenreName, string? Description = null);
}
