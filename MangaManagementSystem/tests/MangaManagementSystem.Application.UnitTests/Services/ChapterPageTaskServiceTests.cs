using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Services;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MangaManagementSystem.Application.UnitTests.Services;

public class ChapterPageTaskServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IChapterPageTaskRepository> _taskRepoMock;
    private readonly ChapterPageTaskService _service;

    public ChapterPageTaskServiceTests()
    {
        _taskRepoMock = new Mock<IChapterPageTaskRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ChapterPageTasks).Returns(_taskRepoMock.Object);
        _service = new ChapterPageTaskService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateChapterPageTaskAsync_CreatesAndReturnsDto()
    {
        var taskId = Guid.NewGuid();
        var dto = new CreateChapterPageTaskDto(
            Guid.NewGuid(), Guid.NewGuid(), "SHADING", "ASSIGNED",
            "Test Task", "Description", 3, null, null, null,
            new List<Guid>());

        _taskRepoMock.Setup(r => r.CreateChapterPageTaskAsync(
            dto.ActorUserId, dto.AssignedToUserId, dto.TypeCode,
            dto.TaskTitle, dto.TaskDescription, (byte)dto.PriorityLevel,
            It.IsAny<DateTime>(), dto.CompensationAmount, dto.PageRegionIds))
            .ReturnsAsync(taskId);

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            AssignedToUserId = dto.AssignedToUserId,
            TypeCode = dto.TypeCode,
            StatusCode = "ASSIGNED",
            TaskTitle = dto.TaskTitle,
            TaskDescription = dto.TaskDescription,
            PriorityLevel = (byte)dto.PriorityLevel,
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = dto.ActorUserId
        };

        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(taskId))
            .ReturnsAsync(entity);

        var result = await _service.CreateChapterPageTaskAsync(dto);

        Assert.Equal(taskId, result.ChapterPageTaskId);
        Assert.Equal("ASSIGNED", result.StatusCode);
        Assert.Equal("SHADING", result.TypeCode);
    }

    [Fact]
    public async Task GetChapterPageTaskByIdAsync_ReturnsNull_WhenNotFound()
    {
        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ChapterPageTask?)null);

        var result = await _service.GetChapterPageTaskByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssignedTasksForAssistantAsync_ReturnsTasks_ForCorrectUser()
    {
        var assistantUserId = Guid.NewGuid();
        var tasks = new List<ChapterPageTask>
        {
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(),
                AssignedToUserId = assistantUserId,
                TypeCode = "SHADING",
                StatusCode = "ASSIGNED",
                TaskTitle = "Task 1",
                TaskDescription = "Desc 1",
                DueAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            },
            new()
            {
                ChapterPageTaskId = Guid.NewGuid(),
                AssignedToUserId = assistantUserId,
                TypeCode = "CLEANUP",
                StatusCode = "UNDER_REVIEW",
                TaskTitle = "Task 2",
                TaskDescription = "Desc 2",
                DueAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            }
        };

        _taskRepoMock.Setup(r => r.GetByAssignedUserIdWithFullContextAsync(assistantUserId))
            .ReturnsAsync(tasks);

        var result = await _service.GetAssignedTasksForAssistantAsync(assistantUserId);

        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.StatusCode == "ASSIGNED");
        Assert.Contains(result, t => t.StatusCode == "UNDER_REVIEW");
    }

    [Fact]
    public async Task GetAssignedTaskDetailForAssistantAsync_ReturnsNull_WhenNotAssigned()
    {
        var assistantUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            AssignedToUserId = otherUserId,
            TypeCode = "SHADING",
            StatusCode = "ASSIGNED",
            TaskTitle = "Task",
            TaskDescription = "Desc",
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };

        _taskRepoMock.Setup(r => r.GetByIdWithFullContextAsync(taskId))
            .ReturnsAsync(entity);

        var result = await _service.GetAssignedTaskDetailForAssistantAsync(assistantUserId, taskId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReturnTaskForReworkAsync_Throws_WhenNotActor()
    {
        var actorUserId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            CreatedByUserId = creatorUserId,
            StatusCode = "UNDER_REVIEW",
            TaskTitle = "Task",
            TaskDescription = "Desc",
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(taskId))
            .ReturnsAsync(entity);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReturnTaskForReworkAsync(actorUserId, taskId, "Reason"));

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnTaskForReworkAsync_Throws_WhenNotUnderReview()
    {
        var actorUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            CreatedByUserId = actorUserId,
            StatusCode = "ASSIGNED",
            TaskTitle = "Task",
            TaskDescription = "Desc",
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(taskId))
            .ReturnsAsync(entity);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReturnTaskForReworkAsync(actorUserId, taskId, "Reason"));

        Assert.Contains("only tasks currently under review", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnTaskForReworkAsync_Succeeds_WhenAuthorized()
    {
        var actorUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            CreatedByUserId = actorUserId,
            StatusCode = "UNDER_REVIEW",
            TaskTitle = "Task",
            TaskDescription = "Desc",
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(taskId))
            .ReturnsAsync(entity);

        await _service.ReturnTaskForReworkAsync(actorUserId, taskId, "Fix the shading");

        _taskRepoMock.Verify(r => r.ReturnTaskForReworkAsync(actorUserId, taskId, "Fix the shading"), Times.Once);
    }

    [Fact]
    public async Task ReassignTaskAsync_Throws_WhenActorEmpty()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReassignTaskAsync(Guid.Empty, Guid.NewGuid(),
                new ReassignChapterPageTaskRequest(Guid.NewGuid(), "Reason", null)));

        Assert.Contains("Actor user ID", ex.Message);
    }

    [Fact]
    public async Task ReassignTaskAsync_Throws_WhenTaskEmpty()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReassignTaskAsync(Guid.NewGuid(), Guid.Empty,
                new ReassignChapterPageTaskRequest(Guid.NewGuid(), "Reason", null)));

        Assert.Contains("Task ID", ex.Message);
    }

    [Fact]
    public async Task ReassignTaskAsync_Throws_WhenReasonEmpty()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReassignTaskAsync(Guid.NewGuid(), Guid.NewGuid(),
                new ReassignChapterPageTaskRequest(Guid.NewGuid(), "", null)));

        Assert.Contains("reason", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReassignTaskAsync_Throws_WhenTaskNotFound()
    {
        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ChapterPageTask?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReassignTaskAsync(Guid.NewGuid(), Guid.NewGuid(),
                new ReassignChapterPageTaskRequest(Guid.NewGuid(), "Valid reason", null)));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReassignTaskAsync_Throws_WhenNotCreator()
    {
        var actorUserId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var entity = new ChapterPageTask
        {
            ChapterPageTaskId = taskId,
            CreatedByUserId = creatorUserId,
            AssignedToUserId = Guid.NewGuid(),
            StatusCode = "ASSIGNED",
            TaskTitle = "Task",
            TaskDescription = "Desc",
            DueAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _taskRepoMock.Setup(r => r.GetByIdWithRegionsAsync(taskId))
            .ReturnsAsync(entity);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReassignTaskAsync(actorUserId, taskId,
                new ReassignChapterPageTaskRequest(Guid.NewGuid(), "Valid reason", null)));

        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelTaskAsync_CallsRepository()
    {
        var actorUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await _service.CancelTaskAsync(actorUserId, taskId, "No longer needed");

        _taskRepoMock.Verify(r => r.CancelTaskAsync(actorUserId, taskId, "No longer needed"), Times.Once);
    }
}
