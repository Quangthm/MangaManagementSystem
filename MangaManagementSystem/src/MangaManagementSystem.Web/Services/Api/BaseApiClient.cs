using System.Text.Json;

namespace MangaManagementSystem.Web.Services.Api;

public abstract class BaseApiClient
{
    protected static async Task<string> ExtractErrorMessageAsync(
        HttpResponseMessage response,
        string defaultMessage = "An unexpected error occurred. Please try again.",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
                return defaultMessage;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
            {
                var msg = msgProp.GetString();
                if (!string.IsNullOrWhiteSpace(msg))
                    return msg;
            }

            if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
            {
                var detail = detailProp.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                    return detail;
            }

            if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                var title = titleProp.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
        }
        catch (JsonException)
        {
        }

        return defaultMessage;
    }
}
