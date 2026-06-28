using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SubmitChapterEditorialReview
{
    public sealed class SubmitChapterEditorialReviewCommandHandler
        : IRequestHandler<SubmitChapterEditorialReviewCommand, SubmitChapterEditorialReviewResponse>
    {
        private static readonly string[] AllowedDecisions = { "APPROVED", "REVISION_REQUESTED", "CANCELLED" };
        private const int MaxCommentsLength = 2000;

        private readonly IEditorChapterReviewRepository _repository;

        public SubmitChapterEditorialReviewCommandHandler(IEditorChapterReviewRepository repository)
        {
            _repository = repository;
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

            var result = await _repository.SubmitChapterEditorialReviewAsync(
                request.ActorUserId,
                request.ChapterId,
                request.DecisionCode,
                comments,
                request.MarkupFileId,
                cancellationToken);

            return new SubmitChapterEditorialReviewResponse(
                result.ChapterId,
                result.StatusCode,
                result.ReviewId,
                result.DecisionCode,
                result.Comments,
                result.ReviewedAtUtc);
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
