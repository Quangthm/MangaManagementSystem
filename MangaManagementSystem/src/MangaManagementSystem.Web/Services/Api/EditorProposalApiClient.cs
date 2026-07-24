using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// HttpClient-backed implementation of <see cref="IEditorProposalApiClient"/>. Builds
    /// multipart/form-data for decision actions that
    /// carry a markup file, and parses safe error messages for UI snackbars.
    /// </summary>
    public class EditorProposalApiClient : IEditorProposalApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EditorProposalApiClient> _logger;

        public EditorProposalApiClient(HttpClient httpClient, ILogger<EditorProposalApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProposalQueueItemDto>> GetQueueAsync(
            string? statusCode = null,
            CancellationToken cancellationToken = default)
        {
            var route = "api/editor/proposals";
            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                route += $"?status={Uri.EscapeDataString(statusCode)}";
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var queue = await response.Content.ReadFromJsonAsync<List<ProposalQueueItemDto>>(
                    cancellationToken: cancellationToken);
                return queue ?? new List<ProposalQueueItemDto>();
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load editorial proposal queue failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<EditorProposalDetailDto?> GetDetailAsync(
            Guid proposalId,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, $"api/editor/proposals/{proposalId}");
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EditorProposalDetailDto>(
                    cancellationToken: cancellationToken);
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load proposal detail {ProposalId} failed: {StatusCode} {ReasonPhrase}",
                proposalId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<EditorReviewActionResultDto> ClaimAsync(
            Guid proposalId,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            var payload = new { Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim() };
            using var content = JsonContent.Create(payload);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/proposals/{proposalId}/claims")
            {
                Content = content
            };
            return await SendDecisionAsync(requestMessage, proposalId, "claim", cancellationToken);
        }

        public async Task<EditorReviewActionResultDto> RequestRevisionAsync(
            Guid proposalId,
            string comments,
            byte[]? markupFileBytes = null,
            string? markupFileName = null,
            string? markupContentType = null,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildDecisionForm(comments, markupFileBytes, markupFileName, markupContentType);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/proposals/{proposalId}/revision-requests")
            {
                Content = form
            };
            return await SendDecisionAsync(requestMessage, proposalId, "request revision", cancellationToken);
        }

        public async Task<EditorReviewActionResultDto> PassToBoardAsync(
            Guid proposalId,
            string? comments = null,
            byte[]? markupFileBytes = null,
            string? markupFileName = null,
            string? markupContentType = null,
            CancellationToken cancellationToken = default)
        {
            using var form = BuildDecisionForm(comments, markupFileBytes, markupFileName, markupContentType);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/proposals/{proposalId}/board-submissions")
            {
                Content = form
            };
            return await SendDecisionAsync(requestMessage, proposalId, "pass to board", cancellationToken);
        }

        public async Task<EditorReviewActionResultDto> CancelAsync(
            Guid proposalId,
            string comments,
            byte[] markupFileBytes,
            string markupFileName,
            string markupContentType,
            CancellationToken cancellationToken = default)
        {
            if (markupFileBytes is not { Length: > 0 })
            {
                throw new InvalidOperationException("A markup file is required to cancel a proposal.");
            }

            using var form = BuildDecisionForm(comments, markupFileBytes, markupFileName, markupContentType);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, $"api/editor/proposals/{proposalId}/cancellations")
            {
                Content = form
            };
            return await SendDecisionAsync(requestMessage, proposalId, "cancel", cancellationToken);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static MultipartFormDataContent BuildDecisionForm(
            string? comments,
            byte[]? markupFileBytes,
            string? markupFileName,
            string? markupContentType)
        {
            var form = new MultipartFormDataContent();

            // Part name must match the API form contract property "Comments".
            form.Add(new StringContent(comments ?? string.Empty), "Comments");

            if (markupFileBytes is { Length: > 0 })
            {
                var fileContent = new ByteArrayContent(markupFileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(markupContentType) ? "application/octet-stream" : markupContentType);

                // Part name must match the API form contract property "MarkupFile".
                form.Add(fileContent, "MarkupFile",
                    string.IsNullOrWhiteSpace(markupFileName) ? "markup" : markupFileName);
            }

            return form;
        }

        private async Task<EditorReviewActionResultDto> SendDecisionAsync(
            HttpRequestMessage requestMessage,
            Guid proposalId,
            string actionLabel,
            CancellationToken cancellationToken)
        {
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EditorReviewActionResultDto>(
                    cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The action completed but no confirmation was returned. Please refresh and verify.");
                }

                return result;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Editorial decision ({Action}) failed for proposal {ProposalId}: {StatusCode} {ReasonPhrase}",
                actionLabel, proposalId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return "An unexpected error occurred. Please try again.";
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // ApiErrorResponse: { "message": "..." }
                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg)) return msg;
                }

                // ProblemDetails: { "detail": "...", "title": "..." }
                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail)) return detail;
                }

                // ValidationProblemDetails: { "errors": { field: [msg, ...] } }
                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var error in errorsProp.EnumerateObject())
                    {
                        if (error.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var msg in error.Value.EnumerateArray())
                            {
                                if (msg.ValueKind == JsonValueKind.String)
                                {
                                    var errMsg = msg.GetString();
                                    if (!string.IsNullOrWhiteSpace(errMsg)) return errMsg;
                                }
                            }
                        }
                    }
                }

                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title)) return title;
                }
            }
            catch (JsonException)
            {
                // Not valid JSON — fall through.
            }

            return "An unexpected error occurred. Please try again.";
        }
    }
}
