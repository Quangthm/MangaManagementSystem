using MangaManagementSystem.Application.Features.Notifications;
using MangaManagementSystem.Domain.Interfaces;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Notifications;

public sealed class NotificationHandlersTests
{
    [Fact]
    public async Task MarkAsRead_EmptyRecipientUserId_ThrowsWithoutRepositoryCall()
    {
        var repository =
            new Mock<INotificationRepository>(
                MockBehavior.Strict);

        var handler =
            new MarkNotificationAsReadCommandHandler(
                repository.Object);

        var command =
            new MarkNotificationAsReadCommand(
                Guid.Empty,
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Contains(
            "Recipient user id is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MarkAsRead_EmptyNotificationId_ThrowsWithoutRepositoryCall()
    {
        var repository =
            new Mock<INotificationRepository>(
                MockBehavior.Strict);

        var handler =
            new MarkNotificationAsReadCommandHandler(
                repository.Object);

        var command =
            new MarkNotificationAsReadCommand(
                Guid.NewGuid(),
                Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Contains(
            "Notification id is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MarkAsRead_ValidRequest_ForwardsRecipientNotificationAndUtcTimestamp()
    {
        var recipientUserId =
            Guid.NewGuid();

        var notificationId =
            Guid.NewGuid();

        DateTime capturedReadAtUtc =
            default;

        var repository =
            new Mock<INotificationRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.MarkAsReadAsync(
                    recipientUserId,
                    notificationId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTime, CancellationToken>(
                (_, _, readAtUtc, _) =>
                    capturedReadAtUtc = readAtUtc)
            .ReturnsAsync(true);

        var handler =
            new MarkNotificationAsReadCommandHandler(
                repository.Object);

        var before =
            DateTime.UtcNow;

        var result =
            await handler.Handle(
                new MarkNotificationAsReadCommand(
                    recipientUserId,
                    notificationId),
                CancellationToken.None);

        var after =
            DateTime.UtcNow;

        Assert.True(result);
        Assert.InRange(
            capturedReadAtUtc,
            before,
            after);

        repository.Verify(
            repo =>
                repo.MarkAsReadAsync(
                    recipientUserId,
                    notificationId,
                    It.Is<DateTime>(
                        value =>
                            value >= before
                            && value <= after),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_ValidRequest_ReturnsRepositoryUpdatedCount()
    {
        var recipientUserId =
            Guid.NewGuid();

        var repository =
            new Mock<INotificationRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.MarkAllAsReadAsync(
                    recipientUserId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var handler =
            new MarkAllNotificationsAsReadCommandHandler(
                repository.Object);

        var result =
            await handler.Handle(
                new MarkAllNotificationsAsReadCommand(
                    recipientUserId),
                CancellationToken.None);

        Assert.Equal(
            4,
            result.UpdatedCount);

        repository.Verify(
            repo =>
                repo.MarkAllAsReadAsync(
                    recipientUserId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_ValidRecipient_ReturnsRepositoryCount()
    {
        var recipientUserId =
            Guid.NewGuid();

        var repository =
            new Mock<INotificationRepository>(
                MockBehavior.Strict);

        repository
            .Setup(repo =>
                repo.CountUnreadByRecipientUserIdAsync(
                    recipientUserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var handler =
            new GetUnreadNotificationCountQueryHandler(
                repository.Object);

        var result =
            await handler.Handle(
                new GetUnreadNotificationCountQuery(
                    recipientUserId),
                CancellationToken.None);

        Assert.Equal(
            7,
            result.UnreadCount);

        repository.Verify(
            repo =>
                repo.CountUnreadByRecipientUserIdAsync(
                    recipientUserId,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
}