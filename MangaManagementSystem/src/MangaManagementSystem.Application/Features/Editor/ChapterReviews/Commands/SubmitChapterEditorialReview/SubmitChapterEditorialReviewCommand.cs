using System;
using MangaManagementSystem.Application.DTOs.Editor;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.ChapterReviews.Commands.SubmitChapterEditorialReview;

public sealed record SubmitChapterEditorialReviewCommand(
    Guid ActorUserId,
    Guid ChapterId,
    string DecisionCode,
    string? Comments,
    byte[]? MarkupFileBytes,
    string? MarkupFileName,
    string? MarkupContentType) : IRequest<SubmitChapterEditorialReviewResponse>;
