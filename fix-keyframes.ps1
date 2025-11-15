# Diagnóstico do arquivo CompressPdf.razor
Write-Host "🔍 DIAGNÓSTICO - CompressPdf.razor" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$filePath = "PdfProcessor.Web\Components\Pages\CompressPdf.razor"

if (-Not (Test-Path $filePath)) {
    Write-Host "❌ Arquivo não encontrado!" -ForegroundColor Red
    exit 1
}

# Ler linhas
$lines = Get-Content $filePath

# Mostrar linhas ao redor da 387
Write-Host "📄 Linhas 380-395:" -ForegroundColor Yellow
Write-Host ""

for ($i = 379; $i -lt 395 -and $i -lt $lines.Count; $i++) {
    $lineNum = $i + 1
    $line = $lines[$i]
    
    if ($lineNum -eq 387 -or $lineNum -eq 388) {
        Write-Host "$lineNum : $line" -ForegroundColor Red
    } else {
        Write-Host "$lineNum : $line"
    }
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan

# Procurar por todos os @ problemáticos
Write-Host ""
Write-Host "🔍 Procurando por @ problemáticos..." -ForegroundColor Yellow
Write-Host ""

$problemLines = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    # Procurar por @ seguido de espaço ou letra minúscula (exceto em strings)
    if ($line -match '@\s' -or ($line -match '@[a-z]' -and $line -notmatch '@@')) {
        if ($line -notmatch '@page' -and $line -notmatch '@inject' -and $line -notmatch '@code' -and $line -notmatch '@if' -and $line -notmatch '@bind' -and $line -notmatch '@onclick' -and $line -notmatch '@onchange') {
            $lineNum = $i + 1
            $problemLines += "$lineNum : $line"
        }
    }
}

if ($problemLines.Count -gt 0) {
    Write-Host "⚠️  Linhas com possível problema:" -ForegroundColor Yellow
    foreach ($pLine in $problemLines) {
        Write-Host $pLine -ForegroundColor Red
    }
} else {
    Write-Host "✅ Nenhum @ problemático encontrado" -ForegroundColor Green
}

Write-Host ""