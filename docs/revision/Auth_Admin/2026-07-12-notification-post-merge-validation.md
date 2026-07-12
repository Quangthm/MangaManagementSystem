# Notification Post-Merge Validation

## 1. Scope

This revision records the validation performed after PR #80 was merged into `main` and after the leader requested:

1. updating the local database constraint;
2. testing the current OTP cache behavior with two browsers;
3. synchronizing the latest `main`.

No additional application code was changed during this follow-up.

## 2. Main synchronization

The local `main` branch was synchronized with `origin/main` using fast-forward only.

Validated commit:

```text
4fb8361 - Merge pull request #80 from Quangthm/feature/notification-bell-distributed-otp-file-constants
```

The working tree was clean before synchronization.

## 3. Local database constraint update

The API local connection was confirmed as:

```text
Server: localhost
Database: MangaManagementDB
```

Before the update:

```text
constraint_name: ck_notification_type_code
supports_account_approved: NO
is_disabled: 0
is_not_trusted: 0
```

Existing notification records only used approved values, and the invalid-type validation query returned no rows.

Following the leader's direct instruction, only the local constraint below was updated:

```text
manga.Notification.ck_notification_type_code
```

No table data, stored procedure, migration, or unrelated schema object was modified.

After the update:

```text
supports_account_approved: YES
is_disabled: 0
is_not_trusted: 0
```

## 4. ACCOUNT_APPROVED end-to-end validation

Test account:

```text
TestNotifi6
```

Validation flow:

1. The account was listed in Admin User Approval.
2. Admin selected Approve.
3. The UI displayed `Approved TestNotifi6`.
4. The account was removed from the pending list.
5. The database returned:

```text
username: TestNotifi6
status_code: ACTIVE
notification_id: generated GUID
notification_type_code: ACCOUNT_APPROVED
title: Account approved
read_at_utc: NULL
```

6. The approved user successfully signed in.
7. The notification Bell displayed badge `1`.
8. The Bell displayed the unread `Account approved` notification.
9. The individual `Mark as read` action was visible.

Result:

```text
PASS - ACCOUNT_APPROVED works end-to-end from Admin approval to user notification Bell.
```

## 5. Two-browser OTP validation

Test clients:

```text
Browser A: Cốc Cốc
Browser B: Chrome with Swagger UI
```

Test account:

```text
username: BrowserOtpTest
email: tangochoangphan+browserotp@gmail.com
```

Validation flow:

1. Cốc Cốc submitted the registration information and requested an OTP.
2. The API stored the pending registration and OTP in the current server-side cache.
3. Chrome called:

```text
POST /api/registration/complete
```

4. Chrome supplied the same email and the OTP generated from the Cốc Cốc request.
5. The API returned HTTP 200.
6. The response returned:

```text
username: BrowserOtpTest
statusCode: PENDING_APPROVAL
```

7. The database confirmed:

```text
username: BrowserOtpTest
email: tangochoangphan+browserotp@gmail.com
status_code: PENDING_APPROVAL
```

Result:

```text
PASS - two different clients can use the same OTP when both requests reach the same API process.
```

## 6. Interpretation of the two-browser result

The two-browser test is a multi-client test, not a multi-instance test.

Current behavior:

```text
Cốc Cốc ─┐
         ├── one API process
Chrome ──┘        └── one AddDistributedMemoryCache instance
```

The cache is stored on the API server, not inside either browser. Therefore, both browsers can access the same OTP when they call the same API process.

This result does not prove that OTP data is shared between separate API processes.

With two API processes:

```text
API instance A -> memory A
API instance B -> memory B
```

An OTP created in instance A is not available in the memory of instance B when using `AddDistributedMemoryCache`.

A production multi-instance deployment must use a shared provider such as Redis or SQL distributed cache.

The current implementation must not be reported as a completed multi-instance distributed cache solution.

## 7. Build validation

Builds were run from the synchronized `main` branch.

API:

```text
Build succeeded
0 errors
26 warnings
```

Web:

```text
Build succeeded
0 errors
41 warnings
```

Final command result:

```text
PASS: main API and Web builds succeeded.
```

No code changes were required after the merge.

## 8. Final result

All three leader follow-up requests were completed:

| Leader request | Result |
|---|---|
| Update the local database constraint | PASS |
| Test the behavior with two browsers | PASS |
| Synchronize the latest main | PASS |

Additional end-to-end confirmation:

| Validation | Result |
|---|---|
| ACCOUNT_APPROVED database insert | PASS |
| Approved user notification Bell | PASS |
| Cross-browser OTP completion | PASS |
| API build | PASS |
| Web build | PASS |

## 9. Files and database objects changed

Repository:

```text
docs/revision/Auth_Admin/2026-07-12-notification-post-merge-validation.md
```

Local database:

```text
manga.Notification.ck_notification_type_code
```

No application source file was modified during this post-merge validation.
