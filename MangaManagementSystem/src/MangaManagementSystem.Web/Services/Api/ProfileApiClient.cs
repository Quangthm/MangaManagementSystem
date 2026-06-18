using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ProfileApiClient
        : IProfileApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfileApiClient> _logger;

        public ProfileApiClient(
            HttpClient httpClient,
            ILogger<ProfileApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UserDto> GetProfileAsync(
            Guid userId)
        {
            using var response =
                await _httpClient.GetAsync(
                    $"api/profile/{userId:D}");

            return await ReadRequiredAsync<UserDto>(
                response,
                "Unable to load profile.");
        }

        public async Task<FileResourceDto?> GetFileAsync(
            Guid fileResourceId)
        {
            using var response =
                await _httpClient.GetAsync(
                    $"api/profile/files/{fileResourceId:D}");

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return null;
            }

            return await ReadRequiredAsync<FileResourceDto>(
                response,
                "Unable to load file information.");
        }

        public async Task<UserDto>
            UpdateDisplayNameAsync(
                Guid userId,
                string displayName)
        {
            using var response =
                await _httpClient.PutAsJsonAsync(
                    $"api/profile/{userId:D}/display-name",
                    new
                    {
                        DisplayName = displayName
                    });

            return await ReadRequiredAsync<UserDto>(
                response,
                "Unable to update display name.");
        }

        public Task<UserDto> UpdateAvatarAsync(
            Guid userId,
            byte[] fileBytes,
            string fileName,
            string contentType)
        {
            return UploadFileAsync(
                $"api/profile/{userId:D}/avatar",
                fileBytes,
                fileName,
                contentType,
                "Unable to update avatar.");
        }

        public Task<UserDto> UpdatePortfolioAsync(
            Guid userId,
            byte[] fileBytes,
            string fileName,
            string contentType)
        {
            return UploadFileAsync(
                $"api/profile/{userId:D}/portfolio",
                fileBytes,
                fileName,
                contentType,
                "Unable to update portfolio.");
        }

        private async Task<UserDto> UploadFileAsync(
            string requestUri,
            byte[] fileBytes,
            string fileName,
            string contentType,
            string defaultErrorMessage)
        {
            if (fileBytes == null ||
                fileBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "A valid file is required.");
            }

            var safeFileName =
                string.IsNullOrWhiteSpace(fileName)
                    ? "upload.bin"
                    : fileName;

            using var form =
                new MultipartFormDataContent();

            using var fileContent =
                new ByteArrayContent(fileBytes);

            if (MediaTypeHeaderValue.TryParse(
                    contentType,
                    out var parsedContentType))
            {
                fileContent.Headers.ContentType =
                    parsedContentType;
            }
            else
            {
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/octet-stream");
            }

            form.Add(
                fileContent,
                "file",
                safeFileName);

            using var response =
                await _httpClient.PostAsync(
                    requestUri,
                    form);

            return await ReadRequiredAsync<UserDto>(
                response,
                defaultErrorMessage);
        }

        private async Task<T> ReadRequiredAsync<T>(
            HttpResponseMessage response,
            string defaultErrorMessage)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message =
                    await ExtractErrorMessageAsync(
                        response,
                        defaultErrorMessage);

                _logger.LogWarning(
                    "Profile API failed: {StatusCode} {ReasonPhrase}",
                    (int)response.StatusCode,
                    response.ReasonPhrase);

                throw new InvalidOperationException(
                    message);
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<T>(
                        JsonOptions);

            return result ??
                throw new InvalidOperationException(
                    "The Profile API returned an empty response.");
        }

        private static async Task<string>
            ExtractErrorMessageAsync(
                HttpResponseMessage response,
                string defaultMessage)
        {
            try
            {
                var body =
                    await response.Content
                        .ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return defaultMessage;
                }

                using var document =
                    JsonDocument.Parse(body);

                var root =
                    document.RootElement;

                foreach (var propertyName in
                    new[] { "message", "detail", "title" })
                {
                    if (root.TryGetProperty(
                            propertyName,
                            out var property) &&
                        property.ValueKind ==
                            JsonValueKind.String)
                    {
                        var value =
                            property.GetString();

                        if (!string.IsNullOrWhiteSpace(
                                value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Use the safe fallback message.
            }

            return defaultMessage;
        }
    }
}
