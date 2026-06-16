
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;


namespace MangaManagementSystem.Application.Services
{
    public class UserService : IUserService
    {
        private const string StatusPendingApproval = "PENDING_APPROVAL";
        private const string StatusActive = "ACTIVE";
        private const string StatusDisabled = "DISABLED";
        private const string StatusRejected = "REJECTED";

        private static readonly HashSet<string> AllowedRoleNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "Mangaka",
            "Assistant",
            "Tantou Editor",
            "Editorial Board Member",
            "Editorial Board Chief"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOtpCacheService otpCacheService,
            IFileStorageService fileStorageService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _otpCacheService = otpCacheService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var roleName = NormalizeRoleName(dto.RoleName);
            EnsureValidRoleName(roleName);

            var username = dto.Username.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            var newUserId = await _unitOfWork.Users.CreateUserViaProcAsync(
                roleName,
                username,
                email,
                passwordHash,
                dto.DisplayName,
                dto.AvatarFileId,
                dto.PortfolioFileId,
                null);

            var created = await GetRequiredUserByIdForDtoAsync(newUserId);
            return created.ToDto();
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var entity = await GetUserByIdForDtoAsync(id);
            return entity is null ? null : entity.ToDto();
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var entity = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

            return entity is null ? null : entity.ToDto();
        }

        public async Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status)
        {
            var entities = await _unitOfWork.Users.GetByStatusAsync(status);
            return entities.Select(user => user.ToDto());
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
        {
            var entities = await _unitOfWork.Users.GetByRoleNameAsync(roleName);
            return entities.Select(user => user.ToDto());
        }

        public async Task<UserDto> ApproveUserAsync(Guid adminUserId, Guid userId)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User registration approved.");

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        public async Task RejectUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            await RequirePendingUserAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusRejected,
                string.IsNullOrWhiteSpace(reason)
                    ? "User registration rejected."
                    : reason.Trim());
        }

        public async Task<UserDto> ActivateUserAsync(Guid adminUserId, Guid userId)
        {
            await GetRequiredUserByIdAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusActive,
                "User account activated.");

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        public async Task<UserDto> DisableUserAsync(Guid adminUserId, Guid userId, string? reason = null)
        {
            await GetRequiredUserByIdAsync(userId);

            await _unitOfWork.Users.ChangeUserStatusViaProcAsync(
                adminUserId,
                userId,
                StatusDisabled,
                string.IsNullOrWhiteSpace(reason)
                    ? "User account disabled."
                    : reason.Trim());

            var updated = await GetRequiredUserByIdForDtoAsync(userId);
            return updated.ToDto();
        }

        private async Task<User> RequirePendingUserAsync(Guid userId)
        {
            var user = await GetRequiredUserByIdAsync(userId);

            if (!string.Equals(user.StatusCode, StatusPendingApproval, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"User {userId} cannot be processed because their status is '{user.StatusCode}', not '{StatusPendingApproval}'.");
            }

            return user;
        }

        private async Task<User> GetRequiredUserByIdAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            return user;
        }

        private async Task<User?> GetUserByIdForDtoAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            if (user.Role is not null)
            {
                return user;
            }

            /*
                Generic GetByIdAsync usually does not Include Role.
                Current UserRepository.GetByEmailAsync does Include Role, so we reload through email
                before mapping to UserDto.
            */
            var userWithRole = await _unitOfWork.Users.GetByEmailAsync(user.Email);
            return userWithRole ?? user;
        }

        private async Task<User> GetRequiredUserByIdForDtoAsync(Guid userId)
        {
            var user = await GetUserByIdForDtoAsync(userId);

            if (user is null)
            {
                throw new InvalidOperationException($"User {userId} was not found.");
            }

            return user;
        }

        private static void EnsureValidRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || !AllowedRoleNames.Contains(roleName))
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' is invalid. Allowed roles are: {string.Join(", ", AllowedRoleNames)}.");
            }
        }

        private static string NormalizeRoleName(string roleName)
            => roleName.Trim();

        public async Task<UserDto> UpdateDisplayNameAsync(
            Guid userId,
            string displayName)
        {
            await RequireExistingUserAsync(userId);

            var trimmedDisplayName =
                displayName?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedDisplayName))
            {
                throw new InvalidOperationException(
                    "Display name cannot be empty.");
            }

            await _unitOfWork.Users
                .UpdateDisplayNameViaProcAsync(
                    userId,
                    trimmedDisplayName);

            var updated =
                await GetRequiredUserByIdForDtoAsync(userId);

            return updated.ToDto();
        }

        public async Task<UserDto> UpdateAvatarFileAsync(
            Guid userId,
            FileUploadResultDto upload)
        {
            ArgumentNullException.ThrowIfNull(upload);

            UserFileReplacementResult replacementResult;

            try
            {
                await RequireExistingUserAsync(userId);

                ValidateUploadedFile(upload);

                var request =
                    new UserFileReplacementRequest(
                        userId,
                        upload.OriginalFileName,
                        upload.PublicId,
                        upload.SecureUrl,
                        upload.ContentType,
                        upload.FileSizeBytes,
                        upload.Sha256Hash!);

                replacementResult =
                    await _unitOfWork.Users
                        .UpdateAvatarFileViaProcAsync(request);
            }
            catch
            {
                await TryDeleteCloudinaryAssetAsync(
                    upload.PublicId,
                    upload.ContentType,
                    "new avatar after database failure");

                throw;
            }

            if (!string.Equals(
                    replacementResult.OldCloudinaryPublicId,
                    upload.PublicId,
                    StringComparison.Ordinal))
            {
                await TryDeleteCloudinaryAssetAsync(
                    replacementResult.OldCloudinaryPublicId,
                    replacementResult.OldContentType,
                    "old avatar after successful replacement");
            }

            var updated =
                await GetRequiredUserByIdForDtoAsync(userId);

            return updated.ToDto();
        }

        public async Task<UserDto> UpdatePortfolioFileAsync(
            Guid userId,
            FileUploadResultDto upload)
        {
            ArgumentNullException.ThrowIfNull(upload);

            UserFileReplacementResult replacementResult;

            try
            {
                await RequireExistingUserAsync(userId);

                ValidateUploadedFile(upload);

                var request =
                    new UserFileReplacementRequest(
                        userId,
                        upload.OriginalFileName,
                        upload.PublicId,
                        upload.SecureUrl,
                        upload.ContentType,
                        upload.FileSizeBytes,
                        upload.Sha256Hash!);

                replacementResult =
                    await _unitOfWork.Users
                        .UpdatePortfolioFileViaProcAsync(request);
            }
            catch
            {
                await TryDeleteCloudinaryAssetAsync(
                    upload.PublicId,
                    upload.ContentType,
                    "new portfolio after database failure");

                throw;
            }

            if (!string.Equals(
                    replacementResult.OldCloudinaryPublicId,
                    upload.PublicId,
                    StringComparison.Ordinal))
            {
                await TryDeleteCloudinaryAssetAsync(
                    replacementResult.OldCloudinaryPublicId,
                    replacementResult.OldContentType,
                    "old portfolio after successful replacement");
            }

            var updated =
                await GetRequiredUserByIdForDtoAsync(userId);

            return updated.ToDto();
        }

        public async Task ResetPasswordAsync(
            Guid userId,
            string newPassword)
        {
            await RequireExistingUserAsync(userId);

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new InvalidOperationException(
                    "New password cannot be empty.");
            }

            if (newPassword.Length < 8)
            {
                throw new InvalidOperationException(
                    "New password must be at least 8 characters.");
            }

            var passwordHash =
                _passwordHasher.HashPassword(newPassword);

            await _unitOfWork.Users.ResetPasswordViaProcAsync(
                userId,
                passwordHash);
        }

        public async Task SendProfileOtpAsync(
            Guid userId,
            string actionCode)
        {
            var user =
                await RequireExistingUserAsync(userId);

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException(
                    "User email is required for OTP verification.");
            }

            if (string.IsNullOrWhiteSpace(actionCode))
            {
                throw new InvalidOperationException(
                    "OTP action code is required.");
            }

            var otp = GenerateOtp();

            _otpCacheService.StoreProfileActionOtp(
                user.Email,
                actionCode,
                otp);

            await _emailService.SendOtpEmailAsync(
                user.Email,
                otp);
        }

        public async Task<bool> VerifyProfileOtpAsync(
            Guid userId,
            string actionCode,
            string otpCode)
        {
            var user =
                await RequireExistingUserAsync(userId);

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException(
                    "User email is required for OTP verification.");
            }

            if (string.IsNullOrWhiteSpace(actionCode)
                || string.IsNullOrWhiteSpace(otpCode))
            {
                return false;
            }

            return _otpCacheService
                .TryValidateAndRemoveProfileActionOtp(
                    user.Email,
                    actionCode,
                    otpCode.Trim());
        }

        public async Task RecordProfileAuditAsync(
            Guid actorUserId,
            string actionCode,
            string detailJson)
        {
            var user =
                await RequireExistingUserAsync(actorUserId);

            if (string.IsNullOrWhiteSpace(actionCode))
            {
                throw new InvalidOperationException(
                    "Audit action code is required.");
            }

            var role =
                await _unitOfWork.Roles
                    .GetByIdAsync(user.RoleId);

            var actorRoleName =
                role?.RoleName
                ?? user.Role?.RoleName;

            var entity =
                new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode =
                        actionCode.Trim().ToUpperInvariant(),
                    EntityType = "USER",
                    EntityId = actorUserId.ToString(),
                    DetailJson =
                        string.IsNullOrWhiteSpace(detailJson)
                            ? null
                            : detailJson
                };

            await _unitOfWork.AuditEvents.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<User> RequireExistingUserAsync(
            Guid userId)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException(
                    $"User {userId} was not found.");
            }

            return user;
        }

        private static string GenerateOtp()
        {
            var value =
                RandomNumberGenerator.GetInt32(
                    100000,
                    1000000);

            return value.ToString();
        }

        private static string ResolveCloudinaryResourceType(
            string? contentType)
        {
            return !string.IsNullOrWhiteSpace(contentType)
                   && contentType.StartsWith(
                       "image/",
                       StringComparison.OrdinalIgnoreCase)
                ? "image"
                : "raw";
        }

        private async Task TryDeleteCloudinaryAssetAsync(
            string? publicId,
            string? contentType,
            string cleanupContext)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            var resourceType =
                ResolveCloudinaryResourceType(contentType);

            try
            {
                await _fileStorageService.DeleteFileAsync(
                    publicId,
                    resourceType);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to clean up Cloudinary asset "
                    + "{PublicId} ({CleanupContext}).",
                    publicId,
                    cleanupContext);
            }
        }

        private static void ValidateUploadedFile(
            FileUploadResultDto upload)
        {
            if (string.IsNullOrWhiteSpace(upload.PublicId))
            {
                throw new InvalidOperationException(
                    "Cloudinary public id is required.");
            }

            if (string.IsNullOrWhiteSpace(upload.SecureUrl))
            {
                throw new InvalidOperationException(
                    "Cloudinary secure URL is required.");
            }

            if (string.IsNullOrWhiteSpace(upload.OriginalFileName))
            {
                throw new InvalidOperationException(
                    "Original file name is required.");
            }

            if (string.IsNullOrWhiteSpace(upload.ContentType))
            {
                throw new InvalidOperationException(
                    "File content type is required.");
            }

            if (upload.FileSizeBytes <= 0)
            {
                throw new InvalidOperationException(
                    "File size must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(upload.Sha256Hash)
                || upload.Sha256Hash.Length != 64)
            {
                throw new InvalidOperationException(
                    "A valid SHA-256 file hash is required.");
            }
        }

    }
}
