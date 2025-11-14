// fileUploadHelper.js
window.fileUploadHelper = {
    initializeFileInput: function (dropZoneId, inputFileId) {
        const dropZone = document.getElementById(dropZoneId);
        const inputFile = document.getElementById(inputFileId);

        if (!dropZone || !inputFile) {
            console.error('Drop zone ou input file não encontrado');
            return;
        }

        // Prevenir comportamento padrão do navegador
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, preventDefaults, false);
            document.body.addEventListener(eventName, preventDefaults, false);
        });

        function preventDefaults(e) {
            e.preventDefault();
            e.stopPropagation();
        }

        // Visual feedback
        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, highlight, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, unhighlight, false);
        });

        function highlight() {
            dropZone.classList.add('dragging');
        }

        function unhighlight() {
            dropZone.classList.remove('dragging');
        }

        // Handle drop
        dropZone.addEventListener('drop', handleDrop, false);

        function handleDrop(e) {
            const dt = e.dataTransfer;
            const files = dt.files;

            // Transferir arquivos para o input
            inputFile.files = files;

            // Disparar evento change manualmente
            const event = new Event('change', { bubbles: true });
            inputFile.dispatchEvent(event);
        }

        // Handle click
        dropZone.addEventListener('click', function (e) {
            // Não disparar se clicar em um card PDF
            if (!e.target.closest('.pdf-card')) {
                inputFile.click();
            }
        });
    },

    triggerFileInput: function (inputFileId) {
        const inputFile = document.getElementById(inputFileId);
        if (inputFile) {
            inputFile.click();
        }
    }
};