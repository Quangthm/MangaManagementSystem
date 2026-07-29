using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MangaManagementSystem.Application.Features.Ranking.Repositories;
using MangaManagementSystem.Application.Features.Ranking.Warnings;
using Microsoft.Extensions.Logging;
using MediatR;

namespace MangaManagementSystem.Application.Features.Ranking.Commands;

public sealed record UpdateSeriesVoteInputCommand(
    Guid ActorUserId,
    Guid SeriesVoteInputId,
    int RatingCount,
    decimal AverageRating,
    int ReadingCount,
    string? DataSourceNote) : IRequest<SeriesVoteInputDto>;

public sealed class UpdateSeriesVoteInputCommandHandler
    : IRequestHandler<UpdateSeriesVoteInputCommand, SeriesVoteInputDto>
{
    private readonly ISeriesRankingRepository _repository;
    private readonly IRankingWarningEvaluator _rankingWarningEvaluator;
    private readonly ILogger<UpdateSeriesVoteInputCommandHandler> _logger;

    public UpdateSeriesVoteInputCommandHandler(
        ISeriesRankingRepository repository,
        IRankingWarningEvaluator rankingWarningEvaluator,
        ILogger<UpdateSeriesVoteInputCommandHandler> logger)
    {
        _repository = repository;
        _rankingWarningEvaluator = rankingWarningEvaluator;
        _logger = logger;
    }

    public async Task<SeriesVoteInputDto> Handle(
        UpdateSeriesVoteInputCommand request,
        CancellationToken cancellationToken)
    {
        await ValidateActorAsync(request.ActorUserId, cancellationToken);
        ValidateVoteInput(request.RatingCount, request.AverageRating, request.ReadingCount);

        var result = await _repository.UpdateSeriesVoteInputAsync(
            request.ActorUserId,
            request.SeriesVoteInputId,
            request.RatingCount,
            request.AverageRating,
            request.ReadingCount,
            NormalizeNote(request.DataSourceNote),
            cancellationToken);

        await TryRunRankingWarningCatchUpAsync(result.SeriesId);
        return result;
    }


    private async Task TryRunRankingWarningCatchUpAsync(
        Guid seriesId)
    {
        try
        {
            await _rankingWarningEvaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.CorrectionCatchUp,
                    seriesId),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ranking warning correction catch-up failed after updating vote input. SeriesId={SeriesId}",
                seriesId);
        }
    }

    private async Task ValidateActorAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        if (!await _repository.IsVoteInputActorAsync(actorUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "Only Editorial Board Members or Editorial Board Chief users may update series vote input.");
        }
    }

    private static void ValidateVoteInput(int ratingCount, decimal averageRating, int readingCount)
    {
        if (ratingCount <= 0)
        {
            throw new InvalidOperationException("Rating count must be greater than zero.");
        }

        if (readingCount <= 0)
        {
            throw new InvalidOperationException("Reading count must be greater than zero.");
        }

        if (ratingCount > readingCount)
        {
            throw new InvalidOperationException("Rating count must not exceed reading count.");
        }

        if (averageRating < 0 || averageRating > 10)
        {
            throw new InvalidOperationException("Average rating must be between 0 and 10.");
        }
    }

    private static string? NormalizeNote(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
