import os
import cv2
import numpy as np
import base64
from fastapi import FastAPI, File, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, HTMLResponse
from deep_translator import GoogleTranslator
from ultralytics import YOLO
from huggingface_hub import hf_hub_download
from manga_ocr import MangaOcr
from PIL import Image

app = FastAPI(title="Manga AI Microservice v2 (HuggingFace + manga-ocr)")

@app.get("/")
async def get_test_ui():
    with open("test_ui.html", "r", encoding="utf-8") as f:
        return HTMLResponse(f.read())

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ══════════════════════════════════════════════════════════════
# KHỞI TẠO MODEL
# ══════════════════════════════════════════════════════════════

# 1. YOLO chuyên dụng cho Comic/Manga (từ HuggingFace)
print("⏳ Đang tải model nhận diện bong bóng thoại từ HuggingFace...")
try:
    model_path = hf_hub_download(
        repo_id="ogkalu/comic-speech-bubble-detector-yolov8m",
        filename="comic-speech-bubble-detector.pt"
    )
    yolo_model = YOLO(model_path)
    print("✅ Đã tải model nhận diện bong bóng thành công!")
except Exception as e:
    print(f"❌ Lỗi tải model YOLO: {e}")
    yolo_model = None

# 2. Manga-OCR chuyên đọc chữ manga tiếng Nhật
print("⏳ Đang tải model Manga-OCR (lần đầu sẽ tải ~400MB)...")
try:
    mocr = MangaOcr()
    print("✅ Đã tải Manga-OCR thành công!")
except Exception as e:
    print(f"❌ Lỗi tải Manga-OCR: {e}")
    mocr = None


# ══════════════════════════════════════════════════════════════
# HÀM XỬ LÝ
# ══════════════════════════════════════════════════════════════

def detect_speech_bubbles(image_bgr):
    """
    Dùng model YOLOv8m chuyên dụng cho comic/manga
    để tìm bong bóng thoại. Có bộ lọc loại bỏ panel/frame bị nhầm.
    """
    if not yolo_model:
        return []

    img_h, img_w = image_bgr.shape[:2]
    img_area = img_h * img_w

    # In ra tên các class mà model nhận diện được (chỉ in 1 lần)
    if hasattr(yolo_model, 'names'):
        print(f"📋 Model classes: {yolo_model.names}")

    results = yolo_model.predict(source=image_bgr, conf=0.45, iou=0.45, verbose=False)

    regions = []
    if len(results) > 0:
        boxes = results[0].boxes
        for box in boxes:
            x1, y1, x2, y2 = box.xyxy[0].tolist()
            conf = float(box.conf[0])
            cls_id = int(box.cls[0]) if box.cls is not None else 0

            ix1 = max(0, int(x1))
            iy1 = max(0, int(y1))
            ix2 = min(img_w, int(x2))
            iy2 = min(img_h, int(y2))

            bw = ix2 - ix1
            bh = iy2 - iy1
            box_area = bw * bh

            # ── Bộ lọc ──
            # Bỏ vùng quá lớn (> 25% diện tích ảnh) → đó là panel, không phải bong bóng
            if box_area > img_area * 0.25:
                continue

            # Bỏ vùng quá nhỏ (nhiễu)
            if box_area < 2500:
                continue

            # Bỏ vùng có tỷ lệ cạnh quá mất cân đối (dải ngang/dọc dài)
            aspect = bw / max(bh, 1)
            if aspect > 6 or aspect < 0.15:
                continue

            regions.append({
                "x": ix1,
                "y": iy1,
                "width": bw,
                "height": bh,
                "confidence": round(conf, 3),
                "class_id": cls_id,
                "typeCode": "SPEECH_BUBBLE",
            })

    return regions


def ocr_manga_region(roi_bgr):
    """
    Dùng manga-ocr để đọc chữ tiếng Nhật từ vùng ảnh crop.
    manga-ocr chuyên biệt cho manga: đọc được chữ dọc, furigana, font lạ.
    """
    if not mocr:
        return ""

    # Chuyển BGR (OpenCV) → RGB (PIL)
    roi_rgb = cv2.cvtColor(roi_bgr, cv2.COLOR_BGR2RGB)
    pil_img = Image.fromarray(roi_rgb)

    try:
        text = mocr(pil_img)
        return text.strip()
    except:
        return ""


def translate_text(text, target_lang):
    """
    Dịch text manga tiếng Nhật sang ngôn ngữ đích, ổn định hơn bản cũ:
    - Ép source='ja' trước (đáng tin hơn auto-detect với đoạn chữ ngắn của manga).
    - Nếu lỗi thì fallback sang auto-detect.
    - Nếu vẫn không dịch được thì TRẢ LẠI text gốc (không để bong bóng trống).
    Bỏ ký tự xuống dòng do OCR chèn để dịch mạch hơn.
    """
    if not text or not text.strip():
        return ""

    cleaned = " ".join(text.split())
    for src in ("ja", "auto"):
        try:
            out = GoogleTranslator(source=src, target=target_lang).translate(cleaned)
            if out and out.strip():
                return out.strip()
        except Exception:
            continue
    return cleaned


def translate_texts(texts, target_lang):
    """
    Dịch CẢ TRANG trong một request để có ngữ cảnh (tốt hơn dịch từng bong bóng rời).
    Nối các đoạn không rỗng bằng xuống dòng → dịch 1 lần → tách lại theo dòng.
    Nếu số dòng trả về không khớp (Google gộp/tách dòng) thì fallback dịch từng đoạn.
    Trả về list cùng độ dài input; đoạn rỗng giữ rỗng.
    """
    results = [""] * len(texts)
    idxs = [i for i, t in enumerate(texts) if t and t.strip()]
    if not idxs:
        return results

    cleaned = [" ".join(texts[i].split()) for i in idxs]

    # Chỉ 1 đoạn thì không cần gom
    if len(cleaned) == 1:
        results[idxs[0]] = translate_text(texts[idxs[0]], target_lang)
        return results

    joined = "\n".join(cleaned)
    for src in ("ja", "auto"):
        try:
            out = GoogleTranslator(source=src, target=target_lang).translate(joined)
            if not out:
                continue
            parts = [p.strip() for p in out.split("\n")]
            if len(parts) == len(cleaned):
                for k, i in enumerate(idxs):
                    results[i] = parts[k] if parts[k] else cleaned[k]
                return results
        except Exception:
            continue

    # Fallback: dịch từng đoạn riêng (vẫn hơn để trống)
    for i in idxs:
        results[i] = translate_text(texts[i], target_lang)
    return results


def clean_bubble(roi_bgr):
    """
    Xóa chữ trong bong bóng thoại manga và tô lại màu nền đều.

    FIX v2 (so với bản cũ):
    1. Clamp text_mask trong filled_bubble SAU KHI dilate → không tràn ra viền bong bóng.
    2. filled_bubble ưu tiên component gần tâm ROI thay vì lấy component lớn nhất tuyệt đối
       → tránh bắt nhầm background scene trắng lớn hơn bong bóng.
    3. Shrink filled_bubble 2px trước khi dùng → đảm bảo không nuốt nét viền bong bóng.
    4. bg_color lấy từ 4 góc ROI (vùng chắc chắn là nền bong bóng) thay vì gray > 200
       → đúng màu nền kể cả khi bong bóng có màu hồng/xanh nhạt.
    5. Giảm dilate kernel từ 5×5 xuống 3×3 → ít phình hơn.
    """
    h, w = roi_bgr.shape[:2]
    gray = cv2.cvtColor(roi_bgr, cv2.COLOR_BGR2GRAY)
    clean_roi = roi_bgr.copy()

    # ── BƯỚC 1: TÌM VÙng CHỮ (REGION MASK) ────────────────────────────────
    # Threshold lấy nét tối (chữ manga màu đen)
    _, dark_mask = cv2.threshold(gray, 210, 255, cv2.THRESH_BINARY_INV)

    # Bào mòn để loại speed lines mỏng, giữ lõi chữ dày
    core_mask = cv2.erode(dark_mask, np.ones((3, 3), np.uint8), iterations=1)
    pts = cv2.findNonZero(core_mask)

    region_mask = np.zeros((h, w), dtype=np.uint8)
    if pts is not None:
        x_r, y_r, w_r, h_r = cv2.boundingRect(pts)
        padding = 15
        x_r = max(0, x_r - padding)
        y_r = max(0, y_r - padding)
        w_r = min(w - x_r, w_r + padding * 2)
        h_r = min(h - y_r, h_r + padding * 2)
        cv2.rectangle(region_mask, (x_r, y_r), (x_r + w_r, y_r + h_r), 255, -1)
    else:
        region_mask.fill(255)

    # ── BƯỚC 2: TÌM VÙNG BONG BÓNG (FILLED_BUBBLE MASK) ───────────────────
    # [FIX #2] Ưu tiên component trắng GẦN TÂM ROI thay vì lớn nhất tuyệt đối
    # → tránh bắt nhầm vùng trắng của scene/background rộng hơn bong bóng
    white_mask = (gray > 200).astype(np.uint8) * 255

    num_labels_w, labels_w, stats_w, centroids_w = cv2.connectedComponentsWithStats(
        white_mask, connectivity=4
    )

    cx_roi, cy_roi = w // 2, h // 2
    best_label = 0
    best_score = -1.0

    for i in range(1, num_labels_w):
        area = stats_w[i, cv2.CC_STAT_AREA]
        if area < (w * h * 0.05):   # bỏ qua component quá nhỏ (nhiễu)
            continue
        # Khoảng cách centroid → tâm ROI (càng gần tâm càng ưu tiên)
        cx = centroids_w[i][0]
        cy = centroids_w[i][1]
        dist = ((cx - cx_roi) ** 2 + (cy - cy_roi) ** 2) ** 0.5
        # Score = area / (dist + 1): lớn + gần tâm → score cao
        score = area / (dist + 1.0)
        if score > best_score:
            best_score = score
            best_label = i

    filled_bubble = np.zeros((h, w), dtype=np.uint8)
    if best_label > 0 and best_score > 0:
        bubble_bg_mask = (labels_w == best_label).astype(np.uint8) * 255
        contours, _ = cv2.findContours(
            bubble_bg_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
        )
        cv2.drawContours(filled_bubble, contours, -1, 255, -1)

        # Bổ sung region_mask để lấp 'vịnh' do chữ chạm viền tạo ra
        filled_bubble = cv2.bitwise_or(filled_bubble, region_mask)

        # Nới lỏng nhẹ 3×3 (không tràn viền)
        filled_bubble = cv2.dilate(
            filled_bubble, np.ones((3, 3), np.uint8), iterations=1
        )

        # [FIX #3] Shrink 2px để không nuốt nét viền bong bóng khi dùng làm clamp
        shrink_kernel = np.ones((5, 5), np.uint8)   # erode 5×5 ≈ shrink ~2px
        filled_bubble_safe = cv2.erode(filled_bubble, shrink_kernel, iterations=1)
    else:
        filled_bubble.fill(255)
        filled_bubble_safe = filled_bubble.copy()

    # ── BƯỚC 3: PHÁT HIỆN CHỮ QUA CONNECTED COMPONENTS ────────────────────
    num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(
        dark_mask, connectivity=8
    )

    text_mask = np.zeros((h, w), dtype=np.uint8)

    for i in range(1, num_labels):
        x, y, cw, ch, area = stats[i]

        # LỌC 1: Viền bong bóng rỗng (Hollow Ring)
        fill_ratio = area / (cw * ch) if cw * ch > 0 else 0
        if cw > w * 0.5 and ch > h * 0.5 and fill_ratio < 0.15:
            continue

        # LỌC 2: Đường viền mỏng sát mép (Line Segments)
        if x <= 1 or y <= 1 or (x + cw) >= w - 1 or (y + ch) >= h - 1:
            if ch > 0 and cw > 0:
                aspect_ratio = max(cw / ch, ch / cw)
                if aspect_ratio > 3.5 and (cw > w * 0.25 or ch > h * 0.25):
                    continue

        # LỌC 3: Mảng đen đặc quá lớn (tóc, nền đen manga art)
        if ch > h * 0.9 and cw > w * 0.9 and fill_ratio > 0.6:
            continue

        text_mask[labels == i] = 255

    # Cắt bỏ nét ngoài khu vực chữ và ngoài bong bóng
    text_mask = cv2.bitwise_and(text_mask, region_mask)
    text_mask = cv2.bitwise_and(text_mask, filled_bubble)

    # [FIX #5] Dilate nhỏ hơn (3×3 thay vì 5×5) để ăn anti-alias nhưng không tràn
    kernel = np.ones((3, 3), np.uint8)
    text_mask = cv2.dilate(text_mask, kernel, iterations=1)

    # [FIX #1] Clamp lại trong filled_bubble_safe SAU KHI dilate
    # → đảm bảo không bao giờ tràn ra viền bong bóng
    text_mask = cv2.bitwise_and(text_mask, filled_bubble_safe)

    # ── BƯỚC 4: TÍNH MÀU NỀN TỪ 4 GÓC ROI ─────────────────────────────────
    # [FIX #4] Góc ROI là vùng chắc chắn là nền bong bóng (không bao giờ có chữ ở 4 góc)
    # → đúng màu kể cả bong bóng hồng/xanh nhạt/xám
    margin = max(4, min(w, h) // 10)
    corners = np.concatenate([
        roi_bgr[:margin,  :margin ].reshape(-1, 3),   # góc trên trái
        roi_bgr[:margin,  -margin:].reshape(-1, 3),   # góc trên phải
        roi_bgr[-margin:, :margin ].reshape(-1, 3),   # góc dưới trái
        roi_bgr[-margin:, -margin:].reshape(-1, 3),   # góc dưới phải
    ])

    # Lọc bỏ pixel quá tối (< 180) để không lấy nhầm shadow/nét vẽ ở góc
    bright_corners = corners[np.max(corners, axis=1) > 180]

    if len(bright_corners) > 10:
        bg_color = np.median(bright_corners, axis=0).astype(np.uint8)
    else:
        # Fallback: lấy median toàn bộ pixel sáng trong ROI
        all_bright = roi_bgr[gray > 200]
        if len(all_bright) > 0:
            bg_color = np.median(all_bright, axis=0).astype(np.uint8)
        else:
            bg_color = np.array([255, 255, 255], dtype=np.uint8)

    # ── BƯỚC 5: TÔ MÀU NỀN LÊN VÙNG CHỮ ──────────────────────────────────
    clean_roi[text_mask == 255] = bg_color

    return clean_roi


# ══════════════════════════════════════════════════════════════
# API ENDPOINTS
# ══════════════════════════════════════════════════════════════

@app.post("/api/ai/segment")
async def segment_manga(file: UploadFile = File(...)):
    """API 1: Tìm vùng bong bóng thoại."""
    image_bytes = await file.read()
    nparr = np.frombuffer(image_bytes, np.uint8)
    image_bgr = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

    try:
        regions = detect_speech_bubbles(image_bgr)
        return {
            "status": "success",
            "total_regions": len(regions),
            "regions": regions
        }
    except Exception as e:
        import traceback; traceback.print_exc()
        return JSONResponse(status_code=500, content={"error": str(e)})


@app.post("/api/ai/clean-and-translate")
async def clean_and_translate(file: UploadFile = File(...)):
    """API 2: OCR + Dịch + Xóa chữ."""
    image_bytes = await file.read()
    nparr = np.frombuffer(image_bytes, np.uint8)
    image_bgr = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    clean_bgr = image_bgr.copy()

    try:
        regions = detect_speech_bubbles(image_bgr)
        translated_results = []
        original_texts = []

        for i, reg in enumerate(regions):
            x, y, w, h = reg["x"], reg["y"], reg["width"], reg["height"]
            y2 = min(image_bgr.shape[0], y + h)
            x2 = min(image_bgr.shape[1], x + w)

            # Shrink bounding box 3px để không bao hàm cảnh vật ngoài viền bong bóng
            shrink = 3
            x_s = x + shrink
            y_s = y + shrink
            x2_s = x2 - shrink
            y2_s = y2 - shrink

            # Đảm bảo box sau khi shrink vẫn hợp lệ (không bị âm)
            if x_s >= x2_s or y_s >= y2_s:
                x_s, y_s, x2_s, y2_s = x, y, x2, y2

            roi = image_bgr[y_s:y2_s, x_s:x2_s]

            # ── 1. OCR bằng manga-ocr (đọc trên vùng đầy đủ, không co rìa) ──
            ocr_roi = image_bgr[y:y2, x:x2]
            original_text = ocr_manga_region(ocr_roi)
            original_texts.append(original_text)

            translated_results.append({
                "pageRegionId": i + 1,
                "x": x, "y": y, "width": w, "height": h,
                "originalText": original_text,
                "translatedText": "",
            })

            # ── 2. Tô trắng bên trong bong bóng (xóa chữ sạch) ──
            if original_text:
                clean_roi = clean_bubble(roi)
                clean_bgr[y_s:y2_s, x_s:x2_s] = clean_roi

        # ── 3. Dịch GOM cả trang trong MỘT request để có ngữ cảnh ──
        translations = translate_texts(original_texts, 'vi')
        for res, tr in zip(translated_results, translations):
            res["translatedText"] = tr

        _, buffer = cv2.imencode('.png', clean_bgr)
        clean_base64 = base64.b64encode(buffer).decode('utf-8')

        return {
            "status": "success",
            "clean_image_base64": f"data:image/png;base64,{clean_base64}",
            "regions": translated_results
        }
    except Exception as e:
        import traceback; traceback.print_exc()
        return JSONResponse(status_code=500, content={"error": str(e)})


from pydantic import BaseModel
from typing import List, Optional

class RegionInput(BaseModel):
    id: Optional[int] = None
    x: int
    y: int
    width: int
    height: int

class TranslateSelectedRequest(BaseModel):
    image_base64: str
    regions: List[RegionInput]
    target_lang: Optional[str] = "vi"  # "vi" (Vietnamese) or "en" (English)

@app.post("/api/ai/translate-selected")
async def translate_selected(req: TranslateSelectedRequest):
    """API 3: Dịch chỉ các vùng được người dùng chọn."""
    try:
        # Resolve target language (whitelist; default Vietnamese)
        target_lang = (req.target_lang or "vi").lower()
        if target_lang not in ("vi", "en"):
            target_lang = "vi"

        # Decode ảnh từ base64
        header, data = req.image_base64.split(",", 1) if "," in req.image_base64 else ("", req.image_base64)
        img_bytes = base64.b64decode(data)
        nparr = np.frombuffer(img_bytes, np.uint8)
        image_bgr = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if image_bgr is None:
            return JSONResponse(status_code=400, content={"error": "Could not decode the uploaded image."})
        clean_bgr = image_bgr.copy()

        translated_results = []
        original_texts = []
        for i, reg in enumerate(req.regions):
            x, y, w, h = reg.x, reg.y, reg.width, reg.height
            y2 = min(image_bgr.shape[0], y + h)
            x2 = min(image_bgr.shape[1], x + w)

            shrink = 3
            x_s = x + shrink
            y_s = y + shrink
            x2_s = x2 - shrink
            y2_s = y2 - shrink

            if x_s >= x2_s or y_s >= y2_s:
                x_s, y_s, x2_s, y2_s = x, y, x2, y2

            roi = image_bgr[y_s:y2_s, x_s:x2_s]

            # OCR đọc trên vùng ĐẦY ĐỦ (không co 3px) để không cắt mất chữ ở rìa bong
            # bóng; vùng co (roi) chỉ dùng cho việc xóa chữ (clean_bubble) bên dưới.
            ocr_roi = image_bgr[y:y2, x:x2]
            original_text = ocr_manga_region(ocr_roi)
            original_texts.append(original_text)

            translated_results.append({
                "id": reg.id,
                "pageRegionId": i + 1,
                "x": x, "y": y, "width": w, "height": h,
                "originalText": original_text,
                "translatedText": "",
            })

            if original_text:
                # Bubble cleaning is best-effort: a failure on one region must not
                # 500 the whole translate request (was the cause of the 500 error).
                try:
                    clean_roi = clean_bubble(roi)
                    clean_bgr[y_s:y2_s, x_s:x2_s] = clean_roi
                except Exception:
                    import traceback; traceback.print_exc()

        # Dịch GOM cả trang trong MỘT request để có ngữ cảnh, rồi gán lại từng bong bóng.
        translations = translate_texts(original_texts, target_lang)
        for res, tr in zip(translated_results, translations):
            res["translatedText"] = tr

        _, buffer = cv2.imencode('.png', clean_bgr)
        clean_base64 = base64.b64encode(buffer).decode('utf-8')

        return {
            "status": "success",
            "clean_image_base64": f"data:image/png;base64,{clean_base64}",
            "regions": translated_results
        }
    except Exception as e:
        import traceback; traceback.print_exc()
        return JSONResponse(status_code=500, content={"error": str(e)})

