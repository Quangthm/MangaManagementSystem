using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public sealed record TagDto(Guid TagId, string TagName, string? Description = null);
}
