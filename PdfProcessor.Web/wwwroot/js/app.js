// JavaScript helper functions for PDF Processor
// Add this to wwwroot/js/app.js

window.downloadFile = function (fileName, base64Data) {
    const linkSource = `data:application/octet-stream;base64,${base64Data}`;
    const downloadLink = document.createElement('a');
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
};

window.triggerFileInput = function (inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.click();
    }
};