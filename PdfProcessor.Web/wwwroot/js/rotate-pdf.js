let pdfFiles = [];
let selectedFiles = new Set();
let nextFileId = 1;

// URL DA API
const API_BASE_URL = window.location.hostname === 'localhost'
? 'http://localhost:5239'
: 'http://10.0.0.50:5239';

// Inicializar quando a página carregar
window.addEventListener('load', function () {
const fileInput = document.getElementById('fileInput');
if (fileInput) {
    fileInput.addEventListener('change', handleFileSelection);
}
});

async function handleFileSelection() {
const fileInput = document.getElementById('fileInput');
const files = fileInput.files;

if (!files || files.length === 0) return;

showMessage('📤 Carregando arquivos...', 'info');

// Processar cada arquivo
for (let i = 0; i < files.length; i++) {
    const file = files[i];

    // Validar tamanho
    if (file.size > 16 * 1024 * 1024) {
    showMessage(`❌ ${file.name} é muito grande (máx. 16MB)`, 'error');
    continue;
    }

    // Validar tipo
    if (!file.name.toLowerCase().endsWith('.pdf')) {
    showMessage(`❌ ${file.name} não é um PDF`, 'error');
    continue;
    }

    // Gerar miniatura via API
    const thumbnail = await generateThumbnail(file);

    // Adicionar à lista
    pdfFiles.push({
    id: nextFileId++,
    file: file,
    name: file.name,
    size: file.size,
    rotation: 0,
    thumbnail: thumbnail
    });
}

// Limpar input
fileInput.value = '';

// Atualizar UI
renderPdfGrid();
updateUI();
showMessage(`✅ ${files.length} arquivo(s) carregado(s) com sucesso!`, 'success');
}

async function generateThumbnail(file) {
try {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`${API_BASE_URL}/api/rotatepdf/thumbnail`, {
    method: 'POST',
    body: formData
    });

    if (!response.ok) {
    throw new Error('Erro ao gerar miniatura');
    }

    const data = await response.json();
    return data.thumbnail;
} catch (error) {
    console.error('Erro ao gerar thumbnail:', error);
    return null;
}
}

function renderPdfGrid() {
const grid = document.getElementById('pdfGrid');
grid.innerHTML = '';

pdfFiles.forEach(pdf => {
    const isSelected = selectedFiles.has(pdf.id);
    const card = document.createElement('div');
    card.className = `pdf-card ${isSelected ? 'selected' : ''}`;
    card.onclick = () => toggleSelection(pdf.id);

    const thumbnailHtml = pdf.thumbnail
    ? `<img src="${pdf.thumbnail}" style="transform: rotate(${pdf.rotation}deg);" alt="${pdf.name}" />`
    : `<div class="pdf-icon" style="transform: rotate(${pdf.rotation}deg);">📄</div>`;

    card.innerHTML = `
    <div class="pdf-checkbox">
        <input type="checkbox" ${isSelected ? 'checked' : ''} readonly />
    </div>
    <div class="pdf-thumbnail">
        ${thumbnailHtml}
    </div>
    <div class="pdf-info">
        <div class="pdf-name" title="${pdf.name}">${pdf.name}</div>
        <div class="pdf-size">${formatFileSize(pdf.size)}</div>
        ${pdf.rotation !== 0 ? `<span class="rotation-badge">${pdf.rotation}°</span>` : ''}
    </div>
    `;

    grid.appendChild(card);
});
}

function formatFileSize(bytes) {
if (bytes < 1024) return bytes + ' B';
if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
return (bytes / 1024 / 1024).toFixed(1) + ' MB';
}

function toggleSelection(fileId) {
if (selectedFiles.has(fileId)) {
    selectedFiles.delete(fileId);
} else {
    selectedFiles.add(fileId);
}
renderPdfGrid();
updateSelectAllButton();
}

function toggleSelectAll() {
if (selectedFiles.size === pdfFiles.length) {
    selectedFiles.clear();
} else {
    pdfFiles.forEach(pdf => selectedFiles.add(pdf.id));
}
renderPdfGrid();
updateSelectAllButton();
}

function updateSelectAllButton() {
const text = selectedFiles.size === pdfFiles.length
    ? 'Desmarcar Todos'
    : '✓ Selecionar Todos';
document.getElementById('selectAllText').textContent = text;
}

function rotateSelected(degrees) {
let rotatedCount = 0;
pdfFiles.forEach(pdf => {
    if (selectedFiles.has(pdf.id)) {
    pdf.rotation = (pdf.rotation + degrees) % 360;
    if (pdf.rotation < 0) pdf.rotation += 360;
    rotatedCount++;
    }
});

renderPdfGrid();
showMessage(`✅ ${rotatedCount} arquivo(s) rotacionado(s)`, 'success');
}

async function processDownload() {
if (pdfFiles.length === 0) {
    showMessage('❌ Nenhum arquivo para processar', 'error');
    return;
}

showMessage('⏳ Processando PDFs... Aguarde...', 'info');

try {
    const formData = new FormData();

    // Adicionar arquivos e rotações
    pdfFiles.forEach(pdf => {
    formData.append('files', pdf.file);
    formData.append('rotations', pdf.rotation.toString());
    });

    // Chamar API
    const response = await fetch(`${API_BASE_URL}/api/rotatepdf/batch`, {
    method: 'POST',
    body: formData
    });

    if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || 'Erro ao processar PDFs');
    }

    // Baixar arquivo
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;

    // Nome do arquivo baseado na resposta
    const contentDisposition = response.headers.get('Content-Disposition');
    let filename = 'pdfs_rotacionados.zip';

    if (contentDisposition) {
    const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
    if (matches != null && matches[1]) {
        filename = matches[1].replace(/['"]/g, '');
    }
    }

    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    showMessage('✅ Download concluído com sucesso!', 'success');
} catch (error) {
    console.error('Erro no download:', error);
    showMessage(`❌ Erro: ${error.message}`, 'error');
}
}

function clearAll() {
if (confirm('Deseja limpar todos os arquivos?')) {
    pdfFiles = [];
    selectedFiles.clear();
    updateUI();
    renderPdfGrid();
    showMessage('🗑️ Arquivos limpos', 'info');
}
}

function updateUI() {
document.getElementById('fileCount').textContent = pdfFiles.length;
document.getElementById('previewSection').style.display = pdfFiles.length > 0 ? 'block' : 'none';
updateSelectAllButton();
}

function showMessage(message, type) {
const messageArea = document.getElementById('messageArea');
messageArea.textContent = message;
messageArea.className = `message-area ${type}`;

// Auto-hide após 5 segundos (exceto erros)
if (type !== 'error') {
    setTimeout(() => {
    messageArea.style.display = 'none';
    }, 5000);
}
}