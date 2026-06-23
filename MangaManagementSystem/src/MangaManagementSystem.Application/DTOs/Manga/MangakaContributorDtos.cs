using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Read-model DTO for a single SeriesContributor row on the Mangaka contributor management page.
    /// Includes contributor identity, role, series context, and active/former status.
    /// </summary>
    public sealed record SeriesContributorListItemDto(
        Guid SeriesId,
        string SeriesTitle,
        Guid UserId,
        string DisplayName,
        string? Username,
        string? Email,
        string RoleName,
        DateTime StartDate,
        DateTime? EndDate,
        bool IsActive);

    /// <summary>
    /// Read-model DTO for an eligible Assistant user who can be added as a contributor
    /// to a series. Excludes users who are already active contributors of the series.
    /// </summary>
    public sealed record EligibleAssistantContributorDto(
        Guid UserId,
        string DisplayName,
        string? Username,
        string? Email);

    /// <summary>
    /// Request body for adding an Assistant contributor to a series.
    /// </summary>
    public sealed record AddAssistantContributorRequest(
        [Required] Guid AssistantUserId);

    /// <summary>
    /// Request body for ending (removing) an Assistant contributor from a series.
    /// Reason is required and max 500 characters.
    /// </summary>
    public sealed record EndAssistantContributorRequest(
        [Required][MaxLength(500)] string Reason);
}
