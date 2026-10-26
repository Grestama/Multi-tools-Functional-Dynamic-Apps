using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Local, privacy-preserving AI assistant intent parser and execution mediator.
/// Transforms natural language requests into verified tool invocations with strict permission gates.
/// </summary>
public sealed class AiAssistantService : IAiAssistantService
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILoggingService _logger;
    private bool _isDisposed;

    public bool IsEnabled { get; set; } = false; // Strict opt-in default
    public bool LocalProcessingOnly { get; set; } = true;

    public AiAssistantService(IToolRegistry toolRegistry, ILoggingService logger)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<AiIntentPlan?> PlanIntentAsync(string naturalLanguageQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageQuery))
        {
            return Task.FromResult<AiIntentPlan?>(null);
        }

        string q = naturalLanguageQuery.Trim();
        string lower = q.ToLowerInvariant();

        // 1. JSON Formatter
        if (lower.Contains("format json") || lower.Contains("prettify json") || lower.Contains("validate json") || lower.Contains("minify json"))
        {
            bool minify = lower.Contains("minify");
            string payload = ExtractPayloadAfterKeywords(q, "json", "format", "prettify", "minify", "validate");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-json-format",
                IntentDescription = minify ? "Minify JSON payload" : "Format and validate JSON payload",
                ExtractedInputText = payload,
                ExtractedParameters = { ["mode"] = minify ? "Minify" : "Prettify" }
            });
        }

        // 2. Base64
        if (lower.Contains("base64"))
        {
            bool decode = lower.Contains("decode");
            string payload = ExtractPayloadAfterKeywords(q, "base64", "encode", "decode", "to", "from");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-base64",
                IntentDescription = decode ? "Decode Base64 string" : "Encode text to Base64",
                ExtractedInputText = payload,
                ExtractedParameters = { ["operation"] = decode ? "Decode" : "Encode" }
            });
        }

        // 3. URL Codec
        if (lower.Contains("url encode") || lower.Contains("encode url") || lower.Contains("url decode") || lower.Contains("decode url"))
        {
            bool decode = lower.Contains("decode");
            string payload = ExtractPayloadAfterKeywords(q, "url", "encode", "decode");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-url-codec",
                IntentDescription = decode ? "Decode URL-encoded string" : "Encode string for URL query parameter",
                ExtractedInputText = payload,
                ExtractedParameters = { ["operation"] = decode ? "Decode" : "Encode" }
            });
        }

        // 4. UUID Generator
        if (lower.Contains("uuid") || lower.Contains("guid"))
        {
            int count = 1;
            var match = Regex.Match(lower, @"\b(\d+)\s+(uuid|guid)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int c)) count = Math.Clamp(c, 1, 50);

            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-uuid",
                IntentDescription = $"Generate {count} unique UUID identifier(s)",
                ExtractedParameters = { ["count"] = count }
            });
        }

        // 5. Cryptographic Hash
        if (lower.Contains("sha256") || lower.Contains("md5") || lower.Contains("sha512") || (lower.Contains("hash") && !lower.Contains("file")))
        {
            string algo = "SHA256";
            if (lower.Contains("md5")) algo = "MD5";
            else if (lower.Contains("sha512")) algo = "SHA512";
            else if (lower.Contains("sha1")) algo = "SHA1";

            string payload = ExtractPayloadAfterKeywords(q, "sha256", "md5", "sha512", "sha1", "hash", "of", "for");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-hash",
                IntentDescription = $"Compute {algo} text checksum",
                ExtractedInputText = payload,
                ExtractedParameters = { ["algorithm"] = algo }
            });
        }

        // 6. QR Code
        if (lower.Contains("qr code") || lower.Contains("qr") || lower.Contains("qrcode"))
        {
            string payload = ExtractPayloadAfterKeywords(q, "qr", "code", "for", "make", "generate");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-qr",
                IntentDescription = "Generate vector QR code matrix",
                ExtractedInputText = payload,
                ExtractedParameters = { ["format"] = "SVG" }
            });
        }

        // 7. Unit Converter
        if (lower.Contains("convert") && (lower.Contains("to") || lower.Contains("in")))
        {
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-unit-converter",
                IntentDescription = "Convert physical or digital measurement unit",
                ExtractedInputText = q
            });
        }

        // 8. Color Converter
        if (lower.Contains("color") || lower.Contains("#") || lower.Contains("rgb") || lower.Contains("hsl"))
        {
            var hexMatch = Regex.Match(q, @"#([0-9a-fA-F]{3,8})");
            string colorInput = hexMatch.Success ? hexMatch.Value : q;
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-color",
                IntentDescription = "Convert color format across HEX, RGB, and HSL",
                ExtractedInputText = colorInput
            });
        }

        // 9. Timestamp
        if (lower.Contains("timestamp") || lower.Contains("epoch") || lower.Contains("unix time"))
        {
            string payload = ExtractPayloadAfterKeywords(q, "timestamp", "epoch", "convert", "of");
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-timestamp",
                IntentDescription = "Convert timestamp between Unix epoch and ISO 8601",
                ExtractedInputText = payload
            });
        }

        // 10. Folder Size
        if (lower.Contains("folder size") || lower.Contains("directory size") || lower.Contains("analyze folder"))
        {
            string path = ExtractPathFromQuery(q);
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-file-folder-size",
                IntentDescription = $"Analyze directory disk usage for: {path}",
                ExtractedInputText = path,
                ExtractedParameters = { ["path"] = path }
            });
        }

        // 11. File Search
        if (lower.Contains("search files") || lower.Contains("find files") || lower.Contains("search in"))
        {
            string path = ExtractPathFromQuery(q);
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-file-search",
                IntentDescription = $"Search files in directory {path}",
                ExtractedParameters = { ["path"] = path, ["pattern"] = "*" }
            });
        }

        // 12. Batch Rename (Destructive gate)
        if (lower.Contains("rename files") || lower.Contains("batch rename"))
        {
            string path = ExtractPathFromQuery(q);
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-file-rename",
                IntentDescription = $"Batch rename files in {path}",
                ExtractedInputText = path,
                RequiresConfirmation = true,
                ConfirmationPrompt = $"Are you sure you want to perform a batch rename operation on directory: '{path}'?"
            });
        }

        // 13. File Conversion (Destructive gate)
        if (lower.Contains("convert file") || lower.Contains("csv to json") || lower.Contains("json to csv"))
        {
            string path = ExtractPathFromQuery(q);
            return Task.FromResult<AiIntentPlan?>(new AiIntentPlan
            {
                UserQuery = q,
                TargetToolId = "tool-file-convert",
                IntentDescription = $"Convert file format: {path}",
                ExtractedInputText = path,
                RequiresConfirmation = true,
                ConfirmationPrompt = $"Proceed with converting file: '{path}'?"
            });
        }

        return Task.FromResult<AiIntentPlan?>(null);
    }

    public async Task<AiExecutionResult> ProcessQueryAsync(
        string naturalLanguageQuery,
        Func<AiIntentPlan, Task<bool>>? userConfirmationCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new AiExecutionResult
            {
                Success = false,
                Query = naturalLanguageQuery,
                ErrorMessage = "AI Assistant is disabled. Enable it in Settings to process natural language requests.",
                AssistantResponse = "AI Assistant is disabled by default for privacy (opt-in). Enable it in Settings to process natural language requests."
            };
        }

        _logger.LogInfo($"AI Assistant processing query: '{naturalLanguageQuery}'", "AiAssistant");

        var plan = await PlanIntentAsync(naturalLanguageQuery, cancellationToken);
        if (plan == null)
        {
            return new AiExecutionResult
            {
                Success = false,
                Query = naturalLanguageQuery,
                AssistantResponse = "I could not resolve your query into a registered tool. Try typing the exact tool name in Command Palette (Ctrl+Shift+P)."
            };
        }

        // Verify target tool exists
        var tool = _toolRegistry.GetTool(plan.TargetToolId);
        if (tool == null)
        {
            return new AiExecutionResult
            {
                Success = false,
                Query = naturalLanguageQuery,
                Plan = plan,
                ErrorMessage = $"Mapped tool '{plan.TargetToolId}' is not installed in the Tool Registry."
            };
        }

        // Destructive / Risk Confirmation Gate
        if (plan.RequiresConfirmation && userConfirmationCallback != null)
        {
            bool approved = await userConfirmationCallback(plan);
            if (!approved)
            {
                _logger.LogWarning($"AI execution cancelled: User rejected confirmation for {plan.TargetToolId}", null, "AiAssistant");
                return new AiExecutionResult
                {
                    Success = false,
                    Query = naturalLanguageQuery,
                    Plan = plan,
                    AssistantResponse = "Operation cancelled. No changes were made."
                };
            }
        }

        var context = new ToolContext
        {
            InputText = plan.ExtractedInputText,
            Parameters = plan.ExtractedParameters
        };

        ToolResult toolResult = await _toolRegistry.ExecuteToolAsync(plan.TargetToolId, context, cancellationToken);

        string response = toolResult.Success
            ? $"Executed {tool.DisplayName}:\n{toolResult.OutputText}"
            : $"Failed to execute {tool.DisplayName}: {toolResult.ErrorMessage}";

        return new AiExecutionResult
        {
            Success = toolResult.Success,
            Query = naturalLanguageQuery,
            Plan = plan,
            ToolResult = toolResult,
            AssistantResponse = response,
            ErrorMessage = toolResult.ErrorMessage
        };
    }

    private static string ExtractPayloadAfterKeywords(string input, params string[] keywords)
    {
        string text = input;
        foreach (var kw in keywords)
        {
            int idx = text.IndexOf(kw, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                text = text.Substring(idx + kw.Length).Trim(' ', ':', '"', '\'');
            }
        }
        return text.Trim();
    }

    private static string ExtractPathFromQuery(string query)
    {
        var pathMatch = Regex.Match(query, @"([a-zA-Z]:\\[^\s""]+|/[^\s""]+)");
        return pathMatch.Success ? pathMatch.Value : query;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
