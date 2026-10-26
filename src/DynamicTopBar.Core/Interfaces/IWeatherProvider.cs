using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Abstract weather data provider interface allowing swappable weather backends.
/// </summary>
public interface IWeatherProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(string query, CancellationToken cancellationToken = default);
    Task<WeatherSnapshot> GetWeatherAsync(WeatherLocation location, CancellationToken cancellationToken = default);
}
