# Publication Frequency Change Request Notification

## Date

2026-07-14

## Branch and baseline

- Branch: `feature/notification-remaining-flows`
- Previous commit: `db04a18`
- Original main baseline: `4fb8361`
- No merge or direct change was made on `main`.

## Scope completed

Implemented the complete `PUBLICATION_FREQUENCY_REQUEST` flow for serialized series.

### Mangaka user interface

For a series with status `SERIALIZED`:

- The dashboard shows a `Request Frequency Change` action.
- The action is disabled when the series has no official publication frequency.
- The dialog displays the current official frequency.
- A request/reason is required.
- The form has loading, success, and error states.
- The UI does not change the official frequency.

### Typed Web API client

Added a typed client method that:

- Calls the publication frequency request endpoint.
- Sends the signed-in actor ID using the existing actor header pattern.
- Sends the required reason as JSON.
- Reads the typed result DTO.
- Reuses the existing API error extraction behavior.

### API and Application flow

Added:

- A POST endpoint under the Mangaka series controller.
- A MediatR command and command handler.
- A repository contract.
- Application-owned notification and audit rules.
- Shared constants for:
  - `PUBLICATION_FREQUENCY_REQUEST`
  - related entity type `Series`

The Application layer defines:

- Recipient role: `Editorial Board Chief`
- Recipient status: `ACTIVE`
- Actor role: `Mangaka`
- Notification type, title, and message format
- Related entity type
- Audit action and entity type

### Authorization and business validation

The backend verifies:

- The series exists.
- The series status is `SERIALIZED`.
- The series has an official publication frequency.
- The actor is an active Mangaka contributor of that exact series.
- At least one active Editorial Board Chief exists.
- A non-empty request/reason is supplied.

Client-side visibility is not treated as authorization. The backend performs the authoritative checks.

### Notification and audit persistence

The EF repository:

- Resolves all active Editorial Board Chief recipients.
- Removes duplicate recipient IDs.
- Creates one notification per recipient.
- Links each notification to:
  - related entity type `Series`
  - related entity ID equal to the requested series ID
- Creates an audit event containing the series, current frequency, reason, notification type, recipient role, and recipient count.
- Persists notifications and audit inside one EF transaction.
- Rolls back the complete operation if persistence fails.

## Important behavior

This request flow does not directly modify:

- `Series.PublicationFrequencyCode`
- the serialized series status
- any publication schedule

The request is an in-app communication to the Editorial Board Chief. Changing the official publication frequency remains a separate controlled workflow.

## Database impact

- No schema change.
- No migration.
- No new table.
- No stored procedure added or modified.
- Existing `Notification`, `AuditEvent`, `Series`, `User`, `Role`, and active contributor EF mappings were reused.

## Validation performed

- Exact changed-file scope verified.
- `git diff --check` passed.
- Full solution build succeeded.
- Domain build succeeded.
- Application build succeeded.
- Infrastructure build succeeded.
- API build succeeded.
- Web build succeeded.
- Typed API client markers verified.
- Serialized-series action and request dialog markers verified.
- Notification and audit transaction markers verified.
- No direct publication frequency update was found.

## Manual runtime tests still required

1. Sign in as an active Mangaka contributor.
2. Open the dashboard and select a `SERIALIZED` series.
3. Confirm the current official frequency is displayed.
4. Submit a request with a reason.
5. Confirm the success message reports the Chief recipient count.
6. Sign in as an active Editorial Board Chief.
7. Confirm the Bell shows a `PUBLICATION_FREQUENCY_REQUEST`.
8. Confirm the notification references the correct series and includes the reason.
9. Confirm the notification can be marked as read.
10. Confirm an audit event was inserted.
11. Confirm `Series.PublicationFrequencyCode` remains unchanged.
12. Confirm a non-contributor cannot submit the request.

## Runtime validation update - Publication Frequency Request

The complete successful runtime flow was executed.

### Workflow result

- Runtime series: Notification Frequency Runtime Test.
- Series status before request: SERIALIZED.
- Current official frequency shown by the UI: WEEKLY.
- TestMangaka1 submitted a non-empty request reason.
- The UI reported delivery to 5 Editorial Board Chief accounts.
- The official frequency remained WEEKLY after submission.

### Notification database evidence

- Active Editorial Board Chief count: 5.
- PUBLICATION_FREQUENCY_REQUEST notifications created: 5.
- Distinct recipients: 5.
- Invalid recipient count: 0.
- Duplicate-recipient query returned no rows.
- Every recipient had role Editorial Board Chief and status ACTIVE.
- Related entity type was Series.
- Related entity ID matched the runtime test series.

### Audit evidence

- Audit object: [audit].[AuditEvent].
- Action code: PUBLICATION_FREQUENCY_CHANGE_REQUESTED.
- Actor: TestMangaka1.
- Actor role: Mangaka.
- Recipient role: Editorial Board Chief.
- Recipient count: 5.
- Audit detail retained the current frequency WEEKLY and submitted reason.

### Series state evidence

- Series status: SERIALIZED.
- Official publication frequency: WEEKLY.
- Final state check: PASS.
- The request did not directly modify the official publication frequency.

### Notification Bell evidence

- TestBoardChief1 saw one unread Publication Frequency Change Request.
- The notification showed the correct actor, series, frequency, and reason.
- Mark as read removed the unread badge.
- The notification remained visible with status Read.
- Database read_at_utc was populated.
- Final read-status verification returned PASS.

### Negative paths not forced

The following defensive paths were not intentionally executed against the shared runtime database:

1. A non-contributor submitting a request.
2. A serialized series without an official frequency.
3. A request when no active Editorial Board Chief exists.
4. A forced persistence failure to verify transaction rollback.
