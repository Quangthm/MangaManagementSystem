## Goal

Implement Chapter, ChapterPage, and ChapterPageVersion workflow.

## Source IDs

Business Rules:
- BR-CH-001 to BR-CH-012
- BR-CP-001 to BR-CP-022
- BR-CH-SUB-001 to BR-CH-SUB-006
- BR-CH-REV-001 to BR-CH-REV-014
- BR-CH-CANCEL-001 to BR-CH-CANCEL-005
- BR-PUB-001 to BR-PUB-009

Functional Requirements:
- FR-CH-001 to FR-CH-012
- FR-CP-001 to FR-CP-022
- FR-CH-SUB-001 to FR-CH-SUB-006
- FR-CH-REV-001 to FR-CH-REV-015
- FR-CH-CANCEL-001 to FR-CH-CANCEL-005
- FR-PUB-001 to FR-PUB-009

User Stories:
- US-MANGAKA-008 to US-MANGAKA-011
- US-MANGAKA-018 to US-MANGAKA-021
- US-EDITOR-009 to US-EDITOR-012
- US-ADMIN-008
- US-ADMIN-009
- US-SYSADMIN-006

## Description

Chapters contain logical pages. ChapterPage is a stable logical slot, and ChapterPageVersion stores uploaded media versions. Chapter submission is represented by Chapter.status_code = UNDER_REVIEW, not a separate ChapterSubmission table.

## Acceptance Criteria

- [ ] Chapter number labels are unique per series.
- [ ] ChapterPage page numbers are unique per chapter.
- [ ] Page uploads create ChapterPageVersion records.
- [ ] Only one page version is current at a time.
- [ ] Old page versions are preserved.
- [ ] Chapter submission changes status to UNDER_REVIEW.
- [ ] Chapter pages lock during review.
- [ ] Revision requested allows editing again.
- [ ] Cancelled chapter cannot be scheduled or released.
- [ ] Publication delay is derived from planned release date.

## Out of Scope

- Separate ChapterSubmission table.
- Generated chapter-level PDF submission file.
