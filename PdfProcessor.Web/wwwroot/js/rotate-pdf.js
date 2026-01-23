// ================================================================
// ROTATE PDF - Script Simplificado
// ================================================================
// Usa sistema compartilhado para upload básico
// Mantém lógica específica de thumbnails, rotação e grid
// ================================================================

console.log("🟢 RotatePDF - Script carregado");

// ============================================================
// ESTADO GLOBAL
// ============================================================

let pdfFiles = [];
let selectedFiles = new Set();
let nextFileId = 1;

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl(endpoint = "/batch") {
  return window.PdfProcessorConfig.getEndpoint(`/api/rotatepdf${endpoint}`);
}

// ============================================================
// THUMBNAIL GENERATION
// ============================================================

async function generateThumbnail(file) {
  try {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(getApiUrl("/thumbnail"), {
      method: "POST",
      body: formData,
      mode: "cors",
    });

    if (!response.ok) {
      throw new Error("Erro ao gerar miniatura");
    }

    const data = await response.json();
    return data.thumbnail;
  } catch (error) {
    console.error("Erro ao gerar thumbnail:", error);
    return null;
  }
}

// ============================================================
// FILE PROCESSING (ESPECÍFICO)
// ============================================================

async function processFiles(files) {
  if (!files || files.length === 0) return;

  showLoading(true, "Carregando arquivos e gerando miniaturas...");

  let processedCount = 0;

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(`📄 Processando: ${file.name}`);

    // Gerar miniatura via API
    const thumbnail = await generateThumbnail(file);

    // Adicionar à lista
    pdfFiles.push({
      id: nextFileId++,
      file: file,
      name: file.name,
      size: file.size,
      rotation: 0,
      thumbnail: thumbnail,
    });

    processedCount++;
  }

  // Atualizar UI
  renderPdfGrid();
  updateUI();
  showLoading(false);

  if (processedCount > 0) {
    showMessage(
      `✅ ${processedCount} arquivo(s) carregado(s) com sucesso!`,
      "success",
    );
  }
}

// ============================================================
// CALLBACK DO SISTEMA COMPARTILHADO
// ============================================================

function onFilesSelected(validFiles, errors) {
  console.log("📁 Callback recebido:", {
    validFiles: validFiles.length,
    errors: errors.length,
  });

  // Mostrar erros se houver
  if (errors.length > 0) {
    showMessage(errors[0], "error");
  }

  // Processar arquivos válidos
  if (validFiles.length > 0) {
    processFiles(validFiles);
  }
}

// ============================================================
// PDF GRID RENDERING
// ============================================================

function renderPdfGrid() {
  const grid = document.getElementById("pdfGrid");
  grid.innerHTML = "";

  pdfFiles.forEach((pdf) => {
    const isSelected = selectedFiles.has(pdf.id);
    const card = document.createElement("div");
    card.className = `pdf-card ${isSelected ? "selected" : ""}`;
    card.onclick = () => toggleSelection(pdf.id);

    const thumbnailHtml = pdf.thumbnail
      ? `<img src="${pdf.thumbnail}" style="transform: rotate(${pdf.rotation}deg);" alt="${pdf.name}" />`
      : `<div class="pdf-icon" style="transform: rotate(${pdf.rotation}deg);">📄</div>`;

    card.innerHTML = `
      <div class="pdf-checkbox">
        <input type="checkbox" ${isSelected ? "checked" : ""} readonly />
      </div>
      <div class="pdf-thumbnail">
        ${thumbnailHtml}
      </div>
      <div class="pdf-info">
        <div class="pdf-name" title="${pdf.name}">${pdf.name}</div>
        <div class="pdf-size">${formatFileSize(pdf.size)}</div>
        ${pdf.rotation !== 0 ? `<span class="rotation-badge">${pdf.rotation}°</span>` : ""}
      </div>
    `;

    grid.appendChild(card);
  });
}

function formatFileSize(bytes) {
  if (bytes < 1024) return bytes + " B";
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
  return (bytes / 1024 / 1024).toFixed(1) + " MB";
}

// ============================================================
// SELECTION MANAGEMENT
// ============================================================

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
    pdfFiles.forEach((pdf) => selectedFiles.add(pdf.id));
  }
  renderPdfGrid();
  updateSelectAllButton();
}

function updateSelectAllButton() {
  const btn = document.getElementById("selectAllText");
  if (!btn) return;

  const text =
    selectedFiles.size === pdfFiles.length
      ? "Desmarcar Todos"
      : "✓ Selecionar Todos";
  btn.textContent = text;
}

// ============================================================
// ROTATION
// ============================================================

function rotateSelected(degrees) {
  let rotatedCount = 0;
  pdfFiles.forEach((pdf) => {
    if (selectedFiles.has(pdf.id)) {
      pdf.rotation = (pdf.rotation + degrees) % 360;
      if (pdf.rotation < 0) pdf.rotation += 360;
      rotatedCount++;
    }
  });

  renderPdfGrid();
  showMessage(`✅ ${rotatedCount} arquivo(s) rotacionado(s)`, "success");
}

// ============================================================
// DOWNLOAD
// ============================================================

async function processDownload() {
  if (pdfFiles.length === 0) {
    showMessage("❌ Nenhum arquivo para processar", "error");
    return;
  }

  showLoading(true, "Processando PDFs... Aguarde...");

  try {
    const formData = new FormData();

    // Adicionar arquivos e rotações
    pdfFiles.forEach((pdf) => {
      formData.append("files", pdf.file);
      formData.append("rotations", pdf.rotation.toString());
    });

    // Chamar API
    const response = await fetch(getApiUrl("/batch"), {
      method: "POST",
      body: formData,
      mode: "cors",
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || "Erro ao processar PDFs");
    }

    // Baixar arquivo
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;

    // Nome do arquivo baseado na resposta
    const contentDisposition = response.headers.get("Content-Disposition");
    let filename = "pdfs_rotacionados.zip";

    if (contentDisposition) {
      const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(
        contentDisposition,
      );
      if (matches != null && matches[1]) {
        filename = matches[1].replace(/['"]/g, "");
      }
    }

    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    showMessage("✅ Download concluído com sucesso!", "success");
  } catch (error) {
    console.error("Erro no download:", error);
    showMessage(`❌ Erro: ${error.message}`, "error");
  } finally {
    showLoading(false);
  }
}

// ============================================================
// CLEAR ALL
// ============================================================

function clearAll() {
  if (confirm("Deseja limpar todos os arquivos?")) {
    pdfFiles = [];
    selectedFiles.clear();
    updateUI();
    renderPdfGrid();
    showMessage("🗑️ Arquivos limpos", "info");
  }
}

// ============================================================
// UI UPDATE
// ============================================================

function updateUI() {
  const counterText = document.getElementById("counterText");
  const fileCounter = document.getElementById("fileCounter");
  const previewSection = document.getElementById("previewSection");

  const count = pdfFiles.length;

  if (counterText) {
    counterText.textContent = `Arquivos carregados: ${count}`;
  }

  if (fileCounter) {
    fileCounter.style.display = count > 0 ? "block" : "none";
  }

  if (previewSection) {
    previewSection.style.display = count > 0 ? "block" : "none";
  }

  updateSelectAllButton();
}

function showLoading(show, text = "Processando...") {
  const loading = document.getElementById("loadingIndicator");
  const loadingText = document.getElementById("loadingText");

  if (loading) {
    loading.style.display = show ? "block" : "none";
  }

  if (loadingText && text) {
    loadingText.textContent = text;
  }
}

function showMessage(message, type = "info") {
  const statusMessage = document.getElementById("statusMessage");

  if (!message) {
    statusMessage.style.display = "none";
    return;
  }

  statusMessage.textContent = message;
  statusMessage.className = `alert alert-${type}`;
  statusMessage.style.display = "block";

  // Auto-hide após 5 segundos (exceto errors)
  if (type !== "error") {
    setTimeout(() => {
      statusMessage.style.display = "none";
    }, 5000);
  }
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

window.initializePdfUpload = function () {
  console.log("🔧 Inicializando Rotate PDF Upload Handler...");

  window.PdfUploadHandler.init({
    uploadAreaId: "uploadArea",
    fileInputId: "fileInput",
    onFilesSelected: onFilesSelected,
    maxFileSize: 16 * 1024 * 1024,
    allowMultiple: true,
    debug: true,
  });
};

// Inicialização automática
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", window.initializePdfUpload);
} else {
  setTimeout(window.initializePdfUpload, 50);
}

// ============================================================
// FUNÇÕES GLOBAIS (chamadas pelo HTML)
// ============================================================

window.toggleSelectAll = toggleSelectAll;
window.rotateSelected = rotateSelected;
window.processDownload = processDownload;
window.clearAll = clearAll;

console.log("✅ Script rotate-pdf.js carregado completamente");
