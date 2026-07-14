# Assistant Workflow

## Mục đích

Tài liệu này tách riêng toàn bộ workflow của `Assistant` trong Manga Management System để:

- Dễ đọc hơn so với sơ đồ tổng.
- Dùng làm chuẩn đối chiếu khi dev, test và chỉnh UI/UX.
- Giảm mơ hồ giữa `task`, `workspace`, `chapter`, `page` và `page version`.

## Phạm vi

Workflow này bao phủ 3 nhóm nghiệp vụ chính:

- Mangaka tạo và giao task cho Assistant.
- Assistant mở workspace, thực hiện công việc và submit kết quả.
- Mangaka review bài submit và quyết định `approve`, `return for rework`, `reassign`, hoặc `cancel`.

## Vai trò liên quan

- `Mangaka`: tạo task, giao task, review submission, approve hoặc trả lại.
- `Assistant`: nhận task, mở workspace đúng chapter/page, chỉnh sửa và submit kết quả.
- `System`: điều hướng đúng context, tạo page version, cập nhật task status, lưu note và annotation.

## Thuật ngữ chính

- `Task`: công việc được giao cho Assistant trên một hoặc nhiều region của một page.
- `Assigned page`: page đang chứa vùng được giao task.
- `Workspace`: màn hình thao tác chính để Assistant xem task detail, annotation, chỉnh canvas và submit.
- `Page version`: version ảnh của page được tạo ra sau mỗi lần submit.
- `Submission notes`: ghi chú của Assistant, phải gắn với page version được submit.

## Task Status

Các trạng thái task chính trong flow Assistant:

- `ASSIGNED`: task đã được giao và đang chờ Assistant làm.
- `UNDER_REVIEW`: Assistant đã submit, đang chờ Mangaka review.
- `COMPLETED`: Mangaka đã approve submission.
- `CANCELLED`: task bị hủy.

## Workflow 1: Create Assistant Task

### Mục tiêu

Mangaka tạo một task từ chapter workspace và giao cho một Assistant cụ thể.

### Luồng chuẩn

1. Mangaka mở `chapter workspace`.
2. Mangaka chọn một hoặc nhiều `page regions`.
3. Mangaka nhập:
   - task type
   - task title hoặc mô tả
   - assigned assistant
   - due date
   - compensation nếu có
4. System validate dữ liệu task.
5. Nếu hợp lệ, system tạo task.
6. System gán trạng thái task thành `ASSIGNED`.
7. System phát notification cho Assistant.

### Validation nên có

- Phải chọn ít nhất một region.
- Assistant được chọn phải là contributor hợp lệ của series.
- Due date phải hợp lệ.
- Task phải gắn với đúng page/chapter context.

## Workflow 2: Assistant Open Task and Workspace

### Mục tiêu

Assistant mở task được giao và đi vào đúng chapter/page cần làm, không bị rơi sang chapter khác.

### Luồng chuẩn

1. Assistant đăng nhập.
2. Assistant mở `Dashboard` hoặc `Assigned Tasks`.
3. Assistant chọn task muốn làm.
4. Assistant có thể:
   - xem `Task Detail`
   - bấm `Open Workspace`
5. System resolve task context:
   - `series`
   - `chapter`
   - `page`
   - `assigned regions`
   - `source page version`
6. System điều hướng Assistant vào đúng workspace của task.
7. Workspace phải mở đúng:
   - chapter chứa task
   - page chứa task
   - panel/region được giao nếu có highlight

### Yêu cầu UI/UX

- Assistant phải nhìn thấy khối `Assigned Task` ngay trong workspace.
- Có nút `Jump to assigned page` nếu đang không đứng đúng page.
- Không nên cho Assistant thấy các chapter draft không liên quan đến task của họ.
- Nhưng phải luôn cho Assistant thấy chapter đang chứa task được giao.

## Workflow 3: Assistant Perform Work

### Mục tiêu

Assistant xem yêu cầu, annotation, note và chỉnh sửa đúng nội dung được giao.

### Luồng chuẩn

1. Assistant mở workspace của task.
2. Assistant xem:
   - task description
   - due date
   - priority
   - compensation
   - assigned regions
   - task notes / annotations từ Mangaka
3. Assistant thực hiện chỉnh sửa trên canvas hoặc chuẩn bị file hoàn chỉnh bên ngoài.

### Ghi chú

- Task notes và annotation là đầu vào chính cho vòng rework.
- Assistant cần hiểu rõ mình đang làm trên page nào và version nào.

## Workflow 4: Submit Assistant Task Work

### Mục tiêu

Assistant gửi kết quả công việc dưới dạng page version mới.

### Hai cách submit

Assistant có thể submit theo 1 trong 2 cách:

- `Upload file`: tải lên file ảnh đã hoàn thiện.
- `Submit current canvas`: dùng canvas hiện tại trong workspace để render và submit.

### Luồng chuẩn

1. Assistant mở workspace của task.
2. Assistant vào khu vực `Submit From Workspace`.
3. Assistant chọn một trong hai cách:
   - chọn file để upload
   - không chọn file để system dùng current canvas
4. Assistant nhập `submission notes` nếu cần.
5. System validate submission.
6. Nếu hợp lệ:
   - tạo `new page version`
   - gắn `submission notes` vào page version vừa tạo
   - link page version đó với task
   - đổi task status thành `UNDER_REVIEW`
7. System thông báo submit thành công.
8. Mangaka nhận notification có submission mới.

### Validation nên có

- Task phải đang ở trạng thái `ASSIGNED`.
- Assistant phải là người được giao task đó.
- Chapter/page context phải hợp lệ.
- File type và size phải hợp lệ nếu submit bằng upload.
- Chapter phải cho phép assistant submission theo business rule hiện hành.

### Điều cần chốt rõ trong hệ thống

- Page version được submit có trở thành `current version` ngay hay không.
- Nếu submit bằng canvas, ảnh render ra có giống bản export cuối cùng không.

## Workflow 5: Review Assistant Task Submission

### Mục tiêu

Mangaka review kết quả mà Assistant submit và ra quyết định tiếp theo.

### Luồng chuẩn

1. Mangaka mở task đã submit.
2. Mangaka xem:
   - source page
   - submitted output
   - submission notes
   - task regions
   - annotation liên quan
3. Mangaka chọn 1 trong 4 hướng xử lý:
   - `Approve`
   - `Return for rework`
   - `Reassign`
   - `Cancel`

## Nhánh 5A: Approve

1. Mangaka approve bài làm.
2. System đổi task status thành `COMPLETED`.
3. Assistant nhận notification hoàn thành task.

## Nhánh 5B: Return for Rework

1. Mangaka nhập `revised instructions`.
2. Mangaka có thể tạo thêm annotation hoặc note mới.
3. System giữ nguyên Assistant hiện tại.
4. System đổi task status từ `UNDER_REVIEW` về `ASSIGNED`.
5. Assistant nhận notification yêu cầu sửa lại.
6. Assistant mở lại workspace, xem note/annotation mới và submit lại.

### Ghi chú quan trọng

- Đây là vòng lặp rework chính của Assistant.
- Annotation và task notes phải đủ rõ để Assistant biết cần sửa gì.

## Nhánh 5C: Reassign

1. Mangaka chọn Assistant mới và nhập lý do reassign.
2. System hủy task cũ hoặc đóng task cũ theo rule hệ thống.
3. System tạo `replacement task` cho Assistant mới.
4. System gán task mới về `ASSIGNED`.
5. Assistant cũ không được submit tiếp task cũ.
6. Assistant mới nhận notification và mở workspace từ task mới.

### Khuyến nghị nghiệp vụ

- Nên lưu rõ liên kết giữa task cũ và task thay thế để trace history.

## Nhánh 5D: Cancel

1. Mangaka nhập lý do hủy task.
2. System đổi task status thành `CANCELLED`.
3. Assistant không được submit task này nữa.
4. Lịch sử task vẫn phải được giữ để trace.

## Annotation trong Assistant Flow

Annotation nên đóng vai trò như một phần chính thức của workflow rework:

- Mangaka tạo annotation trên page/version/region.
- Annotation được hiển thị trong task detail hoặc workspace của Assistant.
- Assistant dùng annotation để sửa bài.
- Mangaka có thể resolve annotation khi issue đã được xử lý.

## Notification tối thiểu nên có

System nên phát notification cho các sự kiện sau:

- Task được assign mới.
- Task bị return for rework.
- Task bị reassign.
- Task bị cancel.
- Task được approve.
- Submission mới đang chờ review.

## Quy tắc hiển thị trong Workspace

Để flow Assistant ổn định và dễ test, workspace của Assistant nên tuân theo các rule sau:

- Luôn mở đúng chapter chứa task được giao.
- Luôn có cách quay về đúng assigned page.
- Assistant chỉ thấy chapter đủ dùng cho task của mình.
- Khu vực submit phải nằm ngay trong workspace, không tách rời khó theo dõi.
- Submission notes phải đi kèm page version được submit.
- Task detail, task notes, assigned regions và trạng thái hiện tại phải nhìn thấy ngay trong sidebar hoặc panel chính.

## Đánh giá hiện tại

Dựa trên workflow tổng và trạng thái project hiện tại, flow Assistant đã có đủ phần lõi nghiệp vụ, nhưng để hoàn chỉnh và ít gây lỗi khi dev/test thì cần xem tài liệu này là chuẩn bổ sung cho sơ đồ tổng, đặc biệt ở các điểm:

- `Open workspace đúng chapter/page`
- `Jump to assigned page`
- `Submit current canvas` song song với `upload file`
- `Submission notes gắn với page version`
- `Return for rework` như một vòng lặp rõ ràng
- `Reassign` có trace history và notification đầy đủ

## Đề xuất sử dụng tài liệu này

- Dùng tài liệu này làm chuẩn khi test manual cho role `Assistant`.
- Dùng để đối chiếu khi thiết kế UI task detail và workspace.
- Dùng để tách các test case theo từng nhánh: `assign`, `submit`, `approve`, `rework`, `reassign`, `cancel`.
