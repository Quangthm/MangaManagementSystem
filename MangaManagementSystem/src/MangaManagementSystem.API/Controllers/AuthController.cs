using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
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

        var result = await _authService.LoginAsync(
            new LoginDto(request.UsernameOrEmail, request.Password));

        if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
        {
            return Unauthorized(new ApiErrorResponse(
                result.ErrorMessage ?? "Invalid credentials"));
        }

        var expiresAtUtc = DateTime.UtcNow.AddDays(14);
        var accessToken = GenerateJwtToken(result.User, result.RoleName, expiresAtUtc);

        var response = new LoginResponse(
            result.User,
            result.RoleName,
            accessToken,
            expiresAtUtc);

        return Ok(response);
    }

    private string GenerateJwtToken(
        UserDto user,
        string roleName,
        DateTime expiresAtUtc)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName),
            new("user_id", user.UserId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}