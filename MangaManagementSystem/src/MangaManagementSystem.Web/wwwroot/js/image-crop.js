const imageCropStates = new Map();

function clamp(value, minimum, maximum) {
    return Math.min(Math.max(value, minimum), maximum);
}

function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
        const image = new Image();

        image.onload = () => resolve(image);

        image.onerror = () => reject(
            new Error("Unable to load the selected image.")
        );

        image.src = dataUrl;
    });
}

function getState(canvasId) {
    const state = imageCropStates.get(canvasId);

    if (!state) {
        throw new Error(
            `Image cropper '${canvasId}' is not initialized.`
        );
    }

    return state;
}

function clampOffsets(state) {
    const scale = state.baseScale * state.zoom;

    const scaledWidth =
        state.image.naturalWidth * scale;

    const scaledHeight =
        state.image.naturalHeight * scale;

    const maximumOffsetX = Math.max(
        0,
        (scaledWidth - state.cropWidth) / 2
    );

    const maximumOffsetY = Math.max(
        0,
        (scaledHeight - state.cropHeight) / 2
    );

    state.offsetX = clamp(
        state.offsetX,
        -maximumOffsetX,
        maximumOffsetX
    );

    state.offsetY = clamp(
        state.offsetY,
        -maximumOffsetY,
        maximumOffsetY
    );
}

function drawGrid(context, cropX, cropY, cropWidth, cropHeight) {
    context.save();

    context.strokeStyle =
        "rgba(255, 255, 255, 0.38)";

    context.lineWidth = 1;

    for (let index = 1; index <= 2; index++) {
        const hPosition =
            cropHeight * index / 3;

        context.beginPath();
        context.moveTo(
            cropX,
            cropY + hPosition
        );
        context.lineTo(
            cropX + cropWidth,
            cropY + hPosition
        );
        context.stroke();

        const vPosition =
            cropWidth * index / 3;

        context.beginPath();
        context.moveTo(
            cropX + vPosition,
            cropY
        );
        context.lineTo(
            cropX + vPosition,
            cropY + cropHeight
        );
        context.stroke();
    }

    context.restore();
}

function drawCropper(state) {
    if (state.freeResize) { drawFreeCropper(state); return; }

    const canvas = state.canvas;
    const context = state.context;
    const image = state.image;

    const scale =
        state.baseScale * state.zoom;

    const imageWidth =
        image.naturalWidth * scale;

    const imageHeight =
        image.naturalHeight * scale;

    const imageX =
        canvas.width / 2 +
        state.offsetX -
        imageWidth / 2;

    const imageY =
        canvas.height / 2 +
        state.offsetY -
        imageHeight / 2;

    const cropX =
        (canvas.width - state.cropWidth) / 2;

    const cropY =
        (canvas.height - state.cropHeight) / 2;

    context.clearRect(
        0,
        0,
        canvas.width,
        canvas.height
    );

    context.fillStyle = "#0f172a";

    context.fillRect(
        0,
        0,
        canvas.width,
        canvas.height
    );

    context.drawImage(
        image,
        imageX,
        imageY,
        imageWidth,
        imageHeight
    );

    // Darken area outside the crop rectangle.
    context.save();

    context.fillStyle =
        "rgba(2, 6, 23, 0.68)";

    context.beginPath();

    context.rect(
        0,
        0,
        canvas.width,
        canvas.height
    );

    context.rect(
        cropX,
        cropY,
        state.cropWidth,
        state.cropHeight
    );

    context.fill("evenodd");
    context.restore();

    drawGrid(
        context,
        cropX,
        cropY,
        state.cropWidth,
        state.cropHeight
    );

    // Crop rectangle border.
    context.save();

    context.strokeStyle = "#ffffff";
    context.lineWidth = 4;

    context.strokeRect(
        cropX,
        cropY,
        state.cropWidth,
        state.cropHeight
    );

    context.restore();
}

function handlePointerDown(state, event) {
    if (state.freeResize) { handleFreePointerDown(state, event); return; }

    event.preventDefault();

    state.dragging = true;
    state.lastPointerX = event.clientX;
    state.lastPointerY = event.clientY;

    state.canvas.setPointerCapture(
        event.pointerId
    );

    state.canvas.classList.add(
        "is-dragging"
    );
}

function handlePointerMove(state, event) {
    if (state.freeResize) { handleFreePointerMove(state, event); return; }

    if (!state.dragging) {
        return;
    }

    event.preventDefault();

    const rectangle =
        state.canvas.getBoundingClientRect();

    const ratioX =
        state.canvas.width / rectangle.width;

    const ratioY =
        state.canvas.height / rectangle.height;

    const deltaX =
        (event.clientX - state.lastPointerX) *
        ratioX;

    const deltaY =
        (event.clientY - state.lastPointerY) *
        ratioY;

    state.lastPointerX = event.clientX;
    state.lastPointerY = event.clientY;

    state.offsetX += deltaX;
    state.offsetY += deltaY;

    clampOffsets(state);
    drawCropper(state);
}

function handlePointerEnd(state, event) {
    if (state.freeResize) { handleFreePointerEnd(state, event); return; }

    state.dragging = false;

    state.canvas.classList.remove(
        "is-dragging"
    );

    if (
        state.canvas.hasPointerCapture(
            event.pointerId
        )
    ) {
        state.canvas.releasePointerCapture(
            event.pointerId
        );
    }
}

function attachEvents(state) {
    state.pointerDownHandler =
        event => handlePointerDown(
            state,
            event
        );

    state.pointerMoveHandler =
        event => handlePointerMove(
            state,
            event
        );

    state.pointerUpHandler =
        event => handlePointerEnd(
            state,
            event
        );

    state.pointerCancelHandler =
        event => handlePointerEnd(
            state,
            event
        );

    state.canvas.addEventListener(
        "pointerdown",
        state.pointerDownHandler
    );

    state.canvas.addEventListener(
        "pointermove",
        state.pointerMoveHandler
    );

    state.canvas.addEventListener(
        "pointerup",
        state.pointerUpHandler
    );

    state.canvas.addEventListener(
        "pointercancel",
        state.pointerCancelHandler
    );
}

function detachEvents(state) {
    state.canvas.removeEventListener(
        "pointerdown",
        state.pointerDownHandler
    );

    state.canvas.removeEventListener(
        "pointermove",
        state.pointerMoveHandler
    );

    state.canvas.removeEventListener(
        "pointerup",
        state.pointerUpHandler
    );

    state.canvas.removeEventListener(
        "pointercancel",
        state.pointerCancelHandler
    );
}

export async function initialize(
    canvasId,
    dataUrl,
    aspectRatio = 2 / 3,
    freeResize = false
) {
    dispose(canvasId);

    const canvas =
        document.getElementById(canvasId);

    if (
        !(canvas instanceof HTMLCanvasElement)
    ) {
        throw new Error(
            `Canvas '${canvasId}' was not found.`
        );
    }

    if (!dataUrl) {
        throw new Error(
            "The selected image is empty."
        );
    }

    const context =
        canvas.getContext("2d");

    if (!context) {
        throw new Error(
            "Canvas 2D context is unavailable."
        );
    }

    const image =
        await loadImage(dataUrl);

    const ratio = clamp(
        Number(aspectRatio) || 2 / 3,
        1 / 5,
        5
    );

    // Compute crop dimensions that fit inside the canvas.
    const padding = 40;

    let cropWidth, cropHeight;

    if (ratio <= 1) {
        // Portrait or square.
        cropHeight = canvas.height - padding;
        cropWidth = cropHeight * ratio;

        if (cropWidth > canvas.width - padding) {
            cropWidth = canvas.width - padding;
            cropHeight = cropWidth / ratio;
        }
    } else {
        // Landscape.
        cropWidth = canvas.width - padding;
        cropHeight = cropWidth / ratio;

        if (cropHeight > canvas.height - padding) {
            cropHeight = canvas.height - padding;
            cropWidth = cropHeight * ratio;
        }
    }

    // Image must completely cover the crop area (fixed-frame mode).
    const baseScale = Math.max(
        cropWidth / image.naturalWidth,
        cropHeight / image.naturalHeight
    );

    // Free-resize mode fits the whole image inside the canvas and lets the user drag the crop
    // rectangle's edges/corners to resize it freely (independent width & height) + drag to move.
    const fitScale = Math.min(
        (canvas.width - padding) / image.naturalWidth,
        (canvas.height - padding) / image.naturalHeight
    );
    const fitW = image.naturalWidth * fitScale;
    const fitH = image.naturalHeight * fitScale;
    const fitX = (canvas.width - fitW) / 2;
    const fitY = (canvas.height - fitH) / 2;

    const state = {
        canvas,
        context,
        image,
        cropWidth: freeResize ? fitW : cropWidth,
        cropHeight: freeResize ? fitH : cropHeight,
        cropX: freeResize ? fitX : (canvas.width - cropWidth) / 2,
        cropY: freeResize ? fitY : (canvas.height - cropHeight) / 2,
        baseScale,

        freeResize,
        fitScale,
        fitX,
        fitY,
        fitW,
        fitH,
        activeHandle: null,
        startCrop: null,
        startPointerX: 0,
        startPointerY: 0,

        zoom: 1,
        offsetX: 0,
        offsetY: 0,

        dragging: false,
        lastPointerX: 0,
        lastPointerY: 0,

        pointerDownHandler: null,
        pointerMoveHandler: null,
        pointerUpHandler: null,
        pointerCancelHandler: null
    };

    imageCropStates.set(
        canvasId,
        state
    );

    attachEvents(state);
    if (!freeResize) clampOffsets(state);
    drawCropper(state);
}

export function setZoom(
    canvasId,
    zoomValue
) {
    const state =
        getState(canvasId);

    if (state.freeResize) return;

    state.zoom = clamp(
        Number(zoomValue) || 1,
        1,
        3
    );

    clampOffsets(state);
    drawCropper(state);
}

export function reset(canvasId) {
    const state =
        getState(canvasId);

    if (state.freeResize) {
        state.cropX = state.fitX;
        state.cropY = state.fitY;
        state.cropWidth = state.fitW;
        state.cropHeight = state.fitH;
        drawCropper(state);
        return;
    }

    state.zoom = 1;
    state.offsetX = 0;
    state.offsetY = 0;

    clampOffsets(state);
    drawCropper(state);
}

export function getImageDimensions(canvasId) {
    const state =
        getState(canvasId);

    return {
        naturalWidth: state.image.naturalWidth,
        naturalHeight: state.image.naturalHeight,
        cropSourceWidth: state.cropWidth / state.baseScale,
        cropSourceHeight: state.cropHeight / state.baseScale
    };
}

export async function exportCroppedImageStream(
    canvasId,
    outputWidth = 800,
    outputHeight = 1200
) {
    const state =
        getState(canvasId);

    if (state.freeResize) {
        return exportFreeCrop(state);
    }

    const scale =
        state.baseScale * state.zoom;

    const scaledWidth =
        state.image.naturalWidth * scale;

    const scaledHeight =
        state.image.naturalHeight * scale;

    const imageX =
        state.canvas.width / 2 +
        state.offsetX -
        scaledWidth / 2;

    const imageY =
        state.canvas.height / 2 +
        state.offsetY -
        scaledHeight / 2;

    const cropX =
        (state.canvas.width -
            state.cropWidth) / 2;

    const cropY =
        (state.canvas.height -
            state.cropHeight) / 2;

    const sourceX =
        (cropX - imageX) / scale;

    const sourceY =
        (cropY - imageY) / scale;

    const sourceWidth =
        state.cropWidth / scale;

    const sourceHeight =
        state.cropHeight / scale;

    const outputCanvas =
        document.createElement("canvas");

    outputCanvas.width = outputWidth;
    outputCanvas.height = outputHeight;

    const outputContext =
        outputCanvas.getContext("2d");

    if (!outputContext) {
        throw new Error(
            "Unable to create the cropped image."
        );
    }

    outputContext.imageSmoothingEnabled =
        true;

    outputContext.imageSmoothingQuality =
        "high";

    outputContext.drawImage(
        state.image,

        sourceX,
        sourceY,

        sourceWidth,
        sourceHeight,

        0,
        0,

        outputWidth,
        outputHeight
    );

    const blob = await new Promise(
        (resolve, reject) => {
            outputCanvas.toBlob(
                result => {
                    if (!result) {
                        reject(
                            new Error(
                                "Unable to encode the cropped image."
                            )
                        );

                        return;
                    }

                    resolve(result);
                },
                "image/png"
            );
        }
    );

    const arrayBuffer =
        await blob.arrayBuffer();

    return new Uint8Array(arrayBuffer);
}

// ---- Free-resize mode: a movable + resizable crop rectangle over a fit-to-canvas image ----
const FREE_HANDLE_SIZE = 14;
const FREE_MIN_CROP = 30;

function freeHandlePoints(state) {
    const x = state.cropX, y = state.cropY, w = state.cropWidth, h = state.cropHeight;
    return [
        { name: "nw", x: x,         y: y },
        { name: "n",  x: x + w / 2, y: y },
        { name: "ne", x: x + w,     y: y },
        { name: "e",  x: x + w,     y: y + h / 2 },
        { name: "se", x: x + w,     y: y + h },
        { name: "s",  x: x + w / 2, y: y + h },
        { name: "sw", x: x,         y: y + h },
        { name: "w",  x: x,         y: y + h / 2 }
    ];
}

function freeHandleAt(state, px, py) {
    for (const point of freeHandlePoints(state)) {
        if (Math.abs(px - point.x) <= FREE_HANDLE_SIZE && Math.abs(py - point.y) <= FREE_HANDLE_SIZE) {
            return point.name;
        }
    }
    if (px >= state.cropX && px <= state.cropX + state.cropWidth &&
        py >= state.cropY && py <= state.cropY + state.cropHeight) {
        return "move";
    }
    return null;
}

function freeCursor(handle) {
    switch (handle) {
        case "nw": case "se": return "nwse-resize";
        case "ne": case "sw": return "nesw-resize";
        case "n": case "s": return "ns-resize";
        case "e": case "w": return "ew-resize";
        case "move": return "move";
        default: return "default";
    }
}

function freeToCanvas(state, event) {
    const rect = state.canvas.getBoundingClientRect();
    return {
        x: (event.clientX - rect.left) * (state.canvas.width / rect.width),
        y: (event.clientY - rect.top) * (state.canvas.height / rect.height)
    };
}

function drawFreeCropper(state) {
    const { canvas, context, image } = state;

    context.clearRect(0, 0, canvas.width, canvas.height);
    context.fillStyle = "#0f172a";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(image, state.fitX, state.fitY, state.fitW, state.fitH);

    context.save();
    context.fillStyle = "rgba(2, 6, 23, 0.68)";
    context.beginPath();
    context.rect(0, 0, canvas.width, canvas.height);
    context.rect(state.cropX, state.cropY, state.cropWidth, state.cropHeight);
    context.fill("evenodd");
    context.restore();

    drawGrid(context, state.cropX, state.cropY, state.cropWidth, state.cropHeight);

    context.save();
    context.strokeStyle = "#ffffff";
    context.lineWidth = 3;
    context.strokeRect(state.cropX, state.cropY, state.cropWidth, state.cropHeight);
    context.fillStyle = "#ffffff";
    for (const point of freeHandlePoints(state)) {
        context.fillRect(
            point.x - FREE_HANDLE_SIZE / 2,
            point.y - FREE_HANDLE_SIZE / 2,
            FREE_HANDLE_SIZE,
            FREE_HANDLE_SIZE
        );
    }
    context.restore();
}

function handleFreePointerDown(state, event) {
    const p = freeToCanvas(state, event);
    const handle = freeHandleAt(state, p.x, p.y);
    if (!handle) return;

    event.preventDefault();
    state.activeHandle = handle;
    state.startCrop = { x: state.cropX, y: state.cropY, w: state.cropWidth, h: state.cropHeight };
    state.startPointerX = p.x;
    state.startPointerY = p.y;
    state.canvas.setPointerCapture(event.pointerId);
    state.canvas.classList.add("is-dragging");
}

function handleFreePointerMove(state, event) {
    const p = freeToCanvas(state, event);

    if (!state.activeHandle) {
        state.canvas.style.cursor = freeCursor(freeHandleAt(state, p.x, p.y));
        return;
    }

    event.preventDefault();

    const s = state.startCrop;
    const dx = p.x - state.startPointerX;
    const dy = p.y - state.startPointerY;

    const minX = state.fitX;
    const minY = state.fitY;
    const maxX = state.fitX + state.fitW;
    const maxY = state.fitY + state.fitH;

    if (state.activeHandle === "move") {
        state.cropX = clamp(s.x + dx, minX, maxX - s.w);
        state.cropY = clamp(s.y + dy, minY, maxY - s.h);
        drawCropper(state);
        return;
    }

    let left = s.x;
    let top = s.y;
    let right = s.x + s.w;
    let bottom = s.y + s.h;

    const h = state.activeHandle;
    if (h.indexOf("w") !== -1) left = clamp(s.x + dx, minX, right - FREE_MIN_CROP);
    if (h.indexOf("e") !== -1) right = clamp(s.x + s.w + dx, left + FREE_MIN_CROP, maxX);
    if (h.indexOf("n") !== -1) top = clamp(s.y + dy, minY, bottom - FREE_MIN_CROP);
    if (h.indexOf("s") !== -1) bottom = clamp(s.y + s.h + dy, top + FREE_MIN_CROP, maxY);

    state.cropX = left;
    state.cropY = top;
    state.cropWidth = right - left;
    state.cropHeight = bottom - top;
    drawCropper(state);
}

function handleFreePointerEnd(state, event) {
    state.activeHandle = null;
    state.startCrop = null;
    state.canvas.classList.remove("is-dragging");
    if (state.canvas.hasPointerCapture(event.pointerId)) {
        state.canvas.releasePointerCapture(event.pointerId);
    }
}

async function exportFreeCrop(state) {
    const sourceX = (state.cropX - state.fitX) / state.fitScale;
    const sourceY = (state.cropY - state.fitY) / state.fitScale;
    const sourceWidth = state.cropWidth / state.fitScale;
    const sourceHeight = state.cropHeight / state.fitScale;

    const outW = Math.max(1, Math.round(sourceWidth));
    const outH = Math.max(1, Math.round(sourceHeight));

    const outputCanvas = document.createElement("canvas");
    outputCanvas.width = outW;
    outputCanvas.height = outH;

    const ctx = outputCanvas.getContext("2d");
    if (!ctx) throw new Error("Unable to create the cropped image.");

    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = "high";
    ctx.drawImage(state.image, sourceX, sourceY, sourceWidth, sourceHeight, 0, 0, outW, outH);

    const blob = await new Promise((resolve, reject) => {
        outputCanvas.toBlob(
            result => result ? resolve(result) : reject(new Error("Unable to encode the cropped image.")),
            "image/png"
        );
    });

    const arrayBuffer = await blob.arrayBuffer();
    return new Uint8Array(arrayBuffer);
}

export function dispose(canvasId) {
    const state =
        imageCropStates.get(canvasId);

    if (!state) {
        return;
    }

    detachEvents(state);

    imageCropStates.delete(
        canvasId
    );
}
