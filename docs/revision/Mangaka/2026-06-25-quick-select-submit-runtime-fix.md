# Quick Select Submit Runtime Fix

## Branch
feature/Mangaka

## Date
2026-06-25

## Summary
Fixed Quick Select submit 500 failure caused by incorrect `sp_getapplock` invocation in `AcquireAppLockAsync`. The method was passing `@Result OUTPUT` as a 5th positional parameter, which SQL Server mapped to `@DbPrincipal` (a string input parameter, not an output parameter), causing SqlException 8162.

## Runtime issue
```
API returned 500:
detail = "Quick Select assignment failed. No tasks were created."

SqlException:
The formal parameter "@DbPrincipal" was not declared as an OUTPUT parameter,
but the actual parameter passed in requested output.

Error Number: 8162

Failing method:
QuickSelectRepository.AcquireAppLockAsync(...)
```

## Root cause
`sp_getapplock` has 4 input parameters (`@Resource`, `@LockMode`, `@LockOwner`, `@LockTimeout`) and returns an **integer return code** (not an output parameter). The broken code used positional parameters and passed `@Result OUTPUT` as the 5th argument. SQL Server interpreted this as `@DbPrincipal`, which is a string input parameter — not an output parameter — causing error 8162.

### Broken code (`QuickSelectRepository.cs:232-238`):
```csharp
await _context.Database.ExecuteSqlRawAsync(
    "EXEC sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout, @Result OUTPUT",
    new SqlParameter("@Resource", lockResource),
    new SqlParameter("@LockMode", "Exclusive"),
    new SqlParameter("@LockOwner", "Transaction"),
    new SqlParameter("@LockTimeout", 5000),
    lockResultParam);
```

## Fix details
`AcquireAppLockAsync` now:
- Opens the underlying `DbConnection` directly (ensures it's open inside the transaction)
- Uses `DbCommand` with named parameters
- Captures the return value via `EXEC @lockResult = sys.sp_getapplock ...`
- Uses `SELECT @lockResult;` + `ExecuteScalarAsync` to retrieve the return code
- Wires the command into the EF transaction via `GetDbTransaction()`
- Logs the numeric lock result on failure

### Fixed code:
```csharp
private async Task AcquireAppLockAsync(QuickSelectAssignmentPlan plan, CancellationToken ct)
{
    var connection = _context.Database.GetDbConnection();

    if (connection.State != ConnectionState.Open)
        await connection.OpenAsync(ct);

    await using var command = connection.CreateCommand();

    var currentTransaction = _context.Database.CurrentTransaction;
    if (currentTransaction is not null)
        command.Transaction = currentTransaction.GetDbTransaction();

    command.CommandText = """
        DECLARE @lockResult int;

        EXEC @lockResult = sys.sp_getapplock
            @Resource = @resource,
            @LockMode = @lockMode,
            @LockOwner = @lockOwner,
            @LockTimeout = @lockTimeout;

        SELECT @lockResult;
        """;

    // Named parameters...
    var resultObj = await command.ExecuteScalarAsync(ct);
    var result = Convert.ToInt32(resultObj, CultureInfo.InvariantCulture);

    if (result < 0)
    {
        _logger.LogWarning("sp_getapplock returned {LockResult} for resource {Resource}", result, resource.Value);
        throw new InvalidOperationException(
            "Another Quick Select assignment is already in progress for this chapter. Please wait and try again.");
    }
}
```

### Usings changed
Removed `using Microsoft.Data.SqlClient;` (no longer needed — moved to `DbCommand` with generic parameters).
Added `using System.Data;`, `using System.Globalization;`, `using Microsoft.EntityFrameworkCore.Storage;`.

## Files changed
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/QuickSelectRepository.cs`
  - Fixed `AcquireAppLockAsync` method: replaced positional `sp_getapplock` call with named-parameter return-value capture
  - Adjusted usings
- `MangaManagementSystem/docs/revision/Mangaka/2026-06-25-quick-select-submit-runtime-fix.md` (this file)

## Build result
```
0 errors, 45 pre-existing warnings
```

## Runtime smoke result
Pending — user must re-run the Quick Select flow. Expected:
1. The SqlException 8162 (`@DbPrincipal` / `@Result OUTPUT`) error is gone.
2. If a new error appears after the app lock succeeds, the new structured logging (added in prior commit) will expose the real inner exception.

## DB verification
Pending — run after successful submit:
```sql
SELECT TOP 10 * FROM manga.PageRegion WHERE type_code = N'FULL_PAGE' ORDER BY created_at_utc DESC;
SELECT TOP 10 * FROM manga.ChapterPageTask ORDER BY created_at_utc DESC;
SELECT TOP 10 * FROM audit.AuditEvent WHERE action_code = N'CHAPTER_PAGE_TASK_CREATED' ORDER BY occurred_at_utc DESC;
```

## Existing behavior preserved
- Quick Select UI (MudAutocomplete, page checkboxes, selected-page count/chips)
- Typed API client calls (MangakaTaskClient)
- Transaction with app lock inside EF execution strategy
- Guard re-checks after lock acquisition
- Full-page PageRegion resolution (create-or-reuse)
- ChapterPageTask batch insert with skip-navigation to PageRegion via junction table
- AuditEvent creation with structured JSON
- Cloudinary image dimension resolution (outside transaction)
- Image dimension validation (Width>0, Height>0 guard)
- Structured logging at repository and controller levels

## Remaining issues
1. **Next error after app lock fix**: If a new error appears after the app lock succeeds, it will be the real persistence issue (likely PageRegion CHECK constraint or junction table insert). The diagnostic logging added in the prior commit logs every staged FULL_PAGE region before SaveChanges.
2. **Follow-up**: ReviewSubmissions or related task-loading path may still use direct EF/Application access from Web instead of typed API. Web logs show EF DbCommand activity alongside typed HTTP client calls. Investigate and migrate to typed API in a separate focused task.

## Next step
1. Retest Quick Select submit. If it succeeds, verify DB writes and clean up diagnostic `LogInformation` if too noisy.
2. If a new error appears, capture the inner exception from the structured logs and debug that specific persistence issue.
