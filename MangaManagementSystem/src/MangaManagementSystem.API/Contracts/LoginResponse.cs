using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.API.Contracts;

public sealed record LoginResponse(
    UserDto User,
    string RoleName,
    string AccessToken,
    DateTime ExpiresAtUtc);