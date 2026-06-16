let canvas, ctx, container;
let dotNetRef;
let originalImg = null;
let regions = [];
let annotations = [];
let nextId = 1;

// View state (Zoom & Pan)
let scale = 1;
let panX = 0, panY = 0;
let isPanning = false;
let startPanX = 0, startPanY = 0;

// Tool state
let currentTool = 'select'; // 'select', 'draw', 'pan'
let backgroundCanvas = document.createElement('canvas');
let bgCtx = backgroundCanvas.getContext('2d');

let isDrawing = false;
let isDraggingRegion = false;
let isResizing = false;
let isBrushing = false;
let isSpacePressed = false;
let previousTool = null;

let lastBrushPos = null;
let brushColor = '#ffffff';
let brushSize = 20;
let drawStart = {x:0, y:0};
let targetRegion = null;
let dragOffsetX = 0, dragOffsetY = 0;
let resizeHandle = '';
let dragStartCanvas = null;
let originalRegionRect = null;

// Typography state
let typo = {
    font: 'Comic Sans MS, cursive',
    fontSize: 18,
    color: '#000000',
    align: 'center',
    stroke: false
};

// Undo/Redo
let historyStack = [];
let historyIndex = -1;

let selectionDiv = null;

export function syncAnnotations(anns) {
    annotations = anns || [];
    redraw();
}

export function initCanvas(canvasId, containerId, dotnet) {
    canvas = document.getElementById(canvasId);
    container = document.getElementById(containerId);
    ctx = canvas.getContext('2d');
    dotNetRef = dotnet;

    // CSS HTML đã chuẩn, không cần ghi đè container

    
    canvas.style.position = 'absolute';
    canvas.style.left = '0px';
    canvas.style.top = '0px';
    canvas.style.transformOrigin = 'top left';
    canvas.style.display = 'none'; // Hide initially when no image is loaded

    selectionDiv = document.createElement('div');
    selectionDiv.style.position = 'absolute';
    selectionDiv.style.border = '2px dashed #3498db';
    selectionDiv.style.backgroundColor = 'rgba(52, 152, 219, 0.2)';
    selectionDiv.style.pointerEvents = 'none';
    selectionDiv.style.display = 'none';
    selectionDiv.style.zIndex = '10';
    container.appendChild(selectionDiv);

    setupEvents();
}

function setupEvents() {
    // Zoom with wheel
    container.addEventListener('wheel', (e) => {
        e.preventDefault();
        const rect = container.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
        
        // Calculate new scale, min 0.1, max 10
        let newScale = scale * zoomFactor;
        newScale = Math.max(0.1, Math.min(newScale, 10));

        // Adjust pan to zoom around mouse
        panX = mouseX - (mouseX - panX) * (newScale / scale);
        panY = mouseY - (mouseY - panY) * (newScale / scale);
        scale = newScale;
        
        applyTransform();
    }, { passive: false });

    // Mouse events
    container.addEventListener('mousedown', (e) => {
        if (currentTool === 'pan' || e.button === 1 || isSpacePressed) { // Middle click or Space to pan
            isPanning = true;
            startPanX = e.clientX - panX;
            startPanY = e.clientY - panY;
            container.style.cursor = 'grabbing';
            return;
        }

        const pos = getMousePos(e);
        
        if (currentTool === 'draw') {
            isDrawing = true;
            drawStart = { x: e.clientX, y: e.clientY };
            
            const rect = container.getBoundingClientRect();
            selectionDiv.style.left = (e.clientX - rect.left) + 'px';
            selectionDiv.style.top = (e.clientY - rect.top) + 'px';
            selectionDiv.style.width = '0px';
            selectionDiv.style.height = '0px';
            selectionDiv.style.display = 'block';
        } else if (currentTool === 'pin') {
            const pos = getMousePos(e);
            dotNetRef.invokeMethodAsync('OnPinAdded', Math.round(pos.x), Math.round(pos.y));
            
            // Switch back to select tool automatically
            currentTool = 'select';
            dotNetRef.invokeMethodAsync('OnToolChangedFromJS', 'select');
        } else if (currentTool === 'brush') {
            isBrushing = true;
            lastBrushPos = getMousePos(e);
        } else if (currentTool === 'select') {
            const pos = getMousePos(e);
            
            // Check if clicking on a selected region's handle FIRST
            let clickedHandle = null;
            let clickedRegion = null;
            for (let i = regions.length - 1; i >= 0; i--) {
                const r = regions[i];
                if (r.selected) {
                    const handles = getHandleRects(r);
                    for (let key in handles) {
                        const h = handles[key];
                        const pad = 4;
                        if (pos.x >= h.x - pad && pos.x <= h.x + h.w + pad &&
                            pos.y >= h.y - pad && pos.y <= h.y + h.h + pad) {
                            clickedHandle = key;
                            clickedRegion = r;
                            break;
                        }
                    }
                }
                if (clickedHandle) break;
            }

            if (clickedHandle) {
                isResizing = true;
                resizeHandle = clickedHandle;
                targetRegion = clickedRegion;
                dragStartCanvas = pos;
                originalRegionRect = { x: clickedRegion.x, y: clickedRegion.y, w: clickedRegion.width, h: clickedRegion.height };
                return;
            }

            // Check if clicking inside a region
            const hit = regions.slice().reverse().find(r => 
                pos.x >= r.x && pos.x <= r.x + r.width && 
                pos.y >= r.y && pos.y <= r.y + r.height);
            
            if (hit) {
                if (e.shiftKey || e.ctrlKey) {
                    hit.selected = !hit.selected;
                    syncToBlazor();
                    redraw();
                } else {
                    if (!hit.selected) {
                        regions.forEach(r => r.selected = false);
                        hit.selected = true;
                        syncToBlazor();
                        redraw();
                    }
                }
                isDraggingRegion = true;
                targetRegion = hit;
                dragOffsetX = pos.x - hit.x;
                dragOffsetY = pos.y - hit.y;
            } else {
                if (!e.shiftKey && !e.ctrlKey) {
                    const hadSelection = regions.some(r => r.selected);
                    regions.forEach(r => r.selected = false);
                    if (hadSelection) {
                        syncToBlazor();
                        redraw();
                    }
                }
            }
        }
    });

    container.addEventListener('mousemove', (e) => {
        if (isPanning) {
            panX = e.clientX - startPanX;
            panY = e.clientY - startPanY;
            applyTransform();
            return;
        }

        const pos = getMousePos(e);
        
        if (isBrushing) {
            bgCtx.beginPath();
            bgCtx.moveTo(lastBrushPos.x, lastBrushPos.y);
            bgCtx.lineTo(pos.x, pos.y);
            bgCtx.strokeStyle = brushColor;
            bgCtx.lineWidth = brushSize;
            bgCtx.lineCap = 'round';
            bgCtx.stroke();
            lastBrushPos = pos;
            redraw();
            return;
        }

        if (currentTool === 'select') {
            if (isDraggingRegion && targetRegion) {
                targetRegion.x = Math.round(pos.x - dragOffsetX);
                targetRegion.y = Math.round(pos.y - dragOffsetY);
                targetRegion.isSuggested = false; // Auto-approve on edit
                redraw();
                return;
            }
            if (isResizing && targetRegion) {
                const orig = originalRegionRect;
                let newX = orig.x;
                let newY = orig.y;
                let newW = orig.w;
                let newH = orig.h;

                const dx = pos.x - dragStartCanvas.x;
                const dy = pos.y - dragStartCanvas.y;

                if (resizeHandle.includes('e')) newW = Math.max(10, orig.w + dx);
                if (resizeHandle.includes('s')) newH = Math.max(10, orig.h + dy);
                if (resizeHandle.includes('w')) {
                    const maxDx = orig.w - 10;
                    const clampedDx = Math.min(dx, maxDx);
                    newX = orig.x + clampedDx;
                    newW = orig.w - clampedDx;
                }
                if (resizeHandle.includes('n')) {
                    const maxDy = orig.h - 10;
                    const clampedDy = Math.min(dy, maxDy);
                    newY = orig.y + clampedDy;
                    newH = orig.h - clampedDy;
                }

                targetRegion.x = Math.round(newX);
                targetRegion.y = Math.round(newY);
                targetRegion.width = Math.round(newW);
                targetRegion.height = Math.round(newH);
                targetRegion.isSuggested = false; // Auto-approve on edit
                redraw();
                return;
            }

            // Hover cursor logic
            let hoverCursor = 'default';
            let found = false;
            for (let i = regions.length - 1; i >= 0; i--) {
                const r = regions[i];
                if (r.selected) {
                    const handles = getHandleRects(r);
                    for (let key in handles) {
                        const h = handles[key];
                        const pad = 4;
                        if (pos.x >= h.x - pad && pos.x <= h.x + h.w + pad &&
                            pos.y >= h.y - pad && pos.y <= h.y + h.h + pad) {
                            hoverCursor = h.cursor;
                            found = true;
                            break;
                        }
                    }
                }
                if (found) break;
            }
            if (!found) {
                const hit = regions.slice().reverse().find(r => 
                    pos.x >= r.x && pos.x <= r.x + r.width && 
                    pos.y >= r.y && pos.y <= r.y + r.height);
                if (hit) hoverCursor = 'move';
            }
            container.style.cursor = hoverCursor;
        }

        if (isDrawing) {
            const rect = container.getBoundingClientRect();
            const currentX = e.clientX;
            const currentY = e.clientY;
            
            const left = Math.min(drawStart.x, currentX) - rect.left;
            const top = Math.min(drawStart.y, currentY) - rect.top;
            const width = Math.abs(currentX - drawStart.x);
            const height = Math.abs(currentY - drawStart.y);
            
            selectionDiv.style.left = left + 'px';
            selectionDiv.style.top = top + 'px';
            selectionDiv.style.width = width + 'px';
            selectionDiv.style.height = height + 'px';
        }
    });

    window.addEventListener('mouseup', (e) => {
        if (isBrushing) {
            isBrushing = false;
            saveState();
            return;
        }

        if (isPanning) {
            isPanning = false;
            container.style.cursor = currentTool === 'pan' ? 'grab' : 'crosshair';
        }
        
        if (isDraggingRegion || isResizing) {
            isDraggingRegion = false;
            isResizing = false;
            targetRegion = null;
            saveState();
            syncToBlazor();
            redraw();
        }

        if (isDrawing) {
            isDrawing = false;
            selectionDiv.style.display = 'none';
            
            const rect = container.getBoundingClientRect();
            const startCanvasX = (drawStart.x - rect.left - panX) / scale;
            const startCanvasY = (drawStart.y - rect.top - panY) / scale;
            const endCanvasPos = getMousePos(e);
            
            const x = Math.min(startCanvasX, endCanvasPos.x);
            const y = Math.min(startCanvasY, endCanvasPos.y);
            const w = Math.abs(endCanvasPos.x - startCanvasX);
            const h = Math.abs(endCanvasPos.y - startCanvasY);

            if (w > 10 && h > 10) { // Min size
                regions.push({
                    id: nextId++,
                    x: Math.round(x), y: Math.round(y),
                    width: Math.round(w), height: Math.round(h),
                    selected: true,
                    originalText: '',
                    translatedText: ''
                });
                saveState();
                syncToBlazor();
            }
            redraw();
        }
    });

    container.addEventListener('dblclick', (e) => {
        if (currentTool !== 'select') return;
        const pos = getMousePos(e);
        const hit = regions.slice().reverse().find(r => 
            pos.x >= r.x && pos.x <= r.x + r.width && 
            pos.y >= r.y && pos.y <= r.y + r.height);
        
        if (hit) {
            dotNetRef.invokeMethodAsync('OnRegionDoubleClicked', hit.id);
        }
    });

    window.addEventListener('keydown', (e) => {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        if (e.code === 'Space' && !isSpacePressed) {
            e.preventDefault();
            isSpacePressed = true;
            if (currentTool !== 'pan') {
                previousTool = currentTool;
                setTool('pan');
            }
            return;
        }

        if (e.key === 'Delete' || e.key === 'Backspace') {
            deleteSelectedRegions();
        } else if (e.ctrlKey || e.metaKey) {
            if (e.key.toLowerCase() === 'z') {
                if (e.shiftKey) {
                    redo();
                } else {
                    undo();
                }
                e.preventDefault();
            } else if (e.key.toLowerCase() === 'y') {
                redo();
                e.preventDefault();
            }
        }
    });

    window.addEventListener('keyup', (e) => {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        if (e.code === 'Space') {
            isSpacePressed = false;
            if (previousTool) {
                setTool(previousTool);
                previousTool = null;
            }
        }
    });
}

function getMousePos(e) {
    const rect = container.getBoundingClientRect();
    return {
        x: (e.clientX - rect.left - panX) / scale,
        y: (e.clientY - rect.top - panY) / scale
    };
}

function applyTransform() {
    canvas.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
}

let currentDataUrl = null;

export function loadImage(dataUrl) {
    currentDataUrl = dataUrl;
    return new Promise((resolve) => {
        if (!dataUrl) {
            originalImg = null;
            backgroundCanvas.width = 0;
            backgroundCanvas.height = 0;
            canvas.width = 0;
            canvas.height = 0;
            canvas.style.display = 'none'; // Hide when empty
            redraw();
            resolve();
            return;
        }
        const img = new Image();
        img.onload = () => {
            originalImg = img;
            backgroundCanvas.width = img.width;
            backgroundCanvas.height = img.height;
            bgCtx.drawImage(img, 0, 0);

            canvas.style.display = 'block'; // Show when loaded
            canvas.width = img.width;
            canvas.height = img.height;
            canvas.style.width = img.width + 'px';
            canvas.style.height = img.height + 'px';
            canvas.style.maxWidth = 'none';
            canvas.style.maxHeight = 'none';

            scale = Math.min(container.clientWidth / img.width, container.clientHeight / img.height);
            // Center
            panX = (container.clientWidth - img.width * scale) / 2;
            panY = (container.clientHeight - img.height * scale) / 2;
            applyTransform();
            
            redraw();
            resolve();
        };
        img.src = dataUrl;
    });
}

function dataURItoBlob(dataURI) {
    const byteString = atob(dataURI.split(',')[1]);
    const mimeString = dataURI.split(',')[0].split(':')[1].split(';')[0];
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);
    for (let i = 0; i < byteString.length; i++) {
        ia[i] = byteString.charCodeAt(i);
    }
    return new Blob([ab], {type: mimeString});
}

export function setTool(tool) {
    currentTool = tool;
    container.style.cursor = tool === 'pan' ? 'grab' : 'crosshair';
}

export function zoom(factor) {
    const newScale = scale * factor;
    const cx = container.clientWidth / 2;
    const cy = container.clientHeight / 2;
    panX = cx - (cx - panX) * (newScale / scale);
    panY = cy - (cy - panY) * (newScale / scale);
    scale = newScale;
    applyTransform();
}

export function resetZoom() {
    if(!originalImg) return;
    scale = Math.min(container.clientWidth / originalImg.width, container.clientHeight / originalImg.height);
    panX = (container.clientWidth - originalImg.width * scale) / 2;
    panY = (container.clientHeight - originalImg.height * scale) / 2;
    applyTransform();
}

export function setTypography(config) {
    typo = { ...typo, ...config };
    redraw();
}

export function updateRegionData(id, data) {
    const index = regions.findIndex(r => r.id === id);
    if (index !== -1) {
        regions[index] = { ...regions[index], ...data };
        saveState();
        redraw();
    }
}

export function loadRegions(savedRegionsStr) {
    if (!savedRegionsStr) {
        regions = [];
    } else if (typeof savedRegionsStr === 'string') {
        try {
            const parsed = JSON.parse(savedRegionsStr);
            regions = Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            console.error("Failed to parse regions:", e);
            regions = [];
        }
    } else if (Array.isArray(savedRegionsStr)) {
        regions = savedRegionsStr;
    } else {
        regions = [];
    }
    saveState();
    if (typeof dotNetRef !== 'undefined' && dotNetRef) syncToBlazor();
    redraw();
}

export function selectRegion(id) {
    regions.forEach(r => r.selected = (r.id === id));
    syncToBlazor();
    redraw();
}

export function deleteRegion(id) {
    regions = regions.filter(r => r.id !== id);
    saveState();
    syncToBlazor();
    redraw();
}

export function deleteSelectedRegions() {
    const hasSelected = regions.some(r => r.selected);
    if (!hasSelected) return;
    
    if (window.confirm("Are you sure you want to delete the selected panel(s)?")) {
        regions = regions.filter(r => !r.selected);
        saveState();
        syncToBlazor();
        redraw();
    }
}

export function approveSelectedRegions() {
    regions.forEach(r => {
        if (r.selected) r.isSuggested = false;
    });
    saveState();
    syncToBlazor();
    redraw();
}

export function setBrushSettings(color, size) {
    brushColor = color;
    brushSize = size;
}



// ---------------------------------------------
// TEXT WRAPPING & RENDERING
// ---------------------------------------------
function getHandleRects(r) {
    const s = 10; // Handle size
    const hs = s / 2;
    return {
        nw: { x: r.x - hs, y: r.y - hs, w: s, h: s, cursor: 'nwse-resize' },
        ne: { x: r.x + r.width - hs, y: r.y - hs, w: s, h: s, cursor: 'nesw-resize' },
        sw: { x: r.x - hs, y: r.y + r.height - hs, w: s, h: s, cursor: 'nesw-resize' },
        se: { x: r.x + r.width - hs, y: r.y + r.height - hs, w: s, h: s, cursor: 'nwse-resize' },
        n:  { x: r.x + r.width/2 - hs, y: r.y - hs, w: s, h: s, cursor: 'ns-resize' },
        s:  { x: r.x + r.width/2 - hs, y: r.y + r.height - hs, w: s, h: s, cursor: 'ns-resize' },
        w:  { x: r.x - hs, y: r.y + r.height/2 - hs, w: s, h: s, cursor: 'ew-resize' },
        e:  { x: r.x + r.width - hs, y: r.y + r.height/2 - hs, w: s, h: s, cursor: 'ew-resize' }
    };
}

function redraw() {
    if (!originalImg) return;
    
    // 1. Draw base image
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(backgroundCanvas, 0, 0);

    // 2. Draw regions and text
    regions.forEach(r => {
        const hasText = r.translatedText && r.translatedText.trim() !== '';

        // Fill
        if (hasText) {
            ctx.save();
            ctx.beginPath();
            ctx.rect(r.x, r.y, r.width, r.height);
            ctx.clip();
            drawWrappedText(ctx, r, r.x, r.y, r.width, r.height);
            ctx.restore();
        } else {
            ctx.fillStyle = r.selected ? 'rgba(52,152,219,0.2)' : 'rgba(231,76,60,0.1)';
            ctx.fillRect(r.x, r.y, r.width, r.height);
        }

        let statusColor = '#e74c3c'; // Todo
        if (r.status === 'InProgress') statusColor = '#f39c12';
        else if (r.status === 'Done') statusColor = '#2ecc71';

        // Draw border for all regions (different color if selected or has text)
        ctx.lineWidth = r.selected ? 3 : 2;
        if (r.isSuggested) {
            ctx.setLineDash([5, 5]);
            ctx.strokeStyle = r.selected ? '#3498db' : '#9b59b6'; // Purple for suggested AI region
        } else {
            ctx.setLineDash([]);
            ctx.strokeStyle = r.selected ? '#3498db' : statusColor;
        }
        ctx.strokeRect(r.x, r.y, r.width, r.height);
        ctx.setLineDash([]); // Reset line dash
        
        // Draw Handle/ID for all regions
        ctx.fillStyle = r.selected ? '#3498db' : statusColor;
        ctx.fillRect(r.x, r.y - 20, 40, 20);
        ctx.fillStyle = '#fff';
        ctx.font = '12px Arial';
        ctx.textAlign = 'left';
        ctx.textBaseline = 'alphabetic';
        ctx.fillText(`#${r.id} ${r.type === 'SFX' ? '(SFX)' : ''}`, r.x + 4, r.y - 6);

        // Draw Note indicator
        if (r.note && r.note.trim() !== '') {
            ctx.fillStyle = '#f1c40f'; // Yellow sticky note
            ctx.fillRect(r.x + r.width - 24, r.y - 24, 24, 24);
            ctx.strokeStyle = '#d4ac0d';
            ctx.strokeRect(r.x + r.width - 24, r.y - 24, 24, 24);
            ctx.fillStyle = '#000';
            ctx.font = '16px Arial';
            ctx.fillText('📝', r.x + r.width - 20, r.y - 6);
        }
        
        // Draw resize handles if selected
        if (r.selected) {
            const handles = getHandleRects(r);
            ctx.fillStyle = '#ffffff';
            ctx.strokeStyle = '#3498db';
            ctx.lineWidth = 1.5;
            for (let key in handles) {
                const h = handles[key];
                ctx.fillRect(h.x, h.y, h.w, h.h);
                ctx.strokeRect(h.x, h.y, h.w, h.h);
            }
        }
    });

    // Draw annotations (Pins)
    annotations.forEach(ann => {
        const ax = ann.x ?? ann.X;
        const ay = ann.y ?? ann.Y;
        const isResolved = ann.isResolved ?? ann.IsResolved ?? false;
        
        if (ax != null && ay != null) {
            const radius = 12 / scale;
            // Draw pin circle
            ctx.beginPath();
            ctx.arc(ax, ay, radius, 0, 2 * Math.PI);
            ctx.fillStyle = isResolved ? 'rgba(46, 204, 113, 0.9)' : 'rgba(231, 76, 60, 0.9)';
            ctx.fill();
            
            ctx.lineWidth = 2 / scale;
            ctx.strokeStyle = '#ffffff';
            ctx.stroke();

            // Draw exclamation mark inside
            ctx.fillStyle = '#ffffff';
            ctx.font = `bold ${14 / scale}px sans-serif`;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText('!', ax, ay);
        }
    });
}

function drawWrappedText(context, region, x, y, maxWidth, maxHeight) {
    if (region.isVertical) {
        drawVerticalText(context, region, x, y, maxWidth, maxHeight);
        return;
    }

    const text = region.translatedText;
    const size = region.fontSize > 0 ? region.fontSize : typo.fontSize;
    context.font = `${size}px ${typo.font}`;
    
    if (region.type === 'SFX') {
        context.font = `italic bold ${size + 4}px ${typo.font}`;
    }

    context.fillStyle = typo.color;
    context.textAlign = typo.align;
    context.textBaseline = 'top';
    
    if(typo.stroke) {
        context.strokeStyle = typo.color === '#000000' ? '#ffffff' : '#000000';
        context.lineWidth = region.type === 'SFX' ? 5 : 3;
        context.lineJoin = 'round';
    }

    const words = text.split(' ');
    let line = '';
    const lines = [];

    for(let n = 0; n < words.length; n++) {
        const testLine = line + words[n] + ' ';
        const metrics = context.measureText(testLine);
        const testWidth = metrics.width;
        
        if (testWidth > maxWidth && n > 0) {
            lines.push(line);
            line = words[n] + ' ';
        } else {
            line = testLine;
        }
    }
    lines.push(line);

    // Calculate vertical centering
    const lineHeight = size * 1.2;
    const totalTextHeight = lines.length * lineHeight;
    let currentY = y + (maxHeight - totalTextHeight) / 2;
    
    // Draw each line
    lines.forEach(l => {
        let currentX = x;
        if(typo.align === 'center') currentX = x + maxWidth / 2;
        if(typo.align === 'right') currentX = x + maxWidth;
        
        if(typo.stroke) {
            context.strokeText(l, currentX, currentY);
        }
        context.fillText(l, currentX, currentY);
        currentY += lineHeight;
    });
}

function drawVerticalText(context, region, x, y, maxWidth, maxHeight) {
    const text = region.translatedText;
    const size = region.fontSize > 0 ? region.fontSize : typo.fontSize;
    context.font = `${size}px ${typo.font}`;
    
    if (region.type === 'SFX') {
        context.font = `italic bold ${size + 4}px ${typo.font}`;
    }

    context.fillStyle = typo.color;
    context.textAlign = typo.align;
    context.textBaseline = 'top';

    if(typo.stroke) {
        context.strokeStyle = typo.color === '#000000' ? '#ffffff' : '#000000';
        context.lineWidth = region.type === 'SFX' ? 5 : 3;
        context.lineJoin = 'round';
    }

    // Tách văn bản thành các chữ (words) và lọc bỏ các khoảng trắng rỗng
    const words = text.split(/\s+/).filter(w => w.length > 0);
    
    const lineHeight = size * 1.2;
    const totalTextHeight = words.length * lineHeight;
    // Căn giữa theo chiều dọc
    let currentY = y + (maxHeight - totalTextHeight) / 2;
    
    words.forEach(word => {
        let currentX = x;
        if(typo.align === 'center') currentX = x + maxWidth / 2;
        if(typo.align === 'right') currentX = x + maxWidth;
        
        if(typo.stroke) {
            context.strokeText(word, currentX, currentY);
        }
        context.fillText(word, currentX, currentY);
        currentY += lineHeight; // Mỗi chữ một dòng
    });
}

// ---------------------------------------------
// HISTORY & SYNC
// ---------------------------------------------
function saveState() {
    // Drop future redo history
    if (historyIndex < historyStack.length - 1) {
        historyStack = historyStack.slice(0, historyIndex + 1);
    }
    historyStack.push({
        regions: JSON.parse(JSON.stringify(regions)),
        imgSrc: backgroundCanvas.toDataURL('image/png')
    });
    historyIndex++;
    if (typeof dotNetRef !== 'undefined' && dotNetRef) syncToBlazor();
}

export function undo() {
    if (historyIndex > 0) {
        historyIndex--;
        const state = historyStack[historyIndex];
        regions = JSON.parse(JSON.stringify(state.regions));
        
        if (state.imgSrc && state.imgSrc !== backgroundCanvas.toDataURL('image/png')) {
            const img = new Image();
            img.onload = () => {
                backgroundCanvas.width = img.width;
                backgroundCanvas.height = img.height;
                bgCtx.drawImage(img, 0, 0);
                syncToBlazor();
                redraw();
            };
            img.src = state.imgSrc;
        } else {
            syncToBlazor();
            redraw();
        }
    }
}

export function redo() {
    if (historyIndex < historyStack.length - 1) {
        historyIndex++;
        const state = historyStack[historyIndex];
        regions = JSON.parse(JSON.stringify(state.regions));
        
        if (state.imgSrc && state.imgSrc !== backgroundCanvas.toDataURL('image/png')) {
            const img = new Image();
            img.onload = () => {
                backgroundCanvas.width = img.width;
                backgroundCanvas.height = img.height;
                bgCtx.drawImage(img, 0, 0);
                syncToBlazor();
                redraw();
            };
            img.src = state.imgSrc;
        } else {
            syncToBlazor();
            redraw();
        }
    }
}

function syncToBlazor() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnRegionsUpdated', JSON.stringify(regions), historyIndex > 0, historyIndex < historyStack.length - 1);
    }
}

// ---------------------------------------------
// REAL AI INTEGRATION
// ---------------------------------------------
function calculateIoU(box1, box2) {
    const xA = Math.max(box1.x, box2.x);
    const yA = Math.max(box1.y, box2.y);
    const xB = Math.min(box1.x + box1.width, box2.x + box2.width);
    const yB = Math.min(box1.y + box1.height, box2.y + box2.height);

    const interArea = Math.max(0, xB - xA) * Math.max(0, yB - yA);
    if (interArea === 0) return 0;

    const box1Area = box1.width * box1.height;
    const box2Area = box2.width * box2.height;

    const iou = interArea / parseFloat(box1Area + box2Area - interArea);
    return iou;
}
export async function callSegmentAPI() {
    if (!currentDataUrl) return false;
    try {
        const blob = dataURItoBlob(currentDataUrl);
        const formData = new FormData();
        formData.append('file', blob, 'image.jpg');
        
        const res = await fetch('http://localhost:8000/api/ai/segment', { method: 'POST', body: formData });
        const json = await res.json();
        
        if (json.status === "success" && json.regions) {
            json.regions.forEach(aiReg => {
                let isDuplicate = false;
                for (let r of regions) {
                    if (calculateIoU(aiReg, r) > 0.5) {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (!isDuplicate) {
                    regions.push({
                        id: nextId++,
                        x: aiReg.x, y: aiReg.y, width: aiReg.width, height: aiReg.height,
                        originalText: '',
                        translatedText: '',
                        type: aiReg.typeCode || 'Panel',
                        isSuggested: true,
                        selected: false
                    });
                }
            });
            saveState();
            if (typeof dotNetRef !== 'undefined' && dotNetRef) syncToBlazor();
            redraw();
            return true;
        }
    } catch (e) {
        console.error("AI API Error:", e);
        return false;
    }
    return false;
}

export async function callTranslateAPI() {
    let targets = regions.filter(r => r.selected);
    if (targets.length === 0) targets = regions; // Fallback to all if none selected
    if (targets.length === 0) return false;
    
    try {
        const payload = {
            image_base64: currentDataUrl,
            regions: targets.map(r => ({
                id: r.id, x: r.x, y: r.y, width: r.width, height: r.height
            }))
        };
        
        const res = await fetch('http://localhost:8000/api/ai/translate-selected', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const json = await res.json();
        
        if (json.status === "success" && json.regions) {
            if (json.clean_image_base64) {
                return new Promise((resolve) => {
                    const img = new Image();
                    img.onload = () => {
                        backgroundCanvas.width = img.width;
                        backgroundCanvas.height = img.height;
                        bgCtx.drawImage(img, 0, 0);
                        
                        json.regions.forEach((r) => {
                            let match = regions.find(x => x.id === r.id);
                            if (match) {
                                match.originalText = r.originalText || '';
                                match.translatedText = r.translatedText || '';
                            }
                        });

                        saveState();
                        if (typeof dotNetRef !== 'undefined' && dotNetRef) syncToBlazor();
                        redraw();
                        resolve(true);
                    };
                    img.src = json.clean_image_base64;
                });
            } else {
                json.regions.forEach((r) => {
                    let match = regions.find(x => x.id === r.id);
                    if (match) {
                        match.originalText = r.originalText || '';
                        match.translatedText = r.translatedText || '';
                    }
                });
                saveState();
                if (typeof dotNetRef !== 'undefined' && dotNetRef) syncToBlazor();
                redraw();
                return true;
            }
        }
    } catch (e) {
        console.error("AI API Error:", e);
        return false;
    }
    return false;
}

export function exportImage() {
    return backgroundCanvas.toDataURL('image/png');
}

export function exportRegions() {
    return JSON.stringify(regions);
}

