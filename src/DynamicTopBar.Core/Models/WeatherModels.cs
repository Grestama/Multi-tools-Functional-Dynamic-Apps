using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Temperature measurement unit.
/// </summary>
public enum WeatherUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>
/// Confirmed geographic location for weather queries.
/// </summary>
public record WeatherLocation
{
    public required string CityName { get; init; }
    public required string Country { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public string? AdminArea { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(AdminArea)
        ? $"{CityName}, {Country}"
        : $"{CityName}, {AdminArea}, {Country}";

    public static WeatherLocation DefaultJakarta => new()
    {
        CityName = "Jakarta",
        Country = "Indonesia",
        Latitude = -6.2146,
        Longitude = 106.8451,
        AdminArea = "DKI Jakarta"
    };
}

/// <summary>
/// Current weather observations.
/// </summary>
public record CurrentWeather
{
    public double TemperatureC { get; init; }
    public double ApparentTemperatureC { get; init; }
    public int RelativeHumidity { get; init; }
    public double PrecipitationMm { get; init; }
    public int WeatherCode { get; init; }
    public required string ConditionText { get; init; }
    public required string IconGlyph { get; init; }
    public double WindSpeedKmh { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Daily weather forecast summary.
/// </summary>
public record DailyWeatherForecast
{
    public DateOnly Date { get; init; }
    public int WeatherCode { get; init; }
    public required string ConditionText { get; init; }
    public required string IconGlyph { get; init; }
    public double MaxTempC { get; init; }
    public double MinTempC { get; init; }
    public int PrecipitationProbabilityMax { get; init; }
}

/// <summary>
/// Snapshot of weather state including current conditions, 3-day forecast, cache/offline status.
/// </summary>
public record WeatherSnapshot
{
    public WeatherLocation? Location { get; init; }
    public CurrentWeather? Current { get; init; }
    public IReadOnlyList<DailyWeatherForecast> Forecast { get; init; } = Array.Empty<DailyWeatherForecast>();
    public DateTimeOffset? LastUpdated { get; init; }
    public bool IsOffline { get; init; }
    public bool IsStale { get; init; }
    public string? StatusMessage { get; init; }

    public bool HasData => Current != null;

    public static WeatherSnapshot Empty(string? message = null) => new()
    {
        StatusMessage = message ?? "No weather data loaded"
    };
}
