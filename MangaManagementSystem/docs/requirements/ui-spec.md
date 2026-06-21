# UI Specification




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Addendum â€” Auth/Admin UI and Authorization Specification

### Registration Role Selection

| UI Area | Specification |
|---|---|
| Register page role dropdown | Shows only roles returned/allowed by the Application public-registration whitelist. If `Admin` is present in the whitelist, it may be displayed. |
| Admin role request behavior | Selecting `Admin` must not imply immediate Admin access. The post-registration state remains `PENDING_APPROVAL`. |
| Pending approval result | After successful OTP completion or Google Signup, the UI routes the user to a pending approval state/page instead of an Admin dashboard. |

### Admin Account Management

| UI Area | Specification |
|---|---|
| Pending accounts list | Shows pending users, including pending Admin requests. |
| Approve action | Available only to active Admin users. Approving changes the target account to `ACTIVE`. |
| Reject action | Requires a non-empty reason and changes the target account to `REJECTED`. |
| Disable action | Requires a non-empty reason and changes the target account to `DISABLED`. |
| Activate/re-enable action | Allows active Admin users to reactivate accounts only through the approved Admin workflow. |
| Self-action protection | UI should not offer reject/disable actions against the current Admin's own account; API/Application must still enforce this. |

### Direct URL Authorization Tests

| Scenario | Expected UI/API Behavior |
|---|---|
| Anonymous user types `/admin` or `/admin/users` | Redirect to login or receive unauthorized response. |
| Active non-Admin user types `/admin` or `/admin/users` | Access denied page or forbidden response. |
| Pending user types protected route | Pending approval/access blocked state. |
| Rejected or disabled user tries login then protected route | Login blocked; protected route inaccessible. |
| Active Admin types `/admin` or `/admin/users` | Page loads and data/actions are available according to Admin permissions. |

### UX Notes

- Hiding Admin links in the navigation menu is not sufficient for security.
- Protected pages must enforce route authorization and protected API calls must enforce actor authorization.
- Error messages should be safe and should not expose stack traces, SQL errors, OTP values, password hashes, internal API keys, or secret configuration values.

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

