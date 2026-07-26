namespace MangaManagementSystem.API.Contracts
{
    public sealed record ResetProfilePasswordRequest(
        string OtpCode,
        string NewPassword
    );

    public sealed record ProfilePasswordResponse(
        string Message
    );
}
