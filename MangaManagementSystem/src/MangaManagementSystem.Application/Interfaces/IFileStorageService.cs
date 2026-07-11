using MangaManagementSystem.Application.DTOs.Manga;
using System.IO;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces;

public interface IFileStorageService
{
    // Use a neutral payload to avoid ASP.NET Core dependencies in the Application layer.
    Task<FileUploadResultDto> UploadFileAsync(byte[] fileBytes, string originalFileName, string contentType, string filePurposeCode, int? uploadedByUserId = null);

    // Stream-based upload to avoid buffering entire files in memory.
    Task<FileUploadResultDto> UploadFileAsync(Stream fileStream, string originalFileName, string contentType, string filePurposeCode, int? uploadedByUserId = null);

    Task DeleteFileAsync(string publicId, string resourceType);
}
