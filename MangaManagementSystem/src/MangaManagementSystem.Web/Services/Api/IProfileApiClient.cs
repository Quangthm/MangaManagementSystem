using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IProfileApiClient
    {
        Task<UserDto> GetProfileAsync(
            Guid userId);

        Task<FileResourceDto?> GetFileAsync(
            Guid fileResourceId);

        Task<UserDto> UpdateDisplayNameAsync(
            Guid userId,
            string displayName);

        Task<UserDto> UpdateAvatarAsync(
            Guid userId,
            byte[] fileBytes,
            string fileName,
            string contentType);

        Task<UserDto> UpdatePortfolioAsync(
            Guid userId,
            byte[] fileBytes,
            string fileName,
            string contentType);
    }
}
