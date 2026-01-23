// ================================================================
// COMPRESS PDF - Script Simplificado - ÚLTIMA FERRAMENTA! 🎉
// ================================================================
// Usa sistema compartilhado para upload básico
// Mantém lógica específica de níveis de compressão e opções
// ================================================================

console.log("🟢 CompressPDF - Script carregado - ÚLTIMA FERRAMENTA!");

// ============================================================
// ESTADO GLOBAL
// ============================================================

let selectedFiles = [];

// ============================================================
// API CONFIGURATION
// ============================================================

function getApiUrl(endpoint = "/batch") {
  return window.PdfProcessorConfig.getEndpoint(`/api/compresspdf${endpoint}`);
}

// ============================================================
// FILE PROCESSING
// ============================================================

function processFiles(files) {
  selectedFiles = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(
      `📄 Processando arquivo ${i + 1}/${files.length}: ${file.name}`,
    );

    selectedFiles.push({
      file: file,
      name: file.name,
      size: file.size,
      compressionLevel: "Medium", // Padrão
      removeImages: false,
    });

    console.log(`✅ Arquivo adicionado: ${file.name}`);
  }

  console.log(`\n🎨 Renderizando grid com ${selectedFiles.length} arquivos`);
  renderFilesGrid();
  updateFileCounter();
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
// RENDERIZAÇÃO DA TABELA
// ============================================================

function renderFilesGrid() {
  const grid = document.getElementById("filesGrid");
  const counter = document.getElementById("fileCounter");
  const actionButtons = document.getElementById("actionButtons");

  if (selectedFiles.length === 0) {
    grid.innerHTML = "";
    counter.style.display = "none";
    actionButtons.style.display = "none";
    console.log("📭 Nenhum arquivo para renderizar");
    return;
  }

  counter.style.display = "block";
  actionButtons.style.display = "flex";

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
        ${selectedFiles
          .map(
            (fileData, index) => `
          <tr data-index="${index}">
            <td data-label="Arquivo">
              <div class="file-name-cell">
                <span class="file-icon">📄</span>
                <span class="file-name">${fileData.name}</span>
              </div>
            </td>
            <td data-label="Tamanho">
              <span class="file-size">${formatFileSize(fileData.size)}</span>
            </td>
            <td class="compression-cell" data-label="Compressão">
              <select class="compression-select" onchange="setCompression(${index}, this.value)">
                <option value="Low" ${fileData.compressionLevel === "Low" ? "selected" : ""}>
                  🟢 Baixa (melhor qualidade)
                </option>
                <option value="Medium" ${fileData.compressionLevel === "Medium" ? "selected" : ""}>
                  🟡 Média (balanceado)
                </option>
                <option value="High" ${fileData.compressionLevel === "High" ? "selected" : ""}>
                  🔴 Alta (menor tamanho)
                </option>
              </select>
            </td>
            <td class="options-cell" data-label="Opções">
              <div class="remove-images-checkbox">
                <input type="checkbox" 
                       id="removeImages${index}" 
                       ${fileData.removeImages ? "checked" : ""}
                       onchange="toggleRemoveImages(${index})" />
                <label for="removeImages${index}">Remover imagens</label>
              </div>
            </td>
          </tr>
        `,
          )
          .join("")}
      </tbody>
    </table>
  `;

  console.log("✅ Tabela renderizada");
}

function formatFileSize(bytes) {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
}

// ============================================================
// FILE SETTINGS MANAGEMENT
// ============================================================

function setCompression(index, level) {
  selectedFiles[index].compressionLevel = level;
  renderFilesGrid();
  console.log(`🔧 Arquivo ${index}: compressão alterada para ${level}`);
}

function toggleRemoveImages(index) {
  selectedFiles[index].removeImages = !selectedFiles[index].removeImages;
  console.log(
    `🖼️ Arquivo ${index}: remover imagens = ${selectedFiles[index].removeImages}`,
  );
}

function applyCompressionToAll(level) {
  selectedFiles.forEach((file) => {
    file.compressionLevel = level;
  });
  renderFilesGrid();
  console.log(
    `🎯 Compressão ${level} aplicada a todos os ${selectedFiles.length} arquivos`,
  );
}

// ============================================================
// UI UPDATE
// ============================================================

function updateFileCounter() {
  const counterText = document.getElementById("counterText");
  if (counterText) {
    counterText.textContent = `Arquivos Carregados: ${selectedFiles.length}`;
  }
}

// ============================================================
// COMPRESS EXECUTION
// ============================================================

async function compressAllFiles() {
  if (selectedFiles.length === 0) {
    showMessage("Selecione pelo menos um arquivo PDF", "error");
    return;
  }

  console.log(
    `\n🗜️ Iniciando compressão de ${selectedFiles.length} arquivo(s)`,
  );

  showLoading(true, `Comprimindo ${selectedFiles.length} arquivo(s)...`);

  try {
    const formData = new FormData();

    // Adicionar cada arquivo com suas configurações
    for (let i = 0; i < selectedFiles.length; i++) {
      const fileData = selectedFiles[i];
      formData.append("files", fileData.file);
      formData.append("compressionLevels", fileData.compressionLevel);
      formData.append("removeImages", fileData.removeImages.toString());
    }

    console.log("📡 Enviando requisição para API...");
    const response = await fetch(getApiUrl("/batch"), {
      method: "POST",
      body: formData,
      mode: "cors",
    });

    console.log(
      `📊 Resposta da API: ${response.status} ${response.statusText}`,
    );

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Erro na API: ${response.status} - ${errorText}`);
    }

    // Obter o nome do arquivo do header
    const contentDisposition = response.headers.get("Content-Disposition");
    let fileName =
      selectedFiles.length === 1
        ? `${selectedFiles[0].name.replace(".pdf", "")}_comprimido.pdf`
        : `pdfs_comprimidos_${new Date().toISOString().slice(0, 10)}.zip`;

    if (contentDisposition) {
      const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(
        contentDisposition,
      );
      if (matches && matches[1]) {
        fileName = matches[1].replace(/['"]/g, "");
      }
    }

    // Download do arquivo
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    console.log(`✅ Download iniciado: ${fileName}`);
    showMessage(
      `✅ ${selectedFiles.length} arquivo(s) comprimido(s) com sucesso!`,
      "success",
    );

    // Limpar após sucesso
    setTimeout(clearAllFiles, 2000);
  } catch (error) {
    console.error("❌ Erro ao comprimir:", error);
    showMessage(`❌ Erro ao comprimir: ${error.message}`, "error");
  } finally {
    showLoading(false);
  }
}

// ============================================================
// CLEAR
// ============================================================

function clearAllFiles() {
  selectedFiles = [];
  const fileInput = document.getElementById("fileInput");
  if (fileInput) {
    fileInput.value = "";
  }
  renderFilesGrid();
  updateFileCounter();
  showMessage("", "");
  console.log("🗑️ Todos os arquivos removidos");
}

// ============================================================
// UI HELPERS
// ============================================================

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
}

// ============================================================
// INICIALIZAÇÃO
// ============================================================

window.initializePdfUpload = function () {
  console.log("🔧 Inicializando Compress PDF Upload Handler...");

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

window.compressAllFiles = compressAllFiles;
window.clearAllFiles = clearAllFiles;
window.setCompression = setCompression;
window.toggleRemoveImages = toggleRemoveImages;
window.applyCompressionToAll = applyCompressionToAll;

console.log(
  "✅ Script compress-pdf.js carregado completamente - ÚLTIMA FERRAMENTA COMPLETA! 🎉",
);
