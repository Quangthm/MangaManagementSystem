# Manga AI Microservice

Microservice tích hợp Trí tuệ Nhân tạo cho hệ thống **MangaManagementSystem**. 
Dịch vụ này sử dụng mô hình Deep Learning của [SickZil-Machine](https://github.com/KUR-creative/SickZil-Machine) để nhận diện bong bóng thoại (SegNet) và tẩy chữ (ComplNet), kết hợp với **EasyOCR** để đọc chữ.

## ⚠️ Yêu cầu Cài đặt Quan trọng (Prerequisites)

Dịch vụ này sử dụng mô hình AI dạng file `.pb` (TensorFlow) nặng khoảng 200MB. Các file này **chưa có sẵn** trong source code để tránh làm phình to repo.
**Bắt buộc** phải tải model trước khi chạy server, nếu không API sẽ báo lỗi 500.

1. Vào link Release của SickZil-Machine: [Tại đây](https://github.com/KUR-creative/SickZil-Machine/releases/tag/0.1.1-pre2)
2. Tải file `SickZil-Machine-0.1.1-pre2-win64-cpu-eng.zip` (hoặc bản gpu).
3. Giải nén, tìm thư mục `resource/snet` và `resource/cnet`.
4. Copy 2 file `.pb` bên trong và chép đè vào thư mục `d:\FuncAI-Github\SickZil-Machine\resource\` tương ứng trên máy tính của bạn.

## 🚀 Khởi chạy Server

Mở Terminal tại thư mục `MangaAI_Service` và chạy:

```bash
# 1. Tạo môi trường ảo
python -m venv venv
.\venv\Scripts\Activate.ps1

# 2. Cài đặt thư viện
pip install -r requirements.txt

# 3. Chạy server (Port 8000)
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

## 📚 API Endpoints

Hệ thống cung cấp 2 REST API chính:

### `POST /api/ai/segment`
* **Đầu vào:** `multipart/form-data` chứa file ảnh (key: `file`)
* **Chức năng:** Trả về danh sách tọa độ (bounding box) chứa chữ cái dựa trên mô hình SegNet chuyên dụng cho Manga.
* **Đầu ra:** 
```json
{
  "status": "success",
  "total_regions": 1,
  "regions": [
    { "x": 100, "y": 200, "width": 50, "height": 80, "confidence": 0.99, "typeCode": "SPEECH_BUBBLE" }
  ]
}
```

### `POST /api/ai/clean-and-translate`
* **Đầu vào:** `multipart/form-data` chứa file ảnh (key: `file`)
* **Chức năng:** 
  1. Dùng SegNet tìm chữ.
  2. Dùng EasyOCR cắt các vùng chữ và đọc văn bản tiếng Nhật.
  3. Dùng Google Translate dịch sang tiếng Việt.
  4. Dùng ComplNet (Deepfill v2) xóa sạch chữ cái trên nền, khôi phục lại nét vẽ bong bóng.
* **Đầu ra:** Base64 ảnh sạch và JSON tọa độ/bản dịch.
```json
{
  "status": "success",
  "clean_image_base64": "data:image/png;base64,iVBORw0KGgoAAAANSU...",
  "regions": [
    {
      "pageRegionId": 1,
      "x": 100, "y": 200, "width": 50, "height": 80,
      "originalText": "ありがとう",
      "translatedText": "Cảm ơn"
    }
  ]
}
```
