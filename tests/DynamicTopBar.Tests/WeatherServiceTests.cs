using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class TestWeatherProvider : IWeatherProvider
{
    public string ProviderName => "TestWeatherProvider";
    public int FetchCallCount { get; private set; }
    public WeatherSnapshot? NextSnapshot { get; set; }
    public List<WeatherLocation> SearchResults { get; } = new();

    public Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<WeatherLocation>>(SearchResults);
    }

    public Task<WeatherSnapshot> GetWeatherAsync(WeatherLocation location, CancellationToken cancellationToken = default)
    {
        FetchCallCount++;
        return Task.FromResult(NextSnapshot ?? new WeatherSnapshot
        {
            Location = location,
            Current = new CurrentWeather
            {
                TemperatureC = 28.5,
                ApparentTemperatureC = 31.0,
                RelativeHumidity = 75,
                PrecipitationMm = 0.0,
                WeatherCode = 0,
                ConditionText = "Clear sky",
                IconGlyph = "\uE706",
                WindSpeedKmh = 10.2,
                Timestamp = DateTimeOffset.UtcNow
            },
            LastUpdated = DateTimeOffset.UtcNow,
            IsOffline = false
        });
    }
}

public class WeatherServiceTests : IDisposable
{
    private readonly string _tempCacheFile;

    public WeatherServiceTests()
    {
        _tempCacheFile = Path.Combine(Path.GetTempPath(), $"weather_test_cache_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempCacheFile))
        {
            try { File.Delete(_tempCacheFile); } catch { }
        }
    }

    [Theory]
    [InlineData(0, "Clear sky", "\uE706")]
    [InlineData(2, "Partly cloudy", "\uE708")]
    [InlineData(3, "Overcast", "\uE753")]
    [InlineData(61, "Rain", "\uE774")]
    [InlineData(71, "Snow", "\uE707")]
    [InlineData(95, "Thunderstorm", "\uE709")]
    public void OpenMeteoWeatherProvider_MapWmoCode_MapsAccurately(int code, string expectedCondition, string expectedGlyph)
    {
        var (condition, glyph) = OpenMeteoWeatherProvider.MapWmoCode(code);

        Assert.Equal(expectedCondition, condition);
        Assert.Equal(expectedGlyph, glyph);
    }

    [Fact]
    public void WeatherLocation_DisplayName_FormatsWithOrWithoutAdminArea()
    {
        var withAdmin = new WeatherLocation
        {
            CityName = "Jakarta",
            Country = "Indonesia",
            Latitude = -6.2,
            Longitude = 106.8,
            AdminArea = "DKI Jakarta"
        };

        var withoutAdmin = new WeatherLocation
        {
            CityName = "Bandung",
            Country = "Indonesia",
            Latitude = -6.9,
            Longitude = 107.6,
            AdminArea = null
        };

        Assert.Equal("Jakarta, DKI Jakarta, Indonesia", withAdmin.DisplayName);
        Assert.Equal("Bandung, Indonesia", withoutAdmin.DisplayName);
    }

    [Fact]
    public async Task WeatherService_SetLocation_FetchesDataAndFiresSnapshotChanged()
    {
        var provider = new TestWeatherProvider();
        using var service = new WeatherService(provider, _tempCacheFile);

        bool eventFired = false;
        service.SnapshotChanged += (s, e) => eventFired = true;

        var loc = new WeatherLocation
        {
            CityName = "Tokyo",
            Country = "Japan",
            Latitude = 35.6762,
            Longitude = 139.6503
        };

        await service.SetLocationAsync(loc);

        Assert.True(eventFired);
        Assert.Equal(1, provider.FetchCallCount);
        Assert.NotNull(service.CurrentSnapshot.Current);
        Assert.Equal(28.5, service.CurrentSnapshot.Current.TemperatureC);
        Assert.Equal("Tokyo", service.SelectedLocation?.CityName);
    }

    [Fact]
    public async Task WeatherService_Refresh_ThrottlesWithinRateLimitWindow()
    {
        var provider = new TestWeatherProvider();
        using var service = new WeatherService(provider, _tempCacheFile);

        await service.SetLocationAsync(WeatherLocation.DefaultJakarta);
        Assert.Equal(1, provider.FetchCallCount);

        // Second call immediately without force should be throttled
        await service.RefreshAsync(force: false);
        Assert.Equal(1, provider.FetchCallCount);

        // Calling with force = true should bypass rate-limiting
        await service.RefreshAsync(force: true);
        Assert.Equal(2, provider.FetchCallCount);
    }
}
