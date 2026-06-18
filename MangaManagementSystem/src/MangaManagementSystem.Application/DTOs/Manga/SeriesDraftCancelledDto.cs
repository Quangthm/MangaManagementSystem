using System;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result returned after a successful Cancel Draft workflow.
    /// The stored procedure transitions Series.status_code to CANCELLED and writes the
    /// SERIES_DRAFT_CANCELLED audit event. This DTO carries the resulting identity and
    /// status back to the API controller and typed Web client.
    /// </summary>
    public sealed class SeriesDraftCancelledDto
    {
        public Guid SeriesId { get; init; }
        public string StatusCode { get; init; } = "CANCELLED";
    }
}
