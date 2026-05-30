## Goal

Implement user registration, pending approval, activation, disabling, and role-based access behavior.

## Source IDs

Business Rules:
- BR-USER-001
- BR-USER-002
- BR-USER-003
- BR-USER-004
- BR-USER-005
- BR-USER-006
- BR-USER-010

Functional Requirements:
- FR-USER-001
- FR-USER-002
- FR-USER-003
- FR-USER-004
- FR-USER-005
- FR-USER-006
- FR-USER-009

User Stories:
- US-NEW-001
- US-ADMIN-001
- US-SYSADMIN-001

## Description

The system should support one MVP role per user. New registered users start as PENDING_APPROVAL and cannot access protected workspace functions until approved. Admin users can activate or disable accounts.

## Acceptance Criteria

- [ ] New users are created with PENDING_APPROVAL.
- [ ] Pending users cannot access protected workspace features.
- [ ] Admin can activate pending users.
- [ ] Admin can disable users.
- [ ] Disabled users cannot log in.
- [ ] Account approval/disable actions are audit-log ready.

## Out of Scope

- OAuth login.
- Email verification.
- Multi-role user accounts.
