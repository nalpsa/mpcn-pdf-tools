console.log('🟢 CompressPdf carregado');

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

// ✅ Adicionar event listener quando DOM carregar
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

async function handleFileSelection(event) {
  console.log('\n📂 ========== ARQUIVOS SELECIONADOS ==========');
  console.log('Total de arquivos:', event.target.files.length);
  
  processFiles(event.target.files);
  
  // Limpar input
  event.target.value = '';
}

function processFiles(files) {
  const filesArray = Array.from(files);
  selectedFiles = [];

  for (let i = 0; i < filesArray.length; i++) {
    const file = filesArray[i];
    console.log(`\n📄 Processando arquivo ${i + 1}/${filesArray.length}: ${file.name}`);

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
  console.log('========================================\n');
}

function renderFilesGrid() {
  const grid = document.getElementById('filesGrid');
  const counter = document.getElementById('fileCounter');
  const actionButtons = document.getElementById('actionButtons');

  if (selectedFiles.length === 0) {
    grid.innerHTML = '';
    counter.style.display = 'none';
    actionButtons.style.display = 'none';
    console.log('🔭 Nenhum arquivo para renderizar');
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
      body: formData,
      mode: 'cors'  // ✅ IMPORTANTE: Especificar modo CORS
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

console.log('✅ Script compress-pdf.js carregado completamente');