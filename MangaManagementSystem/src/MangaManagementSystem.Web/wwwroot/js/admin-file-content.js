window.adminFileContent = {
    open: async function (streamReference, contentType, fileName) {
        const arrayBuffer = await streamReference.arrayBuffer();
        const blob = new Blob(
            [arrayBuffer],
            { type: contentType || "application/octet-stream" });

        const objectUrl = URL.createObjectURL(blob);
        const openedWindow = window.open(
            objectUrl,
            "_blank",
            "noopener,noreferrer");

        if (!openedWindow) {
            const anchor = document.createElement("a");

            anchor.href = objectUrl;
            anchor.target = "_blank";
            anchor.rel = "noopener noreferrer";
            anchor.style.display = "none";

            document.body.appendChild(anchor);
            anchor.click();
            anchor.remove();
        }

        window.setTimeout(
            function () {
                URL.revokeObjectURL(objectUrl);
            },
            60000);
    },

    download: async function (streamReference, contentType, fileName) {
        const arrayBuffer = await streamReference.arrayBuffer();
        const blob = new Blob(
            [arrayBuffer],
            { type: contentType || "application/octet-stream" });

        const objectUrl = URL.createObjectURL(blob);
        const anchor = document.createElement("a");

        anchor.href = objectUrl;
        anchor.download = fileName || "download";
        anchor.style.display = "none";

        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();

        window.setTimeout(
            function () {
                URL.revokeObjectURL(objectUrl);
            },
            60000);
    }
};
