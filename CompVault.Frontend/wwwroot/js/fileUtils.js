/**
 * Laster ned en fil i nettleseren
 */
export function downloadFile(fileName, contentType, base64Data) {
    const url = createBlobUrl(base64Data, contentType);

    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();

    URL.revokeObjectURL(url);
}

/**
 * Åpner en fil i ny fane
 */
export function openFile(fileName, contentType, base64Data) {
    const url = createBlobUrl(base64Data, contentType);
    window.open(url, '_blank');
    // Ikke revoke — nettleseren trenger URL-en åpen mens brukeren leser
}

/**
 * Bygger en blob-URL fra base64-data
 */
export function createBlobUrl(base64Data, contentType) {
    const bytes = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: contentType });
    return URL.createObjectURL(blob);
}