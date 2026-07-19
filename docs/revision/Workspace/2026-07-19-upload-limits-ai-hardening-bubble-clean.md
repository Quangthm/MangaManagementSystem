# Session — Upload limits + AI error handling + viết lại clean_bubble (2026-07-19)

**Branch:** `feature/workspace-v3` (PR #78 → main). **Không đụng DB / stored procedure / Cloudinary / luồng save.**

## 1. Bối cảnh

Sau khi merge `origin/main` (task eligibility của team Mangaka) và xác nhận branch `temp` build sạch, rà lại 2 câu hỏi thiết kế + phần AI:

- Data đã xóa trong DB có nên purge không → **KHÔNG**, giữ nguyên thiết kế (xem §5).
- Upload page có nên giới hạn không → **CÓ**, đã làm (§2).
- Tính năng AI có 4 điểm chưa đúng thiết kế → làm 2, hoãn 2 có chủ đích (§3).
- Chất lượng xóa chữ bong bóng (user báo lỗi thật) → viết lại (§4).

## 2. Giới hạn upload (tầng Web)

**Files:** `CreatorWorkspace.razor.cs`, `CreatorWorkspace.Versions.cs`

Trước đó luồng Mangaka **không validate gì** (chỉ `accept="image/*"` = gợi ý client), trong khi luồng Assistant đã validate đúng. Rủi ro thật: Blazor Server buffer upload **trên server**, `GetMultipleFiles(100)` × 20MB → hàng GB trong circuit của một user.

- Hằng số dùng chung: `WorkspaceMaxFileSizeBytes` = 10MB, `WorkspaceMaxPagesPerUpload` = 30, `WorkspaceAllowedFileTypes` = png/jpeg/webp (đổi tên từ `AssistantWorkspace*`, luồng Assistant giữ nguyên hành vi).
- Helper `IsAllowedWorkspaceImage(file, out error)` — check `file.Size` + `file.ContentType` **không mở stream**.
- 3 handler đã sửa: `HandleFileUpload`, `HandleUploadVersion`, `HandleDoublePageSelected`.
- **Điểm quan trọng:** validate TRƯỚC khi `OpenReadStream` → file lỗi bị **bỏ qua kèm cảnh báo**, các file hợp lệ vẫn vào; trước đây `OpenReadStream` throw giữa vòng lặp làm **mất sạch batch** và hiện message exception thô.
- `GetMultipleFiles` vượt hạn → bắt `InvalidOperationException`, báo "tối đa 30 trang".

## 3. AI: timeout + phân biệt lỗi

**Files:** `Infrastructure/DependencyInjection.cs`, `Infrastructure/Services/AiService.cs`, `MangaAI_Service/main.py`

- `AddHttpClient<IAiService, AiService>` → `Timeout = 60s` (mặc định 100s quá dài để giữ circuit + ảnh trong RAM; 60s vẫn đủ cho YOLO + manga-OCR kể cả lần đầu load model ~400MB).
- Gom vào helper `SendAsync<T>`, thay `return null` bằng **4 loại lỗi riêng**: service không chạy / timeout / HTTP lỗi / JSON hỏng. **Không retry khi timeout** (inference nặng, retry chỉ nhân đôi tải).
- **Không phải sửa razor:** `SegmentImageJS`/`TranslateRegionsJS` đã sẵn `catch` và trả `ex.Message`. Chỉ đổi câu fallback `"AI returned null"` → `"AI service returned an empty result"`.
- `main.py`: 7 `print` khởi động → tiếng Anh; đánh dấu `/api/ai/clean-and-translate` là **DEV/DEMO ONLY** (chỉ `test_ui.html` gọi, .NET không dùng) — giữ lại làm công cụ dev, không xóa.

### Hoãn có chủ đích (known deviation)

1. **Base URL hardcode** `http://127.0.0.1:8000/api/ai` trong `AiService.cs`, trong khi `Web/appsettings.json:37` ĐÃ có key `AiService:BaseUrl` **không bao giờ được đọc** → bẫy config. Chỉ đau khi Python chạy máy khác. Fix ~20 phút, rủi ro chính là dấu `/` của `BaseAddress` + URI tương đối.
2. **Vi phạm ranh giới Clean Architecture:** `CreatorWorkspace.razor:27` `@inject IAiService` — Web gọi thẳng Application/Infrastructure, **không có AI controller** trong project API. Sai luồng CLAUDE.md §3.1/§4.6. Hệ quả thật: ảnh base64 nằm trong Blazor circuit suốt thời gian inference; không có chỗ đặt auth/rate-limit/logging. Fix đúng = `MangakaAiController` + `IAiApiClient` + command/handler (~3–4h). Hoãn vì rủi ro cao sát deadline; **nên làm PR riêng.**

## 4. Viết lại `clean_bubble` (v2 → v3) — `MangaAI_Service/main.py`

User báo 2 lỗi thật: (a) tô thiếu, còn lộ chữ cũ; (b) tô lố, **đè mất nét vẽ**.

**Nguyên nhân gốc:** v2 đi dò **nét CHỮ**. Hai thủ phạm cụ thể:
- `bitwise_or(filled_bubble, region_mask)` — nhét **hình chữ nhật** (bounding rect + đệm 15px) vào mask bong bóng tròn → tràn ra 4 góc.
- `fill(255)` fallback — khi KHÔNG nhận ra bong bóng thì **mở khoá tô cả ROI** → xóa trắng artwork. Đây là chế độ hỏng nặng nhất.

**Thiết kế v3:** xác định **lòng bong bóng** rồi tô sạch toàn bộ (bên trong bong bóng chỉ có chữ nên không cần phân loại). 157 → ~90 dòng. Fail-safe: không chắc thì **trả ảnh gốc**, vì chữ sót cứu được còn nét vẽ bị phá thì không.

### Ba lần đo, ba lần sửa hướng (ghi lại để không ai thử lại)

| Thử | Kết quả đo | Xử lý |
|---|---|---|
| `MORPH_CLOSE` để nối lòng bong bóng | Nối **xuyên qua nét viền mảnh** ra vùng trắng ngoài → coverage median **1.00** → chặn **51/55** | Bỏ CLOSE (median về 0.76 ≈ π/4) |
| Guard `coverage > 0.92` | Chặn nhầm **14 bong bóng thật** (caption chữ nhật lấp đầy bbox). Thử tách bằng độ phẳng nền: bong bóng 0.86 vs artwork ngẫu nhiên 0.84 — **không tách được** | Bỏ guard trên; YOLO đã quyết định, đừng đoán lại bằng thống kê pixel |
| Chữ TO không được tô (ca 〈黄昏〉) | Tâm rơi trúng nét chữ (**21%** số bong bóng) → fallback bắt nhầm **ruột chữ kanji** → mảnh tí hon → từ chối. 4/262 ca | Đổi sang **"mảnh nhỏ nhất mà đường bao TÔ ĐẦY bao quanh tâm"** |

Các hướng đã thử và **loại bỏ** cho ca bong bóng kề nhau (đã ghi trong docstring):
- Bao lồi (convex hull): chữ sót 1.44% → 0.02% nhưng **ăn mất hatching** cạnh bong bóng → loại.
- Bao lồi + chỉ lấp lõm nhỏ & sáng: chỉ còn 1.36% → gần như vô dụng.
- Lấp "lỗ" theo phân cấp contour: **226/262 không tô được** — bbox ôm sát làm đường viền chạm mép ROI, vòng bị hở.

### Kết quả đo

Bộ 55 bong bóng (8 trang), v2 vs v3:

| | v2 | v3 |
|---|---|---|
| **Tô lố ra artwork** | **11** | **4** |
| Bỏ qua (fail-safe) | — | 1 |

Bộ 262 bong bóng (40 trang), trước/sau fix chữ to:

| | Trước | Sau |
|---|---|---|
| Không tô được | 4 | **1** |
| Chữ sót trong lòng | 0.17% | **0.00%** |
| Chạm biên (rủi ro artwork) | 20 | **20 — không đổi** |

> 4 ca "chạm biên" còn lại của v3 là **caption chữ nhật lấp đầy khung** — tô tới biên ở đó là đúng.

### Hạn chế còn lại (đã chốt, có chủ đích)

**Bong bóng kề/chồng nhau:** viền bong bóng bên cạnh cắt lòng bong bóng đang xử lý → chữ ở góc bị cắt sót lại (~1.4% pixel, tập trung ở ~15% bong bóng có kề nhau; điển hình là chữ cuối của một dòng dọc). Đã cân nhắc và **chọn giữ**, vì mọi cách xóa triệt để đều đánh đổi bằng phá artwork. Mangaka tô tay bằng brush ~10 giây.

## 5. Data đã xóa trong DB — KHÔNG purge (không đổi code)

Chỉ **2 bảng** có soft-delete: `manga.FileResource`, `manga.ChapterPage` (cặp `deleted_at_utc` + `deleted_by_user_id` + CHECK). Không có job purge — **và không nên thêm**:

1. Hệ thống là workflow + audit, data đã xóa chính là dấu vết. BR-CP-013/020, BR-REG-012 đã chốt: version/region **không bao giờ hard-delete** (`DeleteChapterPageVersionAsync` cố tình throw).
2. Hard-delete làm **mồ côi FK** (task/annotation trỏ tới region của version).
3. Row SQL chỉ là metadata rất nhẹ; thứ tốn chỗ là **file Cloudinary** — và code đã xóa thật rồi (best-effort `DeleteFileAsync` sau soft-delete). Tách bạch này đang đúng.

**Việc nên làm thay vào đó:** đảm bảo mọi query lọc `deleted_at_utc IS NULL` (schema đã có filtered index dòng 133, 692). Lộ data đã xóa ra UI mới là bug thật.

> Chỗ duy nhất cleanup job có giá trị: schema đã có code `CLEANUP_REQUIRED`/`CLEANUP` (dòng 835, 917) để **đối soát file Cloudinary mồ côi** (upload xong nhưng SQL fail) — reconcile Cloudinary, KHÔNG phải purge row SQL.

## 6. Kiểm chứng

- **Build:** API `0 error / 27 warning`, Web `0 error / 67 warning` — đều baseline, **0 warning mới**. (Build ra thư mục riêng vì app đang chạy khóa `bin`, và ổ C: đầy.)
- **Python:** `py_compile` OK; service `--reload` tự nạp.
- **E2E qua service thật:** `/segment` → 15 vùng; `/translate-selected` → `success`, ảnh sạch trả về, OCR + dịch chuẩn (`わかったか？` → "Hiểu rồi?").
- **Ảnh so sánh** (scratchpad, không commit): `compare.png` (8 ca cận cảnh), `page_p4.jpg.png` (nguyên trang), `hier.png` (ca chữ to trước/sau).
- **User đã smoke test trong workspace và xác nhận OK.**

## 7. Follow-up

1. **AI base URL vào config** (~20 phút) — cẩn thận dấu `/` của `BaseAddress`; phải test thật, không tin mỗi build.
2. **AI qua controller + typed API client** (~3–4h) — PR riêng, xem §3.2.
3. Ổ **C: gần đầy** (~3GB trống) — gây lỗi build "not enough space" khi output sang C:.
4. `MudPagination` vẫn đánh số theo vị trí thay vì PageNo thật (tồn từ session trước).
