using PdfProcessor.Core.Interfaces;
using PdfProcessor.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PdfProcessor.Infrastructure.PdfServices;

/// <summary>
/// Serviço para compressão de PDFs usando Ghostscript via Process
/// </summary>
public class PdfCompressService : IPdfCompressService
{
  private readonly ILogger<PdfCompressService> _logger;

  public PdfCompressService(ILogger<PdfCompressService> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Comprime um único PDF usando Ghostscript
  /// </summary>
  public async Task<CompressionResult> CompressPdfAsync(
      Stream pdfStream,
      CompressionLevel compressionLevel,
      bool removeImages = false,
      string fileName = "")
  {
    return await Task.Run(() =>
    {
      var result = new CompressionResult();
      string? tempInputPath = null;
      string? tempOutputPath = null;

      try
      {
        pdfStream.Position = 0;
        result.OriginalSizeBytes = pdfStream.Length;

        _logger.LogInformation("🗜️ Comprimindo PDF: {FileName} ({Size} bytes) - Nível: {Level}",
            fileName, result.OriginalSizeBytes, compressionLevel);

        // Verificar se Ghostscript está disponível
        var gsPath = FindGhostscript();
        if (string.IsNullOrEmpty(gsPath))
        {
          throw new Exception("Ghostscript não encontrado no sistema. Instale com: sudo apt-get install ghostscript");
        }

        _logger.LogInformation("✅ Ghostscript encontrado em: {Path}", gsPath);

        // Criar arquivos temporários
        tempInputPath = Path.Combine(Path.GetTempPath(), $"compress_input_{Guid.NewGuid()}.pdf");
        tempOutputPath = Path.Combine(Path.GetTempPath(), $"compress_output_{Guid.NewGuid()}.pdf");

        _logger.LogDebug("📁 Input temp: {Input}", tempInputPath);
        _logger.LogDebug("📁 Output temp: {Output}", tempOutputPath);

        // Salvar stream para arquivo temporário
        using (var fileStream = File.Create(tempInputPath))
        {
          pdfStream.CopyTo(fileStream);
        }

        var inputSize = new FileInfo(tempInputPath).Length;
        _logger.LogDebug("✅ Arquivo temporário criado: {Size} bytes", inputSize);

        // Comprimir usando Ghostscript
        CompressWithGhostscript(gsPath, tempInputPath, tempOutputPath, compressionLevel, removeImages);

        // Verificar se arquivo de saída foi criado
        if (!File.Exists(tempOutputPath))
        {
          throw new Exception("Ghostscript não gerou arquivo de saída");
        }

        var outputInfo = new FileInfo(tempOutputPath);
        if (outputInfo.Length == 0)
        {
          throw new Exception("Ghostscript gerou arquivo vazio");
        }

        // Ler arquivo comprimido
        result.CompressedData = File.ReadAllBytes(tempOutputPath);
        result.CompressedSizeBytes = result.CompressedData.Length;
        result.Success = true;

        var savings = ((1 - (double)result.CompressedSizeBytes / result.OriginalSizeBytes) * 100);

        _logger.LogInformation("✅ PDF comprimido: {Original} → {Compressed} (economia de {Savings:F2}%)",
            result.OriginalSizeMB, result.CompressedSizeMB, savings);

        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao comprimir PDF: {FileName}", fileName);

        // Em caso de erro, retornar o arquivo original
        try
        {
          pdfStream.Position = 0;
          using var ms = new MemoryStream();
          pdfStream.CopyTo(ms);

          result.CompressedData = ms.ToArray();
          result.CompressedSizeBytes = result.CompressedData.Length;
          result.Success = true;
          result.ErrorMessage = $"Aviso: PDF retornado sem compressão. {ex.Message}";

          _logger.LogWarning("⚠️ Retornando PDF original sem compressão");
        }
        catch
        {
          result.Success = false;
          result.ErrorMessage = ex.Message;
        }

        return result;
      }
      finally
      {
        // Limpar arquivos temporários
        try
        {
          if (tempInputPath != null && File.Exists(tempInputPath))
          {
            File.Delete(tempInputPath);
            _logger.LogDebug("🗑️ Arquivo temporário removido: {Path}", tempInputPath);
          }
          if (tempOutputPath != null && File.Exists(tempOutputPath))
          {
            File.Delete(tempOutputPath);
            _logger.LogDebug("🗑️ Arquivo temporário removido: {Path}", tempOutputPath);
          }
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "⚠️ Erro ao limpar arquivos temporários");
        }
      }
    });
  }

  /// <summary>
  /// Comprime múltiplos PDFs
  /// </summary>
  public async Task<Dictionary<string, CompressionResult>> CompressBatchAsync(
      Dictionary<string, Stream> files,
      Dictionary<string, CompressionSettings> compressionSettings)
  {
    var results = new Dictionary<string, CompressionResult>();

    foreach (var file in files)
    {
      var fileName = file.Key;
      var stream = file.Value;

      var settings = compressionSettings.ContainsKey(fileName)
          ? compressionSettings[fileName]
          : new CompressionSettings();

      var result = await CompressPdfAsync(
          stream,
          settings.Level,
          settings.RemoveImages,
          fileName);

      results[fileName] = result;
    }

    return results;
  }

  /// <summary>
  /// Encontra o executável do Ghostscript
  /// </summary>
  private string? FindGhostscript()
  {
    var possiblePaths = new[]
    {
      "/usr/bin/gs",              // Linux padrão
      "/usr/local/bin/gs",        // Linux local
      "/bin/gs",                  // Linux alternativo
      "gs"                        // PATH
    };

    foreach (var path in possiblePaths)
    {
      try
      {
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = path,
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
          return path;
        }
      }
      catch
      {
        continue;
      }
    }

    return null;
  }

  /// <summary>
  /// Comprime PDF usando Ghostscript via Process
  /// </summary>
  private void CompressWithGhostscript(
      string gsPath,
      string inputPath,
      string outputPath,
      CompressionLevel level,
      bool removeImages)
  {
    try
    {
      var (dPDFSettings, imageResolution) = GetQualitySettings(level);

      // Construir argumentos
      var args = new List<string>
      {
        "-sDEVICE=pdfwrite",
        "-dCompatibilityLevel=1.4",
        $"-dPDFSETTINGS=/{dPDFSettings}",
        "-dNOPAUSE",
        "-dQUIET",
        "-dBATCH",
        "-dSAFER",
        "-dDetectDuplicateImages=true",
        "-dCompressFonts=true",
        "-dCompressPages=true",
        "-dEmbedAllFonts=true",
        "-dSubsetFonts=true",
        "-dAutoRotatePages=/None",
        $"-dColorImageDownsampleType=/Bicubic",
        $"-dColorImageResolution={imageResolution}",
        $"-dGrayImageDownsampleType=/Bicubic",
        $"-dGrayImageResolution={imageResolution}",
        $"-dMonoImageDownsampleType=/Bicubic",
        $"-dMonoImageResolution={imageResolution}",
        $"-sOutputFile={outputPath}",
        inputPath
      };

      if (removeImages)
      {
        args.Insert(8, "-dFILTERIMAGE");
      }

      var argumentString = string.Join(" ", args);
      _logger.LogDebug("🔧 Comando Ghostscript: {Command} {Args}", gsPath, argumentString);

      // Executar processo
      var process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = gsPath,
          Arguments = argumentString,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      var output = new System.Text.StringBuilder();
      var error = new System.Text.StringBuilder();

      process.OutputDataReceived += (sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
          output.AppendLine(e.Data);
      };

      process.ErrorDataReceived += (sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
          error.AppendLine(e.Data);
      };

      process.Start();
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();
      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        var errorMessage = error.ToString();
        _logger.LogError("❌ Ghostscript falhou (exit code {Code}): {Error}",
            process.ExitCode, errorMessage);
        throw new Exception($"Ghostscript falhou: {errorMessage}");
      }

      _logger.LogDebug("✅ Ghostscript executado com sucesso");

      if (output.Length > 0)
      {
        _logger.LogDebug("📝 Output: {Output}", output.ToString());
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "❌ Erro ao executar Ghostscript");
      throw;
    }
  }

  /// <summary>
  /// Obtém configurações de qualidade
  /// </summary>
  private (string dPDFSettings, int imageResolution) GetQualitySettings(CompressionLevel level)
  {
    return level switch
    {
      CompressionLevel.Low => ("printer", 300),
      CompressionLevel.Medium => ("ebook", 150),
      CompressionLevel.High => ("screen", 72),
      _ => ("ebook", 150)
    };
  }
}