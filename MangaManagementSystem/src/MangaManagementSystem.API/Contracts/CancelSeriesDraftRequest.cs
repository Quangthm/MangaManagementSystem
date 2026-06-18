namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// JSON body for POST /api/mangaka/series/{seriesId}/draft-cancellations.
    /// Reason is optional; pass null or omit the body entirely to cancel without a reason.
    /// Max 500 characters matches the manga.usp_Series_CancelDraft @reason parameter.
    /// </summary>
    public sealed class CancelSeriesDraftRequest
    {
        public string? Reason { get; init; }
    }
}
