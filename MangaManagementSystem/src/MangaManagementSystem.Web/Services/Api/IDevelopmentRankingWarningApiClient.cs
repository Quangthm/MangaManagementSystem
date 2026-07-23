using MangaManagementSystem.Application.Features.Ranking.Warnings;

namespace MangaManagementSystem.Web.Services.Api;

public interface IDevelopmentRankingWarningApiClient
{
    Task<DevelopmentTimeState> GetTimeStateAsync(
        CancellationToken cancellationToken = default);

    Task<DevelopmentTimeState> SetFixedUtcAsync(
        DateTimeOffset fixedUtc,
        CancellationToken cancellationToken = default);

    Task<DevelopmentTimeState> ApplyOffsetAsync(
        double offsetMinutes,
        CancellationToken cancellationToken = default);

    Task<DevelopmentTimeState> ResetTimeAsync(
        CancellationToken cancellationToken = default);

    Task<RankingWarningEvaluationSummary> RunEvaluationAsync(
        CancellationToken cancellationToken = default);
}
