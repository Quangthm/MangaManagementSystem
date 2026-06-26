using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core repository for Mangaka chapter draft and submission workflow.
    /// Uses EF transaction for new chapter workflow transitions because no dedicated stored
    /// procedure exists yet. Future hardening may move these transitions into SPs if the team
    /// wants SP-owned workflow control.
    /// </summary>
    public sealed class MangakaChapterRepository : IMangakaChapterRepository
    {
        private const string MangakaRoleName = "Mangaka";
        private const string ActiveUserStatus = "ACTIVE";
        private const string DraftStatus = "DRAFT";
        private const string UnderReviewStatus = "UNDER_REVIEW";
        private const string RevisionRequestedStatus = "REVISION_REQUESTED";
        private const string ApprovedStatus = "APPROVED";
        private const string ScheduledStatus = "SCHEDULED";

        private readonly ApplicationDbContext _context;

        public MangakaChapterRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetMyChaptersAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            if (actorUserId == Guid.Empty)
                return Array.Empty<MangakaChapterListItemDto>();

            var chapters = await QueryAccessibleChapters(actorUserId)
                .OrderBy(c => c.Series!.Title)
                .ThenBy(c => c.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var result = new List<MangakaChapterListItemDto>(chapters.Count);
            foreach (var chapter in chapters)
            {
                result.Add(await MapToDtoAsync(chapter, cancellationToken));
            }
            return result;
        }

        public async Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            if (actorUserId == Guid.Empty || seriesId == Guid.Empty)
                return Array.Empty<MangakaChapterListItemDto>();

            var chapters = await QueryAccessibleChapters(actorUserId)
                .Where(c => c.SeriesId == seriesId)
                .OrderBy(c => c.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var result = new List<MangakaChapterListItemDto>(chapters.Count);
            foreach (var chapter in chapters)
            {
                result.Add(await MapToDtoAsync(chapter, cancellationToken));
            }
            return result;
        }

        public async Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
            Guid actorUserId,
            Guid seriesId,
            string chapterNumberLabel,
            string? chapterTitle,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await EnsureActiveMangakaContributorAsync(actorUserId, seriesId, cancellationToken);

                string normalizedLabel = NormalizeRequiredLabel(chapterNumberLabel);
                string? normalizedTitle = NormalizeOptionalTitle(chapterTitle);

                var chapter = new Chapter
                {
                    SeriesId = seriesId,
                    ChapterNumberLabel = normalizedLabel,
                    ChapterTitle = normalizedTitle,
                    StatusCode = DraftStatus,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = actorUserId,
                    UpdatedAtUtc = null
                };

                _context.Chapters.Add(chapter);
                await SaveChangesWithDuplicateLabelHandlingAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetChapterByIdForActorAsync(actorUserId, chapter.ChapterId, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
            Guid actorUserId,
            Guid chapterId,
            string chapterNumberLabel,
            string? chapterTitle,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, cancellationToken);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveMangakaContributorAsync(actorUserId, chapter.SeriesId, cancellationToken);
                EnsureCanEditChapter(chapter.StatusCode);

                string normalizedLabel = NormalizeRequiredLabel(chapterNumberLabel);
                string? normalizedTitle = NormalizeOptionalTitle(chapterTitle);

                chapter.ChapterNumberLabel = normalizedLabel;
                chapter.ChapterTitle = normalizedTitle;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await SaveChangesWithDuplicateLabelHandlingAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetChapterByIdForActorAsync(actorUserId, chapter.ChapterId, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, cancellationToken);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveMangakaContributorAsync(actorUserId, chapter.SeriesId, cancellationToken);

                if (chapter.StatusCode != DraftStatus && chapter.StatusCode != RevisionRequestedStatus)
                    throw new InvalidOperationException("Only DRAFT or REVISION_REQUESTED chapters can be submitted for editorial review.");

                chapter.StatusCode = UnderReviewStatus;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetChapterByIdForActorAsync(actorUserId, chapter.ChapterId, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<MangakaChapterListItemDto> ScheduleApprovedChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, cancellationToken);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                await EnsureActiveMangakaContributorAsync(actorUserId, chapter.SeriesId, cancellationToken);

                if (chapter.StatusCode != ApprovedStatus)
                    throw new InvalidOperationException("Only APPROVED chapters can be scheduled.");

                var normalizedDate = plannedReleaseDate.Date;
                if (normalizedDate < DateTime.UtcNow.Date)
                    throw new InvalidOperationException("Planned release date cannot be in the past.");

                chapter.PlannedReleaseDate = normalizedDate;
                chapter.StatusCode = ScheduledStatus;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetChapterByIdForActorAsync(actorUserId, chapter.ChapterId, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private IQueryable<Chapter> QueryAccessibleChapters(Guid actorUserId)
        {
            return _context.Chapters
                .AsNoTracking()
                .Include(c => c.Series)
                .Where(c => _context.SeriesContributors.Any(sc =>
                    sc.SeriesId == c.SeriesId &&
                    sc.UserId == actorUserId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == ActiveUserStatus &&
                    sc.User.Role != null &&
                    sc.User.Role.RoleName == MangakaRoleName));
        }

        private async Task<MangakaChapterListItemDto> GetChapterByIdForActorAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken)
        {
            var chapter = await QueryAccessibleChapters(actorUserId)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId, cancellationToken);

            if (chapter == null)
                throw new InvalidOperationException("Chapter was updated but could not be reloaded.");

            return await MapToDtoAsync(chapter, cancellationToken);
        }

        private async Task EnsureActiveMangakaContributorAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            bool isActiveContributor = await _context.SeriesContributors
                .Include(sc => sc.User)
                    .ThenInclude(u => u!.Role)
                .AnyAsync(sc =>
                    sc.SeriesId == seriesId &&
                    sc.UserId == actorUserId &&
                    sc.EndDate == null &&
                    sc.User != null &&
                    sc.User.StatusCode == ActiveUserStatus &&
                    sc.User.Role != null &&
                    sc.User.Role.RoleName == MangakaRoleName,
                    cancellationToken);

            if (!isActiveContributor)
                throw new InvalidOperationException("Only active Mangaka contributors of this series can manage chapter drafts.");
        }

        private static void EnsureCanEditChapter(string statusCode)
        {
            if (statusCode != DraftStatus && statusCode != RevisionRequestedStatus)
                throw new InvalidOperationException("Chapter metadata can only be edited while the chapter is DRAFT or REVISION_REQUESTED.");
        }

        private async Task SaveChangesWithDuplicateLabelHandlingAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueChapterNumberViolation(ex))
            {
                throw new InvalidOperationException("A chapter with this number already exists for this series.");
            }
        }

        private async Task<MangakaChapterListItemDto> MapToDtoAsync(Chapter chapter, CancellationToken cancellationToken)
        {
            var latestReview = await _context.ChapterEditorialReviews
                .AsNoTracking()
                .Include(r => r.ReviewerUser)
                .Include(r => r.MarkupFile)
                .Where(r => r.ChapterId == chapter.ChapterId)
                .OrderByDescending(r => r.ReviewedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            ChapterEditorialReviewSummaryDto? latestReviewDto = null;
            if (latestReview != null)
            {
                latestReviewDto = new ChapterEditorialReviewSummaryDto(
                    latestReview.ChapterEditorialReviewId,
                    latestReview.ReviewerUser?.DisplayName ?? string.Empty,
                    latestReview.DecisionCode,
                    latestReview.Feedback,
                    latestReview.MarkupFileId,
                    latestReview.MarkupFile?.CloudinarySecureUrl,
                    latestReview.ReviewedAtUtc);
            }

            return new MangakaChapterListItemDto(
                chapter.ChapterId,
                chapter.SeriesId,
                chapter.Series?.Title ?? string.Empty,
                chapter.ChapterNumberLabel,
                chapter.ChapterTitle,
                chapter.StatusCode,
                chapter.PlannedReleaseDate,
                chapter.ReleasedAtUtc,
                chapter.CreatedAtUtc,
                chapter.UpdatedAtUtc,
                latestReviewDto);
        }

        private static string NormalizeRequiredLabel(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Chapter number label is required.");
            return normalized;
        }

        private static string? NormalizeOptionalTitle(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        private static bool IsUniqueChapterNumberViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number is 2601 or 2627;
            }

            return false;
        }
    }
}
