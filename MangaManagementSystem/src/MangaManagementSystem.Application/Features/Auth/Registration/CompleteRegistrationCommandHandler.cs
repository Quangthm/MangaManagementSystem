using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed class CompleteRegistrationCommandHandler
        : IRequestHandler<
            CompleteRegistrationCommand,
            UserDto>
    {
        private readonly IAuthService _authService;

        public CompleteRegistrationCommandHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public Task<UserDto> Handle(
            CompleteRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            return _authService
                .CompleteRegistrationWithOtpAsync(
                    request.Email,
                    request.Otp,
                    request.PortfolioFileBytes,
                    request.PortfolioFileName,
                    request.PortfolioContentType);
        }
    }
}
