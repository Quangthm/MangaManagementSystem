using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        /// <summary>
        /// Default role name assigned to new registrations (Google signup and standard OTP signup).
        /// Resolved by name from auth.Roles at runtime.
        /// </summary>
        private const string DefaultRegistrationRoleName = "Mangaka";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly ILogger<AuthService> _logger;
        private readonly IFileStorageService _fileStorageService;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOtpCacheService otpCacheService,
            ILogger<AuthService> logger,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _otpCacheService = otpCacheService;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }

        public async Task<bool> SendRegistrationOtpAsync(RegisterDto request)
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            var trimmedUsername = request.Username.Trim();

            await EnsureEmailAndUsernameAvailableAsync(normalizedEmail, trimmedUsername);

            var otp = GenerateOtp();
            var cachedRequest = request with
            {
                Email = normalizedEmail,
                Username = trimmedUsername
            };

            _otpCacheService.StoreRegistrationOtp(normalizedEmail, otp, cachedRequest);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otp);

            return true;
        }

        public async Task<UserDto> CompleteRegistrationWithOtpAsync(
            string email,
            string otp,
            byte[]? portfolioFileBytes = null,
            string? portfolioFileName = null,
            string? portfolioContentType = null)
        {
            var normalizedEmail = NormalizeEmail(email);
            var pendingRegistration = _otpCacheService.TryValidateAndRemoveRegistrationOtp(normalizedEmail, otp);

            if (pendingRegistration is null)
            {
                throw new InvalidOperationException("The verification code is invalid or has expired.");
            }

            // When the user uploads portfolio at step 2 (multipart complete), override the
            // cached registration's portfolio fields so the existing upload/linking logic applies.
            if (portfolioFileBytes is not null)
            {
                pendingRegistration = pendingRegistration with
                {
                    PortfolioFileBytes = portfolioFileBytes,
                    PortfolioFileName = portfolioFileName,
                    PortfolioContentType = portfolioContentType
                };
            }

            await EnsureEmailAndUsernameAvailableAsync(normalizedEmail, pendingRegistration.Username);

            var roleName = pendingRegistration.RoleName;
            var passwordHash = _passwordHasher.HashPassword(pendingRegistration.Password);

            string? portfolioPublicId = null;
            string? portfolioSecureUrl = null;
            string? portfolioUploadContentType = null;
            long? portfolioFileSize = null;
            string? portfolioOriginalFileName = null;
            string? portfolioSha256 = null;

            if (pendingRegistration.PortfolioFileBytes is { Length: > 0 })
            {
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    pendingRegistration.PortfolioFileBytes,
                    pendingRegistration.PortfolioFileName ?? "portfolio",
                    pendingRegistration.PortfolioContentType ?? "application/octet-stream",
                    "REGISTRATION_PORTFOLIO",
                    null);

                portfolioPublicId = uploadResult.PublicId;
                portfolioSecureUrl = uploadResult.SecureUrl;
                portfolioUploadContentType = uploadResult.ContentType;
                portfolioFileSize = uploadResult.FileSizeBytes;
                portfolioOriginalFileName = uploadResult.OriginalFileName;
                portfolioSha256 = uploadResult.Sha256Hash;
            }

            Guid newUserId;

            try
            {
                (newUserId, _) = await _unitOfWork.Users.CreateUserWithOptionalPortfolioAsync(
                    roleName,
                    pendingRegistration.Username,
                    normalizedEmail,
                    passwordHash,
                    pendingRegistration.DisplayName,
                    null,
                    portfolioOriginalFileName,
                    portfolioPublicId,
                    portfolioSecureUrl,
                    portfolioUploadContentType,
                    portfolioFileSize,
                    portfolioSha256,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to complete registration for email {Email}. Attempting uploaded portfolio cleanup if needed.",
                    normalizedEmail);

                if (!string.IsNullOrEmpty(portfolioPublicId))
                {
                    await TryDeleteUploadedPortfolioAsync(portfolioPublicId, portfolioUploadContentType);
                }

                throw;
            }

            // Use GetByEmailAsync instead of generic GetByIdAsync because UserRepository.GetByEmailAsync includes Role.
            var created = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
            if (created is null || created.UserId != newUserId)
            {
                throw new InvalidOperationException("Failed to load created user.");
            }

            return created.ToDto();
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto request)
        {
            var loginIdentifier = ResolveLoginIdentifier(request.UsernameOrEmail);
            var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(loginIdentifier);

            if (user is null)
            {
                _logger.LogWarning(
                    "Login failed: User not found for identifier {LoginIdentifier}",
                    loginIdentifier);

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

            var statusFailure = ValidatePasswordLoginStatus(user);
            if (statusFailure is not null)
            {
                _logger.LogWarning(
                    "Login failed: User {UserId} ({Username}) has status {StatusCode}",
                    user.UserId,
                    user.Username,
                    user.StatusCode);

                return statusFailure;
            }

            var authResult = BuildSuccessfulAuthResult(user, "Login failed");

            if (authResult.Succeeded)
            {
                _logger.LogInformation(
                    "Login succeeded for user {UserId} ({Username}) with role {RoleName}",
                    user.UserId,
                    user.Username,
                    authResult.RoleName);
            }

            return authResult;
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

            if (user is null)
            {
                _logger.LogWarning(
                    "Google login failed: No user found for email {Email}",
                    normalizedEmail);

                return new AuthResultDto(false, null, null, "User not found.");
            }

            if (!string.Equals(user.StatusCode, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Google login failed: User {UserId} ({Email}) is not ACTIVE. Current status: {StatusCode}",
                    user.UserId,
                    normalizedEmail,
                    user.StatusCode);

                return new AuthResultDto(false, null, null, "User is not active.");
            }

            var authResult = BuildSuccessfulAuthResult(user, "Google login failed");

            if (authResult.Succeeded)
            {
                _logger.LogInformation(
                    "Google login lookup succeeded for user {UserId} ({Email}) with role {RoleName}",
                    user.UserId,
                    normalizedEmail,
                    authResult.RoleName);
            }

            return authResult;
        }

        public async Task<GoogleSignupCallbackResult> ProcessGoogleSignupCallbackAsync(
            string email,
            string? googleDisplayName)
        {
            var normalizedEmail = NormalizeEmail(email);
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

            if (existingUser is null)
            {
                var username = await GenerateUniqueUsernameAsync(googleDisplayName, normalizedEmail);
                var passwordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N") + "!Aa1");

                var newUserId = await _unitOfWork.Users.CreateUserViaProcAsync(
                    DefaultRegistrationRoleName,
                    username,
                    normalizedEmail,
                    passwordHash,
                    googleDisplayName,
                    null,
                    null,
                    null);

                await SendEmailVerificationOtpAsync(normalizedEmail);

                _logger.LogInformation(
                    "Google sign-up created pending user {UserId} ({Email}) with username {Username}",
                    newUserId,
                    normalizedEmail,
                    username);

                return new GoogleSignupCallbackResult(
                    GoogleSignupFlow.NewUserVerifyOtp,
                    normalizedEmail);
            }

            if (string.Equals(existingUser.StatusCode, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase))
            {
                await SendEmailVerificationOtpAsync(normalizedEmail);

                _logger.LogInformation(
                    "Google sign-up resumed OTP verification for pending user {UserId} ({Email})",
                    existingUser.UserId,
                    normalizedEmail);

                return new GoogleSignupCallbackResult(
                    GoogleSignupFlow.PendingApprovalVerifyOtp,
                    normalizedEmail);
            }

            if (string.Equals(existingUser.StatusCode, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return BuildActiveGoogleSignupLoginResult(existingUser, normalizedEmail);
            }

            _logger.LogWarning(
                "Google sign-up rejected for user {UserId} ({Email}) with status {StatusCode}",
                existingUser.UserId,
                normalizedEmail,
                existingUser.StatusCode);

            return new GoogleSignupCallbackResult(
                GoogleSignupFlow.Rejected,
                normalizedEmail,
                ErrorMessage: "This account cannot be used for sign-up. Contact support.");
        }

        public async Task<bool> SendEmailVerificationOtpAsync(string email)
        {
            var normalizedEmail = NormalizeEmail(email);

            await GetPendingApprovalUserByNormalizedEmailAsync(
                normalizedEmail,
                "Email verification is only available for pending accounts.");

            var otp = GenerateOtp();
            _otpCacheService.StoreEmailVerificationOtp(normalizedEmail, otp);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otp);

            return true;
        }

        public async Task<bool> CompleteEmailVerificationOtpAsync(string email, string otp)
        {
            var normalizedEmail = NormalizeEmail(email);

            if (!_otpCacheService.TryValidateAndRemoveEmailVerificationOtp(normalizedEmail, otp))
            {
                throw new InvalidOperationException("The verification code is invalid or has expired.");
            }

            var user = await GetPendingApprovalUserByNormalizedEmailAsync(
                normalizedEmail,
                "This account is not awaiting email verification.");

            _logger.LogInformation(
                "Email verified via OTP for pending user {UserId} ({Email}). Awaiting admin approval.",
                user.UserId,
                normalizedEmail);

            return true;
        }

        private async Task EnsureEmailAndUsernameAvailableAsync(string normalizedEmail, string username)
        {
            if (await _unitOfWork.Users.GetByEmailAsync(normalizedEmail) is not null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            if (await _unitOfWork.Users.GetByUsernameAsync(username.Trim()) is not null)
            {
                throw new InvalidOperationException("This username is already taken.");
            }
        }

        private AuthResultDto? ValidatePasswordLoginStatus(User user)
        {
            return user.StatusCode.ToUpperInvariant() switch
            {
                "ACTIVE" => null,
                "PENDING_APPROVAL" => new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account pending admin approval."),

                "REJECTED" => new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account registration was rejected."),

                "DISABLED" => new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account is disabled."),

                _ => new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account configuration is invalid. Contact support.")
            };
        }

        private AuthResultDto BuildSuccessfulAuthResult(User user, string failureContext)
        {
            try
            {
                var userDto = user.ToDto();
                return new AuthResultDto(true, userDto, userDto.RoleName, null);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "{FailureContext}: Role was not loaded for user {UserId} ({Username})",
                    failureContext,
                    user.UserId,
                    user.Username);

                return new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account configuration is invalid. Contact support.");
            }
        }

        private GoogleSignupCallbackResult BuildActiveGoogleSignupLoginResult(
            User user,
            string normalizedEmail)
        {
            try
            {
                var userDto = user.ToDto();

                return new GoogleSignupCallbackResult(
                    GoogleSignupFlow.ActiveUserLogin,
                    normalizedEmail,
                    userDto,
                    userDto.RoleName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Google sign-up failed: Role was not loaded for user {UserId} ({Email})",
                    user.UserId,
                    normalizedEmail);

                return new GoogleSignupCallbackResult(
                    GoogleSignupFlow.Rejected,
                    normalizedEmail,
                    ErrorMessage: "Account configuration is invalid. Contact support.");
            }
        }

        private async Task<User> GetPendingApprovalUserByNormalizedEmailAsync(
            string normalizedEmail,
            string invalidStatusMessage)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

            if (user is null)
            {
                throw new InvalidOperationException("No account found for this email.");
            }

            if (!string.Equals(user.StatusCode, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(invalidStatusMessage);
            }

            return user;
        }

        private async Task TryDeleteUploadedPortfolioAsync(
            string portfolioPublicId,
            string? portfolioContentType)
        {
            try
            {
                var resourceType =
                    !string.IsNullOrEmpty(portfolioContentType)
                    && portfolioContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                        ? "image"
                        : "raw";

                await _fileStorageService.DeleteFileAsync(portfolioPublicId, resourceType);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(
                    cleanupEx,
                    "Failed to delete Cloudinary asset {PublicId} after DB failure.",
                    portfolioPublicId);
            }
        }

        private async Task<string> GenerateUniqueUsernameAsync(string? googleDisplayName, string email)
        {
            var baseUsername = BuildBaseUsername(googleDisplayName, email);
            var candidate = baseUsername;
            var suffix = 0;

            while (await _unitOfWork.Users.GetByUsernameAsync(candidate) is not null)
            {
                suffix++;
                candidate = $"{baseUsername}{suffix}";
            }

            return candidate;
        }

        private static string BuildBaseUsername(string? googleDisplayName, string email)
        {
            var fromName = string.IsNullOrWhiteSpace(googleDisplayName)
                ? string.Empty
                : new string(googleDisplayName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            if (fromName.Length >= 3)
            {
                return fromName.Length > 50 ? fromName[..50] : fromName;
            }

            var localPart = email.Split('@')[0];
            var fromEmail = new string(localPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            if (fromEmail.Length >= 3)
            {
                return fromEmail.Length > 50 ? fromEmail[..50] : fromEmail;
            }

            return $"user{Random.Shared.Next(1000, 9999)}";
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
    }
}