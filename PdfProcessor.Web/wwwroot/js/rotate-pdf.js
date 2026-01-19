console.log('🟢 RotatePDF carregado');

let pdfFiles = [];
let selectedFiles = new Set();
let nextFileId = 1;

// ✅ CONFIGURAÇÃO DINÂMICA DA API
function getApiBaseUrl() {
  const hostname = window.location.hostname;
  const port = window.location.port;
  
  console.log('🔍 Detectando configuração de rede...');
  console.log('  Hostname:', hostname);
  console.log('  Port:', port);
  
  // Se está em localhost, API também está em localhost
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    console.log('✅ Modo: LOCALHOST');
    return 'http://localhost:5239';
  }
  
  // Se está acessando por IP, a API está no mesmo IP
  if (hostname.match(/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/)) {
    const apiUrl = `http://${hostname}:5239`;
    console.log('✅ Modo: REDE LOCAL (IP detectado)');
    console.log('  API URL:', apiUrl);
    return apiUrl;
  }
  
  // Fallback para localhost
  console.log('⚠️ Modo: FALLBACK para localhost');
  return 'http://localhost:5239';
}

const API_BASE_URL = getApiBaseUrl();
console.log('🔧 API configurada:', API_BASE_URL);

// ✅ Inicializar quando DOM estiver pronto
document.addEventListener('DOMContentLoaded', function () {
  console.log('🔧 DOM carregado - Iniciando configuração...');
  setupFileInput();
  setupDragAndDrop();
});

// ✅ Configurar file input
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
  
  // ✅ IMPORTANTE: Clonar elemento para limpar listeners antigos
  const newFileInput = fileInput.cloneNode(true);
  fileInput.parentNode.replaceChild(newFileInput, fileInput);
  
  // ✅ Adicionar event listener
  newFileInput.addEventListener('change', handleFileSelection);
  
  // ✅ Click na área de upload
  uploadArea.onclick = function(e) {
    e.preventDefault();
    e.stopPropagation();
    console.log('🖱️ Upload area clicada');
    newFileInput.click();
  };
  
  console.log('✅ File input configurado com sucesso');
}

// ✅ NOVO: Configurar Drag & Drop
function setupDragAndDrop() {
  console.log('🔧 Configurando Drag & Drop...');
  
  const uploadArea = document.getElementById('uploadArea');
  
  if (!uploadArea) {
    console.error('❌ ERRO: Elemento #uploadArea não encontrado!');
    setTimeout(setupDragAndDrop, 200);
    return;
  }
  
  // ✅ Prevenir comportamento padrão (abrir PDF em nova aba)
  ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    uploadArea.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false);
  });
  
  // ✅ Destacar área ao arrastar
  ['dragenter', 'dragover'].forEach(eventName => {
    uploadArea.addEventListener(eventName, highlight, false);
  });
  
  ['dragleave', 'drop'].forEach(eventName => {
    uploadArea.addEventListener(eventName, unhighlight, false);
  });
  
  // ✅ Lidar com drop
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
  
  // ✅ Processar arquivos arrastados
  processFiles(files);
}

async function handleFileSelection() {
  console.log('\n📂 ========== ARQUIVOS SELECIONADOS ==========');
  const fileInput = document.getElementById('fileInput');
  const files = fileInput.files;
  
  console.log('Total de arquivos:', files.length);
  
  await processFiles(files);
  
  // Limpar input
  fileInput.value = '';
}

async function processFiles(files) {
  if (!files || files.length === 0) return;

  showLoading(true, 'Carregando arquivos e gerando miniaturas...');

  let processedCount = 0;

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

    processedCount++;
  }

  // Atualizar UI
  renderPdfGrid();
  updateUI();
  showLoading(false);
  
  if (processedCount > 0) {
    showMessage(`✅ ${processedCount} arquivo(s) carregado(s) com sucesso!`, 'success');
  }
  
  console.log('========================================\n');
}

async function generateThumbnail(file) {
  try {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`${API_BASE_URL}/api/rotatepdf/thumbnail`, {
      method: 'POST',
      body: formData,
      mode: 'cors'  // ✅ IMPORTANTE: Especificar modo CORS
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
  const btn = document.getElementById('selectAllText');
  if (!btn) return;
  
  const text = selectedFiles.size === pdfFiles.length
    ? 'Desmarcar Todos'
    : '✓ Selecionar Todos';
  btn.textContent = text;
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

  showLoading(true, 'Processando PDFs... Aguarde...');

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
      body: formData,
      mode: 'cors'  // ✅ IMPORTANTE: Especificar modo CORS
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
  } finally {
    showLoading(false);
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
  const counterText = document.getElementById('counterText');
  const fileCounter = document.getElementById('fileCounter');
  const previewSection = document.getElementById('previewSection');
  
  const count = pdfFiles.length;
  
  if (counterText) {
    counterText.textContent = `Arquivos carregados: ${count}`;
  }
  
  if (fileCounter) {
    fileCounter.style.display = count > 0 ? 'block' : 'none';
  }
  
  if (previewSection) {
    previewSection.style.display = count > 0 ? 'block' : 'none';
  }
  
  updateSelectAllButton();
}

function showLoading(show, text = 'Processando...') {
  const loading = document.getElementById('loadingIndicator');
  const loadingText = document.getElementById('loadingText');

  if (show) {
    loading.style.display = 'block';
    if (loadingText) loadingText.textContent = text;
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
  
  // Auto-hide após 5 segundos
  if (type !== 'error') {
    setTimeout(() => {
      statusMessage.style.display = 'none';
    }, 5000);
  }
}

console.log('✅ Script rotate-pdf.js carregado completamente');