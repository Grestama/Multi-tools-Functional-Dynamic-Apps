using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing batch renaming, archive management, folder size diagnostics,
/// file searching, cryptographic hashing, and folder favorites.
/// </summary>
public interface IFileToolsService : IDisposable
{
    IReadOnlyList<FavoriteFolderItem> FavoriteFolders { get; }
    IReadOnlyList<RecentFileItem> RecentFiles { get; }

    event EventHandler<IReadOnlyList<FavoriteFolderItem>>? FavoriteFoldersChanged;
    event EventHandler<IReadOnlyList<RecentFileItem>>? RecentFilesChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchRenameItemPreview>> PreviewBatchRenameAsync(
        IEnumerable<string> filePaths,
        BatchRenameRule rule,
        CancellationToken cancellationToken = default);

    Task<BatchRenameResult> ExecuteBatchRenameAsync(
        IEnumerable<string> filePaths,
        BatchRenameRule rule,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    Task<bool> CreateZipArchiveAsync(
        IEnumerable<string> sourcePaths,
        string destinationZipPath,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExtractZipArchiveAsync(
        string zipFilePath,
        string destinationDirectory,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    Task<FolderSizeResult> AnalyzeFolderSizeAsync(
        string folderPath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileSearchResult>> SearchFilesAsync(
        string rootPath,
        string? searchPattern = null,
        long? minSizeBytes = null,
        long? maxSizeBytes = null,
        CancellationToken cancellationToken = default);

    Task<FileHashResult> ComputeFileHashAsync(
        string filePath,
        string algorithm = "SHA256",
        CancellationToken cancellationToken = default);

    Task<FileConvertResult> ConvertFileAsync(
        string sourcePath,
        FileConvertOptions options,
        CancellationToken cancellationToken = default);

    void AddFavoriteFolder(string path, string? customName = null);
    bool RemoveFavoriteFolder(string id);
    void RecordRecentFile(string filePath);
    void ClearRecentFiles();
}
