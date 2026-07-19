# Chapter Submitted for Review Notification - Leader Correction

## Date

2026-07-15

## Branch and baseline

- Branch: `feature/notification-remaining-flows`
- PR: `#82`
- Baseline synchronization commit: `b28a296`
- No direct changes were made on `main`.

## Leader correction

The previous implementation used an incorrect participant rule and notification type.

The corrected requirements are:

1. Use database-supported notification type `CHAPTER_REVIEW`.
2. Notify only active Tantou Editor contributors of the submitted chapter's exact series.
3. Do not notify active Tantou Editors who are not contributors of that series.
4. Reuse the existing active-series-contributor check/view.
5. Do not introduce unnecessary explicit transactions for a single EF Core `SaveChangesAsync`.

## Trigger

A Mangaka submits a chapter whose current status is:

- `DRAFT`; or
- `REVISION_REQUESTED`.

The chapter transitions to:

- `UNDER_REVIEW`.

## Correct recipient rule

Recipients are selected from the existing:

- `manga.vw_ActiveSeriesContributor`;
- EF Core DbSet: `ApplicationDbContext.ActiveSeriesContributors`.

A recipient must satisfy all of the following conditions:

- `SeriesId` equals the submitted chapter's `SeriesId`;
- `RoleName` equals `Tantou Editor`;
- `UserStatusCode` equals `ACTIVE`;
- `EndDate` is `NULL`.

The resulting user IDs are selected with `Distinct()` to prevent duplicate notifications.

This means:

- an active Tantou Editor contributor of the exact series receives the notification;
- an active Tantou Editor who belongs to another series does not receive the notification;
- an ended contributor does not receive the notification;
- an inactive user does not receive the notification.

## Notification values

- Notification type: `CHAPTER_REVIEW`
- Title: `Chapter Submitted for Review`
- Related entity type: `Chapter`
- Related entity ID: submitted `chapter_id`
- Initial read state: unread
- Bell navigation route: `/editor/chapters`

The message identifies:

- the chapter number;
- the optional chapter title;
- the series title.

## Audit Event

The submission creates one Audit Event with:

- action code: `CHAPTER_SUBMITTED_FOR_EDITORIAL_REVIEW`;
- entity type: `Chapter`;
- entity ID: submitted `chapter_id`.

The audit detail includes:

- `chapter_id`;
- `series_id`;
- `series_title`;
- old and new status codes;
- submitting user ID;
- submission timestamp;
- notification type `CHAPTER_REVIEW`;
- recipient role `Tantou Editor`;
- recipient scope `ACTIVE_SERIES_CONTRIBUTOR`;
- recipient count.

## Persistence and transaction boundary

The Chapter status update, Notifications and Audit Event are tracked by one
`ApplicationDbContext` and persisted by one
`ApplicationDbContext.SaveChangesAsync` call.

EF Core provides the atomic persistence boundary for that single save. No
explicit `BeginTransaction`, `Commit` or `Rollback` is added to this EF-only
operation.

## Unit of Work clarification

`IUnitOfWork` is not independent from `ApplicationDbContext`.

The concrete `UnitOfWork` wraps the shared scoped `ApplicationDbContext`.
Its `SaveChangesAsync` method calls `_context.SaveChangesAsync`, and its
transaction methods operate through `_context.Database`.

The Chapter Submit flow is implemented inside
`MangakaChapterRepository` as an EF-only repository operation, so it uses the
repository's injected `ApplicationDbContext` and one save.

Single Task Assignment and Task Reassignment combine stored-procedure or SQL
changes with EF Notification writes. Their Application service therefore uses
the Unit of Work transaction so the task changes, Audit Events and
Notifications share one transaction boundary.

Quick Select and Board Poll currently own explicit transactions directly
inside their Infrastructure repositories through the injected
`ApplicationDbContext`. They use the same EF transaction principle, although
they do not currently use the `IUnitOfWork` abstraction.

The earlier wording that treated DbContext and Unit of Work as separate
persistence mechanisms was incorrect and has been removed.

## Database compatibility

The project database schema already includes:

- `CHAPTER_REVIEW`

in the `ck_notification_type_code` CHECK constraint.

Therefore, this correction does not require:

- an ALTER TABLE statement;
- a migration;
- a new table;
- a stored procedure change;
- a CHECK constraint change.

## Code validation

Confirmed implementation changes:

- added `NotificationTypeCodes.ChapterReview`;
- removed the Chapter Review use of `NotificationTypeCodes.SystemMessage`;
- changed recipient lookup from global active users to `ActiveSeriesContributors`;
- added exact series scoping;
- added active-user, Tantou-role and active-contributor filtering;
- updated Notification Bell routing to recognize `CHAPTER_REVIEW`.

## Build validation

Full solution build result:

- Build: PASS
- Errors: `0`
- Warnings: `69`

The warnings are existing nullable, generated Razor and MudBlazor analyzer warnings and do not block the build.

## Runtime validation status

Runtime validation was repeated after correcting both:

- notification type;
- participant scoping.

The previous `SYSTEM_MESSAGE` and global-editor evidence remains invalid and is not used as final proof.

## Corrected runtime test context

### Series

- Title: `Notification Frequency Runtime Test`
- Series ID: `5da0dfb7-dcbe-4466-a662-323fdde5e286`

### Chapter

- Number: `2`
- Title: `Chapter Review Runtime Test`
- Chapter ID: `624553bb-d271-47f4-4fdf-08dee2640de6`

### Actor

- Username: `TestMangaka1`
- Role: `Mangaka`

### Expected recipient

- Username: `TestEditor1`
- User ID: `0a0b9066-b19e-4008-9f27-a812f3484f49`
- Role: `Tantou Editor`
- User status: `ACTIVE`
- Active contributor of the exact series: `YES`

### Explicitly excluded active Tantou Editors

The following active Tantou Editors were not contributors of the test series and therefore had to receive no notification:

- `TestEditor2`
- `TestEditor3`
- `TestEditor4`
- `TestEditor5`

## Before-submit evidence

Before submission:

- Chapter status: `DRAFT`
- Notification count for Chapter 2: `0`
- Matching Audit Event count: `0`
- `TestEditor1`: `EXPECTED_RECIPIENT`
- `TestEditor2–5`: `MUST_NOT_RECEIVE`

The Mangaka UI displayed:

- Chapter `2 — Chapter Review Runtime Test`;
- status `DRAFT`;
- action `SUBMIT FOR REVIEW`.

## Submission result

After `TestMangaka1` submitted Chapter 2:

- success message displayed: `Chapter submitted for review.`;
- Chapter status changed to `UNDER_REVIEW`;
- UI displayed `Awaiting Tantou Editor review`;
- the submit action was no longer displayed.

Result: PASS.

## Database notification evidence

Exactly one Notification was created:

- Notification ID: `f021216c-e89c-45cd-3d8f-08dee264ae82`
- Recipient: `TestEditor1`
- Recipient role: `Tantou Editor`
- Recipient user status: `ACTIVE`
- Notification type: `CHAPTER_REVIEW`
- Title: `Chapter Submitted for Review`
- Related entity type: `Chapter`
- Related entity ID: `624553bb-d271-47f4-4fdf-08dee2640de6`
- Initial read state: unread

Recipient validation:

- `TestEditor1`: notification count `1`, type `CHAPTER_REVIEW`
- `TestEditor2`: notification count `0`
- `TestEditor3`: notification count `0`
- `TestEditor4`: notification count `0`
- `TestEditor5`: notification count `0`

Duplicate validation:

- `TestEditor1` notification count for Chapter 2: `1`
- Duplicate notification count: `0`

Result: PASS.

## Audit Event evidence

Exactly one Audit Event was created:

- Audit Event ID: `45`
- Actor role: `Mangaka`
- Action code: `CHAPTER_SUBMITTED_FOR_EDITORIAL_REVIEW`
- Entity type: `Chapter`
- Entity ID: `624553bb-d271-47f4-4fdf-08dee2640de6`
- Notification type: `CHAPTER_REVIEW`
- Recipient role: `Tantou Editor`
- Recipient scope: `ACTIVE_SERIES_CONTRIBUTOR`
- Recipient count: `1`
- Old status: `DRAFT`
- New status: `UNDER_REVIEW`

Result: PASS.

## Tantou Editor Bell evidence

After logging in as `TestEditor1`:

- Bell displayed unread badge `1`;
- dropdown displayed `Chapter Submitted for Review`;
- message identified Chapter 2 and the correct series;
- the new notification was displayed as unread;
- the previous Chapter 1 notification remained read.

After clicking the Chapter 2 notification:

- browser navigated to `/editor/chapters`;
- Chapter Review Queue displayed Chapter 2;
- Chapter 2 status was `UNDER REVIEW`;
- Bell unread badge was cleared;
- database `read_at_utc` was populated;
- unread `CHAPTER_REVIEW` count for `TestEditor1` became `0`.

Read timestamp:

- `2026-07-15 11:40:03`

Result: PASS.

## Atomic persistence result

Before submission:

- Chapter status: `DRAFT`
- Notification count: `0`
- Audit Event count: `0`

After successful submission:

- Chapter status: `UNDER_REVIEW`
- Notification count: `1`
- Audit Event count: `1`

The Chapter update, Notification and Audit Event were persisted through one EF Core `SaveChangesAsync`.

No redundant explicit transaction was added.

Result: PASS.

## Acceptance-check results

1. Chapter started as `DRAFT`: PASS
2. Active Tantou Editor contributor existed for exact series: PASS
3. Active Tantou Editors outside the series existed: PASS
4. Chapter transitioned to `UNDER_REVIEW`: PASS
5. Only exact-series active Tantou Editor contributor was notified: PASS
6. Tantou Editors outside the series received no notification: PASS
7. Notification type was `CHAPTER_REVIEW`: PASS
8. Related entity type was `Chapter`: PASS
9. Related entity ID matched Chapter 2: PASS
10. Exactly one Audit Event was created: PASS
11. Bell displayed the notification: PASS
12. Bell navigation opened `/editor/chapters`: PASS
13. `read_at_utc` was populated: PASS
14. Unread count became `0`: PASS
15. No duplicate notification was created: PASS

## Final validation result

- Code correction: COMPLETE
- Publication Frequency flow removal: COMPLETE
- Participant correction: PASS
- Notification type correction: PASS
- Static validation: PASS
- Full solution build: PASS
- Build errors: `0`
- Build warnings: `69`
- Corrected Chapter Review runtime validation: PASS
- Bell UI validation: PASS
- Database recipient validation: PASS
- Read-state validation: PASS