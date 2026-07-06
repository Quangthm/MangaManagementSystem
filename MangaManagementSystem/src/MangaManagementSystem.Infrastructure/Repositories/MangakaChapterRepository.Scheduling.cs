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
            ChapterSchedulingValidator schedulingValidator,
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

                if (!IsPlannableChapter(chapter.StatusCode))
                    throw new InvalidOperationException(
                        "This chapter's status does not allow setting a planned release date. " +
                        "Only DRAFT, REVISION_REQUESTED, or APPROVED chapters can be scheduled.");

                await EnsureActiveMangakaContributorAsync(actorUserId, chapter.SeriesId, cancellationToken);

                var normalizedDate = plannedReleaseDate.Date;
                if (normalizedDate < DateTime.UtcNow.Date)
                    throw new InvalidOperationException("Planned release date cannot be in the past.");

                var frequencyCode = chapter.Series?.PublicationFrequencyCode;

                DateTime? previousPlannedDate = null;
                if (!string.IsNullOrWhiteSpace(frequencyCode) &&
                    frequencyCode.ToUpperInvariant() != "IRREGULAR")
                {
                    previousPlannedDate = await GetPreviousPlannedNonCancelledChapterDateAsync(
                        chapter.SeriesId, chapterId, cancellationToken);
                }

                var validation = await schedulingValidator.ValidateAsync(
                    frequencyCode,
                    chapter.SeriesId,
                    chapterId,
                    normalizedDate,
                    previousPlannedDate,
                    cancellationToken);

                if (!validation.IsValid)
                {
                    return new SetChapterPlannedReleaseDateResponse(
                        chapter.ChapterId,
                        chapter.StatusCode,
                        default,
                        validation.ErrorMessage,
                        validation.AllowedPeriodStart,
                        validation.AllowedPeriodEnd,
                        validation.FrequencyCode);
                }

                string previousStatusCode = chapter.StatusCode;
                DateTime? previousPlanned = chapter.PlannedReleaseDate;

                chapter.PlannedReleaseDate = normalizedDate;

                string newStatusCode;
                if (previousStatusCode == ApprovedStatus)
                {
                    newStatusCode = ScheduledStatus;
                    chapter.StatusCode = ScheduledStatus;
                }
                else
                {
                    newStatusCode = previousStatusCode;
                }

                chapter.UpdatedAtUtc = DateTime.UtcNow;

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
                    ActionCode = previousPlanned.HasValue
                        ? "CHAPTER_PLANNED_RELEASE_DATE_SET"
                        : "CHAPTER_PLANNED_RELEASE_DATE_SET",
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString("D"),
                    DetailJson = detailJson
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new SetChapterPlannedReleaseDateResponse(
                    chapter.ChapterId,
                    newStatusCode,
                    normalizedDate,
                    newStatusCode == ScheduledStatus ? "Chapter scheduled successfully." : "Planned release date set successfully.",
                    validation.AllowedPeriodStart,
                    validation.AllowedPeriodEnd,
                    validation.FrequencyCode);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static bool IsPlannableChapter(string statusCode)
        {
            return statusCode == DraftStatus
                || statusCode == RevisionRequestedStatus
                || statusCode == ApprovedStatus;
        }

        private async Task<DateTime?> GetPreviousPlannedNonCancelledChapterDateAsync(
            Guid seriesId,
            Guid excludeChapterId,
            CancellationToken ct)
        {
            var previousChapter = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.SeriesId == seriesId
                         && c.ChapterId != excludeChapterId
                         && c.StatusCode != CancelledStatus
                         && c.PlannedReleaseDate != null)
                .OrderByDescending(c => c.PlannedReleaseDate)
                .FirstOrDefaultAsync(ct);

            return previousChapter?.PlannedReleaseDate;
        }
    }
}
