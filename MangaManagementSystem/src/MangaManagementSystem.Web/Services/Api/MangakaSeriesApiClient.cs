using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class MangakaSeriesApiClient : IMangakaSeriesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MangakaSeriesApiClient> _logger;

        public MangakaSeriesApiClient(HttpClient httpClient, ILogger<MangakaSeriesApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SeriesDto>> GetMySeriesAsync(
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/mangaka/series/my-series");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var series = await response.Content.ReadFromJsonAsync<List<SeriesDto>>(
                    cancellationToken: cancellationToken);

                return series ?? new List<SeriesDto>();
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load my series failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<SeriesDraftCreatedDto> CreateDraftAsync(
            string title,
            string synopsis,
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
            string? contentLanguageCode = null,
            string? slug = null,
            string? publicationFrequencyCode = null,
            Guid? sourceSeriesId = null,
            byte[]? coverFileBytes = null,
            string? coverFileName = null,
            string? coverContentType = null,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(title), "Title");
            form.Add(new StringContent(synopsis), "Synopsis");

            foreach (var genreId in genreIds ?? Array.Empty<Guid>())
            {
                if (genreId != Guid.Empty)
                    form.Add(new StringContent(genreId.ToString()), "GenreIds");
            }

            foreach (var tagId in tagIds ?? Array.Empty<Guid>())
            {
                if (tagId != Guid.Empty)
                    form.Add(new StringContent(tagId.ToString()), "TagIds");
            }

            if (!string.IsNullOrWhiteSpace(contentLanguageCode))
            {
                form.Add(new StringContent(contentLanguageCode), "ContentLanguageCode");
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                form.Add(new StringContent(slug), "Slug");
            }

            if (!string.IsNullOrWhiteSpace(publicationFrequencyCode))
            {
                form.Add(new StringContent(publicationFrequencyCode), "PublicationFrequencyCode");
            }

            if (sourceSeriesId.HasValue && sourceSeriesId.Value != Guid.Empty)
            {
                form.Add(new StringContent(sourceSeriesId.Value.ToString()), "SourceSeriesId");
            }

            if (coverFileBytes is { Length: > 0 })
            {
                var fileContent = new ByteArrayContent(coverFileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    coverContentType ?? "application/octet-stream");
                form.Add(fileContent, "CoverFile", coverFileName ?? "cover");
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/mangaka/series/drafts")
            {
                Content = form
            };

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<SeriesDraftCreatedDto>(
                    cancellationToken: cancellationToken);
                return created!;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Create series draft failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<SeriesProposalSubmittedDto> SubmitProposalAsync(
            Guid seriesId,
            byte[] proposalFileBytes,
            string proposalFileName,
            string proposalContentType,
            CancellationToken cancellationToken = default)
        {
            if (proposalFileBytes is not { Length: > 0 })
            {
                throw new InvalidOperationException(
                    "A proposal document file is required to submit a series proposal.");
            }

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(proposalFileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(proposalContentType) ? "application/octet-stream" : proposalContentType);

            // Form part name must match the API SubmitSeriesProposalForm.ProposalFile property.
            form.Add(fileContent, "ProposalFile", string.IsNullOrWhiteSpace(proposalFileName) ? "proposal" : proposalFileName);

            var route = $"api/mangaka/series/{seriesId}/proposal-submissions";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = form
            };

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var submitted = await response.Content.ReadFromJsonAsync<SeriesProposalSubmittedDto>(
                    cancellationToken: cancellationToken);

                if (submitted is null)
                {
                    throw new InvalidOperationException(
                        "The proposal was submitted but no confirmation was returned. Please refresh and verify.");
                }

                return submitted;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Submit series proposal failed for series {SeriesId}: {StatusCode} {ReasonPhrase}",
                seriesId,
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<SeriesDraftUpdatedDto> UpdateDraftAsync(
            Guid seriesId,
            string title,
            string synopsis,
            IReadOnlyList<Guid> genreIds,
            IReadOnlyList<Guid> tagIds,
            string contentLanguageCode,
            string? publicationFrequencyCode = null,
            string? slug = null,
            byte[]? coverFileBytes = null,
            string? coverFileName = null,
            string? coverContentType = null,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(title), "Title");
            form.Add(new StringContent(synopsis), "Synopsis");

            foreach (var genreId in genreIds ?? Array.Empty<Guid>())
            {
                if (genreId != Guid.Empty)
                    form.Add(new StringContent(genreId.ToString()), "GenreIds");
            }

            foreach (var tagId in tagIds ?? Array.Empty<Guid>())
            {
                if (tagId != Guid.Empty)
                    form.Add(new StringContent(tagId.ToString()), "TagIds");
            }
            form.Add(new StringContent(contentLanguageCode), "ContentLanguageCode");

            if (!string.IsNullOrWhiteSpace(publicationFrequencyCode))
            {
                form.Add(new StringContent(publicationFrequencyCode), "PublicationFrequencyCode");
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                form.Add(new StringContent(slug), "Slug");
            }

            if (coverFileBytes is { Length: > 0 })
            {
                var fileContent = new ByteArrayContent(coverFileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    coverContentType ?? "application/octet-stream");
                form.Add(fileContent, "CoverFile", coverFileName ?? "cover");
            }

            var route = $"api/mangaka/series/{seriesId}/draft-profile";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Put, route)
            {
                Content = form
            };

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<SeriesDraftUpdatedDto>(
                    cancellationToken: cancellationToken);

                if (updated is null)
                {
                    throw new InvalidOperationException(
                        "The draft was updated but no confirmation was returned. Please refresh to see the latest data.");
                }

                return updated;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Update series draft profile failed for series {SeriesId}: {StatusCode} {ReasonPhrase}",
                seriesId,
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<SeriesDraftCancelledDto> CancelDraftAsync(
            Guid seriesId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var route = $"api/mangaka/series/{seriesId}/draft-cancellations";

            // Serialize the optional reason as a JSON body.
            // If reason is null/empty, send an empty object so the endpoint binds cleanly.
            var payload = new { Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim() };
            using var content = JsonContent.Create(payload);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var cancelled = await response.Content.ReadFromJsonAsync<SeriesDraftCancelledDto>(
                    cancellationToken: cancellationToken);

                if (cancelled is null)
                {
                    throw new InvalidOperationException(
                        "The draft was cancelled but no confirmation was returned. Please refresh to verify.");
                }

                return cancelled;
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Cancel series draft failed for series {SeriesId}: {StatusCode} {ReasonPhrase}",
                seriesId,
                (int)response.StatusCode,
                response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<IReadOnlyList<MangakaSeriesProposalDto>> GetMySeriesProposalsAsync(
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/mangaka/series/proposals");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var proposals = await response.Content.ReadFromJsonAsync<List<MangakaSeriesProposalDto>>(
                    cancellationToken: cancellationToken);

                return proposals ?? new List<MangakaSeriesProposalDto>();
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load my series proposals failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException(message);
        }

        public async Task<MangakaSeriesProposalDto?> GetMySeriesProposalDetailAsync(
            Guid proposalId,
            CancellationToken cancellationToken = default)
        {
            var route = $"api/mangaka/series/proposals/{proposalId}";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MangakaSeriesProposalDto>(
                    cancellationToken: cancellationToken);
            }

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load proposal detail {ProposalId} failed: {StatusCode} {ReasonPhrase}",
                proposalId, (int)response.StatusCode, response.ReasonPhrase);
            throw new InvalidOperationException(message);
        }

        public async Task<SeriesDto?> GetMySeriesCardByIdAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            var route = $"api/mangaka/series/{seriesId}/card";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, route);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SeriesDto>(
                    cancellationToken: cancellationToken);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            var message = await ExtractErrorMessageAsync(response);
            _logger.LogWarning(
                "Load series card by id failed for series {SeriesId}: {StatusCode} {ReasonPhrase}",
                seriesId,
                (int)response.StatusCode,
                response.ReasonPhrase);

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
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        return msg;
                    }
                }

                // ProblemDetails / ValidationProblemDetails: { "detail": "...", "title": "...", "errors": { ... } }
                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        return detail;
                    }
                }

                // ValidationProblemDetails errors dictionary — return the first error message.
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
                                    if (!string.IsNullOrWhiteSpace(errMsg))
                                    {
                                        return errMsg;
                                    }
                                }
                            }
                        }
                    }
                }

                // Fall back to title if nothing else found.
                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return title;
                    }
                }
            }
            catch (JsonException)
            {
                // Response body is not valid JSON — ignore and fall through.
            }

            return "An unexpected error occurred. Please try again.";
        }
    }
}
