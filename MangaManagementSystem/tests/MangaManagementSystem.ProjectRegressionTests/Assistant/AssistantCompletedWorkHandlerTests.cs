using MangaManagementSystem.Application.Features.Assistant.CompletedWork.Queries.GetAssistantCompletedWork;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Assistant;

public sealed class AssistantCompletedWorkHandlerTests
{
    [Fact]
    public async Task Handle_EmptyActorUserId_ThrowsWithoutRepositoryCall()
    {
        var repository =
            new Mock<IAssistantCompletedWorkRepository>(
                MockBehavior.Strict);

        var handler =
            new GetAssistantCompletedWorkQueryHandler(
                repository.Object);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    new GetAssistantCompletedWorkQuery(
                        Guid.Empty),
                    CancellationToken.None));

        Assert.Contains(
            "Actor user ID is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_NoCompletedTasks_ReturnsZeroSummary()
    {
        var actorUserId =
            Guid.NewGuid();

        var repository =
            new Mock<IAssistantCompletedWorkRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.GetCompletedWorkAsync(
                    actorUserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AssistantCompletedWorkReadModel(
                    Array.Empty<AssistantCompletedTaskRow>()));

        var handler =
            new GetAssistantCompletedWorkQueryHandler(
                repository.Object);

        var result =
            await handler.Handle(
                new GetAssistantCompletedWorkQuery(
                    actorUserId),
                CancellationToken.None);

        Assert.Equal(
            0,
            result.CompletedTaskCount);

        Assert.Equal(
            0,
            result.ApprovedRegionCount);

        Assert.Equal(
            0m,
            result.TotalEstimatedAmount);

        Assert.Empty(
            result.Breakdown);

        Assert.Empty(
            result.RecentItems);
    }

    [Fact]
    public async Task Handle_MultipleTaskTypes_AggregatesCountsRegionsAndEstimatedAmounts()
    {
        var actorUserId =
            Guid.NewGuid();

        var now =
            DateTime.UtcNow;

        var tasks =
            new[]
            {
                new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId =
                        Guid.NewGuid(),
                    TypeCode =
                        "SHADING",
                    StatusCode =
                        "COMPLETED",
                    CreatedAtUtc =
                        now.AddDays(-3),
                    RegionCount =
                        3,
                    CompensationAmount =
                        null,
                    SeriesTitle =
                        "Regression Series",
                    ChapterTitle =
                        "Chapter 1",
                    PageNumber =
                        1
                },

                new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId =
                        Guid.NewGuid(),
                    TypeCode =
                        "CLEANUP",
                    StatusCode =
                        "COMPLETED",
                    CreatedAtUtc =
                        now.AddDays(-2),
                    RegionCount =
                        4,
                    CompensationAmount =
                        125000m,
                    SeriesTitle =
                        "Regression Series",
                    ChapterTitle =
                        "Chapter 1",
                    PageNumber =
                        2
                },

                new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId =
                        Guid.NewGuid(),
                    TypeCode =
                        "SHADING",
                    StatusCode =
                        "COMPLETED",
                    CreatedAtUtc =
                        now.AddDays(-1),
                    RegionCount =
                        2,
                    CompensationAmount =
                        null,
                    SeriesTitle =
                        "Regression Series",
                    ChapterTitle =
                        "Chapter 2",
                    PageNumber =
                        3
                }
            };

        var repository =
            new Mock<IAssistantCompletedWorkRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.GetCompletedWorkAsync(
                    actorUserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AssistantCompletedWorkReadModel(
                    tasks));

        var handler =
            new GetAssistantCompletedWorkQueryHandler(
                repository.Object);

        var result =
            await handler.Handle(
                new GetAssistantCompletedWorkQuery(
                    actorUserId),
                CancellationToken.None);

        Assert.Equal(
            3,
            result.CompletedTaskCount);

        Assert.Equal(
            9,
            result.ApprovedRegionCount);

        Assert.Equal(
            325000m,
            result.TotalEstimatedAmount);

        Assert.Equal(
            2,
            result.Breakdown.Count);

        var shading =
            Assert.Single(
                result.Breakdown.Where(
                    item =>
                        item.TaskType == "SHADING"));

        Assert.Equal(
            2,
            shading.CompletedTaskCount);

        Assert.Equal(
            5,
            shading.RegionCount);

        Assert.Equal(
            200000m,
            shading.EstimatedAmount);

        var cleanup =
            Assert.Single(
                result.Breakdown.Where(
                    item =>
                        item.TaskType == "CLEANUP"));

        Assert.Equal(
            1,
            cleanup.CompletedTaskCount);

        Assert.Equal(
            4,
            cleanup.RegionCount);

        Assert.Equal(
            125000m,
            cleanup.EstimatedAmount);
    }

    [Fact]
    public async Task Handle_RecentItems_UsesUpdatedDateWhenAvailableAndSortsNewestFirst()
    {
        var actorUserId =
            Guid.NewGuid();

        var now =
            DateTime.UtcNow;

        var olderTaskId =
            Guid.NewGuid();

        var newerTaskId =
            Guid.NewGuid();

        var tasks =
            new[]
            {
                new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId =
                        newerTaskId,
                    TypeCode =
                        "REVIEW",
                    StatusCode =
                        "COMPLETED",
                    CreatedAtUtc =
                        now.AddDays(-5),
                    UpdatedAtUtc =
                        now.AddHours(-1),
                    RegionCount =
                        1,
                    SeriesTitle =
                        "Series New",
                    ChapterTitle =
                        "Chapter New",
                    PageNumber =
                        10
                },

                new AssistantCompletedTaskRow
                {
                    ChapterPageTaskId =
                        olderTaskId,
                    TypeCode =
                        "REVIEW",
                    StatusCode =
                        "COMPLETED",
                    CreatedAtUtc =
                        now.AddHours(-2),
                    UpdatedAtUtc =
                        null,
                    RegionCount =
                        1,
                    SeriesTitle =
                        "Series Old",
                    ChapterTitle =
                        "Chapter Old",
                    PageNumber =
                        9
                }
            };

        var repository =
            new Mock<IAssistantCompletedWorkRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.GetCompletedWorkAsync(
                    actorUserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AssistantCompletedWorkReadModel(
                    tasks));

        var handler =
            new GetAssistantCompletedWorkQueryHandler(
                repository.Object);

        var result =
            await handler.Handle(
                new GetAssistantCompletedWorkQuery(
                    actorUserId),
                CancellationToken.None);

        Assert.Equal(
            2,
            result.RecentItems.Count);

        Assert.Equal(
            newerTaskId,
            result.RecentItems[0].TaskId);

        Assert.Equal(
            olderTaskId,
            result.RecentItems[1].TaskId);

        Assert.Equal(
            now.AddHours(-1),
            result.RecentItems[0].CompletedAt);

        Assert.Equal(
            now.AddHours(-2),
            result.RecentItems[1].CompletedAt);
    }
}