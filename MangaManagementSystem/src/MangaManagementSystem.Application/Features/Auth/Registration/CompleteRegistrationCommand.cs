using MangaManagementSystem.Application.DTOs.Auth;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed record CompleteRegistrationCommand(
        string Email,
        string Otp,
        byte[]? PortfolioFileBytes = null,
        string? PortfolioFileName = null,
        string? PortfolioContentType = null)
        : IRequest<UserDto>;
}
