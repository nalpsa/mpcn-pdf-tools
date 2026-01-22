// ================================================================
// API CONFIGURATION - Sistema Centralizado
// ================================================================
// Este arquivo centraliza a configuração da API para todo o sistema
// Detecta automaticamente se está em localhost ou rede local
// ================================================================

window.PdfProcessorConfig = (function () {
  "use strict";

  // 🔧 Detectar URL base da API automaticamente
  function getApiBaseUrl() {
    const hostname = window.location.hostname;
    const port = window.location.port;

    console.log("🔍 Detectando configuração de rede...");
    console.log("  Hostname:", hostname);
    console.log("  Port:", port);

    // Localhost
    if (hostname === "localhost" || hostname === "127.0.0.1") {
      console.log("✅ Modo: LOCALHOST");
      return "http://localhost:5239";
    }

    // IP de rede local (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
    if (hostname.match(/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/)) {
      const apiUrl = `http://${hostname}:5239`;
      console.log("✅ Modo: REDE LOCAL (IP detectado)");
      console.log("  API URL:", apiUrl);
      return apiUrl;
    }

    // Fallback para localhost
    console.log("⚠️ Modo: FALLBACK para localhost");
    return "http://localhost:5239";
  }

  const API_BASE_URL = getApiBaseUrl();
  console.log("🔧 API Base URL configurada:", API_BASE_URL);

  // Interface pública
  return {
    getApiUrl: function () {
      return API_BASE_URL;
    },

    getEndpoint: function (path) {
      return API_BASE_URL + path;
    },
  };
})();
