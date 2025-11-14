using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfProcessor.Shared.Extensions;

/// <summary>
/// Extensões comuns utilizadas em todo o sistema
/// </summary>
public static class CommonExtensions
{
  #region String Extensions

  /// <summary>
  /// Verifica se a string está vazia ou nula
  /// </summary>
  public static bool IsNullOrEmpty(this string? value)
      => string.IsNullOrEmpty(value);

  /// <summary>
  /// Verifica se a string está vazia, nula ou contém apenas espaços em branco
  /// </summary>
  public static bool IsNullOrWhiteSpace(this string? value)
      => string.IsNullOrWhiteSpace(value);

  /// <summary>
  /// Remove espaços extras e normaliza a string
  /// </summary>
  public static string Normalize(this string value)
  {
    if (value.IsNullOrWhiteSpace())
      return string.Empty;

    return Regex.Replace(value.Trim(), @"\s+", " ");
  }

  /// <summary>
  /// Remove caracteres especiais mantendo apenas letras, números e espaços
  /// </summary>
  public static string RemoveSpecialCharacters(this string value)
  {
    if (value.IsNullOrWhiteSpace())
      return string.Empty;

    return Regex.Replace(value, @"[^a-zA-Z0-9\s]", "");
  }

  /// <summary>
  /// Converte string para título (primeira letra maiúscula)
  /// </summary>
  public static string ToTitleCase(this string value)
  {
    if (value.IsNullOrWhiteSpace())
      return string.Empty;

    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
  }

  /// <summary>
  /// Trunca a string se exceder o tamanho máximo
  /// </summary>
  public static string Truncate(this string value, int maxLength, string suffix = "...")
  {
    if (value.IsNullOrWhiteSpace())
      return string.Empty;

    if (value.Length <= maxLength)
      return value;

    return value.Substring(0, maxLength - suffix.Length) + suffix;
  }

  #endregion

  #region Decimal Extensions

  /// <summary>
  /// Converte string para decimal, retornando null se inválido
  /// </summary>
  public static decimal? ToDecimalOrNull(this string value)
  {
    if (value.IsNullOrWhiteSpace())
      return null;

    // Remove caracteres não numéricos exceto . , -
    var cleaned = Regex.Replace(value, @"[^\d.,-]", "");

    // Tenta converter
    if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
      return result;

    // Tenta com cultura pt-BR
    if (decimal.TryParse(cleaned, NumberStyles.Any, new CultureInfo("pt-BR"), out result))
      return result;

    return null;
  }

  /// <summary>
  /// Formata decimal como moeda brasileira
  /// </summary>
  public static string ToBrazilianCurrency(this decimal value)
      => value.ToString("C2", new CultureInfo("pt-BR"));

  /// <summary>
  /// Formata decimal como moeda americana
  /// </summary>
  public static string ToUSDCurrency(this decimal value)
      => value.ToString("C2", new CultureInfo("en-US"));

  #endregion

  #region DateTime Extensions

  /// <summary>
  /// Converte string para DateTime, retornando null se inválido
  /// </summary>
  public static DateTime? ToDateTimeOrNull(this string value)
  {
    if (value.IsNullOrWhiteSpace())
      return null;

    var formats = new[]
    {
            "dd/MM/yyyy",
            "dd/MM/yy",
            "MM/dd/yyyy",
            "yyyy-MM-dd",
            "dd-MM-yyyy"
        };

    foreach (var format in formats)
    {
      if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
          DateTimeStyles.None, out var result))
      {
        // ✅ CORREÇÃO: Especificar DateTimeKind.Unspecified
        return DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
      }
    }

    if (DateTime.TryParse(value, out var parsedDate))
    {
      // ✅ CORREÇÃO: Especificar DateTimeKind.Unspecified
      return DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified);
    }

    return null;
  }

  /// <summary>
  /// Formata DateTime para padrão brasileiro (dd/MM/yyyy)
  /// </summary>
  public static string ToBrazilianFormat(this DateTime dateTime)
      => dateTime.ToString("dd/MM/yyyy");

  /// <summary>
  /// Formata DateTime para padrão ISO (yyyy-MM-dd)
  /// </summary>
  public static string ToIsoFormat(this DateTime dateTime)
      => dateTime.ToString("yyyy-MM-dd");

  #endregion

  #region Collection Extensions

  /// <summary>
  /// Verifica se a coleção está vazia ou nula
  /// </summary>
  public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
      => collection == null || !collection.Any();

  /// <summary>
  /// Divide uma lista em chunks menores
  /// </summary>
  public static IEnumerable<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
  {
    return source
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / chunkSize)
        .Select(x => x.Select(v => v.Value).ToList());
  }

  #endregion

  #region Stream Extensions

  /// <summary>
  /// Converte Stream para byte array
  /// </summary>
  public static async Task<byte[]> ToByteArrayAsync(this Stream stream)
  {
    if (stream is MemoryStream memoryStream)
      return memoryStream.ToArray();

    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    return ms.ToArray();
  }

  /// <summary>
  /// Reseta a posição do Stream para o início
  /// </summary>
  public static void Reset(this Stream stream)
  {
    if (stream.CanSeek)
      stream.Position = 0;
  }

  #endregion

  #region File Extensions

  /// <summary>
  /// Sanitiza nome de arquivo removendo caracteres inválidos
  /// </summary>
  public static string SanitizeFileName(this string fileName)
  {
    var invalidChars = Path.GetInvalidFileNameChars();
    var sanitized = string.Join("_", fileName.Split(invalidChars));
    return sanitized.Trim();
  }

  /// <summary>
  /// Obtém extensão do arquivo sem o ponto
  /// </summary>
  public static string GetExtensionWithoutDot(this string fileName)
      => Path.GetExtension(fileName).TrimStart('.');

  /// <summary>
  /// Verifica se o arquivo tem extensão PDF
  /// </summary>
  public static bool IsPdfFile(this string fileName)
      => fileName.GetExtensionWithoutDot().Equals("pdf", StringComparison.OrdinalIgnoreCase);

  #endregion
}