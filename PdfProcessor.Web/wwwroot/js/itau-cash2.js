// ================================================================
// ITAÚ CASH 2.0 - Script de Upload Simplificado
// ================================================================
// Este arquivo usa o sistema compartilhado PdfUploadHandler
// Apenas define a lógica específica do Itaú Cash 2.0
// ================================================================

console.log('🟧 Itaú Cash 2.0 - Script carregado');

// ============================================================
// ESTADO GLOBAL
// ============================================================

let selectedFiles = [];

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl() {
  return window.PdfProcessorConfig.getEndpoint('/api/ItauCash2/process');
}

// ============================================================
// UI FUNCTIONS ESPECÍFICAS DO ITAÚ CASH 2.0
// ============================================================

function showLoading() {
  document.getElementById('loadingSection').style.display = 'block';
  document.getElementById('resultSection').style.display = 'none';
  document.getElementById('errorSection').style.display = 'none';
  document.getElementById('fileList').style.display = 'none';
}

function hideLoading() {
  document.getElementById('loadingSection').style.display = 'none';
}

function showSuccess(filename, url) {
  document.getElementById('resultMessage').textContent = `Arquivo gerado: ${filename}`;
  document.getElementById('downloadLink').href = url;
  document.getElementById('downloadLink').download = filename;
  document.getElementById('resultSection').style.display = 'block';
}

function showError(message) {
  document.getElementById('errorMessage').textContent = message;
  document.getElementById('errorSection').style.display = 'block';
}

function updateUI() {
  const fileList = document.getElementById('fileList');
  const fileListContent = document.getElementById('fileListContent');

  if (selectedFiles.length === 0) {
    fileList.style.display = 'none';
    return;
  }

  // Renderizar lista
  let html = '<ul>';
  selectedFiles.forEach((file) => {
    html += `<li>📄 ${file.name} (${(file.size / 1024).toFixed(2)} KB)</li>`;
  });
  html += '</ul>';

  fileListContent.innerHTML = html;
  fileList.style.display = 'block';

  console.log(`📋 Mostrando ${selectedFiles.length} arquivo(s)`);
}

// ============================================================
// FILE MANAGEMENT
// ============================================================

function clearFiles() {
  console.log('🗑️ Limpando todos os arquivos');
  selectedFiles = [];
  updateUI();

  // Esconder sections
  document.getElementById('resultSection').style.display = 'none';
  document.getElementById('errorSection').style.display = 'none';
}

// ============================================================
// CALLBACK DE ARQUIVOS SELECIONADOS
// ============================================================

function onFilesSelected(validFiles, errors) {
  console.log('📁 Callback recebido:', {
    validFiles: validFiles.length,
    errors: errors.length,
  });

  // Adicionar arquivos válidos
  selectedFiles = validFiles;

  // Mostrar erros se houver
  if (errors.length > 0) {
    showError(errors[0]);
  }

  // Atualizar interface
  updateUI();
}

// ============================================================
// PROCESSAR ARQUIVOS
// ============================================================

async function processFiles() {
  if (selectedFiles.length === 0) {
    showError('Selecione pelo menos um arquivo PDF');
    return;
  }

  console.log('🚀 Iniciando processamento de', selectedFiles.length, 'arquivo(s)');

  showLoading();

  try {
    // Criar FormData
    const formData = new FormData();
    selectedFiles.forEach((file, index) => {
      formData.append('files', file);
      console.log(`📎 Arquivo ${index + 1} adicionado: ${file.name}`);
    });

    console.log('📤 Enviando para API:', getApiUrl());

    // Fazer requisição
    const response = await fetch(getApiUrl(), {
      method: 'POST',
      body: formData,
    });

    console.log('📥 Resposta recebida:', response.status, response.statusText);

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Erro ${response.status}: ${errorText}`);
    }

    // Download do arquivo Excel
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);

    // Tentar extrair filename do header
    const filename =
      response.headers.get('content-disposition')?.split('filename=')[1] ||
      `ItauCash2_Transactions_${new Date().toISOString().split('T')[0]}.xlsx`;

    console.log('✅ Sucesso! Arquivo:', filename);

    showSuccess(filename, url);
  } catch (error) {
    console.error('❌ Erro no processamento:', error);
    showError(error.message);
  } finally {
    hideLoading();
  }
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

// Tornar função disponível globalmente para reinicialização
window.initializePdfUpload = function () {
  console.log('🔧 Inicializando Itaú Cash 2.0 Upload Handler...');

  window.PdfUploadHandler.init({
    uploadAreaId: 'uploadArea',
    fileInputId: 'fileInput',
    onFilesSelected: onFilesSelected,
    maxFileSize: 16 * 1024 * 1024, // 16MB
    allowMultiple: true,
    debug: true,
  });

  // Adicionar listener ao botão processar
  const processBtn = document.getElementById('processBtn');
  if (processBtn) {
    processBtn.addEventListener('click', processFiles);
    console.log('✅ Botão processar configurado');
  }
};

// Inicialização automática
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', window.initializePdfUpload);
} else {
  // DOM já carregado (navegação SPA do Blazor)
  setTimeout(window.initializePdfUpload, 50);
}

// ============================================================
// FUNÇÕES GLOBAIS (chamadas pelo HTML)
// ============================================================

window.processFiles = processFiles;
window.clearFiles = clearFiles;

console.log('✅ Script itau-cash2.js carregado completamente');