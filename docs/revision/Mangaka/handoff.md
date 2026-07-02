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
### Feature work sau merge (đã build OK, CHƯA smoke test runtime)
- **Versions panel** (Left+Right pane): thêm đóng/mở (mặc định đóng) để tiết kiệm canvas — `_versionsCollapsedLeft/Right`.
- **Split view đổi chapter:** `SelectChapter` giờ gọi `OnSplitPageChanged(1)` để render lại canvas Right (trước chỉ Left đổi).
- **Draft warning:** dịch message sang tiếng Anh ("Unsaved draft detected...", action "Restore").
- **Assign to Assistant:** lọc dropdown chỉ còn `RoleName == "Assistant"` (trước hiện cả Editor/Mangaka contributor).
- **Translate Selected:** thành menu chọn **VI/EN** (`target_lang` xuyên suốt UI→JS→`TranslateRequestDto`→AiService→Python).
- **Chất lượng dịch (AI service `main.py`):** OCR vùng đầy đủ (không co 3px), helper `translate_text` (ép `source='ja'`, fallback auto, giữ gốc nếu fail), helper `translate_texts` **gom cả trang dịch 1 request** (có ngữ cảnh, fallback per-bubble nếu lệch dòng). Áp cho cả `translate-selected` và `clean-and-translate`. **Cần restart uvicorn.**
- **Task Panel layout (laptop):** CSS `.workspace-tabs` cho MudTabs lấp đầy chiều cao → vùng ACTIVE TASKS thành khu cuộn thật (`.mf-active-tasks`, min 140px); form NEW TASK thu gọn được (`_newTaskFormCollapsed`).

## ⚠️ ĐIỀU QUAN TRỌNG NHẤT
Mọi sửa đổi là **CODE đã build OK (0 errors)** nhưng app đang chạy của user vẫn là **bản build cũ**
(mỗi lần verify chỉ `dotnet build` ra thư mục scratch để tránh khóa DLL). → **Phải REBUILD + RESTART
app Web, và RESTART `uvicorn` (AI service)** thì các fix mới có hiệu lực. Phần dọn DB (xóa chapter
CANCELLED) thì đã chạy thật trên DB rồi.

## 1. Vấn đề đang fix
Workspace của Mangaka có hàng loạt lỗi + cải tiến:
- (a) Cảnh báo "Phát hiện bản nháp chưa lưu" hiện sai mỗi lần chuyển chapter/page.
- (b) Hiệu năng: chuyển trang/chương nhiều dữ liệu rất chậm; bấm phân trang nhanh → treo / mất kết nối.
- (c) Xóa **page**, **page version**, **chapter** đều không hoạt động (báo lỗi).
- (d) Tính năng **translate** trả 500.
- (e) Chapter 4 mở bị treo (dữ liệu region hỏng).
- (f) Tạo chapter mới lỗi `duplicate key`; nhiều chapter bị CANCELLED ẩn mất.
- (g) Pin tool hiện dư ô `#8`; reload xong **pagination bấm không được**; undo sau khi tạo version.
- (h) Thiết kế lưu bản dịch + nút **Export**.

## 2. Nguyên nhân đã tìm ra
- **Draft warning sai:** JS `loadRegions` luôn gọi `syncToBlazor`→`OnRegionsUpdated`→`saveMmsDraft`
  ngay cả khi chỉ XEM trang (kể cả page rỗng `[]`) → mọi page bị ghi 1 draft "ma".
- **Treo khi bấm nhanh:** `OnPageChanged`/`LoadPage` không có khóa re-entrancy; `SaveProgress` chạy +
  toast mỗi lần chuyển; `LoadPage` fire-and-forget.
- **Pagination chết sau reload (nguyên nhân THẬT):** `LoadPage` gọi `StateHasChanged()` TRƯỚC khi
  `finally` đặt `_isPageLoading=false`; load đầu chạy từ `OnAfterRenderAsync` (không tự render lại) →
  UI kẹt render `_isPageLoading=true` → pagination `Disabled` vĩnh viễn dù giá trị thật là false.
- **Xóa page:** code hard-delete nhưng FK (ChapterPageVersion…) không cascade → đúng thiết kế là
  **soft delete** (`ChapterPage.deleted_at_utc`, mọi query đã lọc sẵn).
- **Xóa version:** `DeleteFileResourceAsync` gọi thiếu user id → set `deleted_at_utc` mà
  `deleted_by_user_id` null → vi phạm CHECK `ck_file_resource_deleted_pair`.
- **Xóa chapter:** code `EXEC manga.usp_Chapter_Delete` nhưng **SP này không tồn tại** trong DB.
- **Translate 500:** lỗi nằm trong service Python; chỗ duy nhất chưa bọc try là `clean_bubble(roi)`.
- **Ch4 treo:** dữ liệu hỏng **887.093 regions** (page2 v1=354k, page3 v3=532k); `SelectChapter`
  eager-load toàn bộ region của mọi version → materialize 887k dòng → treo.
- **Tạo chapter duplicate:** `AddNewChapter` đánh số theo danh sách HIỂN THỊ (đã lọc CANCELLED) → chọn
  số trùng với chapter CANCELLED còn trong DB (`uq_chapter_series_chapter_number` KHÔNG filtered).
- **Pin `#8`:** pin region (vùng ≤0.05 neo annotation) bị vẽ như một region box.
- **Edit-bleed (gốc của "undo sau tạo version"):** chỉnh sửa ghi vào version đang active; `SaveAsNewVersion`
  gọi `SwitchVersion`→`SaveProgress` → rò chỉnh sửa vào version CŨ (bản gốc không còn nguyên).
- **Export:** chỉ là stub (chỉ hiện toast, không tải gì).

## 3. File đã đọc / thay đổi
**Đã thay đổi:**
- `Web/Components/Pages/Mangaka/CreatorWorkspace.razor` — draft-fix (silent load), re-entrancy guard,
  per-page cache (tasks/annotations), region-count cap, `OptimizedImageUrl` (f_auto,q_auto), preload,
  soft-delete page call, version delete user id, AddNewChapter numbering, DeleteChapter (draft mới xóa
  hẳn / khác thì chặn), `IsChapterLocked`+CANCELLED, pin exclusion, pagination re-render fix, version
  create dùng service transaction + cleanup + edit-bleed fix, **ExportCurrentPage** chạy thật.
- `Web/Components/Pages/Mangaka/WorkspaceChapterSidebar.razor` — **MỚI**, child component có `ShouldRender`.
- `Web/wwwroot/js/mangaAiCanvas.js` — `loadRegions(silent)`/`saveState(silent)`, `loadImage` luôn
  resolve (+timeout), `mmsPreloadImages`, `exportRenderedImage`/`downloadRenderedImage`.
- `Application/Services/ChapterPageService.cs` — soft delete page.
- `Application/Services/ChapterPageVersionService.cs` — version delete (ExecuteDelete regions) +
  `CreateVersionWithFileAndRegionsAsync` (transaction).
- `Application/Services/PageRegionService.cs` — `GetRegionCountsByVersionIdsAsync`.
- `Application/Services/FileResourceService.cs` — bắt buộc `deletedByUserId`.
- `Application/Interfaces/IPageRegionService.cs`, `IChapterPageVersionService.cs` — thêm method.
- `Domain/Interfaces/IGenericRepository.cs` — `CountByAsync`, `ExecuteDeleteAsync`.
- `Domain/Interfaces/IUnitOfWork.cs` — Begin/Commit/Rollback transaction.
- `Infrastructure/Repositories/GenericRepository.cs` — impl CountBy/ExecuteDelete.
- `Infrastructure/Repositories/UnitOfWork.cs` — impl transaction.
- `Infrastructure/Repositories/ChapterRepository.cs` — cascade xóa chapter bằng raw SQL (thay SP thiếu).
- `src/MangaAI_Service/main.py` — translate: guard `imdecode None` + bọc try `clean_bubble`.
- **DB (data-only, đã commit):** xóa các chapter CANCELLED của "Mock Serialized Series" (gồm Ch4 hỏng).

**Đã đọc (chính):** `MangaManagementSystem_Schema.sql` (FK/CHECK/index), `docs/context.md`,
`docs/agents/AGENTS.md`, `docs/agents/Csharp_Coding_Conventions.md`, các service/repository/DbContext.

## 4. Bước tiếp theo cần làm
1. **REBUILD + RESTART app Web; RESTART uvicorn.** (Bắt buộc — app đang chạy bản cũ.)
2. Chạy smoke test (mục 5).
3. Nếu translate vẫn 500 → copy traceback ở console uvicorn (`traceback.print_exc()`) để sửa tiếp.
4. **Plan B (dài hạn):** migrate luồng page-version writes sang typed API client → MediatR command
   (dùng lại transaction đã có) cho đúng kiến trúc.
5. Tùy chọn: Export **cả chương** (ZIP); lazy-parse regions cho version không active; tách thêm
   component để giảm re-render; (nếu muốn) bản dịch "phẳng read-only" lưu DB — đã thống nhất ưu tiên
   bake-khi-Export thay vì lưu version read-only.

## 5. Những gì CHƯA kiểm tra
- **Toàn bộ chỉ verify bằng `dotnet build` (0 errors)** + 1 **dry-run SQL** (chapter cascade trên Ch2,
  rollback). **CHƯA chạy app thật** — manual smoke do user thực hiện.
- Chưa test runtime: xóa page (soft), xóa version, xóa chapter (draft) + chặn chapter đã review,
  tạo chapter mới (số đúng), translate sau fix, pagination sau reload, pin `#8` mất, mở Ch-nhiều-region
  không treo (region cap), **Export** tải đúng file PNG có chữ dịch, **edit-bleed** (tạo version mới →
  bản gốc giữ nguyên), best-effort cleanup Cloudinary khi tạo version lỗi.
- Build dùng **per-project** (`...Web.csproj` ra thư mục scratch) vì `.slnx` không build được bằng
  .NET 8 CLI và app đang chạy giữ DLL output. Chưa build qua Visual Studio.
- Cạnh nhỏ đã biết: ngay sau "Save as new version", nếu xem lại version cũ trong CÙNG phiên có thể tạm thấy chỉnh sửa (DB đã đúng, reload sẽ đúng); undo reset khi chuyển version/page là cố ý.
