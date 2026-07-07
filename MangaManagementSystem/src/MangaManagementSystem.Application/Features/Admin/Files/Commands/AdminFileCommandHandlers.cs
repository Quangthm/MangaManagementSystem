using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.Features.Admin.Files.Queries;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Admin.Files.Commands
{
    internal static class AdminFileStorageCleanup
    {
        private const string CloudinaryRawResourceType = "raw";
        private const string CloudinaryImageResourceType = "image";

        internal static async Task<AdminFileCleanupResultDto> DeleteStorageAsync(
            Guid fileResourceId,
            AdminFileResourceDetail? detail,
            IFileStorageService fileStorageService,
            ILogger logger,
            bool requireDeleted)
        {
            if (detail is null)
            {
                return new AdminFileCleanupResultDto(
                    fileResourceId,
                    "-",
                    false,
                    "File resource was not found.");
            }

            if (requireDeleted && detail.DeletedAtUtc is null)
            {
                return new AdminFileCleanupResultDto(
                    detail.FileResourceId,
                    detail.OriginalFileName,
                    false,
                    "Only deleted file resources can be cleaned up.");
            }

            if (string.IsNullOrWhiteSpace(
                    detail.CloudinaryPublicId))
            {
                return new AdminFileCleanupResultDto(
                    detail.FileResourceId,
                    detail.OriginalFileName,
                    true,
                    "No Cloudinary public id was stored for this file.");
            }

            try
            {
                await fileStorageService.DeleteFileAsync(
                    detail.CloudinaryPublicId,
                    DetermineCloudinaryResourceType(
                        detail.ContentType));

                return new AdminFileCleanupResultDto(
                    detail.FileResourceId,
                    detail.OriginalFileName,
                    true,
                    "Cloudinary cleanup completed.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to clean up Cloudinary asset for file resource {FileResourceId}.",
                    detail.FileResourceId);

                return new AdminFileCleanupResultDto(
                    detail.FileResourceId,
                    detail.OriginalFileName,
                    false,
                    "Cloudinary cleanup failed: " + ex.Message);
            }
        }

        private static string DetermineCloudinaryResourceType(
            string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return CloudinaryImageResourceType;
            }

            return contentType.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase)
                ? CloudinaryImageResourceType
                : CloudinaryRawResourceType;
        }
    }

    public sealed class SoftDeleteAdminFileCommandHandler
        : IRequestHandler<
            SoftDeleteAdminFileCommand,
            AdminFileDetailDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;
        private readonly IFileStorageService
            _fileStorageService;
        private readonly ILogger<SoftDeleteAdminFileCommandHandler>
            _logger;

        public SoftDeleteAdminFileCommandHandler(
            IFileResourceRepository fileResourceRepository,
            IFileStorageService fileStorageService,
            ILogger<SoftDeleteAdminFileCommandHandler> logger)
        {
            _fileResourceRepository =
                fileResourceRepository;
            _fileStorageService =
                fileStorageService;
            _logger = logger;
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

            await AdminFileStorageCleanup.DeleteStorageAsync(
                detail.FileResourceId,
                detail,
                _fileStorageService,
                _logger,
                requireDeleted: false);

            return AdminFileDtoMapper
                .ToDetailDto(detail);
        }
    }

    public sealed class CleanupAdminFileStorageCommandHandler
        : IRequestHandler<
            CleanupAdminFileStorageCommand,
            AdminFileCleanupResultDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;
        private readonly IFileStorageService
            _fileStorageService;
        private readonly ILogger<CleanupAdminFileStorageCommandHandler>
            _logger;

        public CleanupAdminFileStorageCommandHandler(
            IFileResourceRepository fileResourceRepository,
            IFileStorageService fileStorageService,
            ILogger<CleanupAdminFileStorageCommandHandler> logger)
        {
            _fileResourceRepository =
                fileResourceRepository;
            _fileStorageService =
                fileStorageService;
            _logger = logger;
        }

        public async Task<AdminFileCleanupResultDto> Handle(
            CleanupAdminFileStorageCommand request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            AdminFileValidation.ValidateFileResourceId(
                request.FileResourceId);

            var detail =
                await _fileResourceRepository
                    .GetAdminByIdAsync(
                        request.ActorUserId,
                        request.FileResourceId,
                        cancellationToken);

            return await AdminFileStorageCleanup.DeleteStorageAsync(
                request.FileResourceId,
                detail,
                _fileStorageService,
                _logger,
                requireDeleted: true);
        }
    }

    public sealed class CleanupDeletedAdminFilesStorageCommandHandler
        : IRequestHandler<
            CleanupDeletedAdminFilesStorageCommand,
            AdminFileCleanupBatchResultDto>
    {
        private readonly IFileResourceRepository
            _fileResourceRepository;
        private readonly IFileStorageService
            _fileStorageService;
        private readonly ILogger<CleanupDeletedAdminFilesStorageCommandHandler>
            _logger;

        public CleanupDeletedAdminFilesStorageCommandHandler(
            IFileResourceRepository fileResourceRepository,
            IFileStorageService fileStorageService,
            ILogger<CleanupDeletedAdminFilesStorageCommandHandler> logger)
        {
            _fileResourceRepository =
                fileResourceRepository;
            _fileStorageService =
                fileStorageService;
            _logger = logger;
        }

        public async Task<AdminFileCleanupBatchResultDto> Handle(
            CleanupDeletedAdminFilesStorageCommand request,
            CancellationToken cancellationToken)
        {
            AdminFileValidation.ValidateActorUserId(
                request.ActorUserId);

            var batchSize =
                request.BatchSize < 1
                    ? 20
                    : Math.Min(
                        request.BatchSize,
                        100);

            var searchResult =
                await _fileResourceRepository
                    .SearchAdminAsync(
                        new AdminFileResourceSearchCriteria(
                            request.ActorUserId,
                            null,
                            null,
                            "DELETED",
                            null,
                            null,
                            1,
                            batchSize),
                        cancellationToken);

            var results =
                new List<AdminFileCleanupResultDto>();

            foreach (var item in searchResult.Items)
            {
                var detail =
                    await _fileResourceRepository
                        .GetAdminByIdAsync(
                            request.ActorUserId,
                            item.FileResourceId,
                            cancellationToken);

                var cleanupResult =
                    await AdminFileStorageCleanup.DeleteStorageAsync(
                        item.FileResourceId,
                        detail,
                        _fileStorageService,
                        _logger,
                        requireDeleted: true);

                results.Add(cleanupResult);
            }

            return new AdminFileCleanupBatchResultDto(
                results,
                results.Count,
                results.Count(result => result.Succeeded),
                results.Count(result => !result.Succeeded));
        }
    }
}
