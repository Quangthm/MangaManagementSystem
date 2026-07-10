using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IPublicationPeriodRepository
    {
        Task<PublicationPeriod?> GetPeriodContainingDateAsync(
            string periodTypeCode,
            DateTime date,
            CancellationToken ct = default);

        Task<PublicationPeriod?> GetNextPeriodAsync(
            string periodTypeCode,
            PublicationPeriod currentPeriod,
            CancellationToken ct = default);

        Task<PublicationPeriod?> GetPeriodContainingDateOrNullAsync(
            string periodTypeCode,
            DateTime date,
            CancellationToken ct = default);
    }
}
