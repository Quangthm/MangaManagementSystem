using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MediatR;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Commands;

public sealed record UpdateSeriesPublicationFrequencyCommand(
    Guid SeriesId,
    Guid ChiefUserId,
    UpdateSeriesPublicationFrequencyRequestDto Request)
    : IRequest<UpdateSeriesPublicationFrequencyResultDto>;

public sealed class UpdateSeriesPublicationFrequencyCommandHandler
    : IRequestHandler<UpdateSeriesPublicationFrequencyCommand, UpdateSeriesPublicationFrequencyResultDto>
{
    private readonly IEditorialBoardRepository _repository;

    public UpdateSeriesPublicationFrequencyCommandHandler(
        IEditorialBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<UpdateSeriesPublicationFrequencyResultDto> Handle(
        UpdateSeriesPublicationFrequencyCommand request,
        CancellationToken cancellationToken)
    {
        return _repository.UpdateSeriesPublicationFrequencyAsync(
            request.SeriesId,
            request.Request,
            request.ChiefUserId,
            cancellationToken);
    }
}