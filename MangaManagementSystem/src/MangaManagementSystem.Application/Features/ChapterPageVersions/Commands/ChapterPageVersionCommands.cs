using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Policies;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Features.ChapterPageVersions.Commands
{
    public record UpdateChapterPageVersionCommand(UpdateChapterPageVersionDto Dto) : IRequest<ChapterPageVersionDto?>;
    public class UpdateChapterPageVersionCommandHandler : IRequestHandler<UpdateChapterPageVersionCommand, ChapterPageVersionDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        public UpdateChapterPageVersionCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        public async Task<ChapterPageVersionDto?> Handle(UpdateChapterPageVersionCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.ChapterPageVersions.GetByIdAsync(request.Dto.ChapterPageVersionId, cancellationToken);
            if (entity == null) return null;

            entity.ChapterPageId = request.Dto.ChapterPageId;
            entity.VersionNo = request.Dto.VersionNo;
            entity.PageFileId = request.Dto.PageFileId;
            entity.VersionNote = request.Dto.VersionNote;
            entity.IsCurrentVersion = request.Dto.IsCurrentVersion;
            _unitOfWork.ChapterPageVersions.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ChapterPageVersionDto(entity.ChapterPageVersionId, entity.ChapterPageId, entity.VersionNo, entity.PageFileId, entity.VersionNote, entity.IsCurrentVersion);
        }
    }

    public record DeleteVersionImageCommand(Guid ChapterPageVersionId, Guid ActorUserId, string? ActorRoleName) : IRequest<DeleteVersionImageResultDto>;
    public class DeleteVersionImageCommandHandler : IRequestHandler<DeleteVersionImageCommand, DeleteVersionImageResultDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChapterPageVersionValidationPolicy _validationPolicy;
        
        public DeleteVersionImageCommandHandler(IUnitOfWork unitOfWork, IChapterPageVersionValidationPolicy validationPolicy)
        {
            _unitOfWork = unitOfWork;
            _validationPolicy = validationPolicy;
        }

        public async Task<DeleteVersionImageResultDto> Handle(DeleteVersionImageCommand request, CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                return new DeleteVersionImageResultDto(false, "A valid signed-in user is required to delete the image.", null);

            var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(request.ChapterPageVersionId, cancellationToken);
            if (version == null)
                return new DeleteVersionImageResultDto(false, "Version not found.", null);

            var validation = await _validationPolicy.CanDeleteVersionImageAsync(request.ChapterPageVersionId, cancellationToken);
            if (!validation.IsAllowed)
            {
                return new DeleteVersionImageResultDto(false, validation.ErrorMessage, null);
            }

            string? publicId = null;
            var file = await _unitOfWork.FileResources.GetByIdAsync(version.PageFileId);
            if (file != null && file.DeletedAtUtc == null)
            {
                file.DeletedAtUtc = DateTime.UtcNow;
                file.DeletedByUserId = request.ActorUserId;
                _unitOfWork.FileResources.Update(file);
                publicId = file.CloudinaryPublicId;
            }

            await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
            {
                OccurredAtUtc = DateTime.UtcNow,
                ActorUserId = request.ActorUserId,
                ActorRoleName = request.ActorRoleName,
                ActionCode = "VERSION_IMAGE_DELETED",
                EntityType = "ChapterPageVersion",
                EntityId = request.ChapterPageVersionId.ToString(),
                DetailJson = System.Text.Json.JsonSerializer.Serialize(new { version_no = version.VersionNo, file_resource_id = version.PageFileId })
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteVersionImageResultDto(true, null, publicId);
        }
    }

    public record SetCurrentVersionCommand(Guid ChapterPageId, Guid ChapterPageVersionId) : IRequest<bool>;
    public class SetCurrentVersionCommandHandler : IRequestHandler<SetCurrentVersionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        public SetCurrentVersionCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        
        public async Task<bool> Handle(SetCurrentVersionCommand request, CancellationToken cancellationToken)
        {
            var versions = await _unitOfWork.ChapterPageVersions.GetByPageIdAsync(request.ChapterPageId, cancellationToken);
            var allVersions = versions.ToList();
            var newCurrent = allVersions.FirstOrDefault(v => v.ChapterPageVersionId == request.ChapterPageVersionId);
            if (newCurrent == null) return false;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var version in allVersions.Where(v => v.IsCurrentVersion && v.ChapterPageVersionId != request.ChapterPageVersionId))
                {
                    version.IsCurrentVersion = false;
                    _unitOfWork.ChapterPageVersions.Update(version);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                newCurrent.IsCurrentVersion = true;
                _unitOfWork.ChapterPageVersions.Update(newCurrent);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
