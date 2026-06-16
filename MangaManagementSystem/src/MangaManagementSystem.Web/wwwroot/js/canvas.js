// Canvas rendering logic for CreatorWorkspace
// This module handles the drawing of manga pages with panel layout

export function drawCanvas(canvas, panels, currentPage, selectedPanelId, showInspector) {
    if (!canvas || !panels || panels.length === 0) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Canvas dimensions (fixed for consistency with React version)
    const dpr = window.devicePixelRatio || 1;
    canvas.width = 800 * dpr;
    canvas.height = 780 * dpr;
    canvas.style.width = '800px';
    canvas.style.height = '780px';
    ctx.scale(dpr, dpr);

    // Background: paper color
    ctx.fillStyle = '#f5f0eb';
    ctx.fillRect(0, 0, 800, 780);

    // Subtle paper grain
    ctx.fillStyle = 'rgba(0,0,0,0.025)';
    for (let i = 0; i < 600; i++) {
        ctx.fillRect(Math.random() * 800, Math.random() * 780, 1, 1);
    }

    panels.forEach(panel => {
        const isSelected = panel.id === selectedPanelId;

        // Panel fill
        ctx.fillStyle = isSelected ? '#fafaf9' : '#eeebe5';
        ctx.fillRect(panel.x + 4, panel.y + 4, panel.width - 8, panel.height - 8);

        // Sketch cross guides
        ctx.strokeStyle = 'rgba(0,0,0,0.12)';
        ctx.lineWidth = 0.5;
        ctx.setLineDash([4, 4]);
        const cx = panel.x + panel.width / 2;
        const cy = panel.y + panel.height / 2;
        ctx.beginPath();
        ctx.moveTo(panel.x + 16, cy);
        ctx.lineTo(panel.x + panel.width - 16, cy);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(cx, panel.y + 16);
        ctx.lineTo(cx, panel.y + panel.height - 16);
        ctx.stroke();
        ctx.setLineDash([]);

        // Panel label
        ctx.fillStyle = 'rgba(0,0,0,0.25)';
        ctx.font = '11px "DM Mono", monospace';
        ctx.fillText(`P${panel.id}`, panel.x + 10, panel.y + 20);

        // Bounding box
        ctx.strokeStyle = isSelected ? '#4f46e5' : '#dc2626';
        ctx.lineWidth = isSelected ? 2.5 : 1.5;
        ctx.strokeRect(panel.x, panel.y, panel.width, panel.height);

        // Corner handles
        const cs = 10;
        ctx.fillStyle = isSelected ? '#4f46e5' : '#dc2626';
        const corners = [
            [panel.x, panel.y],
            [panel.x + panel.width - cs, panel.y],
            [panel.x, panel.y + panel.height - cs],
            [panel.x + panel.width - cs, panel.y + panel.height - cs]
        ];
        corners.forEach(([ox, oy]) => {
            ctx.fillRect(ox - 1, oy - 1, cs, 2.5);
            ctx.fillRect(ox - 1, oy - 1, 2.5, cs);
        });

        if (isSelected) {
            ctx.fillStyle = '#4f46e5';
            ctx.fillRect(panel.x + panel.width - 64, panel.y - 22, 64, 18);
            ctx.fillStyle = '#fff';
            ctx.font = 'bold 9px "Inter", sans-serif';
            ctx.fillText('SELECTED', panel.x + panel.width - 58, panel.y - 10);
        }
    });

    // AI label
    ctx.fillStyle = 'rgba(220, 38, 38, 0.88)';
    ctx.beginPath();
    ctx.roundRect(10, 10, 136, 26, 4);
    ctx.fill();
    ctx.fillStyle = '#fff';
    ctx.font = 'bold 10px "Inter", sans-serif';
    ctx.fillText('AI DETECTED · YOLOv8', 20, 26);

    // Page number
    ctx.fillStyle = 'rgba(0,0,0,0.3)';
    ctx.font = '10px "DM Mono", monospace';
    ctx.fillText(`PAGE ${currentPage} / 4`, 10, 768);
}

export function createPanelsForPage(pageNumber) {
    // Fixed panel layout matching React version
    const pages = {
        1: [
            { id: 1, x: 50, y: 50, width: 700, height: 250, selected: false },
            { id: 2, x: 50, y: 320, width: 340, height: 200, selected: false },
            { id: 3, x: 410, y: 320, width: 340, height: 200, selected: false },
            { id: 4, x: 50, y: 540, width: 700, height: 180, selected: false },
        ],
        2: [
            { id: 1, x: 50, y: 50, width: 450, height: 180, selected: false },
            { id: 2, x: 520, y: 50, width: 230, height: 180, selected: false },
            { id: 3, x: 50, y: 250, width: 230, height: 300, selected: false },
            { id: 4, x: 300, y: 250, width: 450, height: 300, selected: false },
            { id: 5, x: 50, y: 570, width: 700, height: 160, selected: false },
        ],
        3: [
            { id: 1, x: 50, y: 50, width: 450, height: 180, selected: false },
            { id: 2, x: 520, y: 50, width: 230, height: 180, selected: false },
            { id: 3, x: 50, y: 250, width: 350, height: 220, selected: true },
            { id: 4, x: 420, y: 250, width: 330, height: 150, selected: false },
            { id: 5, x: 420, y: 420, width: 330, height: 200, selected: false },
            { id: 6, x: 50, y: 490, width: 350, height: 130, selected: false },
        ],
        4: [
            { id: 1, x: 50, y: 50, width: 300, height: 400, selected: false },
            { id: 2, x: 370, y: 50, width: 380, height: 190, selected: false },
            { id: 3, x: 370, y: 260, width: 380, height: 190, selected: false },
            { id: 4, x: 50, y: 470, width: 700, height: 250, selected: false },
        ]
    };

    return pages[pageNumber] || pages[3];
}

export function getCanvasRect(canvas) {
    const rect = canvas.getBoundingClientRect();
    return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
}

export function getPanelAtPosition(panels, x, y) {
    for (const panel of panels) {
        if (x >= panel.x && x <= panel.x + panel.width &&
            y >= panel.y && y <= panel.y + panel.height) {
            return panel.id;
        }
    }
    return null;
}

// Polyfill for roundRect if not available
if (typeof CanvasRenderingContext2D !== 'undefined' && !CanvasRenderingContext2D.prototype.roundRect) {
    CanvasRenderingContext2D.prototype.roundRect = function (x, y, w, h, r) {
        if (r === undefined) {
            r = 0;
        }
        this.beginPath();
        this.moveTo(x + r, y);
        this.lineTo(x + w - r, y);
        this.quadraticCurveTo(x + w, y, x + w, y + r);
        this.lineTo(x + w, y + h - r);
        this.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
        this.lineTo(x + r, y + h);
        this.quadraticCurveTo(x, y + h, x, y + h - r);
        this.lineTo(x, y + r);
        this.quadraticCurveTo(x, y, x + r, y);
        this.closePath();
        return this;
    };
}
