namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// Standard structured error body for API consumers. Never carries raw SQL or
    /// technical exception text; only friendly, safe messages.
    /// </summary>
    public record ApiErrorResponse(string Message);

    /// <summary>
    /// Simple acknowledgement body for accepted-but-pending workflow steps such as
    /// "OTP sent" or "registration pending approval".
    /// </summary>
    public record ApiMessageResponse(string Message);
}
