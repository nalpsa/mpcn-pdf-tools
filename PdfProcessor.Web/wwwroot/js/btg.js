// ================================================================
// BTG PACTUAL - Script de Upload Simplificado
// ================================================================
// Este arquivo usa o sistema compartilhado PdfUploadHandler
// Apenas define a lógica específica do BTG Pactual
// ================================================================

console.log("🟢 BTG Pactual - Script carregado");

// ============================================================
// ESTADO GLOBAL
// ============================================================

let selectedFiles = [];

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl() {
  return window.PdfProcessorConfig.getEndpoint("/api/Btg/batch");
}

// ============================================================
// UI FUNCTIONS
// ============================================================

function showMessage(message, type = "info") {
  const statusMessage = document.getElementById("statusMessage");
  if (!statusMessage) return;

  statusMessage.textContent = message;
  statusMessage.className = `alert alert-${type}`;
  statusMessage.style.display = "block";

  if (type === "success" || type === "error") {
    setTimeout(() => {
      statusMessage.style.display = "none";
    }, 5000);
  }
}

function showLoading(show, text = "Processando...") {
  const loading = document.getElementById("loadingIndicator");
  const loadingText = document.getElementById("loadingText");

  if (loading) {
    loading.style.display = show ? "flex" : "none";
  }

  if (loadingText && text) {
    loadingText.textContent = text;
  }
}

function updateUI() {
  const filesSelected = document.getElementById("filesSelected");
  const filesList = document.getElementById("filesList");
  const filesSummary = document.getElementById("filesSummary");
  const actionButtons = document.getElementById("actionButtons");
  const processBtn = document.getElementById("processBtn");

  if (!filesSelected || !filesList || !actionButtons) return;

  if (selectedFiles.length === 0) {
    filesSelected.style.display = "none";
    actionButtons.style.display = "none";
    return;
  }

  // Mostrar lista de arquivos
  filesSelected.style.display = "block";
  actionButtons.style.display = "flex";

  // Renderizar lista
  filesList.innerHTML = selectedFiles
    .map(
      (file, index) => `
    <div class="file-item">
      <span class="file-icon">📄</span>
      <span class="file-name">${file.name}</span>
      <span class="file-size">${(file.size / 1024).toFixed(2)} KB</span>
      <button class="btn-remove" onclick="removeFile(${index})">🗑️</button>
    </div>
  `,
    )
    .join("");

  // Atualizar resumo
  if (filesSummary) {
    const totalSize = selectedFiles.reduce((sum, f) => sum + f.size, 0);
    filesSummary.innerHTML = `
      <strong>Total:</strong> ${selectedFiles.length} arquivo(s) - 
      ${(totalSize / 1024).toFixed(2)} KB
    `;
  }

  // Habilitar botão de processar
  if (processBtn) {
    processBtn.disabled = false;
    processBtn.textContent = `🦁 Processar ${selectedFiles.length} arquivo(s)`;
  }
}

// ============================================================
// FILE MANAGEMENT
// ============================================================

function removeFile(index) {
  console.log("🗑️ Removendo arquivo:", selectedFiles[index].name);
  selectedFiles.splice(index, 1);
  updateUI();
}

function clearFiles() {
  console.log("🗑️ Limpando todos os arquivos");
  selectedFiles = [];
  updateUI();
  showMessage("", "info"); // Limpar mensagens
}

// ============================================================
// CALLBACK DE ARQUIVOS SELECIONADOS
// ============================================================

function onFilesSelected(validFiles, errors) {
  console.log("📁 Callback recebido:", {
    validFiles: validFiles.length,
    errors: errors.length,
  });

  // Adicionar arquivos válidos
  selectedFiles = validFiles;

  // Mostrar erros se houver
  if (errors.length > 0) {
    showMessage(errors[0], "error");
  } else if (validFiles.length > 0) {
    showMessage(`${validFiles.length} arquivo(s) selecionado(s)`, "success");
  }

  // Atualizar interface
  updateUI();
}

// ============================================================
// PROCESSAR ARQUIVOS
// ============================================================

async function processFiles() {
  if (selectedFiles.length === 0) {
    showMessage("Nenhum arquivo selecionado", "error");
    return;
  }

  console.log(
    "🚀 Iniciando processamento de",
    selectedFiles.length,
    "arquivo(s)",
  );

  showLoading(
    true,
    `Processando ${selectedFiles.length} extrato(s) do BTG Pactual...`,
  );
  showMessage("", "info"); // Limpar mensagens anteriores

  try {
    // Criar FormData
    const formData = new FormData();
    selectedFiles.forEach((file) => {
      formData.append("files", file);
    });

    console.log("📤 Enviando para API:", getApiUrl());

    // Fazer requisição
    const response = await fetch(getApiUrl(), {
      method: "POST",
      body: formData,
    });

    console.log("📥 Resposta recebida:", response.status, response.statusText);

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Erro ${response.status}: ${errorText}`);
    }

    // Download do arquivo Excel
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `BTG_Pactual_Transactions_${new Date().toISOString().split("T")[0]}.xlsx`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log("✅ Download iniciado com sucesso");

    showMessage("✅ Excel gerado e baixado com sucesso!", "success");

    // Limpar arquivos após sucesso
    setTimeout(() => clearFiles(), 2000);
  } catch (error) {
    console.error("❌ Erro no processamento:", error);
    showMessage(`Erro: ${error.message}`, "error");
  } finally {
    showLoading(false);
  }
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

// Tornar função disponível globalmente para reinicialização
window.initializePdfUpload = function () {
  console.log("🔧 Inicializando BTG Pactual Upload Handler...");

  window.PdfUploadHandler.init({
    uploadAreaId: "uploadArea",
    fileInputId: "fileInput",
    onFilesSelected: onFilesSelected,
    maxFileSize: 16 * 1024 * 1024, // 16MB
    allowMultiple: true,
    debug: true,
  });
};

// Inicialização automática
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", window.initializePdfUpload);
} else {
  // DOM já carregado (navegação SPA do Blazor)
  setTimeout(window.initializePdfUpload, 50);
}

// ============================================================
// FUNÇÕES GLOBAIS (chamadas pelo HTML)
// ============================================================

window.processFiles = processFiles;
window.clearFiles = clearFiles;
window.removeFile = removeFile;
