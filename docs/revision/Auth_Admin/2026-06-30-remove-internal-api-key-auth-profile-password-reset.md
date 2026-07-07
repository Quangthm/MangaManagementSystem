# 2026-06-30 - Remove Internal API Key Flow from Auth/Profile/Password Reset

## Context

Leader requested the original login/logout/admin scope to be reviewed again because `InternalApiOptions` and the internal API key flow were still present after the admin API JWT refactor.

The follow-up review confirmed that Admin Users, Admin Audit, and Admin Files had already moved to JWT bearer authorization, but Auth Google login/signup, Password Reset, and Profile still had remaining internal API key dependencies.

## Changes

### API

- Removed internal API key validation from `AuthController`.
- Kept normal login, Google login resolve, and Google signup routed through API endpoints.
- Marked public auth endpoints with `[AllowAnonymous]` where appropriate.
- Removed internal API key validation from `PasswordResetController`.
- Kept password reset request/complete as public API endpoints because users do not have a JWT while requesting or completing password reset.
- Refactored `ProfileController` to use `[Authorize]` and resolve the authenticated actor from JWT claims instead of internal headers.
- Removed obsolete `InternalApiOptions` registration from API startup.
- Deleted obsolete API `InternalApiOptions` class.

### Web

- Removed internal API key header usage from `AuthApiClient`.
- Removed internal API key header usage from `PasswordResetApiClient`.
- Removed internal API key and actor-header usage from `ProfileApiClient`.
- Registered `IProfileApiClient` with `ApiAuthorizationMessageHandler`, matching the JWT bearer pattern used by admin API clients.
- Removed obsolete `InternalApiOptions` registration from Web startup.
- Deleted obsolete Web `InternalApiOptions` class.
- Removed obsolete Development `InternalApi` config blocks from API and Web appsettings.

## Validation

- Ran `dotnet build .\MangaManagementSystem.slnx`.
- Build pass.
- Ran source search under `src` for the removed internal-key runtime patterns:
  - `InternalApiOptions`
  - `InternalApi`
  - `X-Internal-Api-Key`
  - `CreateInternalRequest`
  - `HasValidInternalApiKey`
  - `InternalUnauthorized`
- Source search returned no runtime code/config matches under `src`.

## Notes

- No database, stored procedure, schema, migration, or user-secret script changes were made.
- `setup-secrets.ps1` was kept outside the Git repo and was not committed.
- `../tem` remains untracked and was not touched.
- Historical docs and old test scripts may still mention the previous internal API key design as past revision history, but runtime source and config no longer depend on it.
