#!/bin/bash

echo "================================================"
echo "🔥 REBUILD FORÇADO COMPLETO - PDF PROCESSOR"
echo "================================================"
echo ""

# Encontrar o diretório do projeto
PROJECT_ROOT=$(find /PROJETOS -type d -name "mpcn-pdf-tools" 2>/dev/null | head -1)

if [ -z "$PROJECT_ROOT" ]; then
  PROJECT_ROOT="/PROJETOS/MPCN-SYSTEMS/mpcn-pdf-tools"
fi

if [ ! -d "$PROJECT_ROOT" ]; then
  echo "❌ Diretório do projeto não encontrado!"
  echo "   Tentei: $PROJECT_ROOT"
  exit 1
fi

echo "📁 Projeto encontrado: $PROJECT_ROOT"
cd "$PROJECT_ROOT" || exit 1
echo ""

# 1. MATAR TODOS OS PROCESSOS
echo "1️⃣ Matando TODOS os processos dotnet do projeto..."
pkill -9 -f "dotnet.*PdfProcessor" 2>/dev/null
pkill -9 -f "PdfProcessor.API" 2>/dev/null
pkill -9 -f "PdfProcessor.Web" 2>/dev/null
sleep 3
echo "   ✅ Processos encerrados"
echo ""

# 2. LIMPAR TODOS OS BIN E OBJ
echo "2️⃣ Removendo TODOS os diretórios bin/ e obj/..."
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null
echo "   ✅ Diretórios removidos"
echo ""

# 3. LIMPAR CACHE DO NUGET
echo "3️⃣ Limpando cache do NuGet..."
dotnet nuget locals all --clear
echo "   ✅ Cache limpo"
echo ""

# 4. RESTAURAR PACOTES
echo "4️⃣ Restaurando pacotes..."
dotnet restore --force
if [ $? -ne 0 ]; then
  echo "   ❌ Erro ao restaurar pacotes!"
  exit 1
fi
echo "   ✅ Pacotes restaurados"
echo ""

# 5. CLEAN
echo "5️⃣ Executando dotnet clean..."
dotnet clean
echo "   ✅ Clean concluído"
echo ""

# 6. BUILD FORÇADO (SEM CACHE)
echo "6️⃣ Compilando projeto (forçado, sem incremental)..."
dotnet build --no-incremental --force --no-cache
if [ $? -ne 0 ]; then
  echo "   ❌ Erro na compilação!"
  exit 1
fi
echo "   ✅ Compilação bem-sucedida!"
echo ""

# 7. VERIFICAR SE O ARQUIVO FOI COMPILADO
echo "7️⃣ Verificando DLL compilada..."
DLL_PATH=$(find . -name "PdfProcessor.Infrastructure.dll" -path "*/bin/*" | head -1)
if [ -n "$DLL_PATH" ]; then
  DLL_DATE=$(stat -c %y "$DLL_PATH" 2>/dev/null || stat -f "%Sm" "$DLL_PATH" 2>/dev/null)
  echo "   ✅ DLL encontrada: $DLL_PATH"
  echo "   📅 Data: $DLL_DATE"
else
  echo "   ⚠️  DLL não encontrada!"
fi
echo ""

echo "================================================"
echo "✅ REBUILD COMPLETO CONCLUÍDO!"
echo "================================================"
echo ""
echo "📝 PRÓXIMOS PASSOS:"
echo ""
echo "   1. Abrir NOVO terminal e executar:"
echo "      cd $PROJECT_ROOT/PdfProcessor.API"
echo "      dotnet run"
echo ""
echo "   2. Abrir OUTRO NOVO terminal e executar:"
echo "      cd $PROJECT_ROOT/PdfProcessor.Web"
echo "      dotnet run"
echo ""
echo "   3. Testar no navegador"
echo ""
echo "⚠️  IMPORTANTE: Use NOVOS terminais para garantir"
echo "   que não há variáveis de ambiente antigas!"
echo ""
