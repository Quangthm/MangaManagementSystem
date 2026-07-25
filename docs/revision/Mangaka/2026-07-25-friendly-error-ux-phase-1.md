# Friendly Error UX Phase 1

## Scoped problem

Mangaka contributor management and task creation could expose HTTP status
prefixes, serialized API error bodies, internal error codes, and workflow enum
names. This batch introduces safe shared parsing and targeted UX improvements
for Manage Contributors and Quick Select.

## Shared parser behavior

- `ApiResponseReader` remains the canonical Web API error reader.
- It parses `ApiErrorResponse`, ProblemDetails, ValidationProblemDetails, and
  safe plain-string business responses.
- HTTP 400/409 responses preserve a parsed business message without exposing
  JSON, status prefixes, error codes, or request URIs.
- HTTP 401 and 403 use safe authentication/permission fallbacks.
- HTTP 404 preserves status metadata through `ApiClientException`.
- HTTP 500+ response bodies are never exposed through `Exception.Message`.

## Clients migrated

- `MangakaSeriesContributorApiClient`
- `MangakaTaskApiClient`

Public signatures, routes, request/success behavior, cancellation, and JWT
handling were preserved.

## Contributor API semantic correction

The contributor command handlers now pre-check the current series lifecycle.
The repository still retains the stored procedure as the final concurrency
guard. Proven SQL error `57203` from `manga.usp_SeriesContributor_Add` is
translated to a safe business exception; other SQL failures remain unexpected
server failures.

The existing contributor controller convention remains HTTP 400 for known
business `InvalidOperationException` failures.

## Completed-series behavior

Adding or ending an Assistant is rejected with:

> This series is completed, so its contributor list can no longer be changed.

Manage Contributors disables both actions when the already-loaded series
status is completed.

## Cancelled-series rule finding

The contributor rules do not independently define cancelled-series mutation,
but the existing `usp_SeriesContributor_Add` procedure explicitly rejects
adding contributors to a cancelled series with error `57203`. That restriction
was preserved and now returns:

> This series has been cancelled, so new contributors can no longer be added.

Ending an Assistant on a cancelled series remains unchanged; no new
restriction was introduced.

## Active-task removal wording

Removing an Assistant with active tasks now returns:

> This assistant still has active tasks. Reassign or cancel those tasks before
> removing them from the series.

## Task eligibility wording

Quick Select and normal shared page-task creation now use:

> Tasks can only be created for chapters in draft or needing revision, while
> the series is serialized or on hiatus.

The eligibility predicates were not changed.

## UI prevention and fallbacks

- Manage Contributors uses loaded series status to disable completed-series
  Add/End and cancelled-series Add actions.
- Contributor loading, assistant searching, and unexpected mutations use safe
  operation-specific fallbacks.
- Repeated autocomplete failures are suppressed until a successful search or
  new dialog session.
- Quick Select uses loaded series and chapter status to prevent obviously
  ineligible progression/submission and presents the friendly shared rule.
- Expected 400/409 failures retain parsed business messages; unexpected Quick
  Select creation failures use a safe fallback.

## Workspace ownership boundary

- Direct Creator Workspace UI was intentionally not modified.
- Workspace-specific UX findings are deferred to the teammate responsible for
  Workspace.
- Shared backend task validation wording was improved where required by direct
  Mangaka flows.
- Such shared backend changes may also benefit Workspace callers incidentally.
- No Workspace-specific branching, presentation, navigation, authorization, or
  control behavior was introduced.

## Intentionally deferred

- Editor clients and pages
- Remaining Mangaka clients and pages
- Creator Workspace presentation and controls
- Annotation, publication, and broader lifecycle error UX
- Platform-wide controller/error-contract normalization

## Files changed

- Shared Web error reader and two scoped typed clients
- Manage Contributors and Quick Select Razor pages
- Contributor repository abstraction, handlers, and repository
- Quick Select and page-task repositories
- This handoff

No API controller change was required.

## Static verification

- `API returned` raw construction: removed from both migrated clients.
- Raw contributor search/load/mutation display: removed; remaining
  `Snackbar.Add(ex.Message, ...)` calls are filtered expected 400/409
  `ApiClientException` business messages.
- Uppercase task eligibility sentence: removed from both scoped repositories.
- Quick Select audit-sensitive Application/Infrastructure behavior was not
  edited.
- Creator Workspace files were not edited.
- Build: not run by request.
- Automated tests: not run by request.
- Manual functional testing: deferred to the user.

