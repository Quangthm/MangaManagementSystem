using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals;
using MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SubmitChapterEditorialReview
{
    public sealed class SubmitChapterEditorialReviewCommandHandler
        : IRequestHandler<SubmitChapterEditorialReviewCommand, SubmitChapterEditorialReviewResponse>
    {
        private static readonly string[] AllowedDecisions = { "APPROVED", "REVISION_REQUESTED", "CANCELLED" };
        private const int MaxCommentsLength = 2000;

        private readonly IEditorChapterReviewRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<SubmitChapterEditorialReviewCommandHandler> _logger;

        public SubmitChapterEditorialReviewCommandHandler(
            IEditorChapterReviewRepository repository,
            IFileStorageService fileStorageService,
            ILogger<SubmitChapterEditorialReviewCommandHandler> logger)
        {
            _repository = repository;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<SubmitChapterEditorialReviewResponse> Handle(
            SubmitChapterEditorialReviewCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("A valid signed-in user is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("A valid chapter is required.");

            if (string.IsNullOrWhiteSpace(request.DecisionCode))
                throw new InvalidOperationException("A decision code is required.");

            if (!AllowedDecisions.Contains(request.DecisionCode))
                throw new InvalidOperationException(
                    "Decision code must be one of: APPROVED, REVISION_REQUESTED, CANCELLED.");

            string? comments = NormalizeComments(request.Comments);

            if (request.DecisionCode == "REVISION_REQUESTED" || request.DecisionCode == "CANCELLED")
            {
                if (string.IsNullOrWhiteSpace(comments))
                    throw new InvalidOperationException(
                        $"Comments are required when the decision is {request.DecisionCode}.");
            }

            // Optional markup upload (Cloudinary, outside the SQL transaction).
            FileUploadResultDto? markup = null;
            bool hasMarkup = request.MarkupFileBytes is { Length: > 0 };

            if (hasMarkup)
            {
                markup = await EditorialMarkupUploader.ValidateAndUploadAsync(
                    _fileStorageService,
                    request.MarkupFileBytes!,
                    request.MarkupFileName,
                    request.MarkupContentType);
            }

            try
            {
                var uploadMeta = markup is not null
                    ? new MangaManagementSystem.Domain.Interfaces.UploadedFileMetadata(
                        markup.OriginalFileName,
                        markup.PublicId,
                        markup.SecureUrl,
                        markup.ContentType,
                        markup.FileSizeBytes,
                        markup.Sha256Hash)
                    : null;

                var result = await _repository.SubmitChapterEditorialReviewWithSchedulingAsync(
                    request.ActorUserId,
                    request.ChapterId,
                    request.DecisionCode,
                    comments,
                    uploadMeta,
                    cancellationToken);

                return new SubmitChapterEditorialReviewResponse(
                    result.ChapterId,
                    result.StatusCode,
                    result.ReviewId,
                    result.DecisionCode,
                    result.Comments,
                    result.ReviewedAtUtc);
            }
            catch
            {
                if (markup is not null)
                {
                    await EditorialMarkupUploader.TryCleanupAsync(
                        _fileStorageService, _logger, markup,
                        $"Workflow failed after markup upload for chapter {request.ChapterId} (decision: {request.DecisionCode}).");
                }
                throw;
            }
        }

        private static string? NormalizeComments(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string trimmed = value.Trim();
            if (trimmed.Length > MaxCommentsLength)
                throw new InvalidOperationException(
                    $"Comments must not exceed {MaxCommentsLength} characters.");

            return trimmed;
        }
    }
}
