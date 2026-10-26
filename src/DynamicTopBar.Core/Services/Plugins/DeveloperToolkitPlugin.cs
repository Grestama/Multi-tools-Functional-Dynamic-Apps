using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services.Plugins;

/// <summary>
/// Sample reference plugin demonstrating modular tool and palette command exposure.
/// </summary>
public sealed class DeveloperToolkitPlugin : PluginBase
{
    private readonly IDeveloperUtilitiesService _devUtils;

    public override PluginManifest Manifest => new()
    {
        Id = "plugin.dev-toolkit",
        Name = "Developer Toolkit Extension",
        Version = "1.0.0",
        Author = "DynamicTopBar Official",
        Description = "Provides rapid developer shortcuts, JWT decoder glance, and hex color utilities.",
        MinAppVersion = "1.0.0",
        Permissions = PluginPermission.Clipboard,
        Tags = new[] { "developer", "jwt", "tools", "hex" }
    };

    public DeveloperToolkitPlugin(IDeveloperUtilitiesService devUtils)
    {
        _devUtils = devUtils ?? throw new ArgumentNullException(nameof(devUtils));
    }

    public override IEnumerable<PaletteCommand> GetCommands()
    {
        yield return new PaletteCommand
        {
            Id = "cmd-dev-jwt-glance",
            Title = "Developer: Decode JWT Payload",
            Description = "Decode token segments into formatted JSON",
            Category = "Developer Plugin",
            Keywords = new[] { "jwt", "token", "auth", "bearer", "decode" },
            IconGlyph = "\uE71D",
            Action = () => { /* Action callback */ }
        };
    }
}
