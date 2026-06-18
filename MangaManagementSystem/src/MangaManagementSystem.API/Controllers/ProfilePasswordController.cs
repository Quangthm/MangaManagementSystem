using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
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
        public async Task<IActionResult> SendOtpAsync(
            [FromBody] SendProfilePasswordOtpRequest request)
        {
            if (request.UserId == Guid.Empty)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "User id is required."));
            }

            try
            {
                await _userService.SendProfileOtpAsync(
                    request.UserId,
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
                    request.UserId);

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
            if (request.UserId == Guid.Empty)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "User id is required."));
            }

            if (string.IsNullOrWhiteSpace(
                    request.OtpCode))
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidOtp,
                        "OTP code is required."));
            }

            if (string.IsNullOrWhiteSpace(
                    request.NewPassword)
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
                    await _userService
                        .VerifyProfileOtpAsync(
                            request.UserId,
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
                    request.UserId,
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
                    request.UserId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RequestFailed,
                        "The password could not be reset. Please try again."));
            }
        }
    }
}
