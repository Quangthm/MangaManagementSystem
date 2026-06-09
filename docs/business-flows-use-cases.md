# Manga Creation Workflow and Publishing Management System — Business Flows / Use Case Notes

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
| `TASK_REFERENCE` | Optional warning only; repeated reference files may be valid. |
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
→ Mangaka enters title, synopsis, genre, content language, optional source series, optional proposed publication frequency, and optional cover image
→ Backend validates the form and confirms the actor is an active Mangaka
→ Backend generates slug from title
→ Backend resolves slug uniqueness
→ If a cover image is provided, backend uploads the file to Cloudinary and calculates SHA-256 from the exact uploaded bytes
→ Backend calls manga.usp_Series_Create with series data and optional cover metadata
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
- If a cover image is provided, C# uploads to Cloudinary first, then passes Cloudinary metadata to SQL.
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
→ Mangaka updates title, synopsis, genre, content language, optional source series, optional proposed publication frequency, and optional cover image
→ Backend validates the form and confirms the actor is an active Mangaka contributor of the selected series
→ Backend confirms the series is still PROPOSAL_DRAFT
→ If title changed, backend regenerates slug from title and resolves slug uniqueness
→ If cover image changed, backend uploads the new cover to Cloudinary and calculates SHA-256 from the exact uploaded bytes
→ Backend calls manga.usp_Series_UpdateProfile with updated series data and optional new cover metadata
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
- Once the series leaves `PROPOSAL_DRAFT`, title, slug, synopsis, genre, cover, content language, source series, and `publication_frequency_code` are locked from normal Mangaka profile editing.
- Slug may auto-regenerate from title during `PROPOSAL_DRAFT` because draft workflows use `series_id`.
- Slug locks after the series leaves `PROPOSAL_DRAFT`; no slug history or redirect table is required for MVP.
- `publication_frequency_code` is treated as Mangaka's proposed/preferred frequency during draft; board serialization/frequency override is handled by a separate board procedure.
- The procedure should rely on database constraints for simple `CHECK`, `UNIQUE`, `NOT NULL`, and FK enforcement, while still validating actor permission and workflow state.

### System Should Try To

- Preserve stable review information after the draft leaves `PROPOSAL_DRAFT`.
- Prevent Admin from owning manga business-content creation/update flows.
- Keep draft editing convenient through popup/modal UI.
- Keep future `/series/{slug}` URLs stable after serialization.

---

## BF-PROP-001 — Submit Series Proposal

**Status:** Agreed  
**Primary actor:** Mangaka  
**Goal:** Submit a formal proposal version for editorial review.

### Main Flow

```text
Mangaka opens proposal submission form
→ Mangaka enters proposal title/synopsis/genre snapshot
→ Mangaka uploads proposal file
→ Backend uploads proposal file to Cloudinary
→ Backend calls manga.usp_SeriesProposal_Submit
→ Database creates FileResource with file_purpose_code = SERIES_PROPOSAL
→ Database creates SeriesProposal row
→ SeriesProposal status becomes UNDER_EDITORIAL_REVIEW
→ Database writes SERIES_PROPOSAL_SUBMITTED audit event
→ UI shows proposal submitted and waiting for editorial review
```

### Important Notes

- `SeriesProposal` represents a formal submitted version.
- Submitted snapshot fields should not be overwritten.
- If revision is requested, submit a new proposal version later.
- Proposal file should be stored as `FileResource`.
- Audit procedure resolves actor role internally.

### System Should Try To

- Preserve submitted proposal history.
- Avoid overwriting previous formal submissions.
- Keep proposal file and proposal row created in one database transaction.

---

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
- Mangaka’s preferred frequency is not stored as a separate official column in MVP.
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

## BF-BOARD-003 — Mangaka Requests Publication Frequency Change After Board Decision

**Status:** Agreed  
**Primary actor:** Mangaka  
**Goal:** Let Mangaka communicate schedule concerns after the board has already decided the official frequency.

### Main Flow

```text
Mangaka views serialized series publication frequency
→ Mangaka writes request/reason for frequency change
→ Backend creates in-app notification to Editorial Board Chief
→ Editorial Board Chief reviews notification
→ Chief may decide to directly change official frequency through separate controlled workflow
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

# 7. Workflow Template for Future Additions

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

# 8. Backlog: Flows To Add Later

Add detailed flows for these when the team finalizes them:

- Admin approval/rejection screen flow
- Series contributor assignment flow
- Tantou Editor proposal review flow
- Chapter creation flow
- Chapter page upload/versioning flow
- General page modification temporary download/save-as-version flow
- Page region AI/manual segmentation flow
- Page annotation flow
- Assistant task assignment and submission flow
- Chapter editorial review flow
- Board vote flow
- Cancel serialization poll flow
- Ranking snapshot generation flow
- Notification read/unread flow
- Cloudinary cleanup failure handling flow
