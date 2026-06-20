# Hướng dẫn Cài đặt & Chạy Manga AI API (Local)

Để chạy được model AI cho tính năng bóc tách hình ảnh và dịch thuật, các bạn làm theo các bước sau nhé:

## Bước 1: Cài đặt Python (Nếu máy chưa có)

- Tải và cài đặt Python (khuyên dùng bản **3.10** hoặc **3.11**) từ trang chủ [python.org](https://www.python.org/).
- ⚠️ **Lưu ý cực kỳ quan trọng:** Ở màn hình cài đặt đầu tiên, bắt buộc phải tick vào ô **"Add Python to PATH"** trước khi bấm Install.

## Bước 2: Mở thư mục chứa code AI

- Sử dụng Terminal (hoặc Command Prompt / PowerShell / Git Bash) và di chuyển (`cd`) vào thư mục chứa code AI (thư mục có chứa file `main.py` và model `best.pt`).
- Ví dụ: `cd đường_dẫn_đến_thư_mục_src_MangaAI_Service`

## Bước 3: Tạo môi trường ảo (Virtual Environment - venv)

Việc này giúp cài thư viện mà không bị rác máy. Chạy lệnh sau:
```bash
python -m venv venv
```
*(Đợi 1 chút để nó tạo ra thư mục `venv`)*

## Bước 4: Kích hoạt môi trường ảo

Phải kích hoạt venv thì cài thư viện mới vào đúng chỗ. Chạy lệnh tương ứng với hệ điều hành của bạn:

- **Windows (Command Prompt / CMD):**
  ```bash
  venv\Scripts\activate.bat
  ```
- **Windows (PowerShell):**
  ```bash
  venv\Scripts\Activate.ps1
  ```
  *(Nếu PowerShell báo lỗi không cho chạy script, hãy mở PowerShell bằng quyền Admin và chạy lệnh `Set-ExecutionPolicy Unrestricted`, bấm `Y` rồi quay lại kích hoạt venv).*

- **Mac/Linux:**
  ```bash
  source venv/bin/activate
  ```

*Dấu hiệu nhận biết thành công: Dòng lệnh hiện tại sẽ có chữ `(venv)` ở đầu.*

## Bước 5: Cài đặt các thư viện cần thiết

Vì hiện tại thư mục chưa có file `requirements.txt`, hãy copy nguyên dòng lệnh sau dán vào terminal để cài tất cả các công cụ (FastAPI, YOLO, EasyOCR, dịch thuật...):
```bash
pip install fastapi uvicorn python-multipart ultralytics Pillow easyocr deep-translator
```

## Bước 6: Khởi chạy AI Server

Sau khi cài xong, chạy lệnh sau để bật API server:
```bash
uvicorn main:app --reload --port 8000
```
Nếu thấy Terminal báo `Uvicorn running on http://127.0.0.1:8000 (Press CTRL+C to quit)` là AI đã chạy thành công! Lúc này Backend C# và giao diện Web (Workspace) mới có thể gọi sang AI để chạy bóc tách/dịch được.

---

## 🔁 Hướng dẫn mở lại Server (Cho các lần code tiếp theo)

Khi bạn đã cài đặt xong các bước ở trên, thư viện đã được lưu đầy đủ vào thư mục `venv`. Nếu bạn lỡ **tắt Terminal** hoặc **khởi động lại máy**, bạn chỉ cần chạy lại các lệnh cực kỳ ngắn gọn sau:

**Bước 1: Di chuyển vào thư mục code (nếu mở Terminal mới):**
```bash
cd đường_dẫn_đến_thư_mục_src_MangaAI_Service
```

**Bước 2: Kích hoạt lại môi trường ảo:**
- *Trên Windows (CMD):* `venv\Scripts\activate.bat`
- *Trên Windows (PowerShell):* `venv\Scripts\Activate.ps1`
- *Trên Mac/Linux:* `source venv/bin/activate`

**Bước 3: Chạy lại server:**
```bash
uvicorn main:app --reload --port 8000
```
*(Tuyệt đối **KHÔNG** cần chạy lại lệnh `pip install` hay `python -m venv` nữa nhé).*
