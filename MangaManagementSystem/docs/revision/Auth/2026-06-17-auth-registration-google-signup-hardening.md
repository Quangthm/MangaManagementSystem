# Auth Registration and Google Signup Hardening



**Date:** 2026-06-17

**Branch:** `feature/auth-admin-hardening`

**Status:** Completed and locally verified



## Scope



This session implemented the confirmed registration and Google Signup requirements:



* Public users may register for all operational roles except `Admin`.

* Google Signup uses the role selected on the registration UI.

* Google Signup bypasses email OTP.

* New Google Signup accounts remain `PENDING\_APPROVAL`.

* Registration write workflows begin following the CQRS command pattern.

* Direct requests cannot use Google Signup to create arbitrary accounts.



## Implemented Changes



### 1. Public registration role whitelist



Added a server-side public registration whitelist containing:



* Mangaka

* Assistant

* Tantou Editor

* Editorial Board Member

* Editorial Board Chief



`Admin` is intentionally excluded.



Role values are normalized using canonical role names before registration processing.



### 2. Registration UI roles



Updated the registration role dropdown to include:



* Mangaka

* Assistant

* Tantou Editor

* Editorial Board Member

* Editorial Board Chief



The selected role is submitted for both standard registration and Google Signup.



### 3. CQRS and MediatR setup



Added MediatR `12.4.1` to the Application project.



Registered handlers from the Application assembly.



Added registration commands and handlers:



* `SendRegistrationOtpCommand`

* `SendRegistrationOtpCommandHandler`

* `ProcessGoogleSignupCommand`

* `ProcessGoogleSignupCommandHandler`



The handlers validate the public role whitelist before calling the application service.



### 4. Standard registration API boundary



Updated `RegistrationController` so the OTP registration workflow dispatches a MediatR command instead of constructing and calling the application workflow directly.



### 5. Google Signup role synchronization



The role selected in the registration UI is stored in Google OAuth `AuthenticationProperties`.



The Google callback retrieves the stored role and validates it again against the public role whitelist.



The previous hard-coded default Google registration role (`Mangaka`) was removed.



### 6. Google Signup without OTP



Google Signup no longer redirects new users to the email OTP verification page.



Google-authenticated users are processed as follows:



* New user: create account and redirect to Pending Approval.

* Existing `PENDING\_APPROVAL` user: redirect to Pending Approval.

* Existing `ACTIVE` user: sign in and redirect to the appropriate dashboard.

* Existing `REJECTED` user: redirect with rejected-account status.

* Existing `DISABLED` user: redirect with disabled-account status.



The existing `auth.usp\_User\_Create` stored procedure creates new accounts with status `PENDING\_APPROVAL`.



### 7. Web-to-API Google Signup boundary



Added:



* `GoogleSignupRequest`

* `IAuthApiClient.ProcessGoogleSignupAsync`

* `AuthApiClient.ProcessGoogleSignupAsync`

* `POST /api/auth/google-signup`



The Web project now calls the API client instead of calling `IAuthService` directly for Google Signup.



### 8. Internal endpoint protection



The Google Signup API endpoint is an internal Web-to-API endpoint.



Protection added:



* Internal API key stored separately in API and Web User Secrets.

* Web sends the key through the `X-Internal-Api-Key` header.

* API compares the key using fixed-time comparison.

* Missing or incorrect key returns `401 Unauthorized`.

* The internal Google Signup endpoint is hidden from Swagger.

* API and Web fail startup when `InternalApi:Key` is missing.



No secret values were added to source control.



## Verification Performed



### Build



Executed:



```powershell

dotnet build

```



Result:



```text

Build succeeded with 33 warning(s)

```



There were no compilation errors. Remaining warnings existed in other project areas and were not introduced by this workflow.



### Public Admin role rejection



Sent a registration request using:



```json

{

&#x20; "roleName": "Admin"

}

```



Result:



```text

400 Bad Request

The selected role is not available for public registration.

```



This confirmed that directly modifying the request cannot create an Admin account.



### Google Signup role test



Selected:



```text

Editorial Board Member

```



Then completed Google Signup with a test Google account.



Verified:



* No email OTP page was shown.

* No registration OTP was required.

* User was redirected to `/pending-approval`.

* Admin User Approval displayed the correct Google email.

* Admin User Approval displayed role `Editorial Board Member`.

* The account was created as a pending approval account.



### Direct internal endpoint test



Called the Google Signup API endpoint directly without the internal API key.



Result:



```text

HTTP/1.1 401 Unauthorized

{"message":"Unauthorized internal request."}

```



This confirmed that a direct external request cannot create a Google Signup account.



### Valid Web-to-API test



Repeated Google Signup through the Web application.



Verified:



* Web sent the internal API key successfully.

* API accepted the internal request.

* Existing pending account redirected to `/pending-approval`.

* No `GoogleSignupFailed` error occurred.



## Files Added



* `src/MangaManagementSystem.API/Contracts/GoogleSignupRequest.cs`

* `src/MangaManagementSystem.API/Options/InternalApiOptions.cs`

* `src/MangaManagementSystem.Application/Features/Auth/Registration/PublicRegistrationRoles.cs`

* `src/MangaManagementSystem.Application/Features/Auth/Registration/SendRegistrationOtpCommand.cs`

* `src/MangaManagementSystem.Application/Features/Auth/Registration/SendRegistrationOtpCommandHandler.cs`

* `src/MangaManagementSystem.Application/Features/Auth/Registration/ProcessGoogleSignupCommand.cs`

* `src/MangaManagementSystem.Application/Features/Auth/Registration/ProcessGoogleSignupCommandHandler.cs`

* `src/MangaManagementSystem.Web/Options/InternalApiOptions.cs`



## Files Updated



* `src/MangaManagementSystem.API/Controllers/AuthController.cs`

* `src/MangaManagementSystem.API/Controllers/RegistrationController.cs`

* `src/MangaManagementSystem.API/Program.cs`

* `src/MangaManagementSystem.Application/DTOs/Auth/GoogleSignupDtos.cs`

* `src/MangaManagementSystem.Application/DependencyInjection.cs`

* `src/MangaManagementSystem.Application/Interfaces/IAuthService.cs`

* `src/MangaManagementSystem.Application/MangaManagementSystem.Application.csproj`

* `src/MangaManagementSystem.Application/Services/AuthService.cs`

* `src/MangaManagementSystem.Web/Components/Pages/RegisterPage.razor`

* `src/MangaManagementSystem.Web/Helpers/GoogleAuthHelper.cs`

* `src/MangaManagementSystem.Web/Program.cs`

* `src/MangaManagementSystem.Web/Services/Api/AuthApiClient.cs`

* `src/MangaManagementSystem.Web/Services/Api/IAuthApiClient.cs`



## Remaining Work



This session completes only the registration and Google Signup portion of the broader auth/admin hardening plan.



The remaining security, RBAC, logout, portfolio, admin management, audit, file management, documentation, and authorization tasks must be implemented in subsequent steps.
