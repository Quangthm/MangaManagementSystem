# Project Context




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Auth/Admin Alignment Notes

### Application vs Database Responsibility

For Phase 7 documentation and tests, the accepted responsibility split is:

```text
C# Application:
- business decision
- workflow orchestration
- role whitelist validation
- actor authorization decision
- account-status behavior decision
- safe error/result mapping
- Cloudinary or external-service orchestration when involved

SQL Server:
- insert/update/link persistence
- transaction consistency for database writes
- database constraints and safety backstops
- audit append through audit.usp_AuditEvent_Append
```

This means documentation should avoid describing stored procedures as the only or primary business decision layer. The database remains important, but the Application layer decides and orchestrates the workflow before Infrastructure calls SQL.

### Auth/Admin Behavior Confirmed for Phase 7

- Public registration may request `Admin` if `Admin` is present in the Application public registration whitelist.
- Public Admin registration creates a `PENDING_APPROVAL` Admin account, not an `ACTIVE` Admin account.
- Pending users, including pending Admin users, cannot access protected functions.
- Rejected and disabled users cannot log in.
- Rejected users keep username and email reserved in MVP.
- An existing active Admin must approve a pending Admin before that account can access Admin routes.
- Direct calls to internal Google Signup must fail without the internal API key.
- Directly typing Admin URLs must not bypass route/API authorization.

### Phase 7 Test Scope

Phase 7 tests should cover:

1. stored-procedure persistence/backstop behavior for user create and Admin status changes;
2. auth workflow behavior showing public Admin registration remains pending;
3. rejected/disabled/pending login blocking;
4. direct internal Google Signup endpoint rejection without internal key;
5. direct URL/Admin API authorization blocking for non-Admin actors.

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

