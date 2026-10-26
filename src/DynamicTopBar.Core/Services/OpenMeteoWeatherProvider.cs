using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Weather provider implementation using the free Open-Meteo Forecast and Geocoding APIs.
/// Licensing: Free for non-commercial use with attribution (Open-Meteo.com).
/// </summary>
public sealed class OpenMeteoWeatherProvider : IWeatherProvider
{
    private const string GeocodingEndpoint = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastEndpoint = "https://api.open-meteo.com/v1/forecast";

    private readonly HttpClient _httpClient;

    public string ProviderName => "Open-Meteo";

    public OpenMeteoWeatherProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<WeatherLocation>();
        }

        string url = $"{GeocodingEndpoint}?name={Uri.EscapeDataString(query.Trim())}&count=6&language=en&format=json";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<WeatherLocation>();
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("results", out var resultsProp) || resultsProp.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<WeatherLocation>();
            }

            var locations = new List<WeatherLocation>();
            foreach (var item in resultsProp.EnumerateArray())
            {
                string name = item.GetProperty("name").GetString() ?? "";
                string country = item.TryGetProperty("country", out var cProp) ? cProp.GetString() ?? "" : "";
                double lat = item.GetProperty("latitude").GetDouble();
                double lon = item.GetProperty("longitude").GetDouble();
                string? admin = item.TryGetProperty("admin1", out var aProp) ? aProp.GetString() : null;

                locations.Add(new WeatherLocation
                {
                    CityName = name,
                    Country = country,
                    Latitude = lat,
                    Longitude = lon,
                    AdminArea = admin
                });
            }

            return locations;
        }
        catch
        {
            return Array.Empty<WeatherLocation>();
        }
    }

    public async Task<WeatherSnapshot> GetWeatherAsync(WeatherLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        string latStr = location.Latitude.ToString("F4", CultureInfo.InvariantCulture);
        string lonStr = location.Longitude.ToString("F4", CultureInfo.InvariantCulture);

        string url = $"{ForecastEndpoint}?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return WeatherSnapshot.Empty($"Weather API returned {(int)response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOpenMeteoJson(json, location);
        }
        catch (HttpRequestException ex)
        {
            return new WeatherSnapshot
            {
                Location = location,
                IsOffline = true,
                StatusMessage = $"Network offline: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return WeatherSnapshot.Empty($"Failed to parse weather: {ex.Message}");
        }
    }

    public static WeatherSnapshot ParseOpenMeteoJson(string json, WeatherLocation location)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        CurrentWeather? current = null;
        if (root.TryGetProperty("current", out var curProp))
        {
            double temp = curProp.GetProperty("temperature_2m").GetDouble();
            double appTemp = curProp.TryGetProperty("apparent_temperature", out var atProp) ? atProp.GetDouble() : temp;
            int humidity = curProp.TryGetProperty("relative_humidity_2m", out var rhProp) ? rhProp.GetInt32() : 0;
            double precip = curProp.TryGetProperty("precipitation", out var pProp) ? pProp.GetDouble() : 0.0;
            int code = curProp.GetProperty("weather_code").GetInt32();
            double wind = curProp.TryGetProperty("wind_speed_10m", out var wsProp) ? wsProp.GetDouble() : 0.0;

            var (condText, glyph) = MapWmoCode(code);

            current = new CurrentWeather
            {
                TemperatureC = Math.Round(temp, 1),
                ApparentTemperatureC = Math.Round(appTemp, 1),
                RelativeHumidity = humidity,
                PrecipitationMm = precip,
                WeatherCode = code,
                ConditionText = condText,
                IconGlyph = glyph,
                WindSpeedKmh = Math.Round(wind, 1),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        var forecasts = new List<DailyWeatherForecast>();
        if (root.TryGetProperty("daily", out var dailyProp))
        {
            var dates = dailyProp.GetProperty("time");
            var codes = dailyProp.GetProperty("weather_code");
            var maxTemps = dailyProp.GetProperty("temperature_2m_max");
            var minTemps = dailyProp.GetProperty("temperature_2m_min");
            JsonElement? precipProbs = dailyProp.TryGetProperty("precipitation_probability_max", out var ppProp) ? ppProp : null;

            int count = Math.Min(dates.GetArrayLength(), 3); // 3-day forecast glance
            for (int i = 0; i < count; i++)
            {
                if (DateOnly.TryParse(dates[i].GetString(), out var date))
                {
                    int dCode = codes[i].GetInt32();
                    double maxT = maxTemps[i].GetDouble();
                    double minT = minTemps[i].GetDouble();
                    int pProb = precipProbs.HasValue && precipProbs.Value.GetArrayLength() > i 
                        ? (precipProbs.Value[i].ValueKind == JsonValueKind.Number ? precipProbs.Value[i].GetInt32() : 0)
                        : 0;

                    var (dText, dGlyph) = MapWmoCode(dCode);

                    forecasts.Add(new DailyWeatherForecast
                    {
                        Date = date,
                        WeatherCode = dCode,
                        ConditionText = dText,
                        IconGlyph = dGlyph,
                        MaxTempC = Math.Round(maxT, 1),
                        MinTempC = Math.Round(minT, 1),
                        PrecipitationProbabilityMax = pProb
                    });
                }
            }
        }

        return new WeatherSnapshot
        {
            Location = location,
            Current = current,
            Forecast = forecasts,
            LastUpdated = DateTimeOffset.UtcNow,
            IsOffline = false,
            IsStale = false,
            StatusMessage = "Live Open-Meteo data"
        };
    }

    public static (string Condition, string Glyph) MapWmoCode(int code)
    {
        return code switch
        {
            0 => ("Clear sky", "\uE706"), // Sunny
            1 => ("Mainly clear", "\uE706"),
            2 => ("Partly cloudy", "\uE708"), // Partly sunny
            3 => ("Overcast", "\uE753"), // Cloudy
            45 or 48 => ("Foggy", "\uE753"),
            51 or 53 or 55 => ("Drizzle", "\uE774"), // Rain
            56 or 57 => ("Freezing drizzle", "\uE774"),
            61 or 63 or 65 => ("Rain", "\uE774"),
            66 or 67 => ("Freezing rain", "\uE774"),
            71 or 73 or 75 => ("Snow", "\uE707"), // Snow
            77 => ("Snow grains", "\uE707"),
            80 or 81 or 82 => ("Rain showers", "\uE774"),
            85 or 86 => ("Snow showers", "\uE707"),
            95 => ("Thunderstorm", "\uE709"), // Lightning
            96 or 99 => ("Thunderstorm with hail", "\uE709"),
            _ => ("Cloudy", "\uE753")
        };
    }
}
