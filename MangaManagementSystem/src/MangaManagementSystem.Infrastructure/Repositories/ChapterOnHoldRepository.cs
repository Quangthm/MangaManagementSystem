using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class ChapterOnHoldRepository : IChapterOnHoldRepository
    {
        private const string TantouEditorRole = "Tantou Editor";
        private const string StatusScheduled = "SCHEDULED";
        private const string StatusOnHold = "ON_HOLD";

        private readonly ApplicationDbContext _dbContext;

        public ChapterOnHoldRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ChapterOnHoldResult> PutScheduledChapterOnHoldAsync(
            Guid actorUserId,
            Guid chapterId,
            string reason,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A reason is required to put a scheduled chapter on hold.");

            if (reason.Length > 1000)
                throw new InvalidOperationException("Reason must not exceed 1000 characters.");

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
                        "You are not authorized to put this chapter on hold.");

                if (chapter.StatusCode != StatusScheduled)
                    throw new InvalidOperationException(
                        "Only SCHEDULED chapters can be put on hold.");

                string previousStatusCode = chapter.StatusCode;
                var previousPlannedDate = chapter.PlannedReleaseDate;

                chapter.StatusCode = StatusOnHold;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                var detailJson = JsonSerializer.Serialize(new
                {
                    chapter_id = chapterId,
                    series_id = chapter.SeriesId,
                    old_status_code = previousStatusCode,
                    new_status_code = StatusOnHold,
                    old_planned_release_date = previousPlannedDate,
                    actor_user_id = actorUserId,
                    reason = reason
                });

                var actorRoleName = await _dbContext.ActiveSeriesContributors
                    .Where(sc => sc.SeriesId == chapter.SeriesId && sc.UserId == actorUserId)
                    .Select(sc => sc.RoleName)
                    .FirstOrDefaultAsync(ct);

                _dbContext.AuditEvents.Add(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = "CHAPTER_PUT_ON_HOLD",
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString(),
                    DetailJson = detailJson
                });

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ChapterOnHoldResult(
                    chapter.ChapterId,
                    chapter.StatusCode,
                    "Chapter has been put on hold.");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
