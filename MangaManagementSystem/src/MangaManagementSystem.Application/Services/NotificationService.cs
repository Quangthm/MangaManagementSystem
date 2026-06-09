using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var entity = new Notification
            {
                RecipientUserId = dto.RecipientUserId,
                NotificationTypeCode = dto.NotificationTypeCode,
                Title = dto.Title,
                Message = dto.Message,
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Notifications.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationsByRecipientUserIdAsync(Guid recipientUserId)
        {
            var all = await _unitOfWork.Notifications.GetAllAsync();
            return all
                .Where(n => n.RecipientUserId == recipientUserId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Select(MapToDto);
        }

        public async Task<NotificationDto?> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var entity = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (entity == null)
            {
                return null;
            }

            entity.ReadAtUtc = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        private static NotificationDto MapToDto(Notification n) => new(
            n.NotificationId,
            n.RecipientUserId,
            n.NotificationTypeCode,
            n.Title,
            n.Message,
            n.RelatedEntityType,
            n.RelatedEntityId,
            n.ReadAtUtc,
            n.CreatedAtUtc
        );
    }
}
