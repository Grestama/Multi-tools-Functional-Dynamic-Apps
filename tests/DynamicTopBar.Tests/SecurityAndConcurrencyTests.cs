using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class SecurityAndConcurrencyTests : IDisposable
{
    private readonly string _testDir;

    public SecurityAndConcurrencyTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DTB_SecTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    private class MockPrefixDataProtector : IDataProtector
    {
        private const string Prefix = "MOCK_ENC:";

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            return Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
        }

        public string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            if (!cipherText.StartsWith(Prefix)) return cipherText; // Plaintext backward compatibility!
            byte[] bytes = Convert.FromBase64String(cipherText.Substring(Prefix.Length));
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }

    [Fact]
    public void NoOpDataProtector_ReturnsInputUnchanged()
    {
        var protector = NoOpDataProtector.Instance;
        string text = "Confidential User Data";
        Assert.Equal(text, protector.Protect(text));
        Assert.Equal(text, protector.Unprotect(text));
    }

    [Fact]
    public async Task FileNotesRepository_ProtectsData_AndMaintainsBackwardCompatibility()
    {
        string filePath = Path.Combine(_testDir, "notes_protected.json");
        var protector = new MockPrefixDataProtector();
        var repo = new FileNotesRepository(filePath, protector);

        var originalNotes = new List<NoteItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Secret Note", Content = "Sensitive passwords" }
        };

        // 1. Save protected
        await repo.SaveAllAsync(originalNotes);

        string rawDiskContent = await File.ReadAllTextAsync(filePath);
        Assert.StartsWith("MOCK_ENC:", rawDiskContent);

        // 2. Read back decrypted
        var loaded = await repo.GetAllAsync();
        Assert.Single(loaded);
        Assert.Equal("Secret Note", loaded[0].Title);
        Assert.Equal("Sensitive passwords", loaded[0].Content);

        // 3. Test backward compatibility with legacy plaintext JSON file
        string plainJson = JsonSerializer.Serialize(originalNotes);
        await File.WriteAllTextAsync(filePath, plainJson);

        var loadedLegacy = await repo.GetAllAsync();
        Assert.Single(loadedLegacy);
        Assert.Equal("Secret Note", loadedLegacy[0].Title);
    }

    [Fact]
    public async Task FileNotesRepository_HandlesConcurrentSavesAndReadsWithoutLockingCrash()
    {
        string filePath = Path.Combine(_testDir, "notes_concurrent.json");
        var repo = new FileNotesRepository(filePath);

        var tasks = Enumerable.Range(0, 15).Select(async i =>
        {
            var notes = new List<NoteItem>
            {
                new() { Id = Guid.NewGuid(), Title = $"Note {i}", Content = $"Content {i}" }
            };
            await repo.SaveAllAsync(notes);
            var read = await repo.GetAllAsync();
            Assert.NotNull(read);
        });

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task FileTodoRepository_HandlesConcurrentSavesAndReadsWithoutLockingCrash()
    {
        string filePath = Path.Combine(_testDir, "todos_concurrent.json");
        var repo = new FileTodoRepository(filePath);

        var tasks = Enumerable.Range(0, 15).Select(async i =>
        {
            var todos = new List<TodoItem>
            {
                new() { Id = Guid.NewGuid(), Title = $"Task {i}", IsCompleted = i % 2 == 0 }
            };
            await repo.SaveAllAsync(todos);
            var read = await repo.GetAllAsync();
            Assert.NotNull(read);
        });

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ClipboardService_WithDataProtector_ProtectsPayloadAndPreservesLegacyPlaintext()
    {
        string clipPath = Path.Combine(_testDir, "clipboard_enc.json");
        var protector = new MockPrefixDataProtector();
        var service = new ClipboardService(persistencePath: clipPath, dataProtector: protector);

        await service.InitializeAsync();
        service.SetOptIn(true);
        service.AddSnippet("Confidential API Key: sk_test_1234567890abcdef");

        // Small delay to ensure disk write
        await Task.Delay(50);

        string fileContent = await File.ReadAllTextAsync(clipPath);
        Assert.StartsWith("MOCK_ENC:", fileContent);

        // Test backward-compatible reload of plain JSON
        var legacySnippets = new List<ClipboardSnippet>
        {
            new() { Id = Guid.NewGuid(), Text = "Legacy Plaintext Snippet" }
        };
        await File.WriteAllTextAsync(clipPath, JsonSerializer.Serialize(legacySnippets));

        var legacyService = new ClipboardService(persistencePath: clipPath, dataProtector: protector);
        await legacyService.InitializeAsync();

        Assert.Single(legacyService.Snippets);
        Assert.Equal("Legacy Plaintext Snippet", legacyService.Snippets[0].Text);
    }
}
