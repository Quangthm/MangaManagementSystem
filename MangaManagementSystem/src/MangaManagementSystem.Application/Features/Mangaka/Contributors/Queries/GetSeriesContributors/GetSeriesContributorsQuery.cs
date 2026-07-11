using MangaManagementSystem.Domain.ReadModels;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.GetSeriesContributors;

public sealed record GetSeriesContributorsQuery(
    Guid ActorUserId,
    Guid SeriesId) : IRequest<IReadOnlyList<SeriesContributorListItemDto>>;
