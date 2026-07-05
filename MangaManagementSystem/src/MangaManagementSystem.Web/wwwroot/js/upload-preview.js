// Upload review previews (#4). Always loaded (App.razor), independent of the canvas ES module, so
// window.mmsMakeThumbnails is guaranteed available when the upload-review dialog needs it.
//
// Downscales full-size images to small JPEG thumbnails: Blazor Server ships the render diff over
// SignalR, so embedding multi-MB base64 <img> in the dialog froze it. Small thumbnails keep the
// payload tiny while still letting the user review each uploaded image.
window.mmsMakeThumbnails = async function (dataUrls, maxDim) {
    maxDim = maxDim || 240;
    const out = [];
    for (const src of (dataUrls || [])) {
        try {
            out.push(await new Promise((resolve) => {
                const img = new Image();
                img.onload = () => {
                    try {
                        const scale = Math.min(maxDim / img.width, maxDim / img.height, 1);
                        const w = Math.max(1, Math.round(img.width * scale));
                        const h = Math.max(1, Math.round(img.height * scale));
                        const c = document.createElement('canvas');
                        c.width = w;
                        c.height = h;
                        c.getContext('2d').drawImage(img, 0, 0, w, h);
                        resolve(c.toDataURL('image/jpeg', 0.6));
                    } catch {
                        resolve(''); // fall back to no preview for this image
                    }
                };
                img.onerror = () => resolve('');
                img.src = src;
            }));
        } catch {
            out.push('');
        }
    }
    return out;
};
