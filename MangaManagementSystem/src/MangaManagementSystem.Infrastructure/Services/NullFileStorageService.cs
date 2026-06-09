using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class NullFileStorageService : IFileStorageService
    {
        public Task<FileUploadResultDto> UploadFileAsync(
            byte[] fileBytes,
            string originalFileName,
            string contentType,
            string filePurposeCode,
            int? uploadedByUserId = null)
        {
            throw new InvalidOperationException(
                "File upload is disabled because Cloudinary credentials are not configured."
            );
        }

        public Task DeleteFileAsync(string publicId, string resourceType)
        {
            return Task.CompletedTask;
        }
    }
}