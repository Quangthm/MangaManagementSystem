# Mangaka JWT Authentication Migration — Batch A

## Branch

`feature/Mangaka`

## Task summary

Migrated only the Mangaka Series and Mangaka Chapters API/Web paths from the
transitional `X-Actor-User-Id` actor header to the existing JWT bearer
authentication infrastructure. Batch B, Editor Batch C, Assistant, Admin, and
the shared `SeriesController` workspace entry remain unchanged.

## Authentication and authorization

Previous transport:

```text
Blazor typed client
-> X-Actor-User-Id
-> API private header parser
```

New transport:

```text
Browser authentication cookie
-> api_access_token cookie claim
-> ApiAuthorizationMessageHandler
-> Authorization: Bearer <JWT>
-> API JWT validation
-> [Authorize(Roles = "Mangaka")]
-> AuthenticatedActorResolver
-> existing MediatR/Application authorization
```

`AuthenticatedActorResolver`:

- Prefers `ClaimTypes.NameIdentifier`.
- Supports the existing `sub` and `user_id` compatibility claims.
- Requires a non-empty GUID.
- Loads the current account through `IUserService`.
- Requires `StatusCode == ACTIVE`.
- Requires the current database role to remain `Mangaka`.
- Does not read `X-Actor-User-Id`.
- Does not contain contributor, series, chapter, or workflow authorization.

Controller failure mapping matches the Admin reference:

| Failure | HTTP result |
| --- | --- |
| Invalid or malformed actor claim | 401 |
| Actor ID no longer exists in the database | 401 |
| Current account is not ACTIVE | 403 |
| Current database role is not Mangaka | 403 |

The JWT role check and current database ACTIVE/role revalidation are both
intentional. Existing Application contributor and workflow checks remain
authoritative.

## Files changed

### API

- `src/MangaManagementSystem.API/Security/AuthenticatedActorResolver.cs`
- `src/MangaManagementSystem.API/Program.cs`
- `src/MangaManagementSystem.API/Controllers/Mangaka/MangakaSeriesController.cs`
- `src/MangaManagementSystem.API/Controllers/Mangaka/MangakaChaptersController.cs`

### Web clients and registration

- `src/MangaManagementSystem.Web/Program.cs`
- `src/MangaManagementSystem.Web/Services/Api/IMangakaSeriesApiClient.cs`
- `src/MangaManagementSystem.Web/Services/Api/MangakaSeriesApiClient.cs`
- `src/MangaManagementSystem.Web/Services/Api/IMangakaChapterApiClient.cs`
- `src/MangaManagementSystem.Web/Services/Api/MangakaChapterApiClient.cs`

The Series client retained its existing single authorization handler. The
Chapter client now has exactly one `ApiAuthorizationMessageHandler`.
Authentication-only `actorUserId` parameters were removed from both client
contracts. Series, proposal, chapter, source-series, and other business IDs
were retained. Multipart form fields, files, content types, and response
handling were not changed.

### Updated callers

- `Components/Pages/Mangaka/MangakaDashboard.razor`
- `Components/Pages/Mangaka/Proposals.razor`
- `Components/Pages/Mangaka/ProposalDetail.razor`
- `Components/Pages/Mangaka/Chapters.razor`
- `Components/Pages/Mangaka/ManageContributors.razor`
- `Components/Pages/Mangaka/ReviewSubmissions.razor`
- `Components/Pages/Workspace/CreatorWorkspace.Save.cs`
- `Components/Pages/Workspace/CreatorWorkspace.razor.cs`
- `Components/Pages/Publication/PublicationScheduleActionDrawer.razor`
- `Components/Pages/Publication/ScheduleCalendar.razor`

Only calls to the two changed typed-client interfaces were updated. Existing
current-user state was retained where the component still uses it for other
roles, contributor operations, workspace behavior, or UI logic.

## Architecture and database impact

The existing flow remains:

```text
Blazor Web
-> typed API client
-> API controller
-> IMediator.Send
-> Application handler
-> Infrastructure
-> SQL Server
```

Application workflow contracts, handlers, Domain, Infrastructure, EF mappings,
schema, migrations, SQL, and stored procedures were not changed.

## Transitional-header cleanup

The focused search returned zero results in:

- `MangakaSeriesController.cs`
- `MangakaChaptersController.cs`
- `MangakaSeriesApiClient.cs`
- `MangakaChapterApiClient.cs`

Remaining repository-wide results were intentionally preserved:

- Mangaka Batch B: contributor management, tasks/Quick Select, pages/page
  versions, regions, annotations, and their Web clients.
- Editor Batch C: dashboard, proposals, chapter review/scheduling,
  annotations, series, their client interfaces, and their Web clients.
- Shared deferred path: `SeriesController` workspace entry and
  `SeriesApiClient`.
- Assistant/out of scope: Assistant task/completed-work controllers and
  `AssistantTaskApiClient`.
- Comments/documentation: task and annotation DTO documentation.

## Verification

### Builds

- API project: succeeded, 0 errors, 26 existing warnings.
- Web project: succeeded, 0 errors, 40 existing warnings.
- Full `MangaManagementSystem.slnx --no-incremental`: succeeded, 0 errors,
  66 existing warnings.

The first sandboxed API build attempt was blocked by NuGet network access. It
was repeated with approved network access and succeeded.

### Tests and static checks

- No test project exists in the current local solution tree, so no automated
  authorization tests were added or run.
- Razor/client compilation verified all changed signatures.
- `git diff --check` passed; only line-ending conversion notices were emitted.
- Static inspection confirmed `[Authorize(Roles = "Mangaka")]` on both
  controllers, corrected 401/403 mappings, current-account revalidation, and
  zero header influence in the migrated paths.

Runtime JWT, disabled-account, role-change, spoofed-header, contributor-denial,
and multipart smoke tests were not run because the API/Web applications and
test data were not started.

## Remaining work

- Batch B: remaining Mangaka transitional paths.
- Batch C: all Tantou Editor transitional paths.
- Separate future scope: Assistant paths and shared workspace entry.
- Perform runtime authorization and workflow smoke testing with suitable local
  accounts and data.

## Commit/push status

Nothing was committed or pushed.
