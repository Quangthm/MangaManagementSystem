using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    /// <summary>
    /// Request shape for starting registration: validates input and triggers an OTP email.
    /// Portfolio upload is handled by the Web host today and is intentionally not part of
    /// this JSON contract; it can be added later as a multipart endpoint.
    /// </summary>
    public record SendRegistrationOtpRequest(
        [Required][MaxLength(50)] string Username,
        [Required][EmailAddress][MaxLength(254)] string Email,
        [Required][MinLength(8)][MaxLength(255)] string Password,
        [Required][MaxLength(30)] string RoleName,
        [MaxLength(100)] string? DisplayName);

    /// <summary>
    /// Request shape for completing registration with the emailed OTP code.
    /// </summary>
    public record CompleteRegistrationRequest(
        [Required][EmailAddress][MaxLength(254)] string Email,
        [Required][MaxLength(10)] string Otp);
}
