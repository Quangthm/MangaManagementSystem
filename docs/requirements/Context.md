# Manga Creation Workflow and Publishing Management System — Project Context for Teammates

> **Purpose of this file:** Give every teammate and AI assistant the same understanding of the system scope, MVP boundaries, business roles, functional requirements, and design decisions.  
> **Important warning:** This is **not** a payroll, salary, public manga reader, e-commerce, or full drawing application. Do **not** add modules such as salary calculation, payment processing, public reader accounts, monetization, or full professional drawing tools unless the team leader explicitly changes the scope.

---

## 1. Project Summary

**Project name:** Manga Creation Workflow and Publishing Management System

**Project type:** University software engineering project

**Main goal:**  
Build a web-based system that helps manga production teams manage the workflow from **series proposal**, **chapter/page production**, **assistant task assignment**, **editorial review**, **board decision-making**, **publication scheduling**, **ranking/tracking**, and **audit logging**.

The system supports manga production management. It does **not** replace professional drawing tools such as Clip Studio Paint, Photoshop, or Krita.

---

## 2. Scope Statement

### 2.1 What the System Is

The system is a **workflow, review, publishing, task coordination, ranking, and audit management platform** for manga production teams.

It focuses on:

- Managing manga series and proposals
- Managing chapters and pages
- Uploading files and manga assets
- Assigning work to assistants
- Reviewing submitted work
- Adding annotations/comments to manga pages
- Supporting editorial review
- Supporting board voting and strategic decisions
- Tracking ranking/performance snapshots
- Managing user accounts and role-based access
- Recording important actions in audit logs
- Optionally supporting AI-assisted page analysis

### 2.2 What the System Is Not

The system is **not**:

- A full manga drawing system
- A professional digital art tool
- A public manga reading website
- A salary/payroll calculation system
- A finance/accounting system
- A full e-commerce or subscription platform
- A fully automatic manga localization system
- A system that replaces human editors, mangaka, or board members

Do **not** implement:

- Salary calculation modules
- Payroll modules
- Payment/earning transaction modules
- Public reader account modules
- Shopping/cart/payment modules
- Full brush/inking/layer drawing tools
- Fully automatic AI translation/localization without human review
- Complex AI model training dashboard
- Advanced AI analytics unless explicitly requested

---

## 3. MVP Scope

The MVP should focus on core workflow and management features that are realistic for a university project.

### 3.1 MVP Features

| Module | Included in MVP? | Notes |
|---|---:|---|
| Authentication and login | Yes | Username/email + password login |
| Role-Based Access Control | Yes | Role determines accessible screens/actions |
| Registration request approval | Yes | Admin approves/rejects user registration requests |
| User management | Yes | Admin can create, lock, disable, unlock/reactivate users |
| Series management | Yes | Create/view/update series profile |
| Series proposal management | Yes | Submit proposal package and review it |
| Chapter management | Yes | Create/view/update chapters |
| Chapter page upload | Yes | Upload and manage page files |
| File resource management | Yes | Store metadata for uploaded files |
| Task assignment | Yes | Mangaka/editor assigns page/chapter tasks to assistants |
| Task submission | Yes | Assistant submits completed work |
| Task review | Yes | Mangaka/editor approves or requests revision |
| Editorial review | Yes | Editor reviews proposals/chapters/pages |
| Board voting/decision | Yes | Board votes and decides serialized/rejected/continued/dropped |
| Ranking dashboard/demo data | Yes | Use synthetic/demo ranking data |
| Publication scheduling | Basic | Schedule/release chapter records |
| Annotation/comments | Basic | Page-level annotations and correction boxes |
| Audit logging | Yes | Track important sensitive actions |
| Notification | Basic | Notify relevant users about assigned/reviewed/decided items |
| AI assistance | Optional demo | Local Python AI service for detection suggestions only |

---

## 4. Future Improvements / Post-MVP Ideas

These are allowed as future extension ideas but should **not** be treated as required MVP features.

### 4.1 Reasonable Future Improvements

- Page version comparison UI
- More advanced page version management
- More detailed AI page-region correction history
- Persistent speech bubble / panel / region objects
- AI-assisted OCR and translation suggestion workflow
- Multi-language translation workflow
- More advanced ranking analytics
- Better notification preferences
- Audit export/archive UI
- Advanced search and filtering by genre/tag/target audience
- Admin configuration pages
- AI service settings page
- Backup and archive tools
- Reporting dashboards

### 4.2 Future Features That Must Stay Human-Reviewed

AI-related features must remain advisory and human-reviewed:

- Panel detection
- Speech bubble detection
- Optional character region detection
- OCR extraction
- Translation suggestions
- Coordinate/bounding-box suggestions

AI should **suggest**. Humans should **confirm, correct, approve, or reject**.

---

## 5. Main User Roles

### 5.1 Mangaka

The Mangaka is the creator/lead production owner of a manga series.

Main responsibilities:

- Create series profile
- Submit series proposal
- Create chapters
- Upload chapter/page drafts
- Decompose page/chapter work into tasks
- Assign tasks to assistants
- Review assistant submissions
- Approve assistant work or request revision
- Submit chapters for editorial review
- Respond to editor feedback
- Track own series ranking/cancellation risk

### 5.2 Assistant

The Assistant supports production work assigned by the Mangaka or editor.

Main responsibilities:

- View assigned tasks
- Download task resources/references
- Work on assigned page/chapter task
- Submit completed work
- Revise work based on feedback
- Track approved/rejected/revision task history

### 5.3 Tantou Editor

The Tantou Editor manages editorial review and production quality.

Main responsibilities:

- Review series proposals
- Review chapter submissions
- Annotate manuscript/page issues
- Request revisions
- Approve proposals/chapters when appropriate
- Monitor production progress
- Prepare/support board decisions
- Add review comments and correction instructions

### 5.4 Editorial Board Member

The Editorial Board Member makes higher-level strategic decisions.

Main responsibilities:

- Review proposal information
- Review ranking/performance information
- Cast votes
- Decide whether a series is serialized, rejected, continued, dropped, placed on hold, or schedule-adjusted
- Monitor ranking and performance dashboards

### 5.5 System Administrator

The System Administrator manages accounts and platform configuration.

Main responsibilities:

- Approve/reject registration requests
- Create internal accounts
- Assign roles
- Lock/unlock accounts
- Disable/reactivate accounts
- View audit logs
- Configure system settings if needed

### 5.6 Auditor

The Auditor is optional.

Main responsibilities:

- View audit logs
- Verify integrity of important records
- Review system activity history
- Does not normally modify manga workflow content

---

## 6. Core Functional Requirements

### 6.1 Authentication and Account Management

The system shall:

- Allow users to log in using valid credentials.
- Store account identity and login state in `auth.Users`.
- Keep account statuses such as:
  - `PENDING_APPROVAL`
  - `ACTIVE`
  - `DISABLED`
  - `LOCKED`
- Support admin approval/rejection of registration requests.
- Support account lock/disable/unlock/reactivation by admin.
- Track failed login attempts and temporary locks.
- Use backend password hashing such as BCrypt or Argon2.
- Avoid password verification inside SQL.

### 6.2 Role-Based Access Control

The system shall:

- Assign users to roles.
- Grant permissions based on roles.
- Restrict screens and actions based on user role.
- Prevent users from accessing unauthorized functions.
- Store role assignment history if feasible.

Important role rules:

- Admin manages accounts and roles.
- Mangaka manages own series/chapter/task workflow.
- Assistant only sees assigned work.
- Editor reviews proposals/chapters/pages.
- Board member votes and makes strategic decisions.
- Auditor views audit records only.

### 6.3 Series and Proposal Management

The system shall:

- Allow Mangaka to create series profiles.
- Store title, synopsis, character overview, genre/category information, target audience, cover file, ranking fields, and current status.
- Use `slug` as URL-friendly unique identifier for series pages.
- Allow proposal submission as a review package.
- Treat proposal revisions as new proposal versions rather than overwriting old submitted proposal records.
- Allow editors and board members to review proposal status.

### 6.4 Chapter and Page Management

The system shall:

- Allow creation of chapters under a series.
- Allow pages to be uploaded under chapters.
- Store page file metadata through `FileResource`.
- Track page/chapter status.
- Allow page-level notes and annotations.
- Support page version management if the team chooses to keep `ChapterPageVersion`.

Important decision:

- `ChapterPage` represents one logical page.
- `ChapterPageVersion` stores different versions/files of that page.
- Do not claim version control exists unless the version table/workflow is actually implemented.

### 6.5 Task Assignment and Submission

The system shall:

- Allow Mangaka/editor to create tasks for chapters/pages.
- Assign tasks to assistants.
- Allow assistants to accept/work/submit assigned tasks.
- Store task submissions and file resources.
- Allow reviewer to approve, reject, or request revision.
- Track assignment/submission/review states.

### 6.6 Editorial Review

The system shall:

- Allow editors to review series proposals and chapter submissions.
- Allow editors to add comments and markup files.
- Allow editors to approve, reject, or request revision.
- Store review round information.
- Keep human review central to the workflow.

### 6.7 Board Voting and Decision

The system shall:

- Allow board members to vote on series proposals.
- Allow aggregation of approve/reject/abstain counts.
- Store final board decision summary.
- Allow board decisions to affect series/proposal status.
- Record important board actions in audit log.

### 6.8 Publication and Ranking

The system shall:

- Allow chapter release scheduling.
- Track release status.
- Store ranking snapshots for series.
- Use demo/synthetic ranking data for MVP.
- Show ranking dashboard to editorial board.
- Support cancellation/continuation decision workflow using ranking data.

### 6.9 File Resource Management

The system shall:

- Store file metadata in `manga.FileResource`.
- Store actual file bytes on disk/cloud/object storage, not directly inside business tables.
- Store file path, original filename, stored filename, content type, file size, optional SHA-256 hash, uploader, and upload time.
- Use controlled endpoints for preview/download.
- Link business tables to `FileResource`.

Important decision:

- Keep `uploaded_at_utc` and `uploaded_by_user_id`.
- These are operational file metadata, not a replacement for audit logs.
- Audit log is for important event traceability; file metadata is for normal application queries.

### 6.10 Annotation

The system shall:

- Support page-level comments and correction boxes.
- Allow editor/mangaka to mark issues on page coordinates.
- Allow annotations to be resolved.
- Use annotation for human feedback and task coordination.
- Avoid turning annotation into full AI operation history unless required.

### 6.11 Notification

The system shall:

- Notify users about important workflow events such as:
  - Registration approval/rejection
  - Task assignment
  - Task review
  - Revision request
  - Chapter review
  - Board decision
  - Publication schedule
  - Ranking warning

### 6.12 Audit Logging

The system shall:

- Record important actions in `audit.AuditEvent`.
- Preserve actor identity and role snapshot.
- Store action code, entity type, entity id, timestamp, and details.
- Support audit integrity through SQL Server Ledger if available.
- Use custom hash chain fallback if SQL Server Ledger is not available.
- Keep audit log tamper-evident and not editable by normal users.

Important audited actions:

- Login failure/success if required
- Account lock/unlock/disable/reactivation
- Role assignment/revocation
- Registration approval/rejection
- Series/proposal submission
- Editorial approval/rejection/revision request
- Board vote/decision
- Ranking snapshot generation
- Series cancellation/status change
- Chapter release approval

---

## 7. Non-Functional Requirements

### 7.1 Security

- Enforce RBAC on every protected action.
- Do not rely only on frontend hiding buttons.
- Hash passwords using backend BCrypt/Argon2.
- Do not use SHA-256 for passwords.
- Validate file uploads by extension, MIME type, size, and permission.
- Prevent unauthorized file access.

### 7.2 Auditability

- Important actions must be traceable.
- Audit records should preserve actor role snapshot.
- Audit records should be tamper-evident if possible.
- Admin/auditor may view/export logs but should not edit them.

### 7.3 Maintainability

- Use clear modules:
  - Auth
  - Manga workflow
  - Audit
  - File resource
  - AI integration
- Avoid overengineering unnecessary features.
- Keep AI optional and separated from the main workflow.

### 7.4 Usability

- Provide role-based dashboards.
- Use shared workspace UI for Mangaka, Assistant, and Editor where possible.
- Show only tools allowed for the current role.
- Keep page/work/task review screens simple and clear.

### 7.5 Traceability

- Connect proposals, chapters, pages, tasks, submissions, reviews, decisions, and audit logs.
- Preserve file metadata and review history.
- Avoid deleting important workflow records; prefer status changes or soft deletion.

---

## 8. Architecture Decisions

### 8.1 Planned Technology Stack

| Layer | Decision |
|---|---|
| Main application | C# / .NET 8 |
| Frontend | Blazor Web App / Blazor Server |
| UI library | MudBlazor |
| Annotation/canvas | Fabric.js over HTML5 Canvas |
| Backend structure | Clean Architecture-style layers |
| Database | SQL Server |
| Schemas | `auth`, `manga`, `audit` |
| AI service | Python local/internal service |
| AI communication | JSON API bridge |
| File storage | Disk/cloud/object storage with metadata in DB |

### 8.2 High-Level Architecture

The intended architecture is a **Distributed Monolith with Local AI Advisory Service**:

- Main app handles business workflow.
- Python AI Hub is separate because AI inference is easier in Python.
- Backend communicates with AI through JSON.
- Database stores workflow metadata and audit records.
- File storage stores uploaded page/proposal/submission files.

### 8.3 Blazor and Figma Decision

- Figma is used for UI/UX design and clickable prototypes.
- Final implementation should be rebuilt in Blazor/MudBlazor.
- Figma Make may generate React/TypeScript, but that should be treated as prototype/reference only.
- The production app should use .NET/Blazor/MudBlazor.

---

## 9. AI Intelligence Hub Scope

### 9.1 What AI Can Do

AI may support:

- Panel detection
- Speech bubble detection
- Optional character region detection
- OCR extraction
- Translation suggestions
- Region coordinate suggestions
- JSON output to .NET backend
- Displaying detected regions in Fabric.js canvas

### 9.2 What AI Must Not Claim

AI must not claim:

- Fully automatic professional manga translation
- Perfect segmentation
- Replacing editor/mangaka decisions
- Replacing professional drawing tools
- Fully automatic publishing decision-making

### 9.3 Best AI Positioning

Use this wording:

> The AI Intelligence Hub is an internal advisory module that suggests page regions, coordinates, OCR/translation candidates, or layout metadata. Human users remain responsible for final correction, approval, and publication decisions.

---

## 10. Important Database / Modeling Decisions

### 10.1 Schemas

Use three main schemas:

- `auth` for user, role, permission, registration
- `manga` for manga workflow data
- `audit` for audit events and integrity records

### 10.2 Lookup Tables

Current design uses lookup tables for status/type values. If the team wants fewer tables, some simple values can be replaced by `CHECK` constraints, but workflow-critical statuses are safer as lookup tables.

Possible workflow lookups:

- Series status
- Proposal status
- Chapter status
- Page status
- Task status
- Submission status
- Review decision
- Vote choice
- Publication status

### 10.3 Genre / Tag / Target Audience

Initial `Series` table has direct columns:

- `genre`
- `target_audience`

For proper filtering, recommended future structure:

- `Genre`
- `SeriesGenre`
- `Tag`
- `SeriesTag`
- `TargetAudience`

Recommended approach:

- Genre: many-to-many if a series can have multiple genres
- Tag: many-to-many
- Target audience: one FK is enough for MVP
- Slug: keep as URL-friendly unique identifier

### 10.4 Slug

A slug is a URL-friendly unique text identifier.

Example:

- Title: `Dark Moon Academy`
- Slug: `dark-moon-academy`
- URL: `/series/dark-moon-academy`

Keep `slug` in `Series`.

### 10.5 FileResource

`FileResource` stores metadata only.

Recommended fields:

- `file_resource_id`
- `file_purpose`
- `original_file_name`
- `stored_file_name`
- `storage_path`
- `content_type`
- `file_size_bytes`
- `sha256_hash`
- `uploaded_by_user_id`
- `uploaded_at_utc`
- `is_deleted`

Do not store file bytes directly in business tables.

### 10.6 Audit Role Snapshot

Keep actor role snapshot in audit records:

- `actor_role_name_snapshot`

Reason:

- If a user changes role later, the audit log should still show their role at the time of the action.

### 10.7 Registration Request

Use a separate table for registration approval workflow:

- `UserRegistrationRequest`

Do not store approval/rejection directly in `Users`.

Recommended statuses:

- `PENDING`
- `APPROVED`
- `REJECTED`
- `CANCELLED`

### 10.8 Page Versioning

If using `ChapterPageVersion`, then:

- `ChapterPage` = logical page
- `ChapterPageVersion` = uploaded versions of that page

If not implementing this, do not claim advanced page version control.

### 10.9 Avoid Salary/Payroll Tables

Do not create:

- Salary
- Payroll
- Payment
- Earnings
- Invoice
- Transaction
- Subscription
- Public reader purchase

These are out of scope.

---

## 11. Recommended Main Tables

### 11.1 Auth

- `auth.Users`
- `auth.Roles`
- `auth.UserRole`
- `auth.UserRegistrationRequest`
- `auth.Permissions`
- `auth.RolePerm`

### 11.2 Audit

- `audit.AuditEvent`
- `audit.AuditHashChain`

### 11.3 Manga Workflow

- `manga.FileResource`
- `manga.Series`
- `manga.SeriesContributor`
- `manga.SeriesProposal`
- `manga.SeriesEditorialReview`
- `manga.SeriesBoardVote`
- `manga.SeriesBoardDecision`
- `manga.SeriesPublicationPolicy`
- `manga.Chapter`
- `manga.ChapterPage`
- `manga.ChapterPageVersion`
- `manga.ChapterPageAnnotation`
- `manga.ChapterTask`
- `manga.ChapterTaskAssignment`
- `manga.ChapterTaskSubmission`
- `manga.ChapterTaskReview`
- `manga.ChapterSubmission`
- `manga.ChapterEditorialReview`
- `manga.ChapterRelease`
- `manga.SeriesRankingSnapshot`
- `manga.Notification`

---

## 12. Recommended Core UI Pages

### 12.1 Public / Auth UI

- Landing page
- Login page
- Register page
- Access denied page

### 12.2 General Logged-In UI

- Role-based dashboard
- Notification center
- Profile page
- Change password page

### 12.3 Shared Manga Workplace UI

One shared workspace for Mangaka, Assistant, and Editor, with tools shown/hidden by role.

Common elements:

- Series/chapter/page list
- Page preview
- Annotation canvas
- Task panel
- Comment/review panel
- Submission panel
- Status indicators

Role-specific tools:

- Mangaka: upload page, create task, assign assistant, review task, submit chapter
- Assistant: view task, download resources, submit work, respond to revision
- Editor: review page/chapter, annotate, approve, reject, request revision
- Board: view summary/ranking/decision data only if needed
- Admin: account/audit/system pages, not production workspace by default

### 12.4 Admin UI

- Admin dashboard
- User account management
- Registration request approval
- Audit log viewer
- Role/permission management
- System settings
- Future placeholder: version management
- Future placeholder: page style settings
- Future placeholder: AI service settings
- Logout navigation

### 12.5 Board UI

- Board dashboard
- Proposal review queue
- Voting page
- Board decision page
- Leaderboard/ranking dashboard
- Publication/cancellation decision view

### 12.6 Review UI

- Editorial review queue
- Chapter review page
- Page review/annotation page
- Revision request page
- Review history page

### 12.7 Task UI

- My tasks
- Task board
- Task detail
- Task submission
- Task review
- Task history

---

## 13. Conceptual ERD Guidance

If drawing a conceptual ERD, draw main business entities only.

Include:

- User
- Role
- RegistrationRequest
- AuditEvent
- Notification
- FileResource
- Series
- SeriesContributor
- SeriesProposal
- SeriesEditorialReview
- SeriesBoardVote
- SeriesBoardDecision
- SeriesPublicationPolicy
- SeriesRankingSnapshot
- Chapter
- ChapterPage
- ChapterPageVersion
- ChapterPageAnnotation
- ChapterTask
- ChapterTaskAssignment
- ChapterTaskSubmission
- ChapterTaskReview
- ChapterSubmission
- ChapterEditorialReview
- ChapterRelease

Do not draw in conceptual ERD:

- IDs
- FK columns
- lookup tables
- indexes
- SQL constraints
- created_by/updated_by technical relationships
- status history tables unless required

---

## 14. One-Verb ERD Relationship Labels

Use simple verbs on ERD relationship lines:

- User **submits** RegistrationRequest
- User **reviews** RegistrationRequest
- User **uploads** FileResource
- User **performs** AuditEvent
- User **receives** Notification
- User **leads** Series
- User **contributes** SeriesContributor
- Series **has** SeriesContributor
- Series **has** SeriesProposal
- Series **contains** Chapter
- Series **tracks** SeriesRankingSnapshot
- SeriesProposal **receives** SeriesEditorialReview
- User **reviews** SeriesEditorialReview
- User **votes** SeriesBoardVote
- SeriesProposal **receives** SeriesBoardVote
- SeriesProposal **receives** SeriesBoardDecision
- User **decides** SeriesBoardDecision
- Series **has** SeriesPublicationPolicy
- Chapter **contains** ChapterPage
- ChapterPage **has** ChapterPageVersion
- ChapterPage **has** ChapterPageAnnotation
- User **annotates** ChapterPageAnnotation
- User **resolves** ChapterPageAnnotation
- Chapter **has** ChapterTask
- ChapterPage **targets** ChapterTask
- User **assigns** ChapterTaskAssignment
- User **receives** ChapterTaskAssignment
- ChapterTaskAssignment **has** ChapterTaskSubmission
- User **submits** ChapterTaskSubmission
- ChapterTaskSubmission **receives** ChapterTaskReview
- User **reviews** ChapterTaskReview
- Chapter **has** ChapterSubmission
- User **submits** ChapterSubmission
- ChapterSubmission **receives** ChapterEditorialReview
- User **reviews** ChapterEditorialReview
- Chapter **has** ChapterRelease
- User **approves** ChapterRelease
- AuditEvent **chains** AuditHashChain

---

## 15. Leader Decisions Already Made

These decisions should be followed unless the leader changes them.

### 15.1 Scope Decisions

- The system is a workflow/review/publishing/task coordination platform.
- The system is not a full drawing system.
- AI is optional and advisory.
- Human review is required for AI output.
- Public reader/payment/payroll features are out of scope.
- Use synthetic/demo data for ranking.

### 15.2 Architecture Decisions

- Main app uses C#/.NET/Blazor.
- UI should be compatible with MudBlazor.
- Python AI service is separate/internal/local.
- Backend communicates with AI using JSON.
- SQL Server is the main database.
- Use `auth`, `manga`, and `audit` schemas.

### 15.3 Auth Decisions

- `auth.Users` focuses on account identity and login state.
- Registration approval uses `auth.UserRegistrationRequest`.
- Password hashing happens in backend.
- Do not use SHA-256 for passwords.
- Account statuses: `PENDING_APPROVAL`, `ACTIVE`, `DISABLED`, `LOCKED`.
- Registration request statuses: `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`.

### 15.4 File Decisions

- Store files on disk/cloud/object storage.
- Store metadata in `FileResource`.
- Keep `uploaded_at_utc` and `uploaded_by_user_id`.
- Use file purpose values to distinguish proposal, page, task submission, markup, portfolio, etc.

### 15.5 Audit Decisions

- Keep audit log for sensitive actions.
- Keep actor role snapshot.
- SQL Server Ledger is optional only if supported.
- Custom hash chain is fallback.
- Admin/auditor may view/export logs but not edit audit records.

### 15.6 AI Decisions

- AI Intelligence Hub is internal/local if team hosts it.
- AI can detect panels/speech bubbles/regions and return coordinates.
- AI output must be human-reviewed.
- Do not claim fully automatic localization.
- Do not track every low-level Fabric.js edit operation for MVP.

### 15.7 Database Simplification Decisions

- Lookup tables can be reduced if the team complains about table count.
- However, workflow-critical statuses should remain controlled in some way, either lookup tables or strict CHECK constraints.
- Genre/tags/target audience filtering may need extra tables later.

---

## 16. Common AI Misinterpretations to Avoid

If an AI assistant suggests these, reject them unless the leader explicitly approves:

- Salary calculation
- Payroll or compensation automation
- Payment module
- Public reader marketplace
- Subscription management
- Manga reading platform
- Full image editing/drawing engine
- AI replacing human review
- Complex AI operation/event tracking for every canvas movement
- Full enterprise DevOps monitoring
- Overly complex microservice architecture
- Blockchain/NFT/copyright marketplace
- Overcomplicated financial reporting
- Automatic scraping of manga sites

Correct interpretation:

> This is a manga production workflow management system for internal team coordination, editorial review, board decisions, publication scheduling, ranking tracking, audit logging, and optional AI-assisted page analysis.

---

## 17. Suggested Prompt for Teammates' AI Models

Copy this when asking another AI model for help:

```text
We are building a university software engineering project called Manga Creation Workflow and Publishing Management System.

This is NOT a payroll system, public manga reader, marketplace, or full drawing tool. Do not add salary/payment/public reader/drawing-engine modules.

The system manages manga production workflow: series proposals, chapters, pages, assistant tasks, task submissions, editorial reviews, board voting/decisions, publication scheduling, ranking dashboards, file resources, notifications, RBAC, admin account management, and audit logging.

Main roles: Mangaka, Assistant, Tantou Editor, Editorial Board Member, System Administrator, optional Auditor.

Tech stack: C# .NET 8 Blazor Web App with MudBlazor, SQL Server, and optional local Python AI Intelligence Hub using JSON API. AI is advisory only and human-reviewed.

MVP scope: RBAC, user management, registration approval, series/proposal/chapter/page CRUD, file upload, task assignment/submission/review, editorial review, board voting/decision, ranking dashboard with demo data, audit log, notification, basic annotation, optional AI detection demo.

Out of scope: salary/payroll/payment modules, public reader account system, e-commerce, full drawing system, fully automatic AI localization, advanced AI operation analytics.
```

---

## 18. Final Team Rule

When uncertain, prioritize:

1. Core manga workflow
2. Role-based permissions
3. Human review and approval
4. File/resource traceability
5. Auditability
6. Simple MVP implementation

Avoid adding unrelated “enterprise” modules that do not directly support manga production workflow.
