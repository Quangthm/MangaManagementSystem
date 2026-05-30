## Goal

Implement PageRegion and saved AI-assisted segmentation workflow.

## Source IDs

Business Rules:
- BR-REG-001 to BR-REG-027
- BR-TRANS-001 to BR-TRANS-009

Functional Requirements:
- FR-REG-001 to FR-REG-029
- FR-TRANS-001 to FR-TRANS-009

User Stories:
- US-PAGE-001
- US-PAGE-002
- US-PAGE-003
- US-MANGAKA-012
- US-MANGAKA-013
- US-EDITOR-004
- US-EDITOR-005

## Description

PageRegion stores saved regions for a specific ChapterPageVersion. Existing saved regions should load when the segmentation tool opens. AI-assisted segmentation may run again even when saved regions exist. Newly detected regions are temporary suggestions until saved.

## Acceptance Criteria

- [ ] PageRegion belongs to one ChapterPageVersion.
- [ ] Region type is restricted to valid type codes.
- [ ] Region source is AI or MANUAL.
- [ ] Manual regions do not keep AI confidence score.
- [ ] Saved regions load when opening segmentation tool.
- [ ] AI segmentation can run even when saved regions already exist.
- [ ] Newly detected AI regions are compared against existing saved regions.
- [ ] Duplicate overlapping regions are prevented or warned against.
- [ ] Non-duplicate regions can be saved.
- [ ] Updated time and updater are recorded when saved region is modified.

## Out of Scope

- Persistent AI job history table.
- Automatic page/chapter approval from AI.
- RegionTranslation table.
