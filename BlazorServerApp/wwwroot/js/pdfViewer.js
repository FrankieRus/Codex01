import * as pdfjsLib from '../lib/pdfjs/pdf.min.mjs';

pdfjsLib.GlobalWorkerOptions.workerSrc = '/lib/pdfjs/pdf.worker.min.mjs';

let currentRenderToken = 0;

export async function render(containerId, url) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    const token = ++currentRenderToken;
    container.innerHTML = '<p class="text-muted">Loading PDF...</p>';

    try {
        const pdf = await pdfjsLib.getDocument(url).promise;
        if (token !== currentRenderToken) {
            return; // a newer render request has started; abandon this one
        }

        container.innerHTML = '';

        for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
            if (token !== currentRenderToken) {
                return;
            }

            const page = await pdf.getPage(pageNum);
            const viewport = page.getViewport({ scale: 1.5 });

            const canvas = document.createElement('canvas');
            canvas.width = viewport.width;
            canvas.height = viewport.height;
            canvas.style.display = 'block';
            canvas.style.marginBottom = '8px';
            canvas.style.maxWidth = '100%';
            canvas.style.height = 'auto';
            container.appendChild(canvas);

            const context = canvas.getContext('2d');
            await page.render({ canvasContext: context, viewport }).promise;
        }
    } catch (err) {
        if (token === currentRenderToken) {
            container.innerHTML = '<p class="text-danger">Failed to render PDF.</p>';
        }
        console.error('PDF render failed', err);
    }
}
