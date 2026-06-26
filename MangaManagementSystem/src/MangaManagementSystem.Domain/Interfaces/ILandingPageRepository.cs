using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface ILandingPageRepository
    {
        Task<LandingPageData> GetDashboardDataAsync(CancellationToken ct = default);
    }

    public sealed record LandingPageData(
        int ActiveReviews,
        int PendingTasks,
        int OpenBoardPolls,
        int CompletedTasks,
        int TotalSeries,
        int TotalChapters,
        int ReleasedChapters,
        IReadOnlyList<LandingPageActivityItem> RecentActivities);

    public sealed record LandingPageActivityItem(
        string Label,
        string TypeCode,
        DateTime OccurredAtUtc);
}
