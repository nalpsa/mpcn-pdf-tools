// ================================================================
// EDIT PDF - Rotação e Remoção de Páginas Individuais
// ================================================================
// Usa sistema compartilhado para upload básico
// Funcionalidade avançada: edição por página individual
// ================================================================

console.log("🟢 EditPDF - Script carregado");

// ============================================================
// ESTADO GLOBAL
// ============================================================

let pdfFile = null;
let pages = []; // Array de {pageNumber, thumbnail, rotation, deleted}

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl(endpoint = "") {
  return window.PdfProcessorConfig.getEndpoint(`/api/editpdf${endpoint}`);
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
    return;
  }

  // Edit PDF aceita apenas 1 arquivo por vez
  if (validFiles.length > 1) {
    showMessage("Por favor, selecione apenas um arquivo PDF por vez", "error");
    return;
  }

  // Processar o arquivo
  if (validFiles.length === 1) {
    loadPdfFile(validFiles[0]);
  }
}

// ============================================================
// CARREGAR PDF E PÁGINAS
// ============================================================

async function loadPdfFile(file) {
  pdfFile = file;
  pages = [];

  console.log(`\n📄 Carregando PDF: ${file.name}`);

  showLoading(true, "Carregando páginas...");

  try {
    // 1. Obter número de páginas
    const pageCount = await getPageCount(file);
    console.log(`📊 PDF tem ${pageCount} páginas`);

    // 2. Mostrar info do arquivo
    showFileInfo(file, pageCount);

    // 3. Carregar thumbnails de todas as páginas
    for (let i = 1; i <= pageCount; i++) {
      console.log(`🖼️ Carregando thumbnail da página ${i}/${pageCount}...`);

      const thumbnail = await loadPageThumbnail(file, i);

      pages.push({
        pageNumber: i,
        thumbnail: thumbnail,
        rotation: 0,
        deleted: false,
      });

      // Renderizar progressivamente
      renderPagesGrid();
      updateStats();
    }

    console.log(`✅ Todas as ${pageCount} páginas carregadas!`);
    showMessage(`✅ ${pageCount} páginas carregadas com sucesso!`, "success");
  } catch (error) {
    console.error("❌ Erro ao carregar PDF:", error);
    showMessage(`❌ Erro ao carregar PDF: ${error.message}`, "error");
  } finally {
    showLoading(false);
  }
}

// ============================================================
// API CALLS
// ============================================================

async function getPageCount(file) {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(getApiUrl("/pagecount"), {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(`Erro ao contar páginas: ${response.status}`);
  }

  const data = await response.json();
  return data.pageCount;
}

async function loadPageThumbnail(file, pageNumber) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("pageNumber", pageNumber);

  const response = await fetch(getApiUrl("/thumbnail"), {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(`Erro ao carregar thumbnail: ${response.status}`);
  }

  const data = await response.json();
  return data.thumbnail;
}

// ============================================================
// RENDERIZAÇÃO
// ============================================================

function showFileInfo(file, pageCount) {
  const container = document.getElementById("fileInfoContainer");

  if (!container) return;

  const sizeFormatted = formatFileSize(file.size);

  container.innerHTML = `
    <div class="file-info-bar">
      <div class="file-info-content">
        <div class="file-info-icon">📄</div>
        <div class="file-info-details">
          <h3>${file.name}</h3>
          <p>${pageCount} páginas • ${sizeFormatted}</p>
        </div>
      </div>
      <div class="file-info-actions">
        <button class="btn-icon-white" onclick="resetAll()">
          🔄 Trocar PDF
        </button>
      </div>
    </div>
  `;

  container.style.display = "block";
}

function renderPagesGrid() {
  const grid = document.getElementById("pagesGrid");
  const section = document.getElementById("pagesSection");

  if (!grid || pages.length === 0) return;

  section.style.display = "block";

  grid.innerHTML = pages
    .map((page, index) => {
      const rotationClass = page.rotation > 0 ? `rotate-${page.rotation}` : "";
      const deletedClass = page.deleted ? "deleted" : "";
      const statusBadge = page.deleted
        ? '<span class="page-status deleted">Será removida</span>'
        : page.rotation > 0
          ? `<span class="page-status rotated">${page.rotation}°</span>`
          : "";

      return `
      <div class="page-card ${deletedClass}" data-page="${index}">
        <div class="page-thumbnail-container">
          <img src="${page.thumbnail}" 
               class="page-thumbnail ${rotationClass}" 
               alt="Página ${page.pageNumber}" />
        </div>
        
        <div class="page-info">
          <div class="page-number">Página ${page.pageNumber}</div>
          ${statusBadge}
        </div>

        <div class="page-controls">
          ${
            page.deleted
              ? `
            <button class="btn-page-control restore" 
                    onclick="restorePage(${index})"
                    title="Restaurar página">
              ↩️
            </button>
          `
              : `
            <button class="btn-page-control rotate-left" 
                    onclick="rotatePage(${index}, -90)"
                    title="Rotacionar 90° esquerda">
              ↶
            </button>
            <button class="btn-page-control rotate-right" 
                    onclick="rotatePage(${index}, 90)"
                    title="Rotacionar 90° direita">
              ↷
            </button>
            <button class="btn-page-control delete" 
                    onclick="deletePage(${index})"
                    title="Remover página">
              🗑️
            </button>
          `
          }
        </div>
      </div>
    `;
    })
    .join("");

  console.log("✅ Grid renderizado");
}

// ============================================================
// PAGE OPERATIONS
// ============================================================

function rotatePage(index, degrees) {
  pages[index].rotation = (pages[index].rotation + degrees + 360) % 360;
  console.log(
    `🔄 Página ${pages[index].pageNumber}: rotação ${pages[index].rotation}°`,
  );
  renderPagesGrid();
  updateStats();
}

function deletePage(index) {
  pages[index].deleted = true;
  console.log(`🗑️ Página ${pages[index].pageNumber}: marcada para remoção`);
  renderPagesGrid();
  updateStats();
}

function restorePage(index) {
  pages[index].deleted = false;
  console.log(`↩️ Página ${pages[index].pageNumber}: restaurada`);
  renderPagesGrid();
  updateStats();
}

function resetRotations() {
  pages.forEach((page) => {
    page.rotation = 0;
  });
  console.log("🔄 Todas as rotações resetadas");
  renderPagesGrid();
  updateStats();
}

function resetDeletions() {
  pages.forEach((page) => {
    page.deleted = false;
  });
  console.log("↩️ Todas as páginas restauradas");
  renderPagesGrid();
  updateStats();
}

// ============================================================
// STATISTICS
// ============================================================

function updateStats() {
  const statsContainer = document.getElementById("actionStats");
  const downloadBtn = document.getElementById("downloadBtn");

  if (!statsContainer) return;

  const totalPages = pages.length;
  const rotatedPages = pages.filter((p) => !p.deleted && p.rotation > 0).length;
  const deletedPages = pages.filter((p) => p.deleted).length;
  const finalPages = totalPages - deletedPages;

  statsContainer.innerHTML = `
    <div class="stat-item">
      <span class="stat-icon">📄</span>
      <span><span class="stat-value">${finalPages}</span> de ${totalPages} páginas</span>
    </div>
    ${
      rotatedPages > 0
        ? `
      <div class="stat-item">
        <span class="stat-icon">🔄</span>
        <span><span class="stat-value">${rotatedPages}</span> rotacionadas</span>
      </div>
    `
        : ""
    }
    ${
      deletedPages > 0
        ? `
      <div class="stat-item">
        <span class="stat-icon">🗑️</span>
        <span><span class="stat-value">${deletedPages}</span> removidas</span>
      </div>
    `
        : ""
    }
  `;

  // Habilitar/desabilitar botão de download
  if (downloadBtn) {
    const hasChanges = rotatedPages > 0 || deletedPages > 0;
    downloadBtn.disabled = !hasChanges || finalPages === 0;

    if (finalPages === 0) {
      downloadBtn.textContent = "⚠️ Nenhuma página restante";
    } else if (!hasChanges) {
      downloadBtn.textContent = "📥 Sem alterações para salvar";
    } else {
      downloadBtn.textContent = `📥 Download PDF (${finalPages} páginas)`;
    }
  }
}

// ============================================================
// DOWNLOAD EDITED PDF
// ============================================================

async function downloadEditedPdf() {
  if (!pdfFile) {
    showMessage("Nenhum PDF carregado", "error");
    return;
  }

  const keptPages = pages.filter((p) => !p.deleted);

  if (keptPages.length === 0) {
    showMessage(
      "Você removeu todas as páginas! Adicione pelo menos uma.",
      "error",
    );
    return;
  }

  console.log(`\n💾 Iniciando download do PDF editado`);
  showLoading(true, "Processando PDF...");

  try {
    const formData = new FormData();
    formData.append("file", pdfFile);

    // Preparar operações por página
    const operations = pages.map((page) => ({
      pageNumber: page.pageNumber,
      rotation: page.rotation,
      keep: !page.deleted,
    }));

    formData.append("pageOperations", JSON.stringify(operations));

    console.log("📡 Enviando para API:", operations);

    const response = await fetch(getApiUrl("/process"), {
      method: "POST",
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Erro na API: ${response.status}`);
    }

    // Download do arquivo
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = pdfFile.name.replace(".pdf", "_edited.pdf");
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log(`✅ Download iniciado`);
    showMessage("✅ PDF editado baixado com sucesso!", "success");
  } catch (error) {
    console.error("❌ Erro ao processar:", error);
    showMessage(`❌ Erro ao processar: ${error.message}`, "error");
  } finally {
    showLoading(false);
  }
}

// ============================================================
// RESET
// ============================================================

function resetAll() {
  pdfFile = null;
  pages = [];

  const fileInput = document.getElementById("fileInput");
  if (fileInput) {
    fileInput.value = "";
  }

  const fileInfoContainer = document.getElementById("fileInfoContainer");
  if (fileInfoContainer) {
    fileInfoContainer.style.display = "none";
  }

  const pagesSection = document.getElementById("pagesSection");
  if (pagesSection) {
    pagesSection.style.display = "none";
  }

  showMessage("", "");
  console.log("🔄 Tudo resetado");
}

// ============================================================
// HELPERS
// ============================================================

function formatFileSize(bytes) {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
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

  if (!statusMessage) return;

  if (!message) {
    statusMessage.style.display = "none";
    return;
  }

  statusMessage.textContent = message;
  statusMessage.className = `alert alert-${type}`;
  statusMessage.style.display = "block";
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

window.initializePdfUpload = function () {
  console.log("🔧 Inicializando Edit PDF Upload Handler...");

  window.PdfUploadHandler.init({
    uploadAreaId: "uploadArea",
    fileInputId: "fileInput",
    onFilesSelected: onFilesSelected,
    maxFileSize: 16 * 1024 * 1024,
    allowMultiple: false, // APENAS 1 arquivo por vez!
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

window.rotatePage = rotatePage;
window.deletePage = deletePage;
window.restorePage = restorePage;
window.resetRotations = resetRotations;
window.resetDeletions = resetDeletions;
window.downloadEditedPdf = downloadEditedPdf;
window.resetAll = resetAll;

console.log("✅ Script edit-pdf.js carregado completamente");
