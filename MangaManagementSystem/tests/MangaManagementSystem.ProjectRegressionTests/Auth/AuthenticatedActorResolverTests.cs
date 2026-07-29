using System.Security.Claims;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Auth;

public sealed class AuthenticatedActorResolverTests
{
    [Fact]
    public async Task ResolveActiveUserAsync_InvalidIdentity_ReturnsFailureWithoutUserLookup()
    {
        var userService = new Mock<IUserService>(MockBehavior.Strict);
        var resolver = new AuthenticatedActorResolver(userService.Object);

        var principal = CreatePrincipal();

        var result =
            await resolver.ResolveActiveUserAsync(principal);

        Assert.False(result.Succeeded);
        Assert.Equal(
            AuthenticatedActorFailureKind.InvalidIdentity,
            result.FailureKind);
        Assert.Equal(Guid.Empty, result.ActorUserId);
        Assert.Equal(string.Empty, result.ActorRoleName);

        userService.Verify(
            service => service.GetUserByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveActiveUserAsync_UserDoesNotExist_ReturnsUserNotFound()
    {
        var actorUserId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.GetUserByIdAsync(actorUserId))
            .ReturnsAsync((UserDto?)null);

        var resolver =
            new AuthenticatedActorResolver(userService.Object);

        var principal =
            CreatePrincipal(
                actorUserId,
                tokenRole: "Admin");

        var result =
            await resolver.ResolveActiveUserAsync(principal);

        Assert.False(result.Succeeded);
        Assert.Equal(
            AuthenticatedActorFailureKind.UserNotFound,
            result.FailureKind);

        userService.Verify(
            service => service.GetUserByIdAsync(actorUserId),
            Times.Once);
    }

    [Fact]
    public async Task ResolveActiveUserAsync_DisabledAccount_ReturnsInactiveAccount()
    {
        var actorUserId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.GetUserByIdAsync(actorUserId))
            .ReturnsAsync(
                CreateUser(
                    actorUserId,
                    statusCode: "DISABLED",
                    roleName: "Admin"));

        var resolver =
            new AuthenticatedActorResolver(userService.Object);

        var principal =
            CreatePrincipal(
                actorUserId,
                tokenRole: "Admin");

        var result =
            await resolver.ResolveActiveUserAsync(principal);

        Assert.False(result.Succeeded);
        Assert.Equal(
            AuthenticatedActorFailureKind.InactiveAccount,
            result.FailureKind);
    }

    [Fact]
    public async Task ResolveAsync_TokenClaimsAdminButDatabaseRoleIsMangaka_ReturnsWrongRole()
    {
        var actorUserId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.GetUserByIdAsync(actorUserId))
            .ReturnsAsync(
                CreateUser(
                    actorUserId,
                    statusCode: "ACTIVE",
                    roleName: "Mangaka"));

        var resolver =
            new AuthenticatedActorResolver(userService.Object);

        var principal =
            CreatePrincipal(
                actorUserId,
                tokenRole: "Admin");

        var result =
            await resolver.ResolveAsync(
                principal,
                "Admin");

        Assert.False(result.Succeeded);
        Assert.Equal(
            AuthenticatedActorFailureKind.WrongRole,
            result.FailureKind);
    }

    [Fact]
    public async Task ResolveAsync_TokenRoleIsStaleButDatabaseRoleIsAdmin_UsesCurrentDatabaseRole()
    {
        var actorUserId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.GetUserByIdAsync(actorUserId))
            .ReturnsAsync(
                CreateUser(
                    actorUserId,
                    statusCode: "ACTIVE",
                    roleName: "Admin"));

        var resolver =
            new AuthenticatedActorResolver(userService.Object);

        var principal =
            CreatePrincipal(
                actorUserId,
                tokenRole: "Mangaka");

        var result =
            await resolver.ResolveAsync(
                principal,
                "Admin");

        Assert.True(result.Succeeded);
        Assert.Equal(
            AuthenticatedActorFailureKind.None,
            result.FailureKind);
        Assert.Equal(actorUserId, result.ActorUserId);
        Assert.Equal("Admin", result.ActorRoleName);
    }

    private static ClaimsPrincipal CreatePrincipal(
        Guid? actorUserId = null,
        string? tokenRole = null)
    {
        var claims =
            new List<Claim>();

        if (actorUserId.HasValue)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.NameIdentifier,
                    actorUserId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(tokenRole))
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    tokenRole));
        }

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                authenticationType: "RegressionTest"));
    }

    private static UserDto CreateUser(
        Guid userId,
        string statusCode,
        string roleName)
    {
        return new UserDto(
            userId,
            Username: "regression-user",
            DisplayName: "Regression User",
            Email: "regression@example.test",
            AvatarFileId: null,
            PortfolioFileId: null,
            StatusCode: statusCode,
            CreatedAtUtc: DateTime.UtcNow,
            RoleName: roleName);
    }
}