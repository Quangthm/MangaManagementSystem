# Session Note — Cancel Draft Workflow

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-17
- **Stored procedure used:** `manga.usp_Series_CancelDraft` (pre-existing)

---

## Task Summary

Implemented the Cancel Draft workflow for Mangaka series. A `PROPOSAL_DRAFT` series can now
be cancelled from the dashboard kebab menu. The workflow transitions the series to `CANCELLED`
via a soft status update — no physical delete is performed.

Uses the established MediatR/CQRS pattern (same as Submit Proposal and Edit Draft).

---

## Architecture Flow

```
MangakaDashboard.razor (Cancel Draft kebab → confirmation modal)
  → IMangakaSeriesApiClient.CancelDraftAsync(actorUserId, seriesId, reason?)
  → POST /api/mangaka/series/{seriesId}/draft-cancellations (JSON body)
  → MangakaSeriesController.CancelDraftAsync (thin controller)
  → IMediator.Send(CancelSeriesDraftCommand)
  → CancelSeriesDraftCommandHandler (validates inputs, calls repository)
  → ISeriesRepository.CancelSeriesDraftViaProcAsync
  → manga.usp_Series_CancelDraft (SQL Server)
```

Auth boundary: same transitional `X-Actor-User-Id` header pattern.

POST is used (not DELETE) because this is a business workflow state transition, not a
physical resource deletion.

---

## Stored Procedure Contract

`manga.usp_Series_CancelDraft` — pre-existing, no SQL changes made.

```sql
@actor_user_id  UNIQUEIDENTIFIER
@series_id      UNIQUEIDENTIFIER
@reason         NVARCHAR(500) = NULL
```

No OUTPUT parameters. SP behavior: acquires app lock per series, validates status =
PROPOSAL_DRAFT (error 57103), validates actor is active Mangaka contributor (error 57104),
sets status_code = CANCELLED, writes SERIES_DRAFT_CANCELLED audit event.

---

## Files Changed

| Layer | File | Change |
|---|---|---|
| Application | `DTOs/Manga/SeriesDraftCancelledDto.cs` | **New** — `{ SeriesId, StatusCode = "CANCELLED" }` |
| Application | `Features/Mangaka/Series/Commands/CancelSeriesDraft/CancelSeriesDraftCommand.cs` | **New** |
| Application | `Features/Mangaka/Series/Commands/CancelSeriesDraft/CancelSeriesDraftCommandHandler.cs` | **New** |
| Domain | `Interfaces/ISeriesRepository.cs` | Added `CancelSeriesDraftViaProcAsync(...)` |
| Infrastructure | `Repositories/SeriesRepository.cs` | Implemented `CancelSeriesDraftViaProcAsync` + `MapCancelSqlException` |
| API | `Contracts/CancelSeriesDraftRequest.cs` | **New** — `{ string? Reason }` |
| API | `Controllers/MangakaSeriesController.cs` | Added `POST {seriesId}/draft-cancellations` endpoint |
| Web | `Services/Api/IMangakaSeriesApiClient.cs` | Added `CancelDraftAsync(...)` |
| Web | `Services/Api/MangakaSeriesApiClient.cs` | Implemented `CancelDraftAsync` (JSON POST) |
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Enabled kebab item, added confirmation modal + state + methods |

---

## API Endpoint

```
POST /api/mangaka/series/{seriesId:guid}/draft-cancellations
Content-Type: application/json
Header: X-Actor-User-Id: <actorUserId>
Body: { "Reason": "optional string, max 500 chars" }

200 OK → SeriesDraftCancelledDto { SeriesId, StatusCode: "CANCELLED" }
400 BadRequest → ApiErrorResponse { message }
500 Problem → generic safe message
```

---

## SQL Error Mapping

| SQL Error | Friendly message |
|---|---|
| 57101 (lock) | "Could not process the cancellation right now. Please try again." |
| 57102 (not found) | "The selected series could not be found." |
| 57103 (not PROPOSAL_DRAFT) | "Only a series in draft status can be cancelled." |
| 57104 (not contributor) | "Only an active Mangaka contributor can cancel this draft." |
| default | "This draft could not be cancelled right now. Please try again." |

---

## UI Behavior

- **Cancel Draft kebab item** — enabled for `PROPOSAL_DRAFT` only (was disabled with tooltip).
  Clicking opens the confirmation modal.
- **Confirmation modal** — shows series title, irreversibility warning, optional reason textarea
  (max 500 chars), "Keep Draft" (cancel) and "Cancel Draft" (confirm, red) buttons.
- Buttons disabled while API call is in-flight (`_cancelDraftBusy`).
- Overlay click-to-close is blocked while busy.
- **On success:** snackbar "Draft cancelled.", card status updated to `CANCELLED` in-memory,
  modal closed. Edit Draft and Submit Proposal card actions disappear automatically because
  they are guarded by `series.StatusCode == "PROPOSAL_DRAFT"`.
- **On failure:** friendly snackbar only; modal stays open for retry or dismiss.
- **CANCELLED card click** → Review Status modal (existing routing in `OpenSeriesCard`).

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

All warnings are pre-existing.

---

## Manual Test Notes

**Not run by OpenCode.** Developer manual testing required.

Checklist:
```
1. Login as active Mangaka.
2. Navigate to /mangaka dashboard.
3. Open kebab menu on a PROPOSAL_DRAFT card.
4. Confirm "Cancel Draft" appears as an enabled red item.
5. Click Cancel Draft → confirmation modal opens with series title.
6. Verify irreversibility warning is visible.
7. Click "Keep Draft" → modal closes, card unchanged, no API call.
8. Click Cancel Draft again → enter an optional reason → click "Cancel Draft" button.
9. Confirm success snackbar: "Draft cancelled."
10. Confirm card status chip updates to Cancelled.
11. Confirm Edit Draft and Submit Proposal card actions are no longer visible.
12. Click the CANCELLED card → Review Status modal opens.
13. Open kebab on a CANCELLED card → Cancel Draft item is not shown.
14. DB: Series.status_code = CANCELLED.
15. DB: AuditEvent row with action_code = SERIES_DRAFT_CANCELLED and reason in detail_json.
16. Try cancelling an UNDER_EDITORIAL_REVIEW series via direct API POST → 400 friendly error.
17. Confirm no raw SQL/stack trace in UI or API responses.
```

---

## Remaining Tasks

- **Create Draft MediatR migration** — still uses transitional `ISeriesService` path.
- **`/series/{slug}` full page** — stub only; chapter list and workspace entry pending.
- **Slug preview in Edit Draft modal** — derived slug not shown to user before saving.
- **`PublicationFrequencyCode` on the `/series/{slug}` stub page** — not displayed currently.
