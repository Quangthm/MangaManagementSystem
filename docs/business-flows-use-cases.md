# Manga Creation Workflow and Publishing Management System — Business Flows / Use Case Notes

> **Purpose:** Record agreed business flows in a clear step-by-step format so the team can understand how UI, backend, Cloudinary, SQL Server procedures, and audit behavior should work together.
>
> **Expansion note:** This file is meant to be continuously expanded when new workflows are agreed upon. Add new flows without rewriting unrelated existing flows.

> **Latest series lifecycle alignment — 2026-07-19:** `HIATUS` means a paused series. Active Mangaka or Tantou Editor contributors may set a `SERIALIZED` series to `HIATUS` and resume it back to `SERIALIZED`. `HIATUS` blocks chapter release only. Only active Mangaka contributors may mark a `SERIALIZED` or `HIATUS` series as `COMPLETED`; completion blocks future mutations, cancels unreleased chapters and their distinct active `ASSIGNED`/`UNDER_REVIEW` page tasks after warning and confirmation, preserves released chapters and terminal task history, and completed series remain visible in rankings when ranking input exists.

> **Latest ranking and notification alignment — 2026-07-21:** Existing approved notification flows for proposal review/decision, board poll/decision, task assignment/review, chapter review/decision, publication scheduling, and account approval remain unchanged. Ranking now uses a MAL-style weighted rating: `ranking_score = (v / (v + m)) * R + (m / (v + m)) * C`, where `R` is the series average rating, `v` is its rating count, `C` is the rating-count-weighted average rating of the effective ranking scope, and `m` is the median rating count of that scope. `reading_count` remains popularity evidence and a tie-breaker, not a direct score boost. `RANKING_WARNING` is finalized as a hybrid weekly rule: a week fails only when both `ranking_score < 6.5` and the series is in the bottom 25% for that week; high risk requires failure in at least 2 of the latest 3 consecutive completed weekly periods including the latest. Recipients are all distinct active contributors of the exact affected series.

> **Latest chapter submission validation — 2026-07-21:** A Mangaka may submit a chapter for editorial review only from `DRAFT` or `REVISION_REQUESTED` and only when zero distinct active page tasks are associated with that chapter. `ASSIGNED` and `UNDER_REVIEW` tasks block submission; `COMPLETED` and `CANCELLED` tasks do not. Task association is derived through the existing task-region/page-region/page-version/page/chapter relationship and deduplicated by `ChapterPageTaskId`. A blocked submission leaves the chapter unchanged, creates no successful submission audit or `CHAPTER_REVIEW` notification, does not mutate tasks, and returns a clean user-facing validation response. Task creation and chapter submission must be concurrency-coordinated for the same chapter so they cannot commit an invalid state.

---

## 1. Document Conventions

### 1.1 Flow Status

| Status | Meaning |
|---|---|
| `Agreed` | The flow is currently accepted for MVP implementation. |
| `Draft` | The flow is proposed but may still change. |
| `Future` | The flow is useful later but not required for MVP. |

### 1.2 Layer Meaning

| Layer | Meaning |
|---|---|
| UI | Blazor/MudBlazor screen or component used by the user. |
| Backend API/Application Service | ASP.NET Core application logic, validation, Cloudinary upload handling, and procedure calls. |
| Cloudinary | External storage for actual uploaded files. |
| SQL Server | Database tables and stored procedures. |
| Audit | `audit.usp_AuditEvent_Append`, which records traceable business events. |

### 1.3 General System Notes

- The UI and backend should validate user input before sending it to SQL Server.
- Stored procedures should not duplicate checks already enforced by database constraints such as `CHECK`, `UNIQUE`, `NOT NULL`, and `FOREIGN KEY`.
- Stored procedures should still validate permission checks, workflow state rules, cross-table rules, concurrency-sensitive rules, and audit behavior.
- Cloudinary stores actual files.
- SQL Server stores file metadata in `manga.FileResource`.
- Audit procedures resolve `actor_role_name` internally from `actor_user_id`; callers should not pass role-name text to audit.
- For series cover uploads from the Web UI, the selected source image may be cropped in the browser before upload; the backend should receive only the cropped image file as the actual `SERIES_COVER`.
- `username` is the login/system identifier.
- `display_name` is the user-facing identity shown in UI.
- If `display_name` is not provided, the system defaults it to `username`.

---

# 2. Account and Identity Flows

## BF-AUTH-001 — Register Account Without Portfolio

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account without uploading a portfolio file.

### Main Flow

```text
UI register form
→ Backend API receives username/email/password/display_name/role_name
→ Backend hashes password using BCrypt
→ Backend calls auth.usp_User_Create
→ Database resolves role_name into role_id
→ Database creates auth.Users row with status_code = PENDING_APPROVAL
→ Database defaults display_name to username if display_name is empty/null
→ Database writes USER_REGISTERED audit event
→ Backend returns registration success / pending approval response
→ UI shows pending approval message
```

### Database Procedure

```text
auth.usp_User_Create
```

### Important Notes

- `created_by_user_id` should be `NULL` for public self-registration.
- The user cannot access protected workspace functions while `status_code = PENDING_APPROVAL`.
- Rejected/disabled users cannot log in.
- Rejected users keep email and username reserved in MVP.
- `username` and `email` uniqueness is enforced by database constraints.
- No portfolio file is created in this flow.

### System Should Try To

- Keep registration simple.
- Keep account approval explicit.
- Avoid duplicate username/email accounts.
- Avoid using `display_name` as a login identifier.

---

## BF-AUTH-002 — Register Account With Optional Portfolio

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account and attach an optional portfolio file for admin review.

### Recommended Main Flow

```text
UI register form
→ User enters username/email/password/display_name/role_name
→ User optionally selects portfolio file
→ Backend API receives form data + optional portfolio file
→ Backend uploads portfolio file to Cloudinary
→ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
→ Backend starts database workflow
→ Database creates auth.Users row with portfolio_file_id = NULL
→ Database creates manga.FileResource row with file_purpose_code = REGISTRATION_PORTFOLIO
→ Database links manga.FileResource.file_resource_id to auth.Users.portfolio_file_id
→ Database writes USER_REGISTERED audit event
→ Database may write REGISTRATION_PORTFOLIO_ATTACHED audit event
→ Backend returns registration success / pending approval response
→ UI shows pending approval message
```

### Recommended Implementation Shape

Use a wrapper workflow in the backend or a database wrapper procedure so the database part is transactionally consistent:

```text
BEGIN TRAN
    EXEC auth.usp_User_Create with @portfolio_file_id = NULL
    IF portfolio file metadata exists:
        EXEC manga.usp_FileResource_Create with @file_purpose_code = REGISTRATION_PORTFOLIO
        UPDATE auth.Users SET portfolio_file_id = @portfolio_file_resource_id
        EXEC audit.usp_AuditEvent_Append for REGISTRATION_PORTFOLIO_ATTACHED
COMMIT
```

### Important Notes

- The UI should not directly own Cloudinary upload unless the team implements secure signed upload.
- For MVP, backend-managed Cloudinary upload is cleaner and easier to control.
- Cloudinary upload and SQL transaction cannot be perfectly atomic together.
- If Cloudinary upload succeeds but the SQL transaction fails, the backend should try to delete the uploaded Cloudinary asset as cleanup.
- `auth.usp_User_Create` should keep `@portfolio_file_id` because it may be useful for future workflows or imports.
- Normal public registration should usually pass `@portfolio_file_id = NULL`, then attach portfolio after the user row exists.

### System Should Try To

- Avoid orphaned Cloudinary files.
- Avoid creating `FileResource` rows without a clear user/account relationship.
- Keep portfolio upload optional.
- Keep user account status as `PENDING_APPROVAL` after registration.
- Let Admin review portfolio later during account approval.

---

## BF-AUTH-003 — Google Signup Callback

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account using Google identity information.

### Main Flow

```text
User signs in with Google
→ Google returns email and display name
→ Backend checks whether user account already exists by email
→ If account does not exist, backend calls auth.usp_User_Create
→ Backend passes username derived from email or chosen account rule
→ Backend passes display_name from Google display name if available
→ If Google display name is unavailable, database defaults display_name to username
→ Database creates user with status_code = PENDING_APPROVAL
→ Database writes USER_REGISTERED audit event
→ Backend returns pending approval response
```

### Important Notes

- Google signup should not automatically activate the account unless the team changes the approval rule.
- `created_by_user_id` should be `NULL`.
- If no display name is provided, the database procedure defaults it to username.
- If the email already exists, the flow should not create a duplicate account.

### System Should Try To

- Keep Google signup consistent with normal registration.
- Preserve pending approval rules.
- Avoid duplicate accounts by email.
- Use `display_name` for UI identity, not for login.

---

## BF-AUTH-004 — Admin Changes User Status

**Status:** Agreed
**Primary actor:** Admin
**Goal:** Allow Admin to activate, reject, or disable a user account.

### Main Flow

```text
Admin opens account management screen
→ Admin selects target user
→ Admin chooses new status: ACTIVE / REJECTED / DISABLED / PENDING_APPROVAL
→ Admin optionally enters reason
→ Backend calls auth.usp_Admin_ChangeUserStatus
→ Database checks actor is active Admin
→ Database locks the target user status-change workflow
→ Database reads old status
→ Database updates status_code
→ Database writes USER_STATUS_CHANGED audit event
→ If a PENDING_APPROVAL account is approved/activated to ACTIVE, backend creates an in-app ACCOUNT_APPROVED notification for the approved user
→ After successful approval processing, backend sends an account-approval email to the approved user's email address
→ UI refreshes account list/status
```

### Important Notes

- Admin can activate pending users.
- Admin can reject pending users.
- Admin can disable accounts.
- Rejected and disabled users cannot log in.
- Rejected users keep email and username reserved in MVP.
- The audit procedure resolves Admin role snapshot internally from `admin_user_id`.

### System Should Try To

- Prevent double-click or parallel status changes from causing confusing results.
- Keep status changes audit-visible.
- Avoid changing unrelated account fields.

---

## BF-AUTH-005 — User Updates Display Name

**Status:** Agreed
**Primary actor:** General System User
**Goal:** Allow an authenticated user to update their visible display name without changing login identity.

### Main Flow

```text
User opens profile settings
→ User enters new display name
→ Backend calls auth.usp_User_UpdateDisplayName
→ Database checks actor_user_id equals target user_id
→ Database locks display-name update for that user
→ Database updates display_name only
→ Database writes USER_DISPLAY_NAME_UPDATED audit event
→ UI refreshes visible display name
```

### Important Notes

- User does not need to provide password to change display name.
- Display name must not be empty or whitespace-only after trimming.
- Display name is not unique.
- Updating display name must not change username, email, password, role, status, or approval state.
- Audit records should store old and new display name.

### System Should Try To

- Make display name updates simple.
- Preserve username as the login/system identifier.
- Keep identity changes traceable through audit logs.

---

## BF-AUTH-006 — User Password Reset / Password Change

**Status:** Agreed
**Primary actor:** General System User / Admin / Token-based reset flow
**Goal:** Update password hash without changing account approval/status state.

### Main Flow

```text
User/Admin/token flow requests password reset/change
→ Backend verifies required password reset condition
→ Backend hashes new password using BCrypt
→ Backend calls auth.usp_User_ResetPassword
→ Database locks password reset workflow for target user
→ Database updates password_hash only
→ Database preserves existing status_code
→ Database writes password-related audit event
```

### Reset Modes

| Mode | Meaning |
|---|---|
| `SELF_CHANGE` | Logged-in user changes their own password. |
| `TOKEN_RESET` | User resets password through verified reset token. |
| `ADMIN_RESET` | Admin resets password for a user after support/admin verification. |

### Important Notes

- Password reset must not approve, reject, disable, or re-enable accounts.
- Login permission is still decided by `status_code`.
- Current MVP `auth.Users` table only stores `password_hash`; do not update login-failure fields unless those columns are added later.
- Audit procedure resolves actor role internally.

### System Should Try To

- Keep password reset separate from account status changes.
- Never store plain-text passwords.
- Store BCrypt hash in `password_hash`.
- Keep password reset traceable without exposing password hash in audit JSON.

---

# 3. File Resource Flows

## BF-FILE-001 — Create File Resource Metadata

**Status:** Agreed
**Primary actor:** Backend workflow / business workflow caller
**Goal:** Store Cloudinary file metadata in SQL Server and return a `file_resource_id` for the business record that owns the file.

### Main Flow

```text
Backend validates file purpose, file type, size, permission, and workflow context
→ Backend calculates SHA-256 from the exact uploaded file bytes
→ Backend uploads file to Cloudinary
→ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
→ Backend calls the relevant business workflow procedure
→ Business workflow procedure starts a SQL transaction
→ Business workflow procedure calls manga.usp_FileResource_Create inside that transaction
→ Database inserts manga.FileResource row
→ Database returns file_resource_id
→ Business workflow procedure creates/updates the owning business record with file_resource_id
→ Business workflow procedure writes one business-level audit event
→ Database transaction commits
```

### Database Procedure

```text
manga.usp_FileResource_Create
```

### Important Notes

- Cloudinary stores the actual file.
- SQL Server stores metadata and relationships through `manga.FileResource`.
- `sha256_hash` is required by the current schema and supports integrity checks and optional duplicate warnings.
- Business tables should reference `FileResource`, not raw Cloudinary URLs.
- `manga.usp_FileResource_Create` is a helper procedure and should normally be called by a higher-level business procedure.
- The higher-level business procedure should own the SQL transaction so the `FileResource` row, business row, and audit event commit or roll back together.
- Do not open the SQL transaction before the Cloudinary upload, because Cloudinary upload is an external network operation.
- If Cloudinary upload succeeds but the later SQL workflow fails, the backend should try to delete the uploaded Cloudinary asset as cleanup.
- File creation itself does not always need a separate audit event; the meaningful business action should be audited by the caller, such as `SERIES_PROPOSAL_SUBMITTED`, `CHAPTER_PAGE_VERSION_CREATED`, or `USER_AVATAR_UPDATED`.

### MVP File Purpose Upload Format Matrix

| File purpose code | Allowed extensions | Allowed content types | Cloudinary resource type | Notes |
|---|---|---|---|---|
| `SERIES_PROPOSAL` | `.pdf`, `.doc`, `.docx` | `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `raw` | Formal series proposal documents only. Markdown, plain text, and image files are not accepted for proposal submission in MVP. |
| `SERIES_COVER` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Series cover image. In the Web draft UI, the selected source image is cropped client-side first, and the cropped `1000×1500` PNG is uploaded as the actual cover. |
| `CHAPTER_PAGE_VERSION` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Official manga page image/version output. |
| `EDITORIAL_ATTACHMENT` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Editorial markup, review attachments, or supporting screenshots/documents. |
| `REGISTRATION_PORTFOLIO` | `.pdf`, `.doc`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.webp` | Proposal-document content types plus `image/jpeg`, `image/png`, `image/webp` | `raw` for documents; `image` for images | Optional portfolio submitted for account approval/profile review. |
| `USER_AVATAR` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | User profile/avatar image. |

### Validation Notes

- The UI may use browser-side file filters for convenience, but backend Application validation remains authoritative.
- Backend validation should check both file extension and content type when possible.
- Cloudinary cleanup should use the resource type associated with the accepted file purpose and uploaded content type.
- SQL stored procedures should receive validated file metadata and do not need to duplicate extension/MIME validation.


### System Should Try To

- Keep file references consistent.
- Avoid storing Cloudinary fields directly in business tables.
- Preserve enough metadata for audit, display, duplicate warning, and cleanup.
- Keep Cloudinary folders as storage organization only, not business state.

---

## BF-FILE-002 — Soft Delete File Resource

**Status:** Agreed
**Primary actor:** Admin or authorized file-management actor
**Goal:** Mark a file resource as deleted without physically removing its database row.

### Main Flow

```text
Authorized user requests file deletion
→ Backend calls manga.usp_FileResource_SoftDelete
→ Database checks file exists and is not already deleted
→ Database sets deleted_at_utc and deleted_by_user_id
→ Database writes FILE_RESOURCE_SOFT_DELETED audit event
→ UI shows safe placeholder where file is unavailable
```

### Important Notes

- Normal queries should exclude files where `deleted_at_utc IS NOT NULL`.
- Historical/audit views may still show deleted file metadata.
- Deleting from Cloudinary should happen through controlled application workflow, not manual console deletion.
- Audit procedure resolves actor role internally.

### System Should Try To

- Avoid broken file references in UI.
- Preserve historical traceability.
- Keep file deletion reversible at metadata level if needed later.

---

## BF-FILE-003 — Optional Duplicate File Warning by SHA-256

**Status:** Agreed, optional MVP usability behavior
**Primary actor:** General System User / Backend workflow
**Goal:** Detect when a newly selected file appears identical to an existing active file and optionally warn the user before saving another copy.

### Main Flow

```text
User selects/uploads a file
→ Backend reads exact file bytes
→ Backend calculates SHA-256 hash
→ Backend searches active FileResource rows with the same sha256_hash
→ If a possible duplicate exists:
    Backend/UI may optionally show a warning
    User/backend may continue, cancel, or reuse depending on the workflow
→ If no duplicate exists or user continues:
    Backend uploads to Cloudinary if needed
    Backend creates new FileResource metadata
```

### Recommended Duplicate-Warning Situations

| File purpose | Optional warning behavior |
|---|---|
| `REGISTRATION_PORTFOLIO` | Warn if the same portfolio file appears to already exist. |
| `SERIES_PROPOSAL` | Warn strongly if the same proposal file was already submitted, especially for the same series. |
| `SERIES_COVER` | Warn or skip update if the selected cover is identical to the current cover. |
| `CHAPTER_PAGE_VERSION` | Warn strongly if the new page version is identical to the current page version. |
| `EDITORIAL_ATTACHMENT` | Optional warning if the same attachment already exists for the same review/context. |
| `USER_AVATAR` | Low-priority warning; repeated avatars may be allowed or ignored. |

### Important Notes

- Duplicate warnings are optional for MVP.
- Some duplicate-warning UI may be omitted when implementation time is limited.
- The system should still calculate and store `sha256_hash` even if duplicate warnings are not implemented yet.
- Duplicate detection should not be a global uniqueness rule.
- The same exact file may be validly reused in different workflow contexts.
- If duplicate behavior becomes workflow-specific later, document that behavior in the relevant flow.

### System Should Try To

- Reduce accidental duplicate uploads.
- Avoid meaningless repeated page versions or proposal revisions where possible.
- Save Cloudinary storage when practical.
- Keep duplicate detection advisory unless a specific workflow later defines blocking behavior.
- Preserve flexibility for valid file reuse.


# 4. Series and Proposal Flows


## BF-SERIES-001 — Create Series Draft

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Create a new draft series profile and immediately register the creating Mangaka as an active contributor.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
→ Mangaka clicks Create Draft
→ UI shows create draft popup/modal
→ Mangaka enters title, synopsis, one or more genres, optional tags, content language, optional source series, optional proposed publication frequency, and optional cover image
→ If a cover image is selected, the UI opens a 2:3 portrait crop preview dialog
→ Mangaka confirms the crop, and the UI produces a `1000×1500` PNG from the selected visible area
→ Backend validates the form and confirms the actor is an active Mangaka
→ Backend generates slug from title
→ Backend resolves slug uniqueness
→ If a cover image is provided, backend uploads the file to Cloudinary and calculates SHA-256 from the exact uploaded bytes
→ Backend calls manga.usp_Series_Create with series data, selected genre/tag IDs, and optional cover metadata
→ Database calls manga.usp_FileResource_Create if cover metadata exists
→ Database creates manga.Series with status_code = PROPOSAL_DRAFT
→ Database creates an active manga.SeriesContributor row for the creating Mangaka
→ Database writes SERIES_CREATED and contributor-related audit event(s)
→ UI refreshes the draft list and shows the created draft
```

### Database Procedure(s)

```text
manga.usp_Series_Create
manga.usp_FileResource_Create
manga.usp_SeriesContributor_Add
audit.usp_AuditEvent_Append
```

### Important Notes

- Normal series draft creation is a Mangaka workflow, not an Admin workflow.
- The MVP schema does not use `series_code`; `series_id` is the internal identifier and `slug` is the URL identifier.
- Draft series are managed internally by `series_id`, not by `/series/{slug}`.
- Backend generates and resolves slug uniqueness before calling the stored procedure.
- `publication_frequency_code` may be provided by Mangaka during draft creation as the proposed/preferred frequency.
- Genres are selected from `manga.Genre` and linked through `manga.SeriesGenre`.
- Tags are selected from `manga.Tag` and linked through `manga.SeriesTag`.
- Genres and tags are current series metadata; they are not duplicated into proposal snapshot tables in MVP.
- If a cover image is provided through the Web UI, the original selected file is used only in the browser for crop preview; the backend receives the cropped image file.
- The MVP cover crop target is a 2:3 portrait image output as `1000×1500` PNG.
- Smaller source images may be upscaled to `1000×1500`; the UI should warn that the final cover may look blurry.
- No original/cropped dual storage and no crop metadata are stored for `SERIES_COVER` in MVP.
- If a cover image is provided, C# uploads the resulting file to Cloudinary first, then passes Cloudinary metadata to SQL.
- SQL creates the `FileResource` row inside the series creation workflow.
- If SQL fails after Cloudinary upload, the backend should attempt to delete the uploaded Cloudinary asset to avoid orphaned files.
- The creator must be added as an active `SeriesContributor` in the same database workflow/transaction as the `Series` creation.

### System Should Try To

- Keep draft creation simple and owned by Mangaka.
- Keep Cloudinary file metadata, Series row creation, contributor creation, and audit writing transactionally consistent on the SQL side.
- Avoid exposing draft series through slug-based public URLs.
- Avoid requiring a redundant series code when `series_id` and `slug` already serve distinct identity purposes.

---

## BF-SERIES-002 — Update Series Draft Profile

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow an active Mangaka contributor to update draft series profile information while the series is still in `PROPOSAL_DRAFT`.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
→ Mangaka selects a draft series
→ UI opens edit draft popup/modal
→ Mangaka updates title, synopsis, genres, tags, content language, optional source series, optional proposed publication frequency, and optional cover image
→ If a replacement cover image is selected, the UI opens a 2:3 portrait crop preview dialog
→ Mangaka confirms the crop, and the UI produces a `1000×1500` PNG from the selected visible area
→ Backend validates the form and confirms the actor is an active Mangaka contributor of the selected series
→ Backend confirms the series is still PROPOSAL_DRAFT
→ If title changed, backend regenerates slug from title and resolves slug uniqueness
→ If cover image changed, backend uploads the new cover to Cloudinary and calculates SHA-256 from the exact uploaded bytes
→ Backend calls manga.usp_Series_UpdateProfile with updated series data, selected genre/tag IDs, and optional new cover metadata
→ Database calls manga.usp_FileResource_Create if new cover metadata exists
→ Database updates manga.Series editable profile fields
→ Database updates updated_at_utc and updated_by_user_id together
→ Database writes SERIES_PROFILE_UPDATED audit event
→ UI refreshes the draft list/detail
```

### Database Procedure(s)

```text
manga.usp_Series_UpdateProfile
manga.usp_FileResource_Create
audit.usp_AuditEvent_Append
```

### Important Notes

- Normal Mangaka profile updates are allowed only while `Series.status_code = PROPOSAL_DRAFT`.
- Once the series leaves `PROPOSAL_DRAFT`, title, slug, synopsis, genres, tags, cover, content language, source series, and `publication_frequency_code` are locked from normal Mangaka profile editing.
- Slug may auto-regenerate from title during `PROPOSAL_DRAFT` because draft workflows use `series_id`.
- Slug locks after the series leaves `PROPOSAL_DRAFT`; no slug history or redirect table is required for MVP.
- `publication_frequency_code` is treated as Mangaka's proposed/preferred frequency during draft; board serialization/frequency override is handled by a separate board procedure.
- Genres and tags are updated as current series metadata through `SeriesGenre` and `SeriesTag` while the draft remains editable.
- The procedure should rely on database constraints for simple `CHECK`, `UNIQUE`, `NOT NULL`, and FK enforcement, while still validating actor permission and workflow state.
- After a successful update, the UI should refresh only the affected series card through a scoped read query instead of reloading the full dashboard list.
- If the scoped card read returns `404`/`NULL`, the UI must not fabricate a card from local form state; it should remove or mark the card unavailable and warn the user.
- If the scoped card read fails because of a network/server error, the UI may keep the existing card unchanged and warn that the visible card may be stale.

### System Should Try To

- Preserve stable review information after the draft leaves `PROPOSAL_DRAFT`.
- Prevent Admin from owning manga business-content creation/update flows.
- Keep draft editing convenient through popup/modal UI.
- Keep future `/series/{slug}` URLs stable after serialization.

---



## BF-SERIES-003 — Submit Series Proposal for Editorial Review

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Formally submit a draft series proposal with a required proposal file so active Tantou Editors can review it from the editorial queue.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
→ Mangaka selects a series with status_code = PROPOSAL_DRAFT
→ Mangaka clicks Submit Proposal
→ UI opens proposal submission modal
→ Mangaka selects the required proposal file
→ Backend validates the actor/session and request shape
→ Backend uploads the proposal file to Cloudinary
→ Backend calculates SHA-256 from the exact uploaded file bytes
→ Backend calls manga.usp_SeriesProposal_Submit with the series ID, actor user ID, and required proposal file metadata
→ Database locks proposal submission for the series
→ Database verifies the series exists and status_code = PROPOSAL_DRAFT
→ Database verifies the submitter is an active Mangaka contributor of the series
→ Database does not require an active Tantou Editor contributor for first submission
→ Database creates manga.FileResource with file_purpose_code = SERIES_PROPOSAL
→ Database creates manga.SeriesProposal with the next proposal_version_no, proposal title/synopsis snapshots, and proposal file reference
→ Database sets manga.SeriesProposal.status_code = UNDER_EDITORIAL_REVIEW
→ Database sets manga.Series.status_code = UNDER_EDITORIAL_REVIEW
→ Database writes SERIES_PROPOSAL_SUBMITTED audit event
→ Backend returns success
→ UI locks normal draft editing and shows the series/proposal as under editorial review
→ Proposal becomes visible in the Tantou Editor editorial review queue
```

### Database Procedure(s)

```text
manga.usp_SeriesProposal_Submit
manga.usp_FileResource_Create
audit.usp_AuditEvent_Append
```

### Important Notes

- Proposal file is required for formal proposal submission.
- The proposal file must be stored as `FileResource` with `file_purpose_code = SERIES_PROPOSAL`.
- `SERIES_PROPOSAL` upload accepts only `.pdf`, `.doc`, and `.docx` files in MVP. Markdown, plain text, and image files are not accepted for proposal submission.
- Proposal document uploads should be stored in Cloudinary using the `raw` resource type.
- First submission requires an active Mangaka contributor but does not require an active Tantou Editor contributor already assigned to the series.
- Submitted proposals appear in the editorial review queue for active Tantou Editors.
- The UI may prioritize unclaimed proposals first, but the database should not block multiple Tantou Editors from becoming active contributors to the same series.
- Submitted proposal snapshot fields should remain locked; `SeriesProposal` snapshots proposal title, synopsis, and proposal file, but does not snapshot genres, tags, or the current series cover file in MVP.
- Revision later creates a new `SeriesProposal` version instead of overwriting the submitted proposal.
- Proposal review screens may display current genres, tags, and cover from the locked `Series` metadata during review; these are not copied into `SeriesProposal`.
- If Cloudinary upload succeeds but SQL fails, the backend should attempt to delete the uploaded Cloudinary asset.

### System Should Try To

- Keep proposal submission as one database business workflow after Cloudinary upload succeeds.
- Keep FileResource creation, proposal creation, series status update, and audit event in the same SQL transaction.
- Avoid requiring a Tantou Editor before first submission, because the submission itself creates the review queue item.
- Make the newly submitted proposal easy for active Tantou Editors to find.

---
# Suggested Additions — Business Flows / Use Case Notes

---


## BF-SERIES-004 — Pause or Resume Series Serialization

**Status:** Agreed  
**Primary actor:** Mangaka contributor / Tantou Editor contributor  
**Goal:** Allow authorized active contributors to temporarily pause a serialized series by setting it to `HIATUS`, or resume it back to `SERIALIZED`.

### Main Flow — Set Series to HIATUS

```text
Active Mangaka or Tantou Editor contributor opens the series page
→ User chooses Set Series to Hiatus
→ UI asks for confirmation and may collect a reason
→ Backend validates actor is an active Mangaka or Tantou Editor contributor of the series
→ Backend validates Series.status_code = SERIALIZED
→ Backend changes Series.status_code to HIATUS
→ Backend/database writes SERIES_HIATUS_SET audit event
→ Backend may notify active contributors
→ UI refreshes the series status badge and available actions
```

### Main Flow — Resume Serialization

```text
Active Mangaka or Tantou Editor contributor opens a HIATUS series
→ User chooses Resume Serialization
→ UI asks for confirmation and may collect a reason
→ Backend validates actor is an active Mangaka or Tantou Editor contributor of the series
→ Backend validates Series.status_code = HIATUS
→ Backend changes Series.status_code to SERIALIZED
→ Backend/database writes SERIES_SERIALIZATION_RESUMED audit event
→ Backend may notify active contributors
→ UI refreshes the series status badge and available actions
```

### Important Notes

- `HIATUS` is the schema status for a paused series; do not introduce a separate `PAUSED` status.
- Hiatus is a series-level release pause, not a full production freeze.
- While a series is `HIATUS`, drafting, chapter creation, page work, review, scheduling, and rescheduling may continue when normal chapter status and role rules allow those actions.
- While a series is `HIATUS`, chapter release actions are blocked.
- Releasing a chapter requires the series to be resumed back to `SERIALIZED` first.
- Setting a series to `HIATUS` must not automatically move scheduled chapters to `ON_HOLD`; scheduled chapters keep their chapter-level status until a valid chapter workflow changes them.
- A `CANCEL_SERIALIZATION` poll may still target a `HIATUS` series.

### System Should Try To

- Keep `HIATUS` simple and understandable as a release pause.
- Avoid mutating unrelated chapter statuses automatically.
- Keep status changes audit-visible.
- Keep contributor permissions based on active `SeriesContributor` membership.

---

## BF-SERIES-005 — Mark Series as Completed

**Status:** Agreed  
**Primary actor:** Mangaka contributor  
**Goal:** Allow the author-side contributor to end a serialized or hiatus series, cancel unfinished production work, and freeze future business changes.

### Main Flow

```text
Active Mangaka contributor opens the series page
→ Mangaka chooses Mark Series as Completed
→ UI requests authoritative completion impact
→ Backend returns the unreleased chapters that would be cancelled and the count of distinct active tasks beneath those chapters
→ UI warns that completion is final, shows the affected chapter count/list, and shows the active-task cancellation count
→ Mangaka confirms the action
→ Backend starts the completion transaction and reloads authoritative state
→ Backend validates actor is an active Mangaka contributor of the series
→ Backend validates Series.status_code is SERIALIZED or HIATUS
→ Backend recalculates affected unreleased chapters
→ Backend recalculates distinct active tasks linked to those affected chapters
→ Backend changes distinct ASSIGNED and UNDER_REVIEW tasks under affected chapters to CANCELLED
→ Backend keeps COMPLETED and already CANCELLED tasks unchanged
→ Backend keeps tasks belonging only to unaffected chapters unchanged
→ Backend keeps RELEASED chapters unchanged
→ Backend changes DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, and ON_HOLD chapters to CANCELLED
→ Backend keeps already CANCELLED chapters unchanged
→ Backend changes Series.status_code to COMPLETED
→ Backend writes SERIES_COMPLETED audit, chapter-cancellation audits, and one CHAPTER_PAGE_TASK_CANCELLED audit per distinct cancelled task
→ Backend saves and commits the series, chapter, task, and required audit changes atomically
→ UI refreshes the series page as read-only/completed
```

### Important Notes

- Only active Mangaka contributors may mark a series as `COMPLETED`.
- `COMPLETED` means the series naturally ended by author-side decision.
- `COMPLETED` is different from `CANCELLED`, which represents board/business cancellation.
- Completion is final and normally irreversible through normal workflow.
- Completion blocks future business mutations under the series, including series profile/status changes, new chapters, page/page-version changes, region edits, task changes, review actions, scheduling, rescheduling, hold, and release actions.
- Completion impact preview is advisory; the execution flow must reload/recalculate authoritative chapters and tasks after confirmation.
- Released chapters remain released.
- Unreleased active chapters cancelled by completion are `DRAFT`, `REVISION_REQUESTED`, `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, and `ON_HOLD`.
- Active tasks cancelled by completion are distinct `ASSIGNED` and `UNDER_REVIEW` tasks linked to those affected chapters.
- A task is counted, changed, and audited once even if it is linked to multiple matching `PageRegion` records.
- `COMPLETED` tasks, already `CANCELLED` tasks, and tasks under unaffected chapters remain unchanged.
- Completion-cancelled chapters and tasks remain preserved as historical records.
- Existing files, page versions, regions, annotations, reviews, notifications, and audit logs remain preserved for authorized viewing.
- Task, chapter, series, and required audit changes must commit as one transaction or roll back together.
- Completed series remain visible in dynamic rankings when vote input exists for the selected publication period.

### System Should Try To

- Make the completion action deliberate and hard to trigger accidentally.
- Warn about both unreleased chapter impact and active assigned-task impact before confirmation.
- Preserve history instead of deleting records.
- Prevent a completed series from retaining active tasks beneath chapters cancelled by completion.
- Keep `COMPLETED`, `HIATUS`, and `CANCELLED` clearly separated.
- Avoid adding generic series status-history tables; use current status plus audit logs.

---
## BF-MANGAKA-001 — View My Series Dashboard

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Show only series where the logged-in actor is an active Mangaka contributor.

### Main Flow

```text
Mangaka opens /mangaka
→ UI calls GET /api/mangaka/series/my-series through typed API client
→ API resolves actor user id
→ API sends GetMyMangakaSeriesQuery
→ Application validates actor id
→ Infrastructure queries series where actor is an active Mangaka contributor
→ Query includes cover, genres, tags, status, slug, proposed frequency, and update time
→ UI renders dashboard cards with filters/search/sort
```

### Important Notes

* `/mangaka` must not show every series in the database.
* A series is visible only when the actor is an active Mangaka contributor.
* The Web layer must call the API through `IMangakaSeriesApiClient`.
* The API controller dispatches through MediatR.
* Read/list filtering may use EF read projections with `AsNoTracking`.
* No stored procedure is required for this read-only dashboard query.

### System Should Try To

* Keep Mangaka dashboard scoped to the current actor.
* Avoid direct Razor-to-Application calls for migrated workflows.
* Keep dashboard filtering/search/sort responsive on the client when data is already loaded.

---

## BF-MANGAKA-002 — Navigate to Assistant Review / Review Submissions

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Open the enhanced task-review page from the Mangaka dashboard sidebar.

### Main Flow

```text
Mangaka opens /mangaka
→ Mangaka clicks sidebar item "Assistant Review"
→ UI routes to /mangaka/review-submissions
→ Page loads under MangakaLayout
→ UI calls GET /api/mangaka/tasks
→ Backend returns tasks created by the Mangaka for assistant work review
→ UI displays task cards with series/chapter/page/version/task context
```

### Important Notes

* The old embedded simple task table inside `MangakaDashboard.razor` is no longer the active destination.
* `/mangaka/review-submissions` is the active Mangaka task-review page.
* Current Series and Series Proposals remain dashboard tabs for now.
* This is a route/navigation decision, not a database workflow.

### System Should Try To

* Keep task review on a dedicated page.
* Avoid large MangakaDashboard refactors until explicitly approved.
* Preserve `/mangaka` dashboard behavior for Current Series and Series Proposals.

---

## BF-TASK-004 — Mangaka Reviews Assistant Task Submissions

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Let a Mangaka review assistant-submitted page work and choose the correct review action: approve, return to the same Assistant for rework, cancel, or reassign to a different Assistant.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
→ UI loads task list for the Mangaka-created tasks
→ UI shows task cards with series, chapter, page, page version, assistant, type, status, due date, priority, compensation, and region count
→ Mangaka filters by series/chapter/title, task type, assistant, and status
→ Mangaka opens original/submitted previews when available
→ Mangaka chooses one available action:
    - Approve
    - Return for Rework
    - Cancel
    - Reassign
→ UI calls the relevant typed API client method
→ API calls the relevant Application task use case/service
→ Application validates actor permission and task state
→ Infrastructure calls the relevant stored procedure/repository workflow
→ UI reloads task list, clears dialog state, and refreshes stat cards/actions
```

### Important Notes

* Task cards should include `PageVersionNo` when available.
* Review actions must refresh the visible UI without requiring browser refresh.
* Filters should continue to apply after refresh.
* Backend rules must remain the source of truth for allowed task transitions.
* **Return for Rework** and **Reassign** are separate workflows:

  * Return for Rework sends the same task back to the same Assistant.
  * Reassign moves ownership to a different Assistant by cancelling the old task and creating a replacement task.

### System Should Try To

* Give Mangaka enough context to review assistant work without opening many pages.
* Keep review actions safe and state-aware.
* Avoid stale task cards after workflow mutations.
* Preserve task history through audit events.

---

## BF-TASK-005 — Return Assistant Task for Rework to Same Assistant

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Return an `UNDER_REVIEW` task to the same assigned Assistant for rework.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
→ UI shows Return for Rework action on eligible UNDER_REVIEW task
→ Mangaka opens Return for Rework dialog
→ Mangaka enters updated task instructions/description
→ UI calls Return for Rework API action
→ API calls Application task return-for-rework use case/service
→ Application validates actor, task state, and updated task description
→ Infrastructure calls manga.usp_ChapterPageTask_ReturnForRework
→ SQL locks the task workflow
→ SQL verifies the task exists
→ SQL verifies status_code = UNDER_REVIEW
→ SQL verifies actor is an active Mangaka contributor of the task's series
→ SQL updates the same ChapterPageTask row:
    - status_code = ASSIGNED
    - completed_page_version_id = NULL
    - task_description = updated task description
    - updated_at_utc = current UTC time
→ SQL writes CHAPTER_PAGE_TASK_RETURNED_FOR_REWORK audit event
→ UI reloads task list and clears dialog state
```

### Database Procedure

```text
manga.usp_ChapterPageTask_ReturnForRework
```

### Important Notes

* Return for Rework applies only to `UNDER_REVIEW` tasks.
* Return for Rework keeps the same assigned Assistant.
* Return for Rework does not create a replacement task.
* The same `chapter_page_task_id` remains active.
* The task status becomes `ASSIGNED`.
* `completed_page_version_id` is cleared because the submitted output was rejected for rework.
* `task_description` is replaced with the updated rework instruction.
* Actor must be an active Mangaka contributor of the task's series.
* SQL derives the task's series through:
  `ChapterPageTaskRegion → PageRegion → ChapterPageVersion → ChapterPage → Chapter → Series`.
* Application/C# should validate the primary business rules before calling SQL when possible.
* SQL remains the final transactional guard and audit owner.

### System Should Try To

* Let Mangaka request revisions from the same Assistant without creating a new task.
* Preserve audit traceability of rejected submitted work.
* Prevent returning tasks that are not currently under review.

---

## BF-TASK-006 — Reassign Assistant Task to Different Assistant

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow a Mangaka to move a task to a different eligible Assistant contributor when the original Assistant should no longer own the task.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
→ UI shows Reassign action for ASSIGNED and UNDER_REVIEW tasks
→ Mangaka opens Reassign dialog
→ UI loads eligible assistants for the task
→ Mangaka selects a different Assistant
→ Mangaka enters required reason
→ UI calls POST /api/mangaka/tasks/{taskId}/reassign
→ API calls Application task reassignment use case
→ Application validates actor, task status, reason, same-user rule, and assistant eligibility
→ Infrastructure calls manga.usp_ChapterPageTask_AssignToDifferentUser
→ SQL cancels old task
→ SQL creates replacement ASSIGNED task for the new Assistant
→ SQL copies task-region links
→ SQL writes CHAPTER_PAGE_TASK_ASSIGNED_TO_DIFFERENT_USER audit event
→ UI reloads task list after success
```

### Database Procedure

```text
manga.usp_ChapterPageTask_AssignToDifferentUser
```

### Important Notes

* Reassignment is different from Return for Rework.
* Reassignment changes the assigned Assistant.
* Reassignment requires the new Assistant to be different from the current assigned Assistant.
* Reassignment is allowed for `ASSIGNED` and `UNDER_REVIEW`.
* Reassignment is not allowed for `COMPLETED` or `CANCELLED`.
* The new Assistant must be an active contributor of the same series.
* Reason is required and should be limited to 500 characters.
* The stored procedure cancels the old task and creates a new replacement task with status `ASSIGNED`.
* Because the task id changes, the UI should reload the task list rather than only mutating the old card.
* C# Application layer owns primary business validation before calling SQL.
* SQL procedure provides locking, final guards, copy of task-region links, and audit.

### System Should Try To

* Avoid assigning work to non-contributors.
* Avoid reassigning completed/cancelled tasks.
* Preserve history by cancelling the old task and creating a replacement.
* Keep audit traceability between old and replacement tasks.


**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow a Mangaka to reassign an Assistant task to a different eligible Assistant contributor on the same series.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
→ UI shows Reassign action for ASSIGNED and UNDER_REVIEW tasks
→ Mangaka opens Reassign dialog
→ UI loads eligible assistants for the task
→ Mangaka selects a different Assistant
→ Mangaka enters required reason
→ UI calls POST /api/mangaka/tasks/{taskId}/reassign
→ API calls Application task reassignment use case
→ Application validates actor, task status, reason, same-user rule, and assistant eligibility
→ Infrastructure calls manga.usp_ChapterPageTask_AssignToDifferentUser
→ SQL cancels old task, creates replacement ASSIGNED task, copies task-region links, and writes audit event
→ UI reloads task list after success
```

### Database Procedure

```text
manga.usp_ChapterPageTask_AssignToDifferentUser
```

### Important Notes

* Reassignment is allowed for `ASSIGNED` and `UNDER_REVIEW`.
* Reassignment is not allowed for `COMPLETED` or `CANCELLED`.
* The new assistant must be different from the current assigned assistant.
* The new assistant must be an active contributor of the same series.
* Reason is required and must be limited to 500 characters.
* The stored procedure cancels the old task and creates a new replacement task with status `ASSIGNED`.
* Because the task id changes, the UI should reload the task list rather than only mutating the old card.
* SQL procedure provides locking, final guards, copy of task-region links, and audit.
* C# Application layer still owns primary business validation before calling SQL.

### System Should Try To

* Avoid reassigning completed/cancelled tasks.
* Avoid assigning work to non-contributors.
* Preserve task history by cancelling the old task and creating a replacement.
* Keep audit traceability between old and replacement tasks.

---

## BF-MANGAKA-CONTRIB-001 — View Series Contributors

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Let a Mangaka view contributor history for series where they are an active Mangaka contributor.

### Main Flow

```text
Mangaka opens /mangaka/contributors
→ UI loads the Mangaka's own series list from GET /api/mangaka/series/my-series
→ Mangaka selects a series
→ UI calls GET /api/mangaka/series/{seriesId}/contributors
→ API sends GetSeriesContributorsQuery
→ Application validates actor permission
→ Infrastructure returns contributor rows for the selected series
→ UI displays active and former contributors with role, status, start date, end date, and actions
```

### Important Notes

* Actor may view contributors only for series where they are an active Mangaka contributor.
* Active contributor means `EndDate == null`.
* Former contributor means `EndDate != null`.
* Contributor history must be preserved.
* Mangaka and Tantou Editor contributors are read-only in this MVP screen.
* Only Assistant contributors are manageable in this workflow.

### System Should Try To

* Keep contributor history visible.
* Avoid deleting contributor rows.
* Prevent Mangaka from viewing contributors for series they do not manage.

---

## BF-MANGAKA-CONTRIB-002 — Add Assistant Contributor to Series

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Add an active Assistant user as an active contributor to one of the Mangaka's own series.

### Main Flow

```text
Mangaka opens /mangaka/contributors
→ Mangaka selects a series
→ Mangaka clicks Add Assistant
→ UI opens Add Assistant dialog
→ UI searches eligible assistants through GET /api/mangaka/series/{seriesId}/contributors/eligible-assistants
→ Backend returns ACTIVE Assistant users who are not currently active contributors of the selected series
→ Mangaka selects an Assistant
→ UI calls POST /api/mangaka/series/{seriesId}/contributors/assistants
→ API sends AddAssistantContributorCommand
→ Application validates actor, target user role/status, and duplicate active contributor rule
→ Infrastructure calls manga.usp_SeriesContributor_Add
→ Database inserts active SeriesContributor row and writes audit
→ UI refreshes contributor list and eligible-assistant search
```

### Database Procedure

```text
manga.usp_SeriesContributor_Add
```

### Important Notes

* Target user must exist.
* Target user must have role `Assistant`.
* Target user must have `status_code = ACTIVE`.
* Target user must not already be an active contributor of the selected series.
* Historical ended contributor rows must not block re-adding.
* The existing generic add procedure may be reused only after Assistant-specific C# validation.
* C# Application validation must not rely only on SQL errors.

### System Should Try To

* Make adding assistants discoverable through search/autocomplete.
* Avoid duplicate active contributor rows.
* Keep historical contributor rows reusable for future re-adds.

---

## BF-MANGAKA-CONTRIB-003 — End Assistant Contribution

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** End an active Assistant contributor's participation in a series while preserving history.

### Main Flow

```text
Mangaka opens /mangaka/contributors
→ Mangaka selects a series
→ UI shows End action only for active Assistant contributor rows
→ Mangaka opens End Assistant dialog
→ Mangaka enters reason
→ UI calls POST /api/mangaka/series/{seriesId}/contributors/assistants/{assistantUserId}/end
→ API sends EndAssistantContributorCommand
→ Application validates actor permission, target role/status, reason, and active task blocking rule
→ Infrastructure calls manga.usp_SeriesContributor_EndAssistant
→ SQL sets end_date to current UTC date and writes audit
→ UI refreshes contributor list and eligible-assistant search
```

### Database Procedure

```text
manga.usp_SeriesContributor_EndAssistant
```

### Important Notes

* This procedure may exist as a deployment script before being applied to the target database.
* Remove/end action is Assistant-only in this MVP screen.
* Do not end Mangaka contributors in this workflow.
* Do not end Tantou Editor contributors in this workflow.
* Do not delete SeriesContributor rows.
* Set `end_date`; preserve history.
* Reason is required and must be limited to 500 characters.
* If the Assistant has `ASSIGNED` or `UNDER_REVIEW` tasks for the series, block ending and show:
  `This assistant has active tasks. Reassign or cancel their tasks before removing them from the series.`
* Active-task lookup must derive series through the task-region/page/chapter chain, not through a direct task series id.

### System Should Try To

* Protect active production work from orphaned assignments.
* Preserve contributor history.
* Keep role management narrow and safe for MVP.

---

## BF-NAV-001 — Safe Return URL Navigation

**Status:** Agreed
**Primary actor:** Any authenticated role
**Goal:** Preserve safe back-navigation across role pages, series pages, and workspace links.

### Main Flow

```text
User opens a page that links to /series/{slug} or workspace
→ UI appends a local returnUrl using SafeReturnUrl.AppendReturnUrl()
→ Target page resolves returnUrl through SafeReturnUrl.Resolve()
→ If returnUrl is safe, Back button uses it
→ If returnUrl is unsafe or missing, page falls back to a safe default such as /dashboard
```

### Important Notes

* Safe return URLs should allow trusted local prefixes only.
* Allowed local prefixes include `/mangaka`, `/assistant`, `/editor`, `/board-chief`, `/board`, `/admin`, `/series`, and `/dashboard`.
* Reject external URLs, protocol-relative URLs, backslashes, `javascript:`, `data:`, `/api/`, and `/signout`.
* SeriesPage fallback should be role-neutral, not hardcoded to `/mangaka`.
* Workspace links from `/mangaka/review-submissions` should pass `returnUrl=/mangaka/review-submissions`.

### System Should Try To

* Prevent open redirects.
* Keep cross-role navigation predictable.
* Preserve user workflow context after opening series/workspace pages.

# 5. Board Poll and Publication Frequency Flows

## BF-BOARD-001 — Open START_SERIALIZATION Poll

**Status:** Agreed
**Primary actor:** Editorial Board Chief
**Goal:** Start board voting for serialization and specify the official publication frequency if approved.

### Main Flow

```text
Editorial Board Chief opens board review queue
→ Chief selects eligible series/proposal
→ Chief enters poll reason
→ Chief selects board_publication_frequency_code
→ Backend calls board poll creation procedure
→ Database checks series is UNDER_BOARD_REVIEW
→ Database checks exactly one active proposal is UNDER_BOARD_REVIEW
→ Database creates SeriesBoardPoll with poll_type_code = START_SERIALIZATION
→ Database stores board_publication_frequency_code on the poll
→ Database writes board poll creation audit event
→ Board members and chief may vote while poll is open
```

### Important Notes

- `START_SERIALIZATION` poll must include board-selected publication frequency.
- The poll stores the frequency being voted on.
- If approved, this frequency becomes `Series.publication_frequency_code`.
- Mangakaâ€™s preferred frequency is not stored as a separate official column in MVP.
- Mangaka may later request a change through notification, not through a formal request table.

### System Should Try To

- Make board frequency decision explicit before voting starts.
- Keep the official frequency controlled by Editorial Board Chief/board workflow.
- Avoid Admin owning board poll actions.

---

## BF-BOARD-002 — Apply START_SERIALIZATION Poll Result

**Status:** Agreed
**Primary actor:** Editorial Board Chief or system workflow
**Goal:** Apply the computed board vote result after poll closure.

### Main Flow

```text
Poll is closed
→ System computes approve/reject/abstain counts
→ If approve > reject:
    Series status becomes SERIALIZED
    Active proposal status becomes APPROVED
    Series.publication_frequency_code = SeriesBoardPoll.board_publication_frequency_code
→ If reject > approve:
    Proposal/series follow MVP rejection/cancellation policy
→ If tied:
    Series/proposal remain UNDER_BOARD_REVIEW
→ Database writes board result application audit event
```

### Important Notes

- Board result is computed from `SeriesBoardVote`.
- No separate `SeriesBoardDecision` table is used in MVP.
- Abstain votes are counted separately but do not directly decide approval/rejection.
- Only closed polls can be applied.
- Cancelled polls are invalidated and cannot affect status.

### System Should Try To

- Keep poll result deterministic.
- Keep proposal/series status changes traceable.
- Apply board-selected publication frequency only when serialization is approved.

---

---

# 6. Page Modification and Page Version Flows

## BF-PAGE-001 — Temporary Page Modification Download

**Status:** Draft
**Primary actor:** Mangaka / Authorized Page Workspace User
**Goal:** Let a user generate or edit a modified page output with system tools and download it for external editing without replacing the current page version.

### Example System Tools

```text
Auto-translation
→ AI/OCR text replacement preview
→ future cleanup tool
→ future effects/coloring helper
→ future panel/text adjustment tool
```

### Main Flow

```text
User opens page workspace for an existing ChapterPageVersion
→ User uses a system tool that modifies or generates a changed page output
→ Backend verifies the user can access the page/version and use the selected tool
→ Backend sends the current page image, Cloudinary URL, or required page data to the relevant tool/service
→ Tool/service generates a modified output file or editable preview
→ Backend may upload the modified output to a temporary Cloudinary folder such as manga-management/temp/page-modifications/
→ Backend returns a temporary preview/download URL to the UI
→ User downloads the modified output for external editing or discards it
→ No manga.FileResource row is created for the temporary output by default
→ No ChapterPageVersion row is created
→ Current ChapterPageVersion remains unchanged
→ Temporary Cloudinary asset is deleted later by cleanup workflow
```

### Important Notes

- This flow covers auto-translation and future system editing tools that generate changed page output.
- This flow does not violate the page-version rule because the generated or modified output is not accepted back into the system as official page content.
- Temporary preview/download files are not business records by default.
- Temporary Cloudinary files should not be referenced by `ChapterPageVersion`.
- The system should not overwrite the existing Cloudinary asset for the current page version.
- The system should use a new temporary Cloudinary public ID for each generated or modified output.
- Temporary download URLs are only for user convenience and should not be treated as audit evidence.
- If the user later wants to keep the externally edited file in the system, they must upload/save it through the normal new page-version flow.

### System Should Try To

- Let users experiment with system-generated or tool-edited output safely.
- Avoid creating meaningless page versions for temporary page modifications.
- Avoid cluttering `manga.FileResource` with temporary files.
- Avoid overwriting current page-version files.
- Clean up temporary Cloudinary assets later.

---

## BF-PAGE-002 — Save Modified Page Output as New Page Version

**Status:** Draft
**Primary actor:** Mangaka / Authorized Page Workspace User
**Goal:** Save an accepted modified page output as an official tracked `ChapterPageVersion`.

### Example Modified Outputs

```text
Accepted auto-translated page output
→ accepted AI/OCR edited page output
→ accepted assistant output
→ user-edited file re-uploaded after temporary download
→ future accepted output from other page editing tools
```

### Main Flow

```text
User reviews modified page output
→ User clicks Save as New Page Version or uploads the externally edited output back into the system
→ Backend validates the user can create a new page version for the logical page
→ Backend uploads the final accepted page file to Cloudinary if it is not already stored as a permanent asset
→ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
→ Backend calls the page-version creation workflow procedure
→ Database transaction begins
→ Database calls manga.usp_FileResource_Create with file_purpose_code = CHAPTER_PAGE_VERSION
→ Database creates manga.FileResource row
→ Database creates manga.ChapterPageVersion row using the returned file_resource_id
→ Database updates current-version state if workflow rules allow this new version to become current
→ Database writes CHAPTER_PAGE_VERSION_CREATED audit event with the source/tool context in detail_json when available
→ Database transaction commits
→ UI shows the new page version in page-version history
```

### Database Procedure(s)

```text
manga.usp_FileResource_Create
manga.usp_ChapterPageVersion_Create or equivalent page-version workflow procedure
```

### Important Notes

- Use `file_purpose_code = CHAPTER_PAGE_VERSION` for any file that becomes official page-version content.
- This includes accepted auto-translation output, accepted future tool output, accepted assistant output, and externally edited output uploaded back into the system.
- Do not use removed purposes such as `CHAPTER_ASSET`, `TASK_SUBMISSION`, or old `CHAPTER_DRAFT`.
- Selecting or uploading a page file for preview must not create a `FileResource` or `ChapterPageVersion` until the user explicitly clicks Save/Confirm.
- In the current MVP, normal users cannot delete a saved `ChapterPageVersion`; they should save a newer version and make it current when the previous saved version is wrong or outdated.
- Future Admin/system retention may purge old or unused page versions after chapter release, but this is outside MVP and must preserve versions referenced by regions, annotations, tasks, reviews, release history, or audit.
- The existing page version should not be overwritten.
- Previous page versions remain available for traceability and comparison.
- The page-version workflow procedure should own the SQL transaction so `FileResource`, `ChapterPageVersion`, current-version updates, and audit event stay transactionally consistent.
- Cloudinary folders are storage organization only; business state is determined by SQL Server records.
- The audit event may include source context such as `source_type = AUTO_TRANSLATION`, `source_type = ASSISTANT_OUTPUT`, or `source_type = MANUAL_UPLOAD` in `detail_json`, but this does not require separate file purpose codes.

### System Should Try To

- Preserve page-version history.
- Avoid overwriting previous files.
- Keep accepted modified output human-reviewed before becoming official page content.
- Keep file creation, page-version creation, and audit behavior consistent.
- Keep the flow extensible for future page-editing tools without creating new file-purpose codes unnecessarily.

---

---

## BF-PAGE-003 — Create Page Regions for a Page Version

**Status:** Agreed
**Primary actor:** Authorized Page Workspace User
**Goal:** Save one or more manual or AI-suggested page regions for a specific `ChapterPageVersion`.

### Main Flow

```text
User opens the chapter/page workspace
→ User selects a page version
→ User draws one or more manual regions or accepts AI-suggested regions
→ UI prepares region JSON as either one object or an array of objects
→ Backend validates the selected chapter_page_version_id and region request shape
→ Backend calls manga.usp_PageRegion_Create
→ Database verifies actor is an ACTIVE user and active SeriesContributor for the series that owns the selected page version
→ Database normalizes single-object JSON into an array when needed
→ Database parses region fields from JSON
→ Database inserts manga.PageRegion rows linked to the selected chapter_page_version_id
→ Database returns created page_region_id values as JSON
→ Database writes PAGE_REGIONS_CREATED audit event
→ UI can use the returned page_region_id values for annotation, task assignment, segmentation display, or later workspace actions
```

### Expected Region JSON

Single region input is allowed:

```json
{
  "type_code": "PANEL",
  "region_label": "Panel 1",
  "x": 10.00,
  "y": 20.00,
  "width": 300.00,
  "height": 200.00,
  "confidence_score": 0.9123,
  "source_type": "AI",
  "original_text": null
}
```

Batch region input is also allowed:

```json
[
  {
    "type_code": "PANEL",
    "region_label": "Panel 1",
    "x": 10.00,
    "y": 20.00,
    "width": 300.00,
    "height": 200.00,
    "confidence_score": 0.9123,
    "source_type": "AI",
    "original_text": null
  },
  {
    "type_code": "SPEECH_BUBBLE",
    "region_label": "Bubble 1",
    "x": 350.00,
    "y": 60.00,
    "width": 120.00,
    "height": 90.00,
    "confidence_score": 0.8750,
    "source_type": "AI",
    "original_text": "ă“ă‚“ă«ă¡ă¯"
  }
]
```

### Database Procedure(s)

```text
manga.usp_PageRegion_Create
audit.usp_AuditEvent_Append
```

### Important Notes

- `manga.usp_PageRegion_Create` is the main PageRegion creation procedure for both singular and bulk creation.
- The procedure accepts either one JSON object or a JSON array of region objects.
- The procedure returns created IDs through `@created_page_region_ids_json`.
- The preferred output shape for C# workflow orchestration is a plain JSON array of GUID strings.
- `PageRegion` is linked directly to `ChapterPageVersion`.
- `PageRegion` should be used for saved manual regions and saved/accepted AI-suggested regions.
- Temporary AI suggestions that the user has not accepted do not need to be saved as `PageRegion`.
- The procedure should check actor permission and ownership context because those are cross-table business rules.
- The table constraints should enforce simple field rules such as valid region type, valid source type, positive dimensions, confidence score rules, and foreign keys.
- `source_type = AI` means the region came from an AI suggestion. If an AI region is manually adjusted later, the system should update it as manual according to the PageRegion editing rules.

### System Should Try To

- Let the same stored procedure handle one region or many regions.
- Keep region creation consistent for manual drawing, accepted AI segmentation output, annotation support, and task support.
- Avoid saving duplicate or temporary AI suggestions unless the user chooses to keep them.
- Return created IDs in a format that is easy for C# to merge with existing region IDs.
- Keep audit detail focused on the page version and created region IDs/count.

---

## BF-PAGE-003A — Delete Unused Page Region

**Status:** Agreed  
**Primary actor:** Authorized Page Workspace User  
**Goal:** Allow a mistaken or unused saved `PageRegion` to be hard-deleted only when it has not become part of task or annotation history.

### Main Flow

```text
User opens the chapter/page workspace
→ User selects a saved PageRegion
→ UI requests deletion/removal
→ Backend validates actor permission for the owning series/page workspace
→ Backend checks whether the PageRegion is linked by ChapterPageAnnotationRegion, ChapterPageTaskRegion, or any other workflow record
→ If the region is not linked to any dependent workflow record, backend hard-deletes the PageRegion
→ UI removes the region overlay from the page version
→ If the region is linked to an annotation or task, backend rejects deletion and returns a clear explanation
```

### Important Notes

- Hard deletion is allowed only for unused regions that are not connected to tasks, annotations, or other workflow records.
- A region linked to a task or annotation is part of workflow history and must be preserved.
- Deleting an unused region must not cascade into annotations, tasks, page versions, or files.
- Whole-page regions created for Quick Select become non-deletable once linked to a task.
- The UI should disable or hide the delete action for linked regions when that information is already known.

### System Should Try To

- Let users clean up mistaken unused regions.
- Prevent deletion from breaking annotation or task history.
- Keep region deletion simple without introducing a soft-delete lifecycle for unused regions in MVP.

---

## BF-PAGE-004 — Create Page Annotation Linked to Existing or Newly Created Page Regions

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor
**Goal:** Create a page annotation/comment and link it to one or more `PageRegion` records.

### Main Flow

```text
User opens the chapter/page workspace
→ User selects one or more existing saved PageRegion records and/or draws new unsaved regions
→ User enters issue type and annotation text
→ Backend separates existing_page_region_ids from new region objects
→ Backend starts one SQL transaction
→ If new unsaved regions exist:
    Backend calls manga.usp_PageRegion_Create with chapter_page_version_id and regions_json
    Database creates PageRegion rows and returns created_page_region_ids_json
    Backend reads the returned created page_region_id values
→ Backend merges existing_page_region_ids with newly created page_region_id values
→ Backend removes duplicate IDs before sending the final region list
→ Backend calls manga.usp_ChapterPageAnnotation_Create with actor_user_id, issue_type_code, annotation_text, and final page_region_ids_json
→ Database validates page_region_ids_json
→ Database verifies all referenced PageRegion rows exist
→ Database verifies all referenced PageRegion rows belong to the same ChapterPageVersion
→ Database derives owning series/page context through linked PageRegion records
→ Database verifies the actor account is ACTIVE
→ Database verifies the actor is an active contributor for the owning series
→ Database verifies the actor role is Mangaka or Tantou Editor
→ Database creates one manga.ChapterPageAnnotation row
→ Database creates one or more manga.ChapterPageAnnotationRegion rows
→ Database writes CHAPTER_PAGE_ANNOTATION_CREATED audit event
→ Backend commits the SQL transaction
→ UI shows the annotation marker/comment linked to all selected regions
```

### Existing Region ID JSON

```json
[
  "A2F98C3A-CE7D-44BD-85F2-B7C1525B4C41",
  "46D8D55C-1174-4DA8-8E7E-6E49B5D6E53A"
]
```

### Database Procedure(s)

```text
manga.usp_PageRegion_Create
manga.usp_ChapterPageAnnotation_Create
audit.usp_AuditEvent_Append
```

### Database Tables

```text
manga.ChapterPageAnnotation
manga.ChapterPageAnnotationRegion
manga.PageRegion
manga.ChapterPageVersion
```

### Important Notes

- `ChapterPageAnnotation` stores the annotation header: issue type, author, text, resolution fields, and created timestamp.
- `ChapterPageAnnotation` no longer stores `page_region_id` directly.
- `ChapterPageAnnotationRegion` links one annotation to one or more `PageRegion` records.
- One annotation may reference multiple regions, such as several speech bubbles with the same typesetting issue.
- Each annotation must link to at least one `PageRegion`.
- All regions linked to the same annotation must belong to the same `ChapterPageVersion`.
- The annotation does not need its own direct `chapter_page_version_id` because the page-version context is derived through `ChapterPageAnnotationRegion → PageRegion → ChapterPageVersion`.
- The annotation does not store direct coordinates; coordinates remain in `PageRegion`.
- Existing saved regions and newly created regions may be mixed in one annotation workflow.
- C# should own the outer transaction for the mixed case so newly created PageRegion rows can be rolled back if annotation creation/linking fails.
- The stored procedures use the standard transaction pattern, so they should not commit when called inside an existing C# transaction.
- The annotation creation procedure should still validate cross-table rules even if C# already checked the IDs.
- The `issue_type_code` allowed values are enforced by the `ChapterPageAnnotation` table constraint.
- `ChapterPageAnnotationRegion` primary key prevents duplicate annotation-region pairs.
- Mangaka-created annotations are production-tracking feedback.
- Tantou Editor-created annotations are editorial-review feedback.
- The MVP does not add an annotation-origin column; permission is guarded by stored procedures using the annotation creator's current role and owning series context.

### System Should Try To

- Keep annotation creation easy from the workspace UI.
- Support comments on one region or many regions.
- Support mixed workflows where some regions already exist and some are created during annotation.
- Keep page-version traceability through linked `PageRegion` records.
- Keep annotation text separate from region geometry.
- Roll back both new regions and the annotation if any part of the mixed workflow fails.

---

## BF-PAGE-005 — Resolve Page Annotation

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor with permission
**Goal:** Mark an annotation as handled without deleting the original feedback record.

### Main Flow

```text
User opens the chapter/page workspace
→ User reviews an unresolved annotation
→ Related work is corrected, usually through a newer ChapterPageVersion or accepted task output for the same logical page derived from linked regions
→ Authorized resolver confirms the issue has been handled
→ Backend calls the annotation resolve workflow procedure
→ Database verifies annotation exists and is not already resolved
→ Database derives owning series/page context through linked PageRegion records
→ Database resolves the annotation creator's current role through annotated_by_user_id
→ Database resolves the resolver's current role through actor_user_id
→ Database verifies the resolver account is ACTIVE and an active contributor for the owning series
→ If the annotation was created by a Mangaka:
    Database allows resolution by an active Mangaka contributor or active Tantou Editor contributor on the same series
→ If the annotation was created by a Tantou Editor:
    Database allows resolution only by an active Tantou Editor contributor on the same series
→ Database sets resolved_at_utc = SYSUTCDATETIME()
→ Database sets resolved_by_user_id = resolver user ID
→ Database writes CHAPTER_PAGE_ANNOTATION_RESOLVED audit event
→ UI marks the annotation as resolved while preserving it for history
```

### Database Procedure(s)

```text
manga.usp_ChapterPageAnnotation_Resolve
audit.usp_AuditEvent_Append
```

### Important Notes

- `resolved_at_utc` and `resolved_by_user_id` work as a pair.
- If the annotation is unresolved, both resolution fields are `NULL`.
- If the annotation is resolved, both resolution fields must be non-null.
- The table constraint `ck_chapter_page_annotation_resolved_pair` enforces that pair rule.
- Resolving an annotation does not delete the annotation.
- Resolved annotations remain useful for traceability, review history, and explaining why a later page version was created.
- Many visual/content issues should normally be resolved only after a corrected page version or accepted output exists.
- Some annotations may be resolved without a new page version if the reviewer decides the feedback is no longer applicable, duplicated, or intentionally accepted.
- Mangaka users may resolve Mangaka-created annotations on the same series.
- Mangaka users must not resolve Tantou Editor-created annotations.
- Tantou Editors assigned/active on the same series may resolve both Mangaka-created and Tantou Editor-created annotations.
- The MVP does not add a new annotation-origin column; stored procedures should enforce this using `annotated_by_user_id`, current user roles, active series contributor membership, and the owning series derived from linked regions.

### System Should Try To

- Make unresolved feedback visible and actionable.
- Preserve resolved feedback as workflow history.
- Avoid deleting annotation records just because the issue was fixed.
- Keep resolution accountable by recording who resolved it and when.
- Keep editorial feedback authority with Tantou Editors while still allowing Mangaka production tracking.

---

## BF-PAGE-005A — Update Page Annotation Text

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor with permission
**Goal:** Correct or clarify unresolved annotation text without changing linked regions or deleting feedback history.

### Main Flow

```text
User opens the chapter/page workspace
→ User selects an unresolved annotation
→ User edits annotation text and optionally enters an update reason
→ Backend calls the annotation text update workflow procedure
→ Database verifies annotation exists and is unresolved
→ Database derives owning series/page context through linked PageRegion records
→ Database resolves the annotation creator's current role through annotated_by_user_id
→ Database resolves the actor's current role through actor_user_id
→ Database verifies the actor account is ACTIVE and an active contributor for the owning series
→ If the annotation was created by a Mangaka:
    Database allows update by an active Mangaka contributor or active Tantou Editor contributor on the same series
→ If the annotation was created by a Tantou Editor:
    Database allows update only by an active Tantou Editor contributor on the same series
→ Database updates annotation_text
→ Database writes CHAPTER_PAGE_ANNOTATION_TEXT_UPDATED audit event with old text, new text, actor, and optional reason
→ UI refreshes the annotation text
```

### Database Procedure(s)

```text
manga.usp_ChapterPageAnnotation_UpdateText
audit.usp_AuditEvent_Append
```

### Important Notes

- This MVP flow updates only `ChapterPageAnnotation.annotation_text`.
- It must not change linked `PageRegion` records.
- It must not delete or recreate the annotation row.
- Resolved annotations should not be edited in MVP.
- Mangaka users may update unresolved Mangaka-created annotations on the same series.
- Mangaka users must not update Tantou Editor-created annotations.
- Tantou Editors assigned/active on the same series may update unresolved annotations created by either Mangaka users or Tantou Editors.
- The procedure is a database guard check; no new annotation-origin column is added for MVP.
- Audit should include old text and new text because annotation text is part of workflow feedback history.

### System Should Try To

- Let Mangaka correct production-tracking notes before or during preparation.
- Let Tantou Editors clarify Mangaka-created notes during chapter review.
- Preserve audit traceability for text changes.
- Avoid silently overwriting feedback history.

## BF-PAGE-005B — Soft Delete Chapter Page from Active Draft

**Status:** Agreed  
**Primary actor:** Mangaka / authorized chapter page manager  
**Goal:** Remove a logical `ChapterPage` from the active chapter draft without deleting its historical page-version records.

### Main Flow

```text
User opens an editable chapter in the chapter/page workspace
→ User selects a logical ChapterPage
→ User chooses Remove Page / Soft Delete Page
→ Backend validates actor permission and chapter editability
→ Backend marks the ChapterPage as soft-deleted
→ Backend preserves all related ChapterPageVersion records
→ Backend preserves related PageRegion, annotation, task, file, and audit history
→ UI hides the soft-deleted page from normal active draft navigation
```

### Important Notes

- `ChapterPage` is a logical page slot, so removing it from an active draft should use soft deletion.
- Soft-deleting a `ChapterPage` must not delete its historical `ChapterPageVersion` rows.
- If a page already has task, annotation, or review relevance, the history remains available through historical/admin views.
- Page version deletion is not a normal user action in the current MVP.

### System Should Try To

- Let Mangaka clean up active draft structure without losing history.
- Keep page-version and feedback history intact.
- Avoid hard-deleting logical page slots that may already be referenced by workflow history.

---

## BF-PAGE-006 — Create Chapter Page Task Linked to Page Regions

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Create a page task for an active Assistant contributor and link the task to one or more `PageRegion` records while normal production is eligible.

### Main Flow

```text
Mangaka opens the chapter/page workspace
→ Mangaka selects one or more existing saved PageRegion records for the task target
→ If the task applies to the whole page, UI/backend uses a full-page PageRegion for the selected ChapterPageVersion
→ Mangaka enters assigned Assistant, task type, title, description, priority, due date, and compensation amount
→ Backend normalizes and deduplicates the selected PageRegion IDs
→ Backend loads authoritative PageRegion → ChapterPageVersion → ChapterPage → Chapter → Series context
→ Backend verifies all referenced PageRegion rows exist and belong to the same ChapterPageVersion
→ Application verifies the creator is an ACTIVE Mangaka and an active Mangaka contributor for the owning series
→ Application verifies the assigned user is an ACTIVE Assistant and an active Assistant contributor for the owning series
→ Application verifies the owning Series.status_code is SERIALIZED or HIATUS
→ Application verifies the owning Chapter.status_code is DRAFT or REVISION_REQUESTED
→ Backend creates one manga.ChapterPageTask row with status_code = ASSIGNED
→ Backend creates one or more manga.ChapterPageTaskRegion rows
→ Backend writes CHAPTER_PAGE_TASK_CREATED audit event in the same transaction
→ Backend commits atomically
→ UI shows the new task with its linked target regions
```

### Page Region IDs

A task must always provide at least one page region ID. Duplicate IDs should be normalized so each task-region link is created once.

Multiple linked regions are allowed, but every linked region must resolve to the same `ChapterPageVersion`.

### Current Application Persistence Path

```text
Application service validation/orchestration
→ EF Core task + task-region persistence
→ audit.usp_AuditEvent_Append
→ one transaction / commit
```

`manga.usp_ChapterPageTask_Create` may remain in legacy/bootstrap SQL, but it is not the current application path for single-task creation.

### Database Tables

```text
manga.ChapterPageTask
manga.ChapterPageTaskRegion
manga.PageRegion
manga.ChapterPageVersion
manga.ChapterPage
manga.Chapter
manga.Series
```

### Important Notes

- `ChapterPageTask` stores the task header: assigned user, task type, status, title, description, priority, due date, compensation amount, completion output, creator, and timestamps.
- `ChapterPageTask` does not store `chapter_page_id` directly.
- `ChapterPageTaskRegion` links one task to one or more `PageRegion` records.
- Each task must link to at least one `PageRegion`.
- A whole-page task must still link to a `PageRegion`; the system should create or reuse a full-page region covering the selected `ChapterPageVersion`.
- All regions linked to the same task must belong to the same `ChapterPageVersion`.
- The task page context is derived through `ChapterPageTaskRegion → PageRegion → ChapterPageVersion → ChapterPage`.
- The task owning series is derived through `PageRegion → ChapterPageVersion → ChapterPage → Chapter → Series`.
- New task creation is allowed only when the parent series is `SERIALIZED` or `HIATUS`.
- New task creation is allowed only when the parent chapter is `DRAFT` or `REVISION_REQUESTED`.
- The creator must be an `ACTIVE` Mangaka account and an active Mangaka contributor for the owning series.
- The assignee must be an `ACTIVE` Assistant account and an active Assistant contributor for the owning series.
- `compensation_amount` is required, must be non-negative, and should use `0.00` when no compensation is paid.
- Compensation amount is task metadata only and does not introduce payroll, salary calculation, payment processing, or accounting features.
- Database constraints remain authoritative for structural rules such as valid status/type/priority values, compensation range, foreign keys, required columns, and junction uniqueness.
- When the task later reaches `UNDER_REVIEW` or `COMPLETED`, the completed page version must be checked against the same logical page derived from the task's linked regions.

### System Should Try To

- Keep task creation tied to visible page regions instead of hidden page-level assumptions.
- Reuse the same production-eligibility rules for single-task and Quick Select/batch task creation.
- Support one-region, multi-region, and whole-page task targets consistently.
- Avoid storing duplicate page context directly on `ChapterPageTask` when it can be derived through linked regions.
- Keep workflow validation in Domain/Application and persistence concerns in Infrastructure/database layers.
- Keep the task assignment workflow traceable through audit.

---
## BF-TASK-007 — Quick Select Batch Task Assignment

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Create multiple assigned tasks at once by selecting pages with their current versions, an assistant, task type, and common task defaults. Each task links to one whole-page `PageRegion`.

### Main Flow

```text
Mangaka opens the Quick Select dialog from the chapter workspace
→ Mangaka selects a series and chapter
→ UI loads available chapters for Quick Select
→ UI loads pages/current versions for the selected chapter
→ UI loads active Assistant contributors for the selected series
→ Mangaka selects one or more pages
→ Mangaka selects an Assistant
→ Mangaka enters task type, title prefix, default description, priority, due date, and compensation
→ Mangaka optionally overrides the description for individual pages
→ Mangaka clicks Confirm
→ Backend validates the full request
→ Backend loads page/version/file metadata
→ Backend resolves Cloudinary image dimensions for each selected page version
→ Backend builds a validated assignment plan
→ Backend opens a SQL transaction
→ Backend acquires a session+series+chapter scoped SQL app lock
→ Backend re-checks authoritative guards (actor, assistant, contributor membership, series, chapter, pages, versions, files)
→ Application verifies the creator is an ACTIVE Mangaka contributor
→ Application verifies the selected Assistant is ACTIVE and an active Assistant contributor
→ Application verifies the parent series is SERIALIZED or HIATUS
→ Application verifies the parent chapter is DRAFT or REVISION_REQUESTED
→ Backend finds or creates one FULL_PAGE PageRegion per selected page version
→ Backend creates one ASSIGNED ChapterPageTask per selected page
→ Backend links each task to its FULL_PAGE PageRegion through ChapterPageTaskRegion
→ Backend writes one CHAPTER_PAGE_TASK_CREATED audit event per task
→ Backend commits the transaction
→ UI reloads the task list and shows created tasks
```

### Database Tables

```text
manga.ChapterPageTask
manga.ChapterPageTaskRegion
manga.PageRegion
audit.AuditEvent
```

### Important Notes

- Quick Select creates multiple new ASSIGNED tasks in one batch.
- One task per selected page/current page version.
- User selects pages, not regions.
- Backend creates or reuses one FULL_PAGE PageRegion per selected page version.
- FULL_PAGE dimensions come from Cloudinary via IImageMetadataProvider.
- FileResource does not store image dimensions.
- Each task links to its FULL_PAGE region.
- Application validates the whole batch before persistence.
- Quick Select uses the same creator, assignee, parent-series, and parent-chapter production-eligibility rules as single-task creation.
- Infrastructure persists with EF batch insert and one SaveChangesAsync.
- Transaction/app-lock prevents overlapping writes for the same actor+series+chapter.
- Rollback prevents partial tasks, regions, or audit rows.
- Audit writes one CHAPTER_PAGE_TASK_CREATED event per created task.
- AuditEvent.entity_type = ChapterPageTask.
- AuditEvent.entity_id = real created ChapterPageTask ID.
- detail_json matches the existing single-task stored procedure audit JSON shape.
- No stored procedure is called for this workflow.
- Cloudinary image bounds are resolved before opening the SQL transaction.

### System Should Try To

- Let Mangaka assign multiple tasks quickly without creating each one individually.
- Keep task creation consistent with single-task creation semantics.
- Reuse existing FULL_PAGE regions instead of creating duplicates.
- Prevent partial batch creation on failure.
- Keep audit trail complete and traceable per task.



## BF-CH-001 — Chapter Editorial Review Decision

**Status:** Agreed
**Primary actor:** Tantou Editor
**Goal:** Record the final chapter-level editorial decision while preserving page-level annotations and review history.

### Main Flow

```text
Tantou Editor opens the submitted chapter review queue
→ Editor opens the chapter workspace/detail for a chapter in UNDER_REVIEW
→ Editor reviews current active page versions, regions, annotations, and any supporting context
→ Editor chooses one decision: APPROVED / REVISION_REQUESTED / CANCELLED
→ If decision is REVISION_REQUESTED or CANCELLED, editor enters non-blank comments
→ Editor may optionally attach an EDITORIAL_ATTACHMENT markup file
→ Backend validates actor permission, chapter status, required comments, optional markup file, and planned release date rules when needed
→ Backend creates manga.ChapterEditorialReview
→ Backend updates manga.Chapter.status_code according to the decision
→ If APPROVED and Chapter.planned_release_date is empty, Chapter.status_code becomes APPROVED
→ If APPROVED and Chapter.planned_release_date already exists and is not in the past, Chapter.status_code becomes SCHEDULED
→ Backend writes the chapter review audit event and the scheduling audit detail when approval moves the chapter to SCHEDULED
→ UI refreshes the chapter status and review history
```

### Decision Outcomes

| Decision | Chapter status after review | Meaning |
|---|---|---|
| `APPROVED` with no planned release date | `APPROVED` | The chapter is accepted but not yet scheduled. |
| `APPROVED` with a future planned release date | `SCHEDULED` | The chapter is accepted and locked for planned release. |
| `REVISION_REQUESTED` | `REVISION_REQUESTED` | The same chapter attempt can be edited and resubmitted with new page versions. |
| `CANCELLED` | `CANCELLED` | The current chapter attempt is a hard stop and becomes read-only historical reference. |

### Important Notes

- `REVISION_REQUESTED` and `CANCELLED` both require non-blank comments.
- Markup files are optional for both `REVISION_REQUESTED` and `CANCELLED`.
- Page-specific annotations remain separate from the final chapter-level review decision.
- `CANCELLED` is not a normal fix-and-resubmit outcome. Use `REVISION_REQUESTED` when the chapter can still be fixed.
- Scheduling is chapter-level; this flow must never set `Series.status_code = SCHEDULED`.
- If approval moves a chapter to `SCHEDULED`, Mangaka/page content mutation workflows become locked.
- Publication frequency provides suggestions and warnings, not hard scheduling enforcement.

### System Should Try To

- Make the difference between approved-but-unscheduled and scheduled clear in the UI.
- Preserve review decisions and page-level context.
- Avoid deleting historical chapter materials.
- Explain scheduled lock behavior to Mangaka users.

---

## BF-CH-002 — Create Replacement Chapter After Cancellation

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow a Mangaka to redo a cancelled chapter as a new chapter draft using the same chapter number label, while preserving the cancelled attempt as history.

### Main Flow

```text
Mangaka opens a cancelled chapter
→ UI shows the cancelled chapter as read-only
→ UI explains that the current chapter attempt cannot be edited or resubmitted
→ Mangaka chooses Create Replacement Draft
→ Backend validates actor is an active Mangaka contributor for the series
→ Backend validates the source chapter status is CANCELLED
→ Backend creates a new manga.Chapter row for the same series and chapter_number_label
→ New chapter starts with status_code = DRAFT
→ Mangaka creates pages/uploads page versions under the new chapter
→ Cancelled chapter pages, page versions, regions, annotations, files, and reviews remain preserved as read-only historical reference
```

### Database Behavior

```text
Old unique constraint on (series_id, chapter_number_label) is replaced by a filtered unique index:
unique among non-cancelled chapters only
```

### Important Notes

- No `replacement_of_chapter_id` relationship is required in MVP.
- A cancelled chapter does not reserve its chapter number label.
- Page number uniqueness remains scoped to each chapter, so the replacement chapter can create its own pages starting from page 1.
- The old cancelled chapter should not be edited, resubmitted, approved, scheduled, or released.

### System Should Try To

- Preserve the cancelled attempt for traceability.
- Make redo work explicit by creating a new chapter draft.
- Avoid accidental edits to cancelled chapter materials.

---

## BF-CH-003 — Create Chapter Draft for an Eligible Production Series

**Status:** Agreed
**Primary actor:** Mangaka contributor
**Goal:** Create a new chapter draft only while the parent series is in a normal production state.

### Main Flow

```text
Active Mangaka contributor opens an eligible series
→ Mangaka chooses Add Chapter
→ Backend loads the authoritative parent series
→ Application validates the actor is an ACTIVE Mangaka and an active Mangaka contributor for the series
→ Application validates Series.status_code is SERIALIZED or HIATUS
→ Backend validates the requested chapter data and database-backed uniqueness constraints
→ Backend creates the Chapter with status_code = DRAFT
→ Backend writes the required chapter-creation audit event
→ Backend commits the chapter creation workflow
→ UI refreshes the chapter list
```

### Important Notes

- New chapter creation is allowed only for parent series in `SERIALIZED` or `HIATUS`.
- Proposal/review states, `COMPLETED`, `CANCELLED`, null, or unknown series states do not allow normal chapter creation.
- `HIATUS` still allows chapter creation; it blocks final chapter release only.
- The actor must be an `ACTIVE` Mangaka account and an active Mangaka contributor for the parent series.
- Database constraints remain authoritative for structural rules such as foreign keys, required fields, and chapter-number uniqueness.

### System Should Try To

- Keep chapter creation aligned with normal series production eligibility.
- Keep business validation in Domain/Application and database structural integrity in the database.
- Preserve existing chapter-number and cancellation-history behavior.

---

## BF-CH-004 — Submit Chapter for Editorial Review With Active-Task Gate

**Status:** Agreed  
**Primary actor:** Mangaka contributor  
**Goal:** Submit a stable chapter for Tantou Editor review only after all active Assistant page-task work for that chapter has been resolved.

### Main Flow

```text
Active Mangaka contributor opens a chapter in DRAFT or REVISION_REQUESTED
→ Mangaka chooses Submit for Editorial Review
→ Backend enters the chapter-scoped concurrency-safe mutation path
→ Backend loads the authoritative chapter state
→ Backend validates the actor is an ACTIVE Mangaka and an active Mangaka contributor of the owning series
→ Backend validates Chapter.status_code is DRAFT or REVISION_REQUESTED
→ Backend resolves page tasks associated with this chapter through:
   ChapterPageTask → PageRegions → ChapterPageVersion → ChapterPage → Chapter
→ Backend deduplicates by ChapterPageTaskId
→ Backend treats ASSIGNED and UNDER_REVIEW as active/blocking task statuses
→ If one or more distinct active tasks exist:
   → Backend rejects submission with a clean user-facing validation response
   → Chapter.status_code remains unchanged
   → No successful chapter-submission audit event is written
   → No CHAPTER_REVIEW notification is created
   → No task is automatically completed, cancelled, reassigned, or otherwise mutated
→ If zero distinct active tasks exist:
   → Backend continues the existing submission validations
   → Backend validates the required active Tantou Editor review recipients/context
   → Chapter.status_code changes to UNDER_REVIEW
   → Backend writes the normal chapter-submission audit event
   → Backend creates CHAPTER_REVIEW notifications for the correct distinct active Tantou Editor contributors of the exact series
   → Backend commits the submission workflow
→ UI refreshes the chapter state or shows the clean validation message returned by the backend
```

### Blocking Task Statuses

| Task status | Blocks chapter submission? |
|---|---|
| `ASSIGNED` | Yes |
| `UNDER_REVIEW` | Yes |
| `COMPLETED` | No |
| `CANCELLED` | No |

### Important Notes

- The backend is authoritative for this validation even if the UI already has task information loaded.
- One task linked to multiple `PageRegion` rows still counts as one task because validation is distinct by `ChapterPageTaskId`.
- `ChapterPageTask` has no direct `ChapterId` or `ChapterPageId`; chapter association is derived through linked regions and page versions.
- This flow blocks submission only; it must not automatically resolve active tasks.
- The Mangaka must complete, cancel, or otherwise resolve active tasks through the existing task-management workflow before retrying chapter submission.
- Task creation and chapter submission for the same chapter must use a concurrency-safe coordination rule so a new `ASSIGNED` task cannot commit concurrently with the chapter entering `UNDER_REVIEW`.

### System Should Try To

- Prevent Tantou Editors from receiving chapters whose Assistant production work is still active.
- Give the Mangaka a clear reason when submission is blocked.
- Preserve task history instead of changing task state automatically.
- Keep successful submission audit and notification behavior unchanged.

---

## BF-PUB-001 — Plan or Reschedule Chapter Release Date

**Status:** Agreed  
**Primary actor:** Mangaka / Tantou Editor  
**Goal:** Set or update a chapter planned release date while treating publication frequency as an advisory suggestion rather than a hard scheduling constraint.

### Main Flow

```text
User opens a chapter planning/review/schedule screen
→ UI shows the chapter status, current planned_release_date, released_at_utc if available, and Series.publication_frequency_code
→ UI may suggest a default date from the series frequency
→ User chooses a planned release date
→ UI asks for confirmation before changing the schedule
→ Backend validates actor permission and chapter status
→ Backend validates the planned release date is not in the past
→ Backend may produce a warning if the date does not match the advisory frequency pattern
→ Backend saves Chapter.planned_release_date
→ If the chapter was APPROVED, backend changes Chapter.status_code to SCHEDULED
→ If the chapter is still DRAFT or REVISION_REQUESTED, backend keeps the current editable/plannable status
→ Backend writes an audit event for planned date set/rescheduled and status change when applicable
→ If the chapter newly enters SCHEDULED, or was already SCHEDULED and its normalized planned date changed, backend creates PUBLICATION_SCHEDULE notifications for all other distinct active contributors of the exact series except the initiating actor
→ If the chapter remains DRAFT, REVISION_REQUESTED, or UNDER_REVIEW after only setting/changing the planned date, or if a SCHEDULED chapter keeps the same normalized date, backend creates no PUBLICATION_SCHEDULE notification
→ UI refreshes the schedule display
```

### Advisory Default Date Rules

| Frequency | Suggested default |
|---|---|
| `WEEKLY` | Same weekday in the next week when a useful reference date exists. |
| `MONTHLY` | Same day number in the next month when possible; otherwise the last valid day of the next month. |
| `IRREGULAR` | No strict default is required; UI may suggest a convenient future date. |
| `NULL` | No strict default is required; UI may show that the official release approach is not decided. |

### Hard Validation Rules

| Rule | Behavior |
|---|---|
| Planned release date is in the past | Block. |
| Chapter is `CANCELLED` or `RELEASED` | Block schedule/reschedule. |
| Actor lacks permission | Block. |
| Date does not match `Series.publication_frequency_code` suggestion | Warn only; allow authorized user to continue. |
| Multiple chapters share the same planned release date | Allow, because bulk/catch-up releases may be intentional. |

### Important Notes

- Scheduling is chapter-level and must not change `Series.status_code`.
- `Series.publication_frequency_code` is an advisory planning label. It may drive suggestions and warnings, but it must not hard-block normal scheduling.
- Mangaka and Tantou Editors may both schedule/reschedule chapters when the chapter status and permissions allow it.
- This MVP assumes Mangaka and Editor coordinate release dates outside the system. The system provides visibility, audit trail, and contributor contact visibility where authorized, but it does not resolve scheduling disputes.
- For scheduled chapters, the business publication date is usually `Chapter.planned_release_date`.
- For released chapters, the release business date is derived from `released_at_utc` converted to Vietnam publication time (UTC+7), then taking the date part.
- Frequency mismatch warnings should explain the suggested pattern without preventing the user from saving.

### System Should Try To

- Keep scheduling flexible enough for bulk releases, catch-up releases, vacations, breaks, and campaign-driven release plans.
- Give helpful defaults without turning frequency into a hard rule.
- Preserve schedule changes in audit so users can see who changed what and when.
- Keep terminal chapter states protected.

---

## BF-PUB-002 — Put a Scheduled Chapter On Hold or Return It to Schedule

**Status:** Agreed  
**Primary actor:** Tantou Editor  
**Goal:** Allow an editor to pause a scheduled chapter and later return it to a new schedule date without reopening page/content mutation workflows.

### Put On Hold Flow

```text
Tantou Editor opens a chapter with status SCHEDULED
→ Editor chooses Put On Hold
→ UI requires a non-blank operational/editorial reason
→ UI asks for confirmation
→ Backend validates actor permission, Chapter.status_code = SCHEDULED, and non-blank reason
→ Backend changes Chapter.status_code to ON_HOLD
→ Backend suspends the active release plan and requires a new future planned_release_date before the chapter can return to SCHEDULED
→ Backend preserves the old planned date in audit details
→ Backend writes CHAPTER_PUT_ON_HOLD audit detail with reason and old/new status/date values
→ UI refreshes the chapter as read-only/on-hold
```

### Return to Schedule Flow

```text
Tantou Editor opens a chapter with status ON_HOLD
→ Editor chooses Return to Schedule / Schedule Again
→ Editor selects a new planned release date that is not in the past
→ UI asks for confirmation
→ Backend validates actor permission, Chapter.status_code = ON_HOLD, and future planned release date
→ Backend sets Chapter.planned_release_date to the new date
→ Backend changes Chapter.status_code to SCHEDULED
→ Backend writes audit detail with old/new status and date values
→ UI refreshes the chapter as scheduled
```

### Important Notes

- `ON_HOLD` means the previous release plan is suspended.
- A chapter returning from `ON_HOLD` to `SCHEDULED` must receive a new future planned release date.
- Mangaka users cannot edit chapter content or perform page/content mutation workflows while the chapter is `SCHEDULED` or `ON_HOLD`.
- Blocked workflows include page creation, page deletion, page-version upload, assistant task output submission that creates or changes page content, and other saved page/content mutations.
- Automatic movement of overdue scheduled chapters to `ON_HOLD` is deferred. The MVP may show an overdue warning, but it should not auto-hold chapters unless a later workflow explicitly implements it.

### System Should Try To

- Keep hold reasons traceable.
- Make it clear that on-hold chapters are paused, not cancelled.
- Avoid using normal read-only pages as hidden write triggers.
- Require an explicit new date before returning a held chapter to schedule.

---

## BF-PUB-003 — Release a Chapter

**Status:** Agreed  
**Primary actor:** Tantou Editor  
**Goal:** Mark a chapter as released using the editor as the final publication enforcer.

### Main Flow

```text
Tantou Editor opens a publication schedule or chapter review screen
→ Editor chooses Release Chapter / Release Now
→ UI shows a confirmation dialog explaining the status and timestamp changes
→ Backend validates actor permission and chapter eligibility
→ If Chapter.planned_release_date is empty, backend sets it to the current publication business date
→ If Chapter.planned_release_date already exists, backend preserves it for planned-vs-actual comparison
→ Backend sets Chapter.released_at_utc to the current UTC timestamp
→ Backend sets Chapter.status_code to RELEASED
→ Backend writes CHAPTER_RELEASED audit detail
→ UI refreshes the release calendar and chapter status
```

### Important Notes

- Releasing a chapter is an explicit editor action. The MVP does not automatically release chapters when the planned date arrives.
- Releasing should ask for confirmation.
- Preserving an existing planned date helps compare planned release timing against actual release time.
- Release action should be blocked for `CANCELLED` and already `RELEASED` chapters.
- Release action should also be blocked when the parent series is `HIATUS`, `COMPLETED`, or `CANCELLED`; a `HIATUS` series must be resumed to `SERIALIZED` before chapter release.
- Bulk release may be supported later; when implemented, it should ask for confirmation and audit each affected chapter.

### System Should Try To

- Make the release action deliberate and traceable.
- Keep planned-vs-actual release information useful.
- Avoid automatic release behavior without explicit team approval.

---

## BF-RANK-001 — Enter Series Vote Input for a Weekly Publication Period

**Status:** Agreed  
**Primary actor:** Editorial Board Member / Editorial Board Chief  
**Goal:** Enter simulated or manually aggregated series-level performance data for a completed weekly publication period.

### Main Flow

```text
Board user opens Ranking Input screen
→ User selects a WEEKLY PublicationPeriod
→ UI lists eligible series
→ User selects a series
→ User enters rating_count, average_rating, reading_count, and optional data_source_note
→ Backend validates actor role, period/series uniqueness, and numeric constraints
→ Backend saves or updates manga.SeriesVoteInput
→ UI refreshes the input list and ranking view
```

### Database Tables / View

```text
manga.PublicationPeriod
manga.SeriesVoteInput
manga.vw_SeriesRanking
```

### Important Notes

- There is no public reader voting module in MVP.
- Vote input is series-level, not chapter-level.
- A series can have only one `SeriesVoteInput` row per `PublicationPeriod`.
- `rating_count` and `reading_count` must be greater than zero.
- `average_rating` must be between 0 and 10.
- `rating_count` must not exceed `reading_count`.
- Manual MVP `SeriesVoteInput` is entered for `WEEKLY` publication periods only. Monthly/yearly/all-time ranking views are derived from weekly source evidence and do not require separately persisted manual aggregate input rows.
- Vote input is period-only. Later weekly input must not include earlier weekly input.
- `data_source_note` should explain the report or source used for the manual input when relevant.

### System Should Try To

- Make manual input traceable through entered/updated user and UTC timestamps.
- Keep vote input simple enough for demo data while preserving clear semantics.
- Prevent duplicate input for the same series and publication period.

---

## BF-RANK-002 — View Dynamic Series Ranking

**Status:** Agreed  
**Primary actor:** Editorial Board Member / Editorial Board Chief / Tantou Editor / Mangaka  
**Goal:** View the current dynamic ranking for a selected publication period without using finalized ranking storage.

### Main Flow

```text
User opens ranking screen
→ UI selects or filters by PublicationPeriod
→ Backend queries manga.vw_SeriesRanking for the selected publication_period_id
→ Database computes ranking_score and DENSE_RANK by publication_period_id
→ Backend returns ranked series rows
→ UI displays rank, title, rating count, average rating, reading count, and ranking score
```

### Ranking Formula

```text
ranking_score =
    (v / (v + m)) * R
    +
    (m / (v + m)) * C

R = series average_rating for the effective ranking scope
v = series rating_count for the effective ranking scope
C = SUM(average_rating * rating_count) / SUM(rating_count)
    across all eligible ranked series in the same effective ranking scope
m = median rating_count across all eligible ranked series
    in the same effective ranking scope
```

### Important Notes

- Ranking is dynamic and reflects the latest `SeriesVoteInput` data.
- `reading_count` does not directly increase `ranking_score`; it remains popularity/readership evidence and may remain a deterministic tie-breaker after score, average rating, and rating count.
- The weighted score stays on the same 0-to-10 rating scale because it is a weighted combination of `R` and `C`.
- For a direct weekly ranking, `C` and `m` are recalculated within that weekly `publication_period_id`.
- For broader derived scopes such as monthly or all-time/yearly reporting, weekly source evidence must be aggregated by series first; then the broader scope must recalculate its own `R`, `v`, `C`, and `m`. Do not average weekly `ranking_score` values and do not reuse weekly `C` or `m`.
- The MVP does not use `SeriesRankingSnapshot` because there is no ranking finalization workflow.
- The ranking view should keep technical IDs available for backend filtering and navigation even if the UI hides them.
- Ranking evidence may support board/editorial review but does not automatically cancel a series.
- Completed series remain visible in dynamic rankings when vote input exists for the selected publication period.

### System Should Try To

- Query direct weekly ranking by `publication_period_id`, not by period name.
- Keep one authoritative weighted-score formula across SQL and any broader-scope application/C# aggregation.
- Recalculate broader-scope statistics after aggregation rather than averaging lower-level ranking scores.
- Avoid storing duplicated ranking score or rank position unless later performance profiling proves caching is required.


---


# 7. Notification Flows

> **Alignment status — 2026-07-21:** The following flows record the approved notification contracts. `RANKING_WARNING` is finalized as a hybrid, persistence-based weekly warning rule using the documented `6.5` weighted-score baseline together with bottom-25% relative performance.

## BF-NOTIF-001 — Account Approved Notification and Email

**Status:** Agreed  
**Primary actor:** Admin  
**Goal:** Inform a pending user when their account is approved.

```text
Admin approves/activates a PENDING_APPROVAL user
→ Account status becomes ACTIVE
→ System creates ACCOUNT_APPROVED for the approved user
→ System sends an approval email to the approved user's email address
→ Account status/audit/notification follow the established approval persistence path
→ Email is a separate delivery channel in addition to the in-app notification
```

---

## BF-NOTIF-002 — Proposal Submitted for Editorial Review

**Status:** Agreed  
**Primary actor:** Mangaka  
**Goal:** Notify only the Tantou Editors who actively contribute to the submitted proposal's series.

```text
Mangaka submits proposal
→ Proposal enters editorial review workflow
→ System creates PROPOSAL_REVIEW
→ Recipients = distinct active Tantou Editor contributors of the exact series
→ Unrelated Tantou Editors do not receive it
→ Related entity = submitted SeriesProposal
```

---

## BF-NOTIF-003 — Proposal Editorial Decision

**Status:** Agreed  
**Primary actor:** Tantou Editor  
**Goal:** Inform the affected series' Mangaka contributors of a proposal decision.

```text
Tantou Editor records Request Revision / Pass To Board / Cancel Proposal
→ System creates PROPOSAL_DECISION
→ Recipients = distinct active Mangaka contributors of the affected series
→ Related entity = affected SeriesProposal
→ Notification content reflects the decision
```

---

## BF-NOTIF-004 — New Board Poll

**Status:** Agreed  
**Primary actor:** Editorial Board Chief  
**Goal:** Inform board voters when a real poll opens.

```text
Editorial Board Chief opens a real board poll
→ System creates BOARD_POLL
→ Recipients = all active users whose exact role is Editorial Board Member
→ Initiating Chief is excluded
→ Duplicate recipient IDs are removed
→ Related entity = new SeriesBoardPoll
```

---

## BF-NOTIF-005 — Board Decision / Poll Cancellation

**Status:** Agreed  
**Primary actor:** Editorial Board Chief  
**Goal:** Inform the affected production team when a poll ends or is manually cancelled.

```text
Board poll closes with APPROVED / REJECTED / NO_DECISION
OR Editorial Board Chief manually cancels the poll
→ System creates BOARD_DECISION
→ Recipients = all distinct active contributors of the exact affected series
→ Related entity = affected SeriesBoardPoll
→ Notification content reflects approved / rejected / no-decision / cancelled outcome
```

Manual cancellation invalidates the poll for workflow-result application, but it still creates the user-facing `BOARD_DECISION` notification so active contributors know that the poll was cancelled.

---

## BF-NOTIF-006 — Task Assignment and Reassignment

**Status:** Agreed  
**Primary actor:** Mangaka  
**Goal:** Keep Assistants aware of new assignments and reassignment changes.

```text
Single task created
→ TASK_ASSIGNMENT to assigned Assistant

Quick Select creates N tasks
→ One TASK_ASSIGNMENT per created task to that task's assigned Assistant

Task reassigned
→ Original task becomes CANCELLED
→ Replacement task becomes ASSIGNED
→ Original Assistant receives TASK_ASSIGNMENT titled as Task Reassigned
→ Original Assistant message includes required reassignment reason
→ Original Assistant notification links to original task
→ Replacement Assistant receives TASK_ASSIGNMENT for replacement task
→ Replacement Assistant notification links to replacement task
→ Task changes, required audit events, and both notifications share the established transaction boundary
```

---

## BF-NOTIF-007 — Assistant Task Submitted for Review

**Status:** Agreed  
**Primary actor:** Assistant  
**Goal:** Tell the series' Mangaka contributors that submitted Assistant work is ready for review.

```text
Assistant submits task work
→ Task transitions ASSIGNED → UNDER_REVIEW
→ System creates TASK_REVIEW
→ Recipients = distinct active Mangaka contributors of the exact series
→ Related entity = submitted ChapterPageTask
```

`TASK_REVIEW` means that Assistant work needs Mangaka review; it is not the notification type for Mangaka approval/rework results.

---

## BF-NOTIF-008 — Chapter Submitted for Editorial Review

**Status:** Agreed  
**Primary actor:** Mangaka  
**Goal:** Notify the correct series-scoped editors when a chapter needs review.

```text
Mangaka attempts to submit chapter from DRAFT or REVISION_REQUESTED
→ Backend first validates that zero distinct ASSIGNED/UNDER_REVIEW page tasks are associated with that chapter
→ If active tasks exist, submission is blocked and no CHAPTER_REVIEW notification is created
→ If validation passes, Chapter transitions to UNDER_REVIEW
→ System creates CHAPTER_REVIEW
→ Recipients = distinct active Tantou Editor contributors of the exact series
→ Unrelated Tantou Editors do not receive it
→ Related entity = Chapter
```

---

## BF-NOTIF-009 — Chapter Editorial Decision

**Status:** Agreed  
**Primary actor:** Tantou Editor  
**Goal:** Inform the affected series' Mangaka contributors of the chapter decision.

```text
Tantou Editor records Approved / Revision Requested / Cancelled
→ System creates CHAPTER_DECISION
→ Recipients = distinct active Mangaka contributors of the affected series
→ Related entity = Chapter
→ Notification content reflects the decision
```

If approval also moves the chapter into `SCHEDULED`, the same business action may also satisfy the separate `PUBLICATION_SCHEDULE` trigger; each notification represents a different business event and must follow its own recipient rule.

---

## BF-NOTIF-010 — Publication Scheduled or Rescheduled

**Status:** Agreed  
**Primary actor:** Mangaka / Tantou Editor  
**Goal:** Inform the rest of the active series team when a chapter becomes scheduled or an existing schedule changes.

### Notify

```text
Old status != SCHEDULED
AND new status == SCHEDULED
→ Create PUBLICATION_SCHEDULE

OR

Old status == SCHEDULED
AND new status == SCHEDULED
AND old normalized planned_release_date != new normalized planned_release_date
→ Create PUBLICATION_SCHEDULE
```

Recipients:

```text
Distinct active contributors of the exact affected series
MINUS the initiating actor
```

### Do Not Notify

```text
DRAFT receives/changes planned date and remains DRAFT
→ No PUBLICATION_SCHEDULE

REVISION_REQUESTED receives/changes planned date and remains REVISION_REQUESTED
→ No PUBLICATION_SCHEDULE

UNDER_REVIEW receives/changes planned date and remains UNDER_REVIEW
→ No PUBLICATION_SCHEDULE

SCHEDULED is saved with the same normalized planned date
→ No PUBLICATION_SCHEDULE
```

This includes real transitions such as `APPROVED → SCHEDULED`, `ON_HOLD → SCHEDULED`, and editorial approval that moves `UNDER_REVIEW → SCHEDULED` because a valid planned date already exists.

---

## BF-NOTIF-011 — Ranking Warning Contract

**Status:** Agreed  
**Primary actor:** System  
**Recipients:** All distinct active contributors of the exact affected series  
**Goal:** Warn the active production team when a currently cancellable series shows persistent low weekly ranking performance without treating relative rank alone as failure.

### Weekly Failure Checks

A weekly ranking result counts as a **failed ranking week** only when **both** checks fail:

```text
Check 1 — Absolute weighted-score check
ranking_score < 6.5

AND

Check 2 — Relative rank check
series is in the bottom 25% of ranked series
for that same completed weekly publication period
```

For the bottom-25% check:

```text
low_group_size = CEILING(total_ranked_series * 0.25)

series is in the low group when:
rank_position > total_ranked_series - low_group_size
```

Each evaluated week must contain at least 4 ranked series. The `6.5` value is the approved MVP low-score baseline for warning evaluation; it is an editorial warning threshold, not a statistical prediction that cancellation will occur.

### Persistent High-Risk Rule

```text
System evaluates the latest 3 consecutive completed WEEKLY publication periods
→ Series must have ranking input in all 3 periods
→ Each period must have at least 4 ranked series
→ Determine whether the series failed both weekly checks in each period
→ High ranking risk exists only when:
     failed ranking week count >= 2 of the latest 3
     AND the latest completed week is itself a failed ranking week
→ If high ranking risk exists and the series is currently SERIALIZED or HIATUS:
     resolve all distinct active contributors of that exact series
     create one RANKING_WARNING per recipient for the evaluated latest week
```

### Recipient Rule

Recipients are **all distinct active contributors of the exact affected series, regardless of contributor role**. A recipient must:

- have an active `SeriesContributor` relationship (`end_date IS NULL`);
- have user status `ACTIVE`;
- belong to the exact affected series;
- be deduplicated by user ID.

### Evaluation / Deduplication

- Only completed weekly periods are authoritative for automatic warning evaluation; the current/incomplete week must not trigger a warning.
- Monthly, yearly, and all-time views are reporting/aggregation views and must not independently create additional `RANKING_WARNING` notifications.
- Re-evaluating the same series and latest completed weekly period must be idempotent: at most one `RANKING_WARNING` may be created per recipient for that series/evaluated week.
- Missing ranking input in any of the latest 3 consecutive completed weekly periods is insufficient evidence and must not be treated as failure.
- Completed or cancelled series may remain visible in ranking history but must not receive a new cancellation-risk warning; the warning applies to series that are currently `SERIALIZED` or `HIATUS`.

### Related Entity / Bell Behavior

The warning is series-scoped because the risk condition is derived from multiple weekly periods. The notification should relate to the affected `Series`, and Bell navigation should open the existing ranking/series context for that series without creating a parallel ranking workflow.

### Important Notes

- `RANKING_WARNING` is advisory only.
- It must not automatically change `Series.status_code`.
- It must not automatically open a `CANCEL_SERIALIZATION` poll.
- It must not automatically cancel or pause a series.
- Ranking-risk evidence may support a later board cancellation discussion, but normal board rules still control any cancellation decision.

---

## BF-NOTIF-012 — Read / Unread Notification Behavior

**Status:** Agreed  
**Primary actor:** General System User  
**Goal:** Let users track notification awareness without treating notifications as audit history.

```text
Notification created
→ read_at_utc = NULL
→ Bell shows unread state

User opens/marks notification as read
→ System records read_at_utc
→ Bell refreshes unread state
```

`SYSTEM_MESSAGE` remains generic/reserved and must not replace a more specific approved notification type.

---

# 8. Workflow Template for Future Additions

Use this template when adding new flows.

```md
## BF-AREA-XXX — Flow Name

**Status:** Draft / Agreed / Future
**Primary actor:** Actor Name
**Goal:** Short goal sentence.

### Main Flow

```text
Step 1
→ Step 2
→ Step 3
```

### Database Procedure(s)

```text
schema.procedure_name
```

### Important Notes

- Note 1
- Note 2
- Note 3

### System Should Try To

- Desired system behavior 1
- Desired system behavior 2
- Desired system behavior 3
```

---

# 9. Backlog: Flows To Add Later

Add detailed flows for these when the team finalizes them:

- Admin approval/rejection screen flow
- Series contributor assignment flow
- Tantou Editor proposal review flow
- Chapter creation flow
- Replacement chapter draft after cancellation flow
- Chapter page upload/versioning flow
- General page modification temporary download/save-as-version flow
- AI suggestion generation before saving PageRegion records
- Assistant task assignment and submission flow
- Board vote flow
- Cancel serialization poll flow
- Series vote input and dynamic ranking view flow
- Cloudinary cleanup failure handling flow