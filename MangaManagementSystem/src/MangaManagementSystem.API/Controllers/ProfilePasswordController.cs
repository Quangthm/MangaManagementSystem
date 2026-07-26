using System.Security.Claims;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/profile/password")]
    public sealed class ProfilePasswordController
        : ControllerBase
    {
        private const string PasswordResetActionCode =
            "PROFILE_PASSWORD_RESET";

        private readonly IUserService _userService;
        private readonly ILogger<ProfilePasswordController>
            _logger;

        public ProfilePasswordController(
            IUserService userService,
            ILogger<ProfilePasswordController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("otp")]
        public async Task<IActionResult> SendOtpAsync()
        {
            if (!TryResolveAuthenticatedUserId(out var userId))
            {
                return Unauthorized(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "Authenticated user information is invalid."));
            }

            try
            {
                await _userService.SendProfileOtpAsync(
                    userId,
                    PasswordResetActionCode);

                return Ok(
                    new ProfilePasswordResponse(
                        "OTP sent to your registered email."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password OTP for user {UserId}.",
                    userId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RequestFailed,
                        "The OTP email could not be sent. Please try again."));
            }
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetAsync(
            [FromBody] ResetProfilePasswordRequest request)
        {
            if (!TryResolveAuthenticatedUserId(out var userId))
            {
                return Unauthorized(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "Authenticated user information is invalid."));
            }

            if (string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidOtp,
                        "OTP code is required."));
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword)
                || request.NewPassword.Length < 8)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "New password must be at least 8 characters."));
            }

            try
            {
                var verified =
                    await _userService.VerifyProfileOtpAsync(
                        userId,
                        PasswordResetActionCode,
                        request.OtpCode);

                if (!verified)
                {
                    return BadRequest(
                        new ApiErrorResponse(
                            AuthErrorCodes.InvalidOtp,
                            "Invalid or expired OTP."));
                }

                await _userService.ResetPasswordAsync(
                    userId,
                    request.NewPassword);

                return Ok(
                    new ProfilePasswordResponse(
                        "Password reset successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reset password for user {UserId}.",
                    userId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RequestFailed,
                        "The password could not be reset. Please try again."));
            }
        }

        private bool TryResolveAuthenticatedUserId(
            out Guid userId)
        {
            var rawUserId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("user_id")
                ?? User.FindFirstValue("UserId");

            return Guid.TryParse(rawUserId, out userId)
                   && userId != Guid.Empty;
        }
    }
}
