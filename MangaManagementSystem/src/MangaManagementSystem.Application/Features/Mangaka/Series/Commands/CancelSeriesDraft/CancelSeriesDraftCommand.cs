using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CancelSeriesDraft
{
    /// <summary>
    /// CQRS write command for the Cancel Draft workflow.
    /// Transitions a PROPOSAL_DRAFT series to CANCELLED via manga.usp_Series_CancelDraft.
    /// No Cloudinary involvement — pure status transition, no file cleanup required.
    /// The stored procedure enforces: PROPOSAL_DRAFT guard, active-Mangaka-contributor check,
    /// app lock, and audit event.
    /// </summary>
    public sealed record CancelSeriesDraftCommand(
        Guid ActorUserId,
        Guid SeriesId,
        string? Reason) : IRequest<SeriesDraftCancelledDto>;
}
