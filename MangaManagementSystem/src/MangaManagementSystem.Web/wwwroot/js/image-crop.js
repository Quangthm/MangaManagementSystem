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
    dataUrl,
    aspectRatio = 2 / 3
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
    const padding = 70;

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

    // Image must completely cover the crop area.
    const baseScale = Math.max(
        cropWidth / image.naturalWidth,
        cropHeight / image.naturalHeight
    );

    const state = {
        canvas,
        context,
        image,
        cropWidth,
        cropHeight,
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

    imageCropStates.set(
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
    outputWidth = 800,
    outputHeight = 1200
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
