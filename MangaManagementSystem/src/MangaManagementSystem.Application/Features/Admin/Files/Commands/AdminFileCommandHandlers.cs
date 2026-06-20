using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.Features.Admin.Files.Queries;
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
}
