## Goal

Implement SeriesProposal submission and editorial review workflow.

## Source IDs

Business Rules:
- BR-PROP-001 to BR-PROP-023

Functional Requirements:
- FR-PROP-001 to FR-PROP-024

User Stories:
- US-MANGAKA-004
- US-MANGAKA-005
- US-MANGAKA-006
- US-MANGAKA-007
- US-EDITOR-002
- US-EDITOR-003
- US-BOARD-001
- US-SYSADMIN-004

## Description

A SeriesProposal represents one formal submitted proposal version. Submitted snapshot fields should remain locked. Revisions create new proposal versions. Editorial review information is stored directly in SeriesProposal for MVP.

## Acceptance Criteria

- [ ] Proposal version numbers are positive and unique per series.
- [ ] Submitted proposal snapshots are preserved.
- [ ] Proposal file uses FileResource with SERIES_PROPOSAL purpose.
- [ ] Submitted proposal snapshot fields are locked.
- [ ] Revision creates a new proposal version.
- [ ] Editorial review can move proposal to UNDER_BOARD_REVIEW.
- [ ] Board approval is required before proposal becomes APPROVED.

## Out of Scope

- Separate SeriesEditorialReview table.
- Fixed minimum completed pages requirement.
