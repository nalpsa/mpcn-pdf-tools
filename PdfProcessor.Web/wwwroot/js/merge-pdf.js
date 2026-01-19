console.log('🟢 MergePDF carregado');

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

// Estado global
let selectedFiles = [];
let filePageCounts = {};
let filePageRanges = {};

// ✅ Adicionar event listener
document.addEventListener('DOMContentLoaded', function() {
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

async function handleFileSelection(event) {
  console.log('\n📂 ========== ARQUIVOS SELECIONADOS ==========');
  console.log('Total de arquivos:', event.target.files.length);
  
  await processFiles(event.target.files);
}

async function processFiles(files) {
  console.log(`\n📂 Processando ${files.length} arquivo(s)`);
  
  const filesArray = Array.from(files);
  selectedFiles = [];
  filePageCounts = {};
  filePageRanges = {};

  for (let i = 0; i < filesArray.length; i++) {
    const file = filesArray[i];
    console.log(`\n📄 Processando arquivo ${i + 1}/${filesArray.length}: ${file.name}`);

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      showMessage('Apenas arquivos PDF são permitidos', 'error');
      continue;
    }

    if (file.size > 16 * 1024 * 1024) {
      showMessage(`Arquivo ${file.name} excede 16MB`, 'error');
      continue;
    }

    // Contar páginas do PDF
    const pageCount = await getPageCount(file);
    
    selectedFiles.push(file);
    filePageCounts[i] = pageCount;
    filePageRanges[i] = `1-${pageCount}`; // Range padrão: todas as páginas

    console.log(`✅ Arquivo adicionado: ${file.name} (${pageCount} páginas)`);
  }

  console.log(`\n🎨 Renderizando lista com ${selectedFiles.length} arquivos`);
  renderFilesList();
  updateMergeInfo();
  console.log('========================================\n');
}

async function getPageCount(file) {
  try {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`${API_BASE_URL}/api/mergepdf/pagecount`, {
      method: 'POST',
      body: formData,
      mode: 'cors'  // ✅ IMPORTANTE: Especificar modo CORS
    });

    if (response.ok) {
      const data = await response.json();
      return data.pageCount || '?';
    } else {
      console.warn(`⚠️ Não foi possível contar páginas de ${file.name}`);
      return '?';
    }
  } catch (error) {
    console.error('❌ Erro ao contar páginas:', error);
    return '?';
  }
}

function renderFilesList() {
  const filesArea = document.getElementById('filesArea');
  const counter = document.getElementById('fileCounter');
  const actionButtons = document.getElementById('actionButtons');

  if (selectedFiles.length === 0) {
    filesArea.innerHTML = '';
    counter.style.display = 'none';
    actionButtons.style.display = 'none';
    return;
  }

  counter.style.display = 'block';
  actionButtons.style.display = 'flex';

  filesArea.innerHTML = selectedFiles.map((file, index) => `
    <div class="file-item" data-index="${index}">
      <div class="file-header">
        <div class="file-info">
          <div class="file-number">${index + 1}</div>
          <div class="file-details">
            <div class="file-name">${file.name}</div>
            <div class="file-meta">
              <span>📄 ${filePageCounts[index]} página(s)</span>
              <span>💾 ${formatFileSize(file.size)}</span>
            </div>
          </div>
        </div>
        <div class="file-actions">
          <button class="btn-icon" 
                  onclick="moveFile(${index}, 'up')" 
                  ${index === 0 ? 'disabled' : ''}
                  title="Mover para cima">
            ↑
          </button>
          <button class="btn-icon" 
                  onclick="moveFile(${index}, 'down')" 
                  ${index === selectedFiles.length - 1 ? 'disabled' : ''}
                  title="Mover para baixo">
            ↓
          </button>
          <button class="btn-icon btn-delete" 
                  onclick="removeFile(${index})"
                  title="Remover arquivo">
            🗑️
          </button>
        </div>
      </div>
      
      <div class="page-range-section">
        <label class="page-range-label">Páginas a incluir:</label>
        <input type="text" 
               class="page-range-input" 
               id="range-${index}"
               value="${filePageRanges[index]}"
               onchange="updatePageRange(${index}, this.value)"
               onblur="validatePageRange(${index}, this.value)"
               placeholder="Ex: 1-3, 1,3,5, ou 1-5,9" />
        <div class="page-range-hint">
          Exemplos: "1,3,5" (páginas específicas), "1-5" (intervalo), "1,3-6,9" (misto)
        </div>
        <div class="range-error" id="range-error-${index}"></div>
      </div>
    </div>
  `).join('');

  console.log('✅ Lista renderizada');
}

function updatePageRange(index, range) {
  filePageRanges[index] = range;
  console.log(`📄 Range atualizado para arquivo ${index}: ${range}`);
}

function validatePageRange(index, range) {
  const input = document.getElementById(`range-${index}`);
  const errorDiv = document.getElementById(`range-error-${index}`);
  const maxPages = filePageCounts[index];

  // Limpar erro
  input.classList.remove('error');
  errorDiv.style.display = 'none';

  if (!range.trim()) {
    showRangeError(index, 'Range de páginas não pode estar vazio');
    return false;
  }

  try {
    parsePageRange(range, maxPages);
    console.log(`✅ Range válido para arquivo ${index}`);
    return true;
  } catch (error) {
    showRangeError(index, error.message);
    return false;
  }
}

function showRangeError(index, message) {
  const input = document.getElementById(`range-${index}`);
  const errorDiv = document.getElementById(`range-error-${index}`);

  input.classList.add('error');
  errorDiv.textContent = message;
  errorDiv.style.display = 'block';
}

function parsePageRange(range, maxPages) {
  const parts = range.split(',');

  for (let part of parts) {
    part = part.trim();
    
    if (part.includes('-')) {
      const [start, end] = part.split('-').map(n => parseInt(n.trim()));
      
      if (isNaN(start) || isNaN(end)) {
        throw new Error(`Range inválido: "${part}"`);
      }
      
      if (start > end) {
        throw new Error(`Range inválido: início (${start}) maior que fim (${end})`);
      }
      
      if (maxPages !== '?' && (start > maxPages || end > maxPages)) {
        throw new Error(`Página fora do limite: máximo é ${maxPages}`);
      }
    } else {
      const pageNum = parseInt(part);
      
      if (isNaN(pageNum) || pageNum < 1) {
        throw new Error(`Número de página inválido: "${part}"`);
      }
      
      if (maxPages !== '?' && pageNum > maxPages) {
        throw new Error(`Página ${pageNum} fora do limite: máximo é ${maxPages}`);
      }
    }
  }

  return true;
}

function moveFile(index, direction) {
  if (direction === 'up' && index > 0) {
    [selectedFiles[index], selectedFiles[index - 1]] = [selectedFiles[index - 1], selectedFiles[index]];
    [filePageCounts[index], filePageCounts[index - 1]] = [filePageCounts[index - 1], filePageCounts[index]];
    [filePageRanges[index], filePageRanges[index - 1]] = [filePageRanges[index - 1], filePageRanges[index]];
    
    renderFilesList();
    console.log(`🔼 Arquivo movido para cima: posição ${index + 1} → ${index}`);
  } else if (direction === 'down' && index < selectedFiles.length - 1) {
    [selectedFiles[index], selectedFiles[index + 1]] = [selectedFiles[index + 1], selectedFiles[index]];
    [filePageCounts[index], filePageCounts[index + 1]] = [filePageCounts[index + 1], filePageCounts[index]];
    [filePageRanges[index], filePageRanges[index + 1]] = [filePageRanges[index + 1], filePageRanges[index]];
    
    renderFilesList();
    console.log(`🔽 Arquivo movido para baixo: posição ${index + 1} → ${index + 2}`);
  }
}

function removeFile(index) {
  const fileName = selectedFiles[index].name;
  selectedFiles.splice(index, 1);

  // Reorganizar índices
  const newPageCounts = {};
  const newPageRanges = {};

  let newIndex = 0;
  for (let i = 0; i < selectedFiles.length + 1; i++) {
    if (i !== index) {
      newPageCounts[newIndex] = filePageCounts[i];
      newPageRanges[newIndex] = filePageRanges[i];
      newIndex++;
    }
  }

  filePageCounts = newPageCounts;
  filePageRanges = newPageRanges;

  renderFilesList();
  updateMergeInfo();
  console.log(`🗑️ Arquivo removido: ${fileName}`);
}

function updateMergeInfo() {
  const counterText = document.getElementById('counterText');
  const mergeBtn = document.getElementById('mergeBtn');

  const count = selectedFiles.length;
  counterText.textContent = `Arquivos selecionados: ${count}`;

  mergeBtn.disabled = count < 2;

  if (count < 2) {
    mergeBtn.textContent = '🔗 Selecione pelo menos 2 PDFs';
  } else {
    mergeBtn.textContent = '🔗 Mesclar PDFs';
  }
}

async function mergePdfs() {
  if (selectedFiles.length < 2) {
    showMessage('Selecione pelo menos 2 PDFs para mesclar', 'error');
    return;
  }

  // Validar todos os ranges
  let allValid = true;
  for (let i = 0; i < selectedFiles.length; i++) {
    if (!validatePageRange(i, filePageRanges[i])) {
      allValid = false;
    }
  }

  if (!allValid) {
    showMessage('Por favor, corrija os erros nos ranges de páginas', 'error');
    return;
  }

  console.log(`\n🔗 Iniciando mesclagem de ${selectedFiles.length} arquivos`);

  showLoading(true, `Mesclando ${selectedFiles.length} PDFs...`);

  try {
    const formData = new FormData();

    // Adicionar arquivos na ordem
    for (let i = 0; i < selectedFiles.length; i++) {
      formData.append('files', selectedFiles[i]);
      formData.append('pageRanges', filePageRanges[i]);
    }

    console.log('📡 Enviando para API...');
    const response = await fetch(`${API_BASE_URL}/api/mergepdf/batch`, {
      method: 'POST',
      body: formData,
      mode: 'cors'  // ✅ IMPORTANTE: Especificar modo CORS
    });

    console.log(`📊 Resposta da API: ${response.status} ${response.statusText}`);

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Erro na API: ${response.status} - ${errorText}`);
    }

    // Download do arquivo
    const blob = await response.blob();
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_');
    const fileName = `pdf_mesclado_${timestamp}.pdf`;

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log(`✅ Download iniciado: ${fileName}`);
    showMessage(`✅ ${selectedFiles.length} PDFs mesclados com sucesso!`, 'success');
    
    // Limpar após 2 segundos
    setTimeout(clearAllFiles, 2000);

  } catch (error) {
    console.error('❌ Erro ao mesclar:', error);
    showMessage(`❌ Erro ao mesclar: ${error.message}`, 'error');
  } finally {
    showLoading(false);
  }
}

function clearAllFiles() {
  selectedFiles = [];
  filePageCounts = {};
  filePageRanges = {};

  const fileInput = document.getElementById('fileInput');
  if (fileInput) {
    fileInput.value = '';
  }

  renderFilesList();
  updateMergeInfo();
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

console.log('✅ Script merge-pdf.js carregado completamente');