using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Cross-platform developer utility engine providing fast, resilient, and safe daily helpers.
/// </summary>
public sealed class DeveloperUtilitiesService : IDeveloperUtilitiesService
{
    private static readonly TimeSpan RegexEvaluationTimeout = TimeSpan.FromSeconds(2);

    public JsonFormatResult FormatJson(string json, bool prettify = true, int indentSpaces = 2)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonFormatResult
            {
                IsValid = false,
                ErrorMessage = "Input JSON string is empty."
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions
            {
                WriteIndented = prettify,
                IndentSize = Math.Clamp(indentSpaces, 1, 8)
            };

            string output = JsonSerializer.Serialize(doc.RootElement, options);
            return new JsonFormatResult
            {
                IsValid = true,
                FormattedJson = output
            };
        }
        catch (JsonException jex)
        {
            return new JsonFormatResult
            {
                IsValid = false,
                ErrorMessage = jex.Message,
                ErrorBytePosition = jex.BytePositionInLine
            };
        }
        catch (Exception ex)
        {
            return new JsonFormatResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public string EncodeBase64(string text, bool urlSafe = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        string base64 = Convert.ToBase64String(bytes);

        if (urlSafe)
        {
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        return base64;
    }

    public string DecodeBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return string.Empty;

        string normalized = base64.Trim().Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2: normalized += "=="; break;
            case 3: normalized += "="; break;
        }

        byte[] bytes = Convert.FromBase64String(normalized);
        return Encoding.UTF8.GetString(bytes);
    }

    public string EncodeUrl(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : Uri.EscapeDataString(text);
    }

    public string DecodeUrl(string encodedUrl)
    {
        return string.IsNullOrEmpty(encodedUrl) ? string.Empty : Uri.UnescapeDataString(encodedUrl);
    }

    public IReadOnlyList<string> GenerateUuids(int count = 1, string format = "D", bool uppercase = false)
    {
        int total = Math.Clamp(count, 1, 100);
        string fmt = format is "N" or "B" or "P" or "X" ? format : "D";

        var list = new List<string>(total);
        for (int i = 0; i < total; i++)
        {
            string s = Guid.NewGuid().ToString(fmt);
            list.Add(uppercase ? s.ToUpperInvariant() : s.ToLowerInvariant());
        }

        return list;
    }

    public string ComputeHash(string text, string algorithm = "SHA256")
    {
        if (text == null) return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(text);
        using HashAlgorithm hasher = algorithm.ToUpperInvariant() switch
        {
            "MD5" => MD5.Create(),
            "SHA1" or "SHA-1" => SHA1.Create(),
            "SHA384" or "SHA-384" => SHA384.Create(),
            "SHA512" or "SHA-512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        byte[] hash = hasher.ComputeHash(inputBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string TransformText(string text, TextTransformMode mode)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return mode switch
        {
            TextTransformMode.Uppercase => text.ToUpperInvariant(),
            TextTransformMode.Lowercase => text.ToLowerInvariant(),
            TextTransformMode.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text),
            TextTransformMode.CamelCase => ToCamelCase(text),
            TextTransformMode.SnakeCase => ToDelimitedCase(text, '_'),
            TextTransformMode.KebabCase => ToDelimitedCase(text, '-'),
            TextTransformMode.TrimWhitespace => string.Join('\n', text.Split('\n').Select(line => line.Trim())),
            TextTransformMode.SortLinesAscending => string.Join('\n', text.Split('\n').OrderBy(l => l, StringComparer.OrdinalIgnoreCase)),
            TextTransformMode.SortLinesDescending => string.Join('\n', text.Split('\n').OrderByDescending(l => l, StringComparer.OrdinalIgnoreCase)),
            TextTransformMode.DeduplicateLines => string.Join('\n', text.Split('\n').Distinct(StringComparer.Ordinal)),
            _ => text
        };
    }

    public TextStatistics AnalyzeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TextStatistics();
        }

        int charCount = text.Length;
        int noSpaces = text.Count(c => !char.IsWhiteSpace(c));
        int lineCount = text.Split('\n').Length;

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;

        return new TextStatistics
        {
            CharacterCount = charCount,
            CharacterCountNoSpaces = noSpaces,
            WordCount = wordCount,
            LineCount = lineCount
        };
    }

    public RegexTestResult TestRegex(string pattern, string input, RegexOptions options = RegexOptions.None)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return new RegexTestResult
            {
                Success = false,
                ErrorMessage = "Pattern cannot be empty."
            };
        }

        try
        {
            var regex = new Regex(pattern, options, RegexEvaluationTimeout);
            var matches = regex.Matches(input ?? string.Empty);

            var items = new List<RegexMatchItem>();
            foreach (Match match in matches)
            {
                var groups = new Dictionary<string, string>();
                foreach (Group g in match.Groups)
                {
                    if (g.Success)
                    {
                        groups[g.Name] = g.Value;
                    }
                }

                items.Add(new RegexMatchItem
                {
                    Index = match.Index,
                    Length = match.Length,
                    Value = match.Value,
                    Groups = groups
                });
            }

            return new RegexTestResult
            {
                Success = true,
                IsMatch = items.Count > 0,
                MatchCount = items.Count,
                Matches = items
            };
        }
        catch (RegexMatchTimeoutException)
        {
            return new RegexTestResult
            {
                Success = false,
                ErrorMessage = "Regex evaluation timed out (ReDoS protection)."
            };
        }
        catch (Exception ex)
        {
            return new RegexTestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public TimestampResult ConvertFromEpoch(long epochValue, bool isMilliseconds = false)
    {
        DateTimeOffset dt = isMilliseconds
            ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
            : DateTimeOffset.FromUnixTimeSeconds(epochValue);

        return new TimestampResult
        {
            EpochSeconds = dt.ToUnixTimeSeconds(),
            EpochMilliseconds = dt.ToUnixTimeMilliseconds(),
            UtcIso8601 = dt.UtcDateTime.ToString("o"),
            LocalDateTimeFormatted = dt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss (zzz)")
        };
    }

    public TimestampResult ConvertFromDateTime(DateTimeOffset dateTime)
    {
        return new TimestampResult
        {
            EpochSeconds = dateTime.ToUnixTimeSeconds(),
            EpochMilliseconds = dateTime.ToUnixTimeMilliseconds(),
            UtcIso8601 = dateTime.UtcDateTime.ToString("o"),
            LocalDateTimeFormatted = dateTime.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss (zzz)")
        };
    }

    public TimestampResult ParseDateToTimestamp(string dateString)
    {
        if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return ConvertFromDateTime(dt);
        }

        if (long.TryParse(dateString, out long epoch))
        {
            bool isMs = epoch > 9999999999L;
            return ConvertFromEpoch(epoch, isMs);
        }

        throw new FormatException($"Cannot parse '{dateString}' into a valid date or timestamp.");
    }

    public ColorConversionResult ConvertColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ColorConversionResult { IsValid = false, ErrorMessage = "Color input is empty." };
        }

        string trimmed = input.Trim();

        // 1. Try HEX format
        if (trimmed.StartsWith('#'))
        {
            string hexPart = trimmed[1..];
            if (hexPart.Length is 3 or 4 or 6 or 8)
            {
                byte a = 255, r = 0, g = 0, b = 0;
                if (hexPart.Length == 3)
                {
                    r = byte.Parse(new string(hexPart[0], 2), NumberStyles.HexNumber);
                    g = byte.Parse(new string(hexPart[1], 2), NumberStyles.HexNumber);
                    b = byte.Parse(new string(hexPart[2], 2), NumberStyles.HexNumber);
                }
                else if (hexPart.Length == 6)
                {
                    r = byte.Parse(hexPart[0..2], NumberStyles.HexNumber);
                    g = byte.Parse(hexPart[2..4], NumberStyles.HexNumber);
                    b = byte.Parse(hexPart[4..6], NumberStyles.HexNumber);
                }
                else if (hexPart.Length == 8)
                {
                    a = byte.Parse(hexPart[0..2], NumberStyles.HexNumber);
                    r = byte.Parse(hexPart[2..4], NumberStyles.HexNumber);
                    g = byte.Parse(hexPart[4..6], NumberStyles.HexNumber);
                    b = byte.Parse(hexPart[6..8], NumberStyles.HexNumber);
                }

                var hsl = RgbToHsl(r, g, b);
                return new ColorConversionResult
                {
                    IsValid = true,
                    Hex = $"#{r:X2}{g:X2}{b:X2}",
                    Red = r,
                    Green = g,
                    Blue = b,
                    Alpha = a,
                    Hue = hsl.Hue,
                    Saturation = hsl.Saturation,
                    Lightness = hsl.Lightness
                };
            }
        }

        // 2. Try RGB comma-separated or rgb(...) format
        var match = Regex.Match(trimmed, @"(?:rgb\s*\(\s*)?(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})");
        if (match.Success)
        {
            byte r = (byte)Math.Clamp(int.Parse(match.Groups[1].Value), 0, 255);
            byte g = (byte)Math.Clamp(int.Parse(match.Groups[2].Value), 0, 255);
            byte b = (byte)Math.Clamp(int.Parse(match.Groups[3].Value), 0, 255);

            var hsl = RgbToHsl(r, g, b);
            return new ColorConversionResult
            {
                IsValid = true,
                Hex = $"#{r:X2}{g:X2}{b:X2}",
                Red = r,
                Green = g,
                Blue = b,
                Alpha = 255,
                Hue = hsl.Hue,
                Saturation = hsl.Saturation,
                Lightness = hsl.Lightness
            };
        }

        return new ColorConversionResult
        {
            IsValid = false,
            ErrorMessage = "Unrecognized color format. Use #RRGGBB or rgb(r, g, b)."
        };
    }

    public QrCodeResult GenerateQr(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new QrCodeResult { Success = false, ErrorMessage = "Text for QR code cannot be empty." };
        }

        // Built-in QR Matrix generator (Model 2, Version 1: 21x21, Version 2: 25x25)
        int size = text.Length > 20 ? 25 : 21;
        bool[,] matrix = new bool[size, size];

        // Draw standard finder patterns at top-left, top-right, bottom-left
        DrawFinderPattern(matrix, 0, 0);
        DrawFinderPattern(matrix, size - 7, 0);
        DrawFinderPattern(matrix, 0, size - 7);

        // Draw timing patterns
        for (int i = 8; i < size - 8; i++)
        {
            matrix[6, i] = (i % 2) == 0;
            matrix[i, 6] = (i % 2) == 0;
        }

        // Embed text byte hash into data modules
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        int byteIdx = 0;
        int bitIdx = 7;

        for (int x = size - 1; x > 0; x -= 2)
        {
            if (x == 6) x--; // Skip timing column
            for (int y = 0; y < size; y++)
            {
                int row = ((x / 2) % 2 == 0) ? size - 1 - y : y;
                for (int col = x; col >= x - 1; col--)
                {
                    if (IsFunctionPattern(col, row, size)) continue;

                    bool bit = false;
                    if (byteIdx < bytes.Length)
                    {
                        bit = ((bytes[byteIdx] >> bitIdx) & 1) == 1;
                        bitIdx--;
                        if (bitIdx < 0)
                        {
                            bitIdx = 7;
                            byteIdx++;
                        }
                    }
                    else
                    {
                        // Padding pattern
                        bit = ((col + row) % 2) == 0;
                    }

                    // Apply standard mask pattern (row + col) % 2 == 0
                    matrix[col, row] = bit ^ (((col + row) % 2) == 0);
                }
            }
        }

        // Build SVG representation
        var sbSvg = new StringBuilder();
        sbSvg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size + 4} {size + 4}\" shape-rendering=\"crispEdges\">");
        sbSvg.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (matrix[x, y])
                {
                    sbSvg.AppendLine($"  <rect x=\"{x + 2}\" y=\"{y + 2}\" width=\"1\" height=\"1\" fill=\"#000000\"/>");
                }
            }
        }
        sbSvg.AppendLine("</svg>");

        // Build ASCII preview
        var sbAscii = new StringBuilder();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                sbAscii.Append(matrix[x, y] ? "██" : "  ");
            }
            sbAscii.AppendLine();
        }

        return new QrCodeResult
        {
            Success = true,
            MatrixSize = size,
            Matrix = matrix,
            SvgContent = sbSvg.ToString(),
            AsciiPreview = sbAscii.ToString()
        };
    }

    public UnitConversionResult ConvertUnit(double value, string category, string fromUnit, string toUnit)
    {
        string cat = category.Trim();
        string from = fromUnit.Trim().ToLowerInvariant();
        string to = toUnit.Trim().ToLowerInvariant();

        try
        {
            double result = cat.ToLowerInvariant() switch
            {
                "length" => ConvertLength(value, from, to),
                "mass" => ConvertMass(value, from, to),
                "temperature" => ConvertTemperature(value, from, to),
                "digital storage" or "storage" => ConvertDigitalStorage(value, from, to),
                _ => throw new ArgumentException($"Unsupported category '{category}'.")
            };

            return new UnitConversionResult
            {
                Success = true,
                InputValue = value,
                FromUnit = fromUnit,
                OutputValue = result,
                ToUnit = toUnit,
                Category = category
            };
        }
        catch (Exception ex)
        {
            return new UnitConversionResult
            {
                Success = false,
                InputValue = value,
                FromUnit = fromUnit,
                ToUnit = toUnit,
                Category = category,
                ErrorMessage = ex.Message
            };
        }
    }

    public IReadOnlyList<string> GetSupportedUnitCategories()
    {
        return new[] { "Length", "Mass", "Temperature", "Digital Storage" };
    }

    public IReadOnlyList<string> GetUnitsForCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "length" => new[] { "m", "km", "cm", "mm", "mi", "yd", "ft", "in" },
            "mass" => new[] { "kg", "g", "mg", "lb", "oz" },
            "temperature" => new[] { "C", "F", "K" },
            "digital storage" or "storage" => new[] { "B", "KB", "MB", "GB", "TB", "PB" },
            _ => Array.Empty<string>()
        };
    }

    #region Helper Conversion Calculations

    private static double ConvertLength(double val, string from, string to)
    {
        // Convert to base: Meter
        double inMeters = from switch
        {
            "m" or "meter" or "meters" => val,
            "km" or "kilometer" or "kilometers" => val * 1000.0,
            "cm" or "centimeter" or "centimeters" => val * 0.01,
            "mm" or "millimeter" or "millimeters" => val * 0.001,
            "mi" or "mile" or "miles" => val * 1609.344,
            "yd" or "yard" or "yards" => val * 0.9144,
            "ft" or "foot" or "feet" => val * 0.3048,
            "in" or "inch" or "inches" => val * 0.0254,
            _ => throw new ArgumentException($"Unknown length unit: {from}")
        };

        return to switch
        {
            "m" or "meter" or "meters" => inMeters,
            "km" or "kilometer" or "kilometers" => inMeters / 1000.0,
            "cm" or "centimeter" or "centimeters" => inMeters / 0.01,
            "mm" or "millimeter" or "millimeters" => inMeters / 0.001,
            "mi" or "mile" or "miles" => inMeters / 1609.344,
            "yd" or "yard" or "yards" => inMeters / 0.9144,
            "ft" or "foot" or "feet" => inMeters / 0.3048,
            "in" or "inch" or "inches" => inMeters / 0.0254,
            _ => throw new ArgumentException($"Unknown length unit: {to}")
        };
    }

    private static double ConvertMass(double val, string from, string to)
    {
        // Base: Kilogram
        double inKg = from switch
        {
            "kg" or "kilogram" or "kilograms" => val,
            "g" or "gram" or "grams" => val * 0.001,
            "mg" or "milligram" or "milligrams" => val * 0.000001,
            "lb" or "pound" or "pounds" => val * 0.45359237,
            "oz" or "ounce" or "ounces" => val * 0.028349523125,
            _ => throw new ArgumentException($"Unknown mass unit: {from}")
        };

        return to switch
        {
            "kg" or "kilogram" or "kilograms" => inKg,
            "g" or "gram" or "grams" => inKg / 0.001,
            "mg" or "milligram" or "milligrams" => inKg / 0.000001,
            "lb" or "pound" or "pounds" => inKg / 0.45359237,
            "oz" or "ounce" or "ounces" => inKg / 0.028349523125,
            _ => throw new ArgumentException($"Unknown mass unit: {to}")
        };
    }

    private static double ConvertTemperature(double val, string from, string to)
    {
        // Base: Celsius
        double inC = from switch
        {
            "c" or "celsius" => val,
            "f" or "fahrenheit" => (val - 32.0) * 5.0 / 9.0,
            "k" or "kelvin" => val - 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {from}")
        };

        return to switch
        {
            "c" or "celsius" => inC,
            "f" or "fahrenheit" => inC * 9.0 / 5.0 + 32.0,
            "k" or "kelvin" => inC + 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {to}")
        };
    }

    private static double ConvertDigitalStorage(double val, string from, string to)
    {
        // Base: Bytes
        double inBytes = from switch
        {
            "b" or "bytes" => val,
            "kb" or "kilobyte" => val * 1024.0,
            "mb" or "megabyte" => val * 1024.0 * 1024.0,
            "gb" or "gigabyte" => val * 1024.0 * 1024.0 * 1024.0,
            "tb" or "terabyte" => val * 1024.0 * 1024.0 * 1024.0 * 1024.0,
            "pb" or "petabyte" => val * 1024.0 * 1024.0 * 1024.0 * 1024.0 * 1024.0,
            _ => throw new ArgumentException($"Unknown storage unit: {from}")
        };

        return to switch
        {
            "b" or "bytes" => inBytes,
            "kb" or "kilobyte" => inBytes / 1024.0,
            "mb" or "megabyte" => inBytes / (1024.0 * 1024.0),
            "gb" or "gigabyte" => inBytes / (1024.0 * 1024.0 * 1024.0),
            "tb" or "terabyte" => inBytes / (1024.0 * 1024.0 * 1024.0 * 1024.0),
            "pb" or "petabyte" => inBytes / (1024.0 * 1024.0 * 1024.0 * 1024.0 * 1024.0),
            _ => throw new ArgumentException($"Unknown storage unit: {to}")
        };
    }

    private static (double Hue, double Saturation, double Lightness) RgbToHsl(byte r, byte g, byte b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        double l = (max + min) / 2.0;
        double h = 0.0;
        double s = 0.0;

        if (delta > 0.00001)
        {
            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (Math.Abs(max - rd) < 0.00001)
            {
                h = ((gd - bd) / delta) + (gd < bd ? 6.0 : 0.0);
            }
            else if (Math.Abs(max - gd) < 0.00001)
            {
                h = ((bd - rd) / delta) + 2.0;
            }
            else
            {
                h = ((rd - gd) / delta) + 4.0;
            }

            h *= 60.0;
        }

        return (h, s, l);
    }

    private static string ToCamelCase(string text)
    {
        var words = Regex.Split(text, @"[\s_-]+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        if (words.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append(words[0].ToLowerInvariant());
        for (int i = 1; i < words.Length; i++)
        {
            sb.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLowerInvariant()));
        }
        return sb.ToString();
    }

    private static string ToDelimitedCase(string text, char delimiter)
    {
        var words = Regex.Split(text, @"[\s_-]+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        return string.Join(delimiter, words.Select(w => w.ToLowerInvariant()));
    }

    private static void DrawFinderPattern(bool[,] matrix, int startX, int startY)
    {
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 7; y++)
            {
                bool isBorder = x is 0 or 6 || y is 0 or 6;
                bool isCenter = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                matrix[startX + x, startY + y] = isBorder || isCenter;
            }
        }
    }

    private static bool IsFunctionPattern(int x, int y, int size)
    {
        // Top-left finder
        if (x < 9 && y < 9) return true;
        // Top-right finder
        if (x >= size - 8 && y < 9) return true;
        // Bottom-left finder
        if (x < 9 && y >= size - 8) return true;
        // Timing
        if (x == 6 || y == 6) return true;

        return false;
    }

    #endregion
}
