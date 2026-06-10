# Project Memory

## Read First

- This is a lightweight memory file for OpenCode sessions. Do not treat it as a replacement for source docs.
- Primary docs inspected for this summary: `README.md`, `docs/context.md`, `docs/business-rules.md`, `docs/functional-requirements.md`, `docs/user-stories.md`, and `docs/Skills/AI_AGENT_SKILLS_GUIDE.md`.

## Tech Stack

- .NET 8 solution with Blazor Web app, ASP.NET Core Web API, Application, Domain, and Infrastructure projects.
- UI uses MudBlazor; page-region/canvas work may use Fabric.js.
- Backend uses ASP.NET Core, Clean Architecture-style layers, EF Core, SQL Server, and Infrastructure repositories/adapters.
- SQL Server stores workflow metadata/audit data; Cloudinary stores actual uploaded media.
- Optional AI Hub is a local/internal Python FastAPI service for YOLO-style page region detection; AI output is advisory only.

## Architecture

- Clean Architecture boundary: Domain contains entities/core rules; Application contains use cases/DTOs/service interfaces; Infrastructure contains EF Core, SQL Server, Cloudinary, external adapters; API/Web expose HTTP/UI concerns.
- Business logic should not be placed directly in UI components or database-specific classes when it belongs in Domain/Application.
- Important database workflows should use SQL Server stored procedures or wrapper procedures when they include multi-table writes, status transitions, approvals, account/security actions, FileResource linking, audit logging, or concurrency-sensitive updates.
- EF Core is acceptable for simple reads, filters, dashboards, and uncomplicated single-table operations.
- Cloudinary operations happen outside SQL transactions: validate/upload in C#, call SQL workflow, then attempt Cloudinary cleanup if SQL fails.

## Database Convention

- Database: SQL Server / `MangaManagementDB`.
- Schemas: `auth`, `manga`, and `audit`.
- Table names are PascalCase; column names are lower snake_case; code values are `SCREAMING_SNAKE_CASE`.
- VERIFIED 2026-06-09: IDs are integer, NOT GUID. `MangaManagementSystem_Schema.sql` has zero `uniqueidentifier`/`NEWID`. Types: `role_id SMALLINT`, `user_id INT`, `series_id BIGINT` (all IDENTITY). Any prior GUID migration claim was not applied.
- Service interfaces match: `IUserService` uses `int`; `ISeriesService` uses `long`. The split reflects `INT` users vs `BIGINT` series; treat it as intentional.
- When fixing backend/database mismatches, check the model, DTO, repository, stored procedure, `SqlParameter`, EF mapping, and route parameter types together against the integer schema.
- Do not introduce `Guid`/`uniqueidentifier` IDs unless the team explicitly requests it and migrates the schema first.
- Do not change schema unless explicitly asked.

## Current Business Rules

- MVP is a manga production workflow system, not a public reader, drawing tool, e-commerce, payroll, or monetization platform.
- Users have one MVP role; new users start as `PENDING_APPROVAL`; Admin activates, rejects, or disables accounts.
- `display_name` is user-facing and not unique; it must not replace `username` for login identity.
- Series teams are managed through `SeriesContributor`, not a direct lead Mangaka field on `Series`.
- Proposals are stored as `SeriesProposal`; revisions create new rows; `APPROVED` means board approval, not only editorial approval.
- Chapter submission is a status change to `UNDER_REVIEW`; do not add a `ChapterSubmission` table.
- Final chapter decisions live in `ChapterEditorialReview`; page annotations support review but do not replace chapter-level decisions.
- Use current status plus domain records and audit logs; avoid generic status-history tables unless explicitly requested.

## Cloudinary and FileResource Rules

- Cloudinary stores actual media; SQL Server stores metadata and references in `manga.FileResource`.
- Business records should reference `FileResource` IDs, not raw Cloudinary URLs, when a file relationship exists.
- Files include avatars, portfolios, series covers, proposal files, chapter page-version files, editorial markup/attachments, and task reference files.
- Every `FileResource` must store backend-calculated `sha256_hash` from exact uploaded bytes.
- File deletion should happen through the application workflow; deleted files are excluded from normal queries unless viewing historical/audit data.
- Files used as chapter page content should use `file_purpose_code = CHAPTER_PAGE_VERSION`; task-only reference files use `TASK_REFERENCE`.

## Page, AI, and Annotation Rules

- `ChapterPage` is a logical page slot; `ChapterPageVersion` stores uploaded/revised files.
- Page revisions create new `ChapterPageVersion` rows; do not overwrite previous page files.
- `PageRegion` belongs to exactly one `ChapterPageVersion`; accepted AI/manual regions are stored directly as `PageRegion`.
- Region coordinates are rectangular `x`, `y`, `width`, `height`; width/height must be positive; source is `AI` or `MANUAL`.
- AI/OCR/translation suggestions are advisory and human-reviewed; AI must not auto-approve, reject, publish, cancel, serialize, or finalize workflows.
- `PageRegion.original_text` stores detected original text, not final translated text.
- Annotations link to `PageRegion`, not direct annotation coordinates; whole-page feedback uses a manually created full-page region.

## Board Chief and Publication Frequency Rules

- Use `SeriesBoardPoll` and `SeriesBoardVote`; do not add a `SeriesBoardDecision` table.
- Editorial Board Chief opens, closes, and cancels board polls for `START_SERIALIZATION` and `CANCEL_SERIALIZATION`.
- Editorial Board Chief must specify publication frequency when opening a `START_SERIALIZATION` poll.
- Board Members and Board Chiefs may vote `APPROVE`, `REJECT`, or `ABSTAIN` at most once per open poll; rejection requires a reason.
- Board results are computed from votes. Only closed polls can produce applicable results; cancelled polls remain traceable but do not affect status.
- Approved `START_SERIALIZATION` applies the board-specified official `Series.publication_frequency_code`.
- Mangaka may provide/update desired publication frequency only while series is `PROPOSAL_DRAFT`; after board decision, they request changes via in-app notification.
- Only Editorial Board Chief may directly change official publication frequency after board decision, with a required audit reason.

## Current Known Issues / Risks

- Existing git working tree had unrelated modified app source files before this memory setup: `RegisterPage.razor` and `WorkspaceCard.razor`.
- Some SQL scripts and repository code still show old integer ID patterns (`@new_user_id INT`, `SqlDbType.Int`, `role_id`/`user_id` usage). Treat these as migration-risk indicators, not facts about the current target schema.
- `opencode.json`, `AGENTS.md`, and `.opencode/` were absent before this setup in the detected repo.
- Avoid broad scans of source code; use grep/ripgrep first and open only targeted files.
