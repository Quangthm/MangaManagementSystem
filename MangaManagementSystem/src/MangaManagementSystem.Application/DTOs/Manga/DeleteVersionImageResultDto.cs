namespace MangaManagementSystem.Application.DTOs.Manga
{
    /// <summary>
    /// Result of deleting a page version's image. The version row and its regions are always
    /// kept as a history placeholder; only the underlying FileResource is soft-deleted.
    /// </summary>
    /// <param name="Success">True when the image was soft-deleted.</param>
    /// <param name="BlockedReason">Non-null when the delete was refused by a business-rule guard.</param>
    /// <param name="CloudinaryPublicId">Public id the caller should best-effort remove from Cloudinary.</param>
    public record DeleteVersionImageResultDto(bool Success, string? BlockedReason, string? CloudinaryPublicId);
}
