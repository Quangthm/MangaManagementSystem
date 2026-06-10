# AGENTS.md

## OpenCode Startup Context

Future OpenCode sessions should read these files first, in order:

1. `docs/ai-memory/session-handoff.md`
2. `docs/ai-memory/project-memory.md`
3. `docs/ai-memory/current-task.md`
4. `docs/ai-memory/decisions.md`
5. `docs/ai-memory/token-rules.md`

## Project Rules

- This is a .NET 8 Blazor + ASP.NET Core Web API + SQL Server + Cloudinary Manga Management System.
- Preserve Clean Architecture boundaries: Domain for core rules, Application for use cases/contracts, Infrastructure for EF Core/SQL/Cloudinary adapters, API/Web for HTTP/UI concerns.
- Keep changes small, targeted, and testable.
- Do not change schema unless explicitly asked.
- Do not touch secrets, appsettings secrets, API keys, provider keys, or environment files.
- Do not rewrite existing docs; update lightweight memory only when stable facts change.

## Database and ID Rules

- VERIFIED 2026-06-09 against `MangaManagementSystem_Schema.sql` and `Application/Interfaces/*`: the database uses integer IDs, NOT GUID. The schema has zero `uniqueidentifier` and zero `NEWID`/`NEWSEQUENTIALID` occurrences.
- Actual ID types: `auth.Roles.role_id SMALLINT IDENTITY`, `auth.Users.user_id INT IDENTITY`, `manga.Series.series_id BIGINT IDENTITY`. FKs follow these (`user_id INT`, `series_id BIGINT`).
- Interfaces match the schema: `IUserService` uses `int` (e.g. `GetUserByIdAsync(int)`); `ISeriesService` uses `long` (e.g. `GetSeriesByIdAsync(long)`). The `int` vs `long` split mirrors `INT` users vs `BIGINT` series and is intentional, not drift.
- Any earlier GUID/`uniqueidentifier` migration was NOT applied. Do not introduce `Guid` IDs unless the team explicitly asks and the schema is migrated first.
- When fixing backend/database mismatch, check model, DTO, repository, stored procedure, `SqlParameter`, EF mapping, and route parameter types together against the integer schema above.

## Workflow Rules

- Cloudinary stores files; SQL Server stores metadata in `manga.FileResource`.
- Important workflows with multi-table writes, status changes, FileResource links, audit events, or concurrency should use stored procedures/wrapper procedures.
- Board Chief owns board poll open/close/cancel behavior and official publication-frequency changes.
- `PageRegion` belongs to `ChapterPageVersion`; annotations link to `PageRegion`.
- Use grep/search first, then open only the smallest relevant files.
