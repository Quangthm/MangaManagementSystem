using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Commands.SubmitSeriesProposal
{
    /// <summary>
    /// CQRS write command for BF-SERIES-003 — Submit Series Proposal for Editorial Review.
    /// The handler owns: input validation, Cloudinary upload, Cloudinary cleanup on SQL failure,
    /// and the stored-procedure call through ISeriesProposalRepository.
    /// 
    /// The controller is intentionally thin: it reads the HTTP boundary (route param, form file,
    /// actor header) and delegates all orchestration here.
    /// </summary>
    public sealed record SubmitSeriesProposalCommand(
        Guid ActorUserId,
        Guid SeriesId,
        byte[] ProposalFileBytes,
        string ProposalFileName,
        string ProposalContentType) : IRequest<SeriesProposalSubmittedDto>;
}
