# Handoff — Mangaka Workspace: manual-save completion + API migration (2026-07-01)

> Branch: `feature/workspace-v3` (local, chưa push). File chính: `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` + `wwwroot/js/mangaAiCanvas.js`.
> Build: **API 0 errors / Web 0 errors** (build ra `-o D:/...` vì **ổ C: temp đầy 100%**). Mọi thay đổi mới **CHƯA smoke test runtime** — cần rebuild + restart **cả API (5234) lẫn Web (5244)**, và **hard-refresh** trình duyệt (có sửa JS).
> Chi tiết bổ sung: `2026-06-30-manual-save-and-task-cancel.md`; tiến độ migration trong memory `workspace-api-migration-progress`.

---

## 1. Vấn đề đang fix (session này)

**A. Hoàn thiện mô hình MANUAL-SAVE cho workspace** (trước đó autosave → đổi sang chỉ ghi khi bấm Save):
- Defer tạo chapter + upload page (buffer RAM), Save mới flush; Discard hoàn tác; gating "Save now?" cho task/annotation/note/submit/version; badge "Unsaved"; per-page indicator.
- Xóa page cũng buffered (Save mới commit soft-delete; Discard khôi phục).

**B. Sửa loạt bug/UX người dùng báo:**
- Xóa task đang hard-delete (sai BR) + không có thông báo.
- Translate xong tạo version mới nhưng **bản dịch đè lên bản gốc v1**.
- Annotation/task hiển thị tọa độ thô + có "Pin at (0,0)" ma; `PageRegion.created_by_user_id` NULL.
- Click task/annotation không highlight panel trên trang.
- Thỉnh thoảng **ảnh không load** (canvas trắng) khi đổi trang.
- Thông báo "Please upload an image" hiện cả khi chapter đã có page.
- Cần: cắt trang đôi khi upload; chip Panel Target tràn che nút Create; đặt giá tiền task; note theo page.

**C. Bám sát kiến trúc "Web gọi API"** — migrate workspace từ gọi Application service trực tiếp (transitional debt) → **typed API client → controller → MediatR/service → SP/EF**.

---

## 2. Nguyên nhân đã tìm ra

- **Translate đè v1:** `callTranslateAPI` (JS) ghi `translatedText` vào regions rồi `syncToBlazor()` → đẩy vào buffer version C# → **Save/flush ghi chữ dịch xuống v1**. (`Create new version` đọc `exportRegions` riêng nên v2 đúng → lỗi "lúc bị lúc không".)
- **Pin (0,0) ma:** `CreateAnnotation`/`CreateTask` tự đẻ region `(0,0, 0.01×0.01)` khi không chọn panel/pin → reload nhận diện là pin.
- **created_by_user_id NULL:** `CreatePageRegionDto` thiếu field `CreatedByUserId`; service create + `BulkReplace` không set.
- **Ảnh trắng intermittent:** `loadImage` tính `scale` từ `container.clientWidth/Height` = 0 lúc điều hướng nhanh (layout chưa settle) → scale 0 → vẽ vô hình; không có retry.
- **"Please upload" sai:** hiện ở `OnAfterRenderAsync(firstRender)` — chạy **trước khi** `SelectChapter` load xong pages → `UploadedPages` rỗng tạm thời.
- **Xóa task sai:** `DeleteTask` gọi `DeleteChapterPageTaskAsync` (hard-delete), vi phạm **BR-PGTASK-015/027/029/031** (phải Cancel, giữ row + audit). SP `usp_ChapterPageTask_Cancel` đã có sẵn.
- **Cancel chapter mất audit khi qua EF:** `AuditableEntityInterceptor` chỉ set `UpdatedAtUtc`, KHÔNG ghi `AuditEvent` rows → path mới phải ghi audit thủ công.
- **Kiến trúc:** workspace inject ~12 Application service trực tiếp (chỉ `SeriesApiClient` đúng chuẩn). Nhiều domain **chưa có API endpoint** → migrate = **dựng mới** controller/client/handler, không phải swap.

---

## 3. Files đã đọc / thay đổi

**Đã thay đổi (Web):**
- `Components/Pages/Mangaka/CreatorWorkspace.razor` — trung tâm: manual-save (buffer/Flush/gating/Discard), task Cancel, buffered page-delete + guard, Page Note, compensation validate, Panel #N (task+annotation), click-to-select panel, upload chooser + split-double-page (dùng `ImageCropDialog`), gỡ unsaved-draft/Restore, sửa "Please upload", **migrate call sang MangakaChapterApi/MangakaTaskApi/MangakaPageApi**.
- `Components/Pages/Mangaka/WorkspaceChapterSidebar.razor` — badge "Unsaved" + layout dọc căn trái.
- `wwwroot/js/mangaAiCanvas.js` — cờ `translationPreview` (strip translatedText khi sync), `selectRegionsByDbIds`, `fitImageOntoCanvas` (retry rAF khi container=0), gỡ `saveMmsDraft/getMmsDraft/clearMmsDraft`.
- `Program.cs` — đăng ký `IMangakaPageApiClient`.
- `Services/Api/` — sửa `IMangakaChapterApiClient`/impl (+CancelChapterSubmission, +CancelChapter), `IMangakaTaskApiClient`/impl (+CreateTask); **MỚI** `IMangakaPageApiClient` + `MangakaPageApiClient`.

**Đã thay đổi (API):**
- `Controllers/Mangaka/MangakaChaptersController.cs` — +action cancel-submission, +cancel.
- `Controllers/Mangaka/MangakaTaskController.cs` — +action create task (notify NEW_TASK_ASSIGNED).
- **MỚI** `Controllers/Mangaka/MangakaPageController.cs` — list/by-id/counts/notes/delete.

**Đã thay đổi (Application/Infrastructure):**
- `Interfaces/IMangakaChapterRepository.cs` + `Infrastructure/Repositories/MangakaChapterRepository.cs` — `CancelChapterSubmissionAsync`, `CancelChapterAsync` (EF + guard contributor + audit CHAPTER_CANCELLED).
- **MỚI** `Features/Mangaka/Chapters/Commands/{CancelChapterSubmission,CancelChapter}/` (Command + Handler).
- DTOs: `PageRegionDtos.cs` (+CreatedByUserId), `ChapterPageDtos.cs` (+UpdatePageNotesRequest/PageCountsRequest), `ChapterPageTaskDtos.cs` (+CreateMangakaTaskRequest).
- `Services/PageRegionService.cs` (set CreatedByUserId), `Services/ChapterPageTaskService.cs` + `Interfaces/IChapterPageTaskService.cs` (bỏ hard-delete `DeleteChapterPageTaskAsync`).

**Đã đọc (chính):** `docs/business-rules.md` (BR-PGTASK, BR-ANN, BR-CH), `MangaManagementSystem_Schema.sql` (task/region/chapter CHECK + audit.AuditEvent), `MangaManagementSystem_Procedures_Views_Bootstrap.sql` (usp_ChapterPageTask_Cancel), `Program.cs` (auth cookie), `ImageCropDialog.razor`, các API client/controller/CQRS chapter mẫu, `AuditableEntityInterceptor.cs`.

**Data cleanup (chưa chạy):** `scratchpad/cleanup-phantom-zero-regions.sql` — dọn region/pin (0,0) ma trong DB (preview + transaction ROLLBACK mặc định).

---

## 4. Bước tiếp theo cần làm

1. **Migration còn lại (theo template 6 tầng, build xanh ra D:):**
   - ~~**Workflow entangle "add page" + "save as new version"** (KHÓ NHẤT)~~: **ĐÃ HOÀN THÀNH** (đã dựng endpoint atomic `create-with-file` và `versions/create-with-file` trong `MangakaPageController`, migrate toàn bộ `FlushPendingAsync`, `HandleUploadVersion`, manual save, `DeleteVersionImage`, `SetCurrentVersion` sang `IMangakaPageApiClient`, tự động cleanup Cloudinary khi DB lỗi).
   - ~~Approve/Unlock chapter~~: **ĐÃ HOÀN THÀNH** (đã loại bỏ khỏi workspace Mangaka do là chức năng Editor-gated).
   - ~~SeriesContributorService & UserService (cho Assistant list)~~: **ĐÃ HOÀN THÀNH** (đã migrate sang `IMangakaSeriesContributorApiClient`).
   - **Tiếp theo cần làm:**
   - ~~**PageRegion** (EnsureRegionsSaved/BulkReplace/GetRegionCounts/GetByVersionsAsync)~~: **ĐÃ HOÀN THÀNH** (toàn bộ `CreatorWorkspace.razor` đã sử dụng `IMangakaPageRegionApiClient`).
   - ~~**Annotation** (create/resolve/get-by-page) + **Task list-by-page**~~: **ĐÃ HOÀN THÀNH** (`LoadPage`, `CreateTask`, `CancelTask`, `CreateAnnotation`, `ResolveAnnotation` trong `CreatorWorkspace.razor` và `TaskWorkspaceRedirect.razor` đã hoàn toàn sử dụng `IMangakaTaskApiClient` & `IMangakaAnnotationApiClient`).
   - **Tiếp theo cần làm:**
     - **AiService** (segment/translate — advisory; hiện tại `IAiService` đang gọi thẳng HTTP sang Python AI server 8000).
2. **Dọn C: temp (đầy 100%)** để build/chạy được.
3. Chạy `cleanup-phantom-zero-regions.sql` (PART 1 preview trước, đổi ROLLBACK→COMMIT) để dọn pin (0,0) cũ.
4. Cân nhắc: auth hardening (SecurePolicy Always ở prod, sliding 7 ngày, revoke theo tài khoản) — xem memory `auth-session-hardening-followup`.

---

## 5. Những gì CHƯA kiểm tra

- **TẤT CẢ chỉ verify bằng build (0 errors), CHƯA smoke test runtime.** Cần **rebuild + restart API + Web + hard-refresh** trình duyệt.
- **Migration cần API chạy ở 5234** (Web gọi HTTP). Chưa test: submit/create/list/rename/cancel/cancel-submission chapter, create/cancel/get task, list/counts/note/delete page — tất cả **qua API thật** (có thể lộ khác biệt validation/lọc của handler so với service cũ).
- Chưa test runtime: manual-save (buffer chapter/page, Save/Discard, gating), buffered page-delete + guard task/annotation, translate→Create v1 giữ gốc (fix translationPreview), pin (0,0) không sinh mới, Panel #N + click-select, cắt trang đôi, ảnh không còn trắng, compensation biên, Page Note lưu DB, cancel chapter ghi audit CHAPTER_CANCELLED.
- **Chưa dọn data:** pin (0,0) ma cũ + region created_by_user_id NULL cũ vẫn còn trong DB (fix chỉ chặn cái mới).
- Build qua **per-project ra D:** (C: đầy); chưa build qua Visual Studio / `.slnx`.
