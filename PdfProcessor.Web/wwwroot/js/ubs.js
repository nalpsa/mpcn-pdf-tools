console.log("🟢 UBS Switzerland carregado");

// Detectar configuração de rede
console.log("🔍 Detectando configuração de rede...");
console.log("  Hostname:", window.location.hostname);
console.log("  Port:", window.location.port);

let API_BASE_URL;

if (
  window.location.hostname === "localhost" ||
  window.location.hostname === "127.0.0.1"
) {
  API_BASE_URL = "http://localhost:5239";
  console.log("✅ Modo: LOCALHOST");
} else {
  const detectedIp = window.location.hostname;
  API_BASE_URL = `http://${detectedIp}:5239`;
  console.log("✅ Modo: REDE LOCAL (IP detectado)");
}

console.log("  API URL:", API_BASE_URL);

const API_URL = `${API_BASE_URL}/api/ubs/batch`;
console.log("🔧 API configurada:", API_URL);

let selectedFiles = [];

document.addEventListener("DOMContentLoaded", function () {
  console.log("🔧 DOM carregado - Iniciando configuração...");

  const fileInput = document.getElementById("fileInput");
  const uploadArea = document.getElementById("uploadArea");

  console.log("🔧 Configurando file input...");

  if (!fileInput || !uploadArea) {
    console.error("❌ Elementos não encontrados:", { fileInput, uploadArea });
    return;
  }

  console.log("✅ Elementos encontrados:", { fileInput, uploadArea });

  // Click no upload area
  uploadArea.addEventListener("click", () => {
    console.log("🖱️ Upload area clicada");
    fileInput.click();
  });

  // Seleção de arquivos
  fileInput.addEventListener("change", (e) => {
    console.log("📂 Arquivos selecionados via input");
    handleFiles(e.target.files);
  });

  console.log("✅ File input configurado com sucesso");

  // Drag & Drop
  console.log("🔧 Configurando Drag & Drop...");

  uploadArea.addEventListener("dragover", (e) => {
    e.preventDefault();
    uploadArea.classList.add("drag-over");
  });

  uploadArea.addEventListener("dragleave", () => {
    uploadArea.classList.remove("drag-over");
  });

  uploadArea.addEventListener("drop", (e) => {
    e.preventDefault();
    uploadArea.classList.remove("drag-over");
    console.log("\n📂 ========== ARQUIVOS ARRASTADOS ==========");
    console.log("Files:", e.dataTransfer.files);
    console.log("Total de arquivos:", e.dataTransfer.files.length);
    handleFiles(e.dataTransfer.files);
  });

  console.log("✅ Drag & Drop configurado com sucesso");

  // Botão processar
  const processBtn = document.getElementById("processBtn");
  if (processBtn) {
    processBtn.addEventListener("click", processFiles);
  }
});

function handleFiles(files) {
  console.log("\n📂 ========== HANDLE FILES ==========");
  console.log("Files recebidos:", files);
  console.log("Quantidade:", files.length);

  selectedFiles = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    console.log(`\n📄 Arquivo ${i + 1}: ${file.name} (${file.size} bytes)`);

    if (file.type === "application/pdf") {
      selectedFiles.push(file);
      console.log("✅ Arquivo válido adicionado");
    } else {
      console.warn("⚠️ Arquivo ignorado (não é PDF)");
    }
  }

  console.log(`\n📋 Total de arquivos válidos: ${selectedFiles.length}`);
  console.log("🎨 Renderizando lista...");
  renderFileList();
  console.log("========================================\n");
}

function renderFileList() {
  console.log("🎨 Renderizando lista de arquivos...");
  const fileList = document.getElementById("fileList");
  const fileListContent = document.getElementById("fileListContent");

  if (selectedFiles.length === 0) {
    fileList.style.display = "none";
    return;
  }

  let html = "<ul>";
  selectedFiles.forEach((file, index) => {
    html += `<li>📄 ${file.name} (${(file.size / 1024).toFixed(2)} KB)</li>`;
  });
  html += "</ul>";

  fileListContent.innerHTML = html;
  fileList.style.display = "block";

  console.log(`📋 Mostrando ${selectedFiles.length} arquivo(s)`);
  console.log("✅ Lista renderizada com sucesso");
}

async function processFiles() {
  console.log("\n🏦 ========== INICIANDO PROCESSAMENTO ==========");

  if (selectedFiles.length === 0) {
    alert("Selecione pelo menos um arquivo PDF");
    return;
  }

  console.log(`📊 Processando ${selectedFiles.length} arquivo(s)`);

  const formData = new FormData();
  selectedFiles.forEach((file, index) => {
    formData.append("files", file);
    console.log(`📎 Arquivo ${index + 1} adicionado ao FormData: ${file.name}`);
  });

  console.log(`📡 Enviando para: ${API_URL}`);

  showLoading();

  try {
    const response = await fetch(API_URL, {
      method: "POST",
      body: formData,
    });

    console.log(
      `📊 Resposta recebida: ${response.status} ${response.statusText}`,
    );

    if (response.ok) {
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const filename =
        response.headers.get("content-disposition")?.split("filename=")[1] ||
        "ubs_switzerland.xlsx";

      showSuccess(filename, url);
      console.log("✅ Sucesso! Arquivo Excel gerado");
    } else {
      const errorText = await response.text();
      console.error(" ❌ Erro da API:", errorText);
      showError(errorText);
    }
  } catch (error) {
    console.error(" ❌ ERRO no processamento:", error);
    showError(error.message);
  } finally {
    hideLoading();
  }

  console.log("========================================\n");
}

function showLoading() {
  document.getElementById("loadingSection").style.display = "block";
  document.getElementById("resultSection").style.display = "none";
  document.getElementById("errorSection").style.display = "none";
}

function hideLoading() {
  document.getElementById("loadingSection").style.display = "none";
}

function showSuccess(filename, url) {
  document.getElementById("resultMessage").textContent =
    `Arquivo gerado: ${filename}`;
  document.getElementById("downloadLink").href = url;
  document.getElementById("downloadLink").download = filename;
  document.getElementById("resultSection").style.display = "block";
}

function showError(message) {
  document.getElementById("errorMessage").textContent = message;
  document.getElementById("errorSection").style.display = "block";
}

console.log("✅ Script ubs.js carregado completamente");
