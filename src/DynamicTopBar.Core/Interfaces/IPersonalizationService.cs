using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing personalization profiles, theme preferences, schema versioning, and configuration migration.
/// </summary>
public interface IPersonalizationService : IDisposable
{
    VersionedConfiguration CurrentConfiguration { get; }
    PersonalizationProfile ActiveProfile { get; }

    event EventHandler<PersonalizationProfile>? ActiveProfileChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetActiveProfileAsync(string profileId);
    Task SaveProfileAsync(PersonalizationProfile profile);
    bool DeleteProfile(string profileId);
    string ExportConfiguration();
    bool ImportConfiguration(string jsonConfig);
    VersionedConfiguration MigrateConfiguration(string rawJson);
}
