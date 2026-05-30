## Goal

Implement FileResource and Cloudinary metadata handling.

## Source IDs

Business Rules:
- BR-FILE-001
- BR-FILE-002
- BR-FILE-003
- BR-FILE-004
- BR-FILE-005
- BR-FILE-006
- BR-FILE-007

Functional Requirements:
- FR-FILE-001
- FR-FILE-002
- FR-FILE-003
- FR-FILE-004
- FR-FILE-005
- FR-FILE-006
- FR-FILE-007
- FR-FILE-008
- FR-FILE-009
- FR-FILE-010

User Stories:
- US-USER-001
- US-USER-002
- US-NEW-002

## Description

Actual media files should be stored in Cloudinary, while SQL Server stores metadata and references through FileResource. Business tables should reference file_resource_id instead of raw Cloudinary URLs.

## Acceptance Criteria

- [ ] FileResource metadata model exists.
- [ ] Business records reference FileResource instead of raw Cloudinary fields.
- [ ] Deleted files are excluded from normal queries.
- [ ] Historical/audit views may still reference inactive files.
- [ ] Avatar and portfolio files use FileResource.
- [ ] UI can display safe placeholder for unavailable/deleted files.

## Out of Scope

- Full file versioning beyond page versions.
