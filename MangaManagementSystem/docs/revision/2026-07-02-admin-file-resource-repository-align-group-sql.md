# 2026-07-02 - Align Admin File Repository with Group SQL

## Context
File Management failed on environments that use the group SQL scripts because the repository was calling admin-specific stored procedures that are not part of the group SQL baseline.

## Scope
Application code only. No database, schema, seed, stored procedure, or SQL script changes.

## Changes
- Replaced admin file search stored procedure dependency with EF Core query against the existing FileResource and Users mappings.
- Replaced admin file detail stored procedure dependency with EF Core query against the existing FileResource and Users mappings.
- Updated soft delete to call the existing group SQL procedure manga.usp_FileResource_SoftDelete instead of the admin-specific proc.

## Not changed
- No SQL files changed.
- No stored procedures created.
- No stored procedures deleted.
- No schema changes.
- No seed data changes.
- No Cloudinary cleanup logic change.
- No UserAvatarMenu refactor in this commit.

## Validation checklist
- Build solution.
- Confirm no references remain to manga.usp_Admin_FileResource_Search.
- Confirm no references remain to manga.usp_Admin_FileResource_GetById.
- Confirm no references remain to manga.usp_Admin_FileResource_SoftDelete.
- Open File Management with a database built from the group SQL baseline.
