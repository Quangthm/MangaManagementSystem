# Business Flows / Use Cases




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Addendum â€” Auth/Admin Account Management Authorization Boundary

**Status:** Agreed  
**Scope:** Phase 7 documentation and auth workflow testing alignment  
**Last updated:** 2026-06-21

### Architecture Responsibility Note

For account registration, account approval, account rejection, account activation, and Admin authorization workflows, the C# Application layer owns the business decision and orchestration.

The Application layer is responsible for:

- validating the requested public registration role against the public registration whitelist;
- deciding that a public registration request, including an Admin role request, can only create a `PENDING_APPROVAL` account;
- deciding whether the current actor is allowed to perform Admin-only account-management operations;
- orchestrating Web/API requests, Application commands/queries, Infrastructure procedure calls, and Cloudinary-related compensation when files are involved;
- returning safe user-facing errors for invalid state, invalid role, rejected account, disabled account, pending account, unauthorized request, or forbidden Admin action.

SQL Server procedures remain the persistence and audit boundary. They insert/update/link records, preserve database consistency, enforce constraints/backstop checks, own important SQL transactions, and append audit events. New documentation and test scripts must not describe the database as the primary owner of UI/API business orchestration.

---

## BF-AUTH-007 â€” Public Admin Registration Request Remains Pending

**Status:** Agreed  
**Primary actor:** New User  
**Goal:** Allow a public user to request the Admin role without creating an active Admin account directly.

### Main Flow

```text
UI register form
â†’ User selects role_name = Admin
â†’ Backend API receives registration request
â†’ C# Application validates Admin is in the public registration whitelist
â†’ C# Application starts the normal registration/OTP workflow
â†’ C# Application hashes password after OTP completion
â†’ Infrastructure calls auth.usp_User_Create or auth.usp_User_CreateWithOptionalPortfolio
â†’ Database inserts auth.Users row with role = Admin and status_code = PENDING_APPROVAL
â†’ Database writes USER_REGISTERED audit event
â†’ Backend returns pending approval response
â†’ UI shows pending approval page/message
```

### Important Notes

- Public registration may request `Admin` only because the Application public-registration whitelist currently allows it.
- Public registration must never create an `ACTIVE` Admin directly.
- The account remains `PENDING_APPROVAL` until an existing active Admin approves it through the Admin account-management workflow.
- A pending Admin account cannot log in to protected Admin pages or protected workspace functions.
- This flow is not a database-only decision. C# Application validates the requested role and orchestrates the registration workflow; SQL persists the user row and audit event.

### Expected Result

```text
Requested role: Admin
Created role: Admin
Created status_code: PENDING_APPROVAL
Login before approval: blocked
Admin dashboard access before approval: blocked
```

---

## BF-AUTH-008 â€” Direct Internal Google Signup Request Is Rejected

**Status:** Agreed  
**Primary actor:** External caller / malicious or accidental direct request  
**Goal:** Ensure direct callers cannot bypass the Web-to-API boundary and create Google Signup accounts directly.

### Main Flow

```text
External caller sends POST /api/auth/google-signup directly
â†’ Request does not contain valid X-Internal-Api-Key
â†’ API rejects the request before Application command processing
â†’ No user account is created
â†’ No role is assigned
â†’ No pending or active account is inserted
â†’ API returns 401 Unauthorized
```

### Important Notes

- `POST /api/auth/google-signup` is an internal Web-to-API endpoint.
- The Web project sends `X-Internal-Api-Key` when processing legitimate Google Signup.
- Missing or invalid internal API key must return `401 Unauthorized`.
- A direct external request must not create an Admin, Mangaka, Assistant, Editor, Board Member, or Board Chief account.
- This test verifies the request boundary. It should be tested through HTTP/API, not by calling a stored procedure directly.

---

## BF-AUTH-009 â€” Direct URL Authorization Cannot Bypass Role or Status

**Status:** Agreed  
**Primary actor:** Authenticated non-Admin user / pending user / rejected user / disabled user  
**Goal:** Ensure typing protected routes directly does not bypass authorization.

### Main Flow

```text
User signs in or attempts to access a protected route
â†’ User types an Admin URL directly, such as /admin or /admin/users
â†’ Web route authorization checks authentication, role, and account status
â†’ API endpoints also validate Admin actor authorization where required
â†’ If user is not an active Admin, access is denied
â†’ UI redirects to login, pending approval, or access denied as appropriate
```

### Expected Results

| Actor/account state | Direct URL/API result |
|---|---|
| Anonymous user | Redirect to login or return 401 |
| `PENDING_APPROVAL` user | Pending approval / access blocked |
| `REJECTED` user | Login blocked / access blocked |
| `DISABLED` user | Login blocked / access blocked |
| Active non-Admin user | Access denied or 403 for Admin pages/API |
| Active Admin user | Admin routes and Admin API actions allowed |

### Important Notes

- UI hiding is not enough. Direct URL access must also be blocked by route/API authorization.
- Admin API endpoints must not rely only on the visible Admin menu.
- The Application/Admin workflow must validate the actor and reject unauthorized operations.

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

