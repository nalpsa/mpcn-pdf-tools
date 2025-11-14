#!/bin/bash

echo "🌐 CONFIGURANDO ACESSO PELA REDE (IP: 10.0.0.50)"
echo "================================================"
echo ""

# Definir IP
IP_ADDRESS="10.0.0.50"

# 1. Criar launchSettings.json para API
echo "1️⃣ Configurando API (porta 5239)..."

mkdir -p PdfProcessor.API/Properties

cat > PdfProcessor.API/Properties/launchSettings.json << EOF
{
  "\$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://0.0.0.0:5239",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF

echo "   ✅ launchSettings.json da API criado"

# 2. Criar launchSettings.json para Web
echo ""
echo "2️⃣ Configurando Web (porta 5087)..."

mkdir -p PdfProcessor.Web/Properties

cat > PdfProcessor.Web/Properties/launchSettings.json << EOF
{
  "\$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://0.0.0.0:5087",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF

echo "   ✅ launchSettings.json do Web criado"

# 3. Configurar Firewall
echo ""
echo "3️⃣ Configurando Firewall..."

if command -v ufw &> /dev/null; then
    echo "   🔥 UFW detectado, configurando portas..."
    sudo ufw allow 5239/tcp comment "PDF Processor API"
    sudo ufw allow 5087/tcp comment "PDF Processor Web"
    echo "   ✅ Firewall configurado"
else
    echo "   ⚠️ UFW não instalado, pular configuração de firewall"
fi

# 4. Verificar IP atual
echo ""
echo "4️⃣ Verificando IP da máquina..."

CURRENT_IP=$(hostname -I | awk '{print $1}')
echo "   �� IP detectado: $CURRENT_IP"

if [ "$CURRENT_IP" != "$IP_ADDRESS" ]; then
    echo "   ⚠️ ATENÇÃO: IP detectado ($CURRENT_IP) diferente do configurado ($IP_ADDRESS)"
    echo "   💡 Você pode precisar atualizar o IP no script"
else
    echo "   ✅ IP correto!"
fi

# 5. Resumo
echo ""
echo "================================================"
echo "✅ CONFIGURAÇÃO CONCLUÍDA!"
echo "================================================"
echo ""
echo "📝 PRÓXIMOS PASSOS:"
echo ""
echo "1. Atualizar CORS no Program.cs da API:"
echo "   Adicionar: http://$IP_ADDRESS:5087"
echo "   Adicionar: http://$IP_ADDRESS:5239"
echo ""
echo "2. Atualizar URL da API nos arquivos .razor:"
echo "   RotatePdf.razor"
echo "   CompressPdf.razor"
echo "   Usar: const API_BASE_URL = 'http://$IP_ADDRESS:5239';"
echo ""
echo "3. Rodar aplicação:"
echo "   Terminal 1: cd PdfProcessor.API && dotnet run"
echo "   Terminal 2: cd PdfProcessor.Web && dotnet run"
echo ""
echo "4. Acessar de outra máquina:"
echo "   http://$IP_ADDRESS:5087"
echo ""
echo "================================================"
