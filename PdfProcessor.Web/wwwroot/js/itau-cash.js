console.log('🟢 Itaú Cash - Script carregado');

// Configuração da API
const API_BASE_URL = window.location.hostname === 'localhost' 
  ? 'http://localhost:5239' 
  : 'http://10.0.0.50:5239';

console.log('🔧 API Base URL:', API_BASE_URL);

// Estado global
let selectedFiles = [];

// IMPORTANTE: Usar window.addEventListener para garantir que funciona
window.addEventListener('load', function() {
  console.log('🔧 Página carregada - Iniciando configuração...');
  setupFileInput();
});

function setupFileInput() {
  console.log('🔧 Tentando configurar file input...');
  
  const fileInput = document.getElementById('fileInput');
  
  if (!fileInput) {
    console.error('❌ ERRO: Elemento #fileInput não encontrado!');
    console.log('🔍 Elementos disponíveis:', document.querySelectorAll('input[type="file"]'));
    
    // Tentar novamente após 500ms
    setTimeout(setupFileInput, 500);
    return;
  }
  
  console.log('✅ Elemento #fileInput encontrado:', fileInput);
  
  // Remover event listeners antigos (se existirem)
  fileInput.removeEventListener('change', handleFileSelection);
  
  // Adicionar event listener
  fileInput.addEventListener('change', handleFileSelection);
  
  console.log('✅ Event listener configurado com sucesso');
  
  // Testar se está funcionando
  console.log('🧪 Teste: clique no botão de upload para verificar');
}

function handleFileSelection(event) {
  console.log('\n📂 ========== ARQUIVO SELECIONADO ==========');
  console.log('Event:', event);
  console.log('Files:', event.target.files);
  console.log('Total de arquivos:', event.target.files.length);
  
  const files = Array.from(event.target.files);
  selectedFiles = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(`\n📄 Arquivo ${i + 1}:`, file.name, '(', file.size, 'bytes)');

    // Validar PDF
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      console.warn('⚠️ Arquivo não é PDF:', file.name);
      showMessage('Apenas arquivos PDF são permitidos', 'error');
      continue;
    }

    // Validar tamanho
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
    console.log('📭 Nenhum arquivo selecionado - escondendo UI');
    filesSelected.style.display = 'none';
    actionButtons.style.display = 'none';
    return;
  }

  console.log('📋 Mostrando', selectedFiles.length, 'arquivo(s)');

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
  
  showLoading(true, `Processando ${selectedFiles.length} extrato(s) do Itaú Cash...`);

  try {
    const formData = new FormData();

    // Adicionar arquivos ao FormData
    for (let i = 0; i < selectedFiles.length; i++) {
      formData.append('files', selectedFiles[i]);
      console.log(`📎 Arquivo ${i + 1} adicionado ao FormData:`, selectedFiles[i].name);
    }

    const apiUrl = `${API_BASE_URL}/api/itaucash/batch`;
    console.log('📡 Enviando para:', apiUrl);

    const response = await fetch(apiUrl, {
      method: 'POST',
      body: formData
    });

    console.log('📊 Resposta recebida:', response.status, response.statusText);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('❌ Erro da API:', errorText);
      throw new Error(`Erro ${response.status}: ${errorText}`);
    }

    // Download do arquivo
    const blob = await response.blob();
    console.log('📦 Blob recebido:', blob.size, 'bytes');
    
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:]/g, '').replace('T', '_');
    const fileName = `itau_cash_${timestamp}.xlsx`;

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
  }
  
  statusMessage.style.display = 'block';
}

console.log('✅ Script itau-cash.js carregado completamente');