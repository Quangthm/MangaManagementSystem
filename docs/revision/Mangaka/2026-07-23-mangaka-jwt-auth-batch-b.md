# Mangaka JWT Authentication Migration — Batch B

Date: 2026-07-23  
Branch: `feature/Mangaka`

## Scope

Batch B migrates the remaining Mangaka contributor, task, Quick Select, page/version/file, region, and annotation workflows from the transitional `X-Actor-User-Id` header to the authenticated bearer JWT. Editor, Assistant, Admin, and the shared `SeriesController` workspace entry remain out of scope.

Relevant existing requirements include:

- Contributors: `US-MANGAKA-002`; `FR-SC-001` through `FR-SC-008`.
- Tasks and Quick Select: `US-MANGAKA-014` through `US-MANGAKA-017`; relevant `FR-PGTASK-003` through `FR-PGTASK-039`.
- Pages and versions: `US-MANGAKA-009`, `US-MANGAKA-010`, `US-MANGAKA-010A`, `US-MANGAKA-011`; `FR-CP-001` through `FR-CP-027`.
- Regions: `US-PAGE-002`, `US-MANGAKA-012`; `FR-REG-001` through `FR-REG-033`.
- Annotations: `US-PAGE-004`, `US-PAGE-009`, `US-PAGE-010`, `US-MANGAKA-012A`, `US-MANGAKA-012B`; `FR-ANN-001` through `FR-ANN-028`.

No separate authentication requirement was identified; this change applies the established Batch A API-boundary authentication pattern without changing the business requirements.

## Authentication change

Previously, the Blazor Web clients parsed the current user ID and sent it in `X-Actor-User-Id`. The API controllers trusted that header as the authenticated caller identity.

The migrated Web clients now use `ApiAuthorizationMessageHandler`, which forwards the existing `api_access_token` claim as an `Authorization: Bearer` JWT. Each migrated controller has `[Authorize(Roles = "Mangaka")]` and uses the existing `IAuthenticatedActorResolver` to:

1. obtain the authenticated user ID from the claims principal;
2. load the current database account;
3. require an `ACTIVE` account;
4. require the current database role to remain `Mangaka`; and
5. return the authoritative actor user ID.

Controller failure mapping remains:

- invalid identity or missing database user: `401 Unauthorized`;
- inactive account or wrong current database role: `403 Forbidden`.

The resolver never reads `X-Actor-User-Id`. A spoofed header therefore has no influence on the migrated endpoints.

## Actor continuity audit

Removing `actorUserId` from a Web client signature removes only client-supplied authentication plumbing. The authenticated actor remains in the backend business flow:

`Bearer JWT -> IAuthenticatedActorResolver -> actorUserId -> existing command/query/service -> existing handler/repository/persistence and audit behavior`

Verified mutation paths:

- Contributor add/end commands receive the JWT-derived actor; `assistantUserId` remains a distinct target-user ID.
- Task create, approve, rework, cancel, and reassign operations receive the JWT-derived actor; `AssignedToUserId` remains a target-user ID.
- Quick Select chapter/assistant selection and task assignment receive the JWT-derived actor; the selected assistant remains a target user.
- Page deletion, page/version creation, version/file creation, and image deletion receive the JWT-derived actor through their existing backend contracts.
- Region creation and bulk replacement set `CreatedByUserId` from the JWT-derived actor; full-page region operations also receive that actor.
- Annotation creation sets `AnnotatedByUserId` from the JWT-derived actor, and annotation resolution receives the same actor.

Existing page note/version/current-version operations did not carry an actor into their downstream contracts before this migration. They now require JWT/current-account revalidation at the controller boundary, but their Application contracts were intentionally not redesigned in Batch B.

No Application command/query, handler, repository, stored procedure, audit, or database contract was changed.

## Files changed

### API controllers

- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaSeriesContributorController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaTaskController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/QuickSelectController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaPageController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaPageRegionController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaAnnotationController.cs`

### Web registration, interfaces, and clients

- `MangaManagementSystem/src/MangaManagementSystem.Web/Program.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaSeriesContributorApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaSeriesContributorApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaTaskApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaTaskApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaPageApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaPageApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaPageRegionApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaPageRegionApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaAnnotationApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaAnnotationApiClient.cs`

Each migrated typed client is registered with `ApiAuthorizationMessageHandler` exactly once. Authentication-only `actorUserId` parameters and all transitional-header writes were removed. Resource and target IDs remain in their existing signatures and request bodies.

### Razor/component callers

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/ManageContributors.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/ReviewSubmissions.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.Pages.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.Save.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.Versions.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.razor.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/TaskWorkspaceRedirect.razor`

Only arguments broken by the migrated interfaces were removed. Current-user state was removed from the two Mangaka pages where it became unused. Creator Workspace identity state was retained because it still supports UI/workspace behavior and out-of-scope Assistant calls.

## Transitional-header cleanup

The focused search over the six migrated controllers and five migrated client implementations returns zero matches for:

- `X-Actor-User-Id`
- `ActorUserIdHeader`
- `TryResolveActorUserId`

Repository-wide remaining matches are intentionally unchanged:

- Shared/out of scope: `SeriesController.cs`, `SeriesApiClient.cs`.
- Editor Batch C: Editor series, proposal, dashboard, chapter-review, and annotation controllers/clients, plus two Editor interface comments.
- Assistant/out of scope: Assistant task/completed-work controllers and the Assistant task client.
- Comments/documentation: stale header descriptions in `ChapterPageTaskDtos.cs` and `ChapterPageAnnotationDtos.cs`.

There are no remaining Batch B runtime header usages.

## Verification

- Static call-site and signature searches: completed.
- Actor-continuity inspection for changed mutations: completed.
- Focused transitional-header search: zero matches.
- `git diff --check`: passed; Git emitted line-ending conversion warnings only.
- API build: not run, per the Batch B instruction prohibiting builds.
- Web build: not run, per the Batch B instruction prohibiting builds.
- Tests: not run, per the Batch B instruction prohibiting tests.
- Browser/manual smoke: not run, per the Batch B instruction prohibiting browser execution.

## Impact and remaining work

- Application workflow impact: none.
- Domain impact: none.
- Infrastructure/repository impact: none.
- Database, schema, migration, and stored-procedure impact: none.
- Batch A controllers/clients were not modified.
- Editor Batch C, Assistant migration, Admin changes, and shared Series workspace-entry migration remain separate future work.
