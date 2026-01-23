#!/bin/bash

# =====================================================
# CONFIGURAÇÕES
# =====================================================

basePath="$(cd "$(dirname "$0")" && pwd)"
outputFile="$basePath/Listagem-detalhada.txt"

# Pastas ignoradas
pastasIgnoradas=(
    "bin"
    "obj"
    "venv"
    "node_modules"
    "vendor"
    ".git"
    ".vs"
    ".idea"
    "dist"
)

# Extensões ignoradas
extensoesIgnoradas=(
    ".lock"
    ".log"
    ".exe"
    ".dll"
    ".pdb"
)

# Arquivos ignorados por nome
arquivosIgnorados=(
    "composer.lock"
    "package-lock.json"
    "yarn.lock"
    "README.md"
    "gerar-listagem.sh"
    "Listagem-detalhada.txt"
)

# =====================================================
# FUNÇÃO: GERAR ÁRVORE (ASCII)
# =====================================================

write_tree() {
    local path="$1"
    local indent="${2:-}"
    
    # Lista de itens no diretório
    local items=()
    
    # Obter itens, ignorando pastas especificadas
    for item in "$path"/* "$path"/.[!.]*; do
        # Ignorar padrão vazio
        [ -e "$item" ] || continue
        
        local nome=$(basename "$item")
        
        # Ignorar pastas na lista de ignoradas
        if [ -d "$item" ]; then
            if [[ " ${pastasIgnoradas[@]} " =~ " ${nome} " ]]; then
                continue
            fi
        fi
        
        items+=("$item")
    done
    
    local count=${#items[@]}
    local index=0
    
    for item in "${items[@]}"; do
        index=$((index + 1))
        local nome=$(basename "$item")
        local isLast=$([ $index -eq $count ] && echo "true" || echo "false")
        
        if [ "$isLast" = "true" ]; then
            prefix="\`-- "
        else
            prefix="|-- "
        fi
        
        echo "${indent}${prefix}${nome}" >> "$outputFile"
        
        if [ -d "$item" ]; then
            if [ "$isLast" = "true" ]; then
                local newIndent="${indent}    "
            else
                local newIndent="${indent}|   "
            fi
            
            write_tree "$item" "$newIndent"
        fi
    done
}

# =====================================================
# FUNÇÃO: VERIFICAR SE DEVE IGNORAR ARQUIVO
# =====================================================

deve_ignorar_arquivo() {
    local file="$1"
    local nome=$(basename "$file")
    local extensao="${nome##*.}"
    
    # Verificar extensões ignoradas
    for ext in "${extensoesIgnoradas[@]}"; do
        if [[ "$nome" == *"$ext" ]]; then
            return 0
        fi
    done
    
    # Verificar nomes ignorados
    for arq in "${arquivosIgnorados[@]}"; do
        if [ "$nome" = "$arq" ]; then
            return 0
        fi
    done
    
    # Verificar se está em pasta ignorada
    local caminho=$(dirname "$file")
    for pasta in "${pastasIgnoradas[@]}"; do
        if [[ "$caminho" == *"/$pasta"* ]] || [[ "$caminha" == *"/$pasta/"* ]]; then
            return 0
        fi
    done
    
    # Ignorar o arquivo de saída
    if [ "$file" = "$outputFile" ]; then
        return 0
    fi
    
    return 1
}

# =====================================================
# INÍCIO
# =====================================================

# Limpar arquivo de saída se existir
if [ -f "$outputFile" ]; then
    rm -f "$outputFile"
fi

# Criar arquivo de saída
touch "$outputFile"

# -----------------------------
# ESTRUTURA
# -----------------------------
echo "Estrutura de arquivos:" >> "$outputFile"
echo "" >> "$outputFile"
write_tree "$basePath"

# -----------------------------
# CONTEÚDO DOS ARQUIVOS
# -----------------------------
echo "" >> "$outputFile"
echo "==============================" >> "$outputFile"
echo "CONTEÚDO DOS ARQUIVOS" >> "$outputFile"
echo "==============================" >> "$outputFile"
echo "" >> "$outputFile"

# Encontrar todos os arquivos
find "$basePath" -type f | while read -r file; do
    # Converter caminho para relativo
    relPath="${file#$basePath/}"
    
    # Verificar se deve ignorar o arquivo
    if deve_ignorar_arquivo "$file"; then
        continue
    fi
    
    echo "----------------------------------------" >> "$outputFile"
    echo "Conteúdo do arquivo: $relPath" >> "$outputFile"
    echo "----------------------------------------" >> "$outputFile"
    echo "" >> "$outputFile"
    
    # Tentar ler o conteúdo do arquivo
    if [ -r "$file" ]; then
        # Verificar se é um arquivo de texto
        if file "$file" | grep -q "text"; then
            cat "$file" >> "$outputFile" 2>/dev/null || echo "[ERRO AO LER O ARQUIVO]" >> "$outputFile"
        else
            echo "[ARQUIVO BINÁRIO - CONTEÚDO OMITIDO]" >> "$outputFile"
        fi
    else
        echo "[ERRO AO LER O ARQUIVO - SEM PERMISSÃO]" >> "$outputFile"
    fi
    
    echo "" >> "$outputFile"
    echo "" >> "$outputFile"
done

echo "Arquivo gerado com sucesso:"
echo "$outputFile"
