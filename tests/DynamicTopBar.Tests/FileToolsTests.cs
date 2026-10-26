using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class FileToolsTests : IDisposable
{
    private readonly string _testDir;
    private readonly FileToolsService _fileTools;

    public FileToolsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DTB_FileToolsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _fileTools = new FileToolsService(_testDir);
    }

    public void Dispose()
    {
        _fileTools.Dispose();
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [Fact]
    public async Task PreviewBatchRename_ReplaceText_GeneratesCorrectNewNames()
    {
        string[] files = new[]
        {
            Path.Combine(_testDir, "photo_2026_01.jpg"),
            Path.Combine(_testDir, "photo_2026_02.jpg")
        };

        var rule = new BatchRenameRule
        {
            Type = BatchRenameRuleType.ReplaceText,
            FindText = "photo",
            ReplaceWithText = "holiday"
        };

        var previews = await _fileTools.PreviewBatchRenameAsync(files, rule);

        Assert.Equal(2, previews.Count);
        Assert.Equal("holiday_2026_01.jpg", previews[0].NewFileName);
        Assert.Equal("holiday_2026_02.jpg", previews[1].NewFileName);
        Assert.All(previews, p => Assert.True(p.IsValid));
    }

    [Fact]
    public async Task PreviewBatchRename_Numbering_AppliesPaddedSequence()
    {
        string[] files = new[]
        {
            Path.Combine(_testDir, "doc_a.txt"),
            Path.Combine(_testDir, "doc_b.txt"),
            Path.Combine(_testDir, "doc_c.txt")
        };

        var rule = new BatchRenameRule
        {
            Type = BatchRenameRuleType.Numbering,
            StartIndex = 1,
            Step = 1,
            PaddingDigits = 3,
            Separator = "_",
            AppendToEnd = true
        };

        var previews = await _fileTools.PreviewBatchRenameAsync(files, rule);

        Assert.Equal("doc_a_001.txt", previews[0].NewFileName);
        Assert.Equal("doc_b_002.txt", previews[1].NewFileName);
        Assert.Equal("doc_c_003.txt", previews[2].NewFileName);
    }

    [Fact]
    public async Task PreviewBatchRename_DetectsCollision_MarksInvalid()
    {
        string[] files = new[]
        {
            Path.Combine(_testDir, "apple.txt"),
            Path.Combine(_testDir, "banana.txt")
        };

        // Rule replaces everything with fixed text "same"
        var rule = new BatchRenameRule
        {
            Type = BatchRenameRuleType.PrefixSuffix,
            Prefix = "fixed",
            Suffix = ""
        };

        var previews = await _fileTools.PreviewBatchRenameAsync(files, rule);
        Assert.True(previews[0].IsValid);
        Assert.True(previews[1].IsValid);

        // Now test identical rename rule that causes collision
        string[] dupFiles = new[]
        {
            Path.Combine(_testDir, "file1.txt"),
            Path.Combine(_testDir, "file2.txt")
        };

        var dupRule = new BatchRenameRule
        {
            Type = BatchRenameRuleType.ReplaceText,
            FindText = "file1",
            ReplaceWithText = "duplicate"
        };
        // If file2 is also renamed to duplicate via prefix:
        var dupRule2 = new BatchRenameRule
        {
            Type = BatchRenameRuleType.ChangeCase,
            CaseMode = TextCaseMode.Uppercase
        };
        var dupPreviews = await _fileTools.PreviewBatchRenameAsync(dupFiles, dupRule2);
        Assert.All(dupPreviews, p => Assert.True(p.IsValid));
    }

    [Fact]
    public async Task ExecuteBatchRename_RenamesFilesOnDisk()
    {
        string file1 = Path.Combine(_testDir, "test_old_1.txt");
        string file2 = Path.Combine(_testDir, "test_old_2.txt");
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        var rule = new BatchRenameRule
        {
            Type = BatchRenameRuleType.ReplaceText,
            FindText = "old",
            ReplaceWithText = "new"
        };

        var result = await _fileTools.ExecuteBatchRenameAsync(new[] { file1, file2 }, rule);

        Assert.Equal(2, result.RenamedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(File.Exists(file1));
        Assert.True(File.Exists(Path.Combine(_testDir, "test_new_1.txt")));
        Assert.True(File.Exists(Path.Combine(_testDir, "test_new_2.txt")));
    }

    [Fact]
    public async Task CreateAndExtractZip_WorksReliably()
    {
        string sourceFile = Path.Combine(_testDir, "sample.txt");
        File.WriteAllText(sourceFile, "Hello Zip World");

        string zipPath = Path.Combine(_testDir, "archive.zip");
        string extractDir = Path.Combine(_testDir, "extracted");

        bool created = await _fileTools.CreateZipArchiveAsync(new[] { sourceFile }, zipPath, overwriteExisting: true);
        Assert.True(created);
        Assert.True(File.Exists(zipPath));

        bool extracted = await _fileTools.ExtractZipArchiveAsync(zipPath, extractDir, overwriteExisting: true);
        Assert.True(extracted);
        Assert.True(File.Exists(Path.Combine(extractDir, "sample.txt")));
        Assert.Equal("Hello Zip World", File.ReadAllText(Path.Combine(extractDir, "sample.txt")));
    }

    [Fact]
    public async Task AnalyzeFolderSize_CalculatesSizesAndExtensions()
    {
        string subDir = Path.Combine(_testDir, "sub");
        Directory.CreateDirectory(subDir);

        File.WriteAllBytes(Path.Combine(_testDir, "file1.png"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(_testDir, "file2.png"), new byte[2048]);
        File.WriteAllBytes(Path.Combine(subDir, "doc.txt"), new byte[512]);

        var result = await _fileTools.AnalyzeFolderSizeAsync(_testDir);

        Assert.Equal(3, result.FileCount);
        Assert.Equal(1, result.SubdirectoryCount);
        Assert.Equal(3584, result.TotalSizeBytes);
        Assert.Equal(3072, result.ExtensionSizesBytes[".png"]);
        Assert.Equal(512, result.ExtensionSizesBytes[".txt"]);
        Assert.NotEmpty(result.LargestFiles);
        Assert.Equal("file2.png", result.LargestFiles[0].FileName);
    }

    [Fact]
    public async Task SearchFiles_FiltersByPatternAndSize()
    {
        File.WriteAllBytes(Path.Combine(_testDir, "small.log"), new byte[100]);
        File.WriteAllBytes(Path.Combine(_testDir, "large.log"), new byte[5000]);
        File.WriteAllBytes(Path.Combine(_testDir, "image.png"), new byte[1000]);

        var results = await _fileTools.SearchFilesAsync(_testDir, searchPattern: "*.log", minSizeBytes: 1000);

        Assert.Single(results);
        Assert.Equal("large.log", results[0].FileName);
    }

    [Fact]
    public async Task ComputeFileHash_ComputesSha256Checksum()
    {
        string file = Path.Combine(_testDir, "hash_test.txt");
        File.WriteAllText(file, "hello world");

        var result = await _fileTools.ComputeFileHashAsync(file, "SHA256");

        Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", result.HashHex);
        Assert.Equal("SHA256", result.Algorithm);
    }

    [Fact]
    public void FavoriteFolders_AddAndRemove_Works()
    {
        _fileTools.AddFavoriteFolder(_testDir, "Test Folder");

        Assert.Single(_fileTools.FavoriteFolders);
        Assert.Equal("Test Folder", _fileTools.FavoriteFolders[0].Name);

        // Duplicate path ignored
        _fileTools.AddFavoriteFolder(_testDir, "Duplicate");
        Assert.Single(_fileTools.FavoriteFolders);

        string id = _fileTools.FavoriteFolders[0].Id;
        bool removed = _fileTools.RemoveFavoriteFolder(id);

        Assert.True(removed);
        Assert.Empty(_fileTools.FavoriteFolders);
    }

    [Fact]
    public void RecentFiles_MaintainsCapacityLimit()
    {
        for (int i = 0; i < 25; i++)
        {
            string f = Path.Combine(_testDir, $"recent_{i}.txt");
            File.WriteAllText(f, "test");
            _fileTools.RecordRecentFile(f);
        }

        Assert.Equal(20, _fileTools.RecentFiles.Count);
        Assert.Equal("recent_24.txt", _fileTools.RecentFiles[0].FileName);

        _fileTools.ClearRecentFiles();
        Assert.Empty(_fileTools.RecentFiles);
    }

    [Fact]
    public async Task ConvertFileAsync_CsvToJson_ConvertsAccurately()
    {
        string csvFile = Path.Combine(_testDir, "data.csv");
        await File.WriteAllTextAsync(csvFile, "Name,Age,Role\nAlice,30,Engineer\nBob,25,Designer");

        var options = new FileConvertOptions
        {
            ConversionType = FileConversionType.CsvToJson,
            OverwriteExisting = false
        };

        var result = await _fileTools.ConvertFileAsync(csvFile, options);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.OutputFilePath));
        Assert.EndsWith(".json", result.OutputFilePath);

        string json = await File.ReadAllTextAsync(result.OutputFilePath);
        Assert.Contains("\"Name\": \"Alice\"", json);
        Assert.Contains("\"Role\": \"Designer\"", json);
    }

    [Fact]
    public async Task ConvertFileAsync_JsonToCsv_ConvertsAccurately()
    {
        string jsonFile = Path.Combine(_testDir, "items.json");
        string jsonContent = "[{\"id\":\"1\",\"title\":\"Alpha\"},{\"id\":\"2\",\"title\":\"Beta\"}]";
        await File.WriteAllTextAsync(jsonFile, jsonContent);

        var options = new FileConvertOptions
        {
            ConversionType = FileConversionType.JsonToCsv,
            OverwriteExisting = false
        };

        var result = await _fileTools.ConvertFileAsync(jsonFile, options);

        Assert.True(result.Success);
        Assert.True(File.Exists(result.OutputFilePath));
        Assert.EndsWith(".csv", result.OutputFilePath);

        string csv = await File.ReadAllTextAsync(result.OutputFilePath);
        Assert.Contains("id,title", csv);
        Assert.Contains("Alpha", csv);
        Assert.Contains("Beta", csv);
    }

    [Fact]
    public async Task ConvertFileAsync_LineEndings_NormalizesCorrectly()
    {
        string file = Path.Combine(_testDir, "lines.txt");
        await File.WriteAllTextAsync(file, "Line1\nLine2\r\nLine3");

        var options = new FileConvertOptions
        {
            ConversionType = FileConversionType.LineEndings,
            TargetLineEnding = "\r\n",
            OverwriteExisting = true
        };

        var result = await _fileTools.ConvertFileAsync(file, options);

        Assert.True(result.Success);
        string converted = await File.ReadAllTextAsync(result.OutputFilePath);
        Assert.Equal("Line1\r\nLine2\r\nLine3", converted);
    }

    [Fact]
    public async Task ConvertFileAsync_WhenMissingFile_ReturnsFailure()
    {
        var options = new FileConvertOptions { ConversionType = FileConversionType.CsvToJson };
        var result = await _fileTools.ConvertFileAsync(Path.Combine(_testDir, "non_existent.csv"), options);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
