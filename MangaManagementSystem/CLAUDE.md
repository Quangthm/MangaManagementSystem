# CLAUDE.md — Manga Management System (solution root)

> Hệ thống **quản lý quy trình sản xuất manga** (Manga Creation Workflow & Publishing Management System) — MVP đồ án (SWP391). Đây là hệ thống **hỗ trợ quản lý workflow**, KHÔNG phải công cụ vẽ/studio và KHÔNG thay thế Clip Studio / Photoshop / Krita.
>
> Chủ repo đang phụ trách phần **workspace của Mangaka** và đang trong quá trình phát triển.

> File này nằm tại **solution root** (cạnh `MangaManagementSystem.slnx` và `src/`). Mọi đường dẫn trong file là **tương đối từ thư mục này**.

---

## 0. Định vị thư mục

```
.                                   # ← bạn đang ở đây (solution root)
├─ MangaManagementSystem.slnx       # file solution (định dạng .slnx mới)
├─ setup-secrets.ps1 / instruction.md
└─ src/                             # 5 project .NET + MangaAI_Service (Python)

../                                 # thư mục cha (docs + SQL scripts)
├─ docs/                            # context, business-rules, functional-requirements, agents/, revision/
├─ README.md, AI_Setup_Guide.md
└─ MangaManagementSystem_Schema.sql, *_Procedures_Views_Bootstrap.sql, SeedData.sql, ...
```

> Một bản CLAUDE.md tổng quát hơn cũng nằm ở working-directory gốc: `D:\SWP391-Project\CLAUDE.md`.

---

## 1. Tech stack & version

| Layer | Công nghệ | Version |
|---|---|---|
| Runtime / SDK | .NET / C# | **net8.0** (SDK đã cài: 8.0.421) |
| Backend API | ASP.NET Core Web API | 8.0 |
| Frontend | Blazor **Server** (Interactive Server) | 8.0 |
| UI library | **MudBlazor** | 8.0.0 |
| CQRS / Mediator | **MediatR** | 12.4.1 |
| ORM | **Entity Framework Core** (SqlServer) | 8.0.5 |
| EF naming | EFCore.NamingConventions (snake_case) | 8.0.3 |
| Database | **SQL Server** (DB: `MangaManagementDB`) | — |
| File storage | **Cloudinary** (CloudinaryDotNet) | Web 1.29.1 / Infrastructure 1.25.0 |
| Auth | Cookie auth + Google OAuth + JwtBearer | 8.0.x |
| Hashing | BCrypt.Net-Next | 4.0.3 |
| Email | MailKit / MimeKit | 4.16.0 |
| API docs | Swashbuckle (Swagger) | 6.6.2 |
| AI service | **Python FastAPI** (riêng biệt, optional, advisory) | uvicorn, port 8000 |
| AI features | YOLO (ultralytics), EasyOCR, deep-translator | — |

Canvas/annotation dự kiến dùng Fabric.js trên HTML5 Canvas. Không có project test tự động (chưa có `*.Tests`).

---

## 2. Cấu trúc thư mục quan trọng

Kiến trúc **Clean Architecture**, 5 project trong `src/` (Domain ← Application ← Infrastructure ← API/Web):

```
src/
├─ MangaManagementSystem.Domain/         # Entities, Enums, Interfaces, ReadModels, Common — KHÔNG phụ thuộc gì
├─ MangaManagementSystem.Application/     # CQRS (MediatR), DTOs, Interfaces, Mappers, Services, Common/Policies
│   └─ Features/                          # Tổ chức theo actor: Mangaka/ Editor/ Assistant/ EditorialBoard/ ReferenceData/ Series
│       └─ <Area>/<Feature>/Commands|Queries/<Name>/   # mỗi use-case 1 folder: Command/Query + Handler
├─ MangaManagementSystem.Infrastructure/ # EF Core (Persistence/ApplicationDbContext, Configurations), Repositories,
│   │                                    #   stored-procedure wrappers, Cloudinary/SMTP/AI adapters, Migrations
│   └─ ...
├─ MangaManagementSystem.API/            # Controllers (thin) theo actor: Controllers/Mangaka, Editor, Assistant
│   │                                    #   + IMediator.Send; Program.cs cấu hình API
│   └─ ...
├─ MangaManagementSystem.Web/            # Blazor Server (UI Mangaka đang phát triển)
│   ├─ Components/Pages/{Mangaka,Editor,Assistant,Admin,Series,...}.razor
│   ├─ Services/Api/                      # Typed API clients: I<Name>ApiClient + <Name>ApiClient
│   └─ Program.cs                         # DI, auth cookie/Google, đăng ký HttpClient typed clients
└─ MangaAI_Service/                       # service Python AI riêng (xem ../AI_Setup_Guide.md). venv/ BỎ QUA.
```

Khác: `HashApp/`, `CreateTestSeries/` — console util nhỏ. Quy ước AI agent ở `../docs/agents/` (**`AGENTS.md`**, **`SESSION_RULE.md`** là nguồn quy tắc chính; `Csharp_Coding_Conventions.md` là chuẩn code). Session note/handoff theo ngày ở `../docs/revision/`.

---

## 3. Pattern đang dùng (naming & structure)

### 3.1 Luồng kiến trúc bắt buộc cho workflow mới
```
Blazor Razor component
  → Typed API client (Web/Services/Api/I<Name>ApiClient)
  → API Controller (thin)
  → IMediator.Send(command/query)
  → Application Handler (CQRS)
  → Infrastructure Repository / stored-procedure wrapper
  → SQL Server (stored procedure) hoặc EF read query
```
- **KHÔNG** gọi tắt: Razor → Application service → DbContext/SP. (Pattern cũ này tồn tại dạng *transitional debt* — không thêm mới; khi sửa page đã migrate thì route qua typed API client.)
- **Command** = thao tác ghi/đổi trạng thái + audit. **Query** = chỉ đọc (không ghi DB trong query handler).
- Có **2 pattern song song**: (a) CQRS/MediatR (mới, ưu tiên — vd `Features/Mangaka/Series/Commands/CreateSeriesDraft/...`) và (b) Application `Service` cũ (vd `IChapterPageTaskService`, đăng ký trong `Application/DependencyInjection.cs`). **Code mới dùng CQRS.**

### 3.2 Auth tạm thời (transitional)
- Web host sở hữu identity qua **cookie**; gọi API thì forward header **`X-Actor-User-Id`** = GUID user đang đăng nhập.
- Controller đọc actor qua header này (`TryResolveActorUserId`). Đây CHƯA phải auth cuối cùng — đừng coi là bảo mật thật.

### 3.3 Naming (theo `../docs/agents/Csharp_Coding_Conventions.md`)
- Entity ID kiểu DB `uniqueidentifier` → **`Guid`** (không dùng `int`/`long`). Ngoại lệ: audit id giữ kiểu DB (vd `long`/BIGINT).
- Param ID camelCase (`seriesId`); DTO property PascalCase (`SeriesId`).
- SQL param trong C# giữ đúng tên SP: `@user_id`, `SqlDbType.UniqueIdentifier`, optional → `DBNull.Value`.
- Class/method/property/DTO/enum = **PascalCase**; param/local = camelCase; private field = **`_camelCase`**.
- Interface `I...`; async method kết thúc `Async`; `CancellationToken cancellationToken` là **tham số cuối**.
- File-scoped namespace; `using` ngoài namespace; 4 spaces; **Allman braces**; ~120 cols.
- DTO/Command suffix: `...Dto`, `...RequestDto`, `...Command`, `...Query`, `...Handler`. Repository = `<Entity>Repository`, Service = `<Feature>Service`, Controller = `<Feature>Controller`.

### 3.4 Database / EF / stored procedure
- 3 schema: **`auth`**, **`manga`**, **`audit`**. PK = `UNIQUEIDENTIFIER DEFAULT NEWID()`. Cột DB **snake_case** (EFCore.NamingConventions tự map).
- Trạng thái lưu dạng **string `status_code`** + CHECK constraint (vd `PENDING_APPROVAL`, `PROPOSAL_DRAFT`, `UNDER_REVIEW`...). Không dùng bảng status-history riêng.
- Workflow đa bảng / status transition / link FileResource / audit → **stored procedure** (`usp_*`), gọi qua wrapper trong Infrastructure (`CommandType.StoredProcedure`, `SqlParameter` typed). Dịch lỗi SQL đã biết → message an toàn (xem `MapSqlException` theo `ex.Number`).
- EF Core chỉ dùng cho **read model** (list/dashboard/filter), ưu tiên `AsNoTracking`.

### 3.5 UI (MudBlazor)
- Page data-driven: dùng typed API client, có **loading / error (+retry) / empty state**, không hardcode mock row, không fake count.
- Giữ route an toàn + return URL (chỉ cho path local tin cậy như `/editor`, `/series/...`; chặn URL ngoài). Route editor là `/editor` (KHÔNG `/editor/dashboard`).

### 3.6 Cloudinary + FileResource
- Cloudinary giữ file thật; SQL giữ metadata qua **`manga.FileResource`**. Bảng nghiệp vụ tham chiếu **`file_resource_id`**, KHÔNG nhúng URL Cloudinary trực tiếp.
- Upload Cloudinary TRƯỚC, rồi SP link atomically. Nếu SP fail sau upload → **best-effort cleanup** Cloudinary. Không giữ transaction SQL mở trong lúc upload.
- `sha256_hash` bắt buộc cho mọi FileResource. `file_purpose_code`: `SERIES_PROPOSAL`, `SERIES_COVER`, `CHAPTER_PAGE_VERSION`, `EDITORIAL_ATTACHMENT`, `REGISTRATION_PORTFOLIO`, `USER_AVATAR`.

---

## 4. Những gì KHÔNG được thay đổi

1. **Cấu trúc database** (`../MangaManagementSystem_Schema.sql`, các stored procedure, schema `auth`/`manga`/`audit`) — đã được nhóm thiết kế. KHÔNG đổi bảng/cột/SP trừ khi chủ repo yêu cầu rõ ràng.
2. **Cấu trúc Cloudinary** (cách lưu file, `file_purpose_code`, mapping FileResource) — KHÔNG đổi.
3. **Mô hình dữ liệu đã chốt** (tránh "diễn giải sai" — xem `../docs/context.md` §11):
   - Genre/Tag **chuẩn hóa** qua `manga.Genre`/`SeriesGenre`, `manga.Tag`/`SeriesTag`. KHÔNG thêm lại `Series.genre`, `SeriesProposal.genre_snapshot`/`tag_snapshot`/cover snapshot.
   - `SeriesProposal` chỉ snapshot: title, synopsis, proposal file, version, status (KHÔNG snapshot genre/tag/cover).
   - `series_id` = identity nội bộ; `slug` = URL identity (chỉ truy cập `/series/{slug}` cho status hợp lệ theo `SeriesNavigationPolicy`).
   - Chapter submit = đổi `Chapter.status_code = UNDER_REVIEW` (KHÔNG tạo bảng `ChapterSubmission`).
   - Board result tính từ `SeriesBoardPoll` + `SeriesBoardVote` (KHÔNG có `SeriesBoardDecision`).
   - Annotation qua `ChapterPageAnnotation` + `ChapterPageAnnotationRegion` (KHÔNG lưu tọa độ/`page_region_id` trực tiếp).
4. **KHÔNG tự ý**: đổi package version, đổi luồng auth, đổi config/secrets, commit/push, force push, reset, xóa hàng loạt, drop DB object, chạy migration phá hủy, ghi đè file dirty không liên quan. → Phải giải thích rủi ro + hỏi trước (xem `../docs/agents/SESSION_RULE.md`).
5. **Out of scope MVP** (đừng tự thêm): public reader portal/accounts, payment/payroll/salary/e-commerce, full drawing editor, generic status-history tables, `ChapterTranslation`/`PageRegionTranslation`, AI tự động duyệt/từ chối page/chapter/proposal/board.
6. **Ranh giới Clean Architecture**: Domain không tham chiếu EF/SQL/Cloudinary/UI; Application không phụ thuộc concrete Infrastructure; API controller mỏng (không EF/SP/Cloudinary trực tiếp); Web không inject Application service / MediatR / DbContext trực tiếp cho workflow mới.
7. **Bảo mật**: KHÔNG in/commit connection string, Cloudinary secret, SMTP cred, JWT secret, OTP, password, token. Secrets dùng **.NET User Secrets** (`setup-secrets.ps1`), không phải appsettings.

---

## 5. Lệnh hay dùng

> ⚠️ **Build CLI:** SDK đã cài là **.NET 8** nhưng solution là **`.slnx`** → `dotnet build/sln` trên `.slnx` **báo lỗi "Expected file header not found"**. Hai cách dùng được:
> - **Visual Studio 2022** (17.10+): mở `MangaManagementSystem.slnx` trực tiếp (cách nhóm đang dùng).
> - **CLI:** build từng project host (kéo theo các project được tham chiếu).

Chạy từ thư mục này (solution root):

```powershell
# Restore + build (build cả 2 host là phủ hết Domain/Application/Infrastructure)
dotnet restore src/MangaManagementSystem.API/MangaManagementSystem.API.csproj
dotnet build   src/MangaManagementSystem.API/MangaManagementSystem.API.csproj --no-incremental
dotnet build   src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj --no-incremental

# Chạy API (Swagger) — http://localhost:5234 ; https://localhost:7256
dotnet run --project src/MangaManagementSystem.API

# Chạy Web (Blazor) — http://localhost:5244 ; https://localhost:7182
dotnet run --project src/MangaManagementSystem.Web

# (Tùy chọn) format theo convention
dotnet format src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj
```

Baseline build đã biết: **0 errors, ~60 warnings pre-existing**. Chỉ coi là "warning mới" nếu nó trỏ tới file vừa sửa/thêm.

**Test:** chưa có project test tự động → verify bằng build + smoke test thủ công (đừng claim "đã test" nếu chưa chạy thật).

**Secrets (local, lần đầu):**
```powershell
./setup-secrets.ps1   # nếu lỗi execution policy: Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

**AI service (Python, optional)** — từ `src/MangaAI_Service` (xem `../AI_Setup_Guide.md`):
```powershell
venv\Scripts\Activate.ps1
uvicorn main:app --reload --port 8000     # http://127.0.0.1:8000
```

**Kiểm tra port trước khi chạy:**
```powershell
Get-NetTCPConnection -LocalPort 5234,7256,5244,7182,8000 -ErrorAction SilentlyContinue |
  Format-Table -AutoSize LocalAddress,LocalPort,State,OwningProcess
```

**Cấu hình liên kết** (`src/MangaManagementSystem.Web/appsettings.json`): `ApiSettings:BaseUrl=http://localhost:5234`, `AiService:BaseUrl=http://localhost:8000`. DB mặc định local: `Server=localhost;Database=MangaManagementDB`.

---

## 6. Quy trình làm việc kỳ vọng (từ docs/agents)

- Đầu task có ý nghĩa: đọc context (`../docs/context.md`, `../docs/business-rules.md`, `../docs/functional-requirements.md`, handoff mới nhất trong `../docs/revision/`), kiểm tra code hiện có trước khi tạo file mới.
- Task có ý nghĩa nên tạo/cập nhật **session note** trong `../docs/revision/` (live: `_CURRENT_SESSION.md`; đóng: `yyyy-MM-dd-<slug>.md`). Ghi: branch, scope, files đổi, luồng kiến trúc, DB/SP impact, kết quả build, smoke test, follow-ups.
- Khi tài liệu mâu thuẫn: ưu tiên (1) chỉ thị user trong session → (2) docs mới nhất trong `../docs/` → (3) handoff mới nhất → (4) code hiện tại → (5) handoff cũ.
- Ưu tiên edit nhỏ, đúng phạm vi; không refactor/format lan man các file không liên quan.
