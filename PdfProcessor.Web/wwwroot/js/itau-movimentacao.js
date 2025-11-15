console.log('🟢 Itaú Movimentação carregado');

// Configuração da API
const API_BASE_URL = window.location.hostname === 'localhost' 
? 'http://localhost:5239' 
: 'http://10.0.0.50:5239';

console.log('🔧 API configurada:', API_BASE_URL);

// Estado global
let selectedFiles = [];

// Setup - executar quando a página carregar
(function() {
console.log('🔧 Configurando event listeners...');

// Tentar múltiplas vezes até o elemento estar disponível
let attempts = 0;
const maxAttempts = 10;

const setupFileInput = function() {
    const fileInput = document.getElementById('fileInput');
    
    if (fileInput) {
    fileInput.addEventListener('change', handleFileSelection);
    console.log('✅ Event listener adicionado ao fileInput');
    return true;
    } else {
    attempts++;
    if (attempts < maxAttempts) {
        console.log(`⏳ Tentativa ${attempts}/${maxAttempts} - fileInput não encontrado, tentando novamente...`);
        setTimeout(setupFileInput, 100);
    } else {
        console.error('❌ fileInput não encontrado após', maxAttempts, 'tentativas');
    }
    return false;
    }
};

// Iniciar setup
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupFileInput);
} else {
    setupFileInput();
}
})();

function handleFileSelection(event) {
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

    if (file.size > 16 * 1024 * 1024) {
    showMessage(`Arquivo ${file.name} excede 16MB`, 'error');
    continue;
    }

    selectedFiles.push(file);
    console.log(`✅ Arquivo adicionado: ${file.name}`);
}

console.log(`\n🎨 Renderizando lista com ${selectedFiles.length} arquivos`);
renderFilesList();
}

function renderFilesList() {
const filesSelected = document.getElementById('filesSelected');
const filesList = document.getElementById('filesList');
const filesSummary = document.getElementById('filesSummary');
const actionButtons = document.getElementById('actionButtons');
const processBtn = document.getElementById('processBtn');

if (selectedFiles.length === 0) {
    filesSelected.style.display = 'none';
    actionButtons.style.display = 'none';
    return;
}

filesSelected.style.display = 'block';
actionButtons.style.display = 'flex';

// Renderizar lista
filesList.innerHTML = selectedFiles.map((file, index) => `
    <div class="file-item">
    <span>📄</span>
    <span>${file.name}</span>
    <span style="margin-left: auto; color: #718096;">${formatFileSize(file.size)}</span>
    </div>
`).join('');

// Resumo
const totalSize = selectedFiles.reduce((sum, file) => sum + file.size, 0);
filesSummary.textContent = `Total: ${selectedFiles.length} arquivo(s) • ${formatFileSize(totalSize)}`;

// Habilitar botão
processBtn.disabled = false;

console.log('✅ Lista renderizada');
}

async function processFiles() {
if (selectedFiles.length === 0) {
    showMessage('Selecione pelo menos um arquivo PDF', 'error');
    return;
}

console.log(`\n💳 Iniciando processamento de ${selectedFiles.length} arquivo(s)`);

showLoading(true, `Processando ${selectedFiles.length} extrato(s) do Itaú...`);

try {
    const formData = new FormData();

    // Adicionar arquivos
    for (let i = 0; i < selectedFiles.length; i++) {
    formData.append('files', selectedFiles[i]);
    }

    console.log('📡 Enviando para API...');
    const response = await fetch(`${API_BASE_URL}/api/itaumovimentacao/batch`, {
    method: 'POST',
    body: formData
    });

    console.log(`📊 Resposta da API: ${response.status} ${response.statusText}`);

    if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Erro na API: ${response.status} - ${errorText}`);
    }

    // Download do arquivo
    const blob = await response.blob();
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_');
    const fileName = `movimentacao_itau_${timestamp}.xlsx`;

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log(`✅ Download iniciado: ${fileName}`);
    showMessage(`✅ Processamento concluído! ${selectedFiles.length} extrato(s) processado(s).`, 'success');
    
    // Limpar após 3 segundos
    setTimeout(clearFiles, 3000);

} catch (error) {
    console.error('❌ Erro ao processar:', error);
    showMessage(`❌ Erro ao processar: ${error.message}`, 'error');
} finally {
    showLoading(false);
}
}

function clearFiles() {
selectedFiles = [];

const fileInput = document.getElementById('fileInput');
if (fileInput) {
    fileInput.value = '';
}

renderFilesList();
showMessage('', '');
console.log('🗑️ Arquivos limpos');
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
const actionButtons = document.getElementById('actionButtons');

if (show) {
    loading.style.display = 'block';
    loadingText.textContent = text;
    actionButtons.style.display = 'none';
} else {
    loading.style.display = 'none';
    actionButtons.style.display = 'flex';
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