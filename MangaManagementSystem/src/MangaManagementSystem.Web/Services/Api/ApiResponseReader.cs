using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api
{
    internal static class ApiResponseReader
    {
        private static readonly JsonSerializerOptions
            JsonOptions =
                new(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true
                };

        public static async Task<T> ReadRequiredAsync<T>(
            HttpResponseMessage response,
            string emptyResponseMessage,
            CancellationToken cancellationToken = default)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateExceptionAsync(
                    response,
                    cancellationToken);
            }

            var value =
                await response.Content
                    .ReadFromJsonAsync<T>(
                        JsonOptions,
                        cancellationToken);

            return value
                ?? throw new InvalidOperationException(
                    emptyResponseMessage);
        }

        public static async Task<ApiClientException>
            CreateExceptionAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken = default)
        {
            var code =
                AuthErrorCodes.RequestFailed;

            var message =
                "The request could not be completed.";

            try
            {
                var body =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var document =
                        JsonDocument.Parse(body);

                    var root =
                        document.RootElement;

                    if (TryReadString(
                            root,
                            "code",
                            out var parsedCode))
                    {
                        code = parsedCode;
                    }

                    if (TryReadString(
                            root,
                            "message",
                            out var parsedMessage)
                        || TryReadString(
                            root,
                            "detail",
                            out parsedMessage)
                        || TryReadString(
                            root,
                            "title",
                            out parsedMessage))
                    {
                        message = parsedMessage;
                    }
                    else if (root.TryGetProperty(
                            "errors",
                            out var errors)
                        && errors.ValueKind ==
                            JsonValueKind.Object)
                    {
                        foreach (var error
                            in errors.EnumerateObject())
                        {
                            if (error.Value.ValueKind
                                != JsonValueKind.Array)
                            {
                                continue;
                            }

                            foreach (var item
                                in error.Value
                                    .EnumerateArray())
                            {
                                if (item.ValueKind
                                    != JsonValueKind.String)
                                {
                                    continue;
                                }

                                var validationMessage =
                                    item.GetString();

                                if (!string.IsNullOrWhiteSpace(
                                        validationMessage))
                                {
                                    message =
                                        validationMessage;

                                    code =
                                        AuthErrorCodes
                                            .ValidationFailed;

                                    break;
                                }
                            }

                            if (code ==
                                AuthErrorCodes
                                    .ValidationFailed)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Use safe defaults.
            }

            return new ApiClientException(
                code,
                message,
                response.StatusCode);
        }

        private static bool TryReadString(
            JsonElement root,
            string propertyName,
            out string value)
        {
            value = string.Empty;

            if (!root.TryGetProperty(
                    propertyName,
                    out var property)
                || property.ValueKind
                    != JsonValueKind.String)
            {
                return false;
            }

            var parsed =
                property.GetString();

            if (string.IsNullOrWhiteSpace(parsed))
            {
                return false;
            }

            value = parsed;
            return true;
        }
    }
}
