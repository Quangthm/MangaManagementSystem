# Session Context: Database Synchronization & Google Sign-up Planning
**Date:** 2026-06-01
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8, EF Core, SQL Server, Blazor Interactive Server

## 1. Resolved Issues (Technical Debt & Bugs)
- **Database Naming Convention Mismatch:** Resolved the `SqlException` caused by EF Core generating `snake_case` queries against a `PascalCase` database.
- **Architecture Shift (Code-First to DB-First):** - Abandoned local EF Core Migrations (deleted the `Migrations` folder).
  - Adopted the team's finalized SQL script (`MangaManagementSystem_Schema.sql`) as the single source of truth.
- **Entity Synchronization (via Cursor):**
  - Enabled `.UseSnakeCaseNamingConvention()` globally.
  - Removed redundant entities (`UserRegistrationRequest`).
  - Added missing properties across multiple entities (`ChapterPageTask`, `SeriesProposal`, `SeriesBoardPoll`, `ChapterReaderVoteSnapshot`).
  - Mapped the read-only SQL View `vw_SeriesBoardPollVoteSummary`.
- **EF Core Shadow State Fix:** Resolved the warning regarding the `Chapter.SeriesId1` shadow property by cleaning up redundant relationship configurations between `Series` and `Chapter`.
- **Database Reset:** Completely dropped the legacy `MangaManagementDB` and recreated it using the final script to clear cached connections and schema conflicts.

## 2. Completed Data Seeding
- Created a robust SQL INSERT script to seed 3 default test accounts (`Mangaka`, `Editor`, `Admin`) with valid BCrypt hashed passwords (`Password123!`), correct `status_code`, and `created_at_utc`.

## 3. New Feature Planning: Google Sign-Up Workflow
Designed a comprehensive 3-tier flow for "Sign-up with Google" to integrate with the existing Minimal API Auth system:
1. **Flow 1 (New User):** Extract Email/Name -> Create DB record (dummy password, `role_id` = 1, `status_code` = 'PENDING_APPROVAL') -> Redirect to OTP Verification.
2. **Flow 2 (Pending User):** User exists but is 'PENDING_APPROVAL' -> Block login -> Redirect to OTP Verification / Error page.
3. **Flow 3 (Active User):** User exists and is 'ACTIVE' -> Issue Auth Cookie -> Redirect to role-specific Dashboard.

## 4. Next Steps & Action Items
- **Task 1:** Execute the Cursor prompt to build the `/api/auth/google-signup` and `/api/auth/google-signup-callback` endpoints.
- **Task 2:** Update `RegisterPage.razor` to include the Google Sign-up `<form>`.
- **Task 3:** Implement the OTP Verification UI (`/verify-otp`) and wire it up with the `EmailService` (using the newly designed HTML Email Template).