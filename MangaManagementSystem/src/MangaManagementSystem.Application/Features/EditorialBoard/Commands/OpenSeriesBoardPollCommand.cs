using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record OpenSeriesBoardPollCommand(
    Guid ChiefUserId,
    OpenSeriesBoardPollRequestDto Request)
    : IRequest<OpenSeriesBoardPollResultDto>;

public sealed class OpenSeriesBoardPollCommandHandler
    : IRequestHandler<OpenSeriesBoardPollCommand, OpenSeriesBoardPollResultDto>
{
    private const string EditorialBoardMemberRoleName =
        "Editorial Board Member";

    private const string ActiveUserStatusCode =
        "ACTIVE";

    private readonly IEditorialBoardRepository _repository;

    public OpenSeriesBoardPollCommandHandler(IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<OpenSeriesBoardPollResultDto> Handle(
        OpenSeriesBoardPollCommand request,
        CancellationToken cancellationToken)
    {
        var notificationPlan =
            new BoardPollNotificationPlan(
                RecipientRoleName:
                    EditorialBoardMemberRoleName,
                RecipientStatusCode:
                    ActiveUserStatusCode,
                NotificationTypeCode:
                    NotificationTypeCodes.BoardPoll,
                Title:
                    "New Board Poll",
                Message:
                    "A new editorial board poll is open and awaiting your vote.",
                RelatedEntityType:
                    NotificationRelatedEntityTypes.SeriesBoardPoll);

        return _repository.OpenPollAsync(
            request.Request,
            request.ChiefUserId,
            notificationPlan,
            cancellationToken);
    }
}