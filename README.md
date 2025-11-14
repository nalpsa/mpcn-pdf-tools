# 📄 PDF Processor - Sistema de Extração e Manipulação de PDFs

## 🎯 Visão Geral

Sistema completo para extração de dados de PDFs bancários e ferramentas de manipulação de documentos PDF, desenvolvido em **.NET 8** com **C#**, seguindo princípios **SOLID** e **Clean Architecture**.

### **Migração:** Python/Flask → .NET 8/C# + Blazor Server

---

## 🏗️ Arquitetura do Sistema

### **Estrutura de Projetos (Clean Architecture)**

```
PdfProcessor/
│
├── src/
│   ├── PdfProcessor.API/                    # ASP.NET Core Web API
│   │   ├── Controllers/                     # Endpoints REST
│   │   ├── Middlewares/                     # Tratamento de erros, logging
│   │   ├── Filters/                         # Validações, autenticação
│   │   └── Program.cs                       # Configuração da API
│   │
│   ├── PdfProcessor.Web/                    # Blazor Server (Frontend)
│   │   ├── Pages/                           # Páginas Blazor
│   │   ├── Components/                      # Componentes reutilizáveis
│   │   ├── Services/                        # Services para consumir API
│   │   └── wwwroot/                         # Assets estáticos
│   │
│   ├── PdfProcessor.Core/                   # Domain Layer
│   │   ├── Entities/                        # Entidades de domínio
│   │   ├── Enums/                           # Enumeradores
│   │   ├── Interfaces/                      # Contratos (abstrações)
│   │   ├── ValueObjects/                    # Value Objects
│   │   └── Exceptions/                      # Exceções customizadas
│   │
│   ├── PdfProcessor.Application/            # Application Layer
│   │   ├── UseCases/                        # Casos de uso
│   │   │   ├── Banks/                       # Extração bancária
│   │   │   │   ├── Itau/
│   │   │   │   │   ├── ExtractCashTransactions/
│   │   │   │   │   └── ExtractMovimentacao/
│   │   │   │   └── ...
│   │   │   └── Tools/                       # Ferramentas PDF
│   │   │       ├── RotatePdf/
│   │   │       ├── MergePdf/
│   │   │       └── CompressPdf/
│   │   ├── Services/                        # Services de aplicação
│   │   ├── DTOs/                            # Data Transfer Objects
│   │   └── Mappings/                        # AutoMapper profiles
│   │
│   ├── PdfProcessor.Infrastructure/         # Infrastructure Layer
│   │   ├── Parsers/                         # Parsers específicos de banco
│   │   │   ├── ItauParser.cs
│   │   │   ├── ItauMovimentacaoParser.cs
│   │   │   └── ...
│   │   ├── PdfServices/                     # Manipulação de PDF
│   │   │   ├── PdfRotateService.cs
│   │   │   ├── PdfMergeService.cs
│   │   │   └── PdfCompressService.cs
│   │   ├── ExcelServices/                   # Geração de Excel
│   │   │   └── ExcelGeneratorService.cs
│   │   ├── FileStorage/                     # Armazenamento de arquivos
│   │   │   └── LocalFileStorage.cs
│   │   └── Repositories/                    # (Futuro: se precisar BD)
│   │
│   └── PdfProcessor.Shared/                 # Shared Kernel
│       ├── Constants/                       # Constantes globais
│       ├── Extensions/                      # Extension methods
│       └── Helpers/                         # Utilitários
│
├── tests/
│   ├── PdfProcessor.UnitTests/              # Testes unitários
│   ├── PdfProcessor.IntegrationTests/       # Testes de integração
│   └── PdfProcessor.E2ETests/               # Testes end-to-end
│
├── docs/                                     # Documentação
├── docker/                                   # Dockerfiles e compose
├── .gitignore
├── README.md
└── PdfProcessor.sln                         # Solution file

```

---

## 🧩 Princípios SOLID Aplicados

### **1. Single Responsibility Principle (SRP)**
- Cada parser é responsável por **apenas um banco**
- Services separados para cada operação de PDF
- DTOs específicos para cada contexto

### **2. Open/Closed Principle (OCP)**
- Interfaces para parsers (`IBankParser<T>`)
- Factory Pattern para criação de parsers
- Strategy Pattern para diferentes algoritmos de extração

### **3. Liskov Substitution Principle (LSP)**
- Todos os parsers implementam `IBankParser<T>`
- Podem ser substituídos sem quebrar o sistema

### **4. Interface Segregation Principle (ISP)**
- Interfaces específicas: `IPdfRotateService`, `IPdfMergeService`, `IPdfCompressService`
- Não forçar implementação de métodos desnecessários

### **5. Dependency Inversion Principle (DIP)**
- Dependências sempre em interfaces, nunca em implementações concretas
- Injeção de dependência em todos os layers

---

## 📦 Tecnologias e Pacotes NuGet

### **API (PdfProcessor.API)**
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
```

### **Web (PdfProcessor.Web)**
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="8.0.*" />
```

### **Application**
```xml
<PackageReference Include="AutoMapper" Version="13.0.*" />
<PackageReference Include="FluentValidation" Version="11.9.*" />
<PackageReference Include="MediatR" Version="12.2.*" />
```

### **Infrastructure**
```xml
<!-- PDF Manipulation -->
<PackageReference Include="PdfSharp" Version="6.0.*" />
<PackageReference Include="itext7" Version="8.0.*" />
<PackageReference Include="UglyToad.PdfPig" Version="0.1.*" />

<!-- Excel Generation -->
<PackageReference Include="ClosedXML" Version="0.102.*" />

<!-- Image Processing (para compressão) -->
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.*" />
```

---

## 🗂️ Entidades e DTOs Principais

### **Core Entities**

```csharp
// PdfProcessor.Core/Entities/Transaction.cs
public class Transaction
{
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public string AccountNumber { get; set; }
}

// PdfProcessor.Core/Entities/BankAccount.cs
public class BankAccount
{
    public string AccountNumber { get; set; }
    public string Currency { get; set; }
    public List<Transaction> Transactions { get; set; }
}

// PdfProcessor.Core/Entities/ProcessingResult.cs
public class ProcessingResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public int ProcessedFiles { get; set; }
}
```

### **Application DTOs**

```csharp
// PdfProcessor.Application/DTOs/UploadRequestDto.cs
public class UploadRequestDto
{
    public List<IFormFile> Files { get; set; }
    public string BankType { get; set; }
}

// PdfProcessor.Application/DTOs/RotatePdfRequestDto.cs
public class RotatePdfRequestDto
{
    public List<IFormFile> Files { get; set; }
    public List<int> RotationAngles { get; set; } // 90, 180, 270
}

// PdfProcessor.Application/DTOs/MergePdfRequestDto.cs
public class MergePdfRequestDto
{
    public List<IFormFile> Files { get; set; }
    public List<int> Order { get; set; }
    public List<string> PageRanges { get; set; } // "1-5", "all", etc.
}
```

---

## 🔌 Interfaces Principais

### **IBankParser<T>**
```csharp
// PdfProcessor.Core/Interfaces/IBankParser.cs
public interface IBankParser<T>
{
    Task<ProcessingResult<T>> ParseAsync(Stream pdfStream, string fileName);
    Task<ProcessingResult<T>> ParseBatchAsync(List<Stream> pdfStreams, List<string> fileNames);
    string BankName { get; }
    string[] SupportedFormats { get; }
}
```

### **IPdfService**
```csharp
// PdfProcessor.Core/Interfaces/IPdfRotateService.cs
public interface IPdfRotateService
{
    Task<byte[]> RotatePdfAsync(Stream pdfStream, int rotationAngle);
    Task<Dictionary<string, byte[]>> RotateBatchAsync(
        Dictionary<string, Stream> pdfs, 
        Dictionary<string, int> rotations
    );
}

// PdfProcessor.Core/Interfaces/IPdfMergeService.cs
public interface IPdfMergeService
{
    Task<byte[]> MergePdfsAsync(List<Stream> pdfStreams, List<string> pageRanges = null);
}

// PdfProcessor.Core/Interfaces/IPdfCompressService.cs
public interface IPdfCompressService
{
    Task<byte[]> CompressPdfAsync(Stream pdfStream, CompressionLevel level);
}
```

### **IExcelGeneratorService**
```csharp
// PdfProcessor.Core/Interfaces/IExcelGeneratorService.cs
public interface IExcelGeneratorService
{
    Task<byte[]> GenerateExcelAsync<T>(
        Dictionary<string, List<T>> dataBySheet,
        string templateName = null
    );
}
```

---

## 🚀 Roadmap de Desenvolvimento

### **FASE 1: Setup e Infraestrutura (Semana 1)**
- [ ] Criar estrutura de projetos (Solution + Projects)
- [ ] Configurar Dependency Injection
- [ ] Setup Docker + Docker Compose
- [ ] Implementar middleware de logging (Serilog)
- [ ] Configurar tratamento global de exceções
- [ ] Setup de testes unitários

### **FASE 2: Core Domain (Semana 1-2)**
- [ ] Definir entidades principais
- [ ] Criar interfaces (contratos)
- [ ] Implementar Value Objects
- [ ] Criar exceções customizadas
- [ ] Documentar domínio

### **FASE 3: Infrastructure - Ferramentas PDF (Semana 2)**
- [ ] **Rotate PDF:**
  - [ ] Implementar `PdfRotateService`
  - [ ] Suporte a múltiplos arquivos
  - [ ] Geração de miniaturas (preview)
- [ ] **Merge PDF:**
  - [ ] Implementar `PdfMergeService`
  - [ ] Suporte a seleção de páginas específicas
- [ ] **Compress PDF:**
  - [ ] Implementar `PdfCompressService`
  - [ ] Níveis de compressão (low, medium, high)
  - [ ] Otimização de imagens

### **FASE 4: Parsers Bancários - Itaú (Semana 3)**
- [ ] **Itaú Cash Transactions:**
  - [ ] Implementar `ItauCashTransactionsParser`
  - [ ] Detectar múltiplas contas
  - [ ] Detectar múltiplas páginas
  - [ ] Extração de dados (Date, Description, Debit, Credit, Balance)
- [ ] **Itaú Movimentação:**
  - [ ] Implementar `ItauMovimentacaoParser`
  - [ ] Correção automática de datas em branco
  - [ ] Separação por conta

### **FASE 5: Application Layer (Semana 3-4)**
- [ ] Implementar Use Cases (CQRS com MediatR)
- [ ] Criar DTOs e Mappings (AutoMapper)
- [ ] Validações com FluentValidation
- [ ] Services de aplicação

### **FASE 6: API REST (Semana 4)**
- [ ] Controllers para bancos
- [ ] Controllers para ferramentas PDF
- [ ] Swagger/OpenAPI documentation
- [ ] Upload de múltiplos arquivos
- [ ] Download de resultados (Excel, PDF, ZIP)

### **FASE 7: Frontend Blazor (Semana 5)**
- [ ] Dashboard principal
- [ ] Página Itaú Cash Transactions
- [ ] Página Itaú Movimentação
- [ ] Páginas de ferramentas PDF
- [ ] Componentes de upload com preview
- [ ] Feedback visual (loading, progress)

### **FASE 8: Testes (Semana 5-6)**
- [ ] Testes unitários (parsers)
- [ ] Testes unitários (services)
- [ ] Testes de integração (API)
- [ ] Testes E2E (Blazor)

### **FASE 9: Docker e Deploy (Semana 6)**
- [ ] Dockerfile para API
- [ ] Dockerfile para Web
- [ ] Docker Compose
- [ ] CI/CD básico
- [ ] Documentação de deploy

### **FASE 10: Melhorias Futuras**
- [ ] Adicionar outros bancos (Morgan Stanley, Julius Baer, etc.)
- [ ] Sistema de filas (processamento assíncrono)
- [ ] Cache de resultados
- [ ] Monitoramento (Application Insights)
- [ ] Autenticação/Autorização

---

## 🐋 Docker

### **docker-compose.yml**
```yaml
version: '3.8'

services:
  pdf-processor-api:
    build:
      context: .
      dockerfile: docker/api.Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./uploads:/app/uploads
      - ./outputs:/app/outputs

  pdf-processor-web:
    build:
      context: .
      dockerfile: docker/web.Dockerfile
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ApiBaseUrl=http://pdf-processor-api:80
    depends_on:
      - pdf-processor-api
```

---

## 🧪 Testes

### **Exemplo de Teste Unitário (Parser)**
```csharp
// PdfProcessor.UnitTests/Parsers/ItauCashTransactionsParserTests.cs
public class ItauCashTransactionsParserTests
{
    private readonly ItauCashTransactionsParser _parser;

    public ItauCashTransactionsParserTests()
    {
        _parser = new ItauCashTransactionsParser();
    }

    [Fact]
    public async Task ParseAsync_ValidPdf_ShouldReturnTransactions()
    {
        // Arrange
        var pdfStream = File.OpenRead("TestFiles/itau_valid.pdf");

        // Act
        var result = await _parser.ParseAsync(pdfStream, "itau_valid.pdf");

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Data);
        Assert.Contains(result.Data, account => account.Currency == "USD");
    }

    [Fact]
    public async Task ParseAsync_InvalidPdf_ShouldReturnError()
    {
        // Arrange
        var pdfStream = File.OpenRead("TestFiles/invalid.pdf");

        // Act
        var result = await _parser.ParseAsync(pdfStream, "invalid.pdf");

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }
}
```

---

## 📝 Convenções de Código

### **Nomenclatura**
- **Classes:** PascalCase (`ItauCashTransactionsParser`)
- **Métodos:** PascalCase (`ParseAsync`)
- **Variáveis:** camelCase (`pdfStream`)
- **Interfaces:** Prefixo `I` (`IBankParser`)
- **DTOs:** Sufixo `Dto` (`UploadRequestDto`)

### **Async/Await**
- Todos os métodos I/O devem ser assíncronos
- Sufixo `Async` em métodos assíncronos

### **Logging**
```csharp
_logger.LogInformation("Processing file {FileName}", fileName);
_logger.LogWarning("No transactions found in {FileName}", fileName);
_logger.LogError(ex, "Error processing {FileName}", fileName);
```

---

## 🔐 Segurança

- [ ] Validação de tipos de arquivo (apenas PDF)
- [ ] Limite de tamanho de arquivo (16MB)
- [ ] Sanitização de nomes de arquivo
- [ ] Timeout para processamento
- [ ] Rate limiting na API

---

## 📚 Documentação Adicional

- [Guia de Contribuição](docs/CONTRIBUTING.md)
- [Arquitetura Detalhada](docs/ARCHITECTURE.md)
- [Padrões de Código](docs/CODING_STANDARDS.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

---

## 👥 Equipe

- **Desenvolvimento:** [Seu Nome]
- **Arquitetura:** Claude AI Assistant

---

## 📄 Licença

[Definir licença - MIT, Apache, etc.]

---

## 🎯 Próximos Passos

1. ✅ Ler e validar este README
2. ✅ Criar estrutura de projetos
3. ✅ Implementar primeira funcionalidade (Rotate PDF)
4. ✅ Testes unitários
5. ✅ Integração contínua

---

**Última atualização:** 2025-11-13
