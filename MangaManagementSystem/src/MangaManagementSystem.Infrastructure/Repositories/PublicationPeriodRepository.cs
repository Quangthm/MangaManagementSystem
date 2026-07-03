using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class PublicationPeriodRepository : IPublicationPeriodRepository
    {
        private readonly ApplicationDbContext _context;

        public PublicationPeriodRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PublicationPeriod?> GetPeriodContainingDateAsync(
            string periodTypeCode,
            DateTime date,
            CancellationToken ct = default)
        {
            var dateOnly = date.Date;
            return await _context.Set<PublicationPeriod>()
                .AsNoTracking()
                .Where(p => p.PeriodTypeCode == periodTypeCode
                         && p.PeriodStartDate <= dateOnly
                         && p.PeriodEndDate >= dateOnly)
                .OrderBy(p => p.PeriodStartDate)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PublicationPeriod?> GetNextPeriodAsync(
            string periodTypeCode,
            PublicationPeriod currentPeriod,
            CancellationToken ct = default)
        {
            return await _context.Set<PublicationPeriod>()
                .AsNoTracking()
                .Where(p => p.PeriodTypeCode == periodTypeCode
                         && p.PeriodStartDate > currentPeriod.PeriodEndDate)
                .OrderBy(p => p.PeriodStartDate)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PublicationPeriod?> GetPeriodContainingDateOrNullAsync(
            string periodTypeCode,
            DateTime date,
            CancellationToken ct = default)
        {
            return await GetPeriodContainingDateAsync(periodTypeCode, date, ct);
        }
    }
}
