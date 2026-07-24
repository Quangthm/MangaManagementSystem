using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.Features.Publication.Schedule;
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
                    EntityId = chapterId.ToString(),
                    DetailJson = detailJson
                };

                _dbContext.AuditEvents.Add(auditEvent);

                var publicationScheduleEvent =
                    PublicationScheduleNotificationSupport.Classify(
                        previousStatusCode,
                        newStatusCode,
                        chapter.PlannedReleaseDate,
                        chapter.PlannedReleaseDate);

                await PublicationScheduleNotificationPersistence.AddNotificationsAsync(
                    _dbContext,
                    actorUserId,
                    chapter,
                    publicationScheduleEvent,
                    reviewedAtUtc,
                    ct);

                var recipientUserIds =
                    await _dbContext.ActiveSeriesContributors
                        .AsNoTracking()
                        .Where(contributor =>
                            contributor.SeriesId == chapter.SeriesId
                            && contributor.RoleName == "Mangaka")
                        .Select(contributor => contributor.UserId)
                        .Distinct()
                        .ToListAsync(ct);

                var notificationContent =
                    BuildChapterDecisionNotificationContent(
                        decisionCode,
                        newStatusCode);

                foreach (var recipientUserId in recipientUserIds)
                {
                    _dbContext.Notifications.Add(
                        new Notification
                        {
                            RecipientUserId = recipientUserId,
                            NotificationTypeCode =
                                NotificationTypeCodes.ChapterDecision,
                            Title = notificationContent.Title,
                            Message = notificationContent.Message,
                            RelatedEntityType =
                                NotificationRelatedEntityTypes.Chapter,
                            RelatedEntityId = chapterId,
                            CreatedAtUtc = reviewedAtUtc
                        });
                }

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

                if (!IsPlannableOrReschedulableChapter(chapter.StatusCode))
                    throw new InvalidOperationException(
                        "This chapter's status does not allow setting a planned release date. " +
                        "Chapters must be DRAFT, UNDER_REVIEW, REVISION_REQUESTED, APPROVED, SCHEDULED, or ON_HOLD.");

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
                if (previousStatusCode == StatusApproved || previousStatusCode == StatusOnHold)
                {
                    newStatusCode = StatusScheduledFull;
                    chapter.StatusCode = StatusScheduledFull;
                }
                else
                {
                    newStatusCode = previousStatusCode;
                }

                var changedAtUtc = DateTime.UtcNow;
                chapter.UpdatedAtUtc = changedAtUtc;

                var publicationScheduleEvent =
                    PublicationScheduleNotificationSupport.Classify(
                        previousStatusCode,
                        newStatusCode,
                        previousPlanned,
                        normalizedDate);

                var frequencyCode = chapter.Series?.PublicationFrequencyCode;

                var actionCode = previousPlanned.HasValue &&
                    (previousStatusCode == StatusScheduledFull || previousStatusCode == StatusOnHold)
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

                var actorRoleName = await _dbContext.ActiveSeriesContributors
                    .Where(sc => sc.SeriesId == chapter.SeriesId && sc.UserId == actorUserId)
                    .Select(sc => sc.RoleName)
                    .FirstOrDefaultAsync(ct);

                _dbContext.AuditEvents.Add(new AuditEvent
                {
                    OccurredAtUtc = changedAtUtc,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = actionCode,
                    EntityType = "Chapter",
                    EntityId = chapterId.ToString(),
                    DetailJson = detailJson
                });

                await PublicationScheduleNotificationPersistence.AddNotificationsAsync(
                    _dbContext,
                    actorUserId,
                    chapter,
                    publicationScheduleEvent,
                    changedAtUtc,
                    ct);

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

        private static (string Title, string Message)
            BuildChapterDecisionNotificationContent(
                string decisionCode,
                string newStatusCode)
        {
            return decisionCode switch
            {
                "APPROVED" when newStatusCode == StatusScheduledFull =>
                    (
                        "Chapter Approved and Scheduled",
                        "Your chapter was approved and scheduled for release. Open chapter management to review the release plan."
                    ),
                "APPROVED" =>
                    (
                        "Chapter Approved",
                        "Your chapter was approved. Open chapter management to review the decision."
                    ),
                "REVISION_REQUESTED" =>
                    (
                        "Chapter Revision Requested",
                        "Your chapter requires revision. Open chapter management to review the editor feedback."
                    ),
                "CANCELLED" =>
                    (
                        "Chapter Cancelled",
                        "Your chapter was cancelled during editorial review. Open chapter management to review the editor feedback."
                    ),
                _ =>
                    throw new InvalidOperationException(
                        $"Unknown decision code: {decisionCode}")
            };
        }

        private static bool IsPlannableOrReschedulableChapter(string statusCode)
        {
            return statusCode == StatusDraft
                || statusCode == StatusUnderReview
                || statusCode == StatusRevisionRequested
                || statusCode == StatusApproved
                || statusCode == StatusScheduledFull
                || statusCode == StatusOnHold;
        }
    }
}
