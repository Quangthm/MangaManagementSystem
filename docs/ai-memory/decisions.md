# Decisions

- Cloudinary stores the actual media files; SQL Server stores file metadata and references in `manga.FileResource`.
- `FileResource` is the system-level file metadata anchor; business records should reference it instead of raw Cloudinary URLs.
- `ChapterPage` is a logical page slot; `ChapterPageVersion` stores uploaded or revised page files.
- Accepted AI/manual regions are stored directly as `PageRegion` and belong to exactly one `ChapterPageVersion`.
- Page annotations link to `PageRegion`, not directly to coordinates.
- Board workflow uses `SeriesBoardPoll` and `SeriesBoardVote`; there is no separate `SeriesBoardDecision` table.
- Editorial Board Chief owns board poll open/close/cancel actions and sets publication frequency for `START_SERIALIZATION` polls.
- Board-approved publication frequency overrides the mangaka preference and becomes the official `Series.publication_frequency_code`.
- CORRECTED 2026-06-09: the database uses integer IDs, not GUID. Verified against `MangaManagementSystem_Schema.sql` (zero `uniqueidentifier`/`NEWID`) and `Application/Interfaces/*`. Types are `role_id SMALLINT`, `user_id INT`, `series_id BIGINT`. The earlier "GUID migration" note was inaccurate; the migration was never applied.
- Service signatures are authoritative: `int` for users (`IUserService`), `long` for series (`ISeriesService`). Match these when editing backend/route/DTO types.
- Clean Architecture boundaries should remain intact; avoid putting workflow logic directly in UI or infrastructure persistence code.
