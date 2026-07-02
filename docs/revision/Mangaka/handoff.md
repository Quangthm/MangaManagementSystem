# Handoff — Mangaka Workspace: manual-save + full API migration (2026-07-01)

> Branch: `feature/workspace-v3` (local). Working tree sạch. File chính:
> `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` + `wwwroot/js/mangaAiCanvas.js`.
> Build: **API 0 / Web 0 errors** — build ra `-o D:/...` vì **ổ C: temp đầy 100%**.
>
> ⚠️ **TRỌNG TÂM SESSION SAU = REVIEW.** Phần migrate workflow **version/file (add-page + save-as-new-version)**
> do **chủ repo tự hoàn thành** (commit `4044f88`) và **CHƯA yên tâm là đúng** → cần rà soát lại.
> Đọc để biết đã làm gì:
> - `83f8e3f` — batch của trợ lý: manual-save + fixes + migrate Chapter/Task/Page/Region/Annotation.
> - `4044f88` — batch của chủ repo: workflow tạo page/version + FileResource + Contributor + bỏ Approve/Unlock.
> - `3949b38` — merge `origin/main` vào branch (đã giải quyết conflict workspace).
> Chi tiết bổ sung: `2026-06-30-manual-save-and-task-cancel.md`; memory `workspace-api-migration-progress`.

---

## 1. Vấn đề đang fix

**A. Hoàn thiện MANUAL-SAVE** (autosave → chỉ ghi khi bấm Save): defer tạo chapter/upload page/xóa page (buffer RAM), Save mới flush; Discard hoàn tác; gating "Save now?"; badge/indicator "Unsaved".

**B. Loạt bug/UX:** xóa task hard-delete (sai BR) + không báo; translate đè bản gốc v1; pin (0,0) ma + `created_by_user_id` NULL; click task/annotation không highlight panel; ảnh trắng khi đổi trang; "Please upload" hiện sai; cắt trang đôi; chip Panel Target tràn; giá tiền task; note theo page.

**C. Bám sát kiến trúc "Web gọi API"** — migrate workspace từ gọi Application service trực tiếp (transitional debt) → **typed API client → controller → MediatR/service → SP/EF**. → **Đã migrate GẦN HẾT; phần version/file mới làm cần REVIEW.**

---

## 2. Nguyên nhân đã tìm ra

- **Translate đè v1:** `callTranslateAPI` (JS) ghi `translatedText` → `syncToBlazor()` → buffer version C# → Save/flush ghi chữ dịch xuống v1. Fix: cờ `translationPreview` strip translatedText khi sync (chỉ `Create new version`/`exportRegions` giữ).
- **Pin (0,0) ma:** `CreateAnnotation`/`CreateTask` tự đẻ region `(0,0, 0.01)` khi không chọn target → reload thành pin. Fix: bắt buộc chọn panel/pin.
- **created_by_user_id NULL:** `CreatePageRegionDto` thiếu `CreatedByUserId`; service + BulkReplace không set. Fix: thêm field + set từ actor.
- **Ảnh trắng intermittent:** `loadImage` tính `scale` từ `container.clientWidth/Height`=0 lúc điều hướng nhanh → scale 0. Fix: `fitImageOntoCanvas` retry rAF.
- **"Please upload" sai:** hiện ở `OnAfterRenderAsync(firstRender)` chạy trước khi pages load. Fix: chuyển sang `SelectChapter` (khi chắc chắn 0 page).
- **Xóa task sai BR:** hard-delete → phải Cancel (BR-PGTASK-015/027/029/031) qua `usp_ChapterPageTask_Cancel`.
- **Cancel chapter mất audit qua EF:** `AuditableEntityInterceptor` chỉ set `UpdatedAtUtc` (KHÔNG ghi `AuditEvent`) → repo phải ghi audit CHAPTER_CANCELLED thủ công.
- **Kiến trúc:** workspace inject ~12 Application service trực tiếp; nhiều domain chưa có API endpoint → phải **dựng mới** controller/client/handler.

---

## 3. Files đã đọc / thay đổi

### Migration đã hoàn thành (theo domain → API client)
| Domain | Client | Controller | Ghi chú |
|---|---|---|---|
| Chapter | `IMangakaChapterApiClient` | `MangakaChaptersController` | +CancelChapterSubmission, +CancelChapter (CQRS command/handler + EF + audit) |
| Task | `IMangakaTaskApiClient` | `MangakaTaskController` | +CreateTask, +by-page; cancel/get |
| Page | `IMangakaPageApiClient` | `MangakaPageController` | list/by-id/counts/notes/delete **+ create-with-file, versions/* (do chủ repo thêm — CẦN REVIEW)** |
| PageRegion | `IMangakaPageRegionApiClient` | `MangakaPageRegionController` | create/bulk-replace/by-versions/counts |
| Annotation | `IMangakaAnnotationApiClient` | `MangakaAnnotationController` | create/resolve/by-page |
| Contributor | `IMangakaSeriesContributorApiClient` | (sẵn có) | Assistant list (do chủ repo migrate) |
| Series header | `ISeriesApiClient` | (sẵn có) | GetSeriesDetailAsync |

### File chính đã đổi
- **Web:** `CreatorWorkspace.razor` (toàn bộ workspace giờ gọi API client, đã gỡ inject Chapter/Task/Annotation/Region/Page/Version/File/Contributor service), `WorkspaceChapterSidebar.razor`, `wwwroot/js/mangaAiCanvas.js` (translationPreview, selectRegionsByDbIds, fitImageOntoCanvas), `Program.cs` (đăng ký các API client), `Services/Api/*` (các `IMangaka*ApiClient` + impl; **`MangakaPageApiClient` giờ gánh cả version/file — điểm cần review**).
- **API:** `Controllers/Mangaka/{MangakaChaptersController, MangakaTaskController, MangakaPageController(+version/file endpoints), MangakaPageRegionController(MỚI), MangakaAnnotationController(MỚI)}`.
- **Application/Infra:** `IMangakaChapterRepository`+`MangakaChapterRepository` (cancel/cancel-submission), `Features/Mangaka/Chapters/Commands/{CancelChapter,CancelChapterSubmission}`, DTOs (`PageRegionDtos`, `ChapterPageDtos`, `ChapterPageTaskDtos`, `ChapterPageAnnotationDtos` + các request DTO create-with-file/version — **do chủ repo thêm**), `PageRegionService`/`ChapterPageTaskService` (bỏ hard-delete).

### Đã đọc (chính)
`docs/business-rules.md` (BR-PGTASK/ANN/CH), `MangaManagementSystem_Schema.sql`, `*_Procedures_Views_Bootstrap.sql` (usp_ChapterPageTask_Cancel), `AuditableEntityInterceptor.cs`, `ImageCropDialog.razor`, các controller/client/CQRS mẫu.

### Data cleanup (chưa chạy)
`scratchpad/cleanup-phantom-zero-regions.sql` — dọn region/pin (0,0) ma (preview + transaction ROLLBACK mặc định).

---

## 4. Bước tiếp theo cần làm

1. **⚠️ REVIEW migration version/file (ưu tiên #1)** — đọc diff commit `4044f88`. Kiểm tra:
   - Endpoint atomic `pages/create-with-file` và `versions/create-with-file`: có ĐÚNG "upload Cloudinary ở Web TRƯỚC, rồi link atomically" không; **best-effort cleanup Cloudinary khi DB fail** có đủ không.
   - `MangakaPageApiClient` gánh cả version/file — cân nhắc **tách `MangakaPageVersionController`/client riêng** cho gọn (page vs version là 2 domain).
   - DTO request/response mới (CreatePageWithVersion*, CreateVersionWithFileAndRegions*) có khớp DB/BR (sha256, file_purpose_code, set-current) không.
   - So sánh hành vi với service cũ (edit-bleed fix, delete-version guard task/annotation, set-current).
2. **AiService** (segment/translate) vẫn gọi thẳng Python 8000 — advisory, migrate optional.
3. **Dọn C: (đầy 100%)**; build ra D:.
4. Chạy `cleanup-phantom-zero-regions.sql` (preview trước, đổi ROLLBACK→COMMIT).
5. Auth hardening (memory `auth-session-hardening-followup`).

---

## 5. Những gì CHƯA kiểm tra

- **TẤT CẢ chỉ verify bằng build (0 errors) — CHƯA smoke test runtime.** Cần **rebuild + restart API (5234) + Web (5244) + hard-refresh** trình duyệt (+ `uvicorn` 8000 cho AI).
- **Migration version/file (commit 4044f88) là phần chủ repo tự làm & CHƯA yên tâm** → chưa test: tạo page (upload → Cloudinary → create-with-file), Save-as-new-version, Upload version, delete-version-image + cleanup Cloudinary, set-current — tất cả qua API thật.
- Chưa test runtime toàn bộ luồng: manual-save (buffer/Save/Discard/gating), buffered page-delete + guard, translate→Create giữ v1 gốc, pin (0,0) không sinh mới, Panel #N + click-select, cắt trang đôi, ảnh không trắng, compensation biên, Page Note, cancel chapter ghi audit.
- **Chưa dọn data:** pin (0,0) ma cũ + `created_by_user_id` NULL cũ vẫn còn trong DB (fix chỉ chặn cái mới).
- Build per-project ra D: (C: đầy, `.slnx` không build CLI được) — chưa build/chạy qua Visual Studio.
