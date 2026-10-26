using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Weather coordinator managing location preferences, local disk caching, offline fallbacks, and rate limiting.
/// </summary>
public sealed class WeatherService : IWeatherService
{
    private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromMinutes(1);

    private readonly IWeatherProvider _provider;
    private readonly string _cacheFilePath;
    private readonly object _lock = new();

    private WeatherSnapshot _currentSnapshot = WeatherSnapshot.Empty();
    private WeatherLocation? _selectedLocation;
    private bool _isLoading;
    private DateTimeOffset _lastFetchTime = DateTimeOffset.MinValue;
    private bool _isDisposed;

    public WeatherSnapshot CurrentSnapshot
    {
        get { lock (_lock) return _currentSnapshot; }
        private set
        {
            lock (_lock) _currentSnapshot = value;
            SnapshotChanged?.Invoke(this, value);
        }
    }

    public WeatherLocation? SelectedLocation
    {
        get { lock (_lock) return _selectedLocation; }
        private set { lock (_lock) _selectedLocation = value; }
    }

    public bool IsLoading
    {
        get { lock (_lock) return _isLoading; }
        private set
        {
            lock (_lock) _isLoading = value;
            LoadingChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<WeatherSnapshot>? SnapshotChanged;
    public event EventHandler<bool>? LoadingChanged;

    public WeatherService(IWeatherProvider provider, string? cacheFilePath = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cacheFilePath = cacheFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "weather_cache.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        LoadFromCache();

        if (SelectedLocation == null)
        {
            SelectedLocation = WeatherLocation.DefaultJakarta;
        }

        // If we have cached data, display it immediately
        if (CurrentSnapshot.HasData)
        {
            // If cache is older than 30 minutes, attempt background refresh
            if (CurrentSnapshot.LastUpdated.HasValue && DateTimeOffset.UtcNow - CurrentSnapshot.LastUpdated.Value > TimeSpan.FromMinutes(30))
            {
                _ = RefreshAsync(force: false, cancellationToken);
            }
        }
        else
        {
            await RefreshAsync(force: true, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(string query, CancellationToken cancellationToken = default)
    {
        return await _provider.SearchLocationsAsync(query, cancellationToken);
    }

    public async Task SetLocationAsync(WeatherLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        SelectedLocation = location;
        await RefreshAsync(force: true, cancellationToken);
    }

    public async Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (SelectedLocation == null) return;

        lock (_lock)
        {
            if (!force && DateTimeOffset.UtcNow - _lastFetchTime < MinRefreshInterval)
            {
                return;
            }
            if (_isLoading) return;
            _isLoading = true;
        }

        LoadingChanged?.Invoke(this, true);

        try
        {
            var result = await _provider.GetWeatherAsync(SelectedLocation, cancellationToken);

            if (result.HasData && !result.IsOffline)
            {
                _lastFetchTime = DateTimeOffset.UtcNow;
                CurrentSnapshot = result;
                SaveToCache(result);
            }
            else
            {
                // Network failed or offline: preserve existing cached data with offline notice
                if (CurrentSnapshot.HasData)
                {
                    CurrentSnapshot = CurrentSnapshot with
                    {
                        IsOffline = true,
                        IsStale = true,
                        StatusMessage = "Offline (Cached data)"
                    };
                }
                else
                {
                    CurrentSnapshot = result;
                }
            }
        }
        catch (Exception ex)
        {
            if (CurrentSnapshot.HasData)
            {
                CurrentSnapshot = CurrentSnapshot with
                {
                    IsOffline = true,
                    IsStale = true,
                    StatusMessage = $"Offline ({ex.Message})"
                };
            }
            else
            {
                CurrentSnapshot = WeatherSnapshot.Empty($"Offline: {ex.Message}");
            }
        }
        finally
        {
            lock (_lock)
            {
                _isLoading = false;
            }
            LoadingChanged?.Invoke(this, false);
        }
    }

    private void SaveToCache(WeatherSnapshot snapshot)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            string tempPath = $"{_cacheFilePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _cacheFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        catch
        {
            // Ignore cache write failure
        }
    }

    private void LoadFromCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                string json = File.ReadAllText(_cacheFilePath);
                var cached = JsonSerializer.Deserialize<WeatherSnapshot>(json);
                if (cached != null)
                {
                    if (cached.Location != null)
                    {
                        SelectedLocation = cached.Location;
                    }

                    bool isStale = cached.LastUpdated.HasValue &&
                        (DateTimeOffset.UtcNow - cached.LastUpdated.Value) > TimeSpan.FromHours(1);

                    CurrentSnapshot = cached with
                    {
                        IsStale = isStale,
                        StatusMessage = isStale ? "Cached (stale)" : "Loaded from cache"
                    };
                }
            }
        }
        catch
        {
            // Ignore corrupted cache file
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
