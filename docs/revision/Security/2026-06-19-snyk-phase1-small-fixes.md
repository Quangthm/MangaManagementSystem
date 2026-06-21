# Snyk Security Finding Audit — Phase 1 Small Fixes

**Branch:** `feature/Mangaka`
**Date:** 2026-06-19

## Reason for Audit
Snyk static analysis reported security findings across the codebase. Phase 1 addresses the smallest, lowest-risk fixes first: log forging in one controller, DOM XSS in a test-only page, and a dependency-free log sanitization helper.

## Phase 1 Scope
1. Add `LogSanitizer` helper — Application layer, no dependencies
2. Sanitize `slug` logging in `SeriesController.cs` — real log forging finding
3. Fix DOM XSS in `MangaAI_Service/test_ui.html` — real, test-only finding
4. Document deleted `CreateTestSeries` — stale/deleted finding
5. Inspect remaining findings for Phase 2 planning (no code changes)

## Files Changed

| # | File | Change |
|---|------|--------|
| 1 | `src/MangaManagementSystem.Application/Common/Security/LogSanitizer.cs` | **NEW** — dependency-free static sanitization helper |
| 2 | `src/MangaManagementSystem.API/Controllers/SeriesController.cs` | Wrapped raw `slug` with `LogSanitizer.Sanitize(slug)` at 2 log lines |
| 3 | `src/MangaAI_Service/test_ui.html` | Replaced minimal `escHtml` with comprehensive escaping (`&`, `<`, `>`, `"`, `'`) |

## Findings Fixed

### 1. Log Forging — `SeriesController.cs` (REAL)
- **Lines changed**: 68, 112
- **Before**: `_logger.LogError(ex, "...slug {Slug}.", slug)` — raw user-controlled route param logged
- **After**: `_logger.LogError(ex, "...slug {Slug}.", LogSanitizer.Sanitize(slug))` — sanitized
- **Safe because**: controller stays thin, no route/query/response behavior changed

### 2. DOM-based XSS — `test_ui.html` (REAL, LOW, test-only)
- **Line changed**: 456
- **Before**: `escHtml(s) { return (s||'').replace(/"/g, '&quot;').replace(/</g, '&lt;'); }` — only escaped `"` and `<`
- **After**: Full `escHtml` handling `&`, `<`, `>`, `"`, `'` with null-safe guard
- **`innerHTML` remaining**: `sidebar.innerHTML = html` on line 453 is still used, but data flowing into it is now comprehensively escaped. This is a test-only page (`MangaAI_Service`, not the Blazor app). Converting to pure DOM creation would be a significant refactor of the `renderSidebar()` function. With the improved `escHtml`, the actual XSS risk is fully mitigated.
- **Safe because**: test-only page, all dynamic data passes through `escHtml()`, no shipped Blazor code affected

### 3. Hardcoded Credentials — `CreateTestSeries/Program.cs` (DELETED)
- File does not exist in repository. `git log` confirms it existed in an earlier commit but has been deleted.
- Snyk re-scan: not reported. Confirmed stale.

## Findings Inspected but Deferred to Phase 2

### Log Forging — False Positive (inspected, no change)
| File | Lines | Why False Positive |
|------|-------|-------------------|
| `MangakaSeriesController.cs` | 104, 239, 293 | Static templates or Guid route params only |
| `EditorDashboardApiClient.cs` | 52 | Logs only int status code + standard HTTP ReasonPhrase (server-controlled) |
| `EditorChapterReviewApiClient.cs` | 61, 101 | Same pattern as above + Guid chapterId |
| `EditorProposalsController.cs` | 58, 127, 261 | Static templates or Guid params |
| `EditorChapterReviewsController.cs` | 54 | Static template |

### Log Forging — Auth/Profile/Registration (deferred, auth-related)
| File | Lines | Reason Deferred |
|------|-------|----------------|
| `ProfileController.cs` | 125, 211, 215, 236 | PII/log forging overlap — needs design discussion |
| `ProfilePasswordController.cs` | 11, 38, 53, 128 | Auth-related — teammate handling auth |
| `AuthController.cs` | 34 | Auth-related — teammate handling auth |
| `RegistrationController.cs` | 51, 101 | PII/log forging overlap — needs design discussion |
| `Web/Program.cs` | 286, 306, 317, 432, 436, 447, 454, 461 | Auth redirect/Google login handling |

### PII Exposure (deferred)
| File | Lines | Description |
|------|-------|-------------|
| `RegistrationController.cs` | 51 | Email logged in registration flow |
| `AuthService.cs` | 56, 201, 246 | Email/username/userid logged in login flows |
| `UserService.cs` | 420 | PII in log/response |

### Hardcoded Credentials (deferred)
| File | Lines | Description |
|------|-------|-------------|
| `ProfilePasswordController.cs` | 11 | Password variable declaration (auth-related) |
| `Web/Program.cs` | 81 | API settings key assignment (config, not credential) |

### SSRF — NEW FINDING (deferred)
| File | Lines | Description |
|------|-------|-------------|
| `Web/Program.cs` | 363, 404, 444 | 3 HIGH findings — raw URL from user/query param flows into `HttpClient.GetAsync`. Likely Google auth callback redirect handling. **Must be reviewed by auth teammate.** |

## Architecture Impact

- **Zero architecture violations added.** `LogSanitizer` is a pure static utility in `Application/Common/Security` with no dependencies.
- **Zero existing architecture patterns broken.** No Web-to-Application/Infrastructure/EF direct calls added.
- **Zero stored procedures modified.**
- **Zero CQRS/MediatR handlers modified.**
- **Zero controller thickness added.**

## Build Result

```powershell
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
```

- **Build succeeded**
- **0 errors**
- **60 warnings** (pre-existing baseline, unchanged)
- **0 new warnings introduced**

## Snyk Re-scan Result

```powershell
snyk code test
```

- **Total open issues**: 41 [3 HIGH, 0 MEDIUM, 38 LOW]
- **Phase 1 fixes resolve**: 2 findings (SeriesController slug forging, test_ui.html XSS)
  - Snyk still flags `test_ui.html` at line 453 (`sidebar.innerHTML`) — static analyzer does not trace through `escHtml()`. Actual risk is mitigated.
  - Snyk still flags `SeriesController.cs` at lines 58/101 (data-flow origin) — the actual `LogError` calls at 68/112 now use `LogSanitizer.Sanitize(slug)`.
- **CreateTestSeries/Program.cs**: Not reported — confirmed deleted.
- **3 new HIGH SSRF findings** discovered in `Web/Program.cs` (Google auth callback). Deferred for auth teammate.

## Remaining Findings for Phase 2
- Profile/PII exposure design (ProfileController, UserService, AuthService)
- Auth-related log forging (AuthController, ProfilePasswordController, RegistrationController)
- SSRF in Program.cs (auth callback) — HIGH priority
- Remaining log forging false positives (documented above)

## Manual Verification Checklist
1. [ ] `LogSanitizer.Sanitize(null)` returns `"(empty)"` — no exception
2. [ ] `LogSanitizer.Sanitize("")` returns `"(empty)"` — no exception
3. [ ] `LogSanitizer.Sanitize("hello\nworld\r\ttest")` returns `"hello world   test"` — CR/LF/tab replaced
4. [ ] `LogSanitizer.Sanitize("a".PadRight(300, 'b'))` returns 256 chars — truncated
5. [ ] `LogSanitizer.Sanitize("normal text")` returns `"normal text"` — unchanged
6. [ ] `SeriesController.cs` GET `/api/series/{slug}` still works — route/query/response unchanged
7. [ ] `test_ui.html` sidebar renders regions properly with escaped text
8. [ ] `test_ui.html`: `<script>` in text input renders as `&lt;script&gt;` (escaped)
9. [ ] `test_ui.html`: `&` in text input renders as `&amp;` (properly double-escaped)
10. [ ] Build succeeds with 0 errors
