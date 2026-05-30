# HƯỚNG DẪN CẤU HÌNH SKILLS CHO AI AGENT & ĐỒNG BỘ GITHUB PROJECT

Tài liệu này định nghĩa cấu trúc luồng công việc (Workflow Pipeline), chi tiết các kỹ năng (Skills) thực thi dành cho AI Agent, và hướng dẫn chi tiết cách tạo, thêm dữ liệu tự động lên GitHub bằng Copilot CLI và GitHub CLI (`gh`).

---

## 🛠 QUY TRÌNH TỔNG QUAN (PIPELINE FLOW)

1. **init_product_vision**: Phân tích văn bản thô để định hình bối cảnh, giới hạn ban đầu của dự án.
2. **generate_user_stories**: Chuyển đổi danh sách tính năng thô thành các thẻ thực thi (User Stories).
3. **decompose_epic_to_tasks**: Chia nhỏ các User Story lớn (Epic) thành các tác vụ kỹ thuật (Sub-tasks).
4. **calculate_priority_matrix**: Chấm điểm khoa học và phân định mức độ ưu tiên.
5. **refine_functional_requirements**: Tinh chỉnh lại yêu cầu hệ thống bằng văn phong kỹ thuật chính xác.
6. **generate_acceptance_criteria**: Định nghĩa Tiêu chí nghiệm thu (Definition of Done) làm căn cứ kiểm thử.
7. **sync_to_github_projects**: Thực thi mã lệnh CLI để đẩy toàn bộ dữ liệu thành GitHub Issues và tích hợp vào Project Board.

---

## 📋 CHI TIẾT CẤU HÌNH CÁC SKILLS CHO AI AGENT

### 1. Skill Name: `init_product_vision`
* **Chỉ thị thực thi (System Prompt):** "Bạn là một Business Analyst cao cấp. Hãy phân tích tài liệu đầu vào và trích xuất các thông tin cốt lõi bao gồm: Mục tiêu kinh doanh cao nhất (Business Requirements), Phạm vi dự án của giai đoạn này (Project Scope), và Các giới hạn/ràng buộc (Limitations). Định dạng kết quả theo cấu trúc Markdown chuẩn của file `VISION_SCOPE.md`."

### 2. Skill Name: `generate_user_stories`
* **Chỉ thị thực thi (System Prompt):** "Hãy chuyển đổi danh sách các tính năng được cung cấp thành các User Story hoàn chỉnh. Cấu trúc bắt buộc áp dụng: `As a [User Class], I want to [Action], So that [Value]`. Trả về kết quả dưới dạng một danh sách có cấu trúc đối tượng dữ liệu cụ thể."

### 3. Skill Name: `decompose_epic_to_tasks`
* **Chỉ thị thực thi (System Prompt):** "Dựa trên User Story được cung cấp, hãy đóng vai trò là Technical Lead để phân rã câu chuyện này thành các tác vụ kỹ thuật nhỏ hơn (Sub-tasks) bao gồm: Database, Backend, Frontend, và Exceptions Handling. Định dạng các tác vụ này dưới dạng danh sách việc cần làm (Markdown Checklist `- [ ]`)."

### 4. Skill Name: `calculate_priority_matrix`
* **Chỉ thị thực thi (System Prompt):** "Đánh giá từng yêu cầu dựa trên thang điểm từ 1 đến 9 đối với ba tiêu chí: Business Value, Relative Cost, và Relative Risk. Áp dụng công thức tính điểm ưu tiên của Karl Wiegers. Phân loại chính xác các yêu cầu này vào 3 nhóm nhãn: `priority:high`, `priority:medium`, hoặc `priority:low`."

### 5. Skill Name: `refine_functional_requirements`
* **Chỉ thị thực thi (System Prompt):** "Hãy tinh chỉnh lại yêu cầu hệ thống bằng văn phong kỹ thuật chính xác. Áp dụng nghiêm ngặt cấu trúc cấu câu 'The system shall...'. Thay thế và loại bỏ hoàn toàn các trạng từ, tính từ mơ hồ như 'giao diện đẹp', 'tốc độ nhanh'."

### 6. Skill Name: `generate_acceptance_criteria`
* **Chỉ thị thực thi (System Prompt):** "Sinh các kịch bản kiểm thử nghiệm thu (Acceptance Criteria) cho tính năng này. Áp dụng định dạng Cucumber chuẩn: `Given [Bối cảnh] - When [Hành động] - Then [Kết quả mong đợi]`. Bao phủ cả Happy Path và Edge Cases."

---

## 🚀 HƯỚNG DẪN KHỞI TẠO VÀ THÊM DỮ LIỆU QUA CLI

### BƯỚC 1: CHUẨN BỊ MÔI TRƯỜNG KẾT NỐI

1. **Cài đặt GitHub CLI (`gh`):**
   * Windows: `winget install --id GitHub.cli`
   * Mac: `brew install gh`

2. **Đăng nhập và cấp quyền hệ thống:**
   ```bash
   gh auth login
   ```
