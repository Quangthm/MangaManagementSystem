using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Domain.Policies;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterReleaseRepository : IChapterReleaseRepository
    {
        private const string TantouEditorRole = "Tantou Editor";
        private const string StatusScheduled = "SCHEDULED";
        private const string StatusApproved = "APPROVED";
        private const string StatusReleased = "RELEASED";

        private readonly ApplicationDbContext _dbContext;

        public ChapterReleaseRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ChapterReleaseResult> ReleaseChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            bool confirmRelease,
            CancellationToken ct = default)
        {
            if (!confirmRelease)
                throw new InvalidOperationException("Release confirmation is required.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .Include(c => c.Series)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                bool isAuthorized = await _dbContext.ActiveSeriesContributors
                    .AnyAsync(sc =>
                        sc.SeriesId == chapter.SeriesId &&
                        sc.UserId == actorUserId &&
                        sc.RoleName == TantouEditorRole,
                        ct);

                if (!isAuthorized)
                    throw new InvalidOperationException(
                        "You are not authorized to release this chapter.");

                var seriesStatusCode = chapter.Series?.StatusCode;
                if (!SeriesReleasePolicy.AllowsChapterRelease(seriesStatusCode))
                    throw new InvalidOperationException(
                        GetSeriesReleaseBlockedMessage(seriesStatusCode));

                if (chapter.StatusCode == StatusReleased)
                    throw new InvalidOperationException("This chapter has already been released.");

                if (chapter.StatusCode == "CANCELLED")
                    throw new InvalidOperationException("A cancelled chapter cannot be released.");

                if (chapter.StatusCode != StatusScheduled && chapter.StatusCode != StatusApproved)
                    throw new InvalidOperationException(
                        "Only SCHEDULED or APPROVED chapters can be released.");

                string previousStatusCode = chapter.StatusCode;
                var previousPlannedDate = chapter.PlannedReleaseDate;
                var now = DateTime.UtcNow;

                if (!chapter.PlannedReleaseDate.HasValue)
                {
                    chapter.PlannedReleaseDate = now.Date;
                }

                chapter.StatusCode = StatusReleased;
                chapter.ReleasedAtUtc = now;
                chapter.UpdatedAtUtc = now;

                var detailJson = JsonSerializer.Serialize(new
                {
                    chapter_id = chapterId,
                    series_id = chapter.SeriesId,
                    old_status_code = previousStatusCode,
                    new_status_code = StatusReleased,
                    old_planned_release_date = previousPlannedDate,
                    planned_release_date = chapter.PlannedReleaseDate,
                    released_at_utc = now,
                    actor_user_id = actorUserId,
                    confirm_release = true
                });

                var actorRoleName = await _dbContext.ActiveSeriesContributors
                    .Where(sc => sc.SeriesId == chapter.SeriesId && sc.UserId == actorUserId)
                    .Select(sc => sc.RoleName)
                    .FirstOrDefaultAsync(ct);

                _dbContext.AuditEvents.Add(new AuditEvent
                {
                    OccurredAtUtc = now,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = "CHAPTER_RELEASED",
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString(),
                    DetailJson = detailJson
                });

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ChapterReleaseResult(
                    chapter.ChapterId,
                    chapter.StatusCode,
                    now,
                    chapter.PlannedReleaseDate,
                    "Chapter has been released successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private static string GetSeriesReleaseBlockedMessage(string? seriesStatusCode)
        {
            if (seriesStatusCode is null)
                return "The chapter's parent series could not be found.";

            if (string.Equals(seriesStatusCode, "HIATUS", StringComparison.OrdinalIgnoreCase))
                return "Chapter release is paused while the series is on hiatus.";

            if (string.Equals(seriesStatusCode, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                return "A chapter under a completed series cannot be released.";

            if (string.Equals(seriesStatusCode, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                return "A chapter under a cancelled series cannot be released.";

            return "Chapters can only be released while the series is serialized.";
        }
    }
}
