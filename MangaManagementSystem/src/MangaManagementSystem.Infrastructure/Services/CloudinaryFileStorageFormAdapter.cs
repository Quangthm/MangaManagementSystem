using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Services
{
    // Adapter available in Infrastructure so controllers or Program.cs can accept IFormFile and forward to the application-safe API.
    public class CloudinaryFileStorageFormAdapter
    {
        private readonly IFileStorageService _fileStorageService;

        public CloudinaryFileStorageFormAdapter(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<MangaManagementSystem.Application.DTOs.Manga.FileUploadResultDto> UploadFormFileAsync(IFormFile file, string purpose, int? uploadedBy)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            return await _fileStorageService.UploadFileAsync(bytes, file.FileName, file.ContentType, purpose, uploadedBy);
        }
    }
}
