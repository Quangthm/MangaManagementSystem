using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Admin.Users.Commands;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Admin;

public sealed class AdminUserCommandHandlersTests
{
    [Fact]
    public async Task Disable_SameActorAndTarget_ThrowsWithoutCallingUserService()
    {
        var adminUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var handler =
            CreateHandler(userService.Object);

        var command =
            new DisableAdminUserCommand(
                adminUserId,
                adminUserId,
                "self disable");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Contains(
            "cannot disable or reject",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        userService.Verify(
            service => service.DisableUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Reject_SameActorAndTarget_ThrowsWithoutCallingUserService()
    {
        var adminUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var handler =
            CreateHandler(userService.Object);

        var command =
            new RejectAdminUserCommand(
                adminUserId,
                adminUserId,
                "self reject");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                command,
                CancellationToken.None));

        userService.Verify(
            service => service.RejectUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>()),
            Times.Never);

        userService.Verify(
            service => service.GetUserByIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task Disable_ValidTarget_ForwardsReasonAndReturnsUpdatedUser()
    {
        var actorUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        const string reason =
            "Regression disable reason";

        var expectedUser =
            CreateUser(
                targetUserId,
                statusCode: "DISABLED");

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.DisableUserAsync(
                    actorUserId,
                    targetUserId,
                    reason))
            .ReturnsAsync(expectedUser);

        var handler =
            CreateHandler(userService.Object);

        var result =
            await handler.Handle(
                new DisableAdminUserCommand(
                    actorUserId,
                    targetUserId,
                    reason),
                CancellationToken.None);

        Assert.Equal(expectedUser, result);

        userService.Verify(
            service => service.DisableUserAsync(
                actorUserId,
                targetUserId,
                reason),
            Times.Once);
    }

    [Fact]
    public async Task Reject_ValidTarget_ForwardsReasonAndReturnsRefreshedUser()
    {
        var actorUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        const string reason =
            "Regression reject reason";

        var expectedUser =
            CreateUser(
                targetUserId,
                statusCode: "REJECTED");

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.RejectUserAsync(
                    actorUserId,
                    targetUserId,
                    reason))
            .Returns(Task.CompletedTask);

        userService
            .Setup(service =>
                service.GetUserByIdAsync(
                    targetUserId))
            .ReturnsAsync(expectedUser);

        var handler =
            CreateHandler(userService.Object);

        var result =
            await handler.Handle(
                new RejectAdminUserCommand(
                    actorUserId,
                    targetUserId,
                    reason),
                CancellationToken.None);

        Assert.Equal(expectedUser, result);

        userService.Verify(
            service => service.RejectUserAsync(
                actorUserId,
                targetUserId,
                reason),
            Times.Once);

        userService.Verify(
            service => service.GetUserByIdAsync(
                targetUserId),
            Times.Once);
    }

    [Fact]
    public async Task Activate_ValidTarget_ForwardsActorAndTarget()
    {
        var actorUserId =
            Guid.NewGuid();

        var targetUserId =
            Guid.NewGuid();

        var expectedUser =
            CreateUser(
                targetUserId,
                statusCode: "ACTIVE");

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.ActivateUserAsync(
                    actorUserId,
                    targetUserId))
            .ReturnsAsync(expectedUser);

        var handler =
            CreateHandler(userService.Object);

        var result =
            await handler.Handle(
                new ActivateAdminUserCommand(
                    actorUserId,
                    targetUserId),
                CancellationToken.None);

        Assert.Equal(expectedUser, result);

        userService.Verify(
            service => service.ActivateUserAsync(
                actorUserId,
                targetUserId),
            Times.Once);
    }

    private static AdminUserCommandHandlers CreateHandler(
        IUserService userService)
    {
        var userRepository =
            new Mock<IUserRepository>(
                MockBehavior.Strict);

        var authService =
            new Mock<IAuthService>(
                MockBehavior.Strict);

        return new AdminUserCommandHandlers(
            userService,
            userRepository.Object,
            authService.Object);
    }

    private static UserDto CreateUser(
        Guid userId,
        string statusCode)
    {
        return new UserDto(
            userId,
            Username: "admin-regression-target",
            DisplayName: "Admin Regression Target",
            Email: "admin-regression@example.test",
            AvatarFileId: null,
            PortfolioFileId: null,
            StatusCode: statusCode,
            CreatedAtUtc: DateTime.UtcNow,
            RoleName: "Mangaka");
    }
}