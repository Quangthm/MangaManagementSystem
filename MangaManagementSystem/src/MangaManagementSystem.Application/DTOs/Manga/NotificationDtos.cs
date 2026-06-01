using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record NotificationDto(
        long NotificationId,
        int RecipientUserId,
        string NotificationTypeCode,
        string? Title,
        string Message,
        string? RelatedEntityType,
        long? RelatedEntityId,
        DateTime? ReadAtUtc,
        DateTime CreatedAtUtc
    );

    public record CreateNotificationDto(
        [Required] int RecipientUserId,
        [Required][MaxLength(50)] string NotificationTypeCode,
        [MaxLength(200)] string? Title,
        string Message,
        string? RelatedEntityType,
        long? RelatedEntityId
    );
}
