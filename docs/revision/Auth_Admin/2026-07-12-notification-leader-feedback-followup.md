# Notification Leader Feedback Follow-up

## 1. Scope

This follow-up addresses the notification feedback received after PR #80:

- notification retrieval must not be permanently limited to 20 records;
- notification bell placement must be clearer and more compact;
- unread items must expose an explicit Mark as read action;
- account approval notification must use ACCOUNT_APPROVED;
- Mark one as read and Mark all as read must be demonstrated separately;
- database persistence must be verified using read_at_utc;
- approval status update and notification creation must not succeed partially.

## 2. Notification paging

The notification list now supports skip/take paging through all layers:

- API accepts skip and take query parameters;
- MediatR query carries Skip and Take;
- query handler normalizes both values;
- repository executes Skip(skip).Take(take);
- Web API client sends skip and take;
- Notification Bell loads 20 items per page and exposes Load more when another page exists.

The value 20 is now a page size, not a permanent history limit.

Runtime validation:

- GET /api/notifications?skip=0&take=1 returned one notification;
- GET /api/notifications?skip=1&take=1 returned an empty array for an account with one notification;
- runtime paging result passed.

The current local dataset contains only one notification per user, so the Load more button cannot naturally appear without creating artificial data. No artificial notification records were inserted.

## 3. Notification Bell UI

The Bell was moved to the right side of the account card and reduced in size.

Unread notification items now display an explicit Mark as read button. Read items display the Read label.

The Mark all as read action remains available at panel level.

## 4. Mark one as read validation

Test account: Phanphonphon.

Passed:

- unread badge displayed 1;
- the notification panel displayed the explicit Mark as read action;
- clicking Mark as read removed the unread badge;
- the item displayed Read;
- refreshing the page preserved the Read state;
- database read_at_utc was updated to 2026-07-11 17:29:44.

## 5. Mark all as read validation

Test account: Testnotifi4.

Passed:

- unread badge displayed 1;
- Mark all as read was available;
- clicking Mark all as read removed the unread badge;
- the item displayed Read;
- refreshing the page preserved the Read state;
- database read_at_utc was updated to 2026-07-11 17:30:54.

Control account TestNotifi2 was not touched and its read_at_utc remained NULL. This confirms that read operations did not update another recipient's notification.

## 6. ACCOUNT_APPROVED type

Application code now creates account approval notifications using:

ACCOUNT_APPROVED

instead of:

SYSTEM_MESSAGE

The repository schema file already contains ACCOUNT_APPROVED.

The existing local database was inspected and its ck_notification_type_code constraint does not yet contain ACCOUNT_APPROVED. No ALTER TABLE, migration, stored procedure, or direct database correction was performed.

An official database constraint synchronization is still required before a new ACCOUNT_APPROVED notification can be created successfully in this local environment.

## 7. Atomic approval transaction

The first local ACCOUNT_APPROVED test exposed a partial-success risk:

- the user status was changed to ACTIVE;
- notification insertion then failed because the local constraint was outdated.

The approval flow was corrected so that:

- a transaction starts before the status stored procedure;
- the stored procedure command participates in the current EF Core transaction;
- notification creation occurs inside the same transaction;
- the transaction commits only after both operations succeed;
- any failure rolls back the status update and notification operation.

Rollback runtime validation used TestNotifi6:

- approval notification insertion failed because the local constraint rejected ACCOUNT_APPROVED;
- the user remained PENDING_APPROVAL after refresh;
- no notification record was inserted;
- partial success no longer occurred.

## 8. Database impact

No database schema, migration, stored procedure, constraint, or data correction was applied.

The only database changes during functional notification testing were normal application updates to read_at_utc produced by:

- Mark as read;
- Mark all as read.

## 9. Validation status

Passed:

- API build;
- Web build;
- Bell placement;
- explicit Mark as read action;
- Mark one as read;
- Mark all as read;
- state persistence after refresh;
- database read_at_utc verification;
- skip/take runtime paging;
- approval rollback when notification creation fails.

Environment dependency:

- the local database constraint must be synchronized officially before end-to-end creation of a new ACCOUNT_APPROVED notification can pass.
## 10. Deployment limitation of the current OTP cache provider

The application code now depends on IDistributedCache instead of directly depending on IMemoryCache for registration and profile-action OTP storage.

The current local provider is registered with AddDistributedMemoryCache.

This provider satisfies the IDistributedCache abstraction, but its data is still stored in the memory of the current application process. It does not share OTP records between separate application instances.

Therefore:

- the current configuration is suitable for local development and a single running instance;
- it must not be reported as a completed multi-instance distributed cache solution;
- if production deploys multiple API instances, a request that creates an OTP on instance A may later be verified by instance B, where that OTP does not exist;
- production must replace the provider with a shared provider such as Redis or SQL distributed cache;
- the OTP business flow, key prefixes, validation behavior, and five-minute expiration can remain unchanged because they already use the IDistributedCache abstraction.

No Redis connection, SQL cache table, infrastructure credential, or deployment configuration was added in this task because those changes require an approved production infrastructure decision.

This limitation must be reported to the leader rather than hidden or overstated.
