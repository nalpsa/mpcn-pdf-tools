console.log('🟢 Página CompressPdf carregada');

// Configuração da API
const API_BASE_URL = window.location.hostname === 'localhost' 
? 'http://localhost:5239' 
: 'http://10.0.0.50:5239';

console.log('🔧 API configurada:', API_BASE_URL);

// Estado global
let selectedFiles = [];

// Adicionar event listener quando DOM carregar
document.addEventListener('DOMContentLoaded', function() {
const fileInput = document.getElementById('fileInput');
if (fileInput) {
    fileInput.addEventListener('change', handleFileSelection);
    console.log('✅ Event listener adicionado');
} else {
    console.error('❌ Elemento fileInput não encontrado');
}
});

async function handleFileSelection(event) {
console.log('\n📂 Arquivos selecionados:', event.target.files.length);

const files = Array.from(event.target.files);
selectedFiles = [];

for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(`\n📄 Processando arquivo ${i + 1}/${files.length}: ${file.name}`);

    if (!file.name.toLowerCase().endsWith('.pdf')) {
    showMessage('Apenas arquivos PDF são permitidos', 'error');
    continue;
    }

    if (file.size > 16 * 1024 * 1024) { // 16MB
    showMessage(`Arquivo ${file.name} excede 16MB`, 'error');
    continue;
    }

    selectedFiles.push({
    file: file,
    name: file.name,
    size: file.size,
    compressionLevel: 'Medium',
    removeImages: false
    });

    console.log(`✅ Arquivo adicionado: ${file.name}`);
}

console.log(`\n🎨 Renderizando grid com ${selectedFiles.length} arquivos`);
renderFilesGrid();
updateFileCounter();
}

function renderFilesGrid() {
const grid = document.getElementById('filesGrid');
const counter = document.getElementById('fileCounter');
const actionButtons = document.getElementById('actionButtons');

if (selectedFiles.length === 0) {
    grid.innerHTML = '';
    counter.style.display = 'none';
    actionButtons.style.display = 'none';
    console.log('📭 Nenhum arquivo para renderizar');
    return;
}

counter.style.display = 'block';
actionButtons.style.display = 'flex';

// Renderizar como tabela
grid.innerHTML = `
    <table class="files-table">
    <thead>
        <tr>
        <th>Arquivo</th>
        <th>Tamanho</th>
        <th>Nível de Compressão</th>
        <th>Opções</th>
        </tr>
    </thead>
    <tbody>
        ${selectedFiles.map((fileData, index) => `
        <tr data-index="${index}">
            <td>
            <div class="file-name-cell">
                <span class="file-icon">📄</span>
                <span class="file-name">${fileData.name}</span>
            </div>
            </td>
            <td>
            <span class="file-size">${formatFileSize(fileData.size)}</span>
            </td>
            <td class="compression-cell">
            <select class="compression-select" onchange="setCompression(${index}, this.value)">
                <option value="Low" ${fileData.compressionLevel === 'Low' ? 'selected' : ''}>
                🟢 Baixa (melhor qualidade)
                </option>
                <option value="Medium" ${fileData.compressionLevel === 'Medium' ? 'selected' : ''}>
                🟡 Média (balanceado)
                </option>
                <option value="High" ${fileData.compressionLevel === 'High' ? 'selected' : ''}>
                🔴 Alta (menor tamanho)
                </option>
            </select>
            </td>
            <td class="options-cell">
            <div class="remove-images-checkbox">
                <input type="checkbox" 
                        id="removeImages${index}" 
                        ${fileData.removeImages ? 'checked' : ''}
                        onchange="toggleRemoveImages(${index})" />
                <label for="removeImages${index}">Remover imagens</label>
            </div>
            </td>
        </tr>
        `).join('')}
    </tbody>
    </table>
`;

console.log('✅ Tabela renderizada');
}

function updateFileCounter() {
const counterText = document.getElementById('counterText');
counterText.textContent = `Arquivos Carregados: ${selectedFiles.length}`;
}

function setCompression(index, level) {
selectedFiles[index].compressionLevel = level;
renderFilesGrid();
console.log(`🔧 Arquivo ${index}: compressão alterada para ${level}`);
}

function toggleRemoveImages(index) {
selectedFiles[index].removeImages = !selectedFiles[index].removeImages;
console.log(`🖼️ Arquivo ${index}: remover imagens = ${selectedFiles[index].removeImages}`);
}

function applyCompressionToAll(level) {
selectedFiles.forEach(file => {
    file.compressionLevel = level;
});
renderFilesGrid();
console.log(`🎯 Compressão ${level} aplicada a todos os ${selectedFiles.length} arquivos`);
}

async function compressAllFiles() {
if (selectedFiles.length === 0) {
    showMessage('Selecione pelo menos um arquivo PDF', 'error');
    return;
}

console.log(`\n🗜️ Iniciando compressão de ${selectedFiles.length} arquivo(s)`);

showLoading(true, `Comprimindo ${selectedFiles.length} arquivo(s)...`);

try {
    const formData = new FormData();

    // Adicionar cada arquivo com suas configurações
    for (let i = 0; i < selectedFiles.length; i++) {
    const fileData = selectedFiles[i];
    formData.append('files', fileData.file);
    formData.append('compressionLevels', fileData.compressionLevel);
    formData.append('removeImages', fileData.removeImages.toString());
    }

    console.log('📡 Enviando requisição para API...');
    const response = await fetch(`${API_BASE_URL}/api/compresspdf/batch`, {
    method: 'POST',
    body: formData
    });

    console.log(`📊 Resposta da API: ${response.status} ${response.statusText}`);

    if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Erro na API: ${response.status} - ${errorText}`);
    }

    // Obter o nome do arquivo do header
    const contentDisposition = response.headers.get('Content-Disposition');
    let fileName = selectedFiles.length === 1
    ? `${selectedFiles[0].name.replace('.pdf', '')}_comprimido.pdf`
    : `pdfs_comprimidos_${new Date().toISOString().slice(0, 10)}.zip`;

    if (contentDisposition) {
    const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
    if (matches && matches[1]) {
        fileName = matches[1].replace(/['"]/g, '');
    }
    }

    // Download do arquivo
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log(`✅ Download iniciado: ${fileName}`);
    showMessage(`✅ ${selectedFiles.length} arquivo(s) comprimido(s) com sucesso!`, 'success');
    
    // Limpar após sucesso
    setTimeout(clearAllFiles, 2000);

} catch (error) {
    console.error('❌ Erro ao comprimir:', error);
    showMessage(`❌ Erro ao comprimir: ${error.message}`, 'error');
} finally {
    showLoading(false);
}
}

function clearAllFiles() {
selectedFiles = [];
const fileInput = document.getElementById('fileInput');
if (fileInput) {
    fileInput.value = '';
}
renderFilesGrid();
updateFileCounter();
showMessage('', '');
console.log('🗑️ Todos os arquivos removidos');
}

function formatFileSize(bytes) {
if (bytes === 0) return '0 B';
const k = 1024;
const sizes = ['B', 'KB', 'MB', 'GB'];
const i = Math.floor(Math.log(bytes) / Math.log(k));
return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showLoading(show, text = 'Processando...') {
const loading = document.getElementById('loadingIndicator');
const loadingText = document.getElementById('loadingText');

if (show) {
    loading.style.display = 'block';
    loadingText.textContent = text;
} else {
    loading.style.display = 'none';
}
}

function showMessage(message, type) {
const statusMessage = document.getElementById('statusMessage');

if (!message) {
    statusMessage.style.display = 'none';
    return;
}

statusMessage.textContent = message;
statusMessage.className = 'alert';

if (type === 'success') {
    statusMessage.classList.add('alert-success');
} else if (type === 'error') {
    statusMessage.classList.add('alert-error');
} else {
    statusMessage.classList.add('alert-info');
}

statusMessage.style.display = 'block';
}