# PageRegion Foundation — FULL_PAGE Type & Cloudinary Image Bounds

## Branch

`feature/Mangaka`

## Date

2026-06-25

## Task summary

Foundation-only changes to support the upcoming Quick Select Task Assignment (Phase 3). Updated PageRegion constraints to support dot regions (`width=0, height=0`), area regions (`width>0, height>0`), and a new `FULL_PAGE` type. Added `IImageMetadataProvider` for later Cloudinary dimension lookup.

No Quick Select UI, task creation, batch procedures, or `usp_ChapterPageTask_Create` calls were implemented.

## Architecture path

```text
Application: IImageMetadataProvider interface
  -> Infrastructure: CloudinaryImageMetadataProvider (Cloudinary GetResource)
```

EF configuration only: `PageRegionConfiguration.cs` updated.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| **Infrastructure** | `Persistence/Configurations/PageRegionConfiguration.cs` | Added `FULL_PAGE` to type_code check; replaced `CK_PageRegion_Width_Height` with `CK_PageRegion_Coordinates_NonNegative`, `CK_PageRegion_Dimensions`, `CK_PageRegion_FullPageShape`; added `HasDefaultValue(0m)` to Width/Height |
| **Application** | `Interfaces/IImageMetadataProvider.cs` | New: interface + `ImageBoundsDto` record |
| **Infrastructure** | `Services/CloudinaryImageMetadataProvider.cs` | New: Cloudinary GetResource implementation |
| **Infrastructure** | `DependencyInjection.cs` | Registered `IImageMetadataProvider` -> `CloudinaryImageMetadataProvider` |
| **Docs** | `business-rules.md` | Updated BR-REG-002 (added FULL_PAGE), BR-REG-003 (pixel-based coords), BR-REG-004 (dot/area semantics); added BR-REG-029 to BR-REG-032 |

## DB/SP impact

**Manual SQL/schema changes needed** (not applied, not a migration):

```sql
-- Drop old constraint
ALTER TABLE manga.PageRegion DROP CONSTRAINT IF EXISTS CK_PageRegion_Width_Height;

-- Drop old type check, add new one with FULL_PAGE
ALTER TABLE manga.PageRegion DROP CONSTRAINT IF EXISTS CK_PageRegion_TypeCode;
ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_TypeCode
    CHECK (type_code IN (N'PANEL', N'SPEECH_BUBBLE', N'CHARACTER', N'SFX_TEXT', N'BACKGROUND', N'FULL_PAGE', N'OTHER'));

-- Add new constraints
ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_Coordinates_NonNegative
    CHECK (x >= 0 AND y >= 0);

ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_Dimensions
    CHECK ((width = 0 AND height = 0) OR (width > 0 AND height > 0));

ALTER TABLE manga.PageRegion ADD CONSTRAINT CK_PageRegion_FullPageShape
    CHECK (type_code <> N'FULL_PAGE' OR (x = 0 AND y = 0 AND width > 0 AND height > 0));

-- Set default values
ALTER TABLE manga.PageRegion ADD DEFAULT 0 FOR width;
ALTER TABLE manga.PageRegion ADD DEFAULT 0 FOR height;
```

The project does not auto-apply migrations. These must be run manually against the target database before `FULL_PAGE` regions can be created.

## PageRegion geometry decisions

| State | width | height | Meaning |
|-------|-------|--------|---------|
| DOT/point region | 0 | 0 | Represents a single point |
| Area/rectangle region | > 0 | > 0 | Standard bounding box |
| **Invalid** | > 0 | = 0 | Rejected by constraint |
| **Invalid** | = 0 | > 0 | Rejected by constraint |
| FULL_PAGE | > 0 | > 0 | x=0, y=0, width/height from Cloudinary |

Coordinates are pixel-based relative to the original page image dimensions. `decimal(10,2)` precision.

## FULL_PAGE type_code decision

`FULL_PAGE` added as a valid `PageRegion.type_code`. Whole-page regions use:
- `type_code = FULL_PAGE`
- `region_label = "Full page"`
- `x = 0`, `y = 0`
- `width` and `height` = Cloudinary image dimensions
- `source_type = MANUAL`
- `confidence_score = NULL`

## Cloudinary dimension source findings

1. **During upload:** `ImageUploadResult` (returned by `_cloudinary.UploadAsync(ImageUploadParams)`) exposes `Width` (int) and `Height` (int). Currently discarded in `CloudinaryFileStorageService.UploadToCloudinaryAsync`.
2. **For existing files:** `Cloudinary.GetResourceAsync(GetResourceParams)` returns `GetResourceResult` with `Width`/`Height`.
3. `FileResource.CloudinaryPublicId` stores the unique Cloudinary identifier, enabling later metadata lookups.
4. `IImageMetadataProvider` / `CloudinaryImageMetadataProvider` wraps `GetResourceAsync` and returns `ImageBoundsDto(Width, Height)` or `null` on failure.

## IImageMetadataProvider

Added as Application-facing abstraction with Infrastructure implementation. Design:
- Returns `ImageBoundsDto?` — null if dimensions cannot be resolved
- Does not throw raw Cloudinary exceptions
- Does not hold database transactions
- Quick Select backend will call this before opening the DB transaction

## Build result

```text
Not yet run
```

## Known issues

- Manual SQL changes must be applied to the target database before FULL_PAGE regions can be inserted.
- Existing `PageRegion` rows created before this change will still satisfy new constraints because they already had `width > 0 AND height > 0` (old constraint) and existing type codes are still valid.
- `Cloudinary.GetResourceAsync` requires a valid Cloudinary API key with read permission. This should already be configured.

## Next step for Quick Select backend

1. Apply the manual SQL changes above.
2. Implement Quick Select read endpoints (chapters, pages, assistants).
3. Implement `POST /api/mangaka/tasks/quick-select` with Application validation + EF batch insert.
4. Use `IImageMetadataProvider` to resolve page image dimensions before creating whole-page PageRegion rows.
5. Do not call `manga.usp_ChapterPageTask_Create`. Use EF batch insert with one `SaveChangesAsync`.
