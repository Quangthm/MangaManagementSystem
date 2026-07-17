using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ChapterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
        {
            var entity = new Chapter
            {
                SeriesId = dto.SeriesId,
                ChapterNumberLabel = dto.ChapterNumberLabel,
                ChapterTitle = dto.ChapterTitle,
                StatusCode = dto.StatusCode,
                PlannedReleaseDate = dto.PlannedReleaseDate,
                CreatedByUserId = dto.CreatedByUserId,   // BR-CH-011: record the creator
                CreatedAtUtc = System.DateTime.UtcNow
            };
            await _unitOfWork.Chapters.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterDto?> GetChapterByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterDto>> GetChaptersBySeriesIdAsync(Guid seriesId)
        {
            var chapters = await _unitOfWork.Chapters.FindAsync(c => c.SeriesId == seriesId);
            return chapters.Select(MapToDto);
        }

        public async Task DeleteChapterAsync(Guid id, Guid? actorUserId = null, string? actorRoleName = null)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity == null)
            {
                return;
            }

            // Business rule BR-CH-CANCEL-003 / BR-CH-002: a chapter that already has content
            // (pages, versions, regions, annotations, tasks, or editorial review history) must
            // never be hard-deleted. Such a chapter is preserved and cancelled through
            // status_code = CANCELLED. Only a truly empty draft (no pages yet) may be removed.
            var pages = await _unitOfWork.ChapterPages.FindAsync(p => p.ChapterId == id);
            if (pages.Count > 0)
            {
                throw new InvalidOperationException(
                    "This chapter already has pages and cannot be deleted. Cancel it instead to preserve its history.");
            }

            _unitOfWork.Chapters.Delete(entity);

            if (actorUserId.HasValue && actorUserId.Value != Guid.Empty)
            {
                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId.Value,
                    ActorRoleName = actorRoleName,
                    ActionCode = "CHAPTER_DELETED",
                    EntityType = "Chapter",
                    EntityId = id.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        chapter_number_label = entity.ChapterNumberLabel,
                        status_code = entity.StatusCode
                    })
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CancelChapterAsync(Guid id, Guid? actorUserId = null, string? actorRoleName = null)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity == null)
            {
                return;
            }

            // BR-CH-CANCEL-003: cancelling preserves the chapter and all its content; it only
            // marks status_code = CANCELLED (a cancelled chapter does not reserve its number).
            entity.StatusCode = "CANCELLED";
            entity.UpdatedAtUtc = System.DateTime.UtcNow;
            _unitOfWork.Chapters.Update(entity);

            if (actorUserId.HasValue && actorUserId.Value != Guid.Empty)
            {
                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = System.DateTime.UtcNow,
                    ActorUserId = actorUserId.Value,
                    ActorRoleName = actorRoleName,
                    ActionCode = "CHAPTER_CANCELLED",
                    EntityType = "Chapter",
                    EntityId = id.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        chapter_number_label = entity.ChapterNumberLabel
                    })
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateChapterStatusAsync(Guid id, string statusCode)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity != null)
            {
                entity.StatusCode = statusCode;
                _unitOfWork.Chapters.Update(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateChapterTitleAsync(Guid id, string newTitle)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (entity != null)
            {
                entity.ChapterTitle = newTitle;
                _unitOfWork.Chapters.Update(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private static ChapterDto MapToDto(Chapter c) => new(
            c.ChapterId,
            c.SeriesId,
            c.ChapterNumberLabel,
            c.ChapterTitle,
            c.StatusCode,
            c.PlannedReleaseDate,
            c.ReleasedAtUtc,
            c.CreatedAtUtc,
            c.CreatedByUserId
        );

        public async Task EnsureChapterAllowsContentMutationsAsync(Guid chapterId)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(chapterId);
            if (entity == null)
                throw new InvalidOperationException("Chapter does not exist.");

            var blockedStatuses = new[]
            {
                "UNDER_REVIEW", "APPROVED", "SCHEDULED",
                "ON_HOLD", "RELEASED", "CANCELLED"
            };

            if (blockedStatuses.Contains(entity.StatusCode))
                throw new InvalidOperationException(
                    "This chapter is locked for content changes. " +
                    "Content editing is blocked while the chapter is UNDER_REVIEW, APPROVED, SCHEDULED, " +
                    "ON_HOLD, RELEASED, or CANCELLED.");
        }

        public async Task EnsureChapterAllowsAssistantTaskSubmissionAsync(Guid chapterId)
        {
            var entity = await _unitOfWork.Chapters.GetByIdAsync(chapterId);
            if (entity == null)
                throw new InvalidOperationException("Chapter does not exist.");

            var blockedStatuses = new[]
            {
                "APPROVED", "SCHEDULED", "ON_HOLD",
                "RELEASED", "CANCELLED"
            };

            if (blockedStatuses.Contains(entity.StatusCode))
                throw new InvalidOperationException(
                    $"This task cannot be submitted because the chapter is {entity.StatusCode}. " +
                    "Assistant submissions are allowed only while the chapter is DRAFT, REVISION_REQUESTED, or UNDER_REVIEW.");
        }
    }
}
