<<<<<<< HEAD
namespace MangaManagementSystem.Application.DTOs.Auth;

public enum GoogleSignupFlow
{
    NewUserVerifyOtp,
    PendingApprovalVerifyOtp,
    ActiveUserLogin,
    Rejected
=======
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
        string? ErrorMessage = null,
        string? ErrorCode = null
    );
>>>>>>> main
}

public record GoogleSignupCallbackResult(
    GoogleSignupFlow Flow,
    string Email,
    UserDto? User = null,
    string? RoleName = null,
    string? ErrorMessage = null
);
