#!/bin/bash

echo "🔍 DIAGNÓSTICO - Edit PDF Service"
echo "=================================="
echo ""

# 1. Verificar se o arquivo existe
echo "1️⃣ Verificando localização do PdfEditService.cs..."
EDIT_SERVICE=$(find /PROJETOS/MPCN-SYSTEMS/mpcn-pdf-tools -name "PdfEditService.cs" 2>/dev/null | head -1)

if [ -z "$EDIT_SERVICE" ]; then
  echo "❌ Arquivo PdfEditService.cs NÃO ENCONTRADO!"
  echo ""
  echo "📁 Estrutura do projeto:"
  find /PROJETOS/MPCN-SYSTEMS/mpcn-pdf-tools -type d -name "PdfServices" 2>/dev/null
  exit 1
fi

echo "✅ Arquivo encontrado: $EDIT_SERVICE"
echo ""

# 2. Verificar se tem a correção aplicada
echo "2️⃣ Verificando se correção foi aplicada..."
if grep -q "CORREÇÃO CRÍTICA" "$EDIT_SERVICE"; then
  echo "✅ Correção aplicada (comentário encontrado)"
else
  echo "❌ Correção NÃO aplicada"
fi
echo ""

# 3. Verificar a ordem do Close vs ToArray
echo "3️⃣ Verificando ordem Close() vs ToArray()..."
CLOSE_LINE=$(grep -n "outputPdfDoc.Close()" "$EDIT_SERVICE" | cut -d: -f1)
ARRAY_LINE=$(grep -n "outputStream.ToArray()" "$EDIT_SERVICE" | cut -d: -f1)

if [ -n "$CLOSE_LINE" ] && [ -n "$ARRAY_LINE" ]; then
  if [ "$CLOSE_LINE" -lt "$ARRAY_LINE" ]; then
    echo "✅ CORRETO: Close() na linha $CLOSE_LINE vem ANTES de ToArray() na linha $ARRAY_LINE"
  else
    echo "❌ ERRADO: ToArray() vem antes de Close()"
    echo "   Close() está na linha $CLOSE_LINE"
    echo "   ToArray() está na linha $ARRAY_LINE"
  fi
else
  echo "⚠️  Não foi possível determinar as linhas"
fi
echo ""

# 4. Mostrar trecho relevante
echo "4️⃣ Mostrando trecho do código (linhas 195-205):"
echo "----------------------------------------"
sed -n '195,205p' "$EDIT_SERVICE"
echo "----------------------------------------"
echo ""

# 5. Verificar se Program.cs registra o serviço
echo "5️⃣ Verificando registro no Program.cs..."
PROGRAM_CS=$(find /PROJETOS/MPCN-SYSTEMS/mpcn-pdf-tools -name "Program.cs" -path "*/PdfProcessor.API/*" 2>/dev/null | head -1)

if [ -n "$PROGRAM_CS" ]; then
  if grep -q "IPdfEditService" "$PROGRAM_CS"; then
    echo "✅ IPdfEditService está registrado no Program.cs"
  else
    echo "❌ IPdfEditService NÃO está registrado no Program.cs"
    echo "   Adicione esta linha:"
    echo "   builder.Services.AddScoped<IPdfEditService, PdfEditService>();"
  fi
else
  echo "⚠️  Program.cs não encontrado"
fi
echo ""

echo "=================================="
echo "✅ Diagnóstico concluído!"
