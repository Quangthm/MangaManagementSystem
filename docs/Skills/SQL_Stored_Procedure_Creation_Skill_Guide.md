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
    FROM auth.Users u
    INNER JOIN auth.Roles r
        ON r.role_id = u.role_id
    WHERE u.user_id = @admin_user_id
      AND u.status_code = N'ACTIVE'
      AND r.role_name = N'Admin'
)
BEGIN
    THROW 53102, 'Admin user is not active or does not have permission.', 1;
END;
```

### 2.2 Business workflow rules

Validate workflow rules that depend on current database state.

Examples:

- A `START_SERIALIZATION` poll can only open when a series is `UNDER_BOARD_REVIEW`.
- A Mangaka may only update preferred publication frequency while the series is `PROPOSAL_DRAFT`.
- A chapter can only be submitted when required current page versions exist.
- An Editorial Board Chief must provide a reason when directly changing official publication frequency.

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
    THROW 50001, 'detail_json must be valid JSON.', 1;
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

    THROW;
END CATCH;
```

### Rule

Only commit or roll back if the procedure started the transaction.

This allows procedures to be safely called by other procedures that already own a transaction.

---

## 4. Concurrency and locking

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

Do **not** pass expressions such as `CONCAT(...)` directly to named procedure parameters.

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
    + CONVERT(NVARCHAR(20), @target_user_id);

EXEC @lock_result = sys.sp_getapplock
    @Resource = @lock_resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;
```

---

## 5. Audit procedure behavior

The audit procedure owns actor role resolution.

Procedures should call audit like this:

```sql
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

## 6. Role resolution pattern

The project’s role table is:

```sql
CREATE TABLE auth.Roles (
    role_id SMALLINT IDENTITY PRIMARY KEY,
    role_name NVARCHAR(30) NOT NULL,
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);
```

If a procedure receives a role from the application, receive `@role_name`, then resolve `role_id` inside the procedure.

Example:

```sql
DECLARE @resolved_role_id SMALLINT;
DECLARE @resolved_role_name NVARCHAR(30);

SELECT
    @resolved_role_id = r.role_id,
    @resolved_role_name = r.role_name
FROM auth.Roles r
WHERE UPPER(LTRIM(RTRIM(r.role_name))) = UPPER(LTRIM(RTRIM(@role_name)));

IF @resolved_role_id IS NULL
BEGIN
    THROW 50015, 'Invalid role name.', 1;
END;
```

Do not let the frontend/backend pass raw `role_id` for business actions unless there is a strong reason.

---

## 7. User table assumptions

Current user table:

```sql
auth.Users
- user_id
- role_id
- username
- display_name
- email
- password_hash
- avatar_file_id
- portfolio_file_id
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

## 8. Display name rules

- `username` is the login/system identifier.
- `display_name` is the user-facing identity.
- `display_name` is not unique.
- If no display name is provided, default it to `username`.
- Users may update their own display name without entering their password.
- Updating display name must not change username, email, password, role, status, or approval state.
- Empty or whitespace-only display names should be rejected after trimming.
- Display name changes should be audit-logged.

---

## 9. Procedure validation style

### Do not duplicate schema constraints

Avoid procedure checks for:

- allowed status values already covered by `CHECK`,
- duplicate username/email already covered by `UNIQUE`,
- missing required columns already covered by `NOT NULL`,
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

## 10. Error handling style

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

## 11. Standard procedure checklist

Before finalizing a stored procedure, check:

- Does it match the actual current table columns?
- Did it avoid duplicate checks for schema constraints?
- Did it validate actor permission?
- Did it validate workflow rules not enforced by schema?
- Does it use a transaction only when needed?
- If it uses `sp_getapplock`, does it pass a variable as `@Resource`?
- Does it call `audit.usp_AuditEvent_Append` without `@actor_role_name`?
- Is `detail_json` focused only on relevant change details?
- Does it avoid updating columns that do not exist?
- Does it preserve MVP scope and avoid extra tables unless required?
- If it uses `THROW`, does it use `;THROW` to avoid SQL Server parser issues?
