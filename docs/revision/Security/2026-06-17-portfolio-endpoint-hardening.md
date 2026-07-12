# Portfolio Endpoint Security Hardening

**Date:** 2026-06-17

**Branch:** `feature/security-rbac-hardening`

**Status:** Completed and locally verified

## Scope

This session completed the following Phase 2 security items:

* Item 25: Protect portfolio and profile endpoints.

* Item 26: Remove the portfolio debug endpoint.

## Implemented Changes

### 1. Protected the Web-to-API profile boundary

Profile API requests now include:

* `X-Internal-Api-Key`

* `X-Actor-User-Id`

* `X-Actor-Role`

The actor user ID and role are obtained from the authenticated Web session rather than from form input.

The API validates:

* The internal API key.

* The actor user ID.

* The actor account exists.

* The actor account status is `ACTIVE`.

* The supplied actor role matches the role stored in the database.

### 2. Enforced profile ownership

The following endpoints now verify that the authenticated actor is managing their own profile:

* `GET /api/profile/{userId}`

* `PUT /api/profile/{userId}/display-name`

* `POST /api/profile/{userId}/avatar`

* `POST /api/profile/{userId}/portfolio`

Changing the `userId` in the request does not allow a user to access or modify another account.

### 3. Protected profile file metadata

The endpoint below now returns file metadata only when the requested file is the authenticated user's current avatar or portfolio:

* `GET /api/profile/files/{fileResourceId}`

Requests for files belonging to another account are rejected.

Soft-deleted files are not returned.

### 4. Added portfolio ownership lookup

Added an EF Core read query that resolves the owner of a portfolio file through:

`User.PortfolioFileId`

The query follows the project architecture requirement:

* Read/query operations use EF Core.

* No stored procedure was added because this is not a write operation.

### 5. Protected portfolio preview and download

The endpoint below now requires an authenticated and active account:

* `GET /api/portfolio/{fileResourceId}`

Access rules:

* The portfolio owner may access their own portfolio.

* An active Admin may access another user's portfolio for account review and approval.

* Another non-Admin user is denied access.

* An anonymous user receives `401 Unauthorized`.

* A missing or soft-deleted portfolio returns `404 Not Found`.

The endpoint no longer returns internal exception messages to the browser.

### 6. Removed direct portfolio Cloudinary preview

Portfolio images, PDF documents, links, and iframe previews now use:

`/api/portfolio/{fileResourceId}`

The UI no longer renders portfolio files directly from `CloudinarySecureUrl`.

This ensures that portfolio access always passes through the authorization checks.

Avatar handling was not changed because it is outside the scope of this portfolio security item.

### 7. Removed the portfolio debug endpoint

The following endpoint was removed completely:

`GET /api/portfolio/{fileResourceId}/debug`

The removed endpoint previously exposed diagnostic information such as:

* Original file metadata.

* Cloudinary public ID.

* Cloudinary secure URL.

* Cloud name.

* Cloudinary request results.

* Internal exception messages.

* Cloudinary Admin API diagnostic behavior.

The endpoint is no longer available in either Development or Production.

## Verification

### Build

Executed:

```text

dotnet build

```

Result:

```text

Build succeeded with 35 warning(s) and 0 errors.

```

The existing warnings were not addressed because they are outside this security scope.

### Debug endpoint removal

Request:

```text

GET /api/portfolio/11111111-1111-1111-1111-111111111111/debug

```

Result:

```text

404 Not Found

```

### Anonymous portfolio access

Request:

```text

GET /api/portfolio/11111111-1111-1111-1111-111111111111

```

Result:

```text

401 Unauthorized

{"message":"Authentication is required."}

```

### Direct Profile API request without internal key

Request:

```text

GET /api/profile/11111111-1111-1111-1111-111111111111

```

Result:

```text

401 Unauthorized

{"message":"Unauthorized internal request."}

```

### Portfolio owner access

An active Mangaka opened their own portfolio through:

```text

/api/portfolio/e37e2033-235b-442c-98f8-052d71b6fa99

```

Result:

* The PDF loaded successfully in the profile preview.

* The PDF also loaded successfully in a separate browser tab.

* No `401` or `403` response occurred.

### Cross-user access

An active Tantou Editor attempted to open the Mangaka portfolio.

Result:

```json

{

&#x20; "message": "You do not have permission to access this portfolio."

}

```

The portfolio content was not returned.

### Admin access

An active Admin opened the same Mangaka portfolio.

Result:

* The PDF loaded successfully.

* Admin access remained available for user review and approval workflows.

## Verified Authorization Matrix

| Actor                    | Portfolio access result |

| ------------------------ | ----------------------- |

| Anonymous                | `401 Unauthorized`      |

| Portfolio owner          | Allowed                 |

| Different non-Admin user | Denied                  |

| Active Admin             | Allowed                 |

| Missing portfolio        | `404 Not Found`         |

| Soft-deleted portfolio   | `404 Not Found`         |

## Files Changed

* `src/MangaManagementSystem.API/Controllers/ProfileController.cs`

* `src/MangaManagementSystem.API/Options/InternalApiOptions.cs`

* `src/MangaManagementSystem.Application/Interfaces/IUserService.cs`

* `src/MangaManagementSystem.Application/Services/UserService.cs`

* `src/MangaManagementSystem.Domain/Interfaces/IUserRepository.cs`

* `src/MangaManagementSystem.Infrastructure/Repositories/UserRepository.cs`

* `src/MangaManagementSystem.Web/Components/Pages/Admin/UserApproval.razor`

* `src/MangaManagementSystem.Web/Components/Pages/ProfileSettings.razor`

* `src/MangaManagementSystem.Web/Options/InternalApiOptions.cs`

* `src/MangaManagementSystem.Web/Program.cs`

* `src/MangaManagementSystem.Web/Services/Api/ProfileApiClient.cs`

## Security Notes

* The internal API key value is not stored in this document.

* No credential or secret value was committed to source control.

* API and Web must use the same `InternalApi:Key`.

* Local development uses .NET User Secrets.

* Deployment must provide the key through secured environment configuration.

## Remaining Phase 2 Work

The following items remain for subsequent work:

* Separate Pending Approval from Access Denied.

* Consolidate the logout flow and enforce anti-forgery.

* Prevent OAuth stack traces from being returned outside Development.

* Move Board Poll functionality out of Admin.

* Remove Admin from Board Decision authorization.

* Add actor permission checks to file soft-delete operations.
