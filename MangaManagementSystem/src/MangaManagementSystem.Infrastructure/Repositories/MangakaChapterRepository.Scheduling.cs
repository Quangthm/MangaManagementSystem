using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Services;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public partial class MangakaChapterRepository
    {
        public async Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Series)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, cancellationToken);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                if (!IsPlannableOrReschedulableChapter(chapter.StatusCode))
                    throw new InvalidOperationException(
                        "This chapter's status does not allow setting a planned release date. " +
                        "Chapters must be DRAFT, REVISION_REQUESTED, APPROVED, SCHEDULED, or ON_HOLD.");

                await EnsureActiveMangakaContributorAsync(actorUserId, chapter.SeriesId, cancellationToken);

                var normalizedDate = plannedReleaseDate.Date;
                if (normalizedDate < DateTime.UtcNow.Date)
                    throw new InvalidOperationException("Planned release date cannot be in the past.");

                var frequencyCode = chapter.Series?.PublicationFrequencyCode;

                string previousStatusCode = chapter.StatusCode;
                DateTime? previousPlanned = chapter.PlannedReleaseDate;

                chapter.PlannedReleaseDate = normalizedDate;

                string newStatusCode;
                if (previousStatusCode == ApprovedStatus || previousStatusCode == "ON_HOLD")
                {
                    newStatusCode = ScheduledStatus;
                    chapter.StatusCode = ScheduledStatus;
                }
                else
                {
                    newStatusCode = previousStatusCode;
                }

                chapter.UpdatedAtUtc = DateTime.UtcNow;

                var actionCode = previousPlanned.HasValue && previousStatusCode == ScheduledStatus
                    ? "CHAPTER_RESCHEDULED"
                    : "CHAPTER_PLANNED_RELEASE_DATE_SET";

                var detailJson = JsonSerializer.Serialize(new
                {
                    chapter_id = chapterId,
                    series_id = chapter.SeriesId,
                    old_status_code = previousStatusCode,
                    new_status_code = newStatusCode,
                    old_planned_release_date = previousPlanned,
                    new_planned_release_date = normalizedDate,
                    actor_user_id = actorUserId,
                    frequency_code = frequencyCode
                });

                _context.AuditEvents.Add(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = MangakaRoleName,
                    ActionCode = actionCode,
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString(),
                    DetailJson = detailJson
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var statusMessage = newStatusCode == ScheduledStatus
                    ? "Chapter scheduled successfully."
                    : "Planned release date set successfully.";

                return new SetChapterPlannedReleaseDateResponse(
                    chapter.ChapterId,
                    newStatusCode,
                    normalizedDate,
                    statusMessage,
                    null,
                    null,
                    frequencyCode);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static bool IsPlannableOrReschedulableChapter(string statusCode)
        {
            return statusCode == DraftStatus
                || statusCode == RevisionRequestedStatus
                || statusCode == ApprovedStatus
                || statusCode == ScheduledStatus
                || statusCode == "ON_HOLD";
        }
    }
}
