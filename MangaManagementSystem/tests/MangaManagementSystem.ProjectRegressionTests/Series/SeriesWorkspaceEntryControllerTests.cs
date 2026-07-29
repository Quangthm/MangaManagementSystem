using System.Security.Claims;
using MangaManagementSystem.API.Controllers;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Features.Series.Queries.GetSeriesWorkspaceEntry;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MangaManagementSystem.ProjectRegressionTests.Series;

public sealed class SeriesWorkspaceEntryControllerTests
{
    [Fact]
    public async Task GetWorkspaceEntryAsync_WhitespaceSlug_ReturnsBadRequestWithoutResolvingActor()
    {
        var mediator =
            new Mock<IMediator>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(
                MockBehavior.Strict);

        var controller =
            CreateController(
                mediator.Object,
                actorResolver.Object);

        var result =
            await controller.GetWorkspaceEntryAsync("   ");

        Assert.IsType<BadRequestObjectResult>(result);

        actorResolver.Verify(
            resolver => resolver.ResolveActiveUserAsync(
                It.IsAny<ClaimsPrincipal>()),
            Times.Never);

        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWorkspaceEntryAsync_InvalidIdentity_ReturnsUnauthorizedWithoutCallingMediator()
    {
        var mediator =
            new Mock<IMediator>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(
                MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Failure(
                    AuthenticatedActorFailureKind.InvalidIdentity));

        var controller =
            CreateController(
                mediator.Object,
                actorResolver.Object);

        var result =
            await controller.GetWorkspaceEntryAsync(
                "regression-series");

        Assert.IsType<UnauthorizedObjectResult>(result);

        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWorkspaceEntryAsync_UserNotFound_ReturnsUnauthorizedWithoutCallingMediator()
    {
        var mediator =
            new Mock<IMediator>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(
                MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Failure(
                    AuthenticatedActorFailureKind.UserNotFound));

        var controller =
            CreateController(
                mediator.Object,
                actorResolver.Object);

        var result =
            await controller.GetWorkspaceEntryAsync(
                "regression-series");

        Assert.IsType<UnauthorizedObjectResult>(result);

        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWorkspaceEntryAsync_InactiveAccount_ReturnsForbiddenWithoutCallingMediator()
    {
        var mediator =
            new Mock<IMediator>(MockBehavior.Strict);

        var actorResolver =
            new Mock<IAuthenticatedActorResolver>(
                MockBehavior.Strict);

        actorResolver
            .Setup(resolver =>
                resolver.ResolveActiveUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(
                AuthenticatedActorResult.Failure(
                    AuthenticatedActorFailureKind.InactiveAccount));

        var controller =
            CreateController(
                mediator.Object,
                actorResolver.Object);

        var result =
            await controller.GetWorkspaceEntryAsync(
                "regression-series");

        var objectResult =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            objectResult.StatusCode);

        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWorkspaceEntryAsync_ActiveActor_UsesResolvedUserIdAndReturnsNotFoundWhenSeriesDoesNotExist()
    {
        var actorUserId =
            Guid.NewGuid();

        var mediator =
            new Mock<IMediator>(MockBehavior.Strict);

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
                    "Mangaka"));

        mediator
            .Setup(sender =>
                sender.Send(
                    It.Is<GetSeriesWorkspaceEntryQuery>(
                        query =>
                            query.Slug == "regression-series"
                            && query.ActorUserId == actorUserId),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (SeriesWorkspaceEntryDto?)null);

        var controller =
            CreateController(
                mediator.Object,
                actorResolver.Object);

        var result =
            await controller.GetWorkspaceEntryAsync(
                "regression-series");

        Assert.IsType<NotFoundObjectResult>(result);

        mediator.Verify(
            sender =>
                sender.Send(
                    It.Is<GetSeriesWorkspaceEntryQuery>(
                        query =>
                            query.Slug == "regression-series"
                            && query.ActorUserId == actorUserId),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SeriesController CreateController(
        IMediator mediator,
        IAuthenticatedActorResolver actorResolver)
    {
        var controller =
            new SeriesController(
                mediator,
                actorResolver,
                NullLogger<SeriesController>.Instance);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }
}