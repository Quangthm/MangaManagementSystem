# SQL Stored Procedure Creation Skill Guide

Project: Manga Creation Workflow and Publishing Management System  
Database: SQL Server  
Purpose: Keep stored procedure design consistent, practical, and aligned with the project’s MVP database rules.

---

## 1. Core principle

Do **not** duplicate validation inside stored procedures for attributes that already have database schema constraints.

If the table already enforces a rule through one of these mechanisms, the procedure does not need to manually re-check the same rule:

- `NOT NULL`
- `CHECK`
- `UNIQUE`
- `PRIMARY KEY`
- `FOREIGN KEY`
- column length/type constraints

The backend/UI should validate input before sending it to the database, and the database constraint remains the final protection.

### Example

If the table has:

```sql
CONSTRAINT ck_users_status_code CHECK (
    status_code IN (
        N'PENDING_APPROVAL',
        N'REJECTED',
        N'ACTIVE',
        N'DISABLED'
    )
)
```

Then the procedure does **not** need:

```sql
IF @new_status_code NOT IN (...)
BEGIN
    THROW ...
END;
```

Let the schema constraint reject invalid status values.

---

## 2. What procedures should still validate

Stored procedures should still validate rules that are **not fully handled by simple schema constraints**.

### 2.1 Permission and actor checks

Procedures should check whether the actor is allowed to perform the action.

Example:

```sql
IF NOT EXISTS
(
    SELECT 1
    FROM
    (
        SELECT
            u.user_id,
            u.role_id
        FROM auth.Users u
        WHERE u.user_id = @admin_user_id
          AND u.status_code = N'ACTIVE'
    ) AS active_admin
    INNER JOIN auth.Roles r
        ON r.role_id = active_admin.role_id
    WHERE r.role_name = N'Admin'
)
BEGIN
    ;THROW 53102, 'Admin user is not active or does not have permission.', 1;
END;
```

### 2.2 Business workflow rules

Validate workflow rules that depend on current database state.

Examples:

- A `START_SERIALIZATION` poll can only open when a series is `UNDER_BOARD_REVIEW`.
- A Mangaka may only update preferred publication frequency while the series is `PROPOSAL_DRAFT`.
- A chapter can only be submitted when required current page versions exist.
- An Editorial Board Chief must provide a reason when directly changing official publication frequency.
- A proposal submission should generate `proposal_version_no` inside the database, not in the backend.
- A proposal submission should snapshot current `manga.Series` title/synopsis/genre inside the procedure.

### 2.3 Cross-row and cross-table rules

Validate rules involving multiple rows or tables.

Examples:

- A series cannot have more than one open poll of the same type.
- A page task output must belong to the same logical chapter page as the task.
- A board vote must belong to an open poll.
- A `START_SERIALIZATION` poll must refer to a series with exactly one active proposal under board review.

### 2.4 Actor-specific behavior

Validate behavior that depends on who performs the action.

Examples:

- Only Admin can activate/reject/disable accounts.
- Only Editorial Board Chief can open, close, or cancel board polls.
- Only Editorial Board Member or Editorial Board Chief can vote.
- A user may update only their own display name.

### 2.5 JSON validity

If a procedure accepts JSON text, validate JSON inside the procedure.

Example:

```sql
IF @detail_json IS NOT NULL AND ISJSON(@detail_json) <> 1
BEGIN
    ;THROW 50001, 'detail_json must be valid JSON.', 1;
END;
```

---

## 3. Transaction pattern

Use this standard transaction pattern for procedures that write data.

```sql
DECLARE @started_tran BIT = 0;

BEGIN TRY
    IF @@TRANCOUNT = 0
    BEGIN
        SET @started_tran = 1;
        BEGIN TRAN;
    END;

    -- procedure logic here

    IF @started_tran = 1
    BEGIN
        COMMIT;
    END;
END TRY
BEGIN CATCH
    IF @started_tran = 1 AND XACT_STATE() <> 0
    BEGIN
        ROLLBACK;
    END;

    ;THROW;
END CATCH;
```

### Rule

Only commit or roll back if the procedure started the transaction.

This allows procedures to be safely called by other procedures that already own a transaction.

---

## 4. GUID / UUID ID handling in procedures

The current project uses SQL Server GUID/UUID-style IDs.

In SQL Server, use:

```sql
UNIQUEIDENTIFIER
```

For business/domain table primary keys, the schema should normally generate the GUID using:

```sql
DEFAULT NEWID()
```

Audit event exception:

```sql
audit.AuditEvent.audit_event_id BIGINT IDENTITY(1, 1)
```

This exception is allowed because audit events are append-only, chronological, and ledger-style.

### 4.1 Procedure rule: do not manually create IDs unless needed

If the table column already has `DEFAULT NEWID()`, the stored procedure should usually let the table generate the GUID.

Preferred insert pattern:

```sql
DECLARE @created_user TABLE
(
    user_id UNIQUEIDENTIFIER NOT NULL,
    status_code NVARCHAR(30) NOT NULL
);

INSERT INTO auth.Users
(
    role_id,
    username,
    email,
    password_hash,
    display_name,
    avatar_file_id,
    portfolio_file_id,
    status_code
)
OUTPUT
    inserted.user_id,
    inserted.status_code
INTO @created_user
(
    user_id,
    status_code
)
VALUES
(
    @resolved_role_id,
    @normalized_username,
    @normalized_email,
    @password_hash,
    @normalized_display_name,
    @avatar_file_id,
    @portfolio_file_id,
    N'PENDING_APPROVAL'
);

SELECT
    @new_user_id = user_id
FROM @created_user;
```

This means:

```text
table DEFAULT NEWID() creates the GUID
procedure captures the generated GUID
procedure returns it through an OUTPUT parameter
```

### 4.2 When a procedure may create the GUID itself

A procedure may call `NEWID()` manually only when the ID must be known before the insert, such as when multiple rows need the same new ID inside one transaction.

Example:

```sql
DECLARE @new_series_id UNIQUEIDENTIFIER = NEWID();

INSERT INTO manga.Series
(
    series_id,
    title,
    slug
)
VALUES
(
    @new_series_id,
    @title,
    @slug
);
```

Use this only when necessary. For ordinary create procedures, prefer table defaults plus `OUTPUT inserted.<id>`.

### 4.3 Procedure parameter and variable types

All ID parameters, output parameters, local variables, and table variables should use `UNIQUEIDENTIFIER`.

Examples:

```sql
@user_id UNIQUEIDENTIFIER
@series_id UNIQUEIDENTIFIER
@file_resource_id UNIQUEIDENTIFIER
@new_user_id UNIQUEIDENTIFIER OUTPUT
```

Avoid old numeric ID types:

```sql
INT
BIGINT
SMALLINT
```

for business IDs and foreign-key IDs in the GUID-based schema.

### 4.4 Lock resource strings with GUID IDs

When using a GUID in an application lock resource, convert it to text.

```sql
SET @lock_resource =
    N'auth_user_status_change_' + CONVERT(NVARCHAR(36), @target_user_id);
```

### 4.5 Audit entity IDs with GUID values

If an audit procedure stores `entity_id` as text, convert GUID IDs to a variable before passing them.

Do not pass expressions directly inside `EXEC` named parameters.

Avoid:

```sql
EXEC audit.usp_AuditEvent_Append
    @entity_id = CONVERT(NVARCHAR(36), @target_user_id);
```

Use:

```sql
DECLARE @audit_entity_id NVARCHAR(100);

SET @audit_entity_id = CONVERT(NVARCHAR(36), @target_user_id);

EXEC audit.usp_AuditEvent_Append
    @entity_id = @audit_entity_id;
```

This avoids SQL Server syntax issues with expressions in procedure parameter assignment.

### 4.6 Backend compatibility reminder

Keeping names like `UserId`, `SeriesId`, and `FileResourceId` is acceptable.

The backend type must change from:

```csharp
int
long
```

to:

```csharp
Guid
```

SQL parameters should use:

```csharp
SqlDbType.UniqueIdentifier
```

not:

```csharp
SqlDbType.Int
SqlDbType.BigInt
```

### 4.7 `NEWID()` vs `NEWSEQUENTIALID()` project rule

For this project, use `NEWID()` unless the team explicitly changes the ID-generation standard.

Recommended table default:

```sql
CONSTRAINT df_users_user_id DEFAULT NEWID()
```

`NEWSEQUENTIALID()` can reduce index page splits and random I/O when GUIDs are used as row identifiers, but it is more predictable than random GUIDs. Since this project values a simple lecturer-compliant GUID/UUID implementation, use `NEWID()` as the default rule for business/domain tables. `audit.AuditEvent.audit_event_id` may remain `BIGINT IDENTITY`.

### 4.8 SeriesProposal submit procedure rule

`proposal_version_no` should be generated inside `manga.usp_SeriesProposal_Submit`, not by the backend.

Recommended pattern:

```text
acquire per-series application lock
→ read current Series row with locking
→ calculate MAX(proposal_version_no) + 1
→ create FileResource
→ insert SeriesProposal
→ return series_proposal_id and proposal_version_no as OUTPUT values
```

The backend should not pass:

```sql
@proposal_version_no
@proposal_title
@synopsis_snapshot
@genre_snapshot
```

The procedure should read these snapshot values from the current `manga.Series` row:

```sql
SELECT
    @proposal_title = s.title,
    @synopsis_snapshot = s.synopsis,
    @genre_snapshot = s.genre
FROM manga.Series s WITH (UPDLOCK, HOLDLOCK)
WHERE s.series_id = @series_id;
```

Keep the table constraint:

```sql
UNIQUE(series_id, proposal_version_no)
```

as final database protection against duplicate proposal versions.

## 5. Concurrency and locking

Use locks only where concurrency could create inconsistent workflow behavior.

### Use `sys.sp_getapplock` for

- changing one user’s status,
- resetting one user’s password,
- appending audit events under high concurrency,
- opening/closing board polls,
- applying board poll results,
- creating records where duplicate rapid clicks would create confusing behavior.

### Do not rely only on locks

Locks improve behavior, but database constraints remain the real protection.

For example, rapid registration clicks are ultimately protected by:

```sql
UNIQUE(username)
UNIQUE(email)
```

The lock only makes the behavior cleaner.

### Important SQL Server syntax rule

Do **not** pass expressions such as `CONCAT(...)`, `CONVERT(...)`, or `CASE ... END` directly to named procedure parameters.

Avoid:

```sql
EXEC @lock_result = sys.sp_getapplock
    @Resource = CONCAT(N'auth_user_status_change_', @target_user_id),
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;
```

Use a variable instead:

```sql
DECLARE @lock_resource NVARCHAR(255);

SET @lock_resource = N'auth_user_status_change_' 
    + CONVERT(NVARCHAR(36), @target_user_id);

EXEC @lock_result = sys.sp_getapplock
    @Resource = @lock_resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;
```

This same rule also applies to audit calls and other procedure calls:

```sql
DECLARE @audit_entity_id NVARCHAR(100);

SET @audit_entity_id = CONVERT(NVARCHAR(36), @target_user_id);

EXEC audit.usp_AuditEvent_Append
    @entity_id = @audit_entity_id;
```

---

## 6. Audit procedure behavior

The audit procedure owns actor role resolution.

Procedures should call audit with precomputed values like this:

```sql
DECLARE @entity_id NVARCHAR(100);

SET @entity_id = CONVERT(NVARCHAR(36), @some_guid_id);

EXEC audit.usp_AuditEvent_Append
    @actor_user_id = @actor_user_id,
    @action_code = N'SOME_ACTION',
    @entity_type = N'SomeEntity',
    @entity_id = @entity_id,
    @detail_json = @detail_json;
```

Do **not** pass:

```sql
@actor_role_name
```

The audit procedure should resolve `actor_role_name` internally by joining:

```sql
auth.Users -> auth.Roles
```

### Audit JSON rule

Keep `detail_json` focused on the actual business change.

For example, for user status change, this is enough:

```sql
SELECT @detail_json =
(
    SELECT
        @target_user_id AS user_id,
        @old_status_code AS old_status_code,
        @new_status_code AS new_status_code,
        @reason AS reason
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);
```

Do not add unrelated fields unless they are useful snapshots for the action.

---

## 7. Role resolution pattern

The project’s role table is:

```sql
CREATE TABLE auth.Roles (
    role_id UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT df_roles_role_id DEFAULT NEWID()
        CONSTRAINT pk_roles PRIMARY KEY,
    role_name NVARCHAR(30) NOT NULL,
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);
```

If a procedure receives a role from the application, receive `@role_name`, then resolve `role_id` inside the procedure.

Example:

```sql
DECLARE @resolved_role_id UNIQUEIDENTIFIER;
DECLARE @resolved_role_name NVARCHAR(30);

SELECT
    @resolved_role_id = r.role_id,
    @resolved_role_name = r.role_name
FROM auth.Roles r
WHERE UPPER(LTRIM(RTRIM(r.role_name))) = UPPER(LTRIM(RTRIM(@role_name)));

IF @resolved_role_id IS NULL
BEGIN
    ;THROW 50015, 'Invalid role name.', 1;
END;
```

Do not let the frontend/backend pass raw `role_id` for business actions unless there is a strong reason.

---

## 8. User table assumptions

Current user table uses GUID/UUID-style ID columns:

```sql
auth.Users
- user_id UNIQUEIDENTIFIER
- role_id UNIQUEIDENTIFIER
- username
- display_name
- email
- password_hash
- avatar_file_id UNIQUEIDENTIFIER
- portfolio_file_id UNIQUEIDENTIFIER
- status_code
- created_at_utc
```

Do not write procedures that update these columns unless they exist in the schema:

- `updated_at_utc`
- `failed_login_attempts`
- `last_failed_login_at`
- `locked_until`

If those columns are added later, procedures may be updated to use them.

---

## 9. Display name rules

- `username` is the login/system identifier.
- `display_name` is the user-facing identity.
- `display_name` is not unique.
- If no display name is provided, default it to `username`.
- Users may update their own display name without entering their password.
- Updating display name must not change username, email, password, role, status, or approval state.
- Empty or whitespace-only display names should be rejected after trimming.
- Display name changes should be audit-logged.

---

## 10. Procedure validation style

### Do not duplicate schema constraints

Avoid procedure checks for:

- allowed status values already covered by `CHECK`,
- duplicate username/email already covered by `UNIQUE`,
- missing required columns already covered by `NOT NULL`,
- GUID ID generation already covered by `DEFAULT NEWID()`,
- invalid role ID already covered by `FOREIGN KEY`.

### Do validate business meaning

Keep procedure checks for:

- actor permission,
- record existence when needed for workflow,
- current state before transition,
- same-page/same-series integrity,
- ownership rules,
- one-open-poll rules,
- audit JSON validity,
- required business reason when the reason is conditional.

---

## 11. Error handling style

Use project-specific error numbers by procedure area.

Example ranges:

| Area | Suggested Range |
|---|---:|
| Audit | 50000–50999 |
| User create/account | 51000–51999 |
| Password/reset | 52000–52999 |
| User status/admin account | 53000–53999 |
| Profile/display name | 54000–54999 |
| Board poll/vote/result | 55000–56999 |
| Series/proposal/chapter/page | 57000–59999 |


### `THROW` statement semicolon rule

SQL Server requires the statement before `THROW` to be terminated with a semicolon. To avoid parser or editor issues, especially in SSMS or Visual Studio SQL scripts, use a leading semicolon before `THROW`.

Recommended style:

```sql
IF @lock_result < 0
BEGIN
    ;THROW 54101, 'Could not acquire user portfolio update lock.', 1;
END;
```

This leading semicolon safely terminates any previous statement before `THROW`. It is harmless and helps avoid messages such as:

```text
Incorrect syntax near 'THROW'.
```

Use this style for all future stored procedures when raising custom errors with `THROW`.

Use clear error messages, but do not over-validate schema constraints.

---

## 12. Standard procedure checklist

Before finalizing a stored procedure, check:

- Does it match the actual current table columns?
- If the procedure calls another procedure, does it pass variables instead of inline expressions such as `CONVERT(...)`, `CONCAT(...)`, or `CASE ... END`?
- If the procedure submits a series proposal, does it generate `proposal_version_no` inside the database using a per-series lock?
- If the procedure submits a series proposal, does it read proposal snapshot fields from `manga.Series` instead of accepting them from the backend?
- Is `audit.AuditEvent.audit_event_id BIGINT IDENTITY` treated as the intentional audit/ledger exception rather than a GUID migration mistake?
- If the procedure inserts into a GUID primary-key table, does it let `DEFAULT NEWID()` generate the ID unless pre-generation is truly needed?
- If the procedure creates a row and needs the new ID, does it use `OUTPUT inserted.<id>` into a table variable and return a `UNIQUEIDENTIFIER OUTPUT` value?
- Are all ID parameters, output parameters, local variables, and table-variable ID columns using `UNIQUEIDENTIFIER`?
- If it uses GUID IDs in lock-resource strings, does it convert them with `CONVERT(NVARCHAR(36), @id)`?
- If backend examples are included, do they use `Guid` and `SqlDbType.UniqueIdentifier` instead of `int/long` and `SqlDbType.Int/BigInt`?
- Did it avoid duplicate checks for schema constraints?
- Did it validate actor permission?
- Did it validate workflow rules not enforced by schema?
- Does it use a transaction only when needed?
- If it uses `sp_getapplock`, does it pass a variable as `@Resource`?
- Does it call `audit.usp_AuditEvent_Append` without `@actor_role_name` and with precomputed `@entity_id` variables?
- Is `detail_json` focused only on relevant change details?
- Does it avoid updating columns that do not exist?
- Does it preserve MVP scope and avoid extra tables unless required?
- If it uses `THROW`, does it use `;THROW` to avoid SQL Server parser issues?
