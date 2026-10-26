using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Cross-platform, thread-safe file operations service delivering batch renaming,
/// zip archiving, folder size analysis, search, and cryptographic hashing.
/// </summary>
public sealed class FileToolsService : IFileToolsService
{
    private readonly object _lock = new();
    private readonly string _favoritesFilePath;
    private readonly string _recentFilesFilePath;
    private readonly List<FavoriteFolderItem> _favorites = new();
    private readonly List<RecentFileItem> _recentFiles = new();
    private bool _isDisposed;

    public IReadOnlyList<FavoriteFolderItem> FavoriteFolders
    {
        get
        {
            lock (_lock) return _favorites.ToList();
        }
    }

    public IReadOnlyList<RecentFileItem> RecentFiles
    {
        get
        {
            lock (_lock) return _recentFiles.ToList();
        }
    }

    public event EventHandler<IReadOnlyList<FavoriteFolderItem>>? FavoriteFoldersChanged;
    public event EventHandler<IReadOnlyList<RecentFileItem>>? RecentFilesChanged;

    public FileToolsService(string? baseStorageDirectory = null)
    {
        string dir = baseStorageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar");

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _favoritesFilePath = Path.Combine(dir, "favorite_folders.json");
        _recentFilesFilePath = Path.Combine(dir, "recent_files.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadFavoritesAsync();
        await LoadRecentFilesAsync();
    }

    public Task<IReadOnlyList<BatchRenameItemPreview>> PreviewBatchRenameAsync(
        IEnumerable<string> filePaths,
        BatchRenameRule rule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(rule);

        var list = filePaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        var previews = new List<BatchRenameItemPreview>();
        var seenNewPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int index = rule.StartIndex;
        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (var originalPath in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string dir = Path.GetDirectoryName(originalPath) ?? string.Empty;
            string originalFileName = Path.GetFileName(originalPath);
            string baseName = Path.GetFileNameWithoutExtension(originalPath);
            string ext = Path.GetExtension(originalPath);

            string newBaseName = baseName;
            string newExt = ext;

            switch (rule.Type)
            {
                case BatchRenameRuleType.ReplaceText:
                    if (!string.IsNullOrEmpty(rule.FindText))
                    {
                        var comparison = rule.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                        newBaseName = baseName.Replace(rule.FindText, rule.ReplaceWithText ?? string.Empty, comparison);
                    }
                    break;

                case BatchRenameRuleType.PrefixSuffix:
                    newBaseName = $"{rule.Prefix ?? string.Empty}{baseName}{rule.Suffix ?? string.Empty}";
                    break;

                case BatchRenameRuleType.Numbering:
                    string numStr = index.ToString($"D{Math.Max(1, rule.PaddingDigits)}", CultureInfo.InvariantCulture);
                    newBaseName = rule.AppendToEnd
                        ? $"{baseName}{rule.Separator}{numStr}"
                        : $"{numStr}{rule.Separator}{baseName}";
                    index += rule.Step;
                    break;

                case BatchRenameRuleType.ChangeCase:
                    newBaseName = rule.CaseMode switch
                    {
                        TextCaseMode.Uppercase => baseName.ToUpperInvariant(),
                        TextCaseMode.Lowercase => baseName.ToLowerInvariant(),
                        TextCaseMode.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(baseName),
                        _ => baseName
                    };
                    break;

                case BatchRenameRuleType.ChangeExtension:
                    if (!string.IsNullOrWhiteSpace(rule.NewExtension))
                    {
                        newExt = rule.NewExtension.StartsWith('.') ? rule.NewExtension : "." + rule.NewExtension;
                    }
                    break;
            }

            string newFileName = newBaseName + newExt;
            string newPath = Path.Combine(dir, newFileName);

            bool isValid = true;
            string? errorMessage = null;

            if (string.IsNullOrWhiteSpace(newBaseName))
            {
                isValid = false;
                errorMessage = "Filename cannot be empty.";
            }
            else if (newFileName.IndexOfAny(invalidChars) >= 0)
            {
                isValid = false;
                errorMessage = "Filename contains invalid characters.";
            }
            else if (seenNewPaths.Contains(newPath))
            {
                isValid = false;
                errorMessage = "Collision: Multiple files would be renamed to the same name.";
            }

            seenNewPaths.Add(newPath);

            previews.Add(new BatchRenameItemPreview
            {
                OriginalPath = originalPath,
                OriginalFileName = originalFileName,
                NewFileName = newFileName,
                NewPath = newPath,
                IsValid = isValid,
                ErrorMessage = errorMessage
            });
        }

        return Task.FromResult<IReadOnlyList<BatchRenameItemPreview>>(previews);
    }

    public async Task<BatchRenameResult> ExecuteBatchRenameAsync(
        IEnumerable<string> filePaths,
        BatchRenameRule rule,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var previews = await PreviewBatchRenameAsync(filePaths, rule, cancellationToken);
        var finalItems = new List<BatchRenameItemPreview>();
        int renamedCount = 0;
        int failedCount = 0;

        foreach (var item in previews)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.IsValid)
            {
                finalItems.Add(item);
                failedCount++;
                continue;
            }

            if (string.Equals(item.OriginalPath, item.NewPath, StringComparison.OrdinalIgnoreCase))
            {
                // Unchanged
                finalItems.Add(item);
                continue;
            }

            try
            {
                if (!File.Exists(item.OriginalPath))
                {
                    finalItems.Add(item with { IsValid = false, ErrorMessage = "Source file does not exist." });
                    failedCount++;
                    continue;
                }

                if (File.Exists(item.NewPath) && !overwriteExisting)
                {
                    finalItems.Add(item with { IsValid = false, ErrorMessage = "Destination already exists and overwrite is disabled." });
                    failedCount++;
                    continue;
                }

                File.Move(item.OriginalPath, item.NewPath, overwriteExisting);
                finalItems.Add(item);
                renamedCount++;
            }
            catch (Exception ex)
            {
                finalItems.Add(item with { IsValid = false, ErrorMessage = ex.Message });
                failedCount++;
            }
        }

        return new BatchRenameResult
        {
            TotalRequested = previews.Count,
            RenamedCount = renamedCount,
            FailedCount = failedCount,
            Items = finalItems
        };
    }

    public async Task<bool> CreateZipArchiveAsync(
        IEnumerable<string> sourcePaths,
        string destinationZipPath,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        if (string.IsNullOrWhiteSpace(destinationZipPath))
        {
            throw new ArgumentException("Destination zip path cannot be empty.", nameof(destinationZipPath));
        }

        if (File.Exists(destinationZipPath))
        {
            if (!overwriteExisting) return false;
            File.Delete(destinationZipPath);
        }

        string destDir = Path.GetDirectoryName(destinationZipPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        await Task.Run(() =>
        {
            using var zipStream = File.Create(destinationZipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

            foreach (var path in sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(path))
                {
                    string entryName = Path.GetFileName(path);
                    archive.CreateEntryFromFile(path, entryName, CompressionLevel.Optimal);
                }
                else if (Directory.Exists(path))
                {
                    string rootDirName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    var dirFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                    foreach (var file in dirFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string relativePath = Path.GetRelativePath(path, file);
                        string entryName = Path.Combine(rootDirName, relativePath).Replace('\\', '/');
                        archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                    }
                }
            }
        }, cancellationToken);

        return true;
    }

    public async Task<bool> ExtractZipArchiveAsync(
        string zipFilePath,
        string destinationDirectory,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipFilePath)) return false;

        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory, overwriteExisting);
        }, cancellationToken);

        return true;
    }

    public async Task<FolderSizeResult> AnalyzeFolderSizeAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");
        }

        return await Task.Run(() =>
        {
            long totalSize = 0;
            int fileCount = 0;
            int dirCount = 0;
            var extSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var allFiles = new List<FileSearchResult>();

            var stack = new Stack<string>();
            stack.Push(folderPath);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string currentDir = stack.Pop();

                DirectoryInfo dirInfo;
                try
                {
                    dirInfo = new DirectoryInfo(currentDir);
                    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0 &&
                        !string.Equals(currentDir, folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip symbolic links and junctions to avoid infinite recursion
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                // Process files
                try
                {
                    foreach (var file in dirInfo.GetFiles())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        fileCount++;
                        long size = file.Length;
                        totalSize += size;

                        string ext = string.IsNullOrEmpty(file.Extension) ? "(no ext)" : file.Extension.ToLowerInvariant();
                        extSizes[ext] = extSizes.GetValueOrDefault(ext, 0) + size;

                        allFiles.Add(new FileSearchResult
                        {
                            FilePath = file.FullName,
                            FileName = file.Name,
                            Extension = ext,
                            SizeBytes = size,
                            LastModified = file.LastWriteTimeUtc
                        });
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }

                // Process subdirectories
                try
                {
                    foreach (var subDir in dirInfo.GetDirectories())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if ((subDir.Attributes & FileAttributes.ReparsePoint) != 0) continue;

                        dirCount++;
                        stack.Push(subDir.FullName);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
            }

            var largest = allFiles
                .OrderByDescending(f => f.SizeBytes)
                .Take(10)
                .ToList();

            return new FolderSizeResult
            {
                FolderPath = folderPath,
                TotalSizeBytes = totalSize,
                FileCount = fileCount,
                SubdirectoryCount = dirCount,
                ExtensionSizesBytes = extSizes,
                LargestFiles = largest
            };
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<FileSearchResult>> SearchFilesAsync(
        string rootPath,
        string? searchPattern = null,
        long? minSizeBytes = null,
        long? maxSizeBytes = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath)) return Array.Empty<FileSearchResult>();

        return await Task.Run(() =>
        {
            var results = new List<FileSearchResult>();
            string pattern = string.IsNullOrWhiteSpace(searchPattern) ? "*" : searchPattern;

            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string currentDir = stack.Pop();

                try
                {
                    var dirInfo = new DirectoryInfo(currentDir);
                    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0 &&
                        !string.Equals(currentDir, rootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var file in dirInfo.GetFiles(pattern))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        long size = file.Length;

                        if (minSizeBytes.HasValue && size < minSizeBytes.Value) continue;
                        if (maxSizeBytes.HasValue && size > maxSizeBytes.Value) continue;

                        results.Add(new FileSearchResult
                        {
                            FilePath = file.FullName,
                            FileName = file.Name,
                            Extension = file.Extension.ToLowerInvariant(),
                            SizeBytes = size,
                            LastModified = file.LastWriteTimeUtc
                        });
                    }

                    foreach (var sub in dirInfo.GetDirectories())
                    {
                        if ((sub.Attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            stack.Push(sub.FullName);
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
            }

            return (IReadOnlyList<FileSearchResult>)results;
        }, cancellationToken);
    }

    public async Task<FileHashResult> ComputeFileHashAsync(
        string filePath,
        string algorithm = "SHA256",
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found for hash computation.", filePath);
        }

        var sw = Stopwatch.StartNew();
        var fi = new FileInfo(filePath);

        using HashAlgorithm hasher = algorithm.ToUpperInvariant() switch
        {
            "MD5" => MD5.Create(),
            "SHA1" or "SHA-1" => SHA1.Create(),
            "SHA384" or "SHA-384" => SHA384.Create(),
            "SHA512" or "SHA-512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        byte[] buffer = new byte[65536];
        using var stream = File.OpenRead(filePath);

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
        }
        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        sw.Stop();
        byte[] hashBytes = hasher.Hash ?? Array.Empty<byte>();
        string hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new FileHashResult
        {
            FilePath = filePath,
            Algorithm = algorithm.ToUpperInvariant(),
            HashHex = hex,
            FileSizeBytes = fi.Length,
            Elapsed = sw.Elapsed
        };
    }

    public async Task<FileConvertResult> ConvertFileAsync(
        string sourcePath,
        FileConvertOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return new FileConvertResult
            {
                Success = false,
                SourceFilePath = sourcePath ?? string.Empty,
                ErrorMessage = $"Source file not found: {sourcePath}"
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var originalFi = new FileInfo(sourcePath);
            long originalSize = originalFi.Length;

            string baseDir = string.IsNullOrWhiteSpace(options.OutputDirectory)
                ? Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory
                : options.OutputDirectory;

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            string originalBaseName = Path.GetFileNameWithoutExtension(sourcePath);
            string originalExt = Path.GetExtension(sourcePath);

            string targetExt = options.ConversionType switch
            {
                FileConversionType.CsvToJson => ".json",
                FileConversionType.JsonToCsv => ".csv",
                FileConversionType.CsvToTsv => ".tsv",
                FileConversionType.TsvToCsv => ".csv",
                _ => originalExt
            };

            string outputFileName = !string.IsNullOrWhiteSpace(options.OutputFileName)
                ? options.OutputFileName
                : $"{originalBaseName}_converted{targetExt}";

            string outputPath = Path.Combine(baseDir, outputFileName);

            // Prevent unintended overwrites
            if (File.Exists(outputPath) && !options.OverwriteExisting)
            {
                int counter = 1;
                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(baseDir, $"{originalBaseName}_converted_{counter}{targetExt}");
                    counter++;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            switch (options.ConversionType)
            {
                case FileConversionType.TextEncoding:
                    {
                        var targetEncoding = ResolveEncoding(options.TargetEncoding, options.IncludeBom);
                        string text = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                        await File.WriteAllTextAsync(outputPath, text, targetEncoding, cancellationToken);
                        break;
                    }

                case FileConversionType.LineEndings:
                    {
                        string text = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                        string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
                        string targetEnding = string.Equals(options.TargetLineEnding, "\n", StringComparison.Ordinal) ? "\n" : "\r\n";
                        string converted = normalized.Replace("\n", targetEnding);
                        await File.WriteAllTextAsync(outputPath, converted, cancellationToken);
                        break;
                    }

                case FileConversionType.CsvToJson:
                    {
                        string[] lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
                        if (lines.Length == 0)
                        {
                            await File.WriteAllTextAsync(outputPath, "[]", cancellationToken);
                            break;
                        }

                        var headers = ParseDelimitedLine(lines[0], ',');
                        var list = new List<Dictionary<string, string>>();

                        for (int i = 1; i < lines.Length; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (string.IsNullOrWhiteSpace(lines[i])) continue;

                            var cols = ParseDelimitedLine(lines[i], ',');
                            var dict = new Dictionary<string, string>();
                            for (int h = 0; h < headers.Count; h++)
                            {
                                dict[headers[h]] = h < cols.Count ? cols[h] : string.Empty;
                            }
                            list.Add(dict);
                        }

                        string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
                        break;
                    }

                case FileConversionType.JsonToCsv:
                    {
                        string json = await File.ReadAllTextAsync(sourcePath, cancellationToken);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        {
                            return new FileConvertResult
                            {
                                Success = false,
                                SourceFilePath = sourcePath,
                                ErrorMessage = "JSON root must be an array of objects to convert to CSV."
                            };
                        }

                        var allKeys = new HashSet<string>();
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            if (elem.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in elem.EnumerateObject())
                                {
                                    allKeys.Add(prop.Name);
                                }
                            }
                        }

                        var keyList = allKeys.ToList();
                        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                        await writer.WriteLineAsync(string.Join(",", keyList.Select(EscapeCsvField)));

                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (elem.ValueKind == JsonValueKind.Object)
                            {
                                var row = new List<string>();
                                foreach (var k in keyList)
                                {
                                    string val = elem.TryGetProperty(k, out var v) ? v.ToString() : string.Empty;
                                    row.Add(EscapeCsvField(val));
                                }
                                await writer.WriteLineAsync(string.Join(",", row));
                            }
                        }
                        break;
                    }

                case FileConversionType.CsvToTsv:
                    {
                        string[] lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
                        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                        foreach (var line in lines)
                        {
                            var cols = ParseDelimitedLine(line, ',');
                            await writer.WriteLineAsync(string.Join("\t", cols));
                        }
                        break;
                    }

                case FileConversionType.TsvToCsv:
                    {
                        string[] lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
                        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                        foreach (var line in lines)
                        {
                            var cols = line.Split('\t');
                            await writer.WriteLineAsync(string.Join(",", cols.Select(EscapeCsvField)));
                        }
                        break;
                    }
            }

            long convertedSize = new FileInfo(outputPath).Length;
            return new FileConvertResult
            {
                Success = true,
                SourceFilePath = sourcePath,
                OutputFilePath = outputPath,
                OriginalSizeBytes = originalSize,
                ConvertedSizeBytes = convertedSize
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            return new FileConvertResult
            {
                Success = false,
                SourceFilePath = sourcePath,
                ErrorMessage = $"Access denied: {ex.Message}"
            };
        }
        catch (IOException ex)
        {
            return new FileConvertResult
            {
                Success = false,
                SourceFilePath = sourcePath,
                ErrorMessage = $"File I/O error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new FileConvertResult
            {
                Success = false,
                SourceFilePath = sourcePath,
                ErrorMessage = ex.Message
            };
        }
    }

    private static System.Text.Encoding ResolveEncoding(string encodingName, bool includeBom)
    {
        return encodingName.ToUpperInvariant() switch
        {
            "UTF-8" or "UTF8" => new System.Text.UTF8Encoding(includeBom),
            "UTF-16" or "UTF16" or "UNICODE" => new System.Text.UnicodeEncoding(false, includeBom),
            "ASCII" => System.Text.Encoding.ASCII,
            "LATIN1" or "ISO-8859-1" => System.Text.Encoding.Latin1,
            _ => new System.Text.UTF8Encoding(includeBom)
        };
    }

    private static List<string> ParseDelimitedLine(string line, char delimiter)
    {
        var result = new List<string>();
        if (line == null) return result;

        bool inQuotes = false;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString().Trim());
        return result;
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public void AddFavoriteFolder(string path, string? customName = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        string normalized = Path.GetFullPath(path);
        string name = string.IsNullOrWhiteSpace(customName)
            ? Path.GetFileName(normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : customName;

        if (string.IsNullOrEmpty(name)) name = normalized;

        lock (_lock)
        {
            if (_favorites.Any(f => string.Equals(f.Path, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _favorites.Add(new FavoriteFolderItem
            {
                Name = name,
                Path = normalized
            });
        }

        SaveFavorites();
        FavoriteFoldersChanged?.Invoke(this, FavoriteFolders);
    }

    public bool RemoveFavoriteFolder(string id)
    {
        bool removed;
        lock (_lock)
        {
            removed = _favorites.RemoveAll(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        if (removed)
        {
            SaveFavorites();
            FavoriteFoldersChanged?.Invoke(this, FavoriteFolders);
        }

        return removed;
    }

    public void RecordRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        string normalized = Path.GetFullPath(filePath);
        var fi = new FileInfo(normalized);

        lock (_lock)
        {
            _recentFiles.RemoveAll(r => string.Equals(r.FilePath, normalized, StringComparison.OrdinalIgnoreCase));
            _recentFiles.Insert(0, new RecentFileItem
            {
                FilePath = normalized,
                FileName = fi.Name,
                SizeBytes = fi.Length,
                AccessedAt = DateTimeOffset.UtcNow
            });

            // Keep capacity at 20
            while (_recentFiles.Count > 20)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }
        }

        SaveRecentFiles();
        RecentFilesChanged?.Invoke(this, RecentFiles);
    }

    public void ClearRecentFiles()
    {
        lock (_lock)
        {
            _recentFiles.Clear();
        }

        SaveRecentFiles();
        RecentFilesChanged?.Invoke(this, RecentFiles);
    }

    private void SaveFavorites()
    {
        try
        {
            List<FavoriteFolderItem> copy;
            lock (_lock) copy = _favorites.ToList();

            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{_favoritesFilePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _favoritesFilePath, overwrite: true);
        }
        catch { }
    }

    private Task LoadFavoritesAsync()
    {
        if (!File.Exists(_favoritesFilePath)) return Task.CompletedTask;

        try
        {
            string json = File.ReadAllText(_favoritesFilePath);
            var list = JsonSerializer.Deserialize<List<FavoriteFolderItem>>(json);
            if (list != null)
            {
                lock (_lock)
                {
                    _favorites.Clear();
                    _favorites.AddRange(list);
                }
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    private void SaveRecentFiles()
    {
        try
        {
            List<RecentFileItem> copy;
            lock (_lock) copy = _recentFiles.ToList();

            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{_recentFilesFilePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _recentFilesFilePath, overwrite: true);
        }
        catch { }
    }

    private Task LoadRecentFilesAsync()
    {
        if (!File.Exists(_recentFilesFilePath)) return Task.CompletedTask;

        try
        {
            string json = File.ReadAllText(_recentFilesFilePath);
            var list = JsonSerializer.Deserialize<List<RecentFileItem>>(json);
            if (list != null)
            {
                lock (_lock)
                {
                    _recentFiles.Clear();
                    _recentFiles.AddRange(list);
                }
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
