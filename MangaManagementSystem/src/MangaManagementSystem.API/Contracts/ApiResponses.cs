using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// Standard structured error body for API consumers.
    /// The code is stable for client branching while Message remains safe for display.
    /// </summary>
    public sealed record ApiErrorResponse
    {
        public ApiErrorResponse(
            string message)
            : this(
                AuthErrorCodes.RequestFailed,
                message)
        {
        }

        public ApiErrorResponse(
            string code,
            string message)
        {
            Code =
                string.IsNullOrWhiteSpace(code)
                    ? AuthErrorCodes.RequestFailed
                    : code.Trim();

            Message =
                string.IsNullOrWhiteSpace(message)
                    ? "The request could not be completed."
                    : message.Trim();
        }

        public string Code { get; init; }

        public string Message { get; init; }
    }

    /// <summary>
    /// Simple acknowledgement body for accepted workflow steps.
    /// </summary>
    public sealed record ApiMessageResponse(
        string Message);
}
