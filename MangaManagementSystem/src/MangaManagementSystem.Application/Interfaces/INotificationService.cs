using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);
        Task<NotificationDto?> GetNotificationByIdAsync(Guid id);
        Task<IEnumerable<NotificationDto>> GetNotificationsByRecipientUserIdAsync(Guid recipientUserId);
        Task<NotificationDto?> MarkNotificationAsReadAsync(Guid notificationId);
    }
}
