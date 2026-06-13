const avatarCropStates = new Map();

function clamp(value, minimum, maximum) {
    return Math.min(Math.max(value, minimum), maximum);
}

function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
        const image = new Image();

        image.onload = () => resolve(image);

        image.onerror = () => reject(
            new Error("Unable to load the selected avatar image.")
        );

        image.src = dataUrl;
    });
}

function getState(canvasId) {
    const state = avatarCropStates.get(canvasId);

    if (!state) {
        throw new Error(
            `Avatar cropper '${canvasId}' is not initialized.`
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
        (scaledWidth - state.cropSize) / 2
    );

    const maximumOffsetY = Math.max(
        0,
        (scaledHeight - state.cropSize) / 2
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

function drawGrid(context, cropX, cropY, cropSize) {
    context.save();

    context.strokeStyle =
        "rgba(255, 255, 255, 0.38)";

    context.lineWidth = 1;

    for (let index = 1; index <= 2; index++) {
        const position =
            cropSize * index / 3;

        context.beginPath();
        context.moveTo(
            cropX + position,
            cropY
        );
        context.lineTo(
            cropX + position,
            cropY + cropSize
        );
        context.stroke();

        context.beginPath();
        context.moveTo(
            cropX,
            cropY + position
        );
        context.lineTo(
            cropX + cropSize,
            cropY + position
        );
        context.stroke();
    }

    context.restore();
}

function drawCropper(state) {
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
        (canvas.width - state.cropSize) / 2;

    const cropY =
        (canvas.height - state.cropSize) / 2;

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

    // Làm tối vùng nằm ngoài khung crop.
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
        state.cropSize,
        state.cropSize
    );

    context.fill("evenodd");
    context.restore();

    drawGrid(
        context,
        cropX,
        cropY,
        state.cropSize
    );

    // Khung crop vuông.
    context.save();

    context.strokeStyle = "#ffffff";
    context.lineWidth = 4;

    context.strokeRect(
        cropX,
        cropY,
        state.cropSize,
        state.cropSize
    );

    // Viền tròn mô phỏng avatar cuối cùng.
    context.strokeStyle =
        "rgba(255, 255, 255, 0.84)";

    context.lineWidth = 3;

    context.beginPath();

    context.arc(
        canvas.width / 2,
        canvas.height / 2,
        state.cropSize / 2,
        0,
        Math.PI * 2
    );

    context.stroke();
    context.restore();
}

function handlePointerDown(state, event) {
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
    dataUrl
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
            "The selected avatar image is empty."
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

    const cropSize =
        Math.min(
            canvas.width,
            canvas.height
        ) - 70;

    // Ảnh luôn phải phủ kín khung crop.
    const baseScale = Math.max(
        cropSize / image.naturalWidth,
        cropSize / image.naturalHeight
    );

    const state = {
        canvas,
        context,
        image,
        cropSize,
        baseScale,

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

    avatarCropStates.set(
        canvasId,
        state
    );

    attachEvents(state);
    clampOffsets(state);
    drawCropper(state);
}

export function setZoom(
    canvasId,
    zoomValue
) {
    const state =
        getState(canvasId);

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

    state.zoom = 1;
    state.offsetX = 0;
    state.offsetY = 0;

    clampOffsets(state);
    drawCropper(state);
}

export async function exportCroppedImageStream(
    canvasId,
    outputSize = 512
) {
    const state =
        getState(canvasId);

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
            state.cropSize) / 2;

    const cropY =
        (state.canvas.height -
            state.cropSize) / 2;

    const sourceX =
        (cropX - imageX) / scale;

    const sourceY =
        (cropY - imageY) / scale;

    const sourceSize =
        state.cropSize / scale;

    const outputCanvas =
        document.createElement("canvas");

    outputCanvas.width = outputSize;
    outputCanvas.height = outputSize;

    const outputContext =
        outputCanvas.getContext("2d");

    if (!outputContext) {
        throw new Error(
            "Unable to create the cropped avatar."
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

        sourceSize,
        sourceSize,

        0,
        0,

        outputSize,
        outputSize
    );

    const blob = await new Promise(
        (resolve, reject) => {
            outputCanvas.toBlob(
                result => {
                    if (!result) {
                        reject(
                            new Error(
                                "Unable to encode the cropped avatar."
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

    /*
     * Quan trọng:
     * Trả Uint8Array trực tiếp.
     * Không gọi DotNet.createJSStreamReference ở đây.
     * Blazor tự chuyển Uint8Array thành IJSStreamReference.
     */
    return new Uint8Array(arrayBuffer);
}

export function dispose(canvasId) {
    const state =
        avatarCropStates.get(canvasId);

    if (!state) {
        return;
    }

    detachEvents(state);

    avatarCropStates.delete(
        canvasId
    );
}

/*
 * Giữ hàm cũ để tương thích nếu còn nơi khác đang gọi.
 */
export function cropCenter(
    dataUrl,
    size = 512,
    quality = 0.92
) {
    return new Promise(resolve => {
        let completed = false;

        const safeResolve = value => {
            if (completed) {
                return;
            }

            completed = true;

            resolve(value || dataUrl);
        };

        const timeout =
            setTimeout(
                () => safeResolve(dataUrl),
                3000
            );

        try {
            const image = new Image();

            image.onload = () => {
                clearTimeout(timeout);

                try {
                    const sourceSize =
                        Math.min(
                            image.naturalWidth,
                            image.naturalHeight
                        );

                    const sourceX =
                        (
                            image.naturalWidth -
                            sourceSize
                        ) / 2;

                    const sourceY =
                        (
                            image.naturalHeight -
                            sourceSize
                        ) / 2;

                    const canvas =
                        document.createElement(
                            "canvas"
                        );

                    canvas.width = size;
                    canvas.height = size;

                    const context =
                        canvas.getContext("2d");

                    if (!context) {
                        safeResolve(dataUrl);
                        return;
                    }

                    context.drawImage(
                        image,

                        sourceX,
                        sourceY,

                        sourceSize,
                        sourceSize,

                        0,
                        0,

                        size,
                        size
                    );

                    safeResolve(
                        canvas.toDataURL(
                            "image/jpeg",
                            quality
                        )
                    );
                }
                catch {
                    safeResolve(dataUrl);
                }
            };

            image.onerror = () => {
                clearTimeout(timeout);
                safeResolve(dataUrl);
            };

            image.src = dataUrl;
        }
        catch {
            clearTimeout(timeout);
            safeResolve(dataUrl);
        }
    });
}

window.profileSettingsCrop = {
    cropCenter
};