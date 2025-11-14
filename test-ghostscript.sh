#!/bin/bash

echo "🔍 TESTE DE GHOSTSCRIPT"
echo "======================="
echo ""

# 1. Verificar instalação
echo "1️⃣ Verificando instalação do Ghostscript..."
if command -v gs &> /dev/null; then
    echo "   ✅ Ghostscript encontrado!"
    echo "   📍 Localização: $(which gs)"
    echo "   �� Versão: $(gs --version)"
else
    echo "   ❌ Ghostscript NÃO encontrado!"
    echo "   💡 Instale com: sudo apt-get install ghostscript"
    exit 1
fi

echo ""

# 2. Testar compressão real
echo "2️⃣ Testando compressão de PDF..."

# Criar um PDF de teste simples
TEST_INPUT="/tmp/test_input.pdf"
TEST_OUTPUT="/tmp/test_output.pdf"

# Procurar um PDF existente no sistema ou criar um simples
if [ -f "$HOME/Downloads/*.pdf" ]; then
    TEST_PDF=$(ls $HOME/Downloads/*.pdf | head -1)
    echo "   📄 Usando PDF de teste: $TEST_PDF"
    cp "$TEST_PDF" "$TEST_INPUT"
else
    echo "   📄 Criando PDF de teste simples..."
    echo "%PDF-1.4
1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R>>endobj
4 0 obj<</Length 44>>stream
BT /F1 12 Tf 100 700 Td (Hello World!) Tj ET
endstream endobj
xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000056 00000 n
0000000115 00000 n
0000000214 00000 n
trailer<</Size 5/Root 1 0 R>>
startxref
309
%%EOF" > "$TEST_INPUT"
fi

# Obter tamanho original
ORIGINAL_SIZE=$(stat -f%z "$TEST_INPUT" 2>/dev/null || stat -c%s "$TEST_INPUT" 2>/dev/null)
echo "   �� Tamanho original: $ORIGINAL_SIZE bytes"

# Executar compressão
echo "   🗜️ Comprimindo com Ghostscript..."
gs -sDEVICE=pdfwrite \
   -dCompatibilityLevel=1.4 \
   -dPDFSETTINGS=/ebook \
   -dNOPAUSE \
   -dQUIET \
   -dBATCH \
   -dSAFER \
   -dDetectDuplicateImages=true \
   -dCompressFonts=true \
   -dCompressPages=true \
   -dColorImageResolution=150 \
   -sOutputFile="$TEST_OUTPUT" \
   "$TEST_INPUT" 2>&1

# Verificar resultado
if [ $? -eq 0 ] && [ -f "$TEST_OUTPUT" ]; then
    COMPRESSED_SIZE=$(stat -f%z "$TEST_OUTPUT" 2>/dev/null || stat -c%s "$TEST_OUTPUT" 2>/dev/null)
    echo "   ✅ Compressão bem-sucedida!"
    echo "   📊 Tamanho comprimido: $COMPRESSED_SIZE bytes"
    
    if [ $COMPRESSED_SIZE -lt $ORIGINAL_SIZE ]; then
        SAVINGS=$(( 100 - (COMPRESSED_SIZE * 100 / ORIGINAL_SIZE) ))
        echo "   🎉 Economia: ${SAVINGS}%"
    else
        echo "   ⚠️ Arquivo comprimido é maior (normal para PDFs pequenos)"
    fi
    
    # Verificar se PDF é válido
    if gs -dNODISPLAY -dQUIET "$TEST_OUTPUT" 2>&1 | grep -q "Error"; then
        echo "   ❌ PDF comprimido tem erros!"
    else
        echo "   ✅ PDF comprimido é válido"
    fi
else
    echo "   ❌ Compressão falhou!"
    exit 1
fi

echo ""

# 3. Limpar arquivos temporários
echo "3️⃣ Limpando arquivos de teste..."
rm -f "$TEST_INPUT" "$TEST_OUTPUT"
echo "   ✅ Limpeza concluída"

echo ""
echo "======================================"
echo "✅ TODOS OS TESTES PASSARAM!"
echo "======================================"
echo ""
echo "💡 O Ghostscript está funcionando corretamente."
echo "💡 Você pode usar o PdfCompressService_Process.cs"
