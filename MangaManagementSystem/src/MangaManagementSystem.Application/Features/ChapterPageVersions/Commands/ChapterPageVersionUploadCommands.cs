using MangaManagementSystem.Application.DTOs.Manga;
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
    public record CreatePageWithVersionCommand(
        CreatePageWithVersionRequestDto Request, 
        Guid ActorUserId, 
        string? ActorRoleName) : IRequest<CreatePageWithVersionResponseDto>;

    public class CreatePageWithVersionCommandHandler : IRequestHandler<CreatePageWithVersionCommand, CreatePageWithVersionResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreatePageWithVersionCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

        public async Task<CreatePageWithVersionResponseDto> Handle(CreatePageWithVersionCommand command, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var file = new FileResource
                {
                    FilePurposeCode = command.Request.FileDto.FilePurposeCode,
                    OriginalFileName = command.Request.FileDto.OriginalFileName,
                    CloudinaryPublicId = command.Request.FileDto.CloudinaryPublicId,
                    CloudinarySecureUrl = command.Request.FileDto.CloudinarySecureUrl,
                    ContentType = command.Request.FileDto.ContentType,
                    FileSizeBytes = command.Request.FileDto.FileSizeBytes,
                    Sha256Hash = command.Request.FileDto.Sha256Hash,
                    UploadedByUserId = command.ActorUserId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var page = new ChapterPage
                {
                    ChapterId = command.Request.ChapterId,
                    PageNo = command.Request.PageNo,
                    PageNotes = command.Request.PageNotes
                };
                await _unitOfWork.ChapterPages.AddAsync(page, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var version = new ChapterPageVersion
                {
                    ChapterPageId = page.ChapterPageId,
                    VersionNo = 1,
                    PageFileId = file.FileResourceId,
                    VersionNote = command.Request.VersionNote,
                    IsCurrentVersion = true
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = command.ActorUserId,
                    ActorRoleName = command.ActorRoleName,
                    ActionCode = "PAGE_CREATED",
                    EntityType = "ChapterPage",
                    EntityId = page.ChapterPageId.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new { chapter_id = page.ChapterId, page_no = page.PageNo, chapter_page_version_id = version.ChapterPageVersionId, file_resource_id = file.FileResourceId })
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var versionDto = new ChapterPageVersionDto(version.ChapterPageVersionId, version.ChapterPageId, version.VersionNo, version.PageFileId, version.VersionNote, version.IsCurrentVersion);
                return new CreatePageWithVersionResponseDto(
                    new ChapterPageDto(page.ChapterPageId, page.ChapterId, page.PageNo, page.PageNotes, page.DeletedAtUtc, page.DeletedByUserId),
                    versionDto,
                    new FileResourceDto(file.FileResourceId, file.FilePurposeCode, file.OriginalFileName, file.CloudinarySecureUrl, file.CloudinaryPublicId, file.ContentType, file.FileSizeBytes, file.Sha256Hash, file.UploadedByUserId, file.UploadedAtUtc, file.DeletedAtUtc, file.DeletedByUserId)
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }

    public record CreateVersionWithFileCommand(
        Guid ChapterPageId, short VersionNo, CreateFileResourceDto FileDto, string? VersionNote, 
        IEnumerable<CreatePageRegionDto> RegionDtos, bool SetAsCurrent, Guid ActorUserId, string? ActorRoleName) : IRequest<ChapterPageVersionDto>;

    public class CreateVersionWithFileCommandHandler : IRequestHandler<CreateVersionWithFileCommand, ChapterPageVersionDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CreateVersionWithFileCommandHandler(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

        public async Task<ChapterPageVersionDto> Handle(CreateVersionWithFileCommand command, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var file = new FileResource
                {
                    FilePurposeCode = command.FileDto.FilePurposeCode,
                    OriginalFileName = command.FileDto.OriginalFileName,
                    CloudinaryPublicId = command.FileDto.CloudinaryPublicId,
                    CloudinarySecureUrl = command.FileDto.CloudinarySecureUrl,
                    ContentType = command.FileDto.ContentType,
                    FileSizeBytes = command.FileDto.FileSizeBytes,
                    Sha256Hash = command.FileDto.Sha256Hash,
                    UploadedByUserId = command.ActorUserId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var existing = await _unitOfWork.ChapterPageVersions.GetByPageIdAsync(command.ChapterPageId, cancellationToken);
                var nextVersionNo = (short)(existing.Select(v => (int)v.VersionNo).DefaultIfEmpty(0).Max() + 1);
                var version = new ChapterPageVersion
                {
                    ChapterPageId = command.ChapterPageId,
                    VersionNo = nextVersionNo,
                    PageFileId = file.FileResourceId,
                    VersionNote = command.VersionNote,
                    IsCurrentVersion = false
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var dto in command.RegionDtos ?? Enumerable.Empty<CreatePageRegionDto>())
                {
                    await _unitOfWork.PageRegions.AddAsync(new PageRegion
                    {
                        ChapterPageVersionId = version.ChapterPageVersionId, TypeCode = dto.TypeCode, RegionLabel = dto.RegionLabel,
                        X = dto.X, Y = dto.Y, Width = dto.Width, Height = dto.Height, ConfidenceScore = dto.ConfidenceScore,
                        SourceType = dto.SourceType, OriginalText = dto.OriginalText, CreatedAtUtc = DateTime.UtcNow
                    });
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (command.SetAsCurrent)
                {
                    foreach (var v in existing.Where(v => v.IsCurrentVersion))
                    {
                        v.IsCurrentVersion = false;
                        _unitOfWork.ChapterPageVersions.Update(v);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    version.IsCurrentVersion = true;
                    _unitOfWork.ChapterPageVersions.Update(version);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow, ActorUserId = command.ActorUserId, ActorRoleName = command.ActorRoleName,
                    ActionCode = "VERSION_CREATED", EntityType = "ChapterPageVersion", EntityId = version.ChapterPageVersionId.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new { chapter_page_id = command.ChapterPageId, version_no = version.VersionNo, file_resource_id = file.FileResourceId, set_as_current = command.SetAsCurrent })
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return new ChapterPageVersionDto(version.ChapterPageVersionId, version.ChapterPageId, version.VersionNo, version.PageFileId, version.VersionNote, version.IsCurrentVersion);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
