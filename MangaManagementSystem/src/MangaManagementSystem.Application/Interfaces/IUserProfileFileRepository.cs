using System;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public sealed record UserFileReplacementRequest(
        Guid UserId,
        string OriginalFileName,
        string CloudinaryPublicId,
        string CloudinarySecureUrl,
        string ContentType,
        long FileSizeBytes,
        string Sha256Hash);

    public sealed record UserFileReplacementResult(
        Guid NewFileResourceId,
        Guid? OldFileResourceId,
        string? OldCloudinaryPublicId,
        string? OldContentType);

    public interface IUserProfileFileRepository
    {
        Task<UserFileReplacementResult> ReplaceAvatarFileAsync(
            UserFileReplacementRequest request);

        Task<UserFileReplacementResult> ReplacePortfolioFileAsync(
            UserFileReplacementRequest request);
    }
}