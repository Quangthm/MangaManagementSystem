
namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    /// <summary>
    /// Pure (stateless) helper functions for the Mangaka workspace. Extracted from CreatorWorkspace
    /// so they can be unit-reasoned in isolation and to slim the component. The component brings these
    /// into scope via <c>@using static ...WorkspaceHelpers</c>, so call sites stay unqualified.
    /// </summary>
    public static class WorkspaceHelpers
    {
        /// <summary>
        /// #5: builds region DTOs from a stored regions JSON string, used when the deferred "Upload
        /// Version" flush creates the version. Mirrors the segmentation mapping and excludes tiny pins.
        /// </summary>
        public static List<CreatePageRegionDto> BuildRegionDtosForSave(string? regionsJson)
        {
            var empty = new List<CreatePageRegionDto>();
            if (string.IsNullOrWhiteSpace(regionsJson)) return empty;

            List<RegionModel>? regions;
            try
            {
                var opts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                regions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(regionsJson, opts);
            }
            catch { return empty; }
            if (regions == null) return empty;

            return regions
                .Where(r => r.Width > 0.05 || r.Height > 0.05) // exclude tiny pin markers
                .Select(r =>
                {
                    var textObj = new Dictionary<string, string>
                    {
                        { "original", r.OriginalText ?? "" },
                        { "translated", r.TranslatedText ?? "" }
                    };
                    return new CreatePageRegionDto(
                        ChapterPageVersionId: Guid.Empty, // assigned inside the service
                        TypeCode: (r.Type ?? "OTHER").ToUpperInvariant(),
                        RegionLabel: r.Label,
                        X: (decimal)r.X,
                        Y: (decimal)r.Y,
                        Width: (decimal)r.Width,
                        Height: (decimal)r.Height,
                        ConfidenceScore: null,
                        SourceType: "MANUAL",
                        OriginalText: System.Text.Json.JsonSerializer.Serialize(textObj));
                })
                .ToList();
        }

        /// <summary>
        /// Removes the transient "selected" flag from a regions JSON string so a mere selection change
        /// is not treated as an unsaved content edit (used to compare current vs saved regions).
        /// </summary>
        public static string StripSelected(string? regionsJson)
        {
            if (string.IsNullOrWhiteSpace(regionsJson)) return "[]";
            try
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(regionsJson);
                if (node is System.Text.Json.Nodes.JsonArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (item is System.Text.Json.Nodes.JsonObject obj)
                        {
                            obj.Remove("selected");
                            obj.Remove("Selected");
                        }
                    }
                    return arr.ToJsonString();
                }
            }
            catch { }
            return regionsJson.Trim();
        }

        /// <summary>
        /// Requests a bandwidth-optimized rendition (auto format + auto quality) from Cloudinary WITHOUT
        /// any resize, so pixel dimensions — and therefore region coordinates — stay identical. Returns
        /// the original URL unchanged if it is not a recognizable Cloudinary delivery URL or is already
        /// transformed.
        /// </summary>
        public static string OptimizedImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url ?? "";
            const string marker = "/upload/";
            int idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return url;
            int insertAt = idx + marker.Length;
            if (url.AsSpan(insertAt).StartsWith("f_auto")) return url;
            // f_auto,q_auto = format (WebP/AVIF) + quality auto — shrinks the FILE without changing pixel
            // dimensions. Do NOT add a resize (w_/h_/c_limit) here: fitImageOntoCanvas sets canvas.width =
            // image.width, so the canvas coordinate space == the delivered image's pixel size. Region
            // coordinates are stored in that absolute pixel space, so delivering a smaller image shifts the
            // canvas coords and breaks region hit-testing (click-select) and alignment. Reducing image size
            // further needs the regions made resolution-independent (normalize in JS) first.
            return url.Insert(insertAt, "f_auto,q_auto/");
        }

        /// <summary>
        /// Maps legacy/canvas type values onto the DB type codes (ck_page_region_type_code). FULL_PAGE is
        /// system-managed (needs x=0,y=0) so it is not offered for manual edit; anything unknown → OTHER.
        /// </summary>
        public static string NormalizeRegionType(string? type)
        {
            var t = (type ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "PANEL" => "PANEL",
                "SPEECH_BUBBLE" or "BUBBLE" or "SPEECH" => "SPEECH_BUBBLE",
                "CHARACTER" => "CHARACTER",
                "SFX" or "SFX_TEXT" => "SFX_TEXT",
                "BACKGROUND" => "BACKGROUND",
                "FULL_PAGE" => "FULL_PAGE",
                _ => "OTHER"
            };
        }
    }
}
