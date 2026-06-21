# Business Rules




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Addendum â€” Auth/Admin Business Rules and Responsibility Boundary

| Rule ID | Business Rule | Review Status |
|---|---|---|
| BR-ARCH-001 | C# Application owns business decisions and orchestration for registration, Admin account management, authorization, direct-request handling, Cloudinary cleanup orchestration, and user-facing error mapping. | Active draft |
| BR-ARCH-002 | SQL Server procedures are used as persistence, transactional consistency, constraint/backstop, and audit-writing boundaries; they must not be documented as the primary owner of UI/API business orchestration. | Active draft |
| BR-ARCH-003 | Web UI must call protected business workflows through typed API clients and API endpoints, not by directly calling Application services, Infrastructure repositories, DbContext, or stored procedures. | Active draft |
| BR-ARCH-004 | API controllers should remain thin HTTP adapters that bind requests, resolve the actor, call Application commands/queries, and map known failures to safe HTTP responses. | Active draft |
| BR-USER-023 | Public registration may request any role in the Application public-registration whitelist, including `Admin`, but the resulting account must remain `PENDING_APPROVAL`. | Active draft |
| BR-USER-024 | Public registration must never create an `ACTIVE` Admin account directly. Activation of a newly registered Admin account requires an existing active Admin account. | Active draft |
| BR-USER-025 | Pending Admin accounts must be treated the same as other pending accounts: they cannot log in to protected functions or access Admin routes until approved. | Active draft |
| BR-USER-026 | An active Admin may approve a `PENDING_APPROVAL` account, reject a pending account with a reason, disable an active account with a reason, and reactivate a `REJECTED` or `DISABLED` account when the Admin workflow allows it. | Active draft |
| BR-USER-027 | Rejection and disabling must remain audit-visible account-status changes. The user row is preserved; username and email remain reserved in MVP. | Active draft |
| BR-USER-028 | Direct HTTP requests to internal Google Signup endpoints must be rejected unless they include the required internal Web-to-API credential. | Active draft |
| BR-USER-029 | Direct URL entry must not bypass role or account-status authorization for Admin pages, protected workspace pages, or protected API endpoints. | Active draft |
| BR-USER-030 | UI menu visibility is only a usability layer. Route-level and API-level authorization remain required for protected workflows. | Active draft |
| BR-USER-031 | Admin users must not reject, disable, or otherwise remove their own active Admin access through the normal account-management workflow. | Active draft |

### Phase 7 Interpretation Notes

- â€œDirect request cannot create Adminâ€ means a direct request must not create an `ACTIVE` Admin or bypass internal/API authorization. It does **not** mean the public role dropdown must remove `Admin` if the current Application whitelist allows public Admin registration.
- Public Admin registration is acceptable only as a pending approval request.
- An existing active Admin remains the only actor who can activate the pending Admin account through the Admin account-management workflow.
- SQL tests should verify stored-procedure persistence/backstop behavior. HTTP/manual tests should verify direct request and direct URL authorization behavior.

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

