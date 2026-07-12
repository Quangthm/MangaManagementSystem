window.scheduleDrag = window.scheduleDrag || {};

window.scheduleDrag.enableCustomDrawerDragImage = function () {
   

    document.addEventListener("dragstart", function (event) {
        const card = event.target && event.target.closest
            ? event.target.closest('[data-schedule-drag-card="true"]')
            : null;

        if (!card || !event.dataTransfer) {
            return;
        }

        const title = card.getAttribute("data-drag-title") || "Series";
        const chapter = card.getAttribute("data-drag-chapter") || "";
        const status = card.getAttribute("data-drag-status") || "";
        const cover = card.getAttribute("data-drag-cover") || "";

        event.dataTransfer.setData("text/plain", `${title} — ${chapter}`);
        event.dataTransfer.effectAllowed = "move";

        const ghost = document.createElement("div");
        ghost.className = "mf-drag-preview-card";

        const coverBox = document.createElement("div");
        coverBox.className = "mf-drag-preview-cover";

        if (cover) {
            const img = document.createElement("img");
            img.src = cover;
            img.alt = "";
            coverBox.appendChild(img);
        } else {
            coverBox.textContent = "No Cover";
        }

        const info = document.createElement("div");
        info.className = "mf-drag-preview-info";

        const titleEl = document.createElement("div");
        titleEl.className = "mf-drag-preview-title";
        titleEl.textContent = title;

        const chapterEl = document.createElement("div");
        chapterEl.className = "mf-drag-preview-chapter";
        chapterEl.textContent = chapter;

        const statusEl = document.createElement("div");
        statusEl.className = "mf-drag-preview-status";
        statusEl.textContent = status;

        info.appendChild(titleEl);
        info.appendChild(chapterEl);
        info.appendChild(statusEl);

        ghost.appendChild(coverBox);
        ghost.appendChild(info);

        document.body.appendChild(ghost);

        // Force layout so the browser captures the styled element.
        ghost.getBoundingClientRect();

        event.dataTransfer.setDragImage(ghost, 18, 18);

        window.setTimeout(function () {
            if (ghost.parentNode) {
                ghost.parentNode.removeChild(ghost);
            }
        }, 1500);
    }, true);
};