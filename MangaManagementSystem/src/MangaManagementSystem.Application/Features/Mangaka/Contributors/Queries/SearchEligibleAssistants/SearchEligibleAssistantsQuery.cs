using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Contributors.Queries.SearchEligibleAssistants
{
    public sealed record SearchEligibleAssistantsQuery(
        Guid ActorUserId,
        Guid SeriesId,
        string? Search) : IRequest<IReadOnlyList<EligibleAssistantContributorDto>>;
}
