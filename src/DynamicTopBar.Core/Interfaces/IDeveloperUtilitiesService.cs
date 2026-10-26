using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract providing daily developer utilities: JSON formatting, Base64/URL encoding,
/// UUID generation, hashing, text transformations, regex testing, timestamp conversions,
/// color spaces, QR codes, and unit conversions.
/// </summary>
public interface IDeveloperUtilitiesService
{
    JsonFormatResult FormatJson(string json, bool prettify = true, int indentSpaces = 2);

    string EncodeBase64(string text, bool urlSafe = false);
    string DecodeBase64(string base64);

    string EncodeUrl(string text);
    string DecodeUrl(string encodedUrl);

    IReadOnlyList<string> GenerateUuids(int count = 1, string format = "D", bool uppercase = false);

    string ComputeHash(string text, string algorithm = "SHA256");

    string TransformText(string text, TextTransformMode mode);
    TextStatistics AnalyzeText(string text);

    RegexTestResult TestRegex(string pattern, string input, RegexOptions options = RegexOptions.None);

    TimestampResult ConvertFromEpoch(long epochValue, bool isMilliseconds = false);
    TimestampResult ConvertFromDateTime(DateTimeOffset dateTime);
    TimestampResult ParseDateToTimestamp(string dateString);

    ColorConversionResult ConvertColor(string input);

    QrCodeResult GenerateQr(string text);

    UnitConversionResult ConvertUnit(double value, string category, string fromUnit, string toUnit);
    IReadOnlyList<string> GetSupportedUnitCategories();
    IReadOnlyList<string> GetUnitsForCategory(string category);
}
