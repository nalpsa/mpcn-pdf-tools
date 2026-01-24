# =====================================================
# CONFIGURACOES
# =====================================================

$basePath = $PSScriptRoot
$outputFile = Join-Path $basePath "Listagem-detalhada.txt"

# Pastas ignoradas
$pastasIgnoradas = @(
    "bin",
    "obj",
    "venv",
    "node_modules",
    "vendor",
    ".git",
    ".vs",
    ".idea",
    "PdfProcessor.UnitTests",
    ".git",
    "bootstrap"
)

# Extensões ignoradas
$extensoesIgnoradas = @(
    ".lock",
    ".log",
    ".exe",
    ".dll",
    ".pdb"
)

# Arquivos ignorados por nome
$arquivosIgnorados = @(
    "composer.lock",
    "package-lock.json",
    "yarn.lock",
    "gerar-listagem.ps1",
    "gerar-listagem.sh",
    "README.md",
    "Listagem-detalhada.txt"
)

# =====================================================
# FUNCAO: GERAR ARVORE (ASCII)
# =====================================================

function Write-Tree {
    param (
        [string]$Path,
        [string]$Indent = ""
    )

    $items = Get-ChildItem $Path -Force | Where-Object {
        if ($_.PSIsContainer) {
            -not ($pastasIgnoradas -contains $_.Name)
        } else {
            $true
        }
    }

    $count = $items.Count
    $index = 0

    foreach ($item in $items) {
        $index++
        $isLast = ($index -eq $count)

        $prefix = if ($isLast) { "`-- " } else { "|-- " }
        $writer.WriteLine("$Indent$prefix$($item.Name)")

        if ($item.PSIsContainer) {
            $newIndent = if ($isLast) {
                "$Indent    "
            } else {
                "$Indent|   "
            }

            Write-Tree -Path $item.FullName -Indent $newIndent
        }
    }
}

# =====================================================
# INICIO
# =====================================================

if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}

$writer = New-Object System.IO.StreamWriter(
    $outputFile,
    $false,
    [System.Text.Encoding]::UTF8
)

try {

    # -----------------------------
    # ESTRUTURA
    # -----------------------------
    $writer.WriteLine("Estrutura de arquivos:`r`n")
    Write-Tree -Path $basePath

    # -----------------------------
    # CONTEUDO DOS ARQUIVOS
    # -----------------------------
    $writer.WriteLine("`r`n==============================")
    $writer.WriteLine("CONTEÚDO DOS ARQUIVOS")
    $writer.WriteLine("==============================`r`n")

    Get-ChildItem $basePath -Recurse -File | Where-Object {

        $caminho = $_.FullName

        # Ignora pastas
        if ($pastasIgnoradas | Where-Object {
            $caminho -match "\\$_\\"
        }) {
            return $false
        }

        # Ignora o próprio TXT
        if ($_.FullName -eq $outputFile) {
            return $false
        }

        # Ignora extensões
        if ($extensoesIgnoradas -contains $_.Extension) {
            return $false
        }

        # Ignora nomes específicos
        if ($arquivosIgnorados -contains $_.Name) {
            return $false
        }

        return $true

    } | ForEach-Object {

        $writer.WriteLine("----------------------------------------")
        $writer.WriteLine("Conteudo do arquivo: $($_.FullName)")
        $writer.WriteLine("----------------------------------------`r`n")

        try {
            $conteudo = Get-Content $_.FullName -Raw -ErrorAction Stop
            $writer.WriteLine($conteudo)
        }
        catch {
            $writer.WriteLine("[ERRO AO LER O ARQUIVO]")
        }

        $writer.WriteLine("`r`n")
    }

}
finally {
    $writer.Close()
}

Write-Host "Arquivo gerado com sucesso:"
Write-Host $outputFile
