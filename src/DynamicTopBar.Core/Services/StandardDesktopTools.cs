using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Built-in tools conforming to the IDesktopTool contract for the Universal Tools Engine.
/// </summary>
public static class StandardDesktopTools
{
    public static IEnumerable<IDesktopTool> CreateAll(
        IDeveloperUtilitiesService devUtils,
        IFileToolsService fileTools,
        IImageToolsService? imageTools = null)
    {
        yield return new JsonFormatTool(devUtils);
        yield return new Base64Tool(devUtils);
        yield return new UrlCodecTool(devUtils);
        yield return new UuidTool(devUtils);
        yield return new TextFormatTool(devUtils);
        yield return new RegexTesterTool(devUtils);
        yield return new HashCalculatorTool(devUtils);
        yield return new TimestampConverterTool(devUtils);
        yield return new ColorConverterTool(devUtils);
        yield return new QrGeneratorTool(devUtils);
        yield return new UnitConverterTool(devUtils);
        yield return new BatchRenameTool(fileTools);
        yield return new ZipArchiveTool(fileTools);
        yield return new FolderSizeTool(fileTools);
        yield return new FileSearchTool(fileTools);
        yield return new FileHashTool(fileTools);
        yield return new FileConvertTool(fileTools);

        if (imageTools != null)
        {
            yield return new ImageCompressTool(imageTools);
            yield return new ImageResizeTool(imageTools);
            yield return new ImageConvertTool(imageTools);
        }
    }

    #region Developer Tools

    public sealed class JsonFormatTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-json-format";
        public string DisplayName => "JSON Formatter";
        public string Description => "Format, validate, or minify JSON data";
        public string Category => "Developer";
        public string IconGlyph => "\uE943"; // Code icon

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "mode",
                DisplayName = "Mode",
                Description = "Prettify or Minify",
                Type = ToolParameterType.Options,
                Options = new[] { "Prettify", "Minify" },
                DefaultValue = "Prettify"
            },
            new ToolParameterDefinition
            {
                Key = "indent",
                DisplayName = "Indent Spaces",
                Type = ToolParameterType.Number,
                DefaultValue = 2
            }
        };

        public JsonFormatTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input JSON is required."));
            }

            string mode = context.GetParameter("mode", "Prettify") ?? "Prettify";
            int indent = context.GetParameter("indent", 2);
            bool prettify = string.Equals(mode, "Prettify", StringComparison.OrdinalIgnoreCase);

            var res = _devUtils.FormatJson(context.InputText, prettify, indent);
            return Task.FromResult(res.IsValid
                ? ToolResult.Ok(res.FormattedJson, res)
                : ToolResult.Fail(res.ErrorMessage ?? "Invalid JSON."));
        }
    }

    public sealed class Base64Tool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-base64";
        public string DisplayName => "Base64 Encoder/Decoder";
        public string Description => "Encode and decode text to/from Base64";
        public string Category => "Developer";
        public string IconGlyph => "\uE8D7";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "operation",
                DisplayName = "Operation",
                Type = ToolParameterType.Options,
                Options = new[] { "Encode", "Decode" },
                DefaultValue = "Encode"
            },
            new ToolParameterDefinition
            {
                Key = "urlSafe",
                DisplayName = "URL Safe",
                Type = ToolParameterType.Boolean,
                DefaultValue = false
            }
        };

        public Base64Tool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input text is required."));
            }

            string op = context.GetParameter("operation", "Encode") ?? "Encode";
            bool urlSafe = context.GetParameter("urlSafe", false);

            try
            {
                string output = string.Equals(op, "Decode", StringComparison.OrdinalIgnoreCase)
                    ? _devUtils.DecodeBase64(context.InputText)
                    : _devUtils.EncodeBase64(context.InputText, urlSafe);

                return Task.FromResult(ToolResult.Ok(output));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Fail(ex.Message));
            }
        }
    }

    public sealed class UrlCodecTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-url-codec";
        public string DisplayName => "URL Encoder/Decoder";
        public string Description => "Encode special characters or decode URL-encoded text";
        public string Category => "Developer";
        public string IconGlyph => "\uE71B"; // Link glyph

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "operation",
                DisplayName = "Operation",
                Type = ToolParameterType.Options,
                Options = new[] { "Encode", "Decode" },
                DefaultValue = "Encode"
            }
        };

        public UrlCodecTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input text is required for URL encoding/decoding."));
            }

            string op = context.GetParameter("operation", "Encode") ?? "Encode";
            try
            {
                string result = string.Equals(op, "Decode", StringComparison.OrdinalIgnoreCase)
                    ? _devUtils.DecodeUrl(context.InputText)
                    : _devUtils.EncodeUrl(context.InputText);

                return Task.FromResult(ToolResult.Ok(result));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Fail(ex.Message));
            }
        }
    }

    public sealed class UuidTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-uuid";
        public string DisplayName => "UUID Generator";
        public string Description => "Generate standard GUID/UUIDs in various formats";
        public string Category => "Developer";
        public string IconGlyph => "\uE71D";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "count",
                DisplayName = "Count",
                Type = ToolParameterType.Number,
                DefaultValue = 1
            },
            new ToolParameterDefinition
            {
                Key = "format",
                DisplayName = "Format",
                Type = ToolParameterType.Options,
                Options = new[] { "D", "N", "B", "P" },
                DefaultValue = "D"
            },
            new ToolParameterDefinition
            {
                Key = "uppercase",
                DisplayName = "Uppercase",
                Type = ToolParameterType.Boolean,
                DefaultValue = false
            }
        };

        public UuidTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int count = context.GetParameter("count", 1);
            string format = context.GetParameter("format", "D") ?? "D";
            bool uppercase = context.GetParameter("uppercase", false);

            var list = _devUtils.GenerateUuids(count, format, uppercase);
            return Task.FromResult(ToolResult.Ok(string.Join(Environment.NewLine, list), list));
        }
    }

    public sealed class TextFormatTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-text-format";
        public string DisplayName => "Text Formatter";
        public string Description => "Transform text case, trim whitespace, sort or deduplicate lines";
        public string Category => "Developer";
        public string IconGlyph => "\uE8D2";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "mode",
                DisplayName = "Transformation",
                Type = ToolParameterType.Options,
                Options = Enum.GetNames<TextTransformMode>(),
                DefaultValue = "Uppercase"
            }
        };

        public TextFormatTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input text is required."));
            }

            string modeStr = context.GetParameter("mode", "Uppercase") ?? "Uppercase";
            if (!Enum.TryParse<TextTransformMode>(modeStr, true, out var mode))
            {
                mode = TextTransformMode.Uppercase;
            }

            string result = _devUtils.TransformText(context.InputText, mode);
            var stats = _devUtils.AnalyzeText(result);
            return Task.FromResult(ToolResult.Ok(result, stats));
        }
    }

    public sealed class RegexTesterTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-regex-tester";
        public string DisplayName => "Regex Tester";
        public string Description => "Test regular expressions against input text";
        public string Category => "Developer";
        public string IconGlyph => "\uE71E";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "pattern",
                DisplayName = "Pattern",
                Type = ToolParameterType.String,
                IsRequired = true
            },
            new ToolParameterDefinition
            {
                Key = "ignoreCase",
                DisplayName = "Ignore Case",
                Type = ToolParameterType.Boolean,
                DefaultValue = true
            }
        };

        public RegexTesterTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? pattern = context.GetParameter<string>("pattern");
            if (string.IsNullOrEmpty(pattern))
            {
                return Task.FromResult(ToolResult.Fail("Regex pattern is required."));
            }

            bool ignoreCase = context.GetParameter("ignoreCase", true);
            var opts = ignoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None;

            var res = _devUtils.TestRegex(pattern, context.InputText ?? string.Empty, opts);
            if (!res.Success)
            {
                return Task.FromResult(ToolResult.Fail(res.ErrorMessage ?? "Regex evaluation failed."));
            }

            string summary = res.IsMatch
                ? $"Matched {res.MatchCount} instance(s)."
                : "No match found.";

            return Task.FromResult(ToolResult.Ok(summary, res));
        }
    }

    public sealed class HashCalculatorTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-hash";
        public string DisplayName => "Hash Calculator";
        public string Description => "Calculate MD5, SHA-1, SHA-256, or SHA-512 hashes";
        public string Category => "Developer";
        public string IconGlyph => "\uE8D7";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "algorithm",
                DisplayName = "Algorithm",
                Type = ToolParameterType.Options,
                Options = new[] { "MD5", "SHA1", "SHA256", "SHA512" },
                DefaultValue = "SHA256"
            }
        };

        public HashCalculatorTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input text is required."));
            }

            string algo = context.GetParameter("algorithm", "SHA256") ?? "SHA256";
            string hash = _devUtils.ComputeHash(context.InputText, algo);
            return Task.FromResult(ToolResult.Ok(hash));
        }
    }

    public sealed class TimestampConverterTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-timestamp";
        public string DisplayName => "Timestamp Converter";
        public string Description => "Convert Unix epoch timestamps to dates and vice versa";
        public string Category => "Developer";
        public string IconGlyph => "\uE823";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "value",
                DisplayName = "Epoch or Date",
                Type = ToolParameterType.String
            }
        };

        public TimestampConverterTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            string? val = context.GetParameter<string>("value") ?? context.InputText;
            if (string.IsNullOrWhiteSpace(val))
            {
                var now = _devUtils.ConvertFromDateTime(DateTimeOffset.UtcNow);
                return Task.FromResult(ToolResult.Ok($"Now: {now.EpochSeconds}s | {now.LocalDateTimeFormatted}", now));
            }

            try
            {
                var res = _devUtils.ParseDateToTimestamp(val);
                return Task.FromResult(ToolResult.Ok($"{res.EpochSeconds}s\n{res.UtcIso8601}\n{res.LocalDateTimeFormatted}", res));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Fail(ex.Message));
            }
        }
    }

    public sealed class ColorConverterTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-color";
        public string DisplayName => "Color Picker & Converter";
        public string Description => "Convert between HEX, RGB, and HSL colors";
        public string Category => "Developer";
        public string IconGlyph => "\uE790";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "color",
                DisplayName = "Color String",
                Type = ToolParameterType.String,
                DefaultValue = "#00B4FF"
            }
        };

        public ColorConverterTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            string? val = context.GetParameter<string>("color") ?? context.InputText;
            if (string.IsNullOrWhiteSpace(val))
            {
                return Task.FromResult(ToolResult.Fail("Color string is required."));
            }

            var res = _devUtils.ConvertColor(val);
            if (!res.IsValid)
            {
                return Task.FromResult(ToolResult.Fail(res.ErrorMessage ?? "Invalid color format."));
            }

            string outStr = $"HEX: {res.Hex}\nRGB: {res.RgbString}\nHSL: {res.HslString}";
            return Task.FromResult(ToolResult.Ok(outStr, res));
        }
    }

    public sealed class QrGeneratorTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-qr";
        public string DisplayName => "QR Code Generator";
        public string Description => "Generate QR codes with SVG export and preview";
        public string Category => "Developer";
        public string IconGlyph => "\uED14";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => Array.Empty<ToolParameterDefinition>();

        public QrGeneratorTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(context.InputText))
            {
                return Task.FromResult(ToolResult.Fail("Input text is required to generate a QR code."));
            }

            var res = _devUtils.GenerateQr(context.InputText);
            return Task.FromResult(res.Success
                ? ToolResult.Ok(res.AsciiPreview, res)
                : ToolResult.Fail(res.ErrorMessage ?? "QR generation failed."));
        }
    }

    public sealed class UnitConverterTool : IDesktopTool
    {
        private readonly IDeveloperUtilitiesService _devUtils;
        public string Id => "tool-unit-convert";
        public string DisplayName => "Unit Converter";
        public string Description => "Convert Length, Mass, Temperature, and Digital Storage";
        public string Category => "Developer";
        public string IconGlyph => "\uE8EF";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "value",
                DisplayName = "Value",
                Type = ToolParameterType.Number,
                DefaultValue = 1.0
            },
            new ToolParameterDefinition
            {
                Key = "category",
                DisplayName = "Category",
                Type = ToolParameterType.Options,
                Options = new[] { "Length", "Mass", "Temperature", "Digital Storage" },
                DefaultValue = "Length"
            },
            new ToolParameterDefinition
            {
                Key = "from",
                DisplayName = "From Unit",
                Type = ToolParameterType.String,
                DefaultValue = "km"
            },
            new ToolParameterDefinition
            {
                Key = "to",
                DisplayName = "To Unit",
                Type = ToolParameterType.String,
                DefaultValue = "mi"
            }
        };

        public UnitConverterTool(IDeveloperUtilitiesService devUtils) => _devUtils = devUtils;

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            double val = context.GetParameter("value", 1.0);
            string cat = context.GetParameter("category", "Length") ?? "Length";
            string from = context.GetParameter("from", "km") ?? "km";
            string to = context.GetParameter("to", "mi") ?? "mi";

            var res = _devUtils.ConvertUnit(val, cat, from, to);
            return Task.FromResult(res.Success
                ? ToolResult.Ok(res.FormattedResult, res)
                : ToolResult.Fail(res.ErrorMessage ?? "Conversion failed."));
        }
    }

    #endregion

    #region File Tools

    public sealed class BatchRenameTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-rename";
        public string DisplayName => "Batch File Renamer";
        public string Description => "Rename multiple files using text replace, numbering, prefix/suffix";
        public string Category => "File Tools";
        public string IconGlyph => "\uE8AC";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "ruleType",
                DisplayName = "Rule Type",
                Type = ToolParameterType.Options,
                Options = Enum.GetNames<BatchRenameRuleType>(),
                DefaultValue = "ReplaceText"
            },
            new ToolParameterDefinition { Key = "find", DisplayName = "Find Text", Type = ToolParameterType.String },
            new ToolParameterDefinition { Key = "replace", DisplayName = "Replace With", Type = ToolParameterType.String },
            new ToolParameterDefinition { Key = "prefix", DisplayName = "Prefix", Type = ToolParameterType.String },
            new ToolParameterDefinition { Key = "suffix", DisplayName = "Suffix", Type = ToolParameterType.String }
        };

        public BatchRenameTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            if (context.InputFiles == null || context.InputFiles.Count == 0)
            {
                return ToolResult.Fail("No input files provided for batch rename.");
            }

            string ruleTypeStr = context.GetParameter("ruleType", "ReplaceText") ?? "ReplaceText";
            Enum.TryParse<BatchRenameRuleType>(ruleTypeStr, true, out var ruleType);

            var rule = new BatchRenameRule
            {
                Type = ruleType,
                FindText = context.GetParameter("find", string.Empty) ?? string.Empty,
                ReplaceWithText = context.GetParameter("replace", string.Empty) ?? string.Empty,
                Prefix = context.GetParameter("prefix", string.Empty) ?? string.Empty,
                Suffix = context.GetParameter("suffix", string.Empty) ?? string.Empty
            };

            var res = await _fileTools.ExecuteBatchRenameAsync(context.InputFiles, rule, overwriteExisting: false, cancellationToken);
            string summary = $"Renamed {res.RenamedCount} of {res.TotalRequested} file(s). Failed: {res.FailedCount}.";
            return ToolResult.Ok(summary, res);
        }
    }

    public sealed class ZipArchiveTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-zip";
        public string DisplayName => "ZIP Archiver";
        public string Description => "Compress files into ZIP archives or extract them";
        public string Category => "File Tools";
        public string IconGlyph => "\uF012";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "destination",
                DisplayName = "Destination Path",
                Type = ToolParameterType.FilePath,
                IsRequired = true
            }
        };

        public ZipArchiveTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            if (context.InputFiles == null || context.InputFiles.Count == 0)
            {
                return ToolResult.Fail("No source files specified for archive creation.");
            }

            string? dest = context.GetParameter<string>("destination");
            if (string.IsNullOrWhiteSpace(dest))
            {
                return ToolResult.Fail("Destination zip path is required.");
            }

            bool ok = await _fileTools.CreateZipArchiveAsync(context.InputFiles, dest, overwriteExisting: true, cancellationToken);
            return ok
                ? ToolResult.Ok($"Successfully created archive: {dest}", outputFiles: new[] { dest })
                : ToolResult.Fail("Failed to create ZIP archive.");
        }
    }

    public sealed class FolderSizeTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-folder-size";
        public string DisplayName => "Folder Size Analyzer";
        public string Description => "Analyze directory disk usage, file counts, and composition";
        public string Category => "File Tools";
        public string IconGlyph => "\uE7B8";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "path",
                DisplayName = "Directory Path",
                Type = ToolParameterType.DirectoryPath,
                IsRequired = true
            }
        };

        public FolderSizeTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            string? path = context.GetParameter<string>("path") ?? context.InputText;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return ToolResult.Fail("Valid directory path is required.");
            }

            var res = await _fileTools.AnalyzeFolderSizeAsync(path, cancellationToken);
            double mb = res.TotalSizeBytes / (1024.0 * 1024.0);
            string summary = $"{path}\nTotal Size: {mb:F2} MB ({res.TotalSizeBytes:N0} bytes)\nFiles: {res.FileCount:N0} | Subdirectories: {res.SubdirectoryCount:N0}";
            return ToolResult.Ok(summary, res);
        }
    }

    public sealed class FileSearchTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-search";
        public string DisplayName => "File Search";
        public string Description => "Fast wildcard file and document search with size filtering";
        public string Category => "File Tools";
        public string IconGlyph => "\uE721"; // Search glyph

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition { Key = "path", DisplayName = "Search Root Directory", Type = ToolParameterType.DirectoryPath, IsRequired = true },
            new ToolParameterDefinition { Key = "pattern", DisplayName = "Pattern (e.g. *.txt, *budget*)", Type = ToolParameterType.String, DefaultValue = "*" }
        };

        public FileSearchTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? path = context.GetParameter<string>("path") ?? context.InputText;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return ToolResult.Fail("Valid search root directory path is required.");
            }

            string pattern = context.GetParameter("pattern", "*") ?? "*";
            var results = await _fileTools.SearchFilesAsync(path, pattern, cancellationToken: cancellationToken);

            string summary = $"Found {results.Count} matching file(s) in {path}";
            var outputFiles = results.Select(r => r.FilePath).ToList();
            return ToolResult.Ok(summary, results, outputFiles);
        }
    }

    public sealed class FileHashTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-hash";
        public string DisplayName => "File Hash Calculator";
        public string Description => "Compute cryptographic checksum (SHA256, SHA512, MD5, SHA1) for files";
        public string Category => "File Tools";
        public string IconGlyph => "\uE774"; // Security/Hash glyph

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "algorithm",
                DisplayName = "Algorithm",
                Type = ToolParameterType.Options,
                Options = new[] { "SHA256", "MD5", "SHA1", "SHA512" },
                DefaultValue = "SHA256"
            }
        };

        public FileHashTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? file = context.InputFiles?.FirstOrDefault() ?? context.InputText;
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                return ToolResult.Fail("Valid target file path is required to compute checksum.");
            }

            string algo = context.GetParameter("algorithm", "SHA256") ?? "SHA256";
            var res = await _fileTools.ComputeFileHashAsync(file, algo, cancellationToken);
            string summary = $"Algorithm: {res.Algorithm}\nHash: {res.HashHex}\nSize: {res.FileSizeBytes:N0} bytes\nElapsed: {res.Elapsed.TotalMilliseconds:F1} ms";
            return ToolResult.Ok(summary, res);
        }
    }

    public sealed class FileConvertTool : IDesktopTool
    {
        private readonly IFileToolsService _fileTools;
        public string Id => "tool-file-convert";
        public string DisplayName => "File Format Converter";
        public string Description => "Convert text encoding, line endings, or CSV/JSON/TSV structured data";
        public string Category => "File Tools";
        public string IconGlyph => "\uE8A5"; // File convert glyph

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "type",
                DisplayName = "Conversion Type",
                Type = ToolParameterType.Options,
                Options = new[] { "CsvToJson", "JsonToCsv", "TextEncoding", "LineEndings", "CsvToTsv", "TsvToCsv" },
                DefaultValue = "CsvToJson"
            },
            new ToolParameterDefinition
            {
                Key = "encoding",
                DisplayName = "Target Encoding",
                Type = ToolParameterType.Options,
                Options = new[] { "UTF-8", "UTF-16", "ASCII", "ISO-8859-1" },
                DefaultValue = "UTF-8"
            }
        };

        public FileConvertTool(IFileToolsService fileTools) => _fileTools = fileTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? file = context.InputFiles?.FirstOrDefault() ?? context.InputText;
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                return ToolResult.Fail("Valid source file is required for conversion.");
            }

            string typeStr = context.GetParameter("type", "CsvToJson") ?? "CsvToJson";
            var convType = Enum.TryParse<FileConversionType>(typeStr, true, out var ct) ? ct : FileConversionType.CsvToJson;
            string targetEnc = context.GetParameter("encoding", "UTF-8") ?? "UTF-8";

            var options = new FileConvertOptions
            {
                ConversionType = convType,
                TargetEncoding = targetEnc,
                OverwriteExisting = false
            };

            var res = await _fileTools.ConvertFileAsync(file, options, cancellationToken);
            if (!res.Success)
            {
                return ToolResult.Fail(res.ErrorMessage ?? "File conversion failed.");
            }

            string summary = $"Converted: {Path.GetFileName(res.SourceFilePath)} -> {Path.GetFileName(res.OutputFilePath)}\nSaved: {res.ConvertedSizeBytes:N0} bytes (Original: {res.OriginalSizeBytes:N0} bytes)";
            return ToolResult.Ok(summary, res, new[] { res.OutputFilePath });
        }
    }

    #endregion

    #region Image Tools

    public sealed class ImageCompressTool : IDesktopTool
    {
        private readonly IImageToolsService _imageTools;
        public string Id => "tool-image-compress";
        public string DisplayName => "Image Compression";
        public string Description => "Compress images with quality settings and statistics";
        public string Category => "Image Tools";
        public string IconGlyph => "\uEB9F";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "quality",
                DisplayName = "Quality (1-100)",
                Type = ToolParameterType.Number,
                DefaultValue = 80
            },
            new ToolParameterDefinition
            {
                Key = "format",
                DisplayName = "Target Format",
                Type = ToolParameterType.Options,
                Options = Enum.GetNames<ImageFormatTarget>(),
                DefaultValue = "Jpeg"
            }
        };

        public ImageCompressTool(IImageToolsService imageTools) => _imageTools = imageTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            if (context.InputFiles == null || context.InputFiles.Count == 0)
            {
                return ToolResult.Fail("No image files specified for compression.");
            }

            int quality = context.GetParameter("quality", 80);
            string fmtStr = context.GetParameter("format", "Jpeg") ?? "Jpeg";
            Enum.TryParse<ImageFormatTarget>(fmtStr, true, out var fmt);

            var options = new ImageProcessOptions
            {
                QualityPercent = quality,
                TargetFormat = fmt
            };

            var report = await _imageTools.ProcessBatchAsync(context.InputFiles, options, progress: null, cancellationToken);
            string summary = $"Processed {report.SuccessCount} of {report.TotalCount} image(s).\nSaved: {report.TotalSavedBytes / 1024.0:F1} KB ({report.OverallSavingsPercentage:F1}%)";
            var outputFiles = report.Results.Where(r => r.Success && r.OutputFilePath != null).Select(r => r.OutputFilePath!).ToList();
            return ToolResult.Ok(summary, report, outputFiles);
        }
    }

    public sealed class ImageResizeTool : IDesktopTool
    {
        private readonly IImageToolsService _imageTools;
        public string Id => "tool-image-resize";
        public string DisplayName => "Image Resize & Crop";
        public string Description => "Resize, crop, rotate, or flip images";
        public string Category => "Image Tools";
        public string IconGlyph => "\uE790";

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition { Key = "width", DisplayName = "Target Width", Type = ToolParameterType.Number },
            new ToolParameterDefinition { Key = "height", DisplayName = "Target Height", Type = ToolParameterType.Number },
            new ToolParameterDefinition { Key = "maintainAspect", DisplayName = "Maintain Aspect Ratio", Type = ToolParameterType.Boolean, DefaultValue = true }
        };

        public ImageResizeTool(IImageToolsService imageTools) => _imageTools = imageTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            if (context.InputFiles == null || context.InputFiles.Count == 0)
            {
                return ToolResult.Fail("No image files specified for resize.");
            }

            uint? w = (uint?)context.GetParameter<int?>("width");
            uint? h = (uint?)context.GetParameter<int?>("height");
            bool aspect = context.GetParameter("maintainAspect", true);

            var options = new ImageProcessOptions
            {
                TargetWidth = w,
                TargetHeight = h,
                MaintainAspectRatio = aspect
            };

            var report = await _imageTools.ProcessBatchAsync(context.InputFiles, options, progress: null, cancellationToken);
            string summary = $"Resized {report.SuccessCount} of {report.TotalCount} image(s).";
            var outputFiles = report.Results.Where(r => r.Success && r.OutputFilePath != null).Select(r => r.OutputFilePath!).ToList();
            return ToolResult.Ok(summary, report, outputFiles);
        }
    }

    public sealed class ImageConvertTool : IDesktopTool
    {
        private readonly IImageToolsService _imageTools;
        public string Id => "tool-image-convert";
        public string DisplayName => "Image Format Converter";
        public string Description => "Convert images between JPEG, PNG, and WebP raster formats";
        public string Category => "Image Tools";
        public string IconGlyph => "\uEB9F"; // Photo convert glyph

        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => new[]
        {
            new ToolParameterDefinition
            {
                Key = "format",
                DisplayName = "Target Format",
                Type = ToolParameterType.Options,
                Options = new[] { "JPEG", "PNG", "WebP" },
                DefaultValue = "JPEG"
            },
            new ToolParameterDefinition
            {
                Key = "quality",
                DisplayName = "Quality (1-100)",
                Type = ToolParameterType.Number,
                DefaultValue = 85
            }
        };

        public ImageConvertTool(IImageToolsService imageTools) => _imageTools = imageTools;

        public async Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.InputFiles == null || context.InputFiles.Count == 0)
            {
                return ToolResult.Fail("No image files specified for conversion.");
            }

            string fmtStr = context.GetParameter("format", "JPEG") ?? "JPEG";
            var targetFormat = fmtStr.ToUpperInvariant() switch
            {
                "PNG" => ImageFormatTarget.Png,
                "WEBP" => ImageFormatTarget.WebP,
                _ => ImageFormatTarget.Jpeg
            };

            int quality = context.GetParameter("quality", 85);

            var options = new ImageProcessOptions
            {
                TargetFormat = targetFormat,
                QualityPercent = quality
            };

            var report = await _imageTools.ProcessBatchAsync(context.InputFiles, options, progress: null, cancellationToken);
            string summary = $"Converted {report.SuccessCount} of {report.TotalCount} image(s) to {targetFormat}.";
            var outputFiles = report.Results.Where(r => r.Success && r.OutputFilePath != null).Select(r => r.OutputFilePath!).ToList();
            return ToolResult.Ok(summary, report, outputFiles);
        }
    }

    #endregion
}
