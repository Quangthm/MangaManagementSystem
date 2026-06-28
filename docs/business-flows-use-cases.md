# Manga Creation Workflow and Publishing Management System â€” Business Flows / Use Case Notes

> **Purpose:** Record agreed business flows in a clear step-by-step format so the team can understand how UI, backend, Cloudinary, SQL Server procedures, and audit behavior should work together.
>
> **Expansion note:** This file is meant to be continuously expanded when new workflows are agreed upon. Add new flows without rewriting unrelated existing flows.

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

## BF-AUTH-001 â€” Register Account Without Portfolio

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account without uploading a portfolio file.

### Main Flow

```text
UI register form
â†’ Backend API receives username/email/password/display_name/role_name
â†’ Backend hashes password using BCrypt
â†’ Backend calls auth.usp_User_Create
â†’ Database resolves role_name into role_id
â†’ Database creates auth.Users row with status_code = PENDING_APPROVAL
â†’ Database defaults display_name to username if display_name is empty/null
â†’ Database writes USER_REGISTERED audit event
â†’ Backend returns registration success / pending approval response
â†’ UI shows pending approval message
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

## BF-AUTH-002 â€” Register Account With Optional Portfolio

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account and attach an optional portfolio file for admin review.

### Recommended Main Flow

```text
UI register form
â†’ User enters username/email/password/display_name/role_name
â†’ User optionally selects portfolio file
â†’ Backend API receives form data + optional portfolio file
â†’ Backend uploads portfolio file to Cloudinary
â†’ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
â†’ Backend starts database workflow
â†’ Database creates auth.Users row with portfolio_file_id = NULL
â†’ Database creates manga.FileResource row with file_purpose_code = REGISTRATION_PORTFOLIO
â†’ Database links manga.FileResource.file_resource_id to auth.Users.portfolio_file_id
â†’ Database writes USER_REGISTERED audit event
â†’ Database may write REGISTRATION_PORTFOLIO_ATTACHED audit event
â†’ Backend returns registration success / pending approval response
â†’ UI shows pending approval message
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

## BF-AUTH-003 â€” Google Signup Callback

**Status:** Agreed
**Primary actor:** New User
**Goal:** Create a pending account using Google identity information.

### Main Flow

```text
User signs in with Google
â†’ Google returns email and display name
â†’ Backend checks whether user account already exists by email
â†’ If account does not exist, backend calls auth.usp_User_Create
â†’ Backend passes username derived from email or chosen account rule
â†’ Backend passes display_name from Google display name if available
â†’ If Google display name is unavailable, database defaults display_name to username
â†’ Database creates user with status_code = PENDING_APPROVAL
â†’ Database writes USER_REGISTERED audit event
â†’ Backend returns pending approval response
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

## BF-AUTH-004 â€” Admin Changes User Status

**Status:** Agreed
**Primary actor:** Admin
**Goal:** Allow Admin to activate, reject, or disable a user account.

### Main Flow

```text
Admin opens account management screen
â†’ Admin selects target user
â†’ Admin chooses new status: ACTIVE / REJECTED / DISABLED / PENDING_APPROVAL
â†’ Admin optionally enters reason
â†’ Backend calls auth.usp_Admin_ChangeUserStatus
â†’ Database checks actor is active Admin
â†’ Database locks the target user status-change workflow
â†’ Database reads old status
â†’ Database updates status_code
â†’ Database writes USER_STATUS_CHANGED audit event
â†’ UI refreshes account list/status
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

## BF-AUTH-005 â€” User Updates Display Name

**Status:** Agreed
**Primary actor:** General System User
**Goal:** Allow an authenticated user to update their visible display name without changing login identity.

### Main Flow

```text
User opens profile settings
â†’ User enters new display name
â†’ Backend calls auth.usp_User_UpdateDisplayName
â†’ Database checks actor_user_id equals target user_id
â†’ Database locks display-name update for that user
â†’ Database updates display_name only
â†’ Database writes USER_DISPLAY_NAME_UPDATED audit event
â†’ UI refreshes visible display name
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

## BF-AUTH-006 â€” User Password Reset / Password Change

**Status:** Agreed
**Primary actor:** General System User / Admin / Token-based reset flow
**Goal:** Update password hash without changing account approval/status state.

### Main Flow

```text
User/Admin/token flow requests password reset/change
â†’ Backend verifies required password reset condition
â†’ Backend hashes new password using BCrypt
â†’ Backend calls auth.usp_User_ResetPassword
â†’ Database locks password reset workflow for target user
â†’ Database updates password_hash only
â†’ Database preserves existing status_code
â†’ Database writes password-related audit event
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

## BF-FILE-001 â€” Create File Resource Metadata

**Status:** Agreed
**Primary actor:** Backend workflow / business workflow caller
**Goal:** Store Cloudinary file metadata in SQL Server and return a `file_resource_id` for the business record that owns the file.

### Main Flow

```text
Backend validates file purpose, file type, size, permission, and workflow context
â†’ Backend calculates SHA-256 from the exact uploaded file bytes
â†’ Backend uploads file to Cloudinary
â†’ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
â†’ Backend calls the relevant business workflow procedure
â†’ Business workflow procedure starts a SQL transaction
â†’ Business workflow procedure calls manga.usp_FileResource_Create inside that transaction
â†’ Database inserts manga.FileResource row
â†’ Database returns file_resource_id
â†’ Business workflow procedure creates/updates the owning business record with file_resource_id
â†’ Business workflow procedure writes one business-level audit event
â†’ Database transaction commits
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
| `SERIES_COVER` | `.jpg`, `.jpeg`, `.png`, `.webp` | `image/jpeg`, `image/png`, `image/webp` | `image` | Series cover image. In the Web draft UI, the selected source image is cropped client-side first, and the cropped `1000Ă—1500` PNG is uploaded as the actual cover. |
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

## BF-FILE-002 â€” Soft Delete File Resource

**Status:** Agreed
**Primary actor:** Admin or authorized file-management actor
**Goal:** Mark a file resource as deleted without physically removing its database row.

### Main Flow

```text
Authorized user requests file deletion
â†’ Backend calls manga.usp_FileResource_SoftDelete
â†’ Database checks file exists and is not already deleted
â†’ Database sets deleted_at_utc and deleted_by_user_id
â†’ Database writes FILE_RESOURCE_SOFT_DELETED audit event
â†’ UI shows safe placeholder where file is unavailable
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

## BF-FILE-003 â€” Optional Duplicate File Warning by SHA-256

**Status:** Agreed, optional MVP usability behavior
**Primary actor:** General System User / Backend workflow
**Goal:** Detect when a newly selected file appears identical to an existing active file and optionally warn the user before saving another copy.

### Main Flow

```text
User selects/uploads a file
â†’ Backend reads exact file bytes
â†’ Backend calculates SHA-256 hash
â†’ Backend searches active FileResource rows with the same sha256_hash
â†’ If a possible duplicate exists:
    Backend/UI may optionally show a warning
    User/backend may continue, cancel, or reuse depending on the workflow
â†’ If no duplicate exists or user continues:
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


## BF-SERIES-001 â€” Create Series Draft

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Create a new draft series profile and immediately register the creating Mangaka as an active contributor.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
â†’ Mangaka clicks Create Draft
â†’ UI shows create draft popup/modal
â†’ Mangaka enters title, synopsis, one or more genres, optional tags, content language, optional source series, optional proposed publication frequency, and optional cover image
â†’ If a cover image is selected, the UI opens a 2:3 portrait crop preview dialog
â†’ Mangaka confirms the crop, and the UI produces a `1000Ă—1500` PNG from the selected visible area
â†’ Backend validates the form and confirms the actor is an active Mangaka
â†’ Backend generates slug from title
â†’ Backend resolves slug uniqueness
â†’ If a cover image is provided, backend uploads the file to Cloudinary and calculates SHA-256 from the exact uploaded bytes
â†’ Backend calls manga.usp_Series_Create with series data, selected genre/tag IDs, and optional cover metadata
â†’ Database calls manga.usp_FileResource_Create if cover metadata exists
â†’ Database creates manga.Series with status_code = PROPOSAL_DRAFT
â†’ Database creates an active manga.SeriesContributor row for the creating Mangaka
â†’ Database writes SERIES_CREATED and contributor-related audit event(s)
â†’ UI refreshes the draft list and shows the created draft
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
- The MVP cover crop target is a 2:3 portrait image output as `1000Ă—1500` PNG.
- Smaller source images may be upscaled to `1000Ă—1500`; the UI should warn that the final cover may look blurry.
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

## BF-SERIES-002 â€” Update Series Draft Profile

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow an active Mangaka contributor to update draft series profile information while the series is still in `PROPOSAL_DRAFT`.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
â†’ Mangaka selects a draft series
â†’ UI opens edit draft popup/modal
â†’ Mangaka updates title, synopsis, genres, tags, content language, optional source series, optional proposed publication frequency, and optional cover image
â†’ If a replacement cover image is selected, the UI opens a 2:3 portrait crop preview dialog
â†’ Mangaka confirms the crop, and the UI produces a `1000Ă—1500` PNG from the selected visible area
â†’ Backend validates the form and confirms the actor is an active Mangaka contributor of the selected series
â†’ Backend confirms the series is still PROPOSAL_DRAFT
â†’ If title changed, backend regenerates slug from title and resolves slug uniqueness
â†’ If cover image changed, backend uploads the new cover to Cloudinary and calculates SHA-256 from the exact uploaded bytes
â†’ Backend calls manga.usp_Series_UpdateProfile with updated series data, selected genre/tag IDs, and optional new cover metadata
â†’ Database calls manga.usp_FileResource_Create if new cover metadata exists
â†’ Database updates manga.Series editable profile fields
â†’ Database updates updated_at_utc and updated_by_user_id together
â†’ Database writes SERIES_PROFILE_UPDATED audit event
â†’ UI refreshes the draft list/detail
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



## BF-SERIES-003 â€” Submit Series Proposal for Editorial Review

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Formally submit a draft series proposal with a required proposal file so active Tantou Editors can review it from the editorial queue.

### Main Flow

```text
Mangaka opens /mangaka/series/drafts
â†’ Mangaka selects a series with status_code = PROPOSAL_DRAFT
â†’ Mangaka clicks Submit Proposal
â†’ UI opens proposal submission modal
â†’ Mangaka selects the required proposal file
â†’ Backend validates the actor/session and request shape
â†’ Backend uploads the proposal file to Cloudinary
â†’ Backend calculates SHA-256 from the exact uploaded file bytes
â†’ Backend calls manga.usp_SeriesProposal_Submit with the series ID, actor user ID, and required proposal file metadata
â†’ Database locks proposal submission for the series
â†’ Database verifies the series exists and status_code = PROPOSAL_DRAFT
â†’ Database verifies the submitter is an active Mangaka contributor of the series
â†’ Database does not require an active Tantou Editor contributor for first submission
â†’ Database creates manga.FileResource with file_purpose_code = SERIES_PROPOSAL
â†’ Database creates manga.SeriesProposal with the next proposal_version_no, proposal title/synopsis snapshots, and proposal file reference
â†’ Database sets manga.SeriesProposal.status_code = UNDER_EDITORIAL_REVIEW
â†’ Database sets manga.Series.status_code = UNDER_EDITORIAL_REVIEW
â†’ Database writes SERIES_PROPOSAL_SUBMITTED audit event
â†’ Backend returns success
â†’ UI locks normal draft editing and shows the series/proposal as under editorial review
â†’ Proposal becomes visible in the Tantou Editor editorial review queue
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
# Suggested Additions â€” Business Flows / Use Case Notes

---

## BF-MANGAKA-001 â€” View My Series Dashboard

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Show only series where the logged-in actor is an active Mangaka contributor.

### Main Flow

```text
Mangaka opens /mangaka
â†’ UI calls GET /api/mangaka/series/my-series through typed API client
â†’ API resolves actor user id
â†’ API sends GetMyMangakaSeriesQuery
â†’ Application validates actor id
â†’ Infrastructure queries series where actor is an active Mangaka contributor
â†’ Query includes cover, genres, tags, status, slug, proposed frequency, and update time
â†’ UI renders dashboard cards with filters/search/sort
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

## BF-MANGAKA-002 â€” Navigate to Assistant Review / Review Submissions

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Open the enhanced task-review page from the Mangaka dashboard sidebar.

### Main Flow

```text
Mangaka opens /mangaka
â†’ Mangaka clicks sidebar item "Assistant Review"
â†’ UI routes to /mangaka/review-submissions
â†’ Page loads under MangakaLayout
â†’ UI calls GET /api/mangaka/tasks
â†’ Backend returns tasks created by the Mangaka for assistant work review
â†’ UI displays task cards with series/chapter/page/version/task context
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

## BF-TASK-004 â€” Mangaka Reviews Assistant Task Submissions

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Let a Mangaka review assistant-submitted page work and choose the correct review action: approve, return to the same Assistant for rework, cancel, or reassign to a different Assistant.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
â†’ UI loads task list for the Mangaka-created tasks
â†’ UI shows task cards with series, chapter, page, page version, assistant, type, status, due date, priority, compensation, and region count
â†’ Mangaka filters by series/chapter/title, task type, assistant, and status
â†’ Mangaka opens original/submitted previews when available
â†’ Mangaka chooses one available action:
    - Approve
    - Return for Rework
    - Cancel
    - Reassign
â†’ UI calls the relevant typed API client method
â†’ API calls the relevant Application task use case/service
â†’ Application validates actor permission and task state
â†’ Infrastructure calls the relevant stored procedure/repository workflow
â†’ UI reloads task list, clears dialog state, and refreshes stat cards/actions
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

## BF-TASK-005 â€” Return Assistant Task for Rework to Same Assistant

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Return an `UNDER_REVIEW` task to the same assigned Assistant for rework.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
â†’ UI shows Return for Rework action on eligible UNDER_REVIEW task
â†’ Mangaka opens Return for Rework dialog
â†’ Mangaka enters updated task instructions/description
â†’ UI calls Return for Rework API action
â†’ API calls Application task return-for-rework use case/service
â†’ Application validates actor, task state, and updated task description
â†’ Infrastructure calls manga.usp_ChapterPageTask_ReturnForRework
â†’ SQL locks the task workflow
â†’ SQL verifies the task exists
â†’ SQL verifies status_code = UNDER_REVIEW
â†’ SQL verifies actor is an active Mangaka contributor of the task's series
â†’ SQL updates the same ChapterPageTask row:
    - status_code = ASSIGNED
    - completed_page_version_id = NULL
    - task_description = updated task description
    - updated_at_utc = current UTC time
â†’ SQL writes CHAPTER_PAGE_TASK_RETURNED_FOR_REWORK audit event
â†’ UI reloads task list and clears dialog state
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
  `ChapterPageTaskRegion â†’ PageRegion â†’ ChapterPageVersion â†’ ChapterPage â†’ Chapter â†’ Series`.
* Application/C# should validate the primary business rules before calling SQL when possible.
* SQL remains the final transactional guard and audit owner.

### System Should Try To

* Let Mangaka request revisions from the same Assistant without creating a new task.
* Preserve audit traceability of rejected submitted work.
* Prevent returning tasks that are not currently under review.

---

## BF-TASK-006 â€” Reassign Assistant Task to Different Assistant

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Allow a Mangaka to move a task to a different eligible Assistant contributor when the original Assistant should no longer own the task.

### Main Flow

```text
Mangaka opens /mangaka/review-submissions
â†’ UI shows Reassign action for ASSIGNED and UNDER_REVIEW tasks
â†’ Mangaka opens Reassign dialog
â†’ UI loads eligible assistants for the task
â†’ Mangaka selects a different Assistant
â†’ Mangaka enters required reason
â†’ UI calls POST /api/mangaka/tasks/{taskId}/reassign
â†’ API calls Application task reassignment use case
â†’ Application validates actor, task status, reason, same-user rule, and assistant eligibility
â†’ Infrastructure calls manga.usp_ChapterPageTask_AssignToDifferentUser
â†’ SQL cancels old task
â†’ SQL creates replacement ASSIGNED task for the new Assistant
â†’ SQL copies task-region links
â†’ SQL writes CHAPTER_PAGE_TASK_ASSIGNED_TO_DIFFERENT_USER audit event
â†’ UI reloads task list after success
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
â†’ UI shows Reassign action for ASSIGNED and UNDER_REVIEW tasks
â†’ Mangaka opens Reassign dialog
â†’ UI loads eligible assistants for the task
â†’ Mangaka selects a different Assistant
â†’ Mangaka enters required reason
â†’ UI calls POST /api/mangaka/tasks/{taskId}/reassign
â†’ API calls Application task reassignment use case
â†’ Application validates actor, task status, reason, same-user rule, and assistant eligibility
â†’ Infrastructure calls manga.usp_ChapterPageTask_AssignToDifferentUser
â†’ SQL cancels old task, creates replacement ASSIGNED task, copies task-region links, and writes audit event
â†’ UI reloads task list after success
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

## BF-MANGAKA-CONTRIB-001 â€” View Series Contributors

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Let a Mangaka view contributor history for series where they are an active Mangaka contributor.

### Main Flow

```text
Mangaka opens /mangaka/contributors
â†’ UI loads the Mangaka's own series list from GET /api/mangaka/series/my-series
â†’ Mangaka selects a series
â†’ UI calls GET /api/mangaka/series/{seriesId}/contributors
â†’ API sends GetSeriesContributorsQuery
â†’ Application validates actor permission
â†’ Infrastructure returns contributor rows for the selected series
â†’ UI displays active and former contributors with role, status, start date, end date, and actions
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

## BF-MANGAKA-CONTRIB-002 â€” Add Assistant Contributor to Series

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Add an active Assistant user as an active contributor to one of the Mangaka's own series.

### Main Flow

```text
Mangaka opens /mangaka/contributors
â†’ Mangaka selects a series
â†’ Mangaka clicks Add Assistant
â†’ UI opens Add Assistant dialog
â†’ UI searches eligible assistants through GET /api/mangaka/series/{seriesId}/contributors/eligible-assistants
â†’ Backend returns ACTIVE Assistant users who are not currently active contributors of the selected series
â†’ Mangaka selects an Assistant
â†’ UI calls POST /api/mangaka/series/{seriesId}/contributors/assistants
â†’ API sends AddAssistantContributorCommand
â†’ Application validates actor, target user role/status, and duplicate active contributor rule
â†’ Infrastructure calls manga.usp_SeriesContributor_Add
â†’ Database inserts active SeriesContributor row and writes audit
â†’ UI refreshes contributor list and eligible-assistant search
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

## BF-MANGAKA-CONTRIB-003 â€” End Assistant Contribution

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** End an active Assistant contributor's participation in a series while preserving history.

### Main Flow

```text
Mangaka opens /mangaka/contributors
â†’ Mangaka selects a series
â†’ UI shows End action only for active Assistant contributor rows
â†’ Mangaka opens End Assistant dialog
â†’ Mangaka enters reason
â†’ UI calls POST /api/mangaka/series/{seriesId}/contributors/assistants/{assistantUserId}/end
â†’ API sends EndAssistantContributorCommand
â†’ Application validates actor permission, target role/status, reason, and active task blocking rule
â†’ Infrastructure calls manga.usp_SeriesContributor_EndAssistant
â†’ SQL sets end_date to current UTC date and writes audit
â†’ UI refreshes contributor list and eligible-assistant search
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

## BF-NAV-001 â€” Safe Return URL Navigation

**Status:** Agreed
**Primary actor:** Any authenticated role
**Goal:** Preserve safe back-navigation across role pages, series pages, and workspace links.

### Main Flow

```text
User opens a page that links to /series/{slug} or workspace
â†’ UI appends a local returnUrl using SafeReturnUrl.AppendReturnUrl()
â†’ Target page resolves returnUrl through SafeReturnUrl.Resolve()
â†’ If returnUrl is safe, Back button uses it
â†’ If returnUrl is unsafe or missing, page falls back to a safe default such as /dashboard
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

## BF-BOARD-001 â€” Open START_SERIALIZATION Poll

**Status:** Agreed
**Primary actor:** Editorial Board Chief
**Goal:** Start board voting for serialization and specify the official publication frequency if approved.

### Main Flow

```text
Editorial Board Chief opens board review queue
â†’ Chief selects eligible series/proposal
â†’ Chief enters poll reason
â†’ Chief selects board_publication_frequency_code
â†’ Backend calls board poll creation procedure
â†’ Database checks series is UNDER_BOARD_REVIEW
â†’ Database checks exactly one active proposal is UNDER_BOARD_REVIEW
â†’ Database creates SeriesBoardPoll with poll_type_code = START_SERIALIZATION
â†’ Database stores board_publication_frequency_code on the poll
â†’ Database writes board poll creation audit event
â†’ Board members and chief may vote while poll is open
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

## BF-BOARD-002 â€” Apply START_SERIALIZATION Poll Result

**Status:** Agreed
**Primary actor:** Editorial Board Chief or system workflow
**Goal:** Apply the computed board vote result after poll closure.

### Main Flow

```text
Poll is closed
â†’ System computes approve/reject/abstain counts
â†’ If approve > reject:
    Series status becomes SERIALIZED
    Active proposal status becomes APPROVED
    Series.publication_frequency_code = SeriesBoardPoll.board_publication_frequency_code
â†’ If reject > approve:
    Proposal/series follow MVP rejection/cancellation policy
â†’ If tied:
    Series/proposal remain UNDER_BOARD_REVIEW
â†’ Database writes board result application audit event
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

## BF-BOARD-003 â€” Mangaka Requests Publication Frequency Change After Board Decision

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Let Mangaka communicate schedule concerns after the board has already decided the official frequency.

### Main Flow

```text
Mangaka views serialized series publication frequency
â†’ Mangaka writes request/reason for frequency change
â†’ Backend creates in-app notification to Editorial Board Chief
â†’ Editorial Board Chief reviews notification
â†’ Chief may decide to directly change official frequency through separate controlled workflow
```

### Important Notes

- No official frequency-change request table is required in MVP.
- Notification is communication, not approval.
- Only Editorial Board Chief can directly change official publication frequency.
- Direct frequency change must include audit reason.

### System Should Try To

- Keep the request lightweight for MVP.
- Avoid adding unnecessary workflow tables.
- Preserve official control under Editorial Board Chief.

---

# 6. Page Modification and Page Version Flows

## BF-PAGE-001 â€” Temporary Page Modification Download

**Status:** Draft
**Primary actor:** Mangaka / Authorized Page Workspace User
**Goal:** Let a user generate or edit a modified page output with system tools and download it for external editing without replacing the current page version.

### Example System Tools

```text
Auto-translation
â†’ AI/OCR text replacement preview
â†’ future cleanup tool
â†’ future effects/coloring helper
â†’ future panel/text adjustment tool
```

### Main Flow

```text
User opens page workspace for an existing ChapterPageVersion
â†’ User uses a system tool that modifies or generates a changed page output
â†’ Backend verifies the user can access the page/version and use the selected tool
â†’ Backend sends the current page image, Cloudinary URL, or required page data to the relevant tool/service
â†’ Tool/service generates a modified output file or editable preview
â†’ Backend may upload the modified output to a temporary Cloudinary folder such as manga-management/temp/page-modifications/
â†’ Backend returns a temporary preview/download URL to the UI
â†’ User downloads the modified output for external editing or discards it
â†’ No manga.FileResource row is created for the temporary output by default
â†’ No ChapterPageVersion row is created
â†’ Current ChapterPageVersion remains unchanged
â†’ Temporary Cloudinary asset is deleted later by cleanup workflow
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

## BF-PAGE-002 â€” Save Modified Page Output as New Page Version

**Status:** Draft
**Primary actor:** Mangaka / Authorized Page Workspace User
**Goal:** Save an accepted modified page output as an official tracked `ChapterPageVersion`.

### Example Modified Outputs

```text
Accepted auto-translated page output
â†’ accepted AI/OCR edited page output
â†’ accepted assistant output
â†’ user-edited file re-uploaded after temporary download
â†’ future accepted output from other page editing tools
```

### Main Flow

```text
User reviews modified page output
â†’ User clicks Save as New Page Version or uploads the externally edited output back into the system
â†’ Backend validates the user can create a new page version for the logical page
â†’ Backend uploads the final accepted page file to Cloudinary if it is not already stored as a permanent asset
â†’ Cloudinary returns public_id, secure_url, content_type, file size, and other metadata
â†’ Backend calls the page-version creation workflow procedure
â†’ Database transaction begins
â†’ Database calls manga.usp_FileResource_Create with file_purpose_code = CHAPTER_PAGE_VERSION
â†’ Database creates manga.FileResource row
â†’ Database creates manga.ChapterPageVersion row using the returned file_resource_id
â†’ Database updates current-version state if workflow rules allow this new version to become current
â†’ Database writes CHAPTER_PAGE_VERSION_CREATED audit event with the source/tool context in detail_json when available
â†’ Database transaction commits
â†’ UI shows the new page version in page-version history
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

## BF-PAGE-003 â€” Create Page Regions for a Page Version

**Status:** Agreed
**Primary actor:** Authorized Page Workspace User
**Goal:** Save one or more manual or AI-suggested page regions for a specific `ChapterPageVersion`.

### Main Flow

```text
User opens the chapter/page workspace
â†’ User selects a page version
â†’ User draws one or more manual regions or accepts AI-suggested regions
â†’ UI prepares region JSON as either one object or an array of objects
â†’ Backend validates the selected chapter_page_version_id and region request shape
â†’ Backend calls manga.usp_PageRegion_Create
â†’ Database verifies actor is an ACTIVE user and active SeriesContributor for the series that owns the selected page version
â†’ Database normalizes single-object JSON into an array when needed
â†’ Database parses region fields from JSON
â†’ Database inserts manga.PageRegion rows linked to the selected chapter_page_version_id
â†’ Database returns created page_region_id values as JSON
â†’ Database writes PAGE_REGIONS_CREATED audit event
â†’ UI can use the returned page_region_id values for annotation, task assignment, segmentation display, or later workspace actions
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

## BF-PAGE-004 â€” Create Page Annotation Linked to Existing or Newly Created Page Regions

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor
**Goal:** Create a page annotation/comment and link it to one or more `PageRegion` records.

### Main Flow

```text
User opens the chapter/page workspace
â†’ User selects one or more existing saved PageRegion records and/or draws new unsaved regions
â†’ User enters issue type and annotation text
â†’ Backend separates existing_page_region_ids from new region objects
â†’ Backend starts one SQL transaction
â†’ If new unsaved regions exist:
    Backend calls manga.usp_PageRegion_Create with chapter_page_version_id and regions_json
    Database creates PageRegion rows and returns created_page_region_ids_json
    Backend reads the returned created page_region_id values
â†’ Backend merges existing_page_region_ids with newly created page_region_id values
â†’ Backend removes duplicate IDs before sending the final region list
â†’ Backend calls manga.usp_ChapterPageAnnotation_Create with actor_user_id, issue_type_code, annotation_text, and final page_region_ids_json
â†’ Database validates page_region_ids_json
â†’ Database verifies all referenced PageRegion rows exist
â†’ Database verifies all referenced PageRegion rows belong to the same ChapterPageVersion
â†’ Database derives owning series/page context through linked PageRegion records
â†’ Database verifies the actor account is ACTIVE
â†’ Database verifies the actor is an active contributor for the owning series
â†’ Database verifies the actor role is Mangaka or Tantou Editor
â†’ Database creates one manga.ChapterPageAnnotation row
â†’ Database creates one or more manga.ChapterPageAnnotationRegion rows
â†’ Database writes CHAPTER_PAGE_ANNOTATION_CREATED audit event
â†’ Backend commits the SQL transaction
â†’ UI shows the annotation marker/comment linked to all selected regions
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
- The annotation does not need its own direct `chapter_page_version_id` because the page-version context is derived through `ChapterPageAnnotationRegion â†’ PageRegion â†’ ChapterPageVersion`.
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

## BF-PAGE-005 â€” Resolve Page Annotation

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor with permission
**Goal:** Mark an annotation as handled without deleting the original feedback record.

### Main Flow

```text
User opens the chapter/page workspace
â†’ User reviews an unresolved annotation
â†’ Related work is corrected, usually through a newer ChapterPageVersion or accepted task output for the same logical page derived from linked regions
â†’ Authorized resolver confirms the issue has been handled
â†’ Backend calls the annotation resolve workflow procedure
â†’ Database verifies annotation exists and is not already resolved
â†’ Database derives owning series/page context through linked PageRegion records
â†’ Database resolves the annotation creator's current role through annotated_by_user_id
â†’ Database resolves the resolver's current role through actor_user_id
â†’ Database verifies the resolver account is ACTIVE and an active contributor for the owning series
â†’ If the annotation was created by a Mangaka:
    Database allows resolution by an active Mangaka contributor or active Tantou Editor contributor on the same series
â†’ If the annotation was created by a Tantou Editor:
    Database allows resolution only by an active Tantou Editor contributor on the same series
â†’ Database sets resolved_at_utc = SYSUTCDATETIME()
â†’ Database sets resolved_by_user_id = resolver user ID
â†’ Database writes CHAPTER_PAGE_ANNOTATION_RESOLVED audit event
â†’ UI marks the annotation as resolved while preserving it for history
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

## BF-PAGE-005A â€” Update Page Annotation Text

**Status:** Agreed
**Primary actor:** Mangaka / Tantou Editor with permission
**Goal:** Correct or clarify unresolved annotation text without changing linked regions or deleting feedback history.

### Main Flow

```text
User opens the chapter/page workspace
â†’ User selects an unresolved annotation
â†’ User edits annotation text and optionally enters an update reason
â†’ Backend calls the annotation text update workflow procedure
â†’ Database verifies annotation exists and is unresolved
â†’ Database derives owning series/page context through linked PageRegion records
â†’ Database resolves the annotation creator's current role through annotated_by_user_id
â†’ Database resolves the actor's current role through actor_user_id
â†’ Database verifies the actor account is ACTIVE and an active contributor for the owning series
â†’ If the annotation was created by a Mangaka:
    Database allows update by an active Mangaka contributor or active Tantou Editor contributor on the same series
â†’ If the annotation was created by a Tantou Editor:
    Database allows update only by an active Tantou Editor contributor on the same series
â†’ Database updates annotation_text
â†’ Database writes CHAPTER_PAGE_ANNOTATION_TEXT_UPDATED audit event with old text, new text, actor, and optional reason
â†’ UI refreshes the annotation text
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

## BF-PAGE-006 â€” Create Chapter Page Task Linked to Page Regions

**Status:** Agreed
**Primary actor:** Mangaka / authorized task creator
**Goal:** Create a page task for an Assistant or contributor and link the task to one or more `PageRegion` records.

### Main Flow

```text
User opens the chapter/page workspace
â†’ User selects one or more existing saved PageRegion records for the task target
â†’ If the task applies to the whole page, UI/backend uses a full-page PageRegion for the selected ChapterPageVersion
â†’ User enters assigned user, task type, title, description, priority, due date, and compensation amount
â†’ Backend prepares page_region_ids_json as a JSON array of PageRegion IDs
â†’ Backend calls manga.usp_ChapterPageTask_Create
â†’ Database validates page_region_ids_json is a valid JSON array
â†’ Database verifies all referenced PageRegion rows exist
â†’ Database verifies all referenced PageRegion rows belong to the same ChapterPageVersion
â†’ Database derives the owning series through PageRegion â†’ ChapterPageVersion â†’ ChapterPage â†’ Chapter
â†’ Database verifies the actor is an active contributor for the owning series
â†’ Database verifies the assigned user is ACTIVE and an active contributor for the owning series
â†’ Database creates one manga.ChapterPageTask row
â†’ Database creates one or more manga.ChapterPageTaskRegion rows
â†’ Database writes CHAPTER_PAGE_TASK_CREATED audit event
â†’ UI shows the new task with its linked target regions
```

### Page Region ID JSON

A task must always provide at least one page region ID:

```json
[
  "A2F98C3A-CE7D-44BD-85F2-B7C1525B4C41"
]
```

Multiple linked regions are allowed:

```json
[
  "A2F98C3A-CE7D-44BD-85F2-B7C1525B4C41",
  "46D8D55C-1174-4DA8-8E7E-6E49B5D6E53A"
]
```

### Database Procedure(s)

```text
manga.usp_ChapterPageTask_Create
audit.usp_AuditEvent_Append
```

### Database Tables

```text
manga.ChapterPageTask
manga.ChapterPageTaskRegion
manga.PageRegion
manga.ChapterPageVersion
```

### Important Notes

- `ChapterPageTask` stores the task header: assigned user, task type, status, title, description, priority, due date, compensation amount, completion output, creator, and timestamps.
- `ChapterPageTask` no longer stores `chapter_page_id` directly.
- `ChapterPageTaskRegion` links one task to one or more `PageRegion` records.
- Each task must link to at least one `PageRegion`.
- A whole-page task must still link to a `PageRegion`; the system should create or reuse a full-page region covering the selected `ChapterPageVersion`.
- All regions linked to the same task must belong to the same `ChapterPageVersion`.
- The task page context is derived through `ChapterPageTaskRegion â†’ PageRegion â†’ ChapterPageVersion â†’ ChapterPage`.
- The task owning series is derived through `PageRegion â†’ ChapterPageVersion â†’ ChapterPage â†’ Chapter â†’ Series`.
- The actor creating the task must be an active contributor for the owning series.
- The assigned user must be an `ACTIVE` account and an active contributor for the owning series.
- `compensation_amount` is required, must be non-negative, and should use `0.00` when no compensation is paid.
- Compensation amount is task metadata only and does not introduce payroll, salary calculation, payment processing, or accounting features.
- The `type_code`, `status_code`, `priority_level`, compensation range, and foreign keys should be enforced by table constraints.
- When the task later reaches `UNDER_REVIEW` or `COMPLETED`, the completed page version must be checked against the same logical page derived from the task's linked regions.

### System Should Try To

- Keep task creation tied to visible page regions instead of hidden page-level assumptions.
- Support one-region, multi-region, and whole-page task targets consistently.
- Avoid storing duplicate page context directly on `ChapterPageTask` when it can be derived through linked regions.
- Keep permission checks based on the owning series derived from the selected regions.
- Keep the task assignment workflow traceable through audit.

---

## BF-TASK-007 â€” Quick Select Batch Task Assignment

**Status:** Agreed
**Primary actor:** Mangaka
**Goal:** Create multiple assigned tasks at once by selecting pages with their current versions, an assistant, task type, and common task defaults. Each task links to one whole-page `PageRegion`.

### Main Flow

```text
Mangaka opens the Quick Select dialog from the chapter workspace
â†’ Mangaka selects a series and chapter
â†’ UI loads available chapters for Quick Select
â†’ UI loads pages/current versions for the selected chapter
â†’ UI loads active Assistant contributors for the selected series
â†’ Mangaka selects one or more pages
â†’ Mangaka selects an Assistant
â†’ Mangaka enters task type, title prefix, default description, priority, due date, and compensation
â†’ Mangaka optionally overrides the description for individual pages
â†’ Mangaka clicks Confirm
â†’ Backend validates the full request
â†’ Backend loads page/version/file metadata
â†’ Backend resolves Cloudinary image dimensions for each selected page version
â†’ Backend builds a validated assignment plan
â†’ Backend opens a SQL transaction
â†’ Backend acquires a session+series+chapter scoped SQL app lock
â†’ Backend re-checks guards (actor, assistant, chapter, pages, versions, files)
â†’ Backend finds or creates one FULL_PAGE PageRegion per selected page version
â†’ Backend creates one ASSIGNED ChapterPageTask per selected page
â†’ Backend links each task to its FULL_PAGE PageRegion through ChapterPageTaskRegion
â†’ Backend writes one CHAPTER_PAGE_TASK_CREATED audit event per task
â†’ Backend commits the transaction
â†’ UI reloads the task list and shows created tasks
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
→ Backend validates actor permission, chapter status, required comments, and optional markup file
→ Backend creates manga.ChapterEditorialReview
→ Backend updates manga.Chapter.status_code according to the decision
→ Backend writes the chapter review audit event
→ UI refreshes the chapter status and review history
```

### Decision Outcomes

| Decision | Chapter status after review | Meaning |
|---|---|---|
| `APPROVED` | `APPROVED` | The chapter is accepted and may proceed toward scheduling/release. |
| `REVISION_REQUESTED` | `REVISION_REQUESTED` | The same chapter attempt can be edited and resubmitted with new page versions. |
| `CANCELLED` | `CANCELLED` | The current chapter attempt is a hard stop and becomes read-only historical reference. |

### Important Notes

- `REVISION_REQUESTED` and `CANCELLED` both require non-blank comments.
- Markup files are optional for both `REVISION_REQUESTED` and `CANCELLED`.
- Page-specific annotations remain separate from the final chapter-level review decision.
- `CANCELLED` is not a normal fix-and-resubmit outcome. Use `REVISION_REQUESTED` when the chapter can still be fixed.

### System Should Try To

- Make the difference between revision and cancellation clear in the UI.
- Preserve review decisions and page-level context.
- Avoid deleting historical chapter materials.

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

# 7. Workflow Template for Future Additions

Use this template when adding new flows.

```md
## BF-AREA-XXX â€” Flow Name

**Status:** Draft / Agreed / Future
**Primary actor:** Actor Name
**Goal:** Short goal sentence.

### Main Flow

```text
Step 1
â†’ Step 2
â†’ Step 3
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

# 8. Backlog: Flows To Add Later

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
- Ranking snapshot generation flow
- Notification read/unread flow
- Cloudinary cleanup failure handling flow
