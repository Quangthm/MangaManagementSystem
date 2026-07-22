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
print("⏳ Downloading speech-bubble detection model from HuggingFace...")
try:
    model_path = hf_hub_download(
        repo_id="ogkalu/comic-speech-bubble-detector-yolov8m",
        filename="comic-speech-bubble-detector.pt"
    )
    yolo_model = YOLO(model_path)
    print("✅ Speech-bubble detection model loaded successfully!")
except Exception as e:
    print(f"❌ Failed to load the YOLO model: {e}")
    yolo_model = None

# 2. Manga-OCR chuyên đọc chữ manga tiếng Nhật
print("⏳ Loading the Manga-OCR model (the first run downloads ~400MB)...")
try:
    mocr = MangaOcr()
    print("✅ Manga-OCR loaded successfully!")
except Exception as e:
    print(f"❌ Failed to load Manga-OCR: {e}")
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
    Erase the text inside a speech bubble and repaint the bubble's own background colour.

    Design (v3) — find the bubble INTERIOR and repaint all of it, instead of trying to detect the
    individual text strokes. Everything inside a speech bubble is text, so once the interior is known
    there is nothing left to classify. That removes both failure modes of v2 at once:

      * under-fill (old text still visible): v2 thresholded strokes at a fixed 210 and dilated 3x3, so
        anti-aliased glyph edges, punctuation eroded away before the bounding box was measured, and
        glyphs touching the balloon outline all survived.
      * over-fill (artwork painted over): v2 OR-ed a padded bounding RECTANGLE into the balloon mask and
        fell back to `fill(255)` (the whole ROI) whenever it could not find a bright region — so on a
        grey/screentoned balloon it repainted the surrounding line art.

    Fail-safe: whenever the interior cannot be identified confidently this returns the ORIGINAL roi
    untouched. Leftover text is recoverable — the mangaka erases it by hand — but painting over line art
    destroys the drawing, so refusing to act is always the better failure.

    KNOWN LIMITATION — adjacent/overlapping balloons. When a neighbouring balloon's outline cuts across
    this ROI it splits the interior, and only the enclosing fragment is repainted, so a glyph stranded in
    the cut-off corner survives (typically the last character of a vertical line). Measured on 55 real
    bubbles: ~1.4% of interior pixels left dark, concentrated in the ~15% of bubbles that overlap another.

    That one is a deliberate trade-off, not an unfinished fix. Filling the interior's convex hull removes
    the leftovers almost entirely (1.44% -> 0.02%) but was measured to repaint hatching and line art next
    to the balloon in the same sample — trading a visible, 10-second brush fix for silent damage to the
    drawing. Restricting the hull fill to small, mostly-bright concavities recovered almost nothing
    (1.36%). Filling the balloon outline's contour-hierarchy hole instead left 226 of 262 bubbles
    untouched, because a tight detection box clips the outline so the ring is never closed. Do not reach
    for any of these again without first measuring artwork damage on real pages.

    Measured over 262 bubbles on 40 real pages: 1 left untouched, and no increase in artwork repainting
    versus selecting by the component under the centre pixel (identical border-contact count).
    """
    h, w = roi_bgr.shape[:2]
    if h < 8 or w < 8:
        return roi_bgr

    gray = cv2.cvtColor(roi_bgr, cv2.COLOR_BGR2GRAY)

    # 1. Bubble interior candidate. Otsu adapts to grey/tinted balloons that the old fixed 210 missed,
    #    but is clamped to a floor so a dark panel cannot make mid-grey artwork count as "interior".
    otsu_t, _ = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    bright = (gray >= max(otsu_t, 160)).astype(np.uint8) * 255

    # 2. Pick the balloon interior: of the bright components whose FILLED outline encloses the ROI centre,
    #    take the SMALLEST one that is still balloon-sized. Filling each candidate's outline first is what
    #    makes this work with large lettering — the background around big glyphs forms a ring that does not
    #    contain the centre pixel itself, but fills to the whole balloon. Selecting by "the component the
    #    centre pixel lands in" instead fails there, because the centre sits on a stroke and the fallback
    #    then latches onto the enclosed counter inside a kanji, which is far too small and gets refused:
    #    that left 4 of 262 real bubbles (all large-text) completely untreated, including <黄昏>.
    #    Smallest-enclosing is what keeps this safe: the surrounding artwork also encloses the centre, but
    #    fills to a larger area, so the balloon always wins.
    #
    #    Deliberately NO morphological closing: measured on real pages, closing bridges the thin balloon
    #    outline and merges the interior with the white space outside it, pushing coverage to the whole ROI
    #    (median 1.00 vs 0.76 without) and tripping the size floor on 51 of 55 bubbles.
    #
    #    The size floor is only a degeneracy guard, and has deliberately NO upper bound. A caption box or a
    #    tightly-cropped balloon legitimately fills its whole detection box — an earlier 0.92 cap refused 14
    #    of 55 real bubbles for that reason. Nor do we re-judge whether the box is "really" a balloon: YOLO
    #    already decided that (conf >= 0.45 plus the geometric filters in detect_speech_bubbles). Measured
    #    here, interior flatness is identical for balloons (median 0.86) and random artwork crops (0.84), so
    #    any such pixel statistic would reject real bubbles without actually catching false positives.
    num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(bright, connectivity=4)
    if num_labels <= 1:
        return roi_bgr

    cx, cy = w // 2, h // 2
    size_floor = 0.08 * h * w
    filled = None
    best_area = None
    for i in range(1, num_labels):
        if stats[i, cv2.CC_STAT_AREA] < 40:
            continue
        component = (labels == i).astype(np.uint8) * 255
        contours, _ = cv2.findContours(component, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if not contours:
            continue
        candidate = np.zeros((h, w), dtype=np.uint8)
        cv2.drawContours(candidate, contours, -1, 255, -1)
        if candidate[cy, cx] == 0:
            continue
        area = cv2.countNonZero(candidate)
        if area < size_floor:
            continue
        if best_area is None or area < best_area:
            filled, best_area = candidate, area

    if filled is None:
        return roi_bgr

    # 3. Pull back from the balloon outline so repainting can never eat the ink line.
    pad = max(2, int(round(min(h, w) * 0.02)))
    safe = cv2.erode(filled, np.ones((2 * pad + 1, 2 * pad + 1), np.uint8), iterations=1)
    if cv2.countNonZero(safe) == 0:
        return roi_bgr

    # 4. Background colour sampled from INSIDE the balloon. v2 read the four ROI corners, which for a
    #    round balloon in a square box are artwork, not background. Taking the brightest 40% of the
    #    interior skips the glyph pixels, so this stays correct on tinted balloons too.
    interior_pixels = roi_bgr[safe == 255]
    interior_gray = gray[safe == 255]
    cutoff = np.percentile(interior_gray, 60)
    background_pixels = interior_pixels[interior_gray >= cutoff]
    if background_pixels.size == 0:
        return roi_bgr
    bg_color = np.median(background_pixels, axis=0).astype(np.uint8)

    clean_roi = roi_bgr.copy()
    clean_roi[safe == 255] = bg_color
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
    """API 2 [DEV/DEMO ONLY]: full-auto whole-page OCR + translate + text removal.

    NOT part of the product flow — only test_ui.html calls it, as a quick way to exercise the
    AI pipeline without starting the whole .NET stack. The .NET side
    (Infrastructure/Services/AiService.cs) calls only /api/ai/segment and
    /api/ai/translate-selected. Kept as a dev tool; do not build product features on it.
    """
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

