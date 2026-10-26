using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum AppThemePreference
{
    Dark,
    Light,
    SystemDefault
}

public enum PanelSizePreset
{
    Compact, // 376 x 224 DIPs
    Spacious // 480 x 280 DIPs
}

/// <summary>
/// Named user profile defining customized modules, color theme, panel dimensions, and tool favorites.
/// </summary>
public record PersonalizationProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public AppThemePreference Theme { get; init; } = AppThemePreference.Dark;
    public string AccentColorHex { get; init; } = "#00B4FF";
    public PanelSizePreset SizePreset { get; init; } = PanelSizePreset.Compact;
    public bool ReduceMotion { get; init; } = false;
    public List<string> EnabledFeatureIds { get; init; } = new();
    public List<string> FeatureOrder { get; init; } = new();
    public List<string> FavoriteToolIds { get; init; } = new();
}

/// <summary>
/// Versioned configuration bundle supporting forward/backward schema migration and backup export/import.
/// </summary>
public record VersionedConfiguration
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string ActiveProfileId { get; init; } = "profile.default";
    public List<PersonalizationProfile> Profiles { get; init; } = new();
    public FeaturePreferences BasePreferences { get; init; } = FeaturePreferences.Default;
    public DateTimeOffset LastModified { get; init; } = DateTimeOffset.UtcNow;
}
