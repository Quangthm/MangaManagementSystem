using MangaManagementSystem.Application.DTOs.Manga;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface ISeriesProposalService
    {
        // Reads (EF Core)
        Task<SeriesProposalDto?> GetProposalByIdAsync(Guid seriesProposalId, CancellationToken ct = default);
        Task<IEnumerable<ProposalQueueItemDto>> GetEditorialQueueAsync(ProposalQueueFilterDto filter, CancellationToken ct = default);
        Task<SeriesProposalDto?> GetLatestProposalBySeriesAsync(Guid seriesId, CancellationToken ct = default);

        // Create
        Task<SeriesProposalDto> CreateProposalAsync(CreateProposalDto dto, CancellationToken ct = default);

        // Commands (SPs)
        Task ClaimEditorialReviewAsync(Guid seriesProposalId, Guid actorUserId, string? notes, CancellationToken ct = default);
        Task RequestRevisionAsync(Guid seriesProposalId, Guid actorUserId, string comments, FileUploadResultDto? markupFile, CancellationToken ct = default);
        Task PassToBoardAsync(Guid seriesProposalId, Guid actorUserId, string? comments, FileUploadResultDto? markupFile, CancellationToken ct = default);
        Task CancelProposalAsync(Guid seriesProposalId, Guid actorUserId, string comments, FileUploadResultDto markupFile, CancellationToken ct = default);
    }
}
