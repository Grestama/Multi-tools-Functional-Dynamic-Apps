using System;
using System.IO;
using System.Linq;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class AdvancedClipboardTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _historyFile;
    private readonly ClipboardService _clipboard;

    public AdvancedClipboardTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DTB_ClipboardTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _historyFile = Path.Combine(_testDir, "test_clipboard.json");
        _clipboard = new ClipboardService(platformAdapter: null, persistencePath: _historyFile);
    }

    public void Dispose()
    {
        _clipboard.Dispose();
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [Theory]
    [InlineData("https://google.com", ClipboardContentType.Url)]
    [InlineData("http://localhost:5000/api", ClipboardContentType.Url)]
    [InlineData("{\"title\":\"demo\",\"count\":10}", ClipboardContentType.Json)]
    [InlineData("[1, 2, 3, 4]", ClipboardContentType.Json)]
    [InlineData("12345.67", ClipboardContentType.Number)]
    [InlineData("user@example.com", ClipboardContentType.Email)]
    [InlineData("public class MyService { void Test() { } }", ClipboardContentType.Code)]
    [InlineData("Just plain text note", ClipboardContentType.Text)]
    public void DetectContentType_CategorizesCorrectly(string input, ClipboardContentType expected)
    {
        var category = ClipboardService.DetectContentType(input);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0e3c", true)]
    [InlineData("4532-1234-5678-9010", true)]
    [InlineData("-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA...", true)]
    [InlineData("Authorization: Bearer my-secret-token-1234", true)]
    [InlineData("Just normal text with numbers 1234 5678", false)]
    public void IsSensitivePattern_DetectsSecretsAccurately(string input, bool expectedSensitive)
    {
        bool isSensitive = ClipboardService.IsSensitivePattern(input);
        Assert.Equal(expectedSensitive, isSensitive);
    }

    [Fact]
    public void AddSnippet_WhenSensitivePatternsExcluded_DoesNotCapture()
    {
        _clipboard.SetOptIn(true);
        _clipboard.UpdatePreferences(new ClipboardPreferences
        {
            ExcludeSensitivePatterns = true
        });

        _clipboard.AddSnippet("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0e3c");
        Assert.Empty(_clipboard.Snippets);

        _clipboard.AddSnippet("Normal safe text");
        Assert.Single(_clipboard.Snippets);
    }

    [Fact]
    public void AddSnippet_WhenAppExcluded_DoesNotCapture()
    {
        _clipboard.SetOptIn(true);
        _clipboard.UpdatePreferences(new ClipboardPreferences
        {
            ExcludedApplications = new() { "1Password", "Bitwarden" }
        });

        _clipboard.AddSnippet("MySecretPassword", sourceAppName: "1Password");
        Assert.Empty(_clipboard.Snippets);

        _clipboard.AddSnippet("Public snippet", sourceAppName: "Chrome");
        Assert.Single(_clipboard.Snippets);
    }

    [Fact]
    public void Search_FiltersByQueryAndCategory()
    {
        _clipboard.SetOptIn(true);
        _clipboard.AddSnippet("https://github.com/project");
        _clipboard.AddSnippet("{\"user\":\"alice\"}");
        _clipboard.AddSnippet("Note about github project");

        var urls = _clipboard.Search(null, ClipboardContentType.Url);
        Assert.Single(urls);
        Assert.Equal("https://github.com/project", urls[0].Text);

        var githubItems = _clipboard.Search("github");
        Assert.Equal(2, githubItems.Count);
    }

    [Fact]
    public void AutoPrune_EnforcesCapacityAndAge()
    {
        _clipboard.SetOptIn(true);
        _clipboard.UpdatePreferences(new ClipboardPreferences
        {
            MaxSnippetCapacity = 5,
            AutoPruneDays = 0 // disabled age prune
        });

        for (int i = 0; i < 10; i++)
        {
            _clipboard.AddSnippet($"Snippet {i}");
        }

        Assert.Equal(5, _clipboard.Snippets.Count);
    }

    [Fact]
    public void ExportAndImportSnippets_RoundtripSuccessfully()
    {
        _clipboard.SetOptIn(true);
        _clipboard.AddSnippet("Snippet 1");
        _clipboard.AddSnippet("https://example.com");

        string exportFile = Path.Combine(_testDir, "exported_snippets.json");
        bool exported = _clipboard.ExportSnippets(exportFile);
        Assert.True(exported);
        Assert.True(File.Exists(exportFile));

        _clipboard.ClearHistory();
        Assert.Empty(_clipboard.Snippets);

        bool imported = _clipboard.ImportSnippets(exportFile);
        Assert.True(imported);
        Assert.Equal(2, _clipboard.Snippets.Count);
    }

    [Fact]
    public void StrictPrivacy_WhenOptInFalse_CapturesNothing()
    {
        _clipboard.SetOptIn(false); // default disabled state
        _clipboard.AddSnippet("Ignored content");

        Assert.Empty(_clipboard.Snippets);
    }
}
