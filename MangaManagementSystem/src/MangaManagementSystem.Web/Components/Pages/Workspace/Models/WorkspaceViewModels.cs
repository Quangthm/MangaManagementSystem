
namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    // UI view-models for the Mangaka authorized chapter workspace (CreatorWorkspace). These hold
    // the in-memory/manual-save state of the editor (buffered pages/versions, dirty flags, canvas
    // region state, pending-upload bytes) and are Web concerns only — they are NOT domain entities
    // or Application DTOs. Extracted out of CreatorWorkspace.razor to keep that component focused on
    // markup + behaviour.

    /// <summary>A production task shown in the Task Panel (assistant assignment on a page version).</summary>
    public class ProductionTask
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public string Assistant { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Assigned";
        public DateTime? DueAtUtc { get; set; }   // manga.ChapterPageTask.due_at_utc (task deadline)
        public Guid? VersionId { get; set; }   // version this task belongs to (via its regions)
        public List<RegionModel> Regions { get; set; } = new();
    }

    /// <summary>A page annotation (editorial feedback) shown in the Annotations panel / canvas pins.</summary>
    public class AnnotationModel
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string Type { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Target { get; set; } = "";
        public string Author { get; set; } = "";
        public string? CreatedByRoleName { get; set; }   // creator's role, for BR-ANN-021 resolve gating
        public int PageNumber { get; set; } = 1;
        public bool IsResolved { get; set; } = false;
        public double? PinX { get; set; }
        public double? PinY { get; set; }
        public Guid? VersionId { get; set; }   // version this annotation belongs to (via its regions)
        public List<RegionModel> Regions { get; set; } = new();
    }

    /// <summary>A chapter row in the left sidebar (buffered until saved to the DB).</summary>
    public class ChapterModel
    {
        public int Id { get; set; }                     // internal UI selection key (unique per load), NOT the displayed number
        public string NumberLabel { get; set; } = "";   // chapter_number_label as chosen by the user (supports "2.5"); shown in the UI
        public Guid ChapterId { get; set; }
        public int PageCount { get; set; }
        public bool IsCompleted { get; set; }
        public string StatusCode { get; set; } = "DRAFT";
        public string Title { get; set; } = "";
        public bool IsRenaming { get; set; } = false;
        public bool TitleDirty { get; set; } = false;   // buffered rename: title changed but not yet saved to DB
        public List<PageModel> Pages { get; set; } = new();
        public bool IsPagesLoaded { get; set; } = false;
        public bool IsPending => ChapterId == Guid.Empty;       // created in buffer, not yet in the DB
    }

    /// <summary>One version of a logical page (manual-save buffer for freshly uploaded/edited images).</summary>
    public class PageVersionModel
    {
        public int VersionNo { get; set; }
        public string DataUrl { get; set; } = "";
        public string Note { get; set; } = "";
        public string? Regions { get; set; } = null;
        public bool IsDirty { get; set; } = false;
        public Guid ChapterPageVersionId { get; set; }
        public bool IsCurrentVersion { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // MANUAL-SAVE buffer: a freshly uploaded page is held in memory (raw bytes + a base64 data
        // URL for display) and only pushed to Cloudinary + DB when the user clicks Save. The id stays
        // Guid.Empty until then. FlushPendingAsync() consumes these and swaps in the real ids/URL.
        public byte[]? PendingBytes { get; set; }
        public string? PendingFileName { get; set; }
        public string? PendingContentType { get; set; }
        public bool IsPending => ChapterPageVersionId == Guid.Empty;
    }

    /// <summary>A logical page (a page slot with one or more versions).</summary>
    public class PageModel
    {
        public Guid ChapterPageId { get; set; }
        public int PageNo { get; set; }                         // manga.ChapterPage.page_number (true DB number; may have gaps after soft-deletes)
        public string? PageNotes { get; set; }                  // manga.ChapterPage.page_notes
        public List<PageVersionModel> Versions { get; set; } = new();
        public int ActiveVersionIndex { get; set; } = 0;
        public bool IsPending => ChapterPageId == Guid.Empty;   // not yet persisted to the DB
    }

    /// <summary>A canvas region (panel / bubble / SFX / full-page) with its normalized-to-image box.</summary>
    public class RegionModel
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string? Label { get; set; }
        public string Type { get; set; } = "panel";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Selected { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
        // Provenance, carried through the canvas round-trip so a bulk save does not rewrite an
        // AI-detected region into a MANUAL one (and drop its confidence). A box drawn by hand on
        // the canvas leaves these null and is persisted as MANUAL. See manga.PageRegion
        // ck_page_region_confidence_source: AI requires a score, MANUAL requires none.
        public string? SourceType { get; set; }
        public double? ConfidenceScore { get; set; }
    }
}
