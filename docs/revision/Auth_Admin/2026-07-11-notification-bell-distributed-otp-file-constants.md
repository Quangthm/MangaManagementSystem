# Notification Bell, Distributed OTP Cache Abstraction, and File Constants



- Date: 2026-07-11

- Branch: `feature/notification-bell-distributed-otp-file-constants`

- Base: latest `origin/main` at the start of the task

- Status: implementation and runtime validation completed

- Commit/push status: not committed and not pushed



## 1. Scope



This revision implements the following leader follow-up requirements:



1\. Complete the notification bell flow.

2\. Add current-user notification APIs.

3\. Replace direct OTP `IMemoryCache` usage with `IDistributedCache`.

4\. Standardize Admin File purpose/status codes and labels through constants.

5\. Preserve existing database values and business behavior.

6\. Do not change schema, migrations, or stored procedures.



## 2. Notification Bell



Implemented:



- current-user recent notification list;

- unread count;

- mark one notification as read;

- mark all notifications as read;

- unread badge;

- notification dropdown;

- empty, loading, and retry states;

- persistent read state after page refresh.



API routes:



- `GET /api/notifications`

- `GET /api/notifications/unread-count`

- `POST /api/notifications/{notificationId}/read`

- `POST /api/notifications/read-all`



The recipient user ID is resolved from authenticated claims. The browser does not provide an arbitrary recipient ID.



## 3. Notification Architecture



Added:



- `INotificationRepository`

- `NotificationRepository`

- notification MediatR queries, commands, and handlers

- `NotificationsController`

- `INotificationApiClient`

- `NotificationApiClient`

- `NotificationBell.razor`



Repository updates are filtered by `recipient\_user\_id`.



## 4. OTP Cache



`OtpCacheService` now uses `IDistributedCache` instead of directly using `IMemoryCache`.



Preserved behavior:



- registration OTP keys;

- profile-action OTP keys;

- email normalization;

- action normalization;

- five-minute expiration;

- wrong OTP does not remove the cached entry;

- correct OTP removes the cached entry;

- registration payload serialization.



`AddMemoryCache()` remains because other existing flows still use it.



`AddDistributedMemoryCache()` is registered for local development.



### Deployment limitation



`AddDistributedMemoryCache()` is still process-local.



The code now uses the distributed-cache abstraction, but true shared OTP storage across multiple application instances still requires a shared provider such as Redis or SQL distributed cache.



This implementation must not be reported as fully multi-instance until a shared provider is configured.



## 5. File Constants



Added shared constants and labels for:



### File purpose codes



- `SERIES\_PROPOSAL`

- `SERIES\_COVER`

- `CHAPTER\_PAGE\_VERSION`

- `EDITORIAL\_ATTACHMENT`

- `REGISTRATION\_PORTFOLIO`

- `USER\_AVATAR`



### File purpose labels



- Series Proposal

- Series Cover

- Chapter Page Version

- Editorial Attachment

- Registration Portfolio

- User Avatar



### Admin File deleted-state codes



- `ACTIVE`

- `DELETED`

- `ALL`



### Admin File deleted-state labels



- Active

- Deleted

- All



The constants are now used in:



- Cloudinary purpose validation;

- Cloudinary folder mapping;

- Application state validation;

- EF repository filtering;

- API default parameters;

- Web API client default parameters;

- Admin File dropdown options;

- table labels;

- detail dialog labels.



No stored database value was changed.



## 6. Admin File Applied-State Fix



Separated the selected dropdown state from the state actually applied to the table.



Added `\_appliedDeletedState`.



Correct behavior:



- selecting Deleted without Apply does not immediately show Cleanup All;

- selecting Active or All from an applied Deleted view does not immediately hide Cleanup All;

- the button changes only after Apply;

- Clear resets selected and applied state to Active.



## 7. Changed Files



### API



- `src/MangaManagementSystem.API/Controllers/NotificationsController.cs`

- `src/MangaManagementSystem.API/Controllers/Admin/AdminFilesController.cs`



### Application



- `src/MangaManagementSystem.Application/Common/FileResourceConstants.cs`

- `src/MangaManagementSystem.Application/DTOs/Manga/NotificationDtos.cs`

- `src/MangaManagementSystem.Application/Features/Notifications/NotificationQueries.cs`

- `src/MangaManagementSystem.Application/Features/Notifications/NotificationQueryHandlers.cs`

- `src/MangaManagementSystem.Application/Features/Notifications/NotificationCommands.cs`

- `src/MangaManagementSystem.Application/Features/Notifications/NotificationCommandHandlers.cs`

- `src/MangaManagementSystem.Application/Features/Admin/Files/Queries/AdminFileQueryHandlers.cs`



### Domain



- `src/MangaManagementSystem.Domain/Interfaces/INotificationRepository.cs`



### Infrastructure



- `src/MangaManagementSystem.Infrastructure/DependencyInjection.cs`

- `src/MangaManagementSystem.Infrastructure/Repositories/NotificationRepository.cs`

- `src/MangaManagementSystem.Infrastructure/Repositories/FileResourceRepository.cs`

- `src/MangaManagementSystem.Infrastructure/Services/OtpCacheService.cs`

- `src/MangaManagementSystem.Infrastructure/Services/CloudinaryFileStorageService.cs`



### Web



- `src/MangaManagementSystem.Web/Components/Shared/NotificationBell.razor`

- `src/MangaManagementSystem.Web/Components/Shared/UserAvatarMenu.razor`

- `src/MangaManagementSystem.Web/Components/Pages/Admin/AdminFiles.razor`

- `src/MangaManagementSystem.Web/Services/Api/INotificationApiClient.cs`

- `src/MangaManagementSystem.Web/Services/Api/NotificationApiClient.cs`

- `src/MangaManagementSystem.Web/Services/Api/IAdminFileApiClient.cs`

- `src/MangaManagementSystem.Web/Services/Api/AdminFileApiClient.cs`

- `src/MangaManagementSystem.Web/Program.cs`



## 8. Runtime Validation



### Registration OTP



Passed:



- OTP email was sent;

- correct OTP completed registration;

- pending account was created;

- admin could approve the account;

- registration remained functional after moving to `IDistributedCache`.



### Profile-action password OTP

Passed:

- valid new password and matching confirmation allowed OTP request;
- OTP was sent to the registered email;
- OTP Code and Confirm Reset controls appeared;
- valid OTP completed the password reset;
- the UI displayed Password reset successfully;
- login with the previous password failed;
- login with the new password succeeded;
- the user could access the Mangaka dashboard after login.



### Notification approval



Passed:



- admin approval created a notification;

- approved user could log in;

- bell displayed unread badge `1`;

- dropdown displayed `Account approved`.



### Mark one as read



Passed:



- unread badge changed from `1` to `0`;

- notification changed to read state;

- refresh preserved the result;

- `read\_at\_utc` was updated in `manga.Notification`.



### Mark all as read



Passed:



- unread count changed to zero;

- badge disappeared;

- button became disabled;

- refresh preserved the result;

- `read\_at\_utc` was updated.



### Admin File



Passed:



- purpose dropdown displayed readable labels;

- underscore codes were not shown;

- detail dialog displayed readable labels;

- Active, Deleted, and All filters worked;

- Cleanup All reflected the applied state only;

- Clear reset to All purposes, Active, and page size 20.



No Delete, Cleanup, or Cleanup All operation was executed during this validation.



## 9. Build Validation



Executed:



```powershell

dotnet build .\MangaManagementSystem\src\MangaManagementSystem.API\MangaManagementSystem.API.csproj --no-restore

dotnet build .\MangaManagementSystem\src\MangaManagementSystem.Web\MangaManagementSystem.Web.csproj --no-restore

git diff --check
```

Results:

- API build succeeded.
- Web build succeeded.
- Web produced 41 existing warnings from unrelated files.
- No new error was reported from notification, OTP cache, constants, or Admin File changes.
- `git diff --check` passed.
- No unexpected untracked file exists outside repository scope.

### File availability preview

Passed:

- selected the existing Deleted file view in Admin File Management;
- opened preview directly inside the current page without opening another browser tab;
- the API returned the safe placeholder for a deleted file;
- the preview displayed File unavailable (Deleted);
- the snackbar displayed A safe placeholder was shown because the file is deleted;
- no Delete, Cleanup, or Cleanup All operation was performed during this test.

## 10. Database Impact

No changes were made to:

- schema;
- migrations;
- stored procedures;
- table definitions;
- notification table structure;
- file-purpose stored values;
- deleted-state stored values.

Runtime tests only inserted normal application notification data and updated `read_at_utc`.

## 11. Current Progress

Completed:

- notification bell end-to-end;
- notification current-user APIs;
- mark one and mark all read;
- registration OTP through `IDistributedCache`;
- shared file constants and labels;
- Admin File applied-state synchronization;
- API and Web builds;
- notification and Admin File runtime smoke tests.

Pending before final commit:

- final file review;
- commit only after explicit approval;
- push only after explicit approval.
