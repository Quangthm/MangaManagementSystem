using System.Net.Http.Json;
using System.Net;
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

            var requestMethod =
                response.RequestMessage?.Method?.Method ?? "UNKNOWN";

            var requestUri =
                response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN";

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                message = "Your session is no longer valid. Please sign in again.";
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                message = "You don't have permission to perform this action.";
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                message = "The requested resource could not be found.";
            }
            else if ((int)response.StatusCode >= 500)
            {
                message = "The request could not be completed right now. Please try again.";
            }

            if ((int)response.StatusCode < 500
                && response.StatusCode != HttpStatusCode.Unauthorized)
            {
                try
                {
                    var body =
                        await response.Content
                            .ReadAsStringAsync(
                                cancellationToken);

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var trimmedBody = body.Trim();
                        var looksLikeJson =
                            trimmedBody.StartsWith('{')
                            || trimmedBody.StartsWith('[')
                            || trimmedBody.StartsWith('"');

                        if (looksLikeJson
                            && TryParseStructuredError(
                                body,
                                out var parsedCode,
                                out var parsedMessage))
                        {
                            if (!string.IsNullOrWhiteSpace(parsedCode))
                            {
                                code = parsedCode;
                            }

                            if (!string.IsNullOrWhiteSpace(parsedMessage))
                            {
                                message = parsedMessage;
                            }
                        }
                        else if (!looksLikeJson
                            && string.Equals(
                                response.Content.Headers.ContentType?.MediaType,
                                "text/plain",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            var plainMessage = trimmedBody;
                            if (!string.IsNullOrWhiteSpace(plainMessage))
                            {
                                message = plainMessage;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Use the status-aware safe default.
                }
            }

            return new ApiClientException(
                code,
                message,
                response.StatusCode,
                requestMethod,
                requestUri);
        }

        private static bool TryParseStructuredError(
            string body,
            out string? code,
            out string? message)
        {
            code = null;
            message = null;

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                message = root.GetString();
                return !string.IsNullOrWhiteSpace(message);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (TryReadString(root, "code", out var parsedCode))
            {
                code = parsedCode;
            }

            if (TryReadString(root, "message", out var parsedMessage)
                || TryReadString(root, "detail", out parsedMessage)
                || TryReadString(root, "title", out parsedMessage))
            {
                message = parsedMessage;
                return true;
            }

            if (!root.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Object)
            {
                return code is not null;
            }

            foreach (var error in errors.EnumerateObject())
            {
                if (error.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in error.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var validationMessage = item.GetString();
                    if (string.IsNullOrWhiteSpace(validationMessage))
                    {
                        continue;
                    }

                    message = validationMessage;
                    code = AuthErrorCodes.ValidationFailed;
                    return true;
                }
            }

            return code is not null;
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
