using System.Security.Claims;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Controllers;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Profile;

public sealed class ProfilePasswordControllerTests
{
    [Fact]
    public async Task ResetAsync_InvalidActor_ReturnsUnauthorizedWithoutResettingPassword()
    {
        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Failure(
                    AuthenticatedActorFailureKind.InvalidIdentity));

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.ResetAsync(
                new ResetProfilePasswordRequest(
                    "123456",
                    "ValidPassword123"));

        Assert.IsType<UnauthorizedObjectResult>(result);

        userService.Verify(
            service => service.VerifyProfileOtpAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        userService.Verify(
            service => service.ResetPasswordAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetAsync_EmptyOtp_ReturnsBadRequestWithoutOtpVerification()
    {
        var actorUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var actorResolver =
            CreateSuccessfulActorResolver(actorUserId);

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.ResetAsync(
                new ResetProfilePasswordRequest(
                    " ",
                    "ValidPassword123"));

        Assert.IsType<BadRequestObjectResult>(result);

        userService.Verify(
            service => service.VerifyProfileOtpAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        userService.Verify(
            service => service.ResetPasswordAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetAsync_ShortPassword_ReturnsBadRequestWithoutOtpVerification()
    {
        var actorUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var actorResolver =
            CreateSuccessfulActorResolver(actorUserId);

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.ResetAsync(
                new ResetProfilePasswordRequest(
                    "123456",
                    "short"));

        Assert.IsType<BadRequestObjectResult>(result);

        userService.Verify(
            service => service.VerifyProfileOtpAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        userService.Verify(
            service => service.ResetPasswordAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetAsync_InvalidOtp_ReturnsBadRequestWithoutResettingPassword()
    {
        var actorUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.VerifyProfileOtpAsync(
                    actorUserId,
                    "PROFILE_PASSWORD_RESET",
                    "654321"))
            .ReturnsAsync(false);

        var actorResolver =
            CreateSuccessfulActorResolver(actorUserId);

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.ResetAsync(
                new ResetProfilePasswordRequest(
                    "654321",
                    "ValidPassword123"));

        Assert.IsType<BadRequestObjectResult>(result);

        userService.Verify(
            service => service.VerifyProfileOtpAsync(
                actorUserId,
                "PROFILE_PASSWORD_RESET",
                "654321"),
            Times.Once);

        userService.Verify(
            service => service.ResetPasswordAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetAsync_ValidOtp_ResetsPasswordAndReturnsOk()
    {
        var actorUserId =
            Guid.NewGuid();

        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        userService
            .Setup(service =>
                service.VerifyProfileOtpAsync(
                    actorUserId,
                    "PROFILE_PASSWORD_RESET",
                    "123456"))
            .ReturnsAsync(true);

        userService
            .Setup(service =>
                service.ResetPasswordAsync(
                    actorUserId,
                    "NewPassword123"))
            .Returns(Task.CompletedTask);

        var actorResolver =
            CreateSuccessfulActorResolver(actorUserId);

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.ResetAsync(
                new ResetProfilePasswordRequest(
                    "123456",
                    "NewPassword123"));

        Assert.IsType<OkObjectResult>(result);

        userService.Verify(
            service => service.VerifyProfileOtpAsync(
                actorUserId,
                "PROFILE_PASSWORD_RESET",
                "123456"),
            Times.Once);

        userService.Verify(
            service => service.ResetPasswordAsync(
                actorUserId,
                "NewPassword123"),
            Times.Once);
    }

    [Fact]
    public async Task SendOtpAsync_InactiveActor_ReturnsForbiddenWithoutSendingOtp()
    {
        var userService =
            new Mock<IUserService>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Failure(
                    AuthenticatedActorFailureKind.InactiveAccount));

        var controller =
            CreateController(
                userService.Object,
                actorResolver.Object);

        var result =
            await controller.SendOtpAsync();

        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            objectResult.StatusCode);

        userService.Verify(
            service => service.SendProfileOtpAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static Mock<IAuthenticatedActorResolver>
        CreateSuccessfulActorResolver(
            Guid actorUserId)
    {
        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(
                MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Success(
                    actorUserId,
                    "Admin"));

        return actorResolver;
    }

    private static ProfilePasswordController
        CreateController(
            IUserService userService,
            IAuthenticatedActorResolver actorResolver)
    {
        var controller =
            new ProfilePasswordController(
                userService,
                actorResolver,
                NullLogger<ProfilePasswordController>.Instance);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }
}