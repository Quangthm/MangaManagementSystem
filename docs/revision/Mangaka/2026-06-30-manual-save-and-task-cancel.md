# Session note — Manual-save model + Task Cancel (2026-06-30)

> File chính: `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor`
> (Blazor Server, pattern cũ Web → Application service trực tiếp). Phụ: `WorkspaceChapterSidebar.razor`,
> `Application/Interfaces/IChapterPageTaskService.cs`, `Application/Services/ChapterPageTaskService.cs`.
> **Build:** Web **0 errors** (warnings đều pre-existing). **CHƯA smoke test runtime** — cần rebuild + restart.

---

## 1. Mục tiêu
1. Chuyển workspace Mangaka từ **autosave** → **manual save** (chỉ ghi DB khi bấm **Save**).
2. Sửa **xóa task**: hard-delete → **Cancel** theo BR-PGTASK (giữ row + lý do + audit).

---

## 2. Mô hình manual-save (Phase 1→4)

**Buffer trong RAM, chỉ flush khi Save:**
- **Pending chapter** (`AddNewChapter`): tạo `ChapterModel { ChapterId = Guid.Empty, IsPagesLoaded = true }`, KHÔNG ghi DB. Số hiển thị lấy qua read-only probe (`GetChaptersBySeriesIdAsync`); label thật chốt lại lúc Save (chống trùng cả chapter CANCELLED).
- **Pending page** (`HandleFileUpload`): giữ `PendingBytes` + data URL base64 để hiển thị canvas; KHÔNG upload Cloudinary/DB. `ChapterPageId`/`ChapterPageVersionId = Guid.Empty`.
- **Region**: vẫn cơ chế cũ — vẽ → `OnRegionsUpdated` set `Regions` + `IsDirty`; flush bằng `SaveProgress` (`BulkReplacePageRegionsAsync`).

**Save** (`SaveAllChangesAsync`): `FlushPendingAsync()` → `SaveProgress()`.
- `FlushPendingAsync` đúng thứ tự phụ thuộc: tạo Chapter (label chống trùng) → mỗi pending page: upload Cloudinary → `FileResource` → `ChapterPage` → `ChapterPageVersion` → `SetCurrentVersion`; rồi promote model về id thật + đổi data URL → URL Cloudinary; set `IsDirty` = (có region hay không) để `SaveProgress` flush region.

**Trạng thái chưa lưu:** `HasUnsavedChanges` = `_imageDirty` || có chapter/page pending || version `IsDirty`. Header indicator + nút **Save** (vàng khi dirty).

**Discard/Revert** (`DiscardChangesAsync`, Phase 4): confirm → clear `setUnsavedFlag(false)` → `Nav.NavigateTo(Nav.Uri, forceLoad:true)` (reload từ DB; buffer chỉ ở RAM nên mất sạch, region dirty về bản đã lưu). Nút **Discard** cạnh Save, hiện khi dirty.

**Gating** (`EnsureSavedBeforeAsync`, dựa trên `HasPendingStructure` = chỉ chapter/page pending, KHÔNG gồm image/region dirty để "Save as New Version" không tự prompt chính nó): áp cho `CreateTask`, `CreateAnnotation`, `SaveVersionNote`, `SaveAsNewVersion`, `HandleUploadVersion`, `SubmitChapterForReview` → prompt "Save now?" nếu page/chapter còn pending.

**Navigation guard:** beforeunload + dialog Back (Save/Discard/Cancel) khi dirty (Phase 1).

**Badge "Unsaved"** trên sidebar cho chapter pending (`WorkspaceChapterSidebar` — thêm `IsPending` vào signature để re-render khi đã lưu).

> AI Segment/Translate **không** bị ảnh hưởng: chạy trên ảnh canvas (data URL pending vẫn dùng được), region vào buffer, flush khi Save.

---

## 3. Task: hard-delete → Cancel (BR-PGTASK)

**Lý do:** BR-PGTASK-015 (status chỉ ASSIGNED/UNDER_REVIEW/COMPLETED/CANCELLED — không có "deleted"), 027/029 (giữ row để truy vết), 028 (cancel phải có lý do), 031 (audit cancellation). Hard-delete vi phạm tất cả. Annotation đã làm đúng (BR-ANN-017: không xóa, chỉ resolve).

**Đã làm:**
- `DeleteTask` → thay bằng `OpenCancelTaskDialog` + `ConfirmCancelTask` → gọi **`TaskService.CancelTaskAsync(actor, taskId, reason)`** (SP `manga.usp_ChapterPageTask_Cancel`: set CANCELLED + audit; chặn cancel task COMPLETED/CANCELLED; bắt buộc reason; actor phải Mangaka active). Sau cancel: set local `Status = "Cancelled"` (giữ trong list, gạch ngang), KHÔNG remove.
- Dialog nhập lý do (reuse pattern `ReviewSubmissions.razor`).
- Nút Cancel (icon `Cancel`, đỏ) chỉ hiện khi task **chưa terminal** (không phải Done/Cancelled).
- Load mapping thêm `"CANCELLED" => "Cancelled"` (trước map nhầm về "Todo").
- **Bỏ method chết `DeleteChapterPageTaskAsync`** khỏi `IChapterPageTaskService` + impl (0 caller sau đổi; footgun vi phạm BR).

**KHÔNG đổi:** SP, schema, Cloudinary. Reason lưu ở **`audit.AuditEvent.detail_json`** (key `reason`, action `CHAPTER_PAGE_TASK_CANCELLED`) — SP không ghi vào `task_description`. (BR-PGTASK-028 nói reason nên ở description; SP nhóm chỉ ghi audit → giữ nguyên, không sửa SP.)

### 3b. Status task: đồng bộ DB enum + read-only theo role
- Nhãn UI giờ khớp DB: **Assigned / In Review / Completed / Cancelled** (bỏ "Todo"/"In Progress" — cả hai vốn = ASSIGNED, là fiction vi phạm BR-PGTASK-016 "không track đã-bắt-đầu").
- **Bỏ `CycleTaskStatus`** (Mangaka không tự lật status); chip status thành **read-only** trong workspace.
- Chuyển trạng thái là **role-owned** (enforced ở SP): Assistant submit → UNDER_REVIEW; Mangaka approve → COMPLETED / return → ASSIGNED (ReviewSubmissions); Mangaka cancel → CANCELLED (workspace). Workspace của Mangaka chỉ: tạo + cancel + xem.
- Load map bỏ nhánh chết `IN_PROGRESS` (không có trong CHECK constraint).

---

### 3c. Annotation/Region/Task: panel number + (0,0) phantom + created_by
- **Hiển thị target** (task + annotation): bỏ tọa độ `[X:..,Y:..,W:..,H:..]` → chỉ **`Panel #N`**, số **khớp canvas** (map `PageRegionId → #N` của version đang xem từ `version.Regions` JSON; nhiều panel → "Panel #3, Panel #5").
- **Bug pin (0,0):** nguyên nhân = fallback tự đẻ region `(0,0, 0.01×0.01)` khi tạo annotation/task mà không chọn panel/pin → reload thành "Pin at (0,0)". **Sửa:** bỏ fallback ở cả `CreateAnnotation` và `CreateTask`; không chọn target → cảnh báo + dừng (BR-ANN-001/002, BR-PGTASK-001/007/008). *Behavior change:* không còn annotation/task "whole page" ngầm — phải chọn panel hoặc đặt pin.
- **`PageRegion.created_by_user_id` NULL:** `CreatePageRegionDto` thiếu field → thêm `CreatedByUserId`, set trong `CreatePageRegionAsync` + `BulkReplacePageRegionsAsync`, truyền `_currentUserId` từ `EnsureRegionsSavedAsync` + `SaveProgress`. (Chỉ áp cho region tạo qua workspace; region cũ vẫn NULL; caller khác — Editor — cần thêm tương tự nếu muốn.)
- **Dọn phantom cũ:** script `scratchpad/cleanup-phantom-zero-regions.sql` (preview + transaction mặc định ROLLBACK; xóa annotation chỉ-neo-phantom + unlink task, không hard-delete task).

## 4. Files đã đổi
- `Web/Components/Pages/Mangaka/CreatorWorkspace.razor` — manual-save (buffer/Flush/gating/Discard/indicator), task Cancel (dialog + methods + markup), load status map.
- `Web/Components/Pages/Mangaka/WorkspaceChapterSidebar.razor` — badge "Unsaved" + `IsPending` vào signature.
- `Application/Interfaces/IChapterPageTaskService.cs` — bỏ `DeleteChapterPageTaskAsync`.
- `Application/Services/ChapterPageTaskService.cs` — bỏ impl `DeleteChapterPageTaskAsync`.

---

## 5. Smoke test cần chạy (CHƯA test runtime — REBUILD + RESTART Web trước)
1. Tạo chapter → badge "Unsaved", **chưa** có row DB. Upload page → ảnh hiện ngay, **chưa** lên Cloudinary/DB.
2. **Save** → DB có Chapter/Page/Version/FileResource/Region + ảnh Cloudinary; indicator "Saved"; badge mất.
3. **Discard** khi đang dirty → reload, mất hết thay đổi chưa lưu.
4. Tạo Task/Annotation/Note khi page chưa lưu → prompt "Save now?" → Save → tạo OK.
5. **Cancel task**: nút đỏ → dialog bắt buộc lý do → task chuyển "Cancelled" (gạch ngang, giữ trong list). DB: `status_code = CANCELLED`, audit có bản ghi. Task COMPLETED → không có nút Cancel.
6. Reload trang có task đã cancel → hiện đúng "Cancelled" (không phải "Todo").
7. Reload/Back khi dirty → cảnh báo native / dialog Save-Discard-Cancel.

---

## 6. Follow-ups (Phase 5 / sau)
- **Per-page pending indicator** (chấm trên pagination) — hoãn.
- **Memory hardening** (PHẦN RỦI RO CÒN LẠI, chưa làm): pending page giữ `PendingBytes` + base64 trong circuit Blazor Server (~47MB/page ảnh 20MB) + gửi base64 sang browser. Hướng: blob URL phía JS (`URL.createObjectURL`) để circuit không ôm base64. Rủi ro = vòng đời blob URL (revoke khi Discard/Save/đổi version) + split-view → nên làm tách riêng, test độc lập; chỉ cần khi thật sự lag.
- ~~Nhãn status UI fiction~~ → **ĐÃ XONG** (xem §3b): relabel theo DB + chip read-only + bỏ CycleTaskStatus.
- Cân nhắc route chapter create/submit của workspace qua **chapter CQRS mới của team** (xem `handoff.md` 2026-06-28, mục integration).
