namespace MangaManagementSystem.Application.DTOs.Auth
{
    public enum GoogleSignupFlow
    {
        PendingApproval,
        ActiveUserLogin,
        Rejected,
        Disabled
    }

    public record GoogleSignupCallbackResult(
        GoogleSignupFlow Flow,
        string Email,
        UserDto? User = null,
        string? RoleName = null,
        string? ErrorMessage = null
    );
}
