using MangaManagementSystem.API.Security;
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
        private readonly IAuthenticatedActorResolver
            _actorResolver;
        private readonly ILogger<ProfilePasswordController>
            _logger;

        public ProfilePasswordController(
            IUserService userService,
            IAuthenticatedActorResolver actorResolver,
            ILogger<ProfilePasswordController> logger)
        {
            _userService = userService;
            _actorResolver = actorResolver;
            _logger = logger;
        }

        [HttpPost("otp")]
        public async Task<IActionResult> SendOtpAsync()
        {
            var actor =
                await _actorResolver.ResolveActiveUserAsync(User);

            if (!actor.Succeeded)
            {
                return MapActorFailure(actor);
            }

            var userId = actor.ActorUserId;

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
            var actor =
                await _actorResolver.ResolveActiveUserAsync(User);

            if (!actor.Succeeded)
            {
                return MapActorFailure(actor);
            }

            var userId = actor.ActorUserId;

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

        private IActionResult MapActorFailure(
            AuthenticatedActorResult result)
        {
            var message = result.FailureKind switch
            {
                AuthenticatedActorFailureKind.InactiveAccount =>
                    "The current account is not active.",

                AuthenticatedActorFailureKind.WrongRole =>
                    "The current account is not permitted to use this operation.",

                _ =>
                    "Authenticated user information is invalid."
            };

            var response = new ApiErrorResponse(
                AuthErrorCodes.InvalidRequest,
                message);

            return result.FailureKind
                is AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                    ? Unauthorized(response)
                    : StatusCode(
                        StatusCodes.Status403Forbidden,
                        response);
        }
    }
}
