// ================================================================
// ITAÚ MOVIMENTAÇÃO - Script de Upload Simplificado
// ================================================================
// Este arquivo usa o sistema compartilhado PdfUploadHandler
// Apenas define a lógica específica do Itaú Movimentação
// ================================================================

console.log("🟢 Itaú Movimentação - Script carregado");

// ============================================================
// ESTADO GLOBAL
// ============================================================

let selectedFiles = [];

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl() {
  return window.PdfProcessorConfig.getEndpoint("/api/itaumovimentacao/batch");
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
  const actionButtons = document.getElementById("actionButtons");

  if (loading) {
    loading.style.display = show ? "flex" : "none";
  }

  if (loadingText && text) {
    loadingText.textContent = text;
  }

  if (actionButtons) {
    actionButtons.style.display = show ? "none" : "flex";
  }
}

function formatFileSize(bytes) {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
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
      <span class="file-size">${formatFileSize(file.size)}</span>
    </div>
  `,
    )
    .join("");

  // Atualizar resumo
  if (filesSummary) {
    const totalSize = selectedFiles.reduce((sum, f) => sum + f.size, 0);
    filesSummary.textContent = `Total: ${selectedFiles.length} arquivo(s) • ${formatFileSize(totalSize)}`;
  }

  // Habilitar botão de processar
  if (processBtn) {
    processBtn.disabled = false;
  }
}

// ============================================================
// FILE MANAGEMENT
// ============================================================

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
    `Processando ${selectedFiles.length} extrato(s) do Itaú...`,
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
      mode: "cors",
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

    const timestamp = new Date()
      .toISOString()
      .slice(0, 19)
      .replace(/[-:]/g, "")
      .replace("T", "_");
    a.download = `movimentacao_itau_${timestamp}.xlsx`;

    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log("✅ Download iniciado com sucesso");

    showMessage(
      "✅ Processamento concluído! Excel gerado e baixado.",
      "success",
    );

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
// MODAL LAYOUT CONTROL
// ============================================================

function initLayoutModal() {
  console.log("🔧 Inicializando modal de layout...");
  
  const btnViewLayout = document.getElementById("btnViewLayout");
  const btnCloseModal = document.getElementById("btnCloseModal");
  const modal = document.getElementById("layoutModal");

  if (!btnViewLayout) {
    console.error("❌ Botão btnViewLayout não encontrado!");
    return;
  }

  if (!btnCloseModal) {
    console.error("❌ Botão btnCloseModal não encontrado!");
    return;
  }

  if (!modal) {
    console.error("❌ Modal layoutModal não encontrado!");
    return;
  }

  console.log("✅ Elementos do modal encontrados");

  // Abrir modal
  btnViewLayout.addEventListener("click", function () {
    console.log("🖼️ Abrindo modal de layout");
    modal.classList.add("show");
  });

  // Fechar modal - botão X
  btnCloseModal.addEventListener("click", function () {
    console.log("❌ Fechando modal (botão X)");
    modal.classList.remove("show");
  });

  // Fechar modal - clique fora
  modal.addEventListener("click", function (event) {
    if (event.target === modal) {
      console.log("❌ Fechando modal (clique fora)");
      modal.classList.remove("show");
    }
  });

  // Fechar modal - tecla ESC
  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape" && modal.classList.contains("show")) {
      console.log("❌ Fechando modal (ESC)");
      modal.classList.remove("show");
    }
  });

  console.log("✅ Modal de layout inicializado com sucesso!");
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

// Tornar função disponível globalmente para reinicialização
window.initializePdfUpload = function () {
  console.log("🔧 Inicializando Itaú Movimentação Upload Handler...");

  window.PdfUploadHandler.init({
    uploadAreaId: "uploadArea",
    fileInputId: "fileInput",
    onFilesSelected: onFilesSelected,
    maxFileSize: 16 * 1024 * 1024, // 16MB
    allowMultiple: true,
    debug: true,
  });
};

  setTimeout(initLayoutModal, 100);

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

console.log("✅ Script itau-movimentacao.js carregado completamente");
