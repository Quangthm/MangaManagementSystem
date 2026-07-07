# Handoff — Mangaka Workspace: manual-save + full API migration (2026-07-01)

> ### UPDATE 2026-07-07 — refactor CreatorWorkspace (partial split) + gom using. ⚠️ CHƯA COMMIT, CHƯA CHẠY RUNTIME
>
> **Vấn đề đang làm:** `CreatorWorkspace.razor` quá lớn/khó bảo trì → tách nhỏ (code-behind + topical partials), rút view-model/helper ra file riêng, và **gom using trùng lặp**. Đây là refactor CẤU TRÚC (không đổi hành vi).
>
> **Nguyên nhân / phát hiện quan trọng:**
> - Lúc build kiểm tra `GlobalUsings.cs`, CLI báo **"246 errors"** → ban đầu tôi tưởng `GlobalUsings` gây **ambiguity** nên định bỏ. **CHẨN ĐOÁN SAI.** Thực chất **ổ C: đầy 100%** (scratchpad của tôi phình 348MB) làm bước copy DLL fail (`MSB3021 not enough space`), **KHÔNG có lỗi CS nào**. Sau khi dọn cache + build lại và **chỉ grep lỗi `CS`/`RZ`** → xác nhận `GlobalUsings` compile SẠCH, an toàn. → **Bài học: build tới scratch trên C: không đáng tin khi C: gần đầy; verify bằng cách grep `error CS`/`error RZ`, bỏ qua lỗi MSB copy/lock.**
> - **C: hiện chỉ còn ~318MB trống** (đã dọn 338MB cache build của tôi). Ảnh hưởng cả build trong Visual Studio — user nên dọn C:.
>
> **Files ĐÃ THAY ĐỔI (đều nằm trong `Components/Pages/Workspace/`, đều LOCAL chưa commit):**
> - `CreatorWorkspace.razor` — tách markup, phần logic chuyển sang các partial dưới.
> - `CreatorWorkspace.razor.cs` (mới) — code-behind chính; using rút còn 1 dòng `using static ...WorkspaceHelpers;`.
> - `CreatorWorkspace.Save.cs`, `CreatorWorkspace.Versions.cs`, `CreatorWorkspace.Pages.cs` (mới) — partial theo chủ đề; mỗi file chỉ còn `using static ...WorkspaceHelpers;`.
> - `WorkspaceHelpers.cs` (mới) — hàm pure (BuildRegionDtosForSave, StripSelected, OptimizedImageUrl, NormalizeRegionType); không cần using nào.
> - `Models/WorkspaceViewModels.cs` (mới) — view-model UI (ProductionTask, AnnotationModel, ChapterModel, PageModel, PageVersionModel, RegionModel); không cần using nào.
> - `GlobalUsings.cs` (mới, **cấp project Web**) — mirror `_Imports.razor` cho file `.cs` (Components, MudBlazor, JSInterop, DTOs.Auth/Manga, Interfaces, Services...). `ImplicitUsings` đã lo `System.*`/`Linq`/`Net.Http`.
> - 1 comment mồ côi ở seam Pages đã sửa (quét toàn bộ: chỉ có 1 cái).
> - Ngoài ra 1 message VN→EN đã sửa trước đó (cùng đợt local).
>
> **ĐÃ verify:** `dotnet build` Web **và** API → **0 lỗi `CS`/`RZ`** (compile sạch cả hai). Comment mồ côi: đã quét, sạch.
>
> **CHƯA kiểm tra (QUAN TRỌNG):**
> - **CHƯA chạy runtime** — chưa mở Visual Studio build đầy đủ (link+copy) và chưa smoke test workspace thật (upload page, save version, tạo task/annotation, submit chapter...). Refactor cấu trúc lớn → **BẮT BUỘC smoke test trước khi push.**
> - **CHƯA full build** (chỉ compile-check qua CLI vì C: đầy làm bước copy fail — không phải lỗi code).
> - **CHƯA commit** toàn bộ đợt này (VN fix + refactor + gom using). Merge `origin/main` trước đó ĐÃ push lên `feature/workspace-v3`; mọi thứ sau merge còn local uncommitted.
>
> **CẬP NHẬT 2026-07-07 (cùng ngày, muộn hơn) — đã commit + merge main + dọn C::**
> - Refactor đã **commit local** `bb322b1` (chưa push).
> - **Merge `origin/main` vào `feature/workspace-v3`** → merge commit `fc91f30`, **SẠCH, 0 xung đột** (main +59 commit: auth/admin, publication scheduling, security hardening, file cleanup, legacy delete... KHÔNG đụng file `Workspace/` nào). Branch giờ ahead 61 origin/feature/workspace-v3, **CHƯA push**.
> - **Compile-verify lại SAU merge:** Web + API **0 lỗi CS/RZ/NU** → `GlobalUsings.cs` không đụng độ `using` với code mới của main.
> - **Dọn ổ C::** NuGet cache 1.4GB đã dời C:→`D:\NugetPackages`; set env **`NUGET_PACKAGES=D:\NugetPackages`** cấp User (vĩnh viễn, reversible). C: từ 985MB → ~4.8GB trống. ⚠️ **VS/terminal đang mở phải khởi động lại** mới nhận env mới (tiến trình cũ vẫn ghi cache về C:).
>
> **Bước tiếp theo (vẫn còn):**
> 1. **Khởi động lại Visual Studio 2022** (để nhận `NUGET_PACKAGES` mới) → build đầy đủ + **chạy Web**, smoke test luồng workspace (đặc biệt Save/Versions/Pages vì vừa tách partial) + kiểm tính năng mới từ main không vỡ.
> 2. Nếu OK → **push `feature/workspace-v3`** (hiện đang giữ local, ahead 61).

> ### UPDATE 2026-07-07 (tiếp) — workspace bugfix batch (leader feedback). CHƯA COMMIT
> **Đã sửa (build sạch Web/API, local uncommitted):**
> - **#1** sidebar status label thiếu status mới → thêm SCHEDULED/PUBLISHED/RELEASED/CANCELLED trong `WorkspaceChapterSidebar.razor` (method `StatusLabel()`).
> - **#4** bỏ chip "Active"; chapter đang chọn: nền `#eef2ff` + `border-left 3px #4f46e5` + label bold primary (luôn hiện status thật).
> - **#2** save timeout ("HttpClient.Timeout 100s"): nguyên nhân Cloudinary SDK dùng HttpClient default 100s. Fix trong `CloudinaryFileStorageService` ctor: `_cloudinary.Api.Timeout = 300000` (5', ms). ⚠️ file Infrastructure dùng chung — chỉ nâng timeout.
> - **#5a** task panel gọn: `CompactTarget()` (2 panel + "…(+N)") + tooltip full — `CreatorWorkspace.razor.cs` + `.razor`.
> - **#5b** deadline mỗi task: `ProductionTask.DueAtUtc` + map từ `t.DueAtUtc`/`dbTask.DueAtUtc` (client `GetTasksByPageAsync`/`CreateTaskAsync` trả `ChapterPageTaskDto` vốn CÓ sẵn `DueAtUtc`) + hiển thị "Due MMM d" (đỏ + "(overdue)" nếu quá hạn). KHÔNG đụng API/DTO.
> - **#8** user CHỐT **giữ soft-delete** → không đổi code.
>
> **Còn lại (đã chẩn đoán, CHƯA làm):**
> - **#3** editor vào workspace trống — **GỐC RỄ XÁC ĐỊNH:** `MangakaChapterRepository.QueryAccessibleChapters(actorUserId)` (Infrastructure ~350-362) lọc chapter về **chỉ series mà actor là contributor role "Mangaka"** → editor trả RỖNG. Controller `MangakaChaptersController` không có `[Authorize]` (dùng header `X-Actor-User-Id`); handler chỉ delegate; gate nằm ở repo query. Các read khác (page counts/pages/versions/tasks/annotations) nhiều khả năng cùng cổng Mangaka-only. **KHÔNG phải fix nhanh:** cần "authorized workspace reader = active contributor bất kỳ (Mangaka/Editor/Assistant)" áp NHẤT QUÁN cho MỌI read của workspace — mỗi cái ở repo/query riêng. Ghi/đổi trạng thái VẪN gated Mangaka-only qua `EnsureActiveMangakaContributorAsync` nên nới READ an toàn cho quyền ghi. "Render tùy role" (user) ⇒ đọc business-rules để biết mỗi role thấy gì (vd assistant chỉ task của mình?). → **đụng shared/merged Mangaka code — làm task riêng có scope rõ, KHÔNG rush.**
> - **#6** "Panel Target (Hold Shift to select multiple)" không chạy + **#7** edit nhiều region cùng lúc (nút Edit Region ở `CreatorWorkspace.razor:174` đang `Disabled` khi `SelectedRegions.Count != 1`) → việc **canvas/JS**, CHƯA đọc JS interop chọn region.

> ### ⚠️ FILE MOVE 2026-07-03 — workspace code sang thư mục Workspace
> `CreatorWorkspace.razor` + `WorkspaceChapterSidebar.razor` đã `git mv` từ
> `Components/Pages/Mangaka/` → **`Components/Pages/Workspace/`** (cạnh `TaskWorkspaceRedirect.razor`).
> Namespace cả hai giờ là `...Components.Pages.Workspace` (CreatorWorkspace folder-derived; sidebar đổi `@namespace` từ Mangaka→Workspace). Route `@page` KHÔNG đổi. Chỉ 2 file này tham chiếu lẫn nhau nên move an toàn, build 0 errors.
> **→ Mọi đường dẫn `Mangaka/CreatorWorkspace.razor` / `Mangaka/WorkspaceChapterSidebar.razor` bên dưới trong handoff giờ đọc là `Workspace/...`.**


> ### UPDATE 2026-07-03 — leader PR review fixes (đang làm theo batch)
> Leader review PR workspace (feature/workspace-v3 → main) requested-changes, 11 issue + deadline. Đây là **tập KHÁC** với review version/file bên dưới. Tiến độ:
> - **Batch 1 — ĐÃ TEST OK (user xác nhận):**
>   - **Deadline task:** thêm `DueAtUtc` vào `CreateMangakaTaskRequest` → controller truyền xuống; UI thêm MudDatePicker "Deadline". SP `usp_ChapterPageTask_Create` vốn nhận `@due_at_utc`.
>   - **#8 FULL_PAGE default:** không chọn panel/pin → task & annotation tự anchor FULL_PAGE region (`PageRegionService.EnsureFullPageRegionAsync` — reuse BR-REG-031, dims từ Cloudinary `IImageMetadataProvider` BR-REG-032, tái dùng pattern QuickSelect). +controller/client endpoint `regions/version/{id}/ensure-full-page`.
>   - **#11 delete region guard:** `DeletePageRegionAsync` chặn xóa region đã link task/annotation (throw); UI `DeleteSelectedRegions` chặn + message của leader.
> - **Batch 2 — BUILD OK, CHƯA user-test:**
>   - **#1 discard chapter chưa lưu:** `DeleteChapter` cho `ChapterId==Guid.Empty` → **remove khỏi UI** (không gọi backend, không mark CANCELLED), đổi selection; sidebar menu hiện **"Discard"** thay "Cancel" khi `IsPending`.
>   - **#7 discard page nhân đôi:** root cause = `SelectChapter` load loop **append** vào `chapter.Pages` không clear → reload (Discard reset `IsPagesLoaded`) nhân đôi. Fix: `chapter.Pages.Clear()` trước loop.
>   - **#2 card chapter đè chữ:** khối trái thêm `flex:1 1 auto; overflow:hidden` + gap → title dài ellipsis, không đè status.
> - **Batch 3 (#9) — clamp hardening, CHỜ user confirm:** box vẽ tràn mép → region tọa độ âm/vượt biên (invalid). Fix: clamp box về trong ảnh (`mangaAiCanvas.js` mouseup). **KHÔNG chắc fix hết "freeze"** — nếu vẫn treo cần console error (render loop). JS-only → chỉ hard-refresh.
> - **Batch 4 (#10) — region editing, BUILD OK chưa test:** nút "Edit Region" (enable khi chọn đúng 1 region) → dialog sửa **type + label**. JS `setRegionMeta(id,type,label)` update canvas + syncToBlazor (mark dirty). Persist qua **Save bình thường → BulkReplace** (match theo `PageRegionId`, giữ nguyên id → link task/annotation an toàn; set `updated_by_user_id`). **KHÔNG cần** endpoint mới, **KHÔNG** đụng `UpdatePageRegionAsync` (bug `updated_by` của nó vẫn dormant — chưa dùng ở workspace).
> - **Batch 5 (#3) — save/submit consistency, BUILD OK chưa test:** `SaveAllChangesAsync` giờ đếm `failedCount`; chỉ báo "saved successfully" + set `Saved` khi **không có lỗi VÀ không còn gì pending** — ngược lại **giữ Dirty** + message trung thực ("X failed, Y saved, try again"). `EnsureSavedBeforeAsync` trả `false` nếu save còn dở → **chặn Submit khi save fail** (hết cảnh submit chapter thiếu page). Thêm progress "Uploading pages… (x/y)" ở header. False unsaved-count tự hết vì state giờ nhất quán.
>   - **CHƯA giải quyết trong #3:** (a) "save quá chậm" = do các call mạng tuần tự (upload Cloudinary + create từng page) — progress chỉ đỡ UX, chưa tăng tốc; (b) idempotency khi backend đã lưu nhưng client timeout — giữ Dirty là trung thực, nhưng retry ở ca hiếm đó CÓ THỂ tạo page trùng (cần dedupe backend — follow-up).
> - **Batch 6 (#4 + #5) — BUILD OK chưa test:**
>   - **#4 (GIỮ dialog thumbnail confirm — user muốn):** quy trình: upload → **dialog review thumbnail + Add/Cancel** → Add (page/version vào buffer "unsaved") → lật pager xem từng trang → OK thì **Save** mới upload Cloudinary + ghi DB. 3 bug đã fix:
>     - (a) nhúng base64 gốc to → **thumbnail** (≤240px jpeg 0.6, hàm `window.mmsMakeThumbnails`).
>     - (b) hàm thumbnail để trong ES module canvas có thể chưa load → **chuyển sang script global `js/upload-preview.js`** (khai báo ở `App.razor`, luôn tải sẵn) → không bao giờ trống ảnh.
>     - (c) **inline `MudDialog @bind-Visible` để lại dialog "ma" thứ 2 không ảnh sau khi Add** → thay bằng **overlay div tự dựng (`@if`)** → đúng 1 dialog, bấm Add/Cancel biến mất sạch, không ghost.
>     - KHÔNG ép crop (docs không bắt cho page đơn).
>   - **#5 (defer version upload):** "Upload Version" giờ **buffer thành pending version** (PendingBytes, ChapterPageVersionId=Guid.Empty) + mark dirty, KHÔNG upload ngay. `SaveAllChangesAsync` thêm nhánh: page mới → `CreatePageWithVersion`; **page đã tồn tại + pending version → `CreateVersionWithFileAndRegions`** (helper `BuildRegionDtosForSave`) — tránh bug tạo page trùng. Set-current + cleanup Cloudinary khi fail đã có sẵn trong flush.
> - **UI polish (user yêu cầu thêm, BUILD OK):**
>   - **Toolbar gọn + hết che Versions panel:** 2 Versions panel (left+split) hạ xuống `top:64px` (dưới toolbar) → không còn bị toolbar đè nút upload-version; toolbar nén lại (UPLOAD PAGES → icon button, gap 1px, divider mx-1).
>   - **Bỏ Cancel Submission (cả header + sidebar menu) → thay bằng confirm khi Submit:** `SubmitChapterForReview` giờ hỏi `DialogService.ShowMessageBox` (English) trước khi submit. ⚠️ Tradeoff: submit rồi thì Mangaka không tự huỷ được nữa (chờ editor review / request revision). `CancelSubmission` handler + `OnCancelSubmission` param còn lại dạng dead (harmless) nếu cần khôi phục.
>   - **Versions panel dời lên `top:20px`** (ngang toolbar, cả 2 pane) — trước hạ 64px giờ toolbar đã nén nên không đè nữa.
>   - **Chapters sidebar collapse/expand** (giống Task Panel): `_isLeftPanelOpen` + `ToggleLeftPanel` + wrap sidebar bằng div width-transition + nút chevron ở mép trái `<main>`. Nút "New Chapter" dời lên (`mt-6→mt-2`, aside `py-4→pt-2 pb-4`).
> - **HOÀN TẤT toàn bộ list leader** (#1,#2,#3,#7,#8,#9,#10,#11 + deadline + #4,#5). Follow-up còn lại (không thuộc list leader): idempotency save khi client timeout; tăng tốc save (parallel upload); DB reset theo main (ranking/reader — cần pull main); auth hardening; migrate AiService; `UpdatePageRegionAsync` set `updated_by` (dormant); tách MangakaPageVersionController.
> - **DB main:** branch local đang SAU main (ranking/reader input update chưa pull). Reset DB "theo bản mới" cần pull/merge main trước — **chưa làm** (phá hủy + có thể conflict).
>
> ### UPDATE 2026-07-02 — review version/file + fix #1–#8 (build API 0 / Web 0 errors, chưa smoke-test)
> Đã review commit `4044f88` và sửa 8 điểm (chi tiết trong review session):
> - **#1** Luồng tạo page (`FlushPendingAsync`, `CreatorWorkspace.razor`) giờ **best-effort cleanup Cloudinary** khi DB create fail (giống 2 luồng version) — hết rò rỉ orphan file.
> - **#3** Thêm audit `PAGE_CREATED` / `VERSION_CREATED` trong `ChapterPageVersionService` (trước đây chỉ delete mới ghi audit).
> - **#4** `FileResource.UploadedByUserId` giờ lấy từ **actor header tin cậy** (controller truyền `actorUserId` + role "Mangaka" xuống service), không tin `FileDto.UploadedByUserId` do client khai.
> - **#5** `version_no` **tính server-side** (max+1) trong `CreateVersionWithFileAndRegionsAsync` thay vì tin client (BR-CP-009/010/011). *(page_no vẫn client-side — soft-delete subtlety, để sau.)*
> - **#6** `SetCurrentVersionAsync` (bản standalone) giờ **bọc transaction** — không để page mất current version giữa 2 pass.
> - **#7** Gỡ dead `@inject ISeriesContributorService ContributorService` (đã migrate sang `IMangakaSeriesContributorApiClient`, không dùng nữa).
> - **#2 (KHÔNG làm — cần quyết định):** 2 method create vẫn là EF-transaction trong Application service (không phải `usp_*` SP như §3.4 khuyến nghị, không qua MediatR như §3.1). **Cố ý không đổi** vì chuyển sang SP = tạo mới DB object (trái §4.1). Đã bù phần "audit" (#3). Tech-debt: cân nhắc gói `usp_ChapterPage_CreateWithVersion` / `usp_ChapterPageVersion_CreateWithFileAndRegions` nếu muốn đúng chuẩn.
> - **#8 (giữ nguyên):** `IFileStorageService` inject trong Web là ngoại lệ có chủ đích (§3.6 "Web upload Cloudinary trước"). `IAiService` advisory — migrate optional.
> - Files đổi: `ChapterPageVersionService.cs`, `IChapterPageVersionService.cs`, `MangakaPageController.cs`, `CreatorWorkspace.razor`.
> - **CHƯA smoke-test runtime** — cần rebuild + restart API(5234)/Web(5244) + hard-refresh; test lại: tạo page (fail→cleanup), Save-as-new-version, set-current, audit ghi đúng.


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
