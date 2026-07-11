using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services;

public class ChapterPageService : IChapterPageService
{
    private readonly IUnitOfWork _unitOfWork;

    public ChapterPageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChapterPageDto> CreateChapterPageAsync(CreateChapterPageDto dto)
    {
        var entity = new ChapterPage
        {
            ChapterId = dto.ChapterId,
            PageNo = dto.PageNo,
            PageNotes = dto.PageNotes
        };
        await _unitOfWork.ChapterPages.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ChapterPageDto?> GetChapterPageByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.ChapterPages.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<ChapterPageDto>> GetChapterPagesByChapterIdAsync(Guid chapterId)
    {
        var pages = await _unitOfWork.ChapterPages.FindAsync(p => p.ChapterId == chapterId && p.DeletedAtUtc == null);
        return pages
            .OrderBy(p => p.PageNo)
            .Select(MapToDto);
    }

    public async Task<Dictionary<Guid, int>> GetPageCountsByChapterIdsAsync(IEnumerable<Guid> chapterIds)
    {
        var idSet = chapterIds.ToHashSet();
        var pages = await _unitOfWork.ChapterPages.FindAsync(p => idSet.Contains(p.ChapterId) && p.DeletedAtUtc == null);
        return pages.GroupBy(p => p.ChapterId).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<ChapterPageDto?> UpdateChapterPageAsync(UpdateChapterPageDto dto)
    {
        var entity = await _unitOfWork.ChapterPages.GetByIdAsync(dto.ChapterPageId);
        if (entity == null || entity.DeletedAtUtc != null)
        {
            return null;
        }

        entity.ChapterId = dto.ChapterId;
        entity.PageNo = dto.PageNo;
        entity.PageNotes = dto.PageNotes;
        _unitOfWork.ChapterPages.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<bool> DeleteChapterPageAsync(Guid id, Guid? deletedByUserId = null)
    {
        var entity = await _unitOfWork.ChapterPages.GetByIdAsync(id);
        if (entity == null || entity.DeletedAtUtc != null)
        {
            return false;
        }

        // The schema models page deletion as a SOFT delete: every read query filters
        // `deleted_at_utc IS NULL`, and CHECK ck_chapter_page_deleted_pair requires
        // deleted_at_utc and deleted_by_user_id to be set together. Hard-deleting the
        // row fails on the ChapterPageVersion/PageRegion foreign keys (no cascade) —
        // which was the cause of "An error occurred while saving the entity changes".
        // Soft delete hides the page while preserving its versions/regions/tasks/
        // annotations for traceability.
        if (deletedByUserId is null || deletedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("A valid signed-in user is required to delete a page.");
        }

        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedByUserId = deletedByUserId;
        _unitOfWork.ChapterPages.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static ChapterPageDto MapToDto(ChapterPage p) => new(
        p.ChapterPageId,
        p.ChapterId,
        p.PageNo,
        p.PageNotes,
        p.DeletedAtUtc,
        p.DeletedByUserId
    );
}
