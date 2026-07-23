using MangaManagementSystem.Application.Features.Ranking.Warnings;

namespace MangaManagementSystem.API.HostedServices;

public sealed class RankingWarningEvaluationHostedService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RankingWarningOptions _options;
    private readonly ILogger<RankingWarningEvaluationHostedService> _logger;

    public RankingWarningEvaluationHostedService(
        IServiceScopeFactory scopeFactory,
        RankingWarningOptions options,
        ILogger<RankingWarningEvaluationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunEvaluationAsync(stoppingToken);

            var intervalMinutes = Math.Max(
                1,
                _options.EvaluationIntervalMinutes);

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(intervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunEvaluationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var evaluator = scope.ServiceProvider
                .GetRequiredService<IRankingWarningEvaluator>();

            await evaluator.EvaluateAsync(
                new RankingWarningEvaluationRequest(
                    RankingWarningEvaluationTriggers.Scheduler),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Scheduled ranking warning evaluation failed. It will retry on the next configured interval.");
        }
    }
}
