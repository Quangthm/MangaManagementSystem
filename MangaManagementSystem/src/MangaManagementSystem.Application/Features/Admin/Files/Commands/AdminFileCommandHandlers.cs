using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.Features.Admin.Files.Queries;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Files.Commands
{
    public sealed class SoftDeleteAdminFileCommandHandler
        : IRequestHandler<
            SoftDeleteAdminFileCommand,
            AdminFileDetailDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        public SoftDeleteAdminFileCommandHandler(
            IFileResourceRepository fileResourceRepository)
        {
            _fileResourceRepository =
                fileResourceRepository;
        }

        public async Task<AdminFileDetailDto> Handle(
            SoftDeleteAdminFileCommand request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidateFileResourceId(
                request.FileResourceId);

            var deleteReason =
                AdminFileValidation.NormalizeDeleteReason(
                    request.DeleteReason);

            await _fileResourceRepository
                .SoftDeleteAdminAsync(
                    request.ActorUserId,
                    request.FileResourceId,
                    deleteReason,
                    cancellationToken);

            var detail =
                await _fileResourceRepository
                    .GetAdminByIdAsync(
                        request.ActorUserId,
                        request.FileResourceId,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "The deleted file resource could not be reloaded.");

            return AdminFileDtoMapper
                .ToDetailDto(detail);
        }
    }

    public sealed class CleanupAdminFileStorageCommandHandler
        : IRequestHandler<
            CleanupAdminFileStorageCommand,
            AdminFileStorageCleanupResultDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        private readonly IUserRepository
            _userRepository;

        private readonly IFileStorageService
            _fileStorageService;

        public CleanupAdminFileStorageCommandHandler(
            IFileResourceRepository fileResourceRepository,
            IUserRepository userRepository,
            IFileStorageService fileStorageService)
        {
            _fileResourceRepository =
                fileResourceRepository;

            _userRepository =
                userRepository;

            _fileStorageService =
                fileStorageService;
        }

        public async Task<AdminFileStorageCleanupResultDto> Handle(
            CleanupAdminFileStorageCommand request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidateFileResourceId(
                request.FileResourceId);

            await EnsureActiveAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var reason =
                NormalizeCleanupReason(
                    request.Reason);

            var file =
                await _fileResourceRepository
                    .GetAdminByIdAsync(
                        request.ActorUserId,
                        request.FileResourceId,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "File resource does not exist.");

            return await CleanupOneAsync(
                request.ActorUserId,
                file,
                reason,
                cancellationToken);
        }

        private async Task<AdminFileStorageCleanupResultDto>
            CleanupOneAsync(
                Guid actorUserId,
                AdminFileResourceDetail file,
                string? reason,
                CancellationToken cancellationToken)
        {
            ValidateCleanupEligibility(file);

            var cleanupStatus =
                "FAILED";

            string? cleanupError = null;

            if (string.IsNullOrWhiteSpace(
                    file.CloudinaryPublicId))
            {
                cleanupError =
                    "Cloudinary public id is missing.";
            }
            else
            {
                var deleteResult =
                    await _fileStorageService
                        .DeleteFileAsync(
                            file.CloudinaryPublicId,
                            ResolveCloudinaryResourceType(
                                file.ContentType));

                if (deleteResult.Success)
                {
                    cleanupStatus =
                        "CLEANED";
                }
                else if (deleteResult.NotFound)
                {
                    cleanupStatus =
                        "MISSING";
                }
                else
                {
                    cleanupStatus =
                        "FAILED";

                    cleanupError =
                        string.IsNullOrWhiteSpace(
                            deleteResult.Error)
                            ? "Cloudinary cleanup failed."
                            : deleteResult.Error;
                }
            }

            await _fileResourceRepository
                .UpdateStorageCleanupResultAsync(
                    actorUserId,
                    file.FileResourceId,
                    cleanupStatus,
                    cleanupError,
                    reason,
                    cancellationToken);

            var message =
                cleanupStatus switch
                {
                    "CLEANED" =>
                        "The file was cleaned from storage and audited.",

                    "MISSING" =>
                        "The file was already missing from storage and was audited.",

                    _ =>
                        "The file storage cleanup failed and was audited."
                };

            return new AdminFileStorageCleanupResultDto(
                file.FileResourceId,
                "Deleted",
                cleanupStatus,
                message);
        }

        private async Task EnsureActiveAdminAsync(
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            var actor =
                await _userRepository
                    .GetByIdWithRoleAsync(
                        actorUserId,
                        cancellationToken);

            if (actor is null
                || !string.Equals(
                    actor.StatusCode,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    actor.Role?.RoleName,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Actor is not an active administrator.");
            }
        }

        private static void ValidateCleanupEligibility(
            AdminFileResourceDetail file)
        {
            if (!file.DeletedAtUtc.HasValue)
            {
                throw new InvalidOperationException(
                    "Only deleted file resources can be cleaned from storage.");
            }

            if (string.Equals(
                    file.StorageCleanupStatus,
                    "CLEANED",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    file.StorageCleanupStatus,
                    "MISSING",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "File resource storage has already been cleaned.");
            }
        }

        private static string? NormalizeCleanupReason(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized =
                value.Trim();

            if (normalized.Length > 500)
            {
                throw new InvalidOperationException(
                    "Cleanup reason cannot exceed 500 characters.");
            }

            return normalized;
        }

        private static string ResolveCloudinaryResourceType(
            string contentType)
        {
            return contentType.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase)
                ? "image"
                : "raw";
        }
    }

    public sealed class CleanupDeletedAdminFilesCommandHandler
        : IRequestHandler<
            CleanupDeletedAdminFilesCommand,
            AdminFileStorageCleanupBatchResultDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;

        private readonly IUserRepository
            _userRepository;

        private readonly IFileStorageService
            _fileStorageService;

        public CleanupDeletedAdminFilesCommandHandler(
            IFileResourceRepository fileResourceRepository,
            IUserRepository userRepository,
            IFileStorageService fileStorageService)
        {
            _fileResourceRepository =
                fileResourceRepository;

            _userRepository =
                userRepository;

            _fileStorageService =
                fileStorageService;
        }

        public async Task<AdminFileStorageCleanupBatchResultDto> Handle(
            CleanupDeletedAdminFilesCommand request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            await EnsureActiveAdminAsync(
                request.ActorUserId,
                cancellationToken);

            var candidates =
                await _fileResourceRepository
                    .GetStorageCleanupCandidatesAsync(
                        cancellationToken);

            var cleaned = 0;
            var missing = 0;
            var failed = 0;
            var skipped = 0;

            foreach (var file in candidates)
            {
                try
                {
                    if (!file.DeletedAtUtc.HasValue)
                    {
                        skipped++;
                        continue;
                    }

                    if (string.Equals(
                            file.StorageCleanupStatus,
                            "CLEANED",
                            StringComparison.OrdinalIgnoreCase)
                        || string.Equals(
                            file.StorageCleanupStatus,
                            "MISSING",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    var cleanupStatus =
                        "FAILED";

                    string? cleanupError = null;

                    if (string.IsNullOrWhiteSpace(
                            file.CloudinaryPublicId))
                    {
                        cleanupError =
                            "Cloudinary public id is missing.";
                    }
                    else
                    {
                        var deleteResult =
                            await _fileStorageService
                                .DeleteFileAsync(
                                    file.CloudinaryPublicId,
                                    ResolveCloudinaryResourceType(
                                        file.ContentType));

                        if (deleteResult.Success)
                        {
                            cleanupStatus =
                                "CLEANED";
                        }
                        else if (deleteResult.NotFound)
                        {
                            cleanupStatus =
                                "MISSING";
                        }
                        else
                        {
                            cleanupError =
                                string.IsNullOrWhiteSpace(
                                    deleteResult.Error)
                                    ? "Cloudinary cleanup failed."
                                    : deleteResult.Error;
                        }
                    }

                    await _fileResourceRepository
                        .UpdateStorageCleanupResultAsync(
                            request.ActorUserId,
                            file.FileResourceId,
                            cleanupStatus,
                            cleanupError,
                            "cleanup deleted file from Cloudinary",
                            cancellationToken);

                    switch (cleanupStatus)
                    {
                        case "CLEANED":
                            cleaned++;
                            break;

                        case "MISSING":
                            missing++;
                            break;

                        default:
                            failed++;
                            break;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            return new AdminFileStorageCleanupBatchResultDto(
                candidates.Count,
                cleaned,
                missing,
                failed,
                skipped);
        }

        private async Task EnsureActiveAdminAsync(
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            var actor =
                await _userRepository
                    .GetByIdWithRoleAsync(
                        actorUserId,
                        cancellationToken);

            if (actor is null
                || !string.Equals(
                    actor.StatusCode,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    actor.Role?.RoleName,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Actor is not an active administrator.");
            }
        }

        private static string ResolveCloudinaryResourceType(
            string contentType)
        {
            return contentType.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase)
                ? "image"
                : "raw";
        }
    }
}
