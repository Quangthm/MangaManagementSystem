using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);
        Task<NotificationDto?> GetNotificationByIdAsync(long id);
        Task<IEnumerable<NotificationDto>> GetNotificationsByRecipientUserIdAsync(int recipientUserId);
        Task<NotificationDto?> MarkNotificationAsReadAsync(long notificationId);
    }
}
