using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.Queries;
using MangaManagementSystem.Application.Features.Auth.Registration;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController
        : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AuthController> _logger;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AuthController(
            ISender sender,
            ILogger<AuthController> logger,
            IOptions<InternalApiOptions>
                internalApiOptions)
        {
            _sender = sender;
            _logger = logger;
            _internalApiOptions =
                internalApiOptions.Value;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "Username or email and password are required."));
            }

            try
            {
                var result =
                    await _sender.Send(
                        new AuthenticateUserQuery(
                            request.UsernameOrEmail,
                            request.Password),
                        cancellationToken);

                if (!result.Succeeded
                    || result.User is null
                    || string.IsNullOrWhiteSpace(
                        result.RoleName))
                {
                    return StatusCode(
                        ResolveAuthenticationFailureStatus(
                            result.ErrorCode),
                        new ApiErrorResponse(
                            result.ErrorCode
                                ?? AuthErrorCodes.InvalidCredentials,
                            result.ErrorMessage
                                ?? "Invalid credentials."));
                }

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error processing login.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RequestFailed,
                        "Login could not be completed right now."));
            }
        }

        [HttpPost("google-login/resolve")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult>
            ResolveGoogleLoginAsync(
                [FromBody] GoogleLoginRequest request,
                CancellationToken cancellationToken)
        {
            if (!HasValidInternalApiKey())
            {
                return InternalUnauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.GoogleEmailMissing,
                        "Google did not return a valid email address."));
            }

            try
            {
                var result =
                    await _sender.Send(
                        new ResolveGoogleLoginQuery(
                            request.Email),
                        cancellationToken);

                if (!result.Succeeded
                    || result.User is null
                    || string.IsNullOrWhiteSpace(
                        result.RoleName))
                {
                    return StatusCode(
                        ResolveAuthenticationFailureStatus(
                            result.ErrorCode),
                        new ApiErrorResponse(
                            result.ErrorCode
                                ?? AuthErrorCodes.AccountNotFound,
                            result.ErrorMessage
                                ?? "No active account was found for this Google email."));
                }

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error resolving Google login.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.GoogleOAuthFailed,
                        "Google sign-in could not be completed right now."));
            }
        }

        [HttpPost("google-signup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult>
            GoogleSignupAsync(
                [FromBody] GoogleSignupRequest request,
                CancellationToken cancellationToken)
        {
            if (!HasValidInternalApiKey())
            {
                return InternalUnauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "Google sign-up information is invalid."));
            }

            var command =
                new ProcessGoogleSignupCommand(
                    request.Email,
                    request.GoogleDisplayName,
                    request.RoleName);

            try
            {
                var result =
                    await _sender.Send(
                        command,
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Google sign-up request was rejected.");

                var errorCode =
                    ex.Message.Contains(
                        "role",
                        StringComparison.OrdinalIgnoreCase)
                        ? AuthErrorCodes.InvalidRole
                        : AuthErrorCodes.GoogleSignupFailed;

                var safeMessage =
                    errorCode ==
                    AuthErrorCodes.InvalidRole
                        ? "The selected registration role is invalid."
                        : "Google sign-up could not be completed.";

                return BadRequest(
                    new ApiErrorResponse(
                        errorCode,
                        safeMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error processing Google sign-up.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.GoogleSignupFailed,
                        "We could not process Google sign-up right now. Please try again later."));
            }
        }

        private IActionResult InternalUnauthorized()
        {
            _logger.LogWarning(
                "Rejected unauthorized internal authentication request.");

            return Unauthorized(
                new ApiErrorResponse(
                    AuthErrorCodes.UnauthorizedInternalRequest,
                    "Unauthorized internal request."));
        }

        private bool HasValidInternalApiKey()
        {
            return Request.Headers.TryGetValue(
                    InternalApiOptions.HeaderName,
                    out var suppliedKey)
                && KeysMatch(
                    suppliedKey.ToString(),
                    _internalApiOptions.Key);
        }

        private static int
            ResolveAuthenticationFailureStatus(
                string? errorCode)
        {
            return errorCode switch
            {
                AuthErrorCodes.AccountPending =>
                    StatusCodes.Status403Forbidden,

                AuthErrorCodes.AccountRejected =>
                    StatusCodes.Status403Forbidden,

                AuthErrorCodes.AccountDisabled =>
                    StatusCodes.Status403Forbidden,

                AuthErrorCodes.AccountConfigurationInvalid =>
                    StatusCodes.Status403Forbidden,

                _ =>
                    StatusCodes.Status401Unauthorized
            };
        }

        private static bool KeysMatch(
            string suppliedKey,
            string expectedKey)
        {
            if (string.IsNullOrWhiteSpace(
                    suppliedKey)
                || string.IsNullOrWhiteSpace(
                    expectedKey))
            {
                return false;
            }

            var suppliedBytes =
                Encoding.UTF8.GetBytes(
                    suppliedKey);

            var expectedBytes =
                Encoding.UTF8.GetBytes(
                    expectedKey);

            return suppliedBytes.Length ==
                    expectedBytes.Length
                && CryptographicOperations
                    .FixedTimeEquals(
                        suppliedBytes,
                        expectedBytes);
        }
    }
}
