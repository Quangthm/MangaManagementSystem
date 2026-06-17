using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.Registration;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly InternalApiOptions _internalApiOptions;

        public AuthController(
            ISender sender,
            IAuthService authService,
            ILogger<AuthController> logger,
            IOptions<InternalApiOptions> internalApiOptions)
        {
            _sender = sender;
            _authService = authService;
            _logger = logger;
            _internalApiOptions = internalApiOptions.Value;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result =
                await _authService.LoginAsync(
                    new LoginDto(
                        request.UsernameOrEmail,
                        request.Password));

            if (!result.Succeeded
                || result.User is null
                || string.IsNullOrWhiteSpace(result.RoleName))
            {
                return Unauthorized(
                    new ApiErrorResponse(
                        result.ErrorMessage
                        ?? "Invalid credentials"));
            }

            return Ok(result.User);
        }

        [HttpPost("google-signup")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GoogleSignupAsync(
            [FromBody] GoogleSignupRequest request,
            CancellationToken cancellationToken)
        {
            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.HeaderName,
                    out var suppliedKey)
                || !KeysMatch(
                    suppliedKey.ToString(),
                    _internalApiOptions.Key))
            {
                _logger.LogWarning(
                    "Rejected unauthorized internal Google sign-up request.");

                return Unauthorized(
                    new ApiErrorResponse(
                        "Unauthorized internal request."));
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
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
                return BadRequest(
                    new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error processing Google sign-up.");

                return Problem(
                    detail:
                        "We could not process Google sign-up right now. Please try again later.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }

        private static bool KeysMatch(
            string suppliedKey,
            string expectedKey)
        {
            if (string.IsNullOrWhiteSpace(suppliedKey)
                || string.IsNullOrWhiteSpace(expectedKey))
            {
                return false;
            }

            var suppliedBytes =
                Encoding.UTF8.GetBytes(suppliedKey);

            var expectedBytes =
                Encoding.UTF8.GetBytes(expectedKey);

            return suppliedBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(
                    suppliedBytes,
                    expectedBytes);
        }
    }
}
