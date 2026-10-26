using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum TextTransformMode
{
    Uppercase,
    Lowercase,
    TitleCase,
    CamelCase,
    SnakeCase,
    KebabCase,
    TrimWhitespace,
    SortLinesAscending,
    SortLinesDescending,
    DeduplicateLines
}

public record TextStatistics
{
    public int CharacterCount { get; init; }
    public int CharacterCountNoSpaces { get; init; }
    public int WordCount { get; init; }
    public int LineCount { get; init; }
}

public record JsonFormatResult
{
    public bool IsValid { get; init; }
    public string FormattedJson { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public long? ErrorBytePosition { get; init; }
}

public record RegexMatchItem
{
    public int Index { get; init; }
    public int Length { get; init; }
    public string Value { get; init; } = string.Empty;
    public Dictionary<string, string> Groups { get; init; } = new();
}

public record RegexTestResult
{
    public bool Success { get; init; }
    public bool IsMatch { get; init; }
    public int MatchCount { get; init; }
    public IReadOnlyList<RegexMatchItem> Matches { get; init; } = Array.Empty<RegexMatchItem>();
    public string? ErrorMessage { get; init; }
}

public record TimestampResult
{
    public long EpochSeconds { get; init; }
    public long EpochMilliseconds { get; init; }
    public string UtcIso8601 { get; init; } = string.Empty;
    public string LocalDateTimeFormatted { get; init; } = string.Empty;
}

public record ColorConversionResult
{
    public bool IsValid { get; init; }
    public string Hex { get; init; } = string.Empty;
    public byte Red { get; init; }
    public byte Green { get; init; }
    public byte Blue { get; init; }
    public byte Alpha { get; init; } = 255;
    public double Hue { get; init; }
    public double Saturation { get; init; }
    public double Lightness { get; init; }
    public string RgbString => $"rgb({Red}, {Green}, {Blue})";
    public string HslString => $"hsl({Math.Round(Hue)}, {Math.Round(Saturation * 100)}%, {Math.Round(Lightness * 100)}%)";
    public string? ErrorMessage { get; init; }
}

public record QrCodeResult
{
    public bool Success { get; init; }
    public int MatrixSize { get; init; }
    public bool[,]? Matrix { get; init; }
    public string SvgContent { get; init; } = string.Empty;
    public string AsciiPreview { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

public record UnitConversionResult
{
    public bool Success { get; init; }
    public double InputValue { get; init; }
    public string FromUnit { get; init; } = string.Empty;
    public double OutputValue { get; init; }
    public string ToUnit { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string FormattedResult => $"{InputValue:G6} {FromUnit} = {OutputValue:G6} {ToUnit}";
    public string? ErrorMessage { get; init; }
}
