# Revised User Stories by Actor

Project: Manga Creation Workflow and Publishing Management System  
Basis: Verified MVP business rules, updated PageRegion segmentation rules, and permission-based actor grouping.

## Actor Consolidation and Shared Actor Groups Applied

| Previous Actor Group / Issue | Final Actor Group | Handling |
|---|---|---|
| Authorized Contributor | Mangaka | Merged into Mangaka; duplicate series/chapter/page stories removed. |
| Reviewer / Authorized Reviewer | Tantou Editor | Merged into Tantou Editor; duplicate annotation and task-review stories removed. |
| Auditor / System Admin | Admin | Merged into Admin; duplicate audit/traceability stories removed. |
| Broad `System User` for page tools | Authorized Page Workspace User | Page segmentation, region editing, OCR/translation assistance, and page-version feedback are moved out of `System User` so Editorial Board Members are not accidentally included. |
| Shared workflow list/filtering behavior | Authorized Workflow Participant | Generic workflow-list filtering applies only to users who have permission to view a workflow queue/list for their role. |
| Duplicated Mangaka/Editor AI segmentation stories | Authorized Page Workspace User + role-specific stories | Shared segmentation mechanics are grouped, while Mangaka and Tantou Editor keep separate purpose/value stories. |
| Duplicated Mangaka/Editor AI/OCR translation stories | Authorized Page Workspace User + role-specific stories | Shared human-reviewed AI/OCR behavior is grouped, while Mangaka keeps finalization ownership and Tantou Editor keeps review/checking context. |

## Shared Actor Definitions

| Actor Group | Meaning |
|---|---|
| General System User | Any approved authenticated user using common system features such as file display, status visibility, timestamps, and in-app notifications. |
| Authorized Workflow Participant | A user who is allowed to view a specific workflow list, queue, or dashboard for their role, such as task lists, proposal queues, review queues, board poll lists, or admin queues. |
| Authorized Page Workspace User | A user who is permitted to access page-level editing, review, annotation, segmentation, or translation-support tools. This normally includes Mangaka and Tantou Editor, and may include Assistant only for assigned page/task work. Editorial Board Members are excluded unless explicitly granted page workspace permissions. |

---

## 1. New User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-NEW-001 | BR-USER-002, BR-USER-003 | As a New User, I want to register an account and receive pending approval status, so that I understand why I cannot access protected workspace functions immediately. |
| US-NEW-002 | BR-USER-007, BR-USER-008, BR-USER-009 | As a New User, I want to optionally attach an avatar or portfolio file during account setup, so that my profile can support identity display and admin review. |

---

## 2. General System User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-USER-001 | BR-FILE-001, BR-FILE-002, BR-FILE-007 | As a General System User, I want uploaded and generated files to be managed through the system file workflow, so that proposals, covers, avatars, portfolios, page images, markup files, and task outputs are handled consistently. |
| US-USER-002 | BR-FILE-008 | As a General System User, I want deleted or unavailable files to show a safe placeholder, so that the page remains usable without exposing broken file references. |
| US-USER-003 | BR-HIST-002, BR-HIST-006 | As a General System User, I want current workflow status to be visible on the main record, so that I can quickly understand the latest state without needing a separate status-history page. |
| US-USER-004 | BR-HIST-007 | As a General System User, I want meaningful event timestamps to be visible where relevant, so that submitted, reviewed, voted, created, and released actions are understandable. |
| US-USER-005 | BR-NOTIF-001, BR-NOTIF-005, BR-NOTIF-006 | As a General System User, I want to view unread in-app notifications and mark them as read, so that I can manage important workflow updates. |
| US-USER-006 | BR-NOTIF-003, BR-NOTIF-004 | As a General System User, I want notifications to link to their related business record when available, so that I can navigate directly to the relevant series, chapter, task, proposal, poll, or ranking snapshot. |

---

## 3. Authorized Workflow Participant

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-WORKFLOW-001 | BR-PGTASK-015, BR-PROP-017, BR-PROP-019 | As an Authorized Workflow Participant, I want workflow lists and queues to be filterable by relevant status and ownership fields, so that I can manage the records that my role is allowed to see. |

---

## 4. Authorized Page Workspace User

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-PAGE-001 | BR-REG-014, BR-REG-015, BR-TRANS-006 | As an Authorized Page Workspace User, I want AI suggestions to remain advisory and human-reviewed, so that AI output does not automatically approve, reject, or finalize workflow decisions. |
| US-PAGE-002 | BR-REG-006, BR-REG-007, BR-REG-016, BR-REG-017, BR-REG-019, BR-REG-020, BR-REG-021, BR-REG-022, BR-REG-023, BR-REG-024, BR-REG-025, BR-REG-026, BR-REG-027 | As an Authorized Page Workspace User, I want to load saved page regions and optionally run AI-assisted segmentation again, so that I can discover useful new regions, avoid duplicate saved regions, reuse previous human corrections, and update saved regions when permitted. |
| US-PAGE-003 | BR-TRANS-001, BR-TRANS-006, BR-TRANS-008, BR-TRANS-009 | As an Authorized Page Workspace User, I want AI/OCR translation suggestions to remain editable and human-reviewed, so that translation support does not automatically become final page content. |
| US-PAGE-004 | BR-REG-012, BR-ANN-014, BR-ANN-015, BR-CP-016, BR-CP-017 | As an Authorized Page Workspace User, I want page regions to stay tied to their page version and annotations to derive their page-version context through linked regions, so that feedback remains accurate after later uploads. |

---

## 5. Mangaka

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-MANGAKA-001 | BR-SERIES-003, BR-SERIES-005, BR-SERIES-006, BR-SERIES-007, BR-SERIES-008, BR-SERIES-009 | As a Mangaka, I want to create and maintain a series profile with title, synopsis, genre, language, cover image, and optional source series reference, so that the manga project is clearly represented and traceable. |
| US-MANGAKA-002 | BR-SERIES-010, BR-SERIES-011, BR-SC-001, BR-SC-003, BR-SC-004, BR-SC-005 | As a Mangaka, I want to manage active contributors for a series, so that the production team is visible and duplicate active membership is avoided. |
| US-MANGAKA-003 | BR-SC-006, BR-SC-007, BR-SC-008 | As a Mangaka, I want contributor history and required active team roles to be preserved, so that the series has responsible members before formal review or production. |
| US-MANGAKA-004 | BR-PROP-001, BR-PROP-004, BR-PROP-005, BR-PROP-006, BR-PROP-021 | As a Mangaka, I want to submit a formal series proposal with a proposal file and submission-time snapshot, so that editors can evaluate the exact version I submitted. |
| US-MANGAKA-005 | BR-PROP-002, BR-PROP-003, BR-PROP-007, BR-PROP-008 | As a Mangaka, I want proposal revisions to create new proposal versions instead of overwriting old submissions, so that previous review packages remain historically accurate. |
| US-MANGAKA-006 | BR-PROP-010, BR-PROP-011 | As a Mangaka, I want to withdraw a proposal before final approval or cancellation, so that I can stop review when the submission is no longer intended to proceed. |
| US-MANGAKA-007 | BR-PROP-009, BR-PROP-017, BR-PROP-018 | As a Mangaka, I want to view proposal status and version history for my series, so that I understand previous submissions and current review progress. |
| US-MANGAKA-008 | BR-CH-001, BR-CH-002, BR-CH-003, BR-CH-006 | As a Mangaka, I want to create chapters with unique chapter labels and optional planned release dates, so that the series can be organized into publishable units. |
| US-MANGAKA-009 | BR-CP-001, BR-CP-002, BR-CP-003, BR-CP-004 | As a Mangaka, I want to create logical chapter pages with unique page numbers, so that each chapter can be organized page by page. |
| US-MANGAKA-010 | BR-CP-005, BR-CP-006, BR-CP-007, BR-CP-009, BR-CP-010, BR-CP-012, BR-CP-013, BR-CP-014, BR-CP-021 | As a Mangaka, I want each page upload or revision to create a new page version while keeping only one current version, so that old work is preserved without confusing the active page. |
| US-MANGAKA-011 | BR-CP-019, BR-CP-020 | As a Mangaka, I want to soft-delete logical pages while preserving their historical versions, so that removed draft pages no longer appear active but past work is not lost. |
| US-MANGAKA-012 | BR-REG-019, BR-REG-020, BR-REG-024, BR-REG-025, BR-PGTASK-007, BR-PGTASK-012 | As a Mangaka, I want to use saved or AI-assisted page regions for translation, annotation, and task assignment, so that I can prepare production work areas for myself and assistants. |
| US-MANGAKA-013 | BR-TRANS-002, BR-TRANS-004, BR-TRANS-005, BR-TRANS-006, BR-TRANS-007, BR-TRANS-009, BR-REG-018 | As a Mangaka, I want to review, edit, override, and approve AI/OCR translation suggestions before saving the final translated or edited page as a new page version, so that final page content remains human-controlled and traceable. |
| US-MANGAKA-014 | BR-PGTASK-001, BR-PGTASK-003, BR-PGTASK-004, BR-PGTASK-005, BR-PGTASK-014 | As a Mangaka, I want to create page-based tasks assigned to one assistant or authorized user with a due date, so that production work is divided clearly. |
| US-MANGAKA-015 | BR-PGTASK-007, BR-PGTASK-008, BR-PGTASK-009, BR-PGTASK-011, BR-PGTASK-012 | As a Mangaka, I want page tasks to target one or more linked page regions, so that feedback and production work are tied to exact areas of the page. |
| US-MANGAKA-016 | BR-PGTASK-017, BR-PGTASK-018, BR-PGTASK-019, BR-PGTASK-020, BR-PGTASK-021, BR-PGTASK-022 | As a Mangaka, I want to review the submitted page version before completing a task, so that assistants do not approve their own task output. |
| US-MANGAKA-017 | BR-PGTASK-024, BR-PGTASK-025, BR-PGTASK-027, BR-PGTASK-028, BR-PGTASK-030 | As a Mangaka, I want reassignment to be handled by cancelling or completing the old task and creating a new one, so that assignment history is not overwritten. |
| US-MANGAKA-018 | BR-CH-SUB-001, BR-CH-SUB-002, BR-CH-SUB-005 | As a Mangaka, I want to submit a chapter for editorial review by moving it to UNDER_REVIEW after required active page versions exist, so that the editor reviews a stable chapter draft. |
| US-MANGAKA-019 | BR-CH-SUB-003, BR-CH-SUB-004 | As a Mangaka, I want chapter pages to lock during review and become editable again when revision is requested, so that review is stable but corrections remain possible. |
| US-MANGAKA-020 | BR-CH-REV-003, BR-CH-REV-010, BR-CH-REV-011, BR-CH-REV-012, BR-CH-REV-013 | As a Mangaka, I want to view chapter review history and final decisions, so that I understand whether the chapter was approved, requires revision, or was cancelled. |
| US-MANGAKA-021 | BR-CH-REV-007, BR-CH-CANCEL-003, BR-CH-CANCEL-004 | As a Mangaka, I want cancellation feedback and preserved chapter materials to remain available, so that I understand the decision and previous work is not lost. |
| US-MANGAKA-022 | BR-RANK-005, BR-RANK-008, BR-RANK-012, BR-NOTIF-007, BR-NOTIF-008 | As a Mangaka, I want to track ranking trends and receive cancellation-risk notifications, so that I can respond to series performance issues. |
| US-MANGAKA-023 | BR-PUB-001, BR-PUB-006, BR-PUB-009 | As a Mangaka, I want to view chapter-level release planning and derived delays, so that I understand the publication schedule for my series. |

---

## 6. Assistant

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-ASSISTANT-001 | BR-PGTASK-001, BR-PGTASK-003, BR-PGTASK-004 | As an Assistant, I want to see the exact logical chapter page assigned to me, so that I understand my responsibility. |
| US-ASSISTANT-002 | BR-PGTASK-007, BR-PGTASK-013 | As an Assistant, I want linked page regions to be highlighted in my task view, so that I know exactly which areas need work. |
| US-ASSISTANT-003 | BR-PGTASK-016, BR-PGTASK-017, BR-PGTASK-018, BR-CP-015, BR-CP-022 | As an Assistant, I want to submit completed task output as a new page version, so that my work enters the normal page-version review pipeline. |
| US-ASSISTANT-004 | BR-PGTASK-027, BR-PGTASK-029 | As an Assistant, I want completed and cancelled task records to remain under my account history, so that my contribution history is preserved. |
| US-ASSISTANT-005 | BR-ANN-014, BR-ANN-015 | As an Assistant, I want to view annotations through their linked page regions, so that I understand which feedback applies to the page version used in my assigned work. |
| US-ASSISTANT-006 | BR-NOTIF-009 | As an Assistant, I want to receive an in-app notification when a page task is assigned to me, so that I know what work I need to do. |

---

## 7. Tantou Editor

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-EDITOR-001 | BR-SERIES-003, BR-SERIES-010, BR-SC-005, BR-SC-008 | As a Tantou Editor, I want to view series status, profile information, and active contributors before review, so that I understand the work and responsible team. |
| US-EDITOR-002 | BR-PROP-009, BR-PROP-012, BR-PROP-013, BR-PROP-015, BR-PROP-017, BR-PROP-019, BR-PROP-020 | As a Tantou Editor, I want to review submitted proposal versions directly through the proposal record, so that I can request revision, cancel, or pass the proposal to board review. |
| US-EDITOR-003 | BR-PROP-015, BR-PROP-021 | As a Tantou Editor, I want to attach comments or markup when requesting proposal revision or cancellation, so that the Mangaka receives clear editorial feedback. |
| US-EDITOR-004 | BR-REG-019, BR-REG-020, BR-REG-022, BR-REG-023, BR-REG-026, BR-REG-027, BR-CP-018 | As a Tantou Editor, I want to use saved or AI-assisted page regions for page-level feedback and editorial review, so that review comments are tied to accurate page areas. |
| US-EDITOR-005 | BR-TRANS-001, BR-TRANS-006, BR-TRANS-009 | As a Tantou Editor, I want to review AI/OCR translation suggestions when needed during editorial review, so that translation-related issues can be checked before approval or revision. |
| US-EDITOR-006 | BR-ANN-001, BR-ANN-002, BR-ANN-003, BR-ANN-004 | As a Tantou Editor, I want to create annotations on linked page regions, so that page-specific and whole-page feedback can be recorded without storing direct annotation coordinates. |
| US-EDITOR-007 | BR-ANN-005, BR-ANN-006, BR-ANN-007, BR-ANN-008, BR-ANN-009, BR-ANN-010 | As a Tantou Editor, I want each annotation to include a valid issue type, text, creator, and created time, so that feedback is categorized and understandable. |
| US-EDITOR-008 | BR-ANN-012, BR-ANN-013 | As a Tantou Editor, I want to mark annotations as resolved, so that completed feedback can be distinguished from unresolved feedback. |
| US-EDITOR-009 | BR-CP-018, BR-CH-SUB-002, BR-CH-REV-001, BR-CH-REV-002 | As a Tantou Editor, I want to review submitted chapters page by page while recording the final decision at chapter level, so that detailed feedback and formal approval remain separate. |
| US-EDITOR-010 | BR-CH-REV-006, BR-CH-REV-007, BR-CH-REV-008, BR-CH-REV-010, BR-CH-REV-011, BR-CH-REV-012, BR-CH-REV-013 | As a Tantou Editor, I want to record APPROVED, REVISION_REQUESTED, or CANCELLED decisions with required comments or markup when needed, so that chapter outcomes are clear and enforceable. |
| US-EDITOR-011 | BR-CH-CANCEL-001, BR-CH-CANCEL-002, BR-CH-CANCEL-003, BR-CH-CANCEL-004, BR-CH-REV-006, BR-CH-REV-007, BR-CH-REV-013 | As a Tantou Editor, I want to cancel a chapter through an editorial review decision only when it should not proceed, so that serious rejection decisions are formally recorded and preserved without deleting chapter materials. |
| US-EDITOR-012 | BR-CH-010 | As a Tantou Editor, I want to place a chapter ON_HOLD when there is a valid editorial or operational reason, so that work can pause without being cancelled. |
| US-EDITOR-013 | BR-PGTASK-020, BR-PGTASK-021, BR-PGTASK-022 | As a Tantou Editor, I want to review submitted task page versions when acting as an authorized reviewer, so that task outputs are accepted before completion. |
| US-EDITOR-014 | BR-PUB-002, BR-PUB-003, BR-PUB-004, BR-PUB-005, BR-PUB-006 | As a Tantou Editor, I want to view the series publication frequency and chapter release plan, so that editorial review considers the high-level publishing schedule. |
| US-EDITOR-015 | BR-RANK-005, BR-RANK-008, BR-RANK-012 | As a Tantou Editor, I want to view ranking history and cancellation-risk evidence, so that editorial decisions can consider series performance without automatic cancellation. |

---

## 8. Editorial Board Member

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-BOARD-001 | BR-PROP-013, BR-PROP-014, BR-PROP-016, BR-PROP-021 | As an Editorial Board Member, I want to view proposals under board review with supporting proposal material, so that I can participate in serialization decisions. |
| US-BOARD-002 | BR-BOARD-POLL-001, BR-BOARD-POLL-010, BR-BOARD-VOTE-001 | As an Editorial Board Member, I want to view open board polls, so that I can vote only while a poll is active. |
| US-BOARD-003 | BR-BOARD-VOTE-002, BR-BOARD-VOTE-003, BR-BOARD-VOTE-005 | As an Editorial Board Member, I want to vote APPROVE, REJECT, or ABSTAIN at most once per poll, so that the voting process is fair. |
| US-BOARD-004 | BR-BOARD-VOTE-004 | As an Editorial Board Member, I want to provide a non-empty reason when voting REJECT, so that my objection is clear. |
| US-BOARD-005 | BR-BOARD-VOTE-008, BR-BOARD-VOTE-009, BR-BOARD-VOTE-010, BR-BOARD-VOTE-011 | As an Editorial Board Member, I want my votes to remain visible after a poll is closed or cancelled, so that my participation is traceable but does not directly change status by itself. |
| US-BOARD-006 | BR-BOARD-POLL-006, BR-RANK-005, BR-RANK-008, BR-RANK-012 | As an Editorial Board Member, I want to view ranking and cancellation-risk evidence for relevant series, so that board decisions are supported by performance context. |
| US-BOARD-007 | BR-PUB-002, BR-PUB-003, BR-PUB-004, BR-PUB-006 | As an Editorial Board Member, I want to view the current publication frequency of serialized series, so that I understand the high-level release plan. |
| US-BOARD-008 | BR-NOTIF-011 | As an Editorial Board Member, I want to receive an in-app notification when a new board poll opens, so that I can vote on time. |

---

## 9. Admin

| Story ID | Source Rule(s) | User Story |
|---|---|---|
| US-ADMIN-001 | BR-USER-004, BR-USER-005, BR-USER-006, BR-USER-010, BR-HIST-004 | As an Admin, I want to view, activate, disable, and audit user account actions through current account status, so that account access is controlled and account management remains traceable without a separate registration request table. |
| US-ADMIN-002 | BR-FILE-003, BR-FILE-004, BR-FILE-007 | As an Admin, I want file deletion to happen through the application workflow while historical records retain file references when needed, so that Cloudinary assets, SQL metadata, and past workflow evidence remain consistent. |
| US-ADMIN-003 | BR-SERIES-001, BR-SERIES-002 | As an Admin, I want series codes and URL slugs to remain unique, so that series records can be identified reliably. |
| US-ADMIN-004 | BR-SC-006, BR-SC-007 | As an Admin, I want contributor history to remain available after contributors leave a series, so that past participation can be reviewed and audited. |
| US-ADMIN-005 | BR-PROP-007, BR-PROP-012, BR-PROP-017, BR-PROP-020 | As an Admin, I want proposal versions and review metadata to remain searchable and traceable, so that proposal decisions can be reviewed later. |
| US-ADMIN-006 | BR-BOARD-POLL-001, BR-BOARD-POLL-002, BR-BOARD-POLL-003, BR-BOARD-POLL-004, BR-BOARD-POLL-005, BR-BOARD-POLL-007, BR-BOARD-POLL-008, BR-BOARD-POLL-009 | As an Admin, I want to open valid START_SERIALIZATION or CANCEL_SERIALIZATION board polls with reasons and optional end times, so that board voting starts from a correct workflow state. |
| US-ADMIN-007 | BR-BOARD-POLL-014, BR-BOARD-POLL-015, BR-BOARD-POLL-017 | As an Admin, I want to cancel invalid board polls without deleting votes, so that incorrect poll setups are invalidated but remain traceable. |
| US-ADMIN-008 | BR-BOARD-POLL-016, BR-BOARD-POLL-017, BR-BOARD-RESULT-001, BR-BOARD-RESULT-002, BR-BOARD-RESULT-003, BR-BOARD-RESULT-004, BR-BOARD-RESULT-005, BR-BOARD-RESULT-006, BR-BOARD-RESULT-007, BR-BOARD-RESULT-010 | As an Admin, I want to close a valid poll and view computed vote totals, so that the applicable board result can be determined without a separate decision table. |
| US-ADMIN-009 | BR-BOARD-RESULT-011, BR-BOARD-RESULT-012, BR-BOARD-RESULT-013, BR-BOARD-RESULT-014, BR-BOARD-RESULT-015, BR-BOARD-RESULT-017 | As an Admin, I want applicable closed poll results to update proposal or series status according to the board rules, so that board decisions affect workflow only at the correct time. |
| US-ADMIN-010 | BR-BOARD-POLL-013, BR-BOARD-POLL-015, BR-BOARD-POLL-017, BR-BOARD-RESULT-017 | As an Admin, I want board polls, cancelled polls, votes, and result applications to remain audit-visible, so that board-driven status changes are traceable. |
| US-ADMIN-011 | BR-CH-CANCEL-005 | As an Admin, I want to perform administrative chapter cancellation overrides only when audit-logged, so that exceptional cancellations remain traceable. |
| US-ADMIN-012 | BR-CH-004, BR-CH-012, BR-CH-REV-014, BR-CH-CANCEL-003, BR-CH-CANCEL-005 | As an Admin, I want chapter status changes, editorial decisions, cancellation decisions, and preserved chapter materials to remain traceable, so that important workflow decisions can be reviewed later. |
| US-ADMIN-013 | BR-REG-012, BR-ANN-014, BR-ANN-015, BR-CP-016 | As an Admin, I want page regions to preserve their page-version ownership and annotations to derive page-version context through linked regions, so that review history remains accurate after new uploads. |
| US-ADMIN-014 | BR-PGTASK-027, BR-PGTASK-029, BR-PGTASK-031 | As an Admin, I want page task creation, cancellation, completion, status changes, and preserved old task records to be audit-visible, so that task workflow remains traceable. |
| US-ADMIN-015 | BR-PUB-001, BR-PUB-008, BR-PUB-009, BR-CH-007, BR-CH-008, BR-CH-009 | As an Admin, I want to set planned release dates, schedule approved chapters, and record released timestamps according to Chapter status rules, so that publication planning is complete and delays can be derived. |
| US-ADMIN-016 | BR-VOTE-INPUT-001, BR-VOTE-INPUT-002, BR-VOTE-INPUT-003, BR-VOTE-INPUT-005, BR-VOTE-INPUT-006, BR-VOTE-INPUT-007, BR-VOTE-INPUT-008, BR-VOTE-INPUT-009 | As an Admin, I want to enter one simulated or aggregated reader vote snapshot for a released chapter with valid counts, rating, timestamp, and entered-by user, so that ranking can be demonstrated without a public reader module. |
| US-ADMIN-017 | BR-VOTE-INPUT-004, BR-VOTE-INPUT-009, BR-RANK-008 | As an Admin, I want reader vote input and ranking snapshots to record evidence and entered-by information, so that simulated ranking data can be audited. |
| US-ADMIN-018 | BR-RANK-001, BR-RANK-002, BR-RANK-003, BR-RANK-004, BR-RANK-009, BR-RANK-010, BR-RANK-011 | As an Admin, I want ranking snapshots to be generated and stored by ranking period from reader vote input, so that series performance can be reviewed over time. |
| US-ADMIN-019 | BR-NOTIF-013, BR-HIST-004 | As an Admin, I want important notification-triggering workflow actions to also be audit-logged when required, so that user awareness does not replace traceability. |
| US-ADMIN-020 | BR-HIST-001, BR-HIST-002, BR-HIST-003, BR-HIST-005, BR-HIST-006, BR-HIST-008 | As an Admin, I want domain records and audit logs to serve as workflow evidence instead of separate status-history tables, so that the MVP stays simple while preserving traceability. |

---
