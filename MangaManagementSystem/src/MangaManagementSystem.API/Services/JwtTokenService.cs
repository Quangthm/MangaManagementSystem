using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MangaManagementSystem.API.Services
{
    public sealed class JwtTokenService
        : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public JwtTokenIssueResult IssueToken(
            UserDto user,
            string roleName)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new InvalidOperationException(
                    "Role name is required to issue a JWT.");
            }

            var expiresAtUtc =
                DateTime.UtcNow.AddMinutes(
                    ResolveExpireMinutes());

            var accessToken =
                GenerateJwtToken(
                    user,
                    roleName,
                    expiresAtUtc);

            return new JwtTokenIssueResult(
                accessToken,
                expiresAtUtc);
        }

        private int ResolveExpireMinutes()
        {
            var configuredValue =
                _configuration["Jwt:ExpireMinutes"];

            if (!int.TryParse(
                    configuredValue,
                    out var expireMinutes)
                || expireMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:ExpireMinutes must be configured as a positive integer.");
            }

            return expireMinutes;
        }

        private string GenerateJwtToken(
            UserDto user,
            string roleName,
            DateTime expiresAtUtc)
        {
            var jwtKey =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is missing.");

            var jwtIssuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "Jwt:Issuer is missing.");

            var jwtAudience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "Jwt:Audience is missing.");

            var claims =
                new List<Claim>
                {
                    new(
                        JwtRegisteredClaimNames.Sub,
                        user.UserId.ToString("D")),
                    new(
                        ClaimTypes.NameIdentifier,
                        user.UserId.ToString("D")),
                    new(
                        ClaimTypes.Name,
                        user.Username),
                    new(
                        ClaimTypes.Email,
                        user.Email),
                    new(
                        ClaimTypes.Role,
                        roleName),
                    new(
                        "user_id",
                        user.UserId.ToString("D"))
                };

            var signingKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        jwtKey));

            var credentials =
                new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: expiresAtUtc,
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}