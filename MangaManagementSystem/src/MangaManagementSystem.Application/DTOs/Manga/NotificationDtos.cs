using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record NotificationDto(
        long NotificationId,
        int RecipientUserId,
        string NotificationTypeCode,
        string? Title,
        string? Message,
        string? RelatedEntityType,
        string? RelatedEntityId,
        DateTime? ReadAtUtc,
        DateTime CreatedAtUtc
    );

    public record CreateNotificationDto(
        [Required] int RecipientUserId,
        [Required][MaxLength(50)] string NotificationTypeCode,
        [MaxLength(200)] string? Title,
        [MaxLength(1000)] string? Message,
        string? RelatedEntityType,
        string? RelatedEntityId
    );
}
