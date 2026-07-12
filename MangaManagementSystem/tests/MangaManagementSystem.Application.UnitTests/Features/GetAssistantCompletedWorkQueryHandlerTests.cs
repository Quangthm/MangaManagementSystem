using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork;
using MangaManagementSystem.Application.Interfaces;
using Moq;
using Xunit;

namespace MangaManagementSystem.Application.UnitTests.Features;

public class GetAssistantCompletedWorkQueryHandlerTests
{
    private readonly Mock<IAssistantCompletedWorkRepository> _repoMock;
    private readonly GetAssistantCompletedWorkQueryHandler _handler;

    public GetAssistantCompletedWorkQueryHandlerTests()
    {
        _repoMock = new Mock<IAssistantCompletedWorkRepository>();
        _handler = new GetAssistantCompletedWorkQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSummary_WithCorrectCounts()
    {
        var userId = Guid.NewGuid();
        var tasks = new List<AssistantCompletedTaskRow>
        {
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(), TypeCode = "SHADING",
                StatusCode = "COMPLETED", RegionCount = 3, CompensationAmount = 100_000,
                SeriesTitle = "Series A", ChapterTitle = "Ch 1", PageNumber = 5,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(), TypeCode = "CLEANUP",
                StatusCode = "COMPLETED", RegionCount = 2, CompensationAmount = null,
                SeriesTitle = "Series B", ChapterTitle = "Ch 2", PageNumber = 10,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        };

        _repoMock.Setup(r => r.GetCompletedWorkAsync(userId, default))
            .ReturnsAsync(new AssistantCompletedWorkReadModel(tasks));

        var result = await _handler.Handle(new GetAssistantCompletedWorkQuery(userId), default);

        Assert.Equal(2, result.CompletedTaskCount);
        Assert.Equal(5, result.ApprovedRegionCount);
    }

    [Fact]
    public async Task Handle_Breakdown_ByTaskType()
    {
        var userId = Guid.NewGuid();
        var tasks = new List<AssistantCompletedTaskRow>
        {
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(), TypeCode = "SHADING",
                StatusCode = "COMPLETED", RegionCount = 3, CompensationAmount = 100_000,
                SeriesTitle = "S", ChapterTitle = "Ch", PageNumber = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(), TypeCode = "SHADING",
                StatusCode = "COMPLETED", RegionCount = 5, CompensationAmount = 100_000,
                SeriesTitle = "S", ChapterTitle = "Ch", PageNumber = 2,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(), TypeCode = "CLEANUP",
                StatusCode = "COMPLETED", RegionCount = 2, CompensationAmount = null,
                SeriesTitle = "S", ChapterTitle = "Ch", PageNumber = 3,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        _repoMock.Setup(r => r.GetCompletedWorkAsync(userId, default))
            .ReturnsAsync(new AssistantCompletedWorkReadModel(tasks));

        var result = await _handler.Handle(new GetAssistantCompletedWorkQuery(userId), default);

        Assert.Equal(2, result.Breakdown.Count);
        var shading = result.Breakdown.Single(b => b.TaskType == "SHADING");
        Assert.Equal(2, shading.CompletedTaskCount);
        Assert.Equal(8, shading.RegionCount);
        Assert.Equal(200_000, shading.EstimatedAmount);

        var cleanup = result.Breakdown.Single(b => b.TaskType == "CLEANUP");
        Assert.Equal(1, cleanup.CompletedTaskCount);
        Assert.Equal(2, cleanup.RegionCount);
        Assert.Equal(80_000, cleanup.EstimatedAmount);
    }

    [Fact]
    public async Task Handle_RecentItems_LimitedTo10()
    {
        var userId = Guid.NewGuid();
        var tasks = Enumerable.Range(1, 15).Select(i => new AssistantCompletedTaskRow
        {
            ChapterPageTaskId = Guid.NewGuid(),
            TypeCode = "SHADING",
            StatusCode = "COMPLETED",
            RegionCount = 1,
            CompensationAmount = 100_000,
            SeriesTitle = $"Series {i}",
            ChapterTitle = "Ch",
            PageNumber = i,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-i),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-i)
        }).ToList();

        _repoMock.Setup(r => r.GetCompletedWorkAsync(userId, default))
            .ReturnsAsync(new AssistantCompletedWorkReadModel(tasks));

        var result = await _handler.Handle(new GetAssistantCompletedWorkQuery(userId), default);

        Assert.Equal(10, result.RecentItems.Count);
        Assert.Contains(result.RecentItems, r => r.SeriesTitle == "Series 1");
    }

    [Fact]
    public async Task Handle_Throws_WhenActorIdEmpty()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new GetAssistantCompletedWorkQuery(Guid.Empty), default));

        Assert.Contains("Actor user ID", ex.Message);
    }

    [Fact]
    public async Task Handle_ReturnsZero_WhenNoCompletedTasks()
    {
        var userId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetCompletedWorkAsync(userId, default))
            .ReturnsAsync(new AssistantCompletedWorkReadModel(new List<AssistantCompletedTaskRow>()));

        var result = await _handler.Handle(new GetAssistantCompletedWorkQuery(userId), default);

        Assert.Equal(0, result.CompletedTaskCount);
        Assert.Equal(0, result.ApprovedRegionCount);
        Assert.Equal(0, result.TotalEstimatedAmount);
        Assert.Empty(result.Breakdown);
        Assert.Empty(result.RecentItems);
    }
}
