# User Stories




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Addendum â€” Auth/Admin User Stories

### New User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-NEW-004 | BR-USER-023, BR-USER-024, BR-USER-025 | As a New User, I may request the Admin role during public registration when it is available in the role list, so that I can request administrative access while still waiting for approval before I can use Admin functions. |
| US-NEW-005 | BR-USER-028 | As a New User using Google Signup, I want the system to process my signup only through the official Web flow, so that direct external requests cannot create accounts without the internal Web-to-API boundary. |

### Admin

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-ADMIN-ACCOUNT-001 | BR-USER-024, BR-USER-026 | As an Admin, I want to approve pending accounts, including pending Admin requests, so that no newly registered account becomes active without Admin review. |
| US-ADMIN-ACCOUNT-002 | BR-USER-026, BR-USER-027 | As an Admin, I want to reject or disable accounts only with a reason, so that account access decisions are traceable. |
| US-ADMIN-ACCOUNT-003 | BR-USER-031 | As an Admin, I do not want to reject or disable my own account accidentally, so that the system does not remove the last active administrative access through a normal self-action. |

### General System User / Protected Route Actor

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-AUTHZ-001 | BR-USER-029, BR-USER-030 | As a protected-route user, I want direct URL entry to enforce the same authorization rules as normal navigation, so that typing an Admin or workspace URL cannot bypass role or status checks. |
| US-AUTHZ-002 | BR-USER-003, BR-USER-012, BR-USER-025 | As a pending, rejected, or disabled user, I should be blocked from protected functions, so that only active approved users can access role-specific workflows. |

### Developer / Maintainer

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-DEV-ARCH-001 | BR-ARCH-001, BR-ARCH-002, BR-ARCH-003 | As a developer, I want account-management business rules to live in C# Application orchestration and SQL to remain the persistence/audit boundary, so that implementation follows the team leader's architecture direction. |

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

