using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        /// <summary>
        /// auth.Roles seed order: 1 = Mangaka, 2 = Assistant, 3 = Tantou Editor, 4 = Editorial Board Member, 5 = Admin.
        /// </summary>
        private const short DefaultRegistrationRoleId = 1;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOtpCacheService otpCacheService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _otpCacheService = otpCacheService;
            _logger = logger;
        }

        public async Task<bool> SendRegistrationOtpAsync(RegisterDto request)
        {
            var normalizedEmail = NormalizeEmail(request.Email);

            if (await _unitOfWork.Users.GetByEmailAsync(normalizedEmail) != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            if (await _unitOfWork.Users.GetByUsernameAsync(request.Username.Trim()) != null)
            {
                throw new InvalidOperationException("This username is already taken.");
            }

            var otp = GenerateOtp();
            var cachedRequest = request with
            {
                Email = normalizedEmail,
                Username = request.Username.Trim()
            };

            _otpCacheService.StoreRegistrationOtp(normalizedEmail, otp, cachedRequest);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otp);
            return true;
        }

        public async Task<UserDto> CompleteRegistrationWithOtpAsync(string email, string otp)
        {
            var normalizedEmail = NormalizeEmail(email);
            var pendingRegistration = _otpCacheService.TryValidateAndRemoveRegistrationOtp(normalizedEmail, otp);

            if (pendingRegistration == null)
            {
                throw new InvalidOperationException("The verification code is invalid or has expired.");
            }

            if (await _unitOfWork.Users.GetByEmailAsync(normalizedEmail) != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            if (await _unitOfWork.Users.GetByUsernameAsync(pendingRegistration.Username) != null)
            {
                throw new InvalidOperationException("This username is already taken.");
            }

            var user = new User
            {
                RoleId = DefaultRegistrationRoleId,
                Username = pendingRegistration.Username,
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.HashPassword(pendingRegistration.Password),
                Status = "PENDING_APPROVAL",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto request)
        {
            var loginIdentifier = ResolveLoginIdentifier(request.UsernameOrEmail);
            var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(loginIdentifier);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for identifier {LoginIdentifier}", loginIdentifier);
                return new AuthResultDto(false, null, null, "Invalid credentials");
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning(
                    "Login failed: Invalid password for user {UserId} ({Username})",
                    user.UserId,
                    user.Username);
                return new AuthResultDto(false, null, null, "Invalid credentials");
            }

            if (user.Status == "PENDING_APPROVAL")
            {
                _logger.LogWarning(
                    "Login failed: Account pending admin approval for user {UserId} ({Username})",
                    user.UserId,
                    user.Username);
                return new AuthResultDto(false, null, null, "Account pending admin approval.");
            }

            if (user.Status == "DISABLED")
            {
                _logger.LogWarning(
                    "Login failed: Account disabled for user {UserId} ({Username})",
                    user.UserId,
                    user.Username);
                return new AuthResultDto(false, null, null, "Account is disabled.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            if (role == null || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _logger.LogWarning(
                    "Login failed: Role {RoleId} not found for user {UserId} ({Username})",
                    user.RoleId,
                    user.UserId,
                    user.Username);
                return new AuthResultDto(false, null, null, "Account configuration is invalid. Contact support.");
            }

            _logger.LogInformation(
                "Login succeeded for user {UserId} ({Username}) with role {RoleName}",
                user.UserId,
                user.Username,
                role.RoleName);

            return new AuthResultDto(true, MapToDto(user), role.RoleName, null);
        }

        public async Task<AuthResultDto> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Google login failed: Email claim was empty");
                return new AuthResultDto(false, null, null, "Email is required.");
            }

            var normalizedEmail = NormalizeEmail(email);
            var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

            if (user == null)
            {
                _logger.LogWarning("Google login failed: No user found for email {Email}", normalizedEmail);
                return new AuthResultDto(false, null, null, "User not found.");
            }

            if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Google login failed: User {UserId} ({Email}) is not ACTIVE (status: {Status})",
                    user.UserId,
                    normalizedEmail,
                    user.Status);
                return new AuthResultDto(false, null, null, "User is not active.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            if (role == null || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _logger.LogWarning(
                    "Google login failed: Role {RoleId} not found for user {UserId}",
                    user.RoleId,
                    user.UserId);
                return new AuthResultDto(false, null, null, "Account configuration is invalid. Contact support.");
            }

            _logger.LogInformation(
                "Google login lookup succeeded for user {UserId} ({Email}) with role {RoleName}",
                user.UserId,
                normalizedEmail,
                role.RoleName);

            return new AuthResultDto(true, MapToDto(user), role.RoleName, null);
        }

        private static string GenerateOtp()
            => Random.Shared.Next(100000, 999999).ToString();

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        private static string ResolveLoginIdentifier(string usernameOrEmail)
        {
            var trimmed = usernameOrEmail.Trim();
            return trimmed.Contains('@') ? NormalizeEmail(trimmed) : trimmed;
        }

        private static UserDto MapToDto(User user) => new(
            user.UserId,
            user.RoleId,
            user.Username,
            user.Email,
            user.AvatarFileId,
            user.PortfolioFileId,
            user.Status,
            user.CreatedAt
        );
    }
}
