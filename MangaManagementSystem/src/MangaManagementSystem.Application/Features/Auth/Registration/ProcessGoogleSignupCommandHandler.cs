using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed class ProcessGoogleSignupCommandHandler
        : IRequestHandler<
            ProcessGoogleSignupCommand,
            GoogleSignupCallbackResult>
    {
        private readonly IAuthService _authService;

        public ProcessGoogleSignupCommandHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<GoogleSignupCallbackResult> Handle(
            ProcessGoogleSignupCommand request,
            CancellationToken cancellationToken)
        {
            var normalizedRoleName =
                PublicRegistrationRoles.NormalizeOrThrow(
                    request.RoleName);

            return await _authService
                .ProcessGoogleSignupCallbackAsync(
                    request.Email,
                    request.GoogleDisplayName,
                    normalizedRoleName);
        }
    }
}
