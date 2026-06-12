window.profileSettingsCrop = {
    cropCenter: function (dataUrl, size, quality) {
        return new Promise(function (resolve) {
            let completed = false;

            const safeResolve = function (value) {
                if (!completed) {
                    completed = true;
                    resolve(value || dataUrl);
                }
            };

            const timeout = setTimeout(function () {
                safeResolve(dataUrl);
            }, 3000);

            try {
                const image = new Image();

                image.onload = function () {
                    clearTimeout(timeout);

                    try {
                        const sourceSize = Math.min(image.width, image.height);
                        const sx = Math.floor((image.width - sourceSize) / 2);
                        const sy = Math.floor((image.height - sourceSize) / 2);

                        const canvas = document.createElement("canvas");
                        canvas.width = size || 512;
                        canvas.height = size || 512;

                        const ctx = canvas.getContext("2d");

                        if (!ctx) {
                            safeResolve(dataUrl);
                            return;
                        }

                        ctx.drawImage(
                            image,
                            sx,
                            sy,
                            sourceSize,
                            sourceSize,
                            0,
                            0,
                            canvas.width,
                            canvas.height
                        );

                        safeResolve(canvas.toDataURL("image/jpeg", quality || 0.92));
                    } catch {
                        safeResolve(dataUrl);
                    }
                };

                image.onerror = function () {
                    clearTimeout(timeout);
                    safeResolve(dataUrl);
                };

                image.src = dataUrl;
            } catch {
                clearTimeout(timeout);
                safeResolve(dataUrl);
            }
        });
    }
};