window.profileSettingsCrop = {
    cropCenter: function (dataUrl, size, quality) {
        return new Promise(function (resolve, reject) {
            let completed = false;

            const timeout = setTimeout(function () {
                if (!completed) {
                    completed = true;
                    resolve(dataUrl);
                }
            }, 3000);

            try {
                const image = new Image();

                image.onload = function () {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    clearTimeout(timeout);

                    const sourceSize = Math.min(image.width, image.height);
                    const sx = Math.floor((image.width - sourceSize) / 2);
                    const sy = Math.floor((image.height - sourceSize) / 2);

                    const canvas = document.createElement("canvas");
                    canvas.width = size || 512;
                    canvas.height = size || 512;

                    const ctx = canvas.getContext("2d");

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

                    resolve(canvas.toDataURL("image/jpeg", quality || 0.92));
                };

                image.onerror = function () {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    clearTimeout(timeout);
                    resolve(dataUrl);
                };

                image.src = dataUrl;
            } catch (error) {
                if (!completed) {
                    completed = true;
                    clearTimeout(timeout);
                    resolve(dataUrl);
                }
            }
        });
    }
};