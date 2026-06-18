namespace MangaManagementSystem.API.Contracts
{
    public sealed record SendProfilePasswordOtpRequest(
        Guid UserId
    );

    public sealed record ResetProfilePasswordRequest(
        Guid UserId,
        string OtpCode,
        string NewPassword
    );

    public sealed record ProfilePasswordResponse(
        string Message
    );
}