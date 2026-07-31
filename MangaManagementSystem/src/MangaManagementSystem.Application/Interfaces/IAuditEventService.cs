using MangaManagementSystem.Application.DTOs.Audit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IAuditEventService
    {
        Task<AuditEventDto?> GetAuditEventByIdAsync(long id);
        Task<IEnumerable<AuditEventDto>> GetAuditEventsByEntityAsync(string entityType, string entityId);
    }
}
