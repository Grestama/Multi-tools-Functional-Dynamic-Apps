using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing weather location preferences, caching, rate-limiting, and refresh operations.
/// </summary>
public interface IWeatherService : IDisposable
{
    WeatherSnapshot CurrentSnapshot { get; }
    WeatherLocation? SelectedLocation { get; }
    bool IsLoading { get; }

    event EventHandler<WeatherSnapshot>? SnapshotChanged;
    event EventHandler<bool>? LoadingChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(string query, CancellationToken cancellationToken = default);
    Task SetLocationAsync(WeatherLocation location, CancellationToken cancellationToken = default);
    Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default);
}
