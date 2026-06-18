using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Queries
{
    public sealed class AuthenticateUserQueryHandler
        : IRequestHandler<
            AuthenticateUserQuery,
            AuthResultDto>
    {
        private readonly IAuthService _authService;

        public AuthenticateUserQueryHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResultDto> Handle(
            AuthenticateUserQuery request,
            CancellationToken cancellationToken)
        {
            return _authService.LoginAsync(
                new LoginDto(
                    request.UsernameOrEmail,
                    request.Password));
        }
    }

    public sealed class ResolveGoogleLoginQueryHandler
        : IRequestHandler<
            ResolveGoogleLoginQuery,
            AuthResultDto>
    {
        private readonly IAuthService _authService;

        public ResolveGoogleLoginQueryHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResultDto> Handle(
            ResolveGoogleLoginQuery request,
            CancellationToken cancellationToken)
        {
            return _authService.GetUserByEmailAsync(
                request.Email);
        }
    }
}
