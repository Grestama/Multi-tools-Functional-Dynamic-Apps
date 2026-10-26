using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class ToolRegistryTests
{
    private readonly ToolRegistry _registry = new();
    private readonly DeveloperUtilitiesService _devUtils = new();
    private readonly FileToolsService _fileTools = new();

    [Fact]
    public void RegisterAndUnregister_ManagesCatalog()
    {
        var tool = new StandardDesktopTools.JsonFormatTool(_devUtils);
        _registry.Register(tool);

        Assert.Single(_registry.Tools);
        Assert.Equal("tool-json-format", _registry.Tools[0].Id);
        Assert.Equal("Developer", _registry.Tools[0].Category);

        var retrieved = _registry.GetTool("tool-json-format");
        Assert.NotNull(retrieved);
        Assert.Equal("JSON Formatter", retrieved.DisplayName);

        bool removed = _registry.Unregister("tool-json-format");
        Assert.True(removed);
        Assert.Empty(_registry.Tools);
    }

    [Fact]
    public void GetCategories_ReturnsDistinctSortedCategories()
    {
        foreach (var tool in StandardDesktopTools.CreateAll(_devUtils, _fileTools))
        {
            _registry.Register(tool);
        }

        var categories = _registry.GetCategories();
        Assert.Contains("Developer", categories);
        Assert.Contains("File Tools", categories);

        var devTools = _registry.GetToolsByCategory("Developer");
        Assert.NotEmpty(devTools);
        Assert.All(devTools, t => Assert.Equal("Developer", t.Category));
    }

    [Fact]
    public async Task ExecuteToolAsync_DispatchesExecutionCorrectly()
    {
        var tool = new StandardDesktopTools.JsonFormatTool(_devUtils);
        _registry.Register(tool);

        var ctx = new ToolContext
        {
            InputText = "{\"app\":\"DynamicTopBar\"}",
            Parameters = { ["mode"] = "Prettify", ["indent"] = 2 }
        };

        var result = await _registry.ExecuteToolAsync("tool-json-format", ctx);

        Assert.True(result.Success);
        Assert.NotNull(result.OutputText);
        Assert.Contains("  \"app\": \"DynamicTopBar\"", result.OutputText);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnregisteredTool_ReturnsFailure()
    {
        var result = await _registry.ExecuteToolAsync("non-existent-tool", new ToolContext());

        Assert.False(result.Success);
        Assert.Contains("not registered", result.ErrorMessage);
    }

    [Fact]
    public async Task StandardTools_UuidAndBase64_ExecuteSuccessfully()
    {
        var uuidTool = new StandardDesktopTools.UuidTool(_devUtils);
        var base64Tool = new StandardDesktopTools.Base64Tool(_devUtils);
        _registry.Register(uuidTool);
        _registry.Register(base64Tool);

        // UUID
        var uuidCtx = new ToolContext { Parameters = { ["count"] = 3 } };
        var uuidRes = await _registry.ExecuteToolAsync("tool-uuid", uuidCtx);
        Assert.True(uuidRes.Success);
        Assert.Equal(3, uuidRes.OutputText?.Split(Environment.NewLine).Length);

        // Base64
        var b64Ctx = new ToolContext
        {
            InputText = "Hello World",
            Parameters = { ["operation"] = "Encode" }
        };
        var b64Res = await _registry.ExecuteToolAsync("tool-base64", b64Ctx);
        Assert.True(b64Res.Success);
        Assert.Equal("SGVsbG8gV29ybGQ=", b64Res.OutputText);
    }

    [Fact]
    public async Task StandardTools_UrlCodec_EncodesAndDecodesSuccessfully()
    {
        var urlTool = new StandardDesktopTools.UrlCodecTool(_devUtils);
        _registry.Register(urlTool);

        var encodeCtx = new ToolContext
        {
            InputText = "https://example.com/search?q=winui 3 & net9",
            Parameters = { ["operation"] = "Encode" }
        };
        var encRes = await _registry.ExecuteToolAsync("tool-url-codec", encodeCtx);
        Assert.True(encRes.Success);
        Assert.Contains("%20", encRes.OutputText);

        var decodeCtx = new ToolContext
        {
            InputText = encRes.OutputText,
            Parameters = { ["operation"] = "Decode" }
        };
        var decRes = await _registry.ExecuteToolAsync("tool-url-codec", decodeCtx);
        Assert.True(decRes.Success);
        Assert.Equal("https://example.com/search?q=winui 3 & net9", decRes.OutputText);
    }

    [Fact]
    public async Task ExecuteToolAsync_WhenCanceled_ReturnsCanceledResult()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var tool = new StandardDesktopTools.JsonFormatTool(_devUtils);
        _registry.Register(tool);

        var result = await _registry.ExecuteToolAsync("tool-json-format", new ToolContext { InputText = "{}" }, cts.Token);
        Assert.False(result.Success);
        Assert.Contains("canceled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
