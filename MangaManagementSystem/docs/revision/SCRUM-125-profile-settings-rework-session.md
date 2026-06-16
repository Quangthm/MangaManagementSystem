\# SCRUM-125 Profile Settings Rework Session



\## Leader Requirements



\- Build a shared/common Profile UI.

\- Profile updates must call database stored procedures.

\- If stored procedures do not exist, create matching procedures.

\- Avatar and portfolio changes must require confirmation.

\- Avatar update should support image crop.

\- Add reset password setup in the profile/settings page.



\## Current Branch



\- Branch: feature/scrum-125-profile-settings-rework



\## Initial Status



\- Repository downloaded/pulled from GitHub.

\- New feature branch created.

\- No local code changes yet.

\- Next step is to inspect existing profile/account/settings UI and existing SQL procedures.



\## Planned Work



1\. Locate existing profile/user/account/settings UI pages.

2\. Create or reuse a shared profile settings page.

3\. Add stored procedures for:

&#x20;  - display name update

&#x20;  - avatar file update

&#x20;  - portfolio file update

&#x20;  - reset password

4\. Update service/repository layer to call procedures.

5\. Add confirmation before changing avatar and portfolio.

6\. Add avatar crop flow before upload.

7\. Add reset password section in profile/settings page.

8\. Test UI and SQL updates.

9\. Update this revision note after implementation.
## Implementation Update



Completed profile settings rework:



\- Added shared Profile Settings page at `/profile/settings`.

\- Added Profile Settings link to Mangaka sidebar.

\- Added profile menu link from avatar dropdown.

\- Added update display name UI.

\- Added avatar upload with confirmation and crop preview.

\- Added unique avatar file naming to prevent duplicate Cloudinary public id errors.

\- Added portfolio upload with confirmation.

\- Added reset password section in Profile Settings.

\- Profile updates now call stored procedures through repository/service.

\- Added SQL procedures:

&#x20; - `auth.usp\_User\_UpdateDisplayName`

&#x20; - `auth.usp\_User\_UpdateAvatarFile`

&#x20; - `auth.usp\_User\_UpdatePortfolioFile`

&#x20; - `auth.usp\_User\_ResetPassword`



\## Local Test Results



\- Build: Passed

\- Display name update: Passed

\- Avatar upload with confirmation and crop preview: Passed

\- Avatar SQL verification: Passed

\- Portfolio upload with confirmation: Passed

\- Reset password: Passed

\- Profile Settings link in Mangaka sidebar: Passed

\- SQL procedures verification: Passed

