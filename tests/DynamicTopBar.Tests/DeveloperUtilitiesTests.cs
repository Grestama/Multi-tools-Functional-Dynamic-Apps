using System;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class DeveloperUtilitiesTests
{
    private readonly DeveloperUtilitiesService _devUtils = new();

    [Fact]
    public void FormatJson_Prettify_ReturnsFormattedIndentedJson()
    {
        string raw = "{\"name\":\"DynamicTopBar\",\"version\":3,\"active\":true}";
        var res = _devUtils.FormatJson(raw, prettify: true, indentSpaces: 2);

        Assert.True(res.IsValid);
        Assert.Contains("\n", res.FormattedJson);
        Assert.Contains("  \"name\": \"DynamicTopBar\"", res.FormattedJson);
    }

    [Fact]
    public void FormatJson_Minify_ReturnsCompactJson()
    {
        string indented = "{\n  \"name\": \"DynamicTopBar\",\n  \"active\": true\n}";
        var res = _devUtils.FormatJson(indented, prettify: false);

        Assert.True(res.IsValid);
        Assert.DoesNotContain("\n", res.FormattedJson);
        Assert.Contains("\"name\":\"DynamicTopBar\"", res.FormattedJson);
    }

    [Fact]
    public void FormatJson_InvalidJson_ReportsErrorWithPosition()
    {
        string broken = "{\"name\": \"test\", broken}";
        var res = _devUtils.FormatJson(broken);

        Assert.False(res.IsValid);
        Assert.NotNull(res.ErrorMessage);
        Assert.NotNull(res.ErrorBytePosition);
    }

    [Fact]
    public void Base64_Roundtrip_WorksCorrectly()
    {
        string original = "DynamicTopBar 2026 - Windows Native Multi-Tools";
        string encoded = _devUtils.EncodeBase64(original);
        string decoded = _devUtils.DecodeBase64(encoded);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Base64_UrlSafe_ReplacesSpecialChars()
    {
        string text = "subjects?_d=1&test=true+ok";
        string encodedUrlSafe = _devUtils.EncodeBase64(text, urlSafe: true);

        Assert.DoesNotContain("+", encodedUrlSafe);
        Assert.DoesNotContain("/", encodedUrlSafe);
        Assert.DoesNotContain("=", encodedUrlSafe);

        string decoded = _devUtils.DecodeBase64(encodedUrlSafe);
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void UrlEncodeDecode_Roundtrip_Works()
    {
        string raw = "https://example.com/search?q=C# & WinUI 3+Tools";
        string encoded = _devUtils.EncodeUrl(raw);
        string decoded = _devUtils.DecodeUrl(encoded);

        Assert.DoesNotContain(" ", encoded);
        Assert.Equal(raw, decoded);
    }

    [Fact]
    public void GenerateUuids_GeneratesSpecifiedCountAndFormat()
    {
        var uuids = _devUtils.GenerateUuids(count: 5, format: "D", uppercase: true);

        Assert.Equal(5, uuids.Count);
        Assert.All(uuids, u =>
        {
            Assert.True(Guid.TryParse(u, out _));
            Assert.Contains("-", u);
            Assert.Equal(u, u.ToUpperInvariant());
        });
    }

    [Theory]
    [InlineData("hello world", "MD5", "5eb63bbbe01eeed093cb22bb8f5acdc3")]
    [InlineData("hello world", "SHA256", "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9")]
    [InlineData("hello world", "SHA1", "2aae6c35c94fcfb415dbe95f408b9ce91ee846ed")]
    public void ComputeHash_MatchesKnownVectors(string input, string algo, string expectedHex)
    {
        string hash = _devUtils.ComputeHash(input, algo);
        Assert.Equal(expectedHex, hash);
    }

    [Fact]
    public void TransformText_AppliesVariousModes()
    {
        string input = "dynamic top bar";

        Assert.Equal("DYNAMIC TOP BAR", _devUtils.TransformText(input, TextTransformMode.Uppercase));
        Assert.Equal("dynamic top bar", _devUtils.TransformText("DYNAMIC TOP BAR", TextTransformMode.Lowercase));
        Assert.Equal("dynamicTopBar", _devUtils.TransformText(input, TextTransformMode.CamelCase));
        Assert.Equal("dynamic_top_bar", _devUtils.TransformText(input, TextTransformMode.SnakeCase));
        Assert.Equal("dynamic-top-bar", _devUtils.TransformText(input, TextTransformMode.KebabCase));

        string lines = "banana\napple\norange\napple";
        string sorted = _devUtils.TransformText(lines, TextTransformMode.SortLinesAscending);
        Assert.StartsWith("apple\napple", sorted);

        string deduped = _devUtils.TransformText(lines, TextTransformMode.DeduplicateLines);
        Assert.Equal("banana\napple\norange", deduped);
    }

    [Fact]
    public void AnalyzeText_CalculatesStatisticsAccurately()
    {
        string text = "Hello world from WinUI 3\nSecond line here";
        var stats = _devUtils.AnalyzeText(text);

        Assert.Equal(text.Length, stats.CharacterCount);
        Assert.Equal(8, stats.WordCount);
        Assert.Equal(2, stats.LineCount);
    }

    [Fact]
    public void TestRegex_FindsMatchesAndCapturingGroups()
    {
        string pattern = @"(?<key>\w+)=(?<val>\d+)";
        string input = "item1=100; item2=250; item3=999";

        var res = _devUtils.TestRegex(pattern, input);

        Assert.True(res.Success);
        Assert.True(res.IsMatch);
        Assert.Equal(3, res.MatchCount);
        Assert.Equal("item1=100", res.Matches[0].Value);
        Assert.Equal("item1", res.Matches[0].Groups["key"]);
        Assert.Equal("100", res.Matches[0].Groups["val"]);
    }

    [Fact]
    public void TestRegex_InvalidPattern_ReturnsErrorSafely()
    {
        string pattern = "[broken-regex(";
        var res = _devUtils.TestRegex(pattern, "text");

        Assert.False(res.Success);
        Assert.NotNull(res.ErrorMessage);
    }

    [Fact]
    public void TimestampConverter_EpochAndDateRoundtrip()
    {
        long epoch = 1700000000;
        var res = _devUtils.ConvertFromEpoch(epoch, isMilliseconds: false);

        Assert.Equal(epoch, res.EpochSeconds);
        Assert.Contains("2023-11-14", res.UtcIso8601);

        var parseRes = _devUtils.ParseDateToTimestamp("2023-11-14T22:13:20Z");
        Assert.Equal(epoch, parseRes.EpochSeconds);
    }

    [Fact]
    public void ColorConverter_ParsesHexAndCalculatesRgbHsl()
    {
        string hex = "#00B4FF";
        var res = _devUtils.ConvertColor(hex);

        Assert.True(res.IsValid);
        Assert.Equal("#00B4FF", res.Hex);
        Assert.Equal(0, res.Red);
        Assert.Equal(180, res.Green);
        Assert.Equal(255, res.Blue);
        Assert.True(res.Hue > 190 && res.Hue < 205); // Approx 197 deg Hue
    }

    [Fact]
    public void QrGenerator_GeneratesValidMatrixAndSvg()
    {
        string text = "https://github.com";
        var res = _devUtils.GenerateQr(text);

        Assert.True(res.Success);
        Assert.True(res.MatrixSize >= 21);
        Assert.NotNull(res.Matrix);
        Assert.Contains("<svg", res.SvgContent);
        Assert.Contains("██", res.AsciiPreview);
    }

    [Theory]
    [InlineData(1000, "Length", "m", "km", 1.0)]
    [InlineData(1, "Length", "mi", "km", 1.609344)]
    [InlineData(1, "Mass", "kg", "g", 1000.0)]
    [InlineData(100, "Temperature", "C", "F", 212.0)]
    [InlineData(1024, "Digital Storage", "KB", "MB", 1.0)]
    public void UnitConverter_ConvertsAccurately(double input, string cat, string from, string to, double expected)
    {
        var res = _devUtils.ConvertUnit(input, cat, from, to);

        Assert.True(res.Success);
        Assert.Equal(expected, res.OutputValue, 4);
    }
}
