using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record NotificationDto(
        Guid NotificationId,
        Guid RecipientUserId,
        string NotificationTypeCode,
        string? Title,
        string Message,
        string? RelatedEntityType,
        Guid? RelatedEntityId,
        DateTime? ReadAtUtc,
        DateTime CreatedAtUtc
    );

    public record CreateNotificationDto(
        [Required] Guid RecipientUserId,
        [Required][MaxLength(50)] string NotificationTypeCode,
        [MaxLength(200)] string? Title,
        string Message,
        string? RelatedEntityType,
        Guid? RelatedEntityId
    );
}
