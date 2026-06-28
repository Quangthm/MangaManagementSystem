# Handoff — Mangaka Workspace fixes (session 2026-06-28)

> Tóm tắt nhanh để tiếp tục. Chi tiết đầy đủ từng vòng: `docs/revision/Mangaka/2026-06-27-workspace-perf-and-draft-fix.md` (Round 1→10).
> File chính làm việc: `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` (~2.700 dòng, Blazor Server, dùng pattern cũ Web→Application service trực tiếp).

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
- Cạnh nhỏ đã biết: ngay sau "Save as new version", nếu xem lại version cũ trong CÙNG phiên có thể tạm
  thấy chỉnh sửa (DB đã đúng, reload sẽ đúng); undo reset khi chuyển version/page là cố ý.
