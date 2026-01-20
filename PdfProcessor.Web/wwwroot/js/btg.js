console.log('🟢 BTG Pactual carregado');

// ✅ CONFIGURAÇÃO DINÂMICA DA API
function getApiBaseUrl() {
  const hostname = window.location.hostname;
  const port = window.location.port;
  
  console.log('🔍 Detectando configuração de rede...');
  console.log('  Hostname:', hostname);
  console.log('  Port:', port);
  
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    console.log('✅ Modo: LOCALHOST');
    return 'http://localhost:5239';
  }
  
  if (hostname.match(/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/)) {
    const apiUrl = `http://${hostname}:5239`;
    console.log('✅ Modo: REDE LOCAL (IP detectado)');
    console.log('  API URL:', apiUrl);
    return apiUrl;
  }
  
  console.log('⚠️ Modo: FALLBACK para localhost');
  return 'http://localhost:5239';
}

const API_BASE_URL = getApiBaseUrl();
console.log('🔧 API configurada:', API_BASE_URL);

let selectedFiles = [];

document.addEventListener('DOMContentLoaded', function () {
  console.log('🔧 DOM carregado - Iniciando configuração...');
  setupFileInput();
  setupDragAndDrop();
});

function setupFileInput() {
  console.log('🔧 Configurando file input...');
  
  const fileInput = document.getElementById('fileInput');
  const uploadArea = document.getElementById('uploadArea');
  
  if (!fileInput) {
    console.error('❌ ERRO: Elemento #fileInput não encontrado!');
    setTimeout(setupFileInput, 200);
    return;
  }
  
  if (!uploadArea) {
    console.error('❌ ERRO: Elemento #uploadArea não encontrado!');
    setTimeout(setupFileInput, 200);
    return;
  }
  
  console.log('✅ Elementos encontrados:', { fileInput, uploadArea });
  
  const newFileInput = fileInput.cloneNode(true);
  fileInput.parentNode.replaceChild(newFileInput, fileInput);
  
  newFileInput.addEventListener('change', handleFileSelection);
  
  uploadArea.onclick = function(e) {
    e.preventDefault();
    e.stopPropagation();
    console.log('🖱️ Upload area clicada');
    newFileInput.click();
  };
  
  console.log('✅ File input configurado com sucesso');
}

function setupDragAndDrop() {
  console.log('🔧 Configurando Drag & Drop...');
  
  const uploadArea = document.getElementById('uploadArea');
  
  if (!uploadArea) {
    console.error('❌ ERRO: Elemento #uploadArea não encontrado!');
    setTimeout(setupDragAndDrop, 200);
    return;
  }
  
  ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    uploadArea.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false);
  });
  
  ['dragenter', 'dragover'].forEach(eventName => {
    uploadArea.addEventListener(eventName, highlight, false);
  });
  
  ['dragleave', 'drop'].forEach(eventName => {
    uploadArea.addEventListener(eventName, unhighlight, false);
  });
  
  uploadArea.addEventListener('drop', handleDrop, false);
  
  console.log('✅ Drag & Drop configurado com sucesso');
}

function preventDefaults(e) {
  e.preventDefault();
  e.stopPropagation();
}

function highlight(e) {
  const uploadArea = document.getElementById('uploadArea');
  uploadArea.classList.add('drag-over');
}

function unhighlight(e) {
  const uploadArea = document.getElementById('uploadArea');
  uploadArea.classList.remove('drag-over');
}

function handleDrop(e) {
  console.log('\n📂 ========== ARQUIVOS ARRASTADOS ==========');
  const dt = e.dataTransfer;
  const files = dt.files;
  
  console.log('Files:', files);
  console.log('Total de arquivos:', files.length);
  
  processDroppedFiles(files);
}

function processDroppedFiles(files) {
  selectedFiles = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(`\n📄 Arquivo ${i + 1}:`, file.name, '(', file.size, 'bytes)');

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      console.warn('⚠️ Arquivo não é PDF:', file.name);
      showMessage('Apenas arquivos PDF são permitidos', 'error');
      continue;
    }

    if (file.size > 16 * 1024 * 1024) {
      console.warn('⚠️ Arquivo muito grande:', file.name);
      showMessage(`Arquivo ${file.name} excede 16MB`, 'error');
      continue;
    }

    selectedFiles.push(file);
    console.log('✅ Arquivo válido adicionado');
  }

  console.log(`\n📋 Total de arquivos válidos: ${selectedFiles.length}`);
  console.log('🎨 Renderizando lista...');
  
  renderFilesList();
  
  console.log('========================================\n');
}

function handleFileSelection(event) {
  console.log('\n📂 ========== ARQUIVO SELECIONADO ==========');
  console.log('Total de arquivos:', event.target.files.length);
  
  processDroppedFiles(event.target.files);
}

function renderFilesList() {
  console.log('🎨 Renderizando lista de arquivos...');
  
  const filesSelected = document.getElementById('filesSelected');
  const filesList = document.getElementById('filesList');
  const filesSummary = document.getElementById('filesSummary');
  const actionButtons = document.getElementById('actionButtons');
  const processBtn = document.getElementById('processBtn');

  if (!filesSelected || !filesList || !filesSummary || !actionButtons || !processBtn) {
    console.error('❌ Elementos da UI não encontrados!');
    return;
  }

  if (selectedFiles.length === 0) {
    console.log('🔭 Nenhum arquivo selecionado - escondendo UI');
    filesSelected.style.display = 'none';
    actionButtons.style.display = 'none';
    return;
  }

  console.log('📋 Mostrando', selectedFiles.length, 'arquivo(s)');

  filesSelected.style.display = 'block';
  actionButtons.style.display = 'flex';

  filesList.innerHTML = selectedFiles.map((file, index) => `
    <div class="file-item">
      <span>📄</span>
      <span>${file.name}</span>
      <span style="margin-left: auto; color: #718096;">${formatFileSize(file.size)}</span>
    </div>
  `).join('');

  const totalSize = selectedFiles.reduce((sum, file) => sum + file.size, 0);
  filesSummary.textContent = `Total: ${selectedFiles.length} arquivo(s) • ${formatFileSize(totalSize)}`;

  processBtn.disabled = false;

  console.log('✅ Lista renderizada com sucesso');
}

async function processFiles() {
  console.log('\n🏦 ========== INICIANDO PROCESSAMENTO ==========');
  
  if (selectedFiles.length === 0) {
    console.warn('⚠️ Nenhum arquivo para processar');
    showMessage('Selecione pelo menos um arquivo PDF', 'error');
    return;
  }

  console.log(`📊 Processando ${selectedFiles.length} arquivo(s)`);
  
  showLoading(true, `Processando ${selectedFiles.length} extrato(s) do BTG Pactual...`);

  try {
    const formData = new FormData();

    for (let i = 0; i < selectedFiles.length; i++) {
      formData.append('files', selectedFiles[i]);
      console.log(`📎 Arquivo ${i + 1} adicionado ao FormData:`, selectedFiles[i].name);
    }

    const apiUrl = `${API_BASE_URL}/api/btg/batch`;
    console.log('📡 Enviando para:', apiUrl);

    const response = await fetch(apiUrl, {
      method: 'POST',
      body: formData,
      mode: 'cors'
    });

    console.log('📊 Resposta recebida:', response.status, response.statusText);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('❌ Erro da API:', errorText);
      throw new Error(`Erro ${response.status}: ${errorText}`);
    }

    const blob = await response.blob();
    console.log('📦 Blob recebido:', blob.size, 'bytes');
    
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_');
    const fileName = `btg_pactual_${timestamp}.xlsx`;

    console.log('💾 Iniciando download:', fileName);

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log('✅ Download concluído!');
    showMessage(`✅ ${selectedFiles.length} extrato(s) processado(s) com sucesso!`, 'success');
    
    setTimeout(clearFiles, 3000);

  } catch (error) {
    console.error('❌ ERRO no processamento:', error);
    showMessage(`❌ Erro: ${error.message}`, 'error');
  } finally {
    showLoading(false);
    console.log('========================================\n');
  }
}

function clearFiles() {
  console.log('🗑️ Limpando arquivos...');
  
  selectedFiles = [];
  
  const fileInput = document.getElementById('fileInput');
  if (fileInput) {
    fileInput.value = '';
  }
  
  renderFilesList();
  showMessage('', '');
  
  console.log('✅ Arquivos limpos');
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

console.log('✅ Script btg.js carregado completamente');