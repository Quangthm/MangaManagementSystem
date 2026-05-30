## Goal

Implement ChapterPageAnnotation, ChapterPageTask, and task-region links.

## Source IDs

Business Rules:
- BR-ANN-001 to BR-ANN-016
- BR-PGTASK-001 to BR-PGTASK-031

Functional Requirements:
- FR-ANN-001 to FR-ANN-017
- FR-PGTASK-001 to FR-PGTASK-034

User Stories:
- US-PAGE-004
- US-MANGAKA-014 to US-MANGAKA-017
- US-ASSISTANT-001 to US-ASSISTANT-006
- US-EDITOR-006 to US-EDITOR-008
- US-EDITOR-013
- US-SYSADMIN-007
- US-SYSADMIN-008

## Description

Annotations reference PageRegion instead of storing direct coordinates. Page tasks target a logical ChapterPage and can link to one or more PageRegion records through ChapterPageTaskRegion.

## Acceptance Criteria

- [ ] Each annotation references exactly one PageRegion.
- [ ] Annotation page version is derived from linked PageRegion.
- [ ] Whole-page feedback uses a full-page manual PageRegion.
- [ ] Annotation issue type uses fixed MVP values.
- [ ] Page task targets one logical ChapterPage.
- [ ] Page task can link to one or more PageRegion records.
- [ ] Duplicate task-region pairs are prevented.
- [ ] Assistant submits completed task output as a new page version.
- [ ] Assistant cannot approve own task output.
- [ ] Task creation/cancellation/completion/status changes are audit-log ready.

## Out of Scope

- Full drawing editor.
- Complex panel editing tools.
