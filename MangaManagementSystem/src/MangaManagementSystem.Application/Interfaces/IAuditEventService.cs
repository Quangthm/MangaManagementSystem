using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces;

public interface IAuditEventService
{
<<<<<<< HEAD
    Task<AuditEventDto> CreateAuditEventAsync(CreateAuditEventDto dto);
    Task<AuditEventDto?> GetAuditEventByIdAsync(long id);
    Task<IEnumerable<AuditEventDto>> GetAuditEventsByEntityAsync(string entityType, string entityId);
=======
    public interface IAuditEventService
    {
        Task<AuditEventDto?> GetAuditEventByIdAsync(long id);
        Task<IEnumerable<AuditEventDto>> GetAuditEventsByEntityAsync(string entityType, string entityId);
    }
>>>>>>> main
}
