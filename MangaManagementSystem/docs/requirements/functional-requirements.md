# Functional Requirements




<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-START -->

---

## Phase 7 Addendum â€” Auth/Admin Functional Requirements

### 3.X Architecture and Authorization Boundary

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-ARCH-001 | The system shall keep account-management business decisions and orchestration in the C# Application layer. | BR-ARCH-001 |
| FR-ARCH-002 | The system shall use SQL Server procedures for persistence, transaction consistency, constraint/backstop checks, and audit append behavior. | BR-ARCH-002 |
| FR-ARCH-003 | The system shall prevent Web UI components from directly calling Application services, Infrastructure repositories, DbContext, or stored procedures for new or migrated protected business workflows. | BR-ARCH-003 |
| FR-ARCH-004 | The system shall route protected Web workflows through typed API clients, API controllers, Application commands/queries, Infrastructure adapters, and then SQL Server when persistence is required. | BR-ARCH-003, BR-ARCH-004 |

### 3.4A Auth/Admin Account Management Alignment

| ID | Functional Requirement | Source Business Rules |
|---|---|---|
| FR-USER-023 | The system shall allow public registration to request any role in the Application public-registration whitelist. | BR-USER-023 |
| FR-USER-024 | The system shall create publicly registered accounts with `PENDING_APPROVAL` status, including accounts requesting the `Admin` role. | BR-USER-023, BR-USER-024 |
| FR-USER-025 | The system shall prevent public registration from creating an `ACTIVE` Admin account directly. | BR-USER-024 |
| FR-USER-026 | The system shall prevent pending Admin accounts from logging in or accessing Admin pages before approval. | BR-USER-025 |
| FR-USER-027 | The system shall allow only an existing active Admin to activate or approve a pending Admin account. | BR-USER-024, BR-USER-026 |
| FR-USER-028 | The system shall require a reason when an Admin rejects or disables an account. | BR-USER-026, BR-USER-027 |
| FR-USER-029 | The system shall preserve rejected accounts and keep their username and email reserved in MVP. | BR-USER-027 |
| FR-USER-030 | The system shall reject direct calls to internal Google Signup endpoints when the required internal API key is missing or invalid. | BR-USER-028 |
| FR-USER-031 | The system shall prevent direct URL entry from bypassing Admin route authorization, protected workspace authorization, or protected API authorization. | BR-USER-029, BR-USER-030 |
| FR-USER-032 | The system shall prevent an Admin from rejecting or disabling their own Admin account through the normal account-management workflow. | BR-USER-031 |

### Acceptance Criteria Notes

- Public Admin registration is valid only when the resulting account remains `PENDING_APPROVAL`.
- A direct request is considered blocked when it cannot create an `ACTIVE` Admin and cannot bypass the internal/API authorization boundary.
- UI tests must include direct URL entry, not only hidden navigation/menu checks.

<!-- PHASE7-AUTH-ADMIN-ALIGNMENT-END -->

