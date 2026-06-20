window.adminFileContent = {
    open: async function (streamReference, contentType, fileName) {
        const arrayBuffer = await streamReference.arrayBuffer();
        const contentBlob = new Blob(
            [arrayBuffer],
            { type: contentType || "application/octet-stream" });
        const contentUrl = URL.createObjectURL(contentBlob);
        const previewTitle = this.escapeHtml(
            fileName || "File preview");
        const previewHtml = [
            "<!DOCTYPE html>",
            "<html lang=\"en\">",
            "<head>",
            "<meta charset=\"utf-8\">",
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">",
            "<title>",
            previewTitle,
            "</title>",
            "<style>",
            "html,body{width:100%;height:100%;margin:0;background:#202124;overflow:hidden;}",
            "iframe{width:100%;height:100%;border:0;background:#ffffff;}",
            "</style>",
            "</head>",
            "<body>",
            "<iframe title=\"",
            previewTitle,
            "\" src=\"",
            contentUrl,
            "\"></iframe>",
            "</body>",
            "</html>"
        ].join("");

        const previewPageBlob = new Blob(
            [previewHtml],
            { type: "text/html" });
        const previewPageUrl = URL.createObjectURL(
            previewPageBlob);
        const anchor = document.createElement("a");

        anchor.href = previewPageUrl;
        anchor.target = "_blank";
        anchor.rel = "noopener noreferrer";
        anchor.style.display = "none";

        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();

        window.setTimeout(
            function () {
                URL.revokeObjectURL(previewPageUrl);
            },
            60000);

        window.setTimeout(
            function () {
                URL.revokeObjectURL(contentUrl);
            },
            300000);
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
            1000);
    },

    escapeHtml: function (value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }
};
