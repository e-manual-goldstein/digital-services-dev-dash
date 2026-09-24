window.devDashDownload = {
    downloadText: function (fileName, text, mimeType) {
        const blob = new Blob([text], { type: mimeType || 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName || 'download';
        anchor.click();
        URL.revokeObjectURL(url);
    }
};
