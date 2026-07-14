# Chapter Submitted for Review Notification

## Date

2026-07-14

## Branch and baseline

- Branch: `feature/notification-remaining-flows`
- Baseline merge commit: `b28a296`
- Latest `origin/main` was merged into the feature branch.
- No direct changes were made on `main`.

## Leader request

Add notifications to additional workflow transitions, including when a Mangaka submits a chapter for editorial review.

## Trigger

A Mangaka submits a chapter whose status is:

- `DRAFT`, or
- `REVISION_REQUESTED`

The chapter transitions to:

- `UNDER_REVIEW`

## Recipients

The existing Chapter Review Queue is a global MVP queue and currently has no per-Tantou-Editor assignment relationship.

Therefore, one notification is created for each distinct user who has:

- exact role `Tantou Editor`;
- status `ACTIVE`.

## Notification

- Type: `SYSTEM_MESSAGE`
- Title: `Chapter Submitted for Review`
- Related entity type: `Chapter`
- Related entity ID: submitted `chapter_id`
- Bell navigation route: `/editor/chapters`

## Persistence

The following changes are persisted by one EF Core `SaveChangesAsync` call:

- Chapter status update;
- Audit event;
- Notifications.

No redundant explicit `BeginTransaction`, `Commit` or `Rollback` was added to this EF-only flow.

## Database impact

- No schema change.
- No migration.
- No new table.
- No stored procedure added or modified.
- Existing `manga.Notification` and `audit.AuditEvent` tables are reused.

## Validation status

## First runtime attempt

The first runtime submission attempt failed because the existing database CHECK constraint on `manga.Notification.notification_type_code` does not allow the new value `CHAPTER_SUBMITTED_FOR_REVIEW`.

The failed `SaveChangesAsync` persisted no partial data:

- Chapter remained `DRAFT`;
- notification count remained `0`;
- audit event count remained `0`.

No database constraint, schema, migration or stored procedure was changed.

The implementation was corrected to reuse the existing allowed notification type:

- `APPROVAL_REQUEST`

The Chapter notification remains unambiguous through:

- title `Chapter Submitted for Review`;
- related entity type `Chapter`;
- related entity ID equal to the submitted `chapter_id`.


## Second runtime attempt

The second runtime attempt failed because `APPROVAL_REQUEST` is also not included in the deployed database CHECK constraint `ck_notification_type_code`.

The second failed `SaveChangesAsync` persisted no partial data:

- Chapter remained `DRAFT`;
- notification count remained `0`;
- audit event count remained `0`.

The implementation was corrected without modifying the database and now reuses the existing allowed notification type:

- `SYSTEM_MESSAGE`

The Chapter review event remains identifiable through:

- title `Chapter Submitted for Review`;
- related entity type `Chapter`;
- related entity ID equal to the submitted `chapter_id`.

## Final runtime validation

### Mangaka UI

Test account:

- `TestMangaka1`

Test data:

- Series: `Notification Frequency Runtime Test`
- Chapter: `1`
- Chapter ID: `1f1783c9-e954-4d4e-c23f-08dee1b53155`

Before submission:

- Chapter status was `DRAFT`;
- `SUBMIT FOR REVIEW` was available.

After submission:

- success message `Chapter submitted for review.` was displayed;
- Chapter status changed to `UNDER_REVIEW`;
- the page displayed `Awaiting Tantou Editor review`;
- the submit button was no longer available.

Result: PASS.

### Database validation

The submission produced:

- Chapter status `UNDER_REVIEW`;
- exactly `5` notifications;
- exactly `5` distinct recipients;
- all recipients had exact role `Tantou Editor`;
- all recipients had user status `ACTIVE`;
- invalid recipient count `0`;
- exactly `1` audit event with action code `CHAPTER_SUBMITTED_FOR_EDITORIAL_REVIEW`.

Notification values:

- type: `SYSTEM_MESSAGE`;
- title: `Chapter Submitted for Review`;
- related entity type: `Chapter`;
- related entity ID: `1f1783c9-e954-4d4e-c23f-08dee1b53155`.

Result: PASS.

### Tantou Editor Bell

Test account:

- `TestEditor1`

Before clicking:

- Bell badge displayed `1`;
- dropdown displayed `Chapter Submitted for Review`;
- message identified Chapter `1` and series `Notification Frequency Runtime Test`.

After clicking:

- navigation opened `/editor/chapters`;
- Chapter Review Queue displayed Chapter `1` as `UNDER REVIEW`;
- the notification was marked as read;
- `read_at_utc` was populated;
- TestEditor1 unread count became `0`;
- the other four Tantou Editor notifications remained unread.

Result: PASS.

### Atomic persistence

Chapter status, Audit Event and all five Notifications were saved through one EF Core `SaveChangesAsync` call.

No explicit `BeginTransaction`, `Commit` or `Rollback` was added.

The two failed validation attempts persisted no partial data:

- Chapter remained `DRAFT`;
- notification count remained `0`;
- audit event count remained `0`.

### Database compatibility

The deployed `ck_notification_type_code` constraint does not permit new notification types without a database change.

To comply with the instruction not to modify the database, this flow reuses the allowed `SYSTEM_MESSAGE` type and distinguishes the event through title and related entity fields.

No database schema, migration, CHECK constraint or stored procedure was modified.

### Final build

- Full solution build: PASS
- Errors: `0`
- Warnings: `69` existing warnings after synchronizing the latest `main`