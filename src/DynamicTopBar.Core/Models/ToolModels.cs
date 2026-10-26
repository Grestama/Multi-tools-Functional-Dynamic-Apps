using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicTopBar.Core.Models;

public enum ToolParameterType
{
    String,
    Number,
    Boolean,
    FilePath,
    DirectoryPath,
    Options
}

/// <summary>
/// Definition of a configurable parameter for a desktop tool.
/// </summary>
public record ToolParameterDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = string.Empty;
    public ToolParameterType Type { get; init; } = ToolParameterType.String;
    public bool IsRequired { get; init; }
    public object? DefaultValue { get; init; }
    public IReadOnlyList<string>? Options { get; init; }
}

/// <summary>
/// Execution context supplied to a desktop tool.
/// </summary>
public record ToolContext
{
    public Dictionary<string, object?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? InputText { get; init; }
    public IReadOnlyList<string>? InputFiles { get; init; }

    public T? GetParameter<T>(string key, T? defaultValue = default)
    {
        if (Parameters.TryGetValue(key, out var val))
        {
            if (val is T typed) return typed;
            if (val != null)
            {
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    // Fall back
                }
            }
        }
        return defaultValue;
    }
}

/// <summary>
/// Result of executing a desktop tool.
/// </summary>
public record ToolResult
{
    public bool Success { get; init; }
    public string? OutputText { get; init; }
    public IReadOnlyList<string>? OutputFiles { get; init; }
    public object? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ToolResult Ok(string? outputText = null, object? data = null, IReadOnlyList<string>? outputFiles = null) =>
        new()
        {
            Success = true,
            OutputText = outputText,
            Data = data,
            OutputFiles = outputFiles
        };

    public static ToolResult Fail(string errorMessage) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Contract representing an extensible desktop utility registered with the Universal Tools Engine.
/// </summary>
public interface IDesktopTool
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category { get; }
    string IconGlyph { get; }
    IReadOnlyList<ToolParameterDefinition> ParameterDefinitions { get; }

    Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default);
}
