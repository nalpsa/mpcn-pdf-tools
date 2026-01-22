// ================================================================
// PDF UPLOAD HANDLER - Sistema Universal de Drag & Drop
// ================================================================
// Sistema robusto de upload de PDFs que funciona perfeitamente com:
// - Blazor SPA navigation
// - Múltiplas páginas
// - Recarregamento de página
// - Navegação entre rotas
// ================================================================

window.PdfUploadHandler = (function () {
  "use strict";

  // ============================================================
  // ESTADO PRIVADO
  // ============================================================

  let config = {
    uploadAreaId: "uploadArea",
    fileInputId: "fileInput",
    onFilesSelected: null,
    maxFileSize: 16 * 1024 * 1024, // 16MB
    allowMultiple: true,
    debug: true,
  };

  let listeners = []; // Guardar referências para cleanup

  // ============================================================
  // LOGGING
  // ============================================================

  function log(...args) {
    if (config.debug) {
      console.log("[PdfUploadHandler]", ...args);
    }
  }

  function error(...args) {
    console.error("[PdfUploadHandler]", ...args);
  }

  // ============================================================
  // CLEANUP - Remove todos os listeners anteriores
  // ============================================================

  function cleanup() {
    log("🧹 Limpando listeners antigos...");

    listeners.forEach((item) => {
      item.element.removeEventListener(item.event, item.handler);
    });

    listeners = [];
    log("✅ Cleanup concluído");
  }

  // ============================================================
  // ADICIONAR LISTENER COM TRACKING
  // ============================================================

  function addTrackedListener(element, event, handler) {
    element.addEventListener(event, handler);
    listeners.push({ element, event, handler });
  }

  // ============================================================
  // VALIDAÇÃO DE ARQUIVOS
  // ============================================================

  function validateFile(file) {
    // Validar tipo
    if (!file.name.toLowerCase().endsWith(".pdf")) {
      return { valid: false, error: "Apenas arquivos PDF são permitidos" };
    }

    // Validar tamanho
    if (file.size > config.maxFileSize) {
      const maxMB = (config.maxFileSize / (1024 * 1024)).toFixed(0);
      return { valid: false, error: `Arquivo ${file.name} excede ${maxMB}MB` };
    }

    // Validar que não está vazio
    if (file.size === 0) {
      return { valid: false, error: `Arquivo ${file.name} está vazio` };
    }

    return { valid: true };
  }

  // ============================================================
  // PROCESSAR ARQUIVOS
  // ============================================================

  function processFiles(files) {
    log("📂 Processando arquivos...", files);

    if (!files || files.length === 0) {
      log("⚠️ Nenhum arquivo recebido");
      return;
    }

    const validFiles = [];
    const errors = [];

    // Validar cada arquivo
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const validation = validateFile(file);

      if (validation.valid) {
        validFiles.push(file);
        log(
          `✅ Arquivo válido: ${file.name} (${(file.size / 1024).toFixed(2)} KB)`,
        );
      } else {
        errors.push(validation.error);
        error(`❌ ${validation.error}`);
      }
    }

    // Chamar callback com resultados
    if (config.onFilesSelected) {
      config.onFilesSelected(validFiles, errors);
    }
  }

  // ============================================================
  // DRAG & DROP HANDLERS
  // ============================================================

  function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
  }

  function handleDragEnter(e, uploadArea) {
    preventDefaults(e);
    uploadArea.classList.add("drag-over");
  }

  function handleDragOver(e) {
    preventDefaults(e);
  }

  function handleDragLeave(e, uploadArea) {
    preventDefaults(e);
    uploadArea.classList.remove("drag-over");
  }

  function handleDrop(e, uploadArea) {
    preventDefaults(e);
    uploadArea.classList.remove("drag-over");

    log("📁 Arquivos arrastados");
    const files = e.dataTransfer.files;
    processFiles(files);
  }

  // ============================================================
  // FILE INPUT HANDLER
  // ============================================================

  function handleFileInputChange(e) {
    log("📁 Arquivos selecionados via input");
    const files = e.target.files;
    processFiles(files);

    // Limpar input para permitir selecionar o mesmo arquivo novamente
    e.target.value = "";
  }

  // ============================================================
  // SETUP DO DRAG & DROP
  // ============================================================

  function setupDragAndDrop(uploadArea, fileInput) {
    log("🔧 Configurando Drag & Drop...");

    // ✅ CRITICAL FIX: Desabilitar pointer events nos elementos filhos
    // Isso evita que ícones e textos bloqueiem o drag & drop
    const children = uploadArea.querySelectorAll("*");
    children.forEach((child) => {
      child.style.pointerEvents = "none";
    });
    log(
      `✅ Desabilitados pointer-events em ${children.length} elementos filhos`,
    );

    // Prevenir comportamento padrão em toda a página
    const preventDefaultsHandler = (e) => preventDefaults(e);
    addTrackedListener(document.body, "dragenter", preventDefaultsHandler);
    addTrackedListener(document.body, "dragover", preventDefaultsHandler);
    addTrackedListener(document.body, "dragleave", preventDefaultsHandler);
    addTrackedListener(document.body, "drop", preventDefaultsHandler);

    // Handlers específicos da área de upload
    addTrackedListener(uploadArea, "dragenter", (e) =>
      handleDragEnter(e, uploadArea),
    );
    addTrackedListener(uploadArea, "dragover", handleDragOver);
    addTrackedListener(uploadArea, "dragleave", (e) =>
      handleDragLeave(e, uploadArea),
    );
    addTrackedListener(uploadArea, "drop", (e) => handleDrop(e, uploadArea));

    // Click para abrir seletor de arquivo
    const clickHandler = (e) => {
      e.preventDefault();
      e.stopPropagation();
      log("🖱️ Upload area clicada");
      fileInput.click();
    };
    addTrackedListener(uploadArea, "click", clickHandler);

    // Change do input
    addTrackedListener(fileInput, "change", handleFileInputChange);

    log("✅ Drag & Drop configurado com sucesso");
  }

  // ============================================================
  // FUNÇÃO PRINCIPAL DE INICIALIZAÇÃO
  // ============================================================

  function waitForElements(callback, maxAttempts = 20, interval = 100) {
    let attempts = 0;

    function check() {
      attempts++;

      const uploadArea = document.getElementById(config.uploadAreaId);
      const fileInput = document.getElementById(config.fileInputId);

      if (uploadArea && fileInput) {
        log("✅ Elementos encontrados:", { uploadArea, fileInput });
        callback(uploadArea, fileInput);
        return true;
      }

      if (attempts >= maxAttempts) {
        error(
          `❌ Timeout: Elementos não encontrados após ${maxAttempts} tentativas`,
        );
        error(
          `   Procurando por: #${config.uploadAreaId}, #${config.fileInputId}`,
        );
        return false;
      }

      log(`⏳ Tentativa ${attempts}/${maxAttempts} - Aguardando elementos...`);
      setTimeout(check, interval);
    }

    check();
  }

  function initialize(options = {}) {
    log("🚀 Inicializando PDF Upload Handler...");

    // Fazer cleanup de inicializações anteriores
    cleanup();

    // Atualizar configuração
    config = { ...config, ...options };

    // Validar callback obrigatório
    if (typeof config.onFilesSelected !== "function") {
      error("❌ ERRO: onFilesSelected callback é obrigatório!");
      return false;
    }

    // Aguardar elementos no DOM (necessário para Blazor SPA)
    waitForElements((uploadArea, fileInput) => {
      setupDragAndDrop(uploadArea, fileInput);
      log("🎉 Sistema inicializado com sucesso!");
    });

    return true;
  }

  // ============================================================
  // INTERFACE PÚBLICA
  // ============================================================

  return {
    init: initialize,
    cleanup: cleanup,

    // Utility para reconfigurar
    setDebug: function (enabled) {
      config.debug = enabled;
    },
  };
})();

// ============================================================
// AUTO-INICIALIZAÇÃO PARA BLAZOR
// ============================================================
// Este código detecta quando o Blazor termina de renderizar
// e reinicializa o handler automaticamente
// ============================================================

(function () {
  "use strict";

  let blazorInitialized = false;

  // Detectar enhanced navigation do Blazor
  if (window.Blazor) {
    console.log(
      "[PdfUploadHandler] 🔵 Blazor detectado - configurando listeners...",
    );

    // Blazor 8+ usa este evento
    window.addEventListener("enhancedload", function () {
      console.log("[PdfUploadHandler] 🔄 Blazor enhanced navigation detectada");

      // Dar tempo para o DOM atualizar
      setTimeout(function () {
        if (window.initializePdfUpload) {
          console.log(
            "[PdfUploadHandler] 🔄 Reinicializando após navegação...",
          );
          window.initializePdfUpload();
        }
      }, 100);
    });
  }

  // Fallback para navegação tradicional
  document.addEventListener("DOMContentLoaded", function () {
    console.log("[PdfUploadHandler] 📄 DOMContentLoaded disparado");
    blazorInitialized = true;
  });

  // Fallback adicional
  window.addEventListener("load", function () {
    if (!blazorInitialized) {
      console.log(
        "[PdfUploadHandler] 🔄 Window load - inicialização de segurança",
      );
    }
  });
})();
