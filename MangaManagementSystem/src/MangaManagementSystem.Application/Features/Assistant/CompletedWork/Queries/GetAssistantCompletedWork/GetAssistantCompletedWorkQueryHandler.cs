using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork
{
    public sealed class GetAssistantCompletedWorkQueryHandler
        : IRequestHandler<GetAssistantCompletedWorkQuery, AssistantCompletedWorkSummaryDto>
    {
        private readonly IAssistantCompletedWorkRepository _repository;

        public GetAssistantCompletedWorkQueryHandler(
            IAssistantCompletedWorkRepository repository)
        {
            _repository = repository;
        }

        public async Task<AssistantCompletedWorkSummaryDto> Handle(
            GetAssistantCompletedWorkQuery request, CancellationToken cancellationToken)
        {
            if (request.ActorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");

            var data = await _repository.GetCompletedWorkAsync(
                request.ActorUserId, cancellationToken);

            var tasks = data.Tasks;

            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var breakdown = tasks
                .GroupBy(t => t.TypeCode)
                .Select(g =>
                {
                    var regionCount = g.Sum(t => t.RegionCount);
                    var estimatedAmount = g.Sum(t =>
                        AssistantTaskRateMapper.GetEstimatedAmount(t.TypeCode, t.CompensationAmount));
                    return new AssistantCompletedWorkBreakdownDto(
                        g.Key,
                        g.Count(),
                        regionCount,
                        estimatedAmount);
                })
                .OrderByDescending(b => b.EstimatedAmount)
                .ToList();

            var totalEstimatedAmount = breakdown.Sum(b => b.EstimatedAmount);
            var totalRegionCount = breakdown.Sum(b => b.RegionCount);
            var completedTaskCount = tasks.Count;

            var thisMonthEstimatedAmount = tasks
                .Where(t => GetCompletedDate(t) >= currentMonthStart)
                .Sum(t => AssistantTaskRateMapper.GetEstimatedAmount(t.TypeCode, t.CompensationAmount));

            var recentItems = tasks
                .OrderByDescending(t => GetCompletedDate(t))
                .Take(10)
                .Select(t => new AssistantCompletedWorkItemDto(
                    t.ChapterPageTaskId,
                    t.TypeCode,
                    t.SeriesTitle,
                    t.ChapterTitle,
                    t.PageNumber,
                    t.RegionCount,
                    AssistantTaskRateMapper.GetEstimatedAmount(t.TypeCode, t.CompensationAmount),
                    GetCompletedDate(t)))
                .ToList();

            return new AssistantCompletedWorkSummaryDto(
                completedTaskCount,
                totalRegionCount,
                totalEstimatedAmount,
                thisMonthEstimatedAmount,
                breakdown,
                recentItems);
        }

        private static DateTime GetCompletedDate(Domain.Entities.AssistantCompletedTaskRow task)
            => task.UpdatedAtUtc ?? task.CreatedAtUtc;
    }
}
