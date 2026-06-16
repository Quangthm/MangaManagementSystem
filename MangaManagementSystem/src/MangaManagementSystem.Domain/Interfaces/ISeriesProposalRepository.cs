using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface ISeriesProposalRepository : IGenericRepository<SeriesProposal>
    {
        Task<SeriesProposal?> GetByIdWithDetailsAsync(Guid seriesProposalId, CancellationToken ct = default);
        Task<SeriesProposal?> GetLatestBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
        Task<List<SeriesProposal>> GetEditorialQueueAsync(string? statusCode, Guid? seriesId, Guid? submittedByUserId, Guid? reviewedByUserId, CancellationToken ct = default);

        Task<Guid?> ClaimEditorialReviewAsync(Guid seriesProposalId, Guid actorUserId, string? notes, CancellationToken ct = default);
        
        Task<Guid?> RequestRevisionAsync(Guid seriesProposalId, Guid actorUserId, string comments, 
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null, 
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default);
            
        Task<Guid?> PassToBoardAsync(Guid seriesProposalId, Guid actorUserId, string? comments, 
            string? markupOriginalFileName = null, string? markupCloudinaryPublicId = null, string? markupCloudinarySecureUrl = null, 
            string? markupContentType = null, long? markupFileSizeBytes = null, string? markupSha256Hash = null, CancellationToken ct = default);
            
        Task<Guid> CancelProposalAsync(Guid seriesProposalId, Guid actorUserId, string comments, 
            string markupOriginalFileName, string markupCloudinaryPublicId, string markupCloudinarySecureUrl, 
            string markupContentType, long markupFileSizeBytes, string? markupSha256Hash = null, CancellationToken ct = default);
    }
}
