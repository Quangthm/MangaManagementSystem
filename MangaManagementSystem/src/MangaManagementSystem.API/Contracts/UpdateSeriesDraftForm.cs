using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// Multipart/form-data contract for BF-SERIES-002 — Edit Series Draft Profile.
    /// All profile text fields are required. The cover file is optional;
    /// when omitted the existing cover is kept unchanged.
    /// Cover editing is only allowed for PROPOSAL_DRAFT series; the stored procedure
    /// enforces this status guard.
    /// </summary>
    public sealed class UpdateSeriesDraftForm
    {
        public string Title { get; init; } = string.Empty;
        public string Synopsis { get; init; } = string.Empty;
        public List<Guid> GenreIds { get; init; } = new();
        public List<Guid> TagIds { get; init; } = new();
        public string ContentLanguageCode { get; init; } = "ja";
        public string? PublicationFrequencyCode { get; init; }
        public string? Slug { get; init; }
        /// <summary>
        /// Optional new cover image (PNG/JPG/WEBP, max 5 MB).
        /// Omit to keep the existing cover unchanged.
        /// Only accepted when the series is PROPOSAL_DRAFT.
        /// </summary>
        public IFormFile? CoverFile { get; init; }
    }
}
