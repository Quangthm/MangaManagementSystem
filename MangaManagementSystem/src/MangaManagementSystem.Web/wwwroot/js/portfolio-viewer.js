// Portfolio Viewer JS Interop
// Fetches files from URLs in the browser and renders them via blob URLs
window.portfolioInterop = {
    _currentBlobUrl: null,

    /**
     * Fetches a file from a URL (browser-side) and displays it in a target element.
     * @param {string} elementId - The id of the embed/iframe element to set the src on.
     * @param {string} fileUrl - The URL to fetch the file from.
     * @param {string} contentType - The MIME type of the file (used as blob type).
     * @returns {boolean} True if successful, false otherwise.
     */
    loadDocument: async function (elementId, fileUrl, contentType) {
        try {
            // Revoke any previous blob URL
            this.revokeCurrentUrl();

            // Fetch the file from the browser (bypasses server-side 401 issues)
            var response = await fetch(fileUrl);
            if (!response.ok) {
                console.error('portfolioInterop: fetch failed with status', response.status);
                return false;
            }

            var blob = await response.blob();

            // If the server returned a different content type, use the expected one
            if (contentType && blob.type !== contentType) {
                blob = new Blob([blob], { type: contentType });
            }

            var blobUrl = URL.createObjectURL(blob);
            this._currentBlobUrl = blobUrl;

            // Set the src on the target element
            var element = document.getElementById(elementId);
            if (element) {
                element.src = blobUrl;
                return true;
            }

            console.error('portfolioInterop: element not found:', elementId);
            return false;
        } catch (e) {
            console.error('portfolioInterop.loadDocument failed:', e);
            return false;
        }
    },

    /**
     * Revokes the current blob URL to free memory.
     */
    revokeCurrentUrl: function () {
        if (this._currentBlobUrl) {
            try {
                URL.revokeObjectURL(this._currentBlobUrl);
            } catch (e) { /* ignore */ }
            this._currentBlobUrl = null;
        }
    }
};
