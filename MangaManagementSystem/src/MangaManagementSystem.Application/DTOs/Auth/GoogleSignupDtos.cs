namespace MangaManagementSystem.Application.DTOs.Auth
{
    public enum GoogleSignupFlow
    {
        NewUserVerifyOtp,
        PendingApprovalVerifyOtp,
        ActiveUserLogin,
        Rejected
    }

    public record GoogleSignupCallbackResult(
        GoogleSignupFlow Flow,
        string Email,
        UserDto? User = null,
        string? RoleName = null,
        string? ErrorMessage = null
    );
}
