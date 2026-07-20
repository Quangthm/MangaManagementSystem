# Complete Series active-task cascade

Date: 2026-07-19

## Behavior

- The existing completion-impact response now includes the count of distinct active production tasks under chapters that completion would cancel.
- Completing a series cancels distinct `ASSIGNED` and `UNDER_REVIEW` tasks under affected chapters before the shared transaction commits.
- `COMPLETED` and `CANCELLED` tasks, and tasks under unaffected chapters such as `RELEASED` chapters, remain unchanged.
- Each cancelled task is preserved and receives one `CHAPTER_PAGE_TASK_CANCELLED` audit event.

## Architecture and transaction

- `CompleteSeriesCommandHandler` remains the Application workflow and transaction owner.
- Chapter cancellation statuses are shared through `SeriesLifecycleSupport`; task cancellability is defined by the pure Domain `ChapterPageTaskLifecyclePolicy`.
- Infrastructure exposes a semantic series-for-update operation and implements it with an exact-row SQL Server lifecycle lock. The handler begins the UnitOfWork transaction before invoking it and recalculates chapter/task impact only after the lock is acquired.
- Task loading is rooted at `ChapterPageTask`, and Application defensively deduplicates by task ID before counting, mutation, and audit generation.
- Task, chapter, series, and audit changes use one `SaveChangesAsync` and one commit. Failures, including a transaction selected as a SQL Server deadlock victim, roll back the entire cascade.

## Concurrency note

Concurrent Create Task and Complete Series operations may wait or may deadlock because their broader lock acquisition order differs. Task-creation locking was intentionally not redesigned. A manual concurrency smoke test should cover both operation orderings and verify that a deadlock victim rolls back without an invalid partial state.

## Verification boundary

Only static verification was authorized: changed-file inspection, call-site and raw-status searches, protected-file checks, and `git diff --check`. No restore, build, test, server, migration, or database command was run.
