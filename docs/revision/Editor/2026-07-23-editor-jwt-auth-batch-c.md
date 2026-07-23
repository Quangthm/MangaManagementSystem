# Tantou Editor JWT Authentication Migration — Batch C

Date: 2026-07-23  
Branch: `feature/Mangaka`

## Scope

Batch C migrates the current Tantou Editor dashboard, proposal review, chapter review and publication actions, annotation read workspace, and Editor series library from the transitional `X-Actor-User-Id` header to the existing bearer-JWT authentication boundary.

Batch A/B Mangaka behavior, Assistant workflows, Admin/Auth, Editorial Board workflows, and the shared `SeriesController` workspace-entry endpoint remain unchanged.

## Requirement mappings

- Editor dashboard and scoped series/proposal visibility: `US-EDITOR-001`, `US-EDITOR-001A`, `US-EDITOR-019`; `FR-PROP-018` through `FR-PROP-021`, `FR-PROP-025`.
- Proposal queue, claim, detail, revision, board submission, and cancellation: `US-EDITOR-001A`, `US-EDITOR-002`, `US-EDITOR-003`, `US-EDITOR-003A`; `FR-PROP-009`, `FR-PROP-012` through `FR-PROP-016A`, `FR-PROP-018` through `FR-PROP-022`, `FR-PROP-025`.
- Chapter review and optional markup: `US-EDITOR-009`, `US-EDITOR-010`, `US-EDITOR-011`; `FR-CH-REV-001` through `FR-CH-REV-016`.
- Scheduling, hold, and release: `US-EDITOR-012`, `US-EDITOR-014`, `US-EDITOR-015`; `FR-PUB-018` through `FR-PUB-023`, `FR-PUB-SCHEDULED-003` through `FR-PUB-SCHEDULED-007`, `FR-PUB-SCHEDULED-011`, `FR-PUB-SCHEDULED-012`.
- Annotation workspace: `US-EDITOR-006`, `US-EDITOR-007`, `US-EDITOR-008`, `US-EDITOR-008A`; `FR-ANN-001` through `FR-ANN-028` as applicable to the current read-only Editor endpoint.
- Editor series visibility: `US-EDITOR-001`; lifecycle requirements `US-EDITOR-018`, `FR-SERIES-027` through `FR-SERIES-034` remain business context, but the current `EditorSeriesController` contains only the series-list GET and no lifecycle mutation.

No separate authentication-specific requirement ID was identified; Batch C applies the established authenticated API-boundary pattern to existing Editor requirements.

## Authentication pattern reused

The Batch A/B pattern is reused without redesign:

```text
Browser authentication cookie
-> api_access_token claim
-> ApiAuthorizationMessageHandler
-> Authorization: Bearer JWT
-> API JWT validation
-> [Authorize(Roles = "Tantou Editor")]
-> existing IAuthenticatedActorResolver
-> JWT-derived actorUserId
-> existing MediatR command/query
-> existing business authorization and persistence/audit flow
```

No second resolver was created. The resolver never reads `X-Actor-User-Id`.

Current-account failure mapping remains:

| Resolver failure | HTTP result |
| --- | --- |
| `InvalidIdentity` | `401 Unauthorized` |
| `UserNotFound` | `401 Unauthorized` |
| `InactiveAccount` | `403 Forbidden` |
| `WrongRole` | `403 Forbidden` |

JWT role authorization and current database ACTIVE/role revalidation are intentionally both enforced.

## Files changed

### API controllers

- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Editor/EditorDashboardController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Editor/EditorProposalsController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Editor/EditorChapterReviewsController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Editor/EditorAnnotationsController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Editor/EditorSeriesController.cs`

All five current controllers are Tantou Editor-only and now have controller-level `[Authorize(Roles = "Tantou Editor")]`. Every action revalidates the current account through `IAuthenticatedActorResolver`.

### Web interfaces and clients

- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IEditorDashboardApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/EditorDashboardApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IEditorProposalApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/EditorProposalApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IEditorChapterReviewApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/EditorChapterReviewApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IEditorAnnotationApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/EditorAnnotationApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IEditorSeriesApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/EditorSeriesApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Program.cs`

Authentication-only `actorUserId` parameters and custom header writes were removed. Proposal, chapter, series, annotation, and other resource IDs remain unchanged. `ApiAuthorizationMessageHandler` is attached exactly once to each of the five migrated Editor clients.

### Razor/component callers

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/EditorDashboard.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalList.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewList.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewDetail.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/AnnotationWorkspace.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/SeriesList.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.razor.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Publication/PublicationScheduleActionDrawer.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Publication/ScheduleCalendar.razor`

Current-user parsing was removed from the pure Editor pages where it existed only to supply the migrated client parameter.

Current-user state was intentionally retained in:

- `ProposalReviewDetail.razor`, where it excludes the current editor from other-editor display.
- `CreatorWorkspace.razor.cs`, where it is still required for role-aware workspace behavior, the deferred shared workspace-entry client, and out-of-scope Assistant calls.
- Publication scheduling components, where it remains part of authentication/role-aware UI state and component loading guards.

## Actor continuity

| Flow | Client actor removed? | API actor from JWT? | Existing command/service still gets actor? | Audit/reviewer attribution preserved? |
| --- | --- | --- | --- | --- |
| Proposal claim | Yes | Yes | `ClaimEditorialReviewCommand.ActorUserId` | Yes, existing handler/persistence unchanged |
| Request proposal revision | Yes | Yes | `RequestProposalRevisionCommand.ActorUserId` | Yes, existing decision/audit/notification flow unchanged |
| Pass proposal to board | Yes | Yes | `PassProposalToBoardCommand.ActorUserId` | Yes, existing decision/audit/notification flow unchanged |
| Cancel editorial review | Yes | Yes | `CancelProposalReviewCommand.ActorUserId` | Yes, existing decision/audit/notification flow unchanged |
| Submit chapter review decision | Yes | Yes | `SubmitChapterEditorialReviewCommand.ActorUserId` | Yes, reviewer, audit, and notification flow unchanged |
| Review decision with markup | Yes | Yes | `SubmitChapterEditorialReviewCommand.ActorUserId` | Yes, same reviewer/audit path |
| Set planned release date | Yes | Yes | `EditorSetChapterPlannedReleaseDateCommand.ActorUserId` | Yes, existing schedule audit path unchanged |
| Put chapter on hold | Yes | Yes | `PutScheduledChapterOnHoldCommand.ActorUserId` | Yes, existing hold audit path unchanged |
| Release chapter | Yes | Yes | `ReleaseChapterCommand.ActorUserId` | Yes, existing release audit/notification path unchanged |
| Editor annotation read | Yes | Yes | `GetEditorAnnotationsQuery` receives actor | Read-only; no create/resolver audit mutation exists in this Editor controller |
| Editor series list | Yes | Yes | `GetEditorSeriesQuery` receives actor | Read-only; no lifecycle mutation exists in this Editor controller |

The Web signature change only removes client control of caller identity. Application command/query contracts, handlers, repositories, reviewer fields, audits, and notifications were not changed.

## Business workflow preservation

- Proposal queue discovery, contributor scoping, claim ownership, concurrency rules, revision, pass-to-board, and cancellation semantics are unchanged.
- Chapter decisions remain limited to the existing validation for `APPROVED`, `REVISION_REQUESTED`, and `CANCELLED`.
- `ChapterEditorialReview.ReviewerUserId`, review audit attribution, and notification attribution continue to receive the JWT-derived actor through the existing command/handler path.
- Multipart proposal and chapter markup construction is unchanged: field names, comments/decision values, `MarkupFile`, filenames, MIME type, stream handling, size behavior, and response parsing were preserved.
- Planned-date validation, `SCHEDULED`, `ON_HOLD`, release confirmation, `HIATUS` release blocking, publication notifications, and audit behavior remain in existing Application workflows.
- The current Editor annotation API is read-only. No create/update/resolve endpoint was added or redesigned.
- The current Editor series API is read-only. No HIATUS/resume or completion action was added; Mangaka-only completion boundaries therefore remain untouched.

## Transitional-header cleanup

Focused search over the five migrated Editor controllers and five migrated client implementations returns zero matches for:

- `X-Actor-User-Id`
- `ActorUserIdHeader`
- `TryResolveActorUserId`

Repository-wide remaining results are intentionally limited to:

1. Assistant/out of scope:
   - `AssistantTaskController.cs`
   - `AssistantCompletedWorkController.cs`
   - `AssistantTaskApiClient.cs`
2. Shared/deferred workspace entry:
   - `SeriesController.cs`
   - `SeriesApiClient.cs`
3. Stale comments/documentation:
   - `ChapterPageTaskDtos.cs`
   - `ChapterPageAnnotationDtos.cs`
   - the Assistant controller header comment

There are zero Batch A runtime usages, zero Batch B runtime usages, and zero Batch C Editor runtime usages.

## Architecture and impact

Existing architecture remains:

```text
Blazor Web
-> typed Editor API client
-> Editor API controller
-> IMediator.Send
-> existing Application handler
-> existing Infrastructure repository/storage
-> SQL Server
```

- Application impact: none.
- Domain impact: none.
- Infrastructure impact: none.
- EF mapping impact: none.
- Database/schema/migration/SQL/stored-procedure impact: none.
- Authentication generation, cookies, login/logout, Google authentication, and JWT configuration: unchanged.

## Static verification

- Interface and call-site searches: completed.
- Authorization attribute audit: completed; all migrated controllers are exact-role protected.
- Actor-continuity inspection: completed.
- Focused Editor transitional-header search: zero results.
- Repository-wide transitional-header inventory: categorized above.
- `git diff --check`: passed; Git emitted line-ending conversion warnings only.
- Scoped diff and working-tree status: inspected.

Build verification:  
**NOT RUN — user will perform the build.**

Automated tests:  
**NOT RUN — user will perform validation as applicable.**

Manual black-box functional testing:  
**NOT RUN — user will perform runtime functional testing.**

These omissions are intentional and are not verification failures.

## Deferred work and commit status

- Assistant JWT migration remains deferred.
- Shared `SeriesController.GetWorkspaceEntryAsync` and `SeriesApiClient.GetWorkspaceEntryAsync` remain deferred.
- No Admin/Auth cleanup or redesign was performed.
- Nothing was committed or pushed.
