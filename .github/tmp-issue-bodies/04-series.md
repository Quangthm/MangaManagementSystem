## Goal

Implement Series and SeriesContributor foundation.

## Source IDs

Business Rules:
- BR-SERIES-001 to BR-SERIES-014
- BR-SC-001 to BR-SC-008

Functional Requirements:
- FR-SERIES-001 to FR-SERIES-016
- FR-SC-001 to FR-SC-009

User Stories:
- US-MANGAKA-001
- US-MANGAKA-002
- US-MANGAKA-003
- US-EDITOR-001
- US-SYSADMIN-003

## Description

The system should support series profiles, unique series codes/slugs, content language, cover image reference, source-series reference, and contributor membership through SeriesContributor.

## Acceptance Criteria

- [ ] Series code is unique.
- [ ] Series slug is unique.
- [ ] Series has one lifecycle status.
- [ ] Series ownership/team membership uses SeriesContributor.
- [ ] Contributor history is preserved.
- [ ] A series cannot reference itself as source series.
- [ ] Editable series metadata records update time and updater.

## Out of Scope

- Normalized genre/tag system.
- Contributor specialization table.
