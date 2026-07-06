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
    public partial class EditorChapterReviewRepository
    {
        private const string StatusScheduledFull = "SCHEDULED";

        public async Task<ChapterEditorialReviewResult> SubmitChapterEditorialReviewWithSchedulingAsync(
            Guid actorUserId,
            Guid chapterId,
            string decisionCode,
            string? comments,
            UploadedFileMetadata? markup,
            CancellationToken ct = default)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (chapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            var allowedDecisions = new[] { "APPROVED", "REVISION_REQUESTED", "CANCELLED" };
            if (!allowedDecisions.Contains(decisionCode))
                throw new InvalidOperationException(
                    "Decision code must be one of: APPROVED, REVISION_REQUESTED, CANCELLED.");

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
                        "You are not authorized to review this chapter.");

                string previousStatusCode = chapter.StatusCode;

                if (previousStatusCode != StatusUnderReview)
                    throw new InvalidOperationException(
                        "This chapter is no longer under review and cannot receive a review decision.");

                var reviewedAtUtc = DateTime.UtcNow;
                Guid? markupFileId = null;

                if (markup is not null)
                {
                    var fileResource = new FileResource
                    {
                        FilePurposeCode = "EDITORIAL_ATTACHMENT",
                        OriginalFileName = markup.OriginalFileName,
                        CloudinaryPublicId = markup.PublicId,
                        CloudinarySecureUrl = markup.SecureUrl,
                        ContentType = markup.ContentType,
                        FileSizeBytes = markup.FileSizeBytes,
                        Sha256Hash = markup.Sha256Hash,
                        UploadedByUserId = actorUserId,
                        UploadedAtUtc = reviewedAtUtc
                    };

                    _dbContext.FileResources.Add(fileResource);
                    await _dbContext.SaveChangesAsync(ct);

                    markupFileId = fileResource.FileResourceId;
                }

                var review = new ChapterEditorialReview
                {
                    ChapterId = chapterId,
                    ReviewerUserId = actorUserId,
                    DecisionCode = decisionCode,
                    Feedback = comments,
                    MarkupFileId = markupFileId,
                    ReviewedAtUtc = reviewedAtUtc
                };

                _dbContext.ChapterEditorialReviews.Add(review);

                string newStatusCode;

                if (decisionCode == "APPROVED" && chapter.PlannedReleaseDate.HasValue)
                {
                    newStatusCode = StatusScheduledFull;
                }
                else
                {
                    newStatusCode = decisionCode switch
                    {
                        "APPROVED" => StatusApproved,
                        "REVISION_REQUESTED" => StatusRevisionRequested,
                        "CANCELLED" => StatusCancelled,
                        _ => throw new InvalidOperationException($"Unknown decision code: {decisionCode}")
                    };
                }

                int updated = await _dbContext.Chapters
                    .Where(c => c.ChapterId == chapterId && c.StatusCode == StatusUnderReview)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.StatusCode, newStatusCode)
                        .SetProperty(c => c.UpdatedAtUtc, DateTime.UtcNow), ct);

                if (updated == 0)
                    throw new InvalidOperationException(
                        "This chapter is no longer under review and cannot receive a review decision.");

                var actorRoleName = await _dbContext.ActiveSeriesContributors
                    .Where(sc => sc.SeriesId == chapter.SeriesId && sc.UserId == actorUserId)
                    .Select(sc => sc.RoleName)
                    .FirstOrDefaultAsync(ct);

                string actionCode;
                if (decisionCode == "APPROVED" && newStatusCode == StatusScheduledFull)
                {
                    actionCode = "CHAPTER_SCHEDULED";
                }
                else
                {
                    actionCode = "CHAPTER_EDITORIAL_REVIEW_RECORDED";
                }

                var detailJson = JsonSerializer.Serialize(new
                {
                    chapter_id = chapterId,
                    series_id = chapter.SeriesId,
                    chapter_editorial_review_id = review.ChapterEditorialReviewId,
                    old_status_code = previousStatusCode,
                    new_status_code = newStatusCode,
                    decision_code = decisionCode,
                    planned_release_date = chapter.PlannedReleaseDate,
                    has_markup_file = markupFileId.HasValue,
                    markup_file_id = markupFileId,
                    markup_file_name = markup?.OriginalFileName,
                    markup_content_type = markup?.ContentType,
                    markup_file_size = markup?.FileSizeBytes
                });

                var auditEvent = new AuditEvent
                {
                    OccurredAtUtc = reviewedAtUtc,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = actionCode,
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString("D"),
                    DetailJson = detailJson
                };

                _dbContext.AuditEvents.Add(auditEvent);

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ChapterEditorialReviewResult(
                    chapter.ChapterId,
                    newStatusCode,
                    review.ChapterEditorialReviewId,
                    review.DecisionCode,
                    review.Feedback,
                    review.ReviewedAtUtc);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<ChapterRescheduleResult> ReschedulePlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime newPlannedReleaseDate,
            string reason,
            CancellationToken ct = default)
        {
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
                        "You are not authorized to reschedule this chapter.");

                if (chapter.StatusCode != StatusScheduledFull)
                    throw new InvalidOperationException(
                        "Only SCHEDULED chapters can be rescheduled.");

                var normalizedDate = newPlannedReleaseDate.Date;
                if (normalizedDate < DateTime.UtcNow.Date)
                    throw new InvalidOperationException("Planned release date cannot be in the past.");

                var previousPlannedDate = chapter.PlannedReleaseDate;
                chapter.PlannedReleaseDate = normalizedDate;
                chapter.UpdatedAtUtc = DateTime.UtcNow;

                var detailJson = JsonSerializer.Serialize(new
                {
                    chapter_id = chapterId,
                    series_id = chapter.SeriesId,
                    old_planned_release_date = previousPlannedDate,
                    new_planned_release_date = normalizedDate,
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
                    ActionCode = "CHAPTER_RESCHEDULED",
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString("D"),
                    DetailJson = detailJson
                });

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ChapterRescheduleResult(
                    chapter.ChapterId,
                    chapter.StatusCode,
                    normalizedDate,
                    "Chapter rescheduled successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<ChapterPlannedDateResult> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken ct = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var chapter = await _dbContext.Chapters
                    .Include(c => c.Series)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

                if (chapter == null)
                    throw new InvalidOperationException("Chapter does not exist.");

                if (!IsPlannableChapter(chapter.StatusCode))
                    throw new InvalidOperationException(
                        "This chapter's status does not allow setting a planned release date. " +
                        "Only DRAFT, REVISION_REQUESTED, or APPROVED chapters can be scheduled.");

                bool isAuthorized = await _dbContext.ActiveSeriesContributors
                    .AnyAsync(sc =>
                        sc.SeriesId == chapter.SeriesId &&
                        sc.UserId == actorUserId &&
                        sc.RoleName == TantouEditorRole,
                        ct);

                if (!isAuthorized)
                    throw new InvalidOperationException(
                        "You are not authorized to set the planned release date for this chapter.");

                var normalizedDate = plannedReleaseDate.Date;
                if (normalizedDate < DateTime.UtcNow.Date)
                    throw new InvalidOperationException("Planned release date cannot be in the past.");

                string previousStatusCode = chapter.StatusCode;
                DateTime? previousPlanned = chapter.PlannedReleaseDate;

                chapter.PlannedReleaseDate = normalizedDate;

                string newStatusCode;
                if (previousStatusCode == StatusApproved)
                {
                    newStatusCode = StatusScheduledFull;
                    chapter.StatusCode = StatusScheduledFull;
                }
                else
                {
                    newStatusCode = previousStatusCode;
                }

                chapter.UpdatedAtUtc = DateTime.UtcNow;

                var frequencyCode = chapter.Series?.PublicationFrequencyCode;

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

                var actorRoleName = await _dbContext.ActiveSeriesContributors
                    .Where(sc => sc.SeriesId == chapter.SeriesId && sc.UserId == actorUserId)
                    .Select(sc => sc.RoleName)
                    .FirstOrDefaultAsync(ct);

                _dbContext.AuditEvents.Add(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = "CHAPTER_PLANNED_RELEASE_DATE_SET",
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString("D"),
                    DetailJson = detailJson
                });

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ChapterPlannedDateResult(
                    chapter.ChapterId,
                    newStatusCode,
                    normalizedDate,
                    newStatusCode == StatusScheduledFull
                        ? "Chapter scheduled successfully."
                        : "Planned release date set successfully.",
                    null,
                    null,
                    frequencyCode);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private static bool IsPlannableChapter(string statusCode)
        {
            return statusCode == StatusDraft
                || statusCode == StatusRevisionRequested
                || statusCode == StatusApproved;
        }

        private async Task<DateTime?> GetPreviousPlannedNonCancelledChapterDateAsync(
            Guid seriesId,
            Guid excludeChapterId,
            CancellationToken ct)
        {
            var previousChapter = await _dbContext.Chapters
                .AsNoTracking()
                .Where(c => c.SeriesId == seriesId
                         && c.ChapterId != excludeChapterId
                         && c.StatusCode != StatusCancelled
                         && c.PlannedReleaseDate != null)
                .OrderByDescending(c => c.PlannedReleaseDate)
                .FirstOrDefaultAsync(ct);

            return previousChapter?.PlannedReleaseDate;
        }
    }
}
