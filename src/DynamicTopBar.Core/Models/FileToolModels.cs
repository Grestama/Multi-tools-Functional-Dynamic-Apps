using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum BatchRenameRuleType
{
    ReplaceText,
    PrefixSuffix,
    Numbering,
    ChangeCase,
    ChangeExtension
}

public enum TextCaseMode
{
    Uppercase,
    Lowercase,
    TitleCase
}

/// <summary>
/// Rule configuration for batch file renaming.
/// </summary>
public record BatchRenameRule
{
    public BatchRenameRuleType Type { get; init; } = BatchRenameRuleType.ReplaceText;

    // ReplaceText
    public string FindText { get; init; } = string.Empty;
    public string ReplaceWithText { get; init; } = string.Empty;
    public bool MatchCase { get; init; } = false;

    // PrefixSuffix
    public string Prefix { get; init; } = string.Empty;
    public string Suffix { get; init; } = string.Empty;

    // Numbering
    public int StartIndex { get; init; } = 1;
    public int Step { get; init; } = 1;
    public int PaddingDigits { get; init; } = 3;
    public string Separator { get; init; } = "_";
    public bool AppendToEnd { get; init; } = true;

    // ChangeCase
    public TextCaseMode CaseMode { get; init; } = TextCaseMode.Lowercase;

    // ChangeExtension
    public string NewExtension { get; init; } = string.Empty;
}

/// <summary>
/// Before/after simulation of a single file rename operation.
/// </summary>
public record BatchRenameItemPreview
{
    public required string OriginalPath { get; init; }
    public required string OriginalFileName { get; init; }
    public required string NewFileName { get; init; }
    public required string NewPath { get; init; }
    public bool IsValid { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Summary report of executed batch rename.
/// </summary>
public record BatchRenameResult
{
    public int TotalRequested { get; init; }
    public int RenamedCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<BatchRenameItemPreview> Items { get; init; } = Array.Empty<BatchRenameItemPreview>();
}

/// <summary>
/// Telemetry breakdown of folder size and content composition.
/// </summary>
public record FolderSizeResult
{
    public required string FolderPath { get; init; }
    public long TotalSizeBytes { get; init; }
    public int FileCount { get; init; }
    public int SubdirectoryCount { get; init; }
    public Dictionary<string, long> ExtensionSizesBytes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<FileSearchResult> LargestFiles { get; init; } = Array.Empty<FileSearchResult>();
}

/// <summary>
/// Search hit descriptor for file queries.
/// </summary>
public record FileSearchResult
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public string Extension { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTimeOffset LastModified { get; init; }
}

/// <summary>
/// Computed cryptographic or integrity checksum of a file.
/// </summary>
public record FileHashResult
{
    public required string FilePath { get; init; }
    public required string Algorithm { get; init; }
    public required string HashHex { get; init; }
    public long FileSizeBytes { get; init; }
    public TimeSpan Elapsed { get; init; }
}

/// <summary>
/// User pinned favorite directory shortcut.
/// </summary>
public record FavoriteFolderItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string IconGlyph { get; init; } = "\uE8B7"; // Folder glyph
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Opt-in recently accessed file item.
/// </summary>
public record RecentFileItem
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset AccessedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum FileConversionType
{
    TextEncoding,
    LineEndings,
    CsvToJson,
    JsonToCsv,
    CsvToTsv,
    TsvToCsv
}

/// <summary>
/// Options for non-destructive file format and encoding conversion.
/// </summary>
public record FileConvertOptions
{
    public FileConversionType ConversionType { get; init; } = FileConversionType.TextEncoding;
    public string TargetEncoding { get; init; } = "UTF-8";
    public bool IncludeBom { get; init; }
    public string TargetLineEnding { get; init; } = "\r\n"; // CRLF or LF
    public string? OutputDirectory { get; init; }
    public string? OutputFileName { get; init; }
    public bool OverwriteExisting { get; init; }
}

/// <summary>
/// Result report of file format conversion.
/// </summary>
public record FileConvertResult
{
    public bool Success { get; init; }
    public string SourceFilePath { get; init; } = string.Empty;
    public string OutputFilePath { get; init; } = string.Empty;
    public long OriginalSizeBytes { get; init; }
    public long ConvertedSizeBytes { get; init; }
    public string? ErrorMessage { get; init; }
}
