using MangaManagementSystem.Application.Features.Ranking.Warnings;

namespace MangaManagementSystem.API.Endpoints;

public static class DevelopmentRankingWarningEndpoints
{
    private static readonly string[] AllowedRoles =
    [
        "Admin",
        "Editorial Board Chief",
        "EditorialBoardChief",
        "Board Chief"
    ];

    public static IEndpointRouteBuilder MapDevelopmentRankingWarningEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/development/ranking-warning")
            .RequireAuthorization(policy =>
                policy.RequireRole(AllowedRoles));

        group.MapGet(
            "/time",
            (DevelopmentTimeProvider timeProvider) =>
                Results.Ok(timeProvider.GetState()));

        group.MapPost(
            "/time/fixed",
            (
                SetFixedUtcRequest request,
                DevelopmentTimeProvider timeProvider,
                ILoggerFactory loggerFactory) =>
            {
                var state = timeProvider.SetFixedUtc(request.FixedUtc);

                loggerFactory
                    .CreateLogger("DevelopmentRankingWarningTime")
                    .LogWarning(
                        "Development fake time changed. Mode=FixedUtc, EffectiveUtc={EffectiveUtc}",
                        state.EffectiveUtc);

                return Results.Ok(state);
            });

        group.MapPost(
            "/time/offset",
            (
                ApplyOffsetRequest request,
                DevelopmentTimeProvider timeProvider,
                ILoggerFactory loggerFactory) =>
            {
                var state = timeProvider.ApplyOffset(
                    TimeSpan.FromMinutes(request.OffsetMinutes));

                loggerFactory
                    .CreateLogger("DevelopmentRankingWarningTime")
                    .LogWarning(
                        "Development fake time changed. Mode=Offset, OffsetMinutes={OffsetMinutes}, EffectiveUtc={EffectiveUtc}",
                        request.OffsetMinutes,
                        state.EffectiveUtc);

                return Results.Ok(state);
            });

        group.MapPost(
            "/time/reset",
            (
                DevelopmentTimeProvider timeProvider,
                ILoggerFactory loggerFactory) =>
            {
                var state = timeProvider.Reset();

                loggerFactory
                    .CreateLogger("DevelopmentRankingWarningTime")
                    .LogInformation(
                        "Development fake time reset to real time. EffectiveUtc={EffectiveUtc}",
                        state.EffectiveUtc);

                return Results.Ok(state);
            });

        group.MapPost(
            "/run",
            async (
                IRankingWarningEvaluator evaluator,
                CancellationToken cancellationToken) =>
            {
                var summary = await evaluator.EvaluateAsync(
                    new RankingWarningEvaluationRequest(
                        RankingWarningEvaluationTriggers.Manual),
                    cancellationToken);

                return Results.Ok(summary);
            });

        return endpoints;
    }

    public sealed record SetFixedUtcRequest(
        DateTimeOffset FixedUtc);

    public sealed record ApplyOffsetRequest(
        double OffsetMinutes);
}
